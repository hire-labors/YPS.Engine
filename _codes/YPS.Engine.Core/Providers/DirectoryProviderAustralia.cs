using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using YPS.Engine.Core.Tools;
using System.Threading;
using System.Threading.Tasks;
using YPS.Engine.Core.Interfaces;
using YPS.Engine.Core.Models;
using System.Diagnostics;

namespace YPS.Engine.Core.Providers
{
    public class DirectoryProviderAustralia : DirectoryProviderBase
    {
        /// <summary>
        /// 
        /// </summary>
        private YPTool ypTool { get; set; }

        #region constructor

        /// <summary>
        /// 
        /// </summary>
        /// <param name="directoryProviderSetting"></param>
        public DirectoryProviderAustralia(Models.DirectoryProviderSetting directoryProviderSetting)
            : base(directoryProviderSetting)
        {
            this.ypTool = new YPTool(directoryProviderSetting, base.InvokeEventFrameworkException);
        }

        #endregion

        #region override methods

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override Models.Advertisement.Advertisements StartSearchResultProcessCallback()
        {
            Models.Advertisement.Advertisements _ads = null;

            if(base.SearchItem.searchResult.pointers != null && base.SearchItem.searchResult.pointers.Count > 0)
            {
                int _pgNo = 0;

                _ads = new Models.Advertisement.Advertisements();

                try
                {
                    foreach(Models.SearchItem.SearchResult.UrlPointer _p in base.SearchItem.searchResult.pointers)
                    {
                        Thread.Sleep(5000);

                        _pgNo++;
                        string _htmlDoc = HtmlUtil.GetPageDocument(_p.SearchUrl);

                        if(!string.IsNullOrEmpty(_htmlDoc))
                        {
                            string _errMsg = string.Empty;

                            bool _pgInError = this.ypTool.CheckPageIfError(_htmlDoc, ref _errMsg);
                            _p.IsValid = !_pgInError;

                            if(_p.IsValid)
                            {
                                _p.SearchHtml = _htmlDoc;

                                Stopwatch _stopwatch = new Stopwatch();
                                _stopwatch.Reset();
                                _stopwatch.Start();

                                Models.Advertisement.Advertisements __ads = this.ypTool.ExtractAds(_htmlDoc, base.InvokeEventAdExtracted);

                                if(__ads != null && __ads.Count > 0)
                                    _ads.AddRange(__ads.ToArray());

                                _stopwatch.Stop();

                                base.InvokeEventUrlPointerProcessed(new Handlers.EventHandlers.UrlPointerProcessedEventArgs(_p, _stopwatch.Elapsed));

                                if(base.SearchItem.pagesToProcess.HasValue && _pgNo == base.SearchItem.pagesToProcess)
                                    break;

                                if(__ads != null && __ads.Count == base.SearchItem.searchResult.TotalResults)
                                    break;
                            }
                            else
                            {
                                //correct Search result values here...
                                //base.SearchItem.searchResult.ResultsPerPage=base.SearchItem.searchResult.

                                //Strip the rest forward + all invalid objects
                                base.SearchItem.searchResult.pointers.RemoveAll(p => !p.IsValid);
                            }


                        }
                    }
                }
                catch(Exception ex)
                {
                    Exception _ex = new Exception(string.Format("Exception in {0}.{1}(?)", this.DirectoryProviderSetting.ServicedCountry.ToString(), "StartSearchResultProcessCallback"), ex);
                    base.InvokeEventFrameworkException(new Handlers.EventHandlers.FrameworkExceptionEventArgs(_ex));
                }
            }
            return _ads;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="htmlDocument"></param>
        /// <param name="searchItem"></param>
        /// <returns></returns>
        public override Models.SearchItem AnalyzeSearchCallback(string htmlDocument, Models.SearchItem searchItem)
        {
            Models.SearchItem _searchItem = null;

            Task<Models.SearchItem> resultTask = Task.Factory.StartNew(() =>
                StartAnalyzeSearchCallback(htmlDocument, searchItem));

            _searchItem = resultTask.Result;

            return _searchItem;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pgErrorMsg"></param>
        /// <returns></returns>
        public override string GenerateSearchResultPageCallback(ref bool resultIsError)
        {
            string _result = string.Empty;
            bool _resultIsError = false;

            Task<string> resultTask = Task.Factory.StartNew(
                () => StartGenerateSearchResultPageCallback(ref _resultIsError)
                , TaskCreationOptions.LongRunning);

            resultIsError = _resultIsError;
            _result = resultTask.Result;

            return _result;
        }

        #endregion

        #region thread callbacks

        /// <summary>
        /// 
        /// </summary>
        /// <param name="searchItem"></param>
        private Models.SearchItem StartAnalyzeSearchCallback(string htmlDocument, Models.SearchItem searchItem)
        {
            Models.SearchItem _searchItem = (Models.SearchItem)searchItem;

            if(_searchItem.searchResult == null)
                _searchItem.searchResult = new Models.SearchItem.SearchResult();

            string _errMsg = string.Empty;
            _searchItem.searchResult.PageOnError = this.ypTool.CheckPageIfError(htmlDocument, ref _errMsg);

            if(base.ForceStopSearchAnalysis) return null;

            if(_searchItem.searchResult.PageOnError)
            {
                _searchItem.searchResult.PageErrorMessage = _errMsg;
            }
            else
            {
                _searchItem.searchResult.ResultsPerPage = this.ypTool.GetResultsPerPage(htmlDocument);
                _searchItem.searchResult.TotalResults = this.ypTool.GetTotalResults(htmlDocument);

                int _totPgs = (int)(_searchItem.searchResult.TotalResults % _searchItem.searchResult.ResultsPerPage);
                _searchItem.searchResult.TotalPages = (int)(_searchItem.searchResult.TotalResults / _searchItem.searchResult.ResultsPerPage) + (_totPgs > 0 ? 1 : 0);

                if(_searchItem.searchResult.pointers == null)
                    _searchItem.searchResult.pointers = new List<Models.SearchItem.SearchResult.UrlPointer>();

                for(int _ctr = 0; _ctr < _searchItem.searchResult.TotalPages; _ctr++)
                {
                    if(base.ForceStopSearchAnalysis) return null;

                    string _searchUrl = Models.DirectoryProviderSetting.BuildSearchUrl(base.DirectoryProviderSetting
                                                                        , _searchItem.searchItem
                                                                        , _searchItem.searchLocation
                                                                        , _ctr + 1); ;

                    Models.SearchItem.SearchResult.UrlPointer _ptrItem = new Models.SearchItem.SearchResult.UrlPointer()
                    {
                        PageNo = _ctr + 1,
                        SearchUrl = new Uri(_searchUrl),
                        SearchHtml = (_ctr == 0 ? htmlDocument : string.Empty),
                        IsValid = false, /* is to be decided T/F when processing */
                    };

                    _searchItem.searchResult.pointers.Add(_ptrItem);
                    if(base.ForceStopSearchAnalysis) return null;

                }
            }

            return _searchItem;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="pgErrorMsg"></param>
        /// <returns></returns>
        private string StartGenerateSearchResultPageCallback(ref bool pgIsError)
        {
            string _searchUrl = Models.DirectoryProviderSetting.BuildSearchUrl(base.DirectoryProviderSetting, base.SearchItem.searchItem, base.SearchItem.searchLocation, 1);
            try
            {
                string _htmlDoc = HtmlUtil.GetPageDocument(_searchUrl);
                string _pgErrorMsg = string.Empty;
                pgIsError = this.ypTool.CheckPageIfError(_htmlDoc, ref _pgErrorMsg);

                if(pgIsError)
                    return _pgErrorMsg;
                else return _htmlDoc;
            }
            catch
            {
                return string.Empty;
            }
        }

        #endregion

        /// <summary>
        /// 
        /// </summary>
        internal class YPTool
        {
            public DirectoryProviderSetting directoryProviderSetting { get; set; }
            public Action<Handlers.EventHandlers.FrameworkExceptionEventArgs> frameworkExceptionInvoke { get; set; }

            public YPTool(DirectoryProviderSetting dirProviderSetting)
                : this(dirProviderSetting, null)
            {

            }
            public YPTool(DirectoryProviderSetting dirProviderSetting,
                Action<Handlers.EventHandlers.FrameworkExceptionEventArgs> frameworkExceptionInvoke)
            {
                this.directoryProviderSetting = dirProviderSetting;
                if(frameworkExceptionInvoke != null)
                    this.frameworkExceptionInvoke = frameworkExceptionInvoke;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="htmNode"></param>
            /// <returns></returns>
            public Models.Advertisement EscrapeAdInfo(HtmlAgilityPack.HtmlNode htmNode)
            {
                Models.Advertisement _adInfo = null;
                if(htmNode != null)
                {
                    try
                    {
                        HtmlAgilityPack.HtmlNode __n = null;

                        string _businessName = "[ERROR]";
                        __n = HtmlUtil.GetNode(htmNode, "meta", "itemprop", "name");
                        _businessName = __n.Attributes["content"].Value;
                        _businessName = Models.Advertisement.Resolve(_businessName);

                        string _description = "[NA]";
                        __n = HtmlUtil.GetNode(htmNode, "div", "class", "enhancedTextDesc paragraph");
                        if(__n != null)
                        {
                            _description = __n.InnerText;
                        }

                        string _phone = "[NA]";
                        try
                        {
                            /* <div class="preferredContact paragraph">
                             *  <span class="prefix">ph:</span>
                             *  <span preferredcontact="1">(02) 8222 3333</span>
                             * </div>
                             */
                            _phone = HtmlUtil.GetNode(htmNode, "div", "class", "preferredContact paragraph").Descendants("span").ToArray()[1].InnerText;
                        }
                        catch { }


                        string _fax = "[NA]"; //HtmlUtil.GetInnerText(_n, "div", "class", "phoneNumber");

                        /*
                         <span class="address">Level 11/ 75 Elizabeth St, Sydney NSW 2000</span>
                         */
                        string _fullAddress = HtmlUtil.GetInnerText(htmNode, "span", "class", "address");

                        string _streetBlk = string.Empty;
                        string _locality = string.Empty;
                        string _region = string.Empty;
                        string _postalCode = string.Empty;
                        Parsers.SplitAddresses(_fullAddress, ref _streetBlk, ref _locality, ref _region, ref _postalCode);

                        string _website = "[NA]";
                        __n = HtmlUtil.GetNode(htmNode, "a", "name", "listing_website");
                        if(__n != null)
                        {
                            _website = Models.Advertisement.Resolve(__n.InnerText);
                        }

                        string _latitude = "[NA]";
                        string _longitude = "[NA]";

                        /*<li flagnumber="1" 
                         *  class="gold mappableListing listingContainer omnitureListing" 
                            longitude="151.210118" 
                            latitude="-33.867857" 
                            product=";473590701;;;;evar26=Turner_Freeman_Lawye|evar23=O|evar46=YOLDSOL-DC" listingposition="1">*/


                        _latitude = htmNode.Attributes["latitude"].Value;
                        _longitude = htmNode.Attributes["longitude"].Value;

                        /*
                         <div class="yelp-rating review-rating" review="5"></div>
                         */
                        string _rating = "[NA]";
                        __n = HtmlUtil.GetNode(htmNode, "div", "class", "yelp-rating review-rating");
                        if(__n != null)
                        {
                            _rating = __n.Attributes["review"].Value;
                        }

                        /*  <a href="/nsw/sydney/edwards-barrie-13025623-listing.html?context=businessTypeSearch&amp;referredBy=YOL" name="listing_name" class="omnitureListingNameLink" id="listing-name-link-25">
                                <span id="listing-name-25">Edwards Barrie</span>
                            </a>
                         */
                        string _adLink = "[ERROR]";
                        __n = HtmlUtil.GetNode(htmNode, "a", "class", "omnitureListingNameLink");
                        if(__n != null)
                        {
                            _adLink = __n.Attributes["href"].Value;
                        }

                        //--------------------------------------------------------
                        _adInfo = new Models.Advertisement()
                        {
                            BusinessName = _businessName,
                            Description = _description,
                            Phone = _phone,
                            Fax = _fax,
                            FullAddress = _fullAddress,
                            StreetBlk = _streetBlk,
                            Locality = _locality,
                            Region = _region,
                            PostalCode = _postalCode,
                            Website = _website,
                            Latitude = _latitude,
                            Longtitude = _longitude,
                            Rating = _rating,
                            AdvertiserLink = string.Format("{0}{1}", this.directoryProviderSetting.BaseUrl, _adLink),
                        };
                    }
                    catch(Exception ex)
                    {
                        if(this.frameworkExceptionInvoke != null)
                        {
                            Exception _ex = new Exception(string.Format("Exception in {0}.{1}(?)", this.directoryProviderSetting.ServicedCountry.ToString(), "EscrapeAdInfo"), ex);
                            this.frameworkExceptionInvoke(new Handlers.EventHandlers.FrameworkExceptionEventArgs(_ex));
                        }
                    }
                }
                return _adInfo;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="adInf"></param>
            /// <returns></returns>
            public Models.Advertisement EscrapeAdInfoExtend(Models.Advertisement adInf)
            {
                if(adInf != null && !string.IsNullOrEmpty(adInf.AdvertiserLink))
                {
                    try
                    {
                        string _htmlDoc = HtmlUtil.GetPageDocument(adInf.AdvertiserLink);

                        HtmlAgilityPack.HtmlDocument htmDocAg = new HtmlAgilityPack.HtmlDocument();
                        htmDocAg.LoadHtml(_htmlDoc);

                        string _googMap = "[NA]";

                        string _keywords = "[NA]";
                        var _n = HtmlUtil.GetNode(htmDocAg.DocumentNode, "meta", "name", "keywords");
                        if(_n != null)
                        {
                            _keywords = _n.Attributes["content"].Value;
                        }

                        string _description = "[NA]";
                        HtmlAgilityPack.HtmlNode _metaDesc = HtmlUtil.GetNode(htmDocAg.DocumentNode, "meta", "name", "description");
                        if(_metaDesc != null)
                        {
                            _description = _metaDesc.Attributes["content"].Value;
                        }

                        string _emailAdd = "[NA]";
                        /*
                         <a id="mainEmailAddressLink" class="emailBusinessLink" rel="nofollow" href="/onlineSolution_emailBusiness.do?listingId=14074960&amp;classification=MAIN&amp;context=businessTypeSearch&amp;referredBy=YOL" title="Contact Turner Freeman Lawyers">
		                    <img class="emailAddressIcon" src="/ui/standard/bpp/email_icon.png" alt="Main Email Address">
		                    <span>privacy@turnerfreeman.com.au</span>
	                    </a>
                         */
                        HtmlAgilityPack.HtmlNode _emailAdNode = HtmlUtil.GetNode(htmDocAg.DocumentNode, "a", "id", "mainEmailAddressLink");
                        if(_emailAdNode != null)
                        {
                            _emailAdd = _emailAdNode.Descendants("span").ToArray()[0].InnerText;
                        }

                        string _locations = "[NA]";
                        //string _dateAdded = "[NA]";

                        adInf.GoogleMap = _googMap;
                        adInf.Keywords = _keywords;
                        adInf.Description = _description;
                        adInf.EmailAddress = _emailAdd;
                        adInf.Locations = _locations;
                        //adInf.DateAdded = _dateAdded;
                    }
                    catch(Exception ex)
                    {
                        if(this.frameworkExceptionInvoke != null)
                        {
                            Exception _ex = new Exception(string.Format("Exception in {0}.{1}(?)", this.directoryProviderSetting.ServicedCountry.ToString(), "EscrapeAdInfoExtend"), ex);
                            this.frameworkExceptionInvoke(new Handlers.EventHandlers.FrameworkExceptionEventArgs(_ex));
                        }
                    }
                }
                return adInf;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="htmlDocument"></param>
            /// <returns></returns>
            public Models.Advertisement.Advertisements ExtractAds(string htmlDocument)
            {
                return ExtractAds(htmlDocument, null);
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="htmlDocument"></param>
            /// <param name="actionAdExtracted"></param>
            /// <returns></returns>
            public Models.Advertisement.Advertisements ExtractAds(string htmlDocument, Action<Handlers.EventHandlers.AdExtractedEventArgs> actionAdExtracted)
            {
                Models.Advertisement.Advertisements _ads = null;
                try
                {
                    HtmlAgilityPack.HtmlDocument _doc = new HtmlAgilityPack.HtmlDocument();
                    _doc.LoadHtml(htmlDocument);

                    var _N = HtmlUtil.GetNode(_doc.DocumentNode, "ul", "id", "searchResultListings");
                    HtmlAgilityPack.HtmlNode[] _nodes = _N.Descendants("li")
                                                        .Where(li => li.Attributes.Contains("class")
                                                            && li.Attributes[@"class"].Value.Contains("listingContainer"))
                                                            .ToArray();

                    if(_nodes != null && _nodes.Count() > 0)
                    {
                        _ads = new Models.Advertisement.Advertisements();
                        int _pgItemIdx = 0;

                        foreach(HtmlAgilityPack.HtmlNode _n in _nodes)
                        {
                            Models.Advertisement _ad = new Models.Advertisement();

                            _pgItemIdx++;

                            Stopwatch _stopwatch = new Stopwatch();
                            _stopwatch.Reset();
                            _stopwatch.Start();

                            _ad = this.EscrapeAdInfo(_n);

                            if(_ad != null)
                                _ad = this.EscrapeAdInfoExtend(_ad);

                            _stopwatch.Stop();

                            _ads.Add(_ad);

                            if(actionAdExtracted != null)
                                actionAdExtracted(new Handlers.EventHandlers.AdExtractedEventArgs(_ad, _stopwatch.Elapsed, _pgItemIdx));
                        }
                    }
                }
                catch(Exception ex)
                {
                    if(this.frameworkExceptionInvoke != null)
                    {
                        Exception _ex = new Exception(string.Format("Exception in {0}.{1}(?)", this.directoryProviderSetting.ServicedCountry.ToString(), "ExtractAds"), ex);
                        this.frameworkExceptionInvoke(new Handlers.EventHandlers.FrameworkExceptionEventArgs(_ex));
                    }
                }
                return _ads;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="htmlDocument"></param>
            /// <param name="_errMsg"></param>
            /// <returns></returns>
            public bool CheckPageIfError(string htmlDocument, ref string _errMsg)
            {
                HtmlAgilityPack.HtmlDocument _doc = new HtmlAgilityPack.HtmlDocument();
                _doc.LoadHtml(htmlDocument);

                //http://www.yellowpages.com.au/search/listings?clue=kobo&locationClue=lyoo&selectedViewMode=LIST&emsLocationId=
                /*<div id="zeroResultsMessage">
                      We couldn't find any results for <strong>kobo</strong> in  
                      <strong>Lue, NSW 2850</strong> and/or nearby areas.
                  </div>*/
                HtmlAgilityPack.HtmlNode _node = HtmlUtil.GetNode(_doc.DocumentNode, "div", "id", "zeroResultsMessage");

                if(_node != null)
                {
                    _errMsg = _node.InnerText;
                    _errMsg = _errMsg.Replace("<strong>", "").Replace("</strong>", "");
                    return true;
                }

                if(_node == null)
                {
                    /*
                     <div id="fourOFourMessage">
						<h2>
					    	Sorry we couldn't find that page.<br>
					    	Perhaps the page doesn't exist or address was mistyped.
				    	</h2>
				    	You could try:
				    	<ul>
				    		<li>to do a business search in the search fields at the top of the page</li>
				    		<li>go to the <a href="http://www.yellowpages.com.au">Yellow Pages<sup>®</sup></a> home page</li>
				    	</ul>
			    	</div>
                     */
                    _node = HtmlUtil.GetNode(_doc.DocumentNode, "div", "id", "fourOFourMessage");
                    if(_node != null)
                    {
                        var _Ns = _node.Descendants("h2").ToArray();
                        if(_Ns != null && _Ns.Count() > 0)
                        {
                            var _n = _Ns[0];
                            _errMsg = _n.InnerText;
                            return true;
                        }
                    }

                }

                return false;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="htmlDocument"></param>
            /// <returns></returns>
            public int GetResultsPerPage(string htmlDocument)
            {
                HtmlAgilityPack.HtmlDocument _doc = new HtmlAgilityPack.HtmlDocument();
                _doc.LoadHtml(htmlDocument);

                var _nodes = HtmlUtil.GetNodeCollection(_doc.DocumentNode, "li", "class", "gold mappableListing listingContainer omnitureListing");

                if(_nodes != null && _nodes.Count() > 0)
                    return _nodes.Count();

                return 0;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="htmlDocument"></param>
            /// <returns></returns>
            public int GetTotalResults(string htmlDocument)
            {
                HtmlAgilityPack.HtmlDocument _doc = new HtmlAgilityPack.HtmlDocument();
                _doc.LoadHtml(htmlDocument);

                //<span id="headerResults"><strong>39</strong></span>
                var _node = HtmlUtil.GetNode(_doc.DocumentNode, "span", "id", "headerResults");

                if(_node != null)
                {
                    var _Ns = _node.Descendants("strong").ToArray();
                    if(_Ns != null && _Ns.Count() > 0)
                    {
                        var _n = _Ns[0];
                        int _r = 0;
                        try
                        {
                            _r = int.Parse(_n.InnerText);
                        }
                        catch { }
                        return _r;
                    }
                }
                return 0;
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="htmlDocument"></param>
            /// <returns></returns>
            public int GetTotalPages(string htmlDocument)
            {
                try
                {
                    string Pages = Regex.Match(htmlDocument, "<span class=\"pagingNumOfPages\">((.|\n)*?)</span>", RegexOptions.IgnoreCase | RegexOptions.Multiline | RegexOptions.Singleline).Groups[1].Value;
                    decimal temp = decimal.Parse(Pages.Split(' ')[3]);
                    int total_pages = Convert.ToInt32(temp);
                    return total_pages;
                }
                catch
                {
                    MatchCollection matchcollection = Regex.Matches(htmlDocument, "<h3 class=\"listingTitleLine\">((.|\n)*?)<div class=\"ypgFBLink\">", RegexOptions.IgnoreCase);
                    return matchcollection.Count > 0 ? 1 : -1;
                }
            }

        }

    }
}
