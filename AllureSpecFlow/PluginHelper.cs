using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Allure.Commons;
using Allure.Commons.Helpers;
using Allure.Commons.Model;
using Allure.NUnit.Attributes;
using NUnit.Framework.Internal;
using TechTalk.SpecFlow;
using TechTalk.SpecFlow.Bindings;
using static Allure.Commons.Json.Configuration;
using TestResult = Allure.Commons.Model.TestResult;

namespace AllureSpecFlow
{
    public static class PluginHelper
    {
        public const string IgnoreException = "IgnoreException";
        private static readonly ScenarioInfo EmptyScenarioInfo = new ScenarioInfo(string.Empty, string.Empty);

        private static readonly FeatureInfo EmptyFeatureInfo = new FeatureInfo(
            CultureInfo.CurrentCulture, string.Empty, string.Empty);

        private static SpecFlowCfg _specFlowCfg;

        internal static SpecFlowCfg SpecFlowCfg
        {
            get
            {
                if (_specFlowCfg != null) return _specFlowCfg;
                _specFlowCfg = GetConfiguration();
                return _specFlowCfg;
            }
        }

        private static SpecFlowCfg GetConfiguration()
        {
            var config = AllureLifecycle.Instance.Config.SpecFlow;
            if (config == null)
            {
                const string msg =
                    "Can't find specflow block in allureConfig.json.\nAdd specflow block before run tests.\nVisit https://github.com/Noksa/Allure.NUnit/wiki/SpecFlow-configuration for more information.";
                throw new ConfigurationException(msg);
            }

            ReportHelper.IsSpecFlow = true;
            return config;
        }

        internal static string GetFeatureContainerId(FeatureInfo featureInfo)
        {
            var id = featureInfo != null
                ? featureInfo.GetHashCode().ToString()
                : EmptyFeatureInfo.GetHashCode().ToString();

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
            var featureInfo = featureContext?.FeatureInfo ?? EmptyFeatureInfo;
            var scenarioInfo = scenarioContext?.ScenarioInfo ?? EmptyScenarioInfo;
            var tags = GetTags(featureInfo, scenarioInfo);
            var currentTest = TestExecutionContext.CurrentContext.CurrentTest;
            var fullNameForLog = ReportHelper.GenerateFullNameWithParameters(currentTest, currentTest.FullName);
            var testResult = new TestResult
            {
                uuid = NewId(),
                historyId = fullNameForLog,
                name = scenarioInfo.Title,
                fullName = fullNameForLog,
                labels = new List<Label>
                    {
                        Label.Thread(),
                        string.IsNullOrWhiteSpace(AllureLifecycle.Instance.Config.Allure.Title)
                            ? Label.Host()
                            : Label.Host(AllureLifecycle.Instance.Config.Allure.Title),
                        Label.Feature(featureInfo.Title)
                    }
                    .Union(tags.Item1).ToList(),
                links = tags.Item2
            };
            AllureLifecycle.Instance.StartTestCase(containerId, testResult);
            scenarioContext?.Set(testResult);
            featureContext?.Get<HashSet<TestResult>>().Add(testResult);
            ReportHelper.AddToTestCaseParametersInfo(currentTest, testResult.uuid, new[] { -1 }, new[] { -1 });
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
                   (!string.IsNullOrWhiteSpace(ex.InnerException?.Message)
                       ? $" -> {GetFullExceptionMessage(ex.InnerException)}"
                       : string.Empty);
        }

