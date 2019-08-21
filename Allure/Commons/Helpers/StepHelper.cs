using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Allure.Commons.Model;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Allure.Commons.Helpers
{
    internal class StepHelper
    {
        #region Private fields

        private readonly string _stepText;

        #endregion

        #region Ctor

        internal StepHelper(string stepText)
        {
            _stepText = stepText;
            AssertsInTestBeforeStep = GetCurrentAsserts();
        }

        #endregion

        #region Private properties

        private IEnumerable<AssertionResult> AssertsInTestBeforeStep { get; }

        #endregion


        #region Internal methods

        internal Status GetStepStatus()
        {
            var currentAsserts = GetCurrentAsserts().ToList();

            if (AssertsInTestBeforeStep.Count() == currentAsserts.Count) return Status.passed;

            var onlyNewAsserts = currentAsserts.Except(AssertsInTestBeforeStep);

            if (onlyNewAsserts.All(assert =>
                assert.Status != AssertionStatus.Error && assert.Status != AssertionStatus.Failed))
                return Status.broken;

            return Status.failed;
        }

        internal Status ProceedException(Exception e, out bool throwEx, Status defaultStepStatus = Status.failed)
        {
            throwEx = true;
            var stepStatus = defaultStepStatus;
            Exception throwedEx;
            if (e is TargetInvocationException)
                throwedEx = AllureLifecycle.GetInnerExceptions(e)
                    .First(q => q.GetType() != typeof(TargetInvocationException));
            else
                throwedEx = e;

            if (throwedEx is InconclusiveException)
                stepStatus = Status.skipped;

            else if (throwedEx is SuccessException)
                throwEx = false; // no throw ex, because assert.pass

            var list =
                TestContext.CurrentContext.Test.Properties.Get(AllureConstants.TestAsserts) as List<Exception>;
            list?.Add(throwedEx);
            if (!throwedEx.Data.Contains("Rethrow"))
            {
                throwedEx.Data.Add("Rethrow", true);
                AllureLifecycle.Instance.MakeStepWithExMessage(AssertsInTestBeforeStep.Count(), _stepText, throwedEx, defaultStepStatus);
                AllureLifecycle.CurrentTestActionsInException?.ForEach(action => action.Invoke());
                AllureLifecycle.GlobalActionsInException?.ForEach(action => action.Invoke());
            }

            return stepStatus;
        }

        #endregion

        #region Private methods

        private IEnumerable<AssertionResult> GetCurrentAsserts()
        {
            return TestContext.CurrentContext.Result.Assertions.ToList();
        }

        #endregion
    }
}
