using System.IO;
using System.Reflection;
using Allure.Commons;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace TestsSamples.SamplesWithBaseClass
{
    public abstract class TestsBaseClass : AllureReport
    {

        private static string AllureConfigDir = Path.GetDirectoryName(typeof(AllureLifecycle).Assembly.Location);

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
