using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using System.Threading;
using Allure.Commons.Model;
using Allure.Commons.Storage;
using Allure.NUnit.Attributes;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;
using TestResult = Allure.Commons.Model.TestResult;

[assembly: InternalsVisibleTo("Allure.SpecFlowPlugin")]

namespace Allure.Commons.Helpers
{
    internal static class ReportHelper
    {
        internal static readonly object Locker = new object();
        internal static bool IsSpecFlow { get; set; }

        internal static List<ITest> GetAllTestsInSuite(ITest suite)
        {
            var list = new List<ITest>();
            if (suite.Tests.Count == 0) return list;
            foreach (var nestedTests1 in suite.Tests)
                if (nestedTests1.HasChildren)
                    foreach (var nestedTests2 in nestedTests1.Tests)
                        if (nestedTests2.HasChildren)
                            foreach (var nestedTests3 in nestedTests2.Tests)
                                if (nestedTests3.HasChildren)
                                    foreach (var nestedTests4 in nestedTests3.Tests)
                                        list.Add(nestedTests4);
                                else
                                    list.Add(nestedTests3);
                        else
                            list.Add(nestedTests2);
                else
                    list.Add(nestedTests1);

            return list;
        }

        internal static void AddToTestCaseParametersInfo(ITest test, string testUuid, int[] hideParams,
            int[] removeParams)
        {
            if (AllureLifecycle.Instance.Config.Allure.EnableParameters)
            {
                var listOfArgs = test.Arguments.ToList();
                for (var i = 0; i < listOfArgs.Count; i++)
                {
                    var paramNum = i + 1;
                    var arg = listOfArgs[i];
                    var strArg = arg != null ? arg.ToString() : "null";
                    var argType = arg != null ? arg.GetType().Name : "Unknown";
                    var param = new Parameter
                    {
                        name = $"Parameter #{paramNum}, {argType}",
                        value = hideParams.Contains(paramNum) ? "Parameter is hidden" : strArg
                    };
                    if (removeParams.Contains(paramNum)) continue;
                    AllureLifecycle.Instance.UpdateTestCase(testUuid,
                        x => x.parameters.Add(param));
                }
            }
        }

        internal static void AddStepParameters(object[] stepParams, string stepUuid = null)
        {
            if (string.IsNullOrEmpty(stepUuid)) stepUuid = AllureLifecycle.Instance.Storage.GetCurrentStep();
            if (!AllureLifecycle.Instance.Config.Allure.EnableParameters) return;
            if (stepParams == null || stepParams.Length == 0) return;

            for (var i = 0; i < stepParams.Length; i++)
            {
                var strArg = stepParams[i].ToString();
                var param = new Parameter
                {
                    name = $"Parameter #{i + 1}, {stepParams[i].GetType().Name}",
                    value = strArg
                };
                AllureLifecycle.Instance.UpdateStep(stepUuid, q => q.parameters.Add(param));
            }
        }

        internal static string GenerateFullNameWithParameters(ITest iTest, string suiteNameFromAttr)
        {
            if (iTest.IsSuite)
            {
                if (!AllureLifecycle.Instance.Config.Allure.EnableParameters) return suiteNameFromAttr;
            }

            var listOfArgs = iTest.Arguments.ToList();
            if (listOfArgs.Count == 0) return suiteNameFromAttr;
            var suiteFullName = $"{suiteNameFromAttr} (";
            foreach (var arg in listOfArgs)

            {
                string argValue;
                switch (arg)
                {
                    case string _:
                        argValue = $"\"{arg}\"";
                        break;
                    case decimal _:
                        argValue = ((decimal) arg).ToString(CultureInfo.InvariantCulture);
                        break;
                    case float _:
                        argValue = ((float) arg).ToString(CultureInfo.InvariantCulture);
                        break;
                    case double _:
                        argValue = ((double) arg).ToString(CultureInfo.InvariantCulture);
                        break;
                    default:
                        argValue = $"{arg}";
                        break;
                }

                argValue = $"{argValue}, ";
                suiteFullName = $"{suiteFullName}{argValue}";
            }

            suiteFullName = suiteFullName.Substring(0, suiteFullName.Length - 2);
            suiteFullName = $"{suiteFullName})";
            return suiteFullName;
        }


