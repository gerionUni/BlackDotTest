﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BlackDotTest.Models
{
    public class SearchEngines
    {
        private string _name;
        private string _url;
        private string _escapeString;
        private string _nodeSelector;

        public SearchEngines(string name, string url,string escapeString,string nodeSelector)
        {
            _name = name;
            _url = url;
            _escapeString= escapeString;
            _nodeSelector = nodeSelector;
        }

        public string Name
        {
            get { return _name; }
            set { _name = value; }
        }
        public string Url
        {
            get { return _url; }
            set { _url = value; }
        }
        public string EscapeString
        {
            get { return _escapeString; }
            set { _escapeString = value; }
        }
        public string NodeSelector
        {
            get { return _nodeSelector; }
            set { _nodeSelector = value; }
        }

    }
}

 