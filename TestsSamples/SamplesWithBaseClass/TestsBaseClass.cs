using Allure.Commons;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace TestsSamples.SamplesWithBaseClass
{
    public abstract class TestsBaseClass : AllureReport
    {
        [SetUp]
        public void SetupTest()
        {
            AllureLifecycle.Instance.RunStep(() =>
            {
                TestContext.Progress.WriteLine(
                    $"Test \"{TestExecutionContext.CurrentContext.CurrentTest.FullName}\" is starting...");
            });
        }

        [TearDown]
        public void TearDownTest()
        {
            AllureLifecycle.Instance.RunStep(() =>
            {
                TestContext.Progress.WriteLine(
                    $"Test {TestExecutionContext.CurrentContext.CurrentTest.FullName}\" is stopping...");
            });
        }


    }
}
