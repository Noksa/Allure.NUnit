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
        #region Ctor

        internal StepHelper(string stepText)
        {
            _stepText = stepText;
            _assertsInTestBeforeStep = GetCurrentAsserts();
        }

        #endregion

        #region Private methods

        private static List<AssertionResult> GetCurrentAsserts()
        {
            return TestContext.CurrentContext.Result.Assertions.ToList();
        }

        #endregion

        #region Private fields

        private readonly string _stepText;
        private readonly List<AssertionResult> _assertsInTestBeforeStep;
        private const string Rethrow = "Rethrow";

        #endregion

        #region Internal methods

        internal Status GetStepStatus()
        {
            var currentAsserts = GetCurrentAsserts();

            if (_assertsInTestBeforeStep.Count == currentAsserts.Count) return Status.passed;

            var onlyNewAsserts = currentAsserts.Except(_assertsInTestBeforeStep);

            return onlyNewAsserts.All(assert =>
                assert.Status != AssertionStatus.Error && assert.Status != AssertionStatus.Failed)
                ? Status.broken
                : Status.failed;
        }

        internal (Status StepStatus, Exception ThrowedEx, bool NeedRethrow) ProceedException(Exception e, Status defaultStepStatus = Status.failed)
        {
            var throwEx = true;
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
            if (!throwedEx.Data.Contains(Rethrow))
            {
                throwedEx.Data.Add(Rethrow, true);
                AllureLifecycle.Instance.MakeStepWithExMessage(_assertsInTestBeforeStep.Count, _stepText, throwedEx,
                    defaultStepStatus);
                AllureLifecycle.CurrentTestActionsInException?.ForEach(action => action.Invoke());
                AllureLifecycle.GlobalActionsInException?.ForEach(action => action.Invoke());
            }

            return (stepStatus, throwedEx, throwEx);
        }

        #endregion
    }
}