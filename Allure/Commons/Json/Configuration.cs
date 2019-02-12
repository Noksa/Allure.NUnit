using System.Collections.Generic;

namespace Allure.Commons.Json
{
    public class Configuration
    {
        public AllureCfg Allure { get; set; }
        public List<Category> Categories { get; set; }

        public class AllureCfg
        {
            public string Directory { get; set; } = "allure-results";
            public bool AllowEmptySuites { get; set; } = false;
            public bool EnableParameters { get; set; } = true;

            public bool AllowLocalHistoryTrend { get; set; } = false;

            public bool DebugMode { get; set; } = false;

            public HashSet<string> Links { get; } = new HashSet<string>();
        }

        public class Category
        {
            public string name { get; set; }
            public string messageRegex { get; set; }
            public string traceRegex { get; set; }
            public List<string> matchedStatuses { get; set; }
        }
    }
}