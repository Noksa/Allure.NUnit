using Allure.Commons.Model;
using Newtonsoft.Json;

namespace Allure.Commons.Json
{
    internal class StatusTests
    {
        public string status { get; set; }

        [JsonIgnore]
        internal Status StatusEnum
        {
            get
            {
                switch (status.ToLower())
                {
                    case "skipped":
                        return Status.skipped;
                    case "passed":
                        return Status.passed;
                    case "broken":
                        return Status.broken;
                    case "failed":
                        return Status.failed;
                    default:
                        return Status.none;
                }
            }
        }
    }
}