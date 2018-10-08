using System.Threading;
using System.Threading.Tasks;
using Allure.Commons;
using Allure.NUnit.Attributes;
using NUnit.Framework;

namespace TestsSamples
{
    [Parallelizable(ParallelScope.All)]
    [AllureSuite("This is debug suite")]
    public class Debugging : AllureReport
    {
        [OneTimeSetUp]
        public void TestOneTime()
        {
            AllureLifecycle.Instance.Verify.Pass("OneTimeSetup step");
        }

        [SetUp]
        public void SetupTest()
        {
            AllureLifecycle.Instance.Verify.Pass("Test setup step");
        }

        [TearDown]
        public void TearDownTest()
        {
            AllureLifecycle.Instance.Verify.Pass("Test teardown step");
        }

        [OneTimeTearDown]
        public void OneTimeTearDowning()
        {

            AllureLifecycle.Instance.Verify.Pass("Test OneTimeTearDown step");
        }

        [TestCase(TestName = "Debug tests")]
        public void DebuggingTests()
        {
            var task = Task.Run(() =>
            {
                TestDebug();
                TestDebug();
                TestDebug();
                TestDebug();
                TestDebug();
                TestDebug();
                TestDebug();
                TestDebug();
                TestDebug();
                TestDebug();
                TestDebug();
                TestDebug();
                TestDebug();
                TestDebug();
                TestDebug();
                TestDebug();
            });
            task.Wait();
            AllureLifecycle.Instance.Verify.Pass("Test Body step");

        }

        private void TestDebug()
        {
            AllureLifecycle.Instance.RunStep($"This is step {TestContext.CurrentContext.Random.Next(1, 5000)}", () =>
            {
                AllureLifecycle.Instance.Verify.Pass("Task step");
                Thread.Sleep(1000);
            });
        }
    }
}