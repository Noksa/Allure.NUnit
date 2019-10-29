using Allure.Commons;
using Allure.Commons.Helpers;
using Allure.Commons.Model;
using TechTalk.SpecFlow;

namespace AllureSpecFlow
{
    [Binding]
    public class AllureBindings
    {
        private readonly FeatureContext _featureContext;
        private readonly ScenarioContext _scenarioContext;

        public AllureBindings(FeatureContext featureContext, ScenarioContext scenarioContext)
        {
            _featureContext = featureContext;
            _scenarioContext = scenarioContext;
        }

        [BeforeFeature(Order = int.MinValue)]
        public static void FirstBeforeFeature()
        {
            // start feature container in BindingInvoker
        }

        [AfterFeature(Order = int.MaxValue)]
        public static void LastAfterFeature()
        {
            // write feature container in BindingInvoker
        }

        [BeforeScenario(Order = int.MinValue)]
        public void FirstBeforeScenario()
        {
            PluginHelper.StartTestContainer(_featureContext, _scenarioContext);
            //AllureHelper.StartTestCase(scenarioContainer.uuid, featureContext, scenarioContext);
        }

        [BeforeScenario(Order = int.MaxValue)]
        public void LastBeforeScenario()
        {
            // start scenario after last fixture and before the first step to have valid current step context in allure storage
            var scenarioContainer = PluginHelper.GetCurrentTestConainer(_scenarioContext);
            PluginHelper.StartTestCase(scenarioContainer.uuid, _featureContext, _scenarioContext);
        }

        [AfterScenario(Order = int.MinValue)]
        public void FirstAfterScenario()
        {
            var scenarioId = PluginHelper.GetCurrentTestCase(_scenarioContext).uuid;

            // update status to passed if there were no step of binding failures
            AllureLifecycle.Instance
                .UpdateTestCase(scenarioId,
                    x => x.status = x.status != Status.none ? x.status : Status.passed)
                .StopTestCase(scenarioId, true);
            AllureLifecycle.CurrentTestActionsInException = null;
        }

        [AfterTestRun]
        public static void AfterAllTests()
        {
            AllureLifecycle.GlobalActionsInException = null;
            AllureLifecycle.CurrentTestActionsInException = null;
        }

        [BeforeStep]
        public void BeforeStep()
        {
            var args = _scenarioContext?.StepContext?.StepInfo?.BindingMatch?.Arguments;
            if (args != null && args.Length > 0) ReportHelper.AddStepParameters(args);
        }
    }
}