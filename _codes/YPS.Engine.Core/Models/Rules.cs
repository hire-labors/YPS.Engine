using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace YPS.Engine.Core.Models
{
    public enum ServicedCountryEnum
    {
        Australia,
        Austria,
        Canada,
        France,
        New_Zealand,
        Germany,
        Indonesia,
        Ireland,
        South_Africa,
        United_Kingdom,
        United_States_America,
    }

    public enum DirectoryInfoMember
    {
        BUSINESS_NAME,
        PHONE,
        FAX,
        ADDRESS_FULL,
        STREET_BLOCK,
        LOCALITY,
        REGION,
        POSTAL_CODE,
        LATITUDE,
        LONGITUDE,
        GOOGLEMAP,
        RATING,
        WEBSITE,
        EMAIL,
        DESCRIPTION,
        KEYWORDS,
        LOCATIONS,
    }

    public enum DirectoryProviderRoutineStageEnum
    {
        ProcessStarting,
        ProcessFinished,
        SearchAnalysisStarting,
        SearchAnalysisFinished
    }

}
