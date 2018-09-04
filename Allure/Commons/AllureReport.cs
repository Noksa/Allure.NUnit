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
using NUnit.Framework.Internal;
using TestResult = Allure.Commons.Model.TestResult;

namespace Allure.Commons
{
    [AllureFixture]
    public abstract class AllureReport
    {
        [ThreadStatic] private static string _testUuid;

        [ThreadStatic] internal static IList<ITest> AllTestsCurrentSuite;

        [SetUp]
        protected void StartAllureLogging()
        {
            StartAllureLogging(TestContext.CurrentContext.Test.Name, TestContext.CurrentContext.Test.FullName, TestContext.CurrentContext.Test.ClassName, TestContext.CurrentContext.Test.MethodName);
        }

        

        [TearDown]
        protected void StopAllureLogging()
        {
            StopAllureLogging(TestExecutionContext.CurrentContext.CurrentTest.Method.MethodInfo, TestContext.CurrentContext.Result.Message, TestContext.CurrentContext.Result.StackTrace, TestContext.CurrentContext.Result.Outcome.Status);
        }

        [OneTimeTearDown]
        public void OneTimeTearDown()
        {
            if (TestContext.CurrentContext.Result.Outcome.Site == FailureSite.SetUp && Config.AllowEmptySuites)
            {
                var fixture = new TestResultContainer
                {
                    uuid = TestContext.CurrentContext.Test.ID,
                    name = TestContext.CurrentContext.Test.ClassName
                };
                AllureLifecycle.Instance.StartTestContainer(fixture);
                foreach (var test in AllTestsCurrentSuite)
                {
                    StartAllureLogging(test.Name, test.FullName, test.ClassName, test.MethodName);
                    var uuid = $"{Guid.NewGuid():N}";
                    AllureLifecycle.Instance.StartStep("The test was not started", uuid);
                    AllureLifecycle.Instance.UpdateStep(q =>
                    {
                        q.status = Status.failed;
                        q.stop = AllureLifecycle.ToUnixTimestamp();
                    });
                    StopAllureLogging(test.Method.MethodInfo, TestContext.CurrentContext.Result.Message, TestContext.CurrentContext.Result.StackTrace, TestContext.CurrentContext.Result.Outcome.Status);
                }

                AllureLifecycle.Instance.WriteTestContainer(TestContext.CurrentContext.Test.ID);
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

                AddAttrInfoToTestCaseFromAttributes(testClassAttrs);
            }

            var attrs = testMethod.GetCustomAttributes().ToList();

            var defects = attrs.Where(_ => _ is AllureIssueAttribute).Cast<AllureIssueAttribute>().Count();
            AddAttrInfoToTestCaseFromAttributes(attrs);
            if (defects != 0)
                AllureLifecycle.Instance.UpdateTestCase(_ =>
                {
                    var subSuites = _.labels.Where(lbl => lbl.name.ToLower().Equals("subsuite")).ToList();
                    subSuites.ForEach(lbl => _.labels.Remove(lbl));
                    _.labels.Add(Label.SubSuite("With defects"));
                });
        }

       
        

        protected string MakeGoodErrorMsg(string errorMsg)
        {
            if (string.IsNullOrEmpty(errorMsg)) return errorMsg;
            var index = errorMsg.IndexOf("Multiple", StringComparison.Ordinal);
            if (index == -1 || index == 0) return errorMsg;
            var goodMsg = errorMsg.Substring(0, index) + " \r\n" + errorMsg.Substring(index);
            return goodMsg;
        }

        #region Privates

        private void StopAllureLogging(MethodInfo methodInfo, string resultMsg, string stackTrace, TestStatus status)
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
            AllureLifecycle.Instance.WriteTestCase(_testUuid);
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

        private static void AddAttrInfoToTestCaseFromAttributes(IEnumerable<Attribute> attrs)
        {
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
                }
        }

        private void StartAllureLogging(string testName, string testFullName, string testClassName, string methodName)
        {
            AllureStorage.MainThreadId = Thread.CurrentThread.ManagedThreadId;
            _testUuid = $"{TestContext.CurrentContext.Test.ID}_{Guid.NewGuid():N}";
            var testResult = new TestResult
            {

                uuid = _testUuid,
                name = testName,
                fullName = testFullName,
                labels = new List<Label>
                {
                    Label.Thread(),
                    Label.Host(),
                    Label.TestClass(testClassName),
                    Label.TestMethod(methodName),
                    Label.Package(testClassName),
                },
                historyId = testName
            };
            AllureLifecycle.Instance.StartTestCase(testResult);
        }

        #endregion

        internal class Config
        {
            internal static bool AllowEmptySuites { get; set; }
        }

    }
}