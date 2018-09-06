namespace Allure.Commons
{
    public class Configuration
    {
        public AllureCfg Allure { get; set; }

        public class AllureCfg
        {
            public string Directory { get; set; } = "allure-results";
            public bool AllowEmptySuites { get; set; } = false;
            public bool EnableParameters { get; set; } = true;
        }
    }
}