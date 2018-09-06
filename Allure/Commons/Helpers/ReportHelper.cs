using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Allure.Commons.Model;
using Allure.Commons.Storage;
using Allure.NUnit.Attributes;
using NUnit.Framework;
using NUnit.Framework.Interfaces;
using TestResult = Allure.Commons.Model.TestResult;

namespace Allure.Commons.Helpers
{
    internal static class ReportHelper
    {
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

        internal static void AddToTestCaseParametersInfo(int[] hideParams, int[] removeParams)
        {
            var list = new List<object>();
            if (TestContext.CurrentContext.Result.Outcome.Site == FailureSite.SetUp &&
                AllureLifecycle.Instance.Config.Allure.AllowEmptySuites && string.IsNullOrEmpty(TestContext.CurrentContext.Test.MethodName) && TestContext.CurrentContext.Result.Outcome.Status == TestStatus.Failed)
            {
                if (AllureLifecycle.Instance._currentSuiteTests.Count != 0)
                {
                    foreach (var notStartedTest in AllureLifecycle.Instance._currentSuiteTests)
                    {
                        list = notStartedTest.Arguments.ToList();
                        AddParam(list);
                    }
                }
            }
            else
            {
                list = TestContext.CurrentContext.Test.Arguments.ToList();
                if (list.Count != 0) AddParam(list);
            }

            void AddParam(IReadOnlyList<object> listOfArgs)
            {
                for (var i = 0; i < listOfArgs.Count; i++)
                {
                    var paramNum = i + 1;
                    var strArg = listOfArgs[i].ToString();
                    var param = new Parameter
                    {
                        name = $"Parameter #{paramNum}, {listOfArgs[i].GetType().Name}",
                        value = hideParams.Contains(paramNum) ? "Parameter is hidden" : strArg
                    };
                    if (removeParams.Contains(paramNum)) continue;
                    AllureLifecycle.Instance.UpdateTestCase(AllureReport.TestUuid, x => x.parameters.Add(param));
                }
            }
        }

        internal static void AddInfoInTestCase(MethodInfo testMethod)
        {
            if (testMethod.DeclaringType != null)
            {
                var testClassAttrs = testMethod.DeclaringType.GetCustomAttributes().ToList();
                if (!testClassAttrs.Any(e => e is AllureSuiteAttribute))
                    AllureLifecycle.Instance.UpdateTestCase(x =>
                    {
                        x.labels.Add(testMethod.DeclaringType != null
                            ? Label.Suite(testMethod.DeclaringType.FullName)
                            : Label.Suite(testMethod.Name));
                    });

                AddInfoToTestCaseFromAttributes(testClassAttrs, false);
            }

            var attrs = testMethod.GetCustomAttributes().ToList();

            var defects = attrs.Where(_ => _ is AllureIssueAttribute).Cast<AllureIssueAttribute>().Count();
            AddInfoToTestCaseFromAttributes(attrs, true);
            if (defects != 0)
                AllureLifecycle.Instance.UpdateTestCase(_ =>
                {
                    var subSuites = _.labels.Where(lbl => lbl.name.ToLower().Equals("subsuite")).ToList();
                    subSuites.ForEach(lbl => _.labels.Remove(lbl));
                    _.labels.Add(Label.SubSuite("With defects"));
                });
        }

