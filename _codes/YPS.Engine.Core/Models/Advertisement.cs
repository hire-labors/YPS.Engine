using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YPS.Engine.Core.Models
{
    public class Advertisement
    {
        public string BusinessName { get; set; }
        public string Phone { get; set; }
        public string Fax { get; set; }
        public string FullAddress { get; set; }
        public string StreetBlk { get; set; }
        public string Locality { get; set; }
        public string Region { get; set; }
        public string PostalCode { get; set; }
        public string Latitude { get; set; }
        public string Longtitude { get; set; }
        public string GoogleMap { get; set; }
        public string Rating { get; set; }
        public string Website { get; set; }
        public string EmailAddress { get; set; }
        public string Description { get; set; }
        public string Keywords { get; set; }
        public string Locations { get; set; }
        //public DateTime DateAdded { get; set; }

        public string SourceLink { get; set; }
        public string AdvertiserLink { get; set; }

        public class Advertisements : List<Advertisement>
        {
        }

        #region static members

        public static string Resolve(string val)
        {
            val = val.Replace("&amp;", "&");
            val = val.TrimEnd(',');
            val = val.Replace("%3A", ":").Replace("%2F", "/");
            val = val.Replace("&#034;", "\"");
            val = val.Replace("&#039;", "'");
            val = val.Replace("<br />", "\n");
            val = val.Replace("<br/>", "\n");
            val = val.Trim();
            return val;
        }

        #endregion
    }
}