        private static Tuple<List<Label>, List<Link>> GetTags(FeatureInfo featureInfo, ScenarioInfo scenarioInfo)
        {
            var result = Tuple.Create(new List<Label>(), new List<Link>());

            var tags = scenarioInfo.Tags
                .Union(featureInfo.Tags)
                .Distinct(StringComparer.CurrentCultureIgnoreCase);

            foreach (var tag in tags)
            {
                // link
                if (TryUpdateValueByMatch(SpecFlowCfg.links.link, tag, out var tagValue))
                {
                    var linkAttr = new AllureLinkAttribute(tagValue);
                    var link = ReportHelper.GetValueWithPattern(linkAttr);
                    result.Item2.Add(link);
                    continue;
                }
                // issue

                if (TryUpdateValueByMatch(SpecFlowCfg.links.issue, tag, out tagValue))
                {
                    var issueAttr = new AllureIssueAttribute(tagValue);
                    var issue = ReportHelper.GetValueWithPattern(issueAttr);
                    result.Item2.Add(issue);
                    continue;
                }

                // tms

                if (TryUpdateValueByMatch(SpecFlowCfg.links.tms, tag, out tagValue))
                {
                    var tmsAttr = new AllureTmsAttribute(tagValue);
                    var tms = ReportHelper.GetValueWithPattern(tmsAttr);
                    result.Item2.Add(tms);
                    continue;
                }

                // parent suite

                if (TryUpdateValueByMatch(SpecFlowCfg.grouping.suites.parentSuite, tag, out tagValue))
                {
                    result.Item1.Add(Label.ParentSuite(tagValue));
                    continue;
                }

                // suite

                if (TryUpdateValueByMatch(SpecFlowCfg.grouping.suites.suite, tag, out tagValue))
                {
                    result.Item1.Add(Label.Suite(tagValue));
                    continue;
                }

                // sub suite

                if (TryUpdateValueByMatch(SpecFlowCfg.grouping.suites.subSuite, tag, out tagValue))
                {
                    result.Item1.Add(Label.SubSuite(tagValue));
                    continue;
                }

                // epic

                if (TryUpdateValueByMatch(SpecFlowCfg.grouping.behaviors.epic, tag, out tagValue))
                {
                    result.Item1.Add(Label.Epic(tagValue));
                    continue;
                }

                // story

                if (TryUpdateValueByMatch(SpecFlowCfg.grouping.behaviors.story, tag, out tagValue))
                {
                    result.Item1.Add(Label.Story(tagValue));
                    continue;
                }

                // package

                if (TryUpdateValueByMatch(SpecFlowCfg.grouping.packages.package, tag, out tagValue))
                {
                    result.Item1.Add(Label.Package(tagValue));
                    continue;
                }

                // test class

                if (TryUpdateValueByMatch(SpecFlowCfg.grouping.packages.testClass, tag, out tagValue))
                {
                    result.Item1.Add(Label.TestClass(tagValue));
                    continue;
                }

                // test method

                if (TryUpdateValueByMatch(SpecFlowCfg.grouping.packages.testMethod, tag, out tagValue))
                {
                    result.Item1.Add(Label.TestMethod(tagValue));
                    continue;
                }

                // owner

                if (TryUpdateValueByMatch(SpecFlowCfg.labels.owner, tag, out tagValue))
                {
                    result.Item1.Add(Label.Owner(tagValue));
                    continue;
                }

                // severity

                if (TryUpdateValueByMatch(SpecFlowCfg.labels.severity, tag, out tagValue) &&
                    Enum.TryParse(tagValue, true, out SeverityLevel level))
                {
                    result.Item1.Add(Label.Severity(level));
                    continue;
                }

                result.Item1.Add(Label.Tag(tag));
            }

            return result;
        }

        private static bool TryUpdateValueByMatch(string expression, string tag, out string value)
        {
            value = null;
            if (string.IsNullOrWhiteSpace(expression) || string.IsNullOrWhiteSpace(tag)) return false;

            Regex regex;
            try
            {
                regex = new Regex(expression,
                    RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase);
            }
            catch (Exception)
            {
                return false;
            }

            var match = regex.Match(tag);

            if (!match.Success) return false;

            var groups = match.Groups;
            value = groups.Count == 1 ? groups[0].Value : groups[1].Value;

            return true;
        }
    }
}