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
        [SetUp]
        protected void StartAllureLogging()
        {
            AllureStorage.MainThreadId = Thread.CurrentThread.ManagedThreadId;
            var testResult = new TestResult
            {
                uuid = TestContext.CurrentContext.Test.ID,
                name = TestContext.CurrentContext.Test.Name,
                fullName = TestContext.CurrentContext.Test.FullName,
                labels = new List<Label>
                {
                    Label.Thread(),
                    Label.Host(),
                    Label.TestClass(TestContext.CurrentContext.Test.ClassName),
                    Label.TestMethod(TestContext.CurrentContext.Test.MethodName),
                    Label.Package(TestContext.CurrentContext.Test.ClassName)
                }
            };
            AllureLifecycle.Instance.StartTestCase(testResult);
        }

        [TearDown]
        protected void StopAllureLogging()
        {
            AddInfoInTestCase(TestExecutionContext.CurrentContext.CurrentTest.Method.MethodInfo);
            AllureLifecycle.Instance.UpdateTestCase(x =>
            {
                x.statusDetails = new StatusDetails
                {
                    message = MakeGoodErrorMsg(TestContext.CurrentContext.Result.Message),
                    trace = TestContext.CurrentContext.Result.StackTrace
                };
            });
            AllureLifecycle.Instance.StopTestCase(x =>
                x.status = GetNunitStatus(TestContext.CurrentContext.Result.Outcome.Status));
            AllureLifecycle.Instance.WriteTestCase(TestContext.CurrentContext.Test.ID);
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

        protected string MakeGoodErrorMsg(string errorMsg)
        {
            if (string.IsNullOrEmpty(errorMsg)) return errorMsg;
            var index = errorMsg.IndexOf("Multiple", StringComparison.Ordinal);
            if (index == -1 || index == 0) return errorMsg;
            var goodMsg = errorMsg.Substring(0, index) + " \r\n" + errorMsg.Substring(index);
            return goodMsg;
        }
    }
}