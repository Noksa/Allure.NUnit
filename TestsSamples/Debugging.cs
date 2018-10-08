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

        [TestCase(TestName = "Debug tests")]
        [Repeat(5)]
        public void DebuggingTests()
        {
            var task = Task.Run(() =>
            {
                LogThreadAndTestId();
            });
            var task2 = Task.Run(() =>
            {
                LogThreadAndTestId();
            });
            var task3 = Task.Run(() =>
            {
                LogThreadAndTestId();
            });

            AllureLifecycle.Instance.Verify.Pass("Test Body step");

        }

        private void LogThreadAndTestId()
        {
            var attempt = 1;
            do
            {
                TestContext.Progress.WriteLine($"Thread id \"{Thread.CurrentThread.ManagedThreadId}\", Test Id: {TestContext.CurrentContext.Test.ID}");
                Thread.Sleep(500);
                attempt++;
            } while (attempt < 5);
            
        }
    }
}