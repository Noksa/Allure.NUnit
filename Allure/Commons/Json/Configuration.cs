using System.Collections.Generic;

namespace Allure.Commons.Json
{
    public class Configuration
    {
        public AllureCfg Allure { get; set; }
        public List<Category> Categories { get; set; }

        public SpecFlowCfg SpecFlow { get; set; }

        public class AllureCfg
        {
            public string Directory { get; set; } = "allure-results";
            public bool AllowEmptySuites { get; set; } = false;
            public bool EnableParameters { get; set; } = true;

            public bool AllowLocalHistoryTrend { get; set; } = false;

            public bool DebugMode { get; set; } = false;

            public string Title { get; set; }

            public HashSet<string> Links { get; } = new HashSet<string>();
        }

        public class Category
        {
            public string name { get; set; }
            public string messageRegex { get; set; }
            public string traceRegex { get; set; }
            public List<string> matchedStatuses { get; set; }
        }

        public class SpecFlowCfg
        {
            public Steparguments stepArguments { get; set; } = new Steparguments();
            public Grouping grouping { get; set; } = new Grouping();
            public Labels labels { get; set; } = new Labels();
            public Links links { get; set; } = new Links();
        }

        public class Steparguments
        {
            public bool convertToParameters { get; set; }
            public string paramNameRegex { get; set; }
            public string paramValueRegex { get; set; }
        }

        public class Grouping
        {
            public Suites suites { get; set; } = new Suites();
            public Behaviors behaviors { get; set; } = new Behaviors();
            public Packages packages { get; set; } = new Packages();
        }

        public class Suites
        {
            public string parentSuite { get; set; }
            public string suite { get; set; }
            public string subSuite { get; set; }
        }

        public class Behaviors
        {
            public string epic { get; set; }
            public string story { get; set; }
        }

        public class Packages
        {
            public string package { get; set; }
            public string testClass { get; set; }
            public string testMethod { get; set; }
        }

        public class Labels
        {
            public string owner { get; set; }
            public string severity { get; set; }
        }

        public class Links
        {
            public string link { get; set; }
            public string issue { get; set; }
            public string tms { get; set; }
        }
    }
}