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
    public class DirectoryProviderUnitedKindom : DirectoryProviderBase
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
        public DirectoryProviderUnitedKindom(Models.DirectoryProviderSetting directoryProviderSetting)
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
                _searchItem.searchResult.TotalPages = this.ypTool.GetTotalPages(htmlDocument);
                _searchItem.searchResult.TotalResults = _searchItem.searchResult.TotalPages * _searchItem.searchResult.ResultsPerPage;

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
                        SearchHtml = (_ctr == 0 ? htmlDocument : string.Empty)
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
                        string _businessName = HtmlUtil.GetInnerText(htmNode, "span", "class", "listingTitle");
                        _businessName = Models.Advertisement.Resolve(_businessName);

                        string _phone = HtmlUtil.GetInnerText(htmNode, "div", "class", "phoneNumber");
                        string _fax = "[NA]"; //HtmlUtil.GetInnerText(_n, "div", "class", "phoneNumber");
                        string _fullAddress = HtmlUtil.GetInnerText(htmNode, "div", "class", "address");

                        string _streetBlk = string.Empty;
                        string _locality = string.Empty;
                        string _region = string.Empty;
                        string _postalCode = string.Empty;
                        Parsers.SplitAddresses(_fullAddress, ref _streetBlk, ref _locality, ref _region, ref _postalCode);

                        HtmlAgilityPack.HtmlNode __n = null;

                        string _website = "[NA]";
                        __n = HtmlUtil.GetNode(htmNode, "ul", "class", "ypgListingLinks");
                        if(__n != null)
                        {
                            try
                            {
                                __n = HtmlUtil.GetNode(htmNode, "li", "class", "noPrint");
                                __n = __n.Descendants("a").ToArray()[0];
                                _website = __n.Attributes["href"].Value.Replace("/gourl/", string.Empty);
                            }
                            catch { }
                        }

                        string _adLink = "[NA]";
                        __n = HtmlUtil.GetNode(htmNode, "h3", "class", "listingTitleLine");
                        if(__n != null)
                        {
                            __n = __n.Descendants("a").ToArray()[0];
                            _adLink = __n.Attributes["href"].Value;
                        }

                        //--------------------------------------------------------
                        _adInfo = new Models.Advertisement()
                        {
                            BusinessName = _businessName,
                            Phone = _phone,
                            Fax = _fax,
                            FullAddress = _fullAddress,
                            StreetBlk = _streetBlk,
                            Locality = _locality,
                            Region = _region,
                            PostalCode = _postalCode,
                            Website = _website,
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

                        string _latitude = "[NA]";
                        string _longitude = "[NA]";
                        HtmlAgilityPack.HtmlNode _mapDatNode = HtmlUtil.GetNode(htmDocAg.DocumentNode, "div", "id", "ypgMapContainer");
                        if(_mapDatNode != null)
                        {   //latLong = new VELatLong(43.8087172232, -79.5469648855); map.CreateAndLoadMap
                            string _mapDat = _mapDatNode.InnerText;
                            int _mrkrStart = _mapDat.IndexOf("VELatLong", 0);
                            int _mrkrEnd = _mapDat.IndexOf("map.CreateAndLoadMap", 0);
                            _mapDat = _mapDat.Substring(_mrkrStart, _mrkrEnd - _mrkrStart);

                            _mapDat = _mapDat
                                        .Replace("VELatLong(", "")
                                        .Replace("map.CreateAndLoadMap", "")
                                        .Replace(");", "")
                                        .Trim();

                            string[] _coords = _mapDat.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            _latitude = _coords[0];
                            _longitude = _coords[1];
                        }

                        string _googMap = "[NA]";

                        string _keywords = "[NA]";
                        HtmlAgilityPack.HtmlNode _metaKeywords = HtmlUtil.GetNode(htmDocAg.DocumentNode, "meta", "name", "keywords");
                        if(_metaKeywords != null)
                        {
                            _keywords = _metaKeywords.Attributes["content"].Value;
                        }

                        string _description = "[NA]";
                        HtmlAgilityPack.HtmlNode _metaDesc = HtmlUtil.GetNode(htmDocAg.DocumentNode, "meta", "name", "description");
                        if(_metaDesc != null)
                        {
                            _description = _metaDesc.Attributes["content"].Value;
                        }

                        string _rating = "[NA]";

                        string _emailAdd = "[NA]";
                        HtmlAgilityPack.HtmlNode _emailAdNode = HtmlUtil.GetNode(htmDocAg.DocumentNode, "div", "class", "busCardLeftLinks");
                        if(_emailAdNode != null)
                        {
                            try
                            {
                                _emailAdd = _emailAdNode.Descendants("a").ToArray()[0].Attributes["content"].Value;
                            }
                            catch { }
                        }

                        string _locations = "[NA]";
                        //string _dateAdded = "[NA]";

                        adInf.Latitude = _latitude;
                        adInf.Longtitude = _longitude;
                        adInf.GoogleMap = _googMap;
                        adInf.Keywords = _keywords;
                        adInf.Description = _description;
                        adInf.Rating = _rating;
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

                    var _nodes = HtmlUtil.GetNodeCollection(_doc.DocumentNode, "div", "class", "ypgListing clearfix");
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

                HtmlAgilityPack.HtmlNode _node = HtmlUtil.GetNode(_doc.DocumentNode, "div", "class", "ypgErrorText");

                if(_node == null)
                    _node = HtmlUtil.GetNode(_doc.DocumentNode, "div", "id", "ypgSearchErrorMessage");

                if(_node != null)
                {
                    _errMsg = _node.InnerText;
                    _errMsg = _errMsg.Replace("<strong>", "").Replace("</strong>", "");
                    return true;
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

                var _nodes = HtmlUtil.GetNodeCollection(_doc.DocumentNode, "div", "class", "ypgListing clearfix");

                if(_nodes != null && _nodes.Count() > 0)
                    return _nodes.Count();

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
