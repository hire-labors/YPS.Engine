using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YPS.Engine.Core.Models
{
    public class SearchItem
    {
        public string searchItem { get; set; }
        public string searchLocation { get; set; }
        public SearchResult searchResult { get; set; }
        public int? pagesToProcess { get; set; }

        public class SearchResult
        {
            public bool PageOnError { get; set; }
            public string PageErrorMessage { get; set; }

            public int TotalPages { get; set; }
            public int TotalResults { get; set; }
            public int ResultsPerPage { get; set; }
            public List<UrlPointer> pointers { get; set; }

            public class UrlPointer
            {
                public int PageNo { get; set; }
                public Uri SearchUrl { get; set; }
                public string SearchHtml { get; set; }
                public bool IsValid { get; set; }
            }
        }


    }
}
