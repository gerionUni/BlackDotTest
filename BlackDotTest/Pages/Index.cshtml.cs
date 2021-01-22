using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Net;
using System.Collections.Specialized;
using System.Text;
using System.Collections;
using System.IO;
using System.Linq;
using BlackDotTest.Models;
using System.Text.RegularExpressions;
using System.Web;


namespace BlackDotTest.Pages
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;
        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }
        //used to keep a list of all search engines
        public List<SearchEngines> searchEngines = new List<SearchEngines>();

        //used to keep a list of all search results from all engines
        private List<LinkResults> _results = new List<LinkResults>();

        //used to keep the value of the search term inputed by the user
        public string SearchTerm = "";

        //property for the results to be passed to the razor view
        public List<LinkResults> Results { get { return _results; } set { _results = value; } }


        public void OnGet()
        {
            
        }

        public IActionResult OnPost()
        {
            //create the search engines and add them
            //normally this is done separetely, but this is a test 
            searchEngines = new List<SearchEngines>();
            SearchEngines Google = new SearchEngines("Google", "http://google.com/search?q=",
                "/url?q=", "//a[@href]");
            SearchEngines Bing = new SearchEngines("Bing", "http://bing.com/search?q=",
                "", "//a[@href]"); 
            SearchEngines Yahoo = new SearchEngines("Yahoo", "https://search.yahoo.com/search?p=",
                "", "//a[@href]");
            searchEngines.Add(Google);
            searchEngines.Add(Yahoo);
            searchEngines.Add(Bing);

            //get the value of the search term
            SearchTerm = Request.Form["searchTerm"].ToString().Trim();
            UseSearchTerm(SearchTerm);
            
            return Page();
        }

        //this function call the search function for each engine
        //and also order the result by total number of times the search term is mentioned
        public void UseSearchTerm(string searchTerm)
        {
            foreach (SearchEngines se in searchEngines)
            {
                SearchEngine(se, searchTerm);
            }
            //remove results with 0 mentioned, most probably just noise from engines
            Results.RemoveAll(x => x.MentionedTimesTotal == 0);
            Results = Results.OrderByDescending(x => x.MentionedTimesTotal).
                ThenByDescending(x => x.MentionedInLinks).ToList();

        }

        //this function take a search engine and get all the links
        public void SearchEngine(SearchEngines searchEngine, string searchTerm)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                byte[] ResultsBuffer = new byte[8192];
                //create the search url using the engine url and search term
                string SearchResults = searchEngine.Url + searchTerm.Trim();
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(SearchResults);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                
                //read the response and transform into a string
                Stream resStream = response.GetResponseStream();
                string tempString;
                int count;
                do
                {
                    count = resStream.Read(ResultsBuffer, 0, ResultsBuffer.Length);
                    if (count != 0)
                    {
                        tempString = Encoding.ASCII.GetString(ResultsBuffer, 0, count);
                        sb.Append(tempString);
                    }
                }

                while (count > 0);
                string sbb = sb.ToString();

                //convert the response into a html document with the help of HTMLAgilityPack
                HtmlDocument html = new HtmlDocument();
                html.OptionOutputAsXml = true;
                html.LoadHtml(sbb);
                HtmlNode doc = html.DocumentNode;
                //doc.InnerHtml= HttpUtility.HtmlDecode(doc.InnerHtml);
                //find each link href inside the document
                foreach (HtmlNode link in doc.SelectNodes(searchEngine.NodeSelector))
                {
                    //
                    string hrefValue = link.GetAttributeValue("href", string.Empty);
                    //try to eliminate the ones which are adds or similar
                    if (!hrefValue.ToString().ToUpper().Contains(searchEngine.Name.ToUpper())  
                        && hrefValue.ToString().Contains(searchEngine.EscapeString)
                        && (hrefValue.ToString().ToUpper().Contains("HTTP://") 
                        || hrefValue.ToString().ToUpper().Contains("HTTPS://")))
                    {
                        int index = hrefValue.IndexOf("&");
                        //check if there are additional parameters added by the engine, like google
                        //and remove them if needed, otherwise keep the url
                        if (index > 0)
                        {
                            hrefValue = hrefValue.Substring(0, index);
                            //use the function which will open each link and search for the term inside
                            LinkResults currentLink = SearchLink(hrefValue, searchTerm, searchEngine.Name);
                            if (currentLink != null)
                            {
                                _results.Add(currentLink);
                            }
                            
                        }
                        else
                        {                          
                            LinkResults currentLink = SearchLink(hrefValue, searchTerm, searchEngine.Name);
                            if (currentLink != null)
                            {
                                _results.Add(currentLink);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }

        }
        
        //this function open each link and searches inside how many times the term is
        public LinkResults SearchLink(string url, string searchTerm, string engine)
        {
            try
            {
                LinkResults result = new LinkResults();
                StringBuilder sb = new StringBuilder();
                byte[] ResultsBuffer = new byte[8192];
                string searchURL = "";
                //cut the url if it is google
                if (engine== "Google")
                {
                    searchURL = HttpUtility.UrlDecode(url.Substring(7));
                }
                else
                {
                    searchURL = HttpUtility.UrlDecode(url);
                }
                
                //check if we already searched this link
                if (Results.Where(x => x.LinkUrl == searchURL ).Count() > 0)
                {
                    //if we already found it then check if its a new search engine
                    LinkResults foundLink = Results.Where(x => x.LinkUrl == searchURL 
                    && !x.SearchEngine.Contains(engine)).FirstOrDefault();
                    
                    //if yes then add it so the user knows this page was found in these engines
                    if (foundLink != null)
                    {
                        foundLink.SearchEngine = foundLink.SearchEngine + " and " + engine;
                        return null;
                    }
                    else
                    {
                        //case same engine returned same link twice
                        return null;
                    }
                    
                }
                //if we reached here its a new page so add the page and the engine
                result.LinkUrl = searchURL;
                result.SearchEngine = engine;
                
                //open the page to search inside for the search term
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(searchURL);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                //read the response and transform into a string
                Stream resStream = response.GetResponseStream();
                string tempString;
                int count;
                do
                {
                    count = resStream.Read(ResultsBuffer, 0, ResultsBuffer.Length);
                    if (count != 0)
                    {
                        tempString = Encoding.ASCII.GetString(ResultsBuffer, 0, count);
                        sb.Append(tempString);
                    }
                }

                while (count > 0);
                string sbb = sb.ToString();

                //convert the response into a html document with the help of HTMLAgilityPack
                HtmlDocument html = new HtmlDocument();
                html.OptionOutputAsXml = true;
                html.LoadHtml(sbb);
                HtmlNode doc = html.DocumentNode;
                
                //if no links found then 0 mentioned in links
                if (doc.SelectNodes("//a[@href]") != null)
                {
                    foreach (HtmlNode link in doc.SelectNodes("//a[@href]"))
                    {
                        string hrefValue = link.GetAttributeValue("href", string.Empty);
                        if (hrefValue.Contains(searchTerm))
                        {
                            //if a link mention the search term increase
                            result.MentionedInLinks += 1;
                        }
                    }
                }

                //use regex to find how many times the search term is found
                result.MentionedTimesTotal = Regex.Matches(doc.InnerText.ToUpper(), searchTerm.ToUpper()).Count;
                return result;
            }
            catch (Exception ex)
            {
                //in case of errors it could be 404 or 403 so some logging would be required here
                //to keep track just for the count of them, but this is a test
                return null;
                //throw ex;
            }

        }
    }
}
