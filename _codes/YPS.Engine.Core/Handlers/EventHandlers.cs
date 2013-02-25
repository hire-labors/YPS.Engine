using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YPS.Engine.Core.Models;

namespace YPS.Engine.Core.Handlers
{
    public class EventHandlers
    {
        /// <summary>
        /// 
        /// </summary>
        public class UrlPointerProcessedEventArgs : EventArgs
        {
            public Models.SearchItem.SearchResult.UrlPointer UrlPointer { get; set; }
            public TimeSpan Elapsed { get; set; }
            public UrlPointerProcessedEventArgs(Models.SearchItem.SearchResult.UrlPointer UrlPointer, TimeSpan Elapsed)
            {
                this.UrlPointer = UrlPointer;
                this.Elapsed = Elapsed;
            }
        }
        public delegate void UrlPointerProcessedEventHandler(object sender, UrlPointerProcessedEventArgs e);


        /// <summary>
        /// 
        /// </summary>
        public class SearchAnalysisStartingEventArgs : EventArgs
        {
            public bool cancel { get; set; }

            public SearchAnalysisStartingEventArgs()
            {

            }
        }
        public delegate void SearchAnalysisStartingEventHandler(object sender, SearchAnalysisStartingEventArgs e);

        /// <summary>
        /// 
        /// </summary>
        public class SearchAnalysisFinishedEventArgs : EventArgs
        {
            public SearchItem SearchResult { get; set; }
            public TimeSpan Elapsed { get; set; }

            public SearchAnalysisFinishedEventArgs(SearchItem SearchResult, TimeSpan Elapsed)
            {
                this.SearchResult = SearchResult;
                this.Elapsed = Elapsed;
            }
        }
        public delegate void SearchAnalysisFinishedEventHandler(object sender, SearchAnalysisFinishedEventArgs e);

        /// <summary>
        /// 
        /// </summary>
        public class ProcessStartingEventArgs : EventArgs
        {
            public bool cancel { get; set; }

            public ProcessStartingEventArgs(bool cancel)
            {
                this.cancel = cancel;
            }
        }
        public delegate void ProcessStartingEventHandler(object sender, ProcessStartingEventArgs e);

        /// <summary>
        /// 
        /// </summary>
        public class ProcessFinishedEventArgs : EventArgs
        {
            public Advertisement.Advertisements Advertisements { get; set; }
            public TimeSpan Elapsed { get; set; }
            public ProcessFinishedEventArgs(Advertisement.Advertisements Advertisements, TimeSpan Elapsed)
            {
                this.Advertisements = Advertisements;
                this.Elapsed = Elapsed;
            }
        }
        public delegate void ProcessFinishedEventHandler(object sender, ProcessFinishedEventArgs e);

        /// <summary>
        /// 
        /// </summary>
        public class AdExtractedEventArgs : EventArgs
        {
            public Advertisement Advertisement { get; set; }
            public TimeSpan Elapsed { get; set; }
            public int PgItemIdx { get; set; }

            public AdExtractedEventArgs(Advertisement Advertisement, TimeSpan Elapsed, int PgItemIdx)
            {
                this.Advertisement = Advertisement;
                this.Elapsed = Elapsed;
                this.PgItemIdx = PgItemIdx;
            }
        }
        public delegate void AdExtractedEventHandler(object sender, AdExtractedEventArgs e);

        /// <summary>
        /// 
        /// </summary>
        public class FrameworkExceptionEventArgs : EventArgs
        {
            public Exception exception { get; set; }

            public FrameworkExceptionEventArgs(Exception exception)
            {
                this.exception = exception;
            }
        }
        public delegate void FrameworkExceptionEventHandler(object sender, FrameworkExceptionEventArgs e);
    }
}
