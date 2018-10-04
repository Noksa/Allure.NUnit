using Allure.Commons;
using Allure.NUnit.Attributes;
using NUnit.Framework;

namespace TestsSamples.SamplesWithBaseClass
{
    [AllureSuite("Tests samples with base class")]
    public class TestsSamplesWithBaseClass : TestsBaseClass
    {

        [Test]
        public void JustTest()
        {
            var arg1 = 3;
            var arg2 = 4;
            var sum = arg1 + arg2;
            AllureLifecycle.Instance.Verify.That($"{arg1}+{arg2} should be {sum}", arg1 + arg2, Is.EqualTo(sum));
        }
    }
}
