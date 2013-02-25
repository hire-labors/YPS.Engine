using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YPS.Engine.Core.Tools;

namespace YPS.Engine.Core.Models
{
    public class DirectoryProviderSetting
    {
        public ServicedCountryEnum ServicedCountry { get; set; }
        public string Name { get; set; }
        public string BaseUrl { get; set; }
        public string SearchUrlPattern { get; set; }
        public bool IsActive { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public class DirectoryProviderSettings : List<DirectoryProviderSetting>
        {

            public DirectoryProviderSettings()
            {
                this.AddRange(

                    new List<DirectoryProviderSetting>()
                    {
                        new DirectoryProviderSetting() { 
                            ServicedCountry = ServicedCountryEnum.Canada, 
                            Name = "YellowPage - Canada", 
                            BaseUrl ="http://www.yellowpages.ca", 
                            SearchUrlPattern  = "http://www.yellowpages.ca/search/si/{PAGENO}/{SEARCHITEM}/{LOCATION}?showDD=true",
                            IsActive=true
                        },
                        new DirectoryProviderSetting() { 
                            ServicedCountry = ServicedCountryEnum.Australia, 
                            Name = "YellowPage - Australia", 
                            BaseUrl ="http://www.yellowpages.com.au", 
                            SearchUrlPattern  = "http://www.yellowpages.com.au/search/listings?showAllLocations=false&referredBy=YOL&eventType=pagination&selectedViewMode=list&clue={SEARCHITEM}&context=businessTypeSearch&pageNumber={PAGENO}&locationClue={LOCATION}",
                            IsActive=true
                        },
                        new DirectoryProviderSetting() { 
                            ServicedCountry = ServicedCountryEnum.United_Kingdom, 
                            Name = "YellowPage - United Kingdom", 
                            BaseUrl ="http://www.yell.com", 
                            SearchUrlPattern  = "www.yell.com/ucs/UcsSearchAction.do?keywords={SEARCHITEM}&location={LOCATION}&pageNum={PAGENO}",
                            IsActive=true
                        },

                });
            }

        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dirProviderSetting"></param>
        /// <param name="searchItem"></param>
        /// <param name="searchLocation"></param>
        /// <param name="pageNo"></param>
        /// <returns></returns>
        public static string BuildSearchUrl(DirectoryProviderSetting dirProviderSetting, string searchItem, string searchLocation, int pageNo)
        {
            string urlPattern = dirProviderSetting.SearchUrlPattern;
            searchItem = HtmlUtil.EncodeQueryStringSegment(searchItem);
            searchLocation = HtmlUtil.EncodeQueryStringSegment(searchLocation);

            return urlPattern.Replace("{SEARCHITEM}", searchItem)
                        .Replace("{LOCATION}", searchLocation)
                        .Replace("{PAGENO}", pageNo.ToString());

            //return string.Format(urlPattern, pageNo, searchItem, searchLocation);
        }

    }
}
