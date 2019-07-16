using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Allure.Commons;
using Allure.Commons.Helpers;
using Allure.Commons.Json;
using Allure.Commons.Model;
using Allure.NUnit.Attributes;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;
using TestResult = Allure.Commons.Model.TestResult;

namespace AllureSpecFlow
{
    public static class PluginHelper
    {
        public static string IGNORE_EXCEPTION = "IgnoreException";
        private static readonly ScenarioInfo emptyScenarioInfo = new ScenarioInfo(string.Empty, string.Empty);
        private static FeatureInfo emptyFeatureInfo = new FeatureInfo(
            CultureInfo.CurrentCulture, string.Empty, string.Empty);

        internal static Configuration.SpecFlowCfg PluginConfiguration = GetConfiguration();

        private static Configuration.SpecFlowCfg GetConfiguration()
        {
            var config = AllureLifecycle.Instance.Config.SpecFlow;
            if (config == null) throw new NullReferenceException("Can't find specflow section in allure config file.");
            ReportHelper.IsSpecFlow = true;
            return config;
        }
        internal static string GetFeatureContainerId(FeatureInfo featureInfo)
        {
            var id = (featureInfo != null)
                ? featureInfo.GetHashCode().ToString()
                : emptyFeatureInfo.GetHashCode().ToString();

            return id;
        }

        internal static string NewId()
        {
            return Guid.NewGuid().ToString("N");
        }

        internal static FixtureResult GetFixtureResult(HookBinding hook)
        {
            return new FixtureResult
            {
                name = $"{hook.Method.Name} [{hook.HookOrder}]"
            };
        }

        internal static TestResult StartTestCase(string containerId, FeatureContext featureContext,
            ScenarioContext scenarioContext)
        {
            var featureInfo = featureContext?.FeatureInfo ?? emptyFeatureInfo;
            var scenarioInfo = scenarioContext?.ScenarioInfo ?? emptyScenarioInfo;
            var tags = GetTags(featureInfo, scenarioInfo);
            var testResult = new TestResult
            {
                uuid = NewId(),
                historyId = scenarioInfo.Title,
                name = scenarioInfo.Title,
                fullName = scenarioInfo.Title,
                labels = new List<Label>
                    {
                        Label.Thread(),
                        string.IsNullOrWhiteSpace(AllureLifecycle.Instance.Config.Allure.Title) ? Label.Host() : Label.Host(AllureLifecycle.Instance.Config.Allure.Title),
                        Label.Feature(featureInfo.Title)
                    }
                    .Union(tags.Item1).ToList(),
                links = tags.Item2
            };

            AllureLifecycle.Instance.StartTestCase(containerId, testResult);
            scenarioContext?.Set(testResult);
            featureContext?.Get<HashSet<TestResult>>().Add(testResult);
            return testResult;
        }

        internal static TestResult GetCurrentTestCase(ScenarioContext context)
        {
            context.TryGetValue(out TestResult testresult);
            return testresult;
        }

        internal static TestResultContainer StartTestContainer(FeatureContext featureContext,
            ScenarioContext scenarioContext)
        {
            var containerId = GetFeatureContainerId(featureContext?.FeatureInfo);

            var scenarioContainer = new TestResultContainer
            {
                uuid = NewId()
            };
            AllureLifecycle.Instance.StartTestContainer(containerId, scenarioContainer);
            scenarioContext?.Set(scenarioContainer);
            featureContext?.Get<HashSet<TestResultContainer>>().Add(scenarioContainer);
            return scenarioContainer;
        }

        internal static TestResultContainer GetCurrentTestConainer(ScenarioContext context)
        {
            context.TryGetValue(out TestResultContainer testresultContainer);
            return testresultContainer;
        }

        internal static StatusDetails GetStatusDetails(Exception ex)
        {
            return new StatusDetails
            {
                message = GetFullExceptionMessage(ex),
                trace = ex.ToString()
            };

        }

        private static string GetFullExceptionMessage(Exception ex)
        {
            return ex.Message +
            (!string.IsNullOrWhiteSpace(ex.InnerException?.Message) ?
                $" -> {GetFullExceptionMessage(ex.InnerException)}" : string.Empty);
        }

        private static Tuple<List<Label>, List<Link>> GetTags(FeatureInfo featureInfo, ScenarioInfo scenarioInfo)
        {
            var result = Tuple.Create(new List<Label>(), new List<Link>());

            var tags = scenarioInfo.Tags
                .Union(featureInfo.Tags)
                .Distinct(StringComparer.CurrentCultureIgnoreCase);

            foreach (var tag in tags)
            {
                var tagValue = tag;
                // link
                if (TryUpdateValueByMatch(PluginConfiguration.links.link, ref tagValue))
                {
                    var linkAttr = new AllureLinkAttribute(tagValue);
                    var link = ReportHelper.GetValueWithPattern(linkAttr);
                    result.Item2.Add(link); continue;
                }
                // issue
                if (TryUpdateValueByMatch(PluginConfiguration.links.issue, ref tagValue))
                {
                    var issueAttr = new AllureIssueAttribute(tagValue);
                    var issue = ReportHelper.GetValueWithPattern(issueAttr);
                    result.Item2.Add(issue); continue;
                }
                // tms
                if (TryUpdateValueByMatch(PluginConfiguration.links.tms, ref tagValue))
                {
                    var tmsAttr = new AllureTmsAttribute(tagValue);
                    var tms = ReportHelper.GetValueWithPattern(tmsAttr);
                    result.Item2.Add(tms); continue;
                }
                // parent suite
                if (TryUpdateValueByMatch(PluginConfiguration.grouping.suites.parentSuite, ref tagValue))
                {
                    result.Item1.Add(Label.ParentSuite(tagValue)); continue;
                }
                // suite
                if (TryUpdateValueByMatch(PluginConfiguration.grouping.suites.suite, ref tagValue))
                {
                    result.Item1.Add(Label.Suite(tagValue)); continue;
                }
                // sub suite
                if (TryUpdateValueByMatch(PluginConfiguration.grouping.suites.subSuite, ref tagValue))
                {
                    result.Item1.Add(Label.SubSuite(tagValue)); continue;
                }
                // epic
                if (TryUpdateValueByMatch(PluginConfiguration.grouping.behaviors.epic, ref tagValue))
                {
                    result.Item1.Add(Label.Epic(tagValue)); continue;
                }
                // story
                if (TryUpdateValueByMatch(PluginConfiguration.grouping.behaviors.story, ref tagValue))
                {
                    result.Item1.Add(Label.Story(tagValue)); continue;
                }
                // package
                if (TryUpdateValueByMatch(PluginConfiguration.grouping.packages.package, ref tagValue))
                {
                    result.Item1.Add(Label.Package(tagValue)); continue;
                }
                // test class
                if (TryUpdateValueByMatch(PluginConfiguration.grouping.packages.testClass, ref tagValue))
                {
                    result.Item1.Add(Label.TestClass(tagValue)); continue;
                }
                // test method
                if (TryUpdateValueByMatch(PluginConfiguration.grouping.packages.testMethod, ref tagValue))
                {
                    result.Item1.Add(Label.TestMethod(tagValue)); continue;
                }
                // owner
                if (TryUpdateValueByMatch(PluginConfiguration.labels.owner, ref tagValue))
                {
                    result.Item1.Add(Label.Owner(tagValue)); continue;
                }
                // severity
                if (TryUpdateValueByMatch(PluginConfiguration.labels.severity, ref tagValue) && Enum.TryParse(tagValue, true, out SeverityLevel level))
                {
                    result.Item1.Add(Label.Severity(level)); continue;
                }
                // tag
                result.Item1.Add(Label.Tag(tagValue));
            }
            return result;
        }

        private static bool TryUpdateValueByMatch(string expression, ref string value)
        {
            if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(expression))
                return false;

            Regex regex;
            try
            {
                regex = new Regex(expression, RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }

            if (regex.IsMatch(value))
            {
                var groups = regex.Match(value).Groups;
                value = groups.Count == 1 ? groups[0].Value : groups[1].Value;

                return true;
            }

            return false;
        }
    }
}