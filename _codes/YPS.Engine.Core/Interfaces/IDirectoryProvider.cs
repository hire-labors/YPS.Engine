using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using YPS.Engine.Core.Models;
using YPS.Engine.Core.Handlers;

namespace YPS.Engine.Core.Interfaces
{
    public interface IDirectoryProvider
    {

        bool IsBusy { get; set; }
        Advertisement.Advertisements Advertisements { get; set; }
        SearchItem SearchItem { get; set; }
        DirectoryProviderSetting DirectoryProviderSetting { get; set; }
        DirectoryProviderRoutineStageEnum DirectoryProviderRoutineStage { get; set; }

        void AnalyzeSearch();
        void AnalyzeSearch(string searchItem, string seachLocation);
        void AnalyzeSearch(string HtmlDocument);
        void StopAnalyzeSearch();

        bool StartSearchResultProcess();
        void StopSearchResultProcess();

        event EventHandlers.SearchAnalysisStartingEventHandler OnSearchAnalysisStarting;
        event EventHandlers.SearchAnalysisFinishedEventHandler OnSearchAnalysisFinished;
        event EventHandlers.ProcessStartingEventHandler OnProcessStarting;
        event EventHandlers.ProcessFinishedEventHandler OnProcessFinished;
        event EventHandlers.UrlPointerProcessedEventHandler OnUrlPointerProcessed;
        event EventHandlers.AdExtractedEventHandler OnAdExtracted;
        event EventHandlers.FrameworkExceptionEventHandler OnFrameworkException;

    }
}
