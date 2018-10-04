// To parse this JSON data, add NuGet 'Newtonsoft.Json' then do:
//
//    using QuickType;
//
//    var historyTrend = HistoryTrend.FromJson(jsonString);

using System;
using System.Globalization;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Allure.Commons.Json
{
    public partial class HistoryTrend
    {
        [JsonProperty("statistic")] public Statistic Statistic { get; set; }

        [JsonProperty("buildOrder")] public long BuildOrder { get; set; }

        [JsonProperty("reportUrl", NullValueHandling = NullValueHandling.Ignore)]
        public Uri ReportUrl { get; set; }
    }

    public class Statistic
    {
        [JsonProperty("failed")] public long Failed { get; set; }

        [JsonProperty("broken")] public long Broken { get; set; }

        [JsonProperty("skipped")] public long Skipped { get; set; }

        [JsonProperty("passed")] public long Passed { get; set; }

        [JsonProperty("unknown")] public long Unknown { get; set; }

        [JsonProperty("total")] public long Total => Failed + Broken + Skipped + Passed + Unknown;
    }

    public partial class HistoryTrend
    {
        public static HistoryTrend[] FromJson(string json)
        {
            return JsonConvert.DeserializeObject<HistoryTrend[]>(json, Converter.Settings);
        }
    }

    public static class Serialize
    {
        public static string ToJson(this HistoryTrend[] self)
        {
            return JsonConvert.SerializeObject(self, Converter.Settings);
        }
    }

    internal static class Converter
    {
        public static readonly JsonSerializerSettings Settings = new JsonSerializerSettings
        {
            MetadataPropertyHandling = MetadataPropertyHandling.Ignore,
            DateParseHandling = DateParseHandling.None,
            Converters =
            {
                new IsoDateTimeConverter {DateTimeStyles = DateTimeStyles.AssumeUniversal}
            }
        };
    }
}