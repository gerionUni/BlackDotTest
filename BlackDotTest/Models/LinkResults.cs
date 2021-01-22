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
