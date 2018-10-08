using System.Threading;
using System.Threading.Tasks;
using Allure.Commons;
using Allure.NUnit.Attributes;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace TestsSamples
{
    [NonParallelizable]
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

        [TestCase(TestName = "Debug tests0")]
        [TestCase(TestName = "Debug tests1")]
        [TestCase(TestName = "Debug tests2")]
        [TestCase(TestName = "Debug tests3")]
        [TestCase(TestName = "Debug tests4")]
        [TestCase(TestName = "Debug tests5")]
        [TestCase(TestName = "Debug tests6")]
        [TestCase(TestName = "Debug tests7")]
        [TestCase(TestName = "Debug tests8")]
        [TestCase(TestName = "Debug tests9")]
        [TestCase(TestName = "Debug tests10")]
        [TestCase(TestName = "Debug tests11")]
        [TestCase(TestName = "Debug tests12")]
        public void DebuggingTests()
        {
            var task = Task.Run(() => { TestDebug(); }, new CancellationToken(false));
            Task.WaitAll(task);
            AllureLifecycle.Instance.Verify.Pass("Test Body step");
        }

        private void TestDebug()
        {
            AllureLifecycle.Instance.RunStep($"This is step {TestContext.CurrentContext.Random.Next(1, 5000)}, Test is {TestExecutionContext.CurrentContext.CurrentTest.Id}", () =>
            {
                AllureLifecycle.Instance.Verify.Pass("Task step");
                Thread.Sleep(1000);
            });
        }
    }
}