using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YPS.Engine.Core.Interfaces;
using YPS.Engine.Core.Models;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;

namespace YPS.Engine.Core.Providers
{
    public abstract class DirectoryProviderBase : IDirectoryProvider
    {
        #region variables to store local values

        public bool ForceStopSearchAnalysis { get; set; }
        public bool ForceStopProcess { get; set; }

        #endregion

        #region constructor

        /// <summary>
        /// 
        /// </summary>
        /// <param name="DirectoryProviderSetting"></param>
        public DirectoryProviderBase(DirectoryProviderSetting DirectoryProviderSetting)
        {
            this._DirectoryProviderSetting = DirectoryProviderSetting;
        }

        #endregion

        #region IDirectoryProvider Members

        private bool _IsBusy = false;
        private Advertisement.Advertisements _Advertisements = null;
        private SearchItem _SearchItem = null;
        private DirectoryProviderSetting _DirectoryProviderSetting = null;

        #region properties

        /// <summary>
        /// 
        /// </summary>
        DirectoryProviderRoutineStageEnum _DirectoryProviderRoutineStage = DirectoryProviderRoutineStageEnum.ProcessFinished;
        public DirectoryProviderRoutineStageEnum DirectoryProviderRoutineStage
        {
            get { return _DirectoryProviderRoutineStage; }
            set
            {
                _DirectoryProviderRoutineStage = value;
            }
        }

        /// <summary>
        /// TRUE when StartSearch or StartAnalyze are invoked
        /// FALSE when StopSearch or StopAnalyze are invoked
        /// </summary>
        public bool IsBusy
        {
            get
            {
                return _IsBusy;
            }
            set
            {
                IsBusy = value;
            }
        }

        /// <summary>
        /// Stores all the scraped results
        /// </summary>
        public Models.Advertisement.Advertisements Advertisements
        {
            get
            {
                return _Advertisements;
            }
            set
            {
                _Advertisements = value;
            }
        }

        /// <summary>
        /// Stores the search criteria and underlying preliminary 
        /// search results that can be used later when analyzing the 
        /// Html document attached to SearchResult.SearchResultPointer object
        /// </summary>
        public Models.SearchItem SearchItem
        {
            get
            {
                return _SearchItem;
            }
            set
            {
                _SearchItem = value;
            }
        }

        /// <summary>
        /// Stores the settings and information for this Yellowpage provider
        /// </summary>
        public Models.DirectoryProviderSetting DirectoryProviderSetting
        {
            get
            {
                return _DirectoryProviderSetting;
            }
            set
            {
                _DirectoryProviderSetting = value;
            }
        }

        #endregion

