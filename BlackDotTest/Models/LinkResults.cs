/* class will store the page which has been searched and which search engine has been used on it
 * alongside number of links with the search term and the total times the search term was found
 * 21/01/2021 - Gerion
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlackDotTest.Models
{
    public class LinkResults
    {
        public string LinkUrl { get; set; }
        public int MentionedTimesTotal { get; set; }
        public int MentionedInLinks { get; set; }
        public string SearchEngine { get; set; }
    }
}