        internal static void AddInfoInTestCase(ITest test, string testUuid, ITest suite)
        {
            var testMethod = test.Method.MethodInfo;
            if (testMethod.DeclaringType != null)
            {
                var testClassAttrs = testMethod.DeclaringType.GetCustomAttributes().ToList();
                if (!testClassAttrs.Any(e => e is AllureSuiteAttribute))
                    AllureLifecycle.Instance.UpdateTestCase(testUuid,
                        x => { x.labels.Add(Label.Suite(suite.FullName)); });

                AddInfoToTestCaseFromAttributes(test, testUuid, suite, testClassAttrs, false);
            }

            var attrs = testMethod.GetCustomAttributes().ToList();

            var defects = attrs.Where(_ => _ is AllureIssueAttribute).Cast<AllureIssueAttribute>().Count();
            AddInfoToTestCaseFromAttributes(test, testUuid, suite, attrs, true);
            if (defects != 0)
                AllureLifecycle.Instance.UpdateTestCase(testUuid, _ =>
                {
                    var subSuites = _.labels.Where(lbl => lbl.name.ToLower().Equals("subsuite")).ToList();
                    subSuites.ForEach(lbl => _.labels.Remove(lbl));
                    _.labels.Add(Label.SubSuite("With defects"));
                });
        }

        internal static void AddInfoToTestCaseFromAttributes(ITest test, string testUuid, ITest suite,
            IEnumerable<Attribute> attrs,
            bool testMethodAttrs)
        {
            var removeParamsNumber = new[] {-999};
            var hideParamsNumber = new[] {-999};
            foreach (var attribute in attrs)
                switch (attribute)
                {
                    case AllureFeatureAttribute featureAttr:
                        foreach (var feature in featureAttr.Features)
                            AllureLifecycle.Instance.UpdateTestCase(
                                testUuid,
                                x => x.labels.Add(Label.Feature(feature)));
                        break;
                    case AllureIssueAttribute issueAttr:
                        var value = GetValueWithPattern(issueAttr);
                        AllureLifecycle.Instance.UpdateTestCase(
                            testUuid,
                            x => x.links.Add(value));
                        break;
                    case AllureSeverityAttribute severityAttr:
                        AllureLifecycle.Instance.UpdateTestCase(
                            testUuid,
                            x => x.labels.Add(Label.Severity(severityAttr.Severity)));
                        break;
                    case AllureStoryAttribute storyAttr:
                        foreach (var story in storyAttr.Stories)
                            AllureLifecycle.Instance.UpdateTestCase(
                                testUuid,
                                x => x.labels.Add(Label.Story(story)));
                        break;
                    case AllureTagAttribute tagAttr:
                        foreach (var tag in tagAttr.Tags)
                            AllureLifecycle.Instance.UpdateTestCase(
                                testUuid,
                                x => x.labels.Add(Label.Tag(tag)));
                        break;
                    case AllureTestAttribute testAttr:
                        if (!string.IsNullOrEmpty(testAttr.Description))
                            AllureLifecycle.Instance.UpdateTestCase(
                                testUuid,
                                x => x.description = testAttr.Description);
                        break;
                    case AllureTmsAttribute tmsAttr:
                        value = GetValueWithPattern(tmsAttr);
                        AllureLifecycle.Instance.UpdateTestCase(
                            testUuid,
                            x => x.links.Add(value));
                        break;
                    case AllureSuiteAttribute suiteAttr:
                        AllureLifecycle.Instance.UpdateTestCase(
                            testUuid, x =>
                            {
                                var suiteName = GenerateFullNameWithParameters(suite, suiteAttr.Suite);
                                x.labels.Add(Label.Suite(suiteName));
                            });
                        break;
                    case AllureSubSuiteAttribute subSuiteAttr:
                        AllureLifecycle.Instance.UpdateTestCase(
                            testUuid,
                            x => x.labels.Add(Label.SubSuite(subSuiteAttr.SubSuite)));
                        break;
                    case AllureOwnerAttribute ownerAttr:
                        AllureLifecycle.Instance.UpdateTestCase(
                            testUuid,
                            x => x.labels.Add(Label.Owner(ownerAttr.Owner)));
                        break;
                    case AllureEpicAttribute epicAttr:
                        AllureLifecycle.Instance.UpdateTestCase(
                            testUuid,
                            x => x.labels.Add(Label.Epic(epicAttr.Epic)));
                        break;
                    case AllureParentSuiteAttribute parentSuiteAttr:
                        AllureLifecycle.Instance.UpdateTestCase(
                            testUuid, x =>
                                x.labels.Add(Label.ParentSuite(parentSuiteAttr.ParentSuite)));
                        break;
                    case AllureRemoveParamsAttribute removeParamsAttr:
                        removeParamsNumber = removeParamsAttr.ParamNumbers;
                        break;
                    case AllureHideParamsAttribute hideParamsAttr:
                        hideParamsNumber = hideParamsAttr.ParamNumbers;
                        break;
                    case AllureLinkAttribute linkAttr:
                        value = GetValueWithPattern(linkAttr);
                        AllureLifecycle.Instance.UpdateTestCase(
                            testUuid,
                            x => x.links.Add(value));
                        break;
                    case AllureFlakyAttribute flakyAttr:
                        AllureLifecycle.Instance.UpdateTestCase(
                            testUuid,
                            x => x.statusDetails.flaky = flakyAttr.Flaky);
                        break;
                }

            if (testMethodAttrs) AddToTestCaseParametersInfo(test, testUuid, hideParamsNumber, removeParamsNumber);
        }


        internal static Status GetNUnitStatus(TestStatus status)
        {
            switch (status)
            {
                case TestStatus.Inconclusive:
                    return Status.broken;
                case TestStatus.Skipped:
                    return Status.skipped;
                case TestStatus.Passed:
                    return Status.passed;
                case TestStatus.Warning:
                    return Status.broken;
                case TestStatus.Failed:
                    return Status.failed;
                default:
                    return Status.none;
            }
        }

        internal static void StartAllureLogging(ITest test, string testUuid, string testContainerUuid,
            TestFixture fixture)
        {
            var testFullNameForLog = GenerateFullNameWithParameters(test, test.FullName);
            var ourFixture = new TestResultContainer
            {
                uuid = testContainerUuid,
                name = test.ClassName
            };
            AllureLifecycle.Instance.StartTestContainer(ourFixture);
            var testResult = new TestResult
            {
                uuid = testUuid,
                name = test.Name,
                fullName = testFullNameForLog,
                labels = new List<Label>
                {
                    Label.Thread(),
                    Label.Host(),
                    Label.TestClass(test.ClassName),
                    Label.TestMethod(test.MethodName),
                    Label.Package(test.ClassName)
                },
                historyId = testFullNameForLog,
                statusDetails = new StatusDetails()
            };
            AllureLifecycle.Instance.StartTestCase(testResult);
            AddInfoInTestCase(test, testUuid, fixture);
        }

        internal static void StopAllureLogging(TestContext.ResultAdapter testResult, string testUuid,
            string testContainerUuid, ITest suite, bool updateStopTime, string ignoreReason)
        {
            var result = testResult;
            var testMsg = result.Message;
            var testStackTrace = result.StackTrace;
            Logger.LogInProgress(
                $"Entering stop allure logging for \"{testUuid}\"");

            if (!string.IsNullOrEmpty(ignoreReason)) testMsg = ignoreReason;
            AllureLifecycle.Instance.UpdateTestCase(testUuid, x =>
            {
                x.statusDetails.message = MakeGoodErrorMsg(testMsg);
                x.statusDetails.trace = testStackTrace;
                x.status = GetNUnitStatus(result.Outcome.Status);
            });
            AllureLifecycle.Instance.StopTestCase(testUuid,
                updateStopTime);
            AllureLifecycle.Instance.WriteTestCase(testUuid);
            AllureLifecycle.Instance.UpdateTestContainer(
                testContainerUuid,
                q => q.children.Add(testUuid));
            AllureLifecycle.Instance.StopTestContainer(
                testContainerUuid, updateStopTime);
            AllureLifecycle.Instance.WriteTestContainer(testContainerUuid);
            Logger.LogInProgress($"Stopped allure logging for test {testUuid}, {testContainerUuid}, {ignoreReason}");
        }

        internal static string MakeGoodErrorMsg(string errorMsg)
        {
            if (string.IsNullOrEmpty(errorMsg)) return errorMsg;
            var index = errorMsg.IndexOf("Multiple", StringComparison.Ordinal);
            if (index == -1 || index == 0) return errorMsg;
            var goodMsg = errorMsg.Substring(0, index) + " \r\n" + errorMsg.Substring(index);
            return goodMsg;
        }

        internal static void AddIgnoredTestsToReport(ITest suite)
        {
            var ignoredTests = new Dictionary<ITest, string>();
            AllureStorage.MainThreadId = Thread.CurrentThread.ManagedThreadId;

            bool IsIgnored(ITest oTest)
            {
                return oTest.RunState == RunState.Ignored || oTest.RunState == RunState.Skipped;
            }

            foreach (var testMethod in suite.Tests)
            {
                if (!testMethod.HasChildren && IsIgnored(testMethod))
                    ignoredTests.Add(testMethod, testMethod.Properties.Get(PropertyNames.SkipReason).ToString());

                if (testMethod.HasChildren && IsIgnored(testMethod))
                    testMethod.Tests.ToList().ForEach(_ =>
                        ignoredTests.Add(_, testMethod.Properties.Get(PropertyNames.SkipReason).ToString()));
                if (testMethod.HasChildren && !IsIgnored(testMethod))
                    foreach (var localTest in testMethod.Tests)
                        if (IsIgnored(localTest))
                            ignoredTests.Add(localTest, localTest.Properties.Get(PropertyNames.SkipReason).ToString());
            }

            suite.SetProp(AllureConstants.IgnoredTests, ignoredTests);
            FailIgnoredTests(ignoredTests, suite);
        }

        internal static Link GetValueWithPattern(Attribute attr)
        {
            Link value = null;
            var isNeedReplace = false;
            if (attr is AllureTmsAttribute || attr is AllureLinkAttribute || attr is AllureIssueAttribute)
            {
                switch (attr)
                {
                    case AllureTmsAttribute tmsAttr:
                        value = tmsAttr.TmsLink;
                        isNeedReplace = tmsAttr.ReplaceWithPattern;
                        break;
                    case AllureIssueAttribute issueAttr:
                        value = issueAttr.IssueLink;
                        isNeedReplace = issueAttr.ReplaceWithPattern;
                        break;
                    case AllureLinkAttribute linkAttr:
                        value = linkAttr.Link;
                        isNeedReplace = linkAttr.ReplaceWithPattern;
                        break;
                }
            }
            else throw new ArgumentException(nameof(attr));

            if (!string.IsNullOrWhiteSpace(value?.type) && isNeedReplace)
            {
                var typeValue = $"{{{value.type}}}";
                var pattern = AllureLifecycle.Instance.Config.Allure.Links.FirstOrDefault(p =>
                    Regex.IsMatch(p, $".*{typeValue}.*", RegexOptions.IgnoreCase));
                if (!string.IsNullOrEmpty(pattern))
                {
                    var newUrl = Regex.Replace(pattern, typeValue, value.url);
                    value.url = newUrl;
                }
            }

            return value;
        }

        #region Privates

        private static void FailIgnoredTests(Dictionary<ITest, string> dict, ITest suite)
        {
            lock (Locker)
            {
                foreach (var pair in dict)
                {
                    pair.Key.Properties.Set(AllureConstants.TestIgnoreReason,
                        $"Test was ignored by reason: {pair.Value}");
                    var testResult = new TestContext.ResultAdapter(new TestCaseResult(new TestMethod(pair.Key.Method)));
                    AddInfoToIgnoredTest(ref testResult);
                    pair.Key.Properties.Set(AllureConstants.TestResult, testResult);
                    AllureLifecycle.Instance.Storage.ClearStepContext();
                    AllureLifecycle.Instance.Storage.CurrentThreadStepContext.AddLast(pair.Key.Properties
                        .Get(AllureConstants.TestUuid).ToString());
                    AllureLifecycle.Instance.StartStepAndStopIt(null, $"Test was ignored by reason: {pair.Value}",
                        Status.skipped);
                    AllureLifecycle.Instance.UpdateTestContainer(
                        pair.Key.Properties.Get(AllureConstants.TestContainerUuid).ToString(),
                        x => x.start = AllureLifecycle.ToUnixTimestamp());
                    AllureLifecycle.Instance.UpdateTestCase(pair.Key.Properties
                            .Get(AllureConstants.TestUuid).ToString(),
                        x =>
                        {
                            x.start = AllureLifecycle.ToUnixTimestamp();
                            x.labels.RemoveAll(q => q.name == "thread");
                            x.labels.Add(Label.Thread());
                            var subSuites = x.labels.Where(lbl => lbl.name.ToLower().Equals("subsuite")).ToList();
                            subSuites.ForEach(lbl => x.labels.Remove(lbl));
                            x.labels.Add(Label.SubSuite("Ignored tests/test-cases"));
                        });
                    Thread.Sleep(5);
                    StopAllureLogging(testResult, pair.Key.Properties
                        .Get(AllureConstants.TestUuid).ToString(), pair.Key.Properties
                        .Get(AllureConstants.TestContainerUuid).ToString(), suite, true, pair.Value);
                }
            }
        }

        private static void AddInfoToIgnoredTest(ref TestContext.ResultAdapter testResult)
        {
            var type = testResult.Outcome.GetType();
            var backingField = type.GetBackingField(nameof(ResultState.Status), true);
            backingField.SetValue(testResult.Outcome, TestStatus.Skipped);
            backingField = type.GetBackingField(nameof(ResultState.Label), true);
            backingField.SetValue(testResult.Outcome, AllureConstants.TestWasIgnored);
        }

        #endregion
    }
}