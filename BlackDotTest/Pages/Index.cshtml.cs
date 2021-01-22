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
        public List<SearchEngines> searchEngines = new List<SearchEngines>();
        private List<LinkResults> _results = new List<LinkResults>();
        public string SearchTerm = "";
        public bool ExcactTerm = false;
        public List<LinkResults> Results { get { return _results; } set { _results = value; } }
        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }
        //constructor just for unit testing
       

        public void OnGet()
        {
            
        }

        public IActionResult OnPost()
        {
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
            SearchTerm = Request.Form["searchTerm"].ToString().Trim();
            UseSearchTerm(SearchTerm);
            
            return Page();
        }

        public void UseSearchTerm(string searchTerm)
        {
            foreach (SearchEngines se in searchEngines)
            {
                SearchEngine(se, searchTerm);
            }
            Results = Results.OrderByDescending(x => x.MentionedTimesTotal).
                ThenByDescending(x => x.MentionedInLinks).ToList();
           
    }
        public void SearchEngine(SearchEngines searchEngine, string searchTerm)
        {
            try
            {
                StringBuilder sb = new StringBuilder();
                byte[] ResultsBuffer = new byte[8192];
                string SearchResults = searchEngine.Url + searchTerm.Trim();
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(SearchResults);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

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

                HtmlDocument html = new HtmlDocument();
                html.OptionOutputAsXml = true;
                html.LoadHtml(sbb);
                HtmlNode doc = html.DocumentNode;
                doc.InnerHtml= HttpUtility.HtmlDecode(doc.InnerHtml);
                foreach (HtmlNode link in doc.SelectNodes(searchEngine.NodeSelector))
                {
                    //HtmlAttribute att = link.Attributes["href"];
                    string hrefValue = link.GetAttributeValue("href", string.Empty);
                    if (!hrefValue.ToString().ToUpper().Contains(searchEngine.Name.ToUpper())  
                        && hrefValue.ToString().Contains(searchEngine.EscapeString)
                        && (hrefValue.ToString().ToUpper().Contains("HTTP://") 
                        || hrefValue.ToString().ToUpper().Contains("HTTPS://")))
                    {
                        int index = hrefValue.IndexOf("&");
                        if (index > 0)
                        {
                            hrefValue = hrefValue.Substring(0, index);
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

        public LinkResults SearchLink(string url, string searchTerm, string engine)
        {
            try
            {
                LinkResults result = new LinkResults();
                StringBuilder sb = new StringBuilder();
                byte[] ResultsBuffer = new byte[8192];
                string searchURL = "";
                if (engine== "Google")
                {
                    searchURL = HttpUtility.UrlDecode(url.Substring(7));
                }
                else
                {
                    searchURL = HttpUtility.UrlDecode(url);
                }                 
                if (Results.Where(x => x.LinkUrl == searchURL ).Count() > 0)
                {
                    LinkResults foundLink = Results.Where(x => x.LinkUrl == searchURL 
                    && !x.SearchEngine.Contains(engine)).FirstOrDefault();
                    if (foundLink != null)
                    {
                        foundLink.SearchEngine = foundLink.SearchEngine + " and " + engine;
                        return null;
                    }
                    else
                    {
                        return null;
                    }
                    
                }
                result.LinkUrl = searchURL;
                result.SearchEngine = engine;
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(searchURL);

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

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

                HtmlDocument html = new HtmlDocument();
                html.OptionOutputAsXml = true;
                html.LoadHtml(sbb);
                HtmlNode doc = html.DocumentNode;

                if (doc.SelectNodes("//a[@href]") != null)
                {
                    foreach (HtmlNode link in doc.SelectNodes("//a[@href]"))
                    {
                        string hrefValue = link.GetAttributeValue("href", string.Empty);
                        if (hrefValue.Contains(searchTerm))
                        {
                            result.MentionedInLinks += 1;
                        }
                    }
                }
               
                result.MentionedTimesTotal = Regex.Matches(doc.InnerText.ToUpper(), searchTerm.ToUpper()).Count;
                return result;
            }
            catch (Exception ex)
            {
                return null;
                //throw ex;
            }

        }
    }
}