        internal static void AddInfoToTestCaseFromAttributes(IEnumerable<Attribute> attrs, bool testMethodAttrs)
        {
            var removeParamsNumber = new[] {-999};
            var hideParamsNumber = new[] {-999};
            foreach (var attribute in attrs)
                switch (attribute)
                {
                    case AllureFeatureAttribute featureAttr:
                        foreach (var feature in featureAttr.Features)
                            AllureLifecycle.Instance.UpdateTestCase(x => x.labels.Add(Label.Feature(feature)));
                        break;
                    case AllureIssueAttribute issueAttr:
                        AllureLifecycle.Instance.UpdateTestCase(x => x.links.Add(issueAttr.IssueLink));
                        break;
                    case AllureSeverityAttribute severityAttr:
                        AllureLifecycle.Instance.UpdateTestCase(
                            x => x.labels.Add(Label.Severity(severityAttr.Severity)));
                        break;
                    case AllureStoryAttribute storyAttr:
                        foreach (var story in storyAttr.Stories)
                            AllureLifecycle.Instance.UpdateTestCase(x => x.labels.Add(Label.Story(story)));
                        break;
                    case AllureTagAttribute tagAttr:
                        foreach (var tag in tagAttr.Tags)
                            AllureLifecycle.Instance.UpdateTestCase(x => x.labels.Add(Label.Tag(tag)));
                        break;
                    case AllureTestAttribute testAttr:
                        if (!string.IsNullOrEmpty(testAttr.Description))
                            AllureLifecycle.Instance.UpdateTestCase(x => x.description = testAttr.Description);
                        break;
                    case AllureTmsAttribute tmsAttr:
                        AllureLifecycle.Instance.UpdateTestCase(x => x.links.Add(tmsAttr.TmsLink));
                        break;
                    case AllureSuiteAttribute suiteAttr:
                        AllureLifecycle.Instance.UpdateTestCase(x => x.labels.Add(Label.Suite(suiteAttr.Suite)));
                        break;
                    case AllureSubSuiteAttribute subSuiteAttr:
                        AllureLifecycle.Instance.UpdateTestCase(
                            x => x.labels.Add(Label.SubSuite(subSuiteAttr.SubSuite)));
                        break;
                    case AllureOwnerAttribute ownerAttr:
                        AllureLifecycle.Instance.UpdateTestCase(x => x.labels.Add(Label.Owner(ownerAttr.Owner)));
                        break;
                    case AllureEpicAttribute epicAttr:
                        AllureLifecycle.Instance.UpdateTestCase(x => x.labels.Add(Label.Epic(epicAttr.Epic)));
                        break;
                    case AllureParentSuiteAttribute parentSuiteAttr:
                        AllureLifecycle.Instance.UpdateTestCase(x =>
                            x.labels.Add(Label.ParentSuite(parentSuiteAttr.ParentSuite)));
                        break;
                    case AllureRemoveParamsAttribute removeParamsAttr:
                        removeParamsNumber = removeParamsAttr.ParamNumbers;
                        break;
                    case AllureHideParamsAttribute hideParamsAttr:
                        hideParamsNumber = hideParamsAttr.ParamNumbers;
                        break;
                }

            if (testMethodAttrs) AddToTestCaseParametersInfo(hideParamsNumber, removeParamsNumber);
        }

        internal static void StopAllureLogging(MethodInfo methodInfo, string resultMsg, string stackTrace,
            TestStatus status)
        {
            AddInfoInTestCase(methodInfo);
            AllureLifecycle.Instance.UpdateTestCase(x =>
            {
                x.statusDetails = new StatusDetails
                {
                    message = MakeGoodErrorMsg(resultMsg),
                    trace = stackTrace
                };
            });
            AllureLifecycle.Instance.StopTestCase(x =>
                x.status = GetNunitStatus(status));
            AllureLifecycle.Instance.WriteTestCase(AllureReport.TestUuid);
        }

        private static Status GetNunitStatus(TestStatus status)
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

        internal static void StartAllureLogging(string testName, string testId, string testFullName,
            string testClassName,
            string methodName)
        {
            AllureStorage.MainThreadId = Thread.CurrentThread.ManagedThreadId;
            AllureReport.TestUuid = $"{testId}_{Guid.NewGuid():N}";
            var testResult = new TestResult
            {
                uuid = AllureReport.TestUuid,
                name = testName,
                fullName = testFullName,
                labels = new List<Label>
                {
                    Label.Thread(),
                    Label.Host(),
                    Label.TestClass(testClassName),
                    Label.TestMethod(methodName),
                    Label.Package(testClassName)
                },
                historyId = testId
            };
            AllureLifecycle.Instance.StartTestCase(testResult);
        }

        internal static string MakeGoodErrorMsg(string errorMsg)
        {
            if (string.IsNullOrEmpty(errorMsg)) return errorMsg;
            var index = errorMsg.IndexOf("Multiple", StringComparison.Ordinal);
            if (index == -1 || index == 0) return errorMsg;
            var goodMsg = errorMsg.Substring(0, index) + " \r\n" + errorMsg.Substring(index);
            return goodMsg;
        }
    }
}