using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Allure.Commons.Json;
using Allure.Commons.Model;
using Newtonsoft.Json;

namespace Allure.Commons.Helpers
{
    internal class LocalHistoryTrendMaker
    {
        internal static void MakeLocalHistoryTrend()
        {
            var dirWithResults =
                AllureLifecycle.Instance.GetDirectoryWithResults(AllureLifecycle.Instance.Config.Allure.Directory);
            var historyDir = Path.Combine(dirWithResults, AllureConstants.HistoryDirName);
            if (AllureLifecycle.Instance.Config.Allure.AllowLocalHistoryTrend)
            {
                var newPreviousResult = new Statistic();
                var previousFilesWithTests = Directory.GetFiles(dirWithResults, "*-test-run1*");
                if (previousFilesWithTests.Length == 0) return;

                foreach (var filePath in previousFilesWithTests)
                    using (var reader = File.OpenText(filePath))
                    {
                        var statusOfTest = JsonConvert.DeserializeObject<StatusTests>(reader.ReadToEnd());
                        switch (statusOfTest.StatusEnum)
                        {
                            case Status.none:
                                newPreviousResult.Unknown++;
                                break;
                            case Status.failed:
                                newPreviousResult.Failed++;
                                break;
                            case Status.broken:
                                newPreviousResult.Broken++;
                                break;
                            case Status.passed:
                                newPreviousResult.Passed++;
                                break;
                            case Status.skipped:
                                newPreviousResult.Skipped++;
                                break;
                        }
                    }

                LinkedList<HistoryTrend> historyTrend;
                var pathHistoryTrend = Path.Combine(historyDir, AllureConstants.HistoryTrendFileName);
                long buildOrder = 1;
                if (File.Exists(pathHistoryTrend))
                {
                    using (var reader =
                        new StreamReader(pathHistoryTrend))
                    {
                        var str = reader.ReadToEnd();
                        historyTrend = new LinkedList<HistoryTrend>(HistoryTrend.FromJson(str).ToList());
                        buildOrder = historyTrend.First().BuildOrder + 1;
                    }
                }
                else
                {
                    if (!Directory.Exists(historyDir)) Directory.CreateDirectory(historyDir);

                    historyTrend = new LinkedList<HistoryTrend>();
                }

                historyTrend.AddFirst(new HistoryTrend {BuildOrder = buildOrder});
                historyTrend.First().Statistic = newPreviousResult;

                var serialized = JsonConvert.SerializeObject(historyTrend, Formatting.Indented);
                using (var reader = new StreamWriter(pathHistoryTrend, false))
                {
                    reader.WriteLine(serialized);
                }
            }
            else
            {
                if (Directory.Exists(historyDir))
                    try
                    {
                        Directory.Delete(historyDir, true);
                    }
                    catch (UnauthorizedAccessException)

                    {
                        // nothing
                    }
            }
        }
    }
}