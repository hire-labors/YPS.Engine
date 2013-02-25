using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YPS.Engine.Core.Models;
using YPS.Engine.Core.Interfaces;
using YPS.Engine.Core.Providers;
using System.Threading;

namespace YPS.Engine.Test
{
    class Program
    {
        /// <summary>
        /// 
        /// </summary>
        static EventWaitHandle analyzerWaitHandle = new AutoResetEvent(false);

        /// <summary>
        /// 
        /// </summary>
        static volatile bool _finished = false;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {

            //Define the directorySettings to be used
            DirectoryProviderSetting.DirectoryProviderSettings _dirSettings = new DirectoryProviderSetting.DirectoryProviderSettings();
            DirectoryProviderSetting _dirSet = _dirSettings.Where(p => p.ServicedCountry == ServicedCountryEnum.Australia).FirstOrDefault();

            //Proceed if setting for Canada exist
            if(_dirSet != null)
            {

                //Define the item to be searched...
                SearchItem _searchItem = new SearchItem()
                {
                    /*
                     searchItem = "lawyers",
                    searchLocation = "sydney",
                     */
                    searchItem = "software company",
                    searchLocation = "sydney",
                    pagesToProcess = 2
                };

                Console.WriteLine(string.Format("Initializing search parameters for YP {0}: \r\nSearching {1} in {2}\r\nProcessing {3}.",
                                    _dirSet.ServicedCountry.ToString(),
                                    _searchItem.searchItem,
                                    _searchItem.searchLocation,
                                    !_searchItem.pagesToProcess.HasValue ? "all of the pages" : _searchItem.pagesToProcess.ToString() + " pages only."));

                //Select the provider to execute this search
                IDirectoryProvider _dirProvider = null;
                switch(_dirSet.ServicedCountry)
                {
                    case ServicedCountryEnum.Canada:
                        _dirProvider = new DirectoryProviderCanada(_dirSet);
                        break;
                    case ServicedCountryEnum.Australia:
                        _dirProvider = new DirectoryProviderAustralia(_dirSet);
                        break;
                }

                _dirProvider.SearchItem = _searchItem;
                _dirProvider.OnSearchAnalysisStarting += new Core.Handlers.EventHandlers.SearchAnalysisStartingEventHandler(_dirProvider_OnSearchAnalysisStarting);
                _dirProvider.OnSearchAnalysisFinished += new Core.Handlers.EventHandlers.SearchAnalysisFinishedEventHandler(_dirProvider_OnSearchAnalysisFinished);
                //Start or Invoke method StartSearchResultProcess() when OnSearchAnalysisFinished

                _dirProvider.OnProcessStarting += new Core.Handlers.EventHandlers.ProcessStartingEventHandler(_dirProvider_OnProcessStarting);
                _dirProvider.OnProcessFinished += new Core.Handlers.EventHandlers.ProcessFinishedEventHandler(_dirProvider_OnProcessFinished);
                _dirProvider.OnUrlPointerProcessed += new Core.Handlers.EventHandlers.UrlPointerProcessedEventHandler(_dirProvider_OnUrlPointerProcessed);
                _dirProvider.OnAdExtracted += new Core.Handlers.EventHandlers.AdExtractedEventHandler(_dirProvider_OnAdExtracted);
                _dirProvider.OnFrameworkException += new Core.Handlers.EventHandlers.FrameworkExceptionEventHandler(_dirProvider_OnFrameworkException);

                _dirProvider.AnalyzeSearch();

                new Thread(() =>
                    {
                        lock(typeof(Program))
                        {
                            while(true)
                            {
                                Thread.Sleep(1000);
                                Console.Write(@".");

                                if(_finished)
                                {
                                    Console.WriteLine("Press ENTER to exit!");
                                    break;
                                }
                            }
                        }
                    }) { IsBackground = true }
                    .Start();

                Console.ReadLine();
                _finished = true;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void _dirProvider_OnFrameworkException(object sender, Core.Handlers.EventHandlers.FrameworkExceptionEventArgs e)
        {
            Console.WriteLine(string.Format("\r\nFramework error occured: {0}\r\n{1}\r\n", e.exception.Message, e.exception.InnerException.Message));
        }

        private static int _adIdx = 0;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void _dirProvider_OnAdExtracted(object sender, Core.Handlers.EventHandlers.AdExtractedEventArgs e)
        {
            _adIdx++;
            Console.WriteLine(
                string.Format("\r\nAd No.{0}-[{4}] extracted: {1}\r\nElapsed time: {2} minutes {3} seconds", _adIdx, e.Advertisement.BusinessName, Math.Floor(e.Elapsed.TotalMinutes), e.Elapsed.ToString("ss\\.ff"), e.PgItemIdx)
                );
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void _dirProvider_OnUrlPointerProcessed(object sender, Core.Handlers.EventHandlers.UrlPointerProcessedEventArgs e)
        {
            IDirectoryProvider _d = (IDirectoryProvider)sender;
            Console.WriteLine(
                string.Format("\r\nFinished processing Search Url pointer\r\nPage No. : {0} of {4} (of {7} overall pages)\r\nUrl : {1}\r\nSize : {2} bytes\r\nPage is valid : {3}\r\nElapsed time : {5} minutes {6} seconds\r\n",
                e.UrlPointer.PageNo,
                e.UrlPointer.SearchUrl,
                e.UrlPointer.SearchHtml.Length,
                e.UrlPointer.IsValid ? "YES" : "NO",
                _d.SearchItem.pagesToProcess.HasValue ? _d.SearchItem.pagesToProcess.Value.ToString() : _d.SearchItem.searchResult.TotalPages.ToString(),
                Math.Floor(e.Elapsed.TotalMinutes),
                e.Elapsed.ToString("ss\\.ff"),
                _d.SearchItem.searchResult.TotalPages
                ));
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void _dirProvider_OnProcessFinished(object sender, Core.Handlers.EventHandlers.ProcessFinishedEventArgs e)
        {
            IDirectoryProvider _d = (IDirectoryProvider)sender;
            Console.WriteLine(string.Format("\r\nProcessFinished\r\nNumber of ads scraped : {0} of {3}\r\nPages processed : {4}\r\nElapsed time: {1} minutes {2} seconds", 
                e.Advertisements.Count, 
                Math.Floor(e.Elapsed.TotalMinutes), 
                e.Elapsed.ToString("ss\\.ff"),
                _d.SearchItem.searchResult.TotalResults,
                _d.SearchItem.pagesToProcess.HasValue ? _d.SearchItem.pagesToProcess.Value.ToString() : _d.SearchItem.searchResult.TotalPages.ToString()));
            _finished = true;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void _dirProvider_OnProcessStarting(object sender, Core.Handlers.EventHandlers.ProcessStartingEventArgs e)
        {

            Console.WriteLine("\r\nProcessStarting\r\n");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void _dirProvider_OnSearchAnalysisFinished(object sender, Core.Handlers.EventHandlers.SearchAnalysisFinishedEventArgs e)
        {
            Console.WriteLine(string.Format("\r\nSearchAnalysisFinished\r\nElapsed time: {0} minutes {1} seconds", Math.Floor(e.Elapsed.TotalMinutes), e.Elapsed.ToString("ss\\.ff")));
            try
            {
                IDirectoryProvider _dirProvider = (IDirectoryProvider)sender;
                _dirProvider.StartSearchResultProcess();
            }
            catch(Exception ex)
            {
                Console.WriteLine(string.Format("\r\nError occured when processing search result: {0}\r\n", ex.Message));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void _dirProvider_OnSearchAnalysisStarting(object sender, Core.Handlers.EventHandlers.SearchAnalysisStartingEventArgs e)
        {
            Console.WriteLine("\r\nSearchAnalysisStarting\r\n");
        }
    }
}