        public void AnalyzeSearch()
        {
            this.AnalyzeSearch(string.Empty, string.Empty);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="searchItem"></param>
        /// <param name="seachLocation"></param>
        /// <returns></returns>
        public void AnalyzeSearch(string searchItem, string seachLocation)
        {
            Handlers.EventHandlers.SearchAnalysisStartingEventArgs _ea = new Handlers.EventHandlers.SearchAnalysisStartingEventArgs();
            InvokeEventSearchAnalysisStarting(_ea);
            if(!_ea.cancel)
            {
                _DirectoryProviderRoutineStage = DirectoryProviderRoutineStageEnum.SearchAnalysisStarting;
                new Thread(() =>
                            {
                                this.ForceStopSearchAnalysis = false;
                                this._IsBusy = true;

                                string _htmlDoc = string.Empty;
                                bool _resultIsError = false;

                                if(this._SearchItem == null) this._SearchItem = new SearchItem();
                                if(!string.IsNullOrEmpty(searchItem) && !string.IsNullOrEmpty(seachLocation))
                                {
                                    this._SearchItem.searchItem = searchItem;
                                    this._SearchItem.searchLocation = seachLocation;
                                }

                                _htmlDoc = this.GenerateSearchResultPageCallback(ref _resultIsError);

                                if(_resultIsError)
                                    throw new Exception(string.Format("The search page generated contains error: {0}", _htmlDoc));

                                if(this.ForceStopSearchAnalysis) return;

                                if(!string.IsNullOrEmpty(_htmlDoc)) AnalyzeSearch(_htmlDoc);

                            })
                            {
                                IsBackground = true
                            }
                            .Start();
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public virtual string GenerateSearchResultPageCallback(ref bool resultIsError)
        {
            throw new Exception("Must not execute base class method.");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="htmlDocument"></param>
        public void AnalyzeSearch(string htmlDocument)
        {
            this.ForceStopSearchAnalysis = false;
            this._IsBusy = true;

            Stopwatch _stopWatch = new Stopwatch();

            _stopWatch.Reset();
            _stopWatch.Start();

            Models.SearchItem _searchItem = this.AnalyzeSearchCallback(htmlDocument, this.SearchItem);

            _stopWatch.Stop();

            this.SearchItem = _searchItem;

            _DirectoryProviderRoutineStage = DirectoryProviderRoutineStageEnum.SearchAnalysisFinished;
            this._IsBusy = false;

            InvokeEventSearchAnalysisFinished(new Handlers.EventHandlers.SearchAnalysisFinishedEventArgs(_searchItem, _stopWatch.Elapsed));
        }

        /// <summary>
        /// Important: Must not execute base class method because 
        /// value should be returned by the implementing class
        /// </summary>
        /// <param name="htmlDocument"></param>
        /// <returns></returns>
        public virtual Models.SearchItem AnalyzeSearchCallback(string htmlDocument, Models.SearchItem searchItem)
        {
            throw new Exception("Must not execute base class method.");
        }

        /// <summary>
        /// Stops the pending search analysis
        /// </summary>
        public void StopAnalyzeSearch()
        {
            this.ForceStopSearchAnalysis = true;
        }

        /// <summary>
        /// 
        /// </summary>
        public bool StartSearchResultProcess()
        {
            if(this._SearchItem == null || this._SearchItem.searchResult == null)
                throw new InvalidOperationException("No search item to process.");

            Advertisement.Advertisements _results = null;
            Stopwatch _stopwatch = new Stopwatch();

            _DirectoryProviderRoutineStage = DirectoryProviderRoutineStageEnum.ProcessStarting;
            bool _cancel = false;
            this.InvokeEventProcessStarting(new Handlers.EventHandlers.ProcessStartingEventArgs(_cancel));

            if(!_cancel)
            {
                _stopwatch.Reset();
                _stopwatch.Start();
                _results = this.StartSearchResultProcessCallback();
                _stopwatch.Stop();
            }

            _DirectoryProviderRoutineStage = DirectoryProviderRoutineStageEnum.ProcessFinished;
            this.InvokeEventProcessFinished(new Handlers.EventHandlers.ProcessFinishedEventArgs(_results, _stopwatch.Elapsed));

            if(_results != null)
            {
                this._Advertisements = _results;
                return _results.Count > 0;
            }
            return false;
        }

        /// <summary>
        /// Important: Must not execute base class method because 
        /// value should be returned by the implementing class
        /// </summary>
        /// <param name="searchResult"></param>
        /// <param name="forceStopProcess"></param>
        /// <returns></returns>
        public virtual Advertisement.Advertisements StartSearchResultProcessCallback()
        {
            throw new Exception("Must not execute base class method.");
        }

        /// <summary>
        /// 
        /// </summary>
        public void StopSearchResultProcess()
        {
            this.ForceStopProcess = true;
        }

        #region event handlers

        public event Handlers.EventHandlers.SearchAnalysisStartingEventHandler OnSearchAnalysisStarting;
        public event Handlers.EventHandlers.SearchAnalysisFinishedEventHandler OnSearchAnalysisFinished;
        public event Handlers.EventHandlers.ProcessStartingEventHandler OnProcessStarting;
        public event Handlers.EventHandlers.ProcessFinishedEventHandler OnProcessFinished;
        public event Handlers.EventHandlers.UrlPointerProcessedEventHandler OnUrlPointerProcessed;
        public event Handlers.EventHandlers.AdExtractedEventHandler OnAdExtracted;
        public event Handlers.EventHandlers.FrameworkExceptionEventHandler OnFrameworkException;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        public virtual void InvokeEventFrameworkException(Handlers.EventHandlers.FrameworkExceptionEventArgs a)
        {
            Handlers.EventHandlers.FrameworkExceptionEventHandler handler = OnFrameworkException;
            if(handler != null)
                handler(this, a);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        public virtual void InvokeEventAdExtracted(Handlers.EventHandlers.AdExtractedEventArgs a)
        {
            Handlers.EventHandlers.AdExtractedEventHandler handler = OnAdExtracted;
            if(handler != null)
                handler(this, a);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        public virtual void InvokeEventUrlPointerProcessed(Handlers.EventHandlers.UrlPointerProcessedEventArgs a)
        {
            Handlers.EventHandlers.UrlPointerProcessedEventHandler handler = OnUrlPointerProcessed;
            if(handler != null)
                handler(this, a);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        public virtual void InvokeEventSearchAnalysisStarting(Handlers.EventHandlers.SearchAnalysisStartingEventArgs a)
        {
            Handlers.EventHandlers.SearchAnalysisStartingEventHandler handler = OnSearchAnalysisStarting;
            if(handler != null)
                handler(this, a);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        public virtual void InvokeEventSearchAnalysisFinished(Handlers.EventHandlers.SearchAnalysisFinishedEventArgs a)
        {
            Handlers.EventHandlers.SearchAnalysisFinishedEventHandler handler = OnSearchAnalysisFinished;
            if(handler != null)
                handler(this, a);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        public virtual void InvokeEventProcessStarting(Handlers.EventHandlers.ProcessStartingEventArgs a)
        {
            Handlers.EventHandlers.ProcessStartingEventHandler handler = OnProcessStarting;
            if(handler != null)
                handler(this, a);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        public virtual void InvokeEventProcessFinished(Handlers.EventHandlers.ProcessFinishedEventArgs a)
        {
            Handlers.EventHandlers.ProcessFinishedEventHandler handler = OnProcessFinished;
            if(handler != null)
                handler(this, a);
        }

        #endregion

        #endregion

    }
}
