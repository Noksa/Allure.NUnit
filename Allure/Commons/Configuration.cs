namespace Allure.Commons
{
    public class Configuration
    {
        public AllureCfg Allure { get; set; }

        public class AllureCfg
        {
            public string Directory { get; set; }
            public bool AllowEmptySuites { get; set; }
        }
    }
}