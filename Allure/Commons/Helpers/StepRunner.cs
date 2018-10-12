using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Allure.Commons.Model;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Allure.Commons.Helpers
{
    internal static class StepRunner
    {
        internal static TResult Run<TResult>(string stepName, Delegate del, bool throwEx,
            Status stepStatusIfFailed,
            params object[] stepParams)
        {
            var stepStatus = Status.passed;
            var assertsBefore = TestContext.CurrentContext.Result.Assertions.Count();
            Exception throwedEx = null;
            var resultFunc = default(TResult);
            var uuid = $"{Guid.NewGuid():N}";
            var stepResult = new StepResult
            {
                name = stepName,
                start = AllureLifecycle.ToUnixTimestamp(DateTimeOffset.Now)
            };
            AllureLifecycle.Instance.StartStep(uuid, stepResult);

            if (AllureLifecycle.Instance.Config.Allure.EnableParameters)
                for (var i = 0; i < stepParams.Length; i++)
                {
                    var strArg = stepParams[i].ToString();
                    var param = new Parameter
                    {
                        name = $"Parameter #{i + 1}, {stepParams[i].GetType().Name}",
                        value = strArg
                    };
                    AllureLifecycle.Instance.UpdateStep(uuid, q => q.parameters.Add(param));
                }

            try
            {
                switch (del)
                {
                    case Action action when resultFunc is bool:
                        action.Invoke();
                        resultFunc = (TResult)(object)true;
                        break;
                    case Func<TResult> func:
                        resultFunc = func.Invoke();
                        break;
                    default:
                        resultFunc = (TResult)del.DynamicInvoke();
                        break;
                }

                if (assertsBefore != TestContext.CurrentContext.Result.Assertions.Count())
                    stepStatus = stepStatusIfFailed;
            }
            catch (Exception e)
            {
                if (e is TargetInvocationException)
                    throwedEx = GetInnerExceptions(e)
                        .First(q => q.GetType() != typeof(TargetInvocationException));
                else
                    throwedEx = e;

                if (throwedEx is InconclusiveException)
                    stepStatus = Status.skipped;

                else if (throwedEx is SuccessException)
                    throwEx = false; // no throw ex, because assert.pass
                else
                    stepStatus = stepStatusIfFailed;

                var list =
                    TestExecutionContext.CurrentContext.CurrentTest.GetProp(AllureConstants.TestAsserts) as
                        List<Exception>;
                list?.Add(throwedEx);
                if (!throwedEx.Data.Contains("Rethrow"))
                {
                    throwedEx.Data.Add("Rethrow", true);
                    AllureLifecycle.Instance.MakeStepWithExMessage(assertsBefore, stepName, throwedEx, stepStatusIfFailed);
                    AllureLifecycle.CurrentTestActionsInException?.ForEach(action => action.Invoke());
                    AllureLifecycle.GlobalActionsInException?.ForEach(action => action.Invoke());
                }
            }
            finally
            {
                AllureLifecycle.Instance.UpdateStep(step => step.status = stepStatus);
                AllureLifecycle.Instance.StopStep(uuid);
            }

            if (throwEx && throwedEx != null) throw throwedEx;
            return resultFunc;
        }

        internal static string GetCallerMethodName()
        {
            var frame = new StackFrame(2);
            var method = frame.GetMethod();
            return method.Name;
        }
        internal static IEnumerable<Exception> GetInnerExceptions(Exception ex)
        {
            if (ex == null) throw new ArgumentNullException(nameof(ex));

            if (ex.InnerException == null) yield break;
            var innerException = ex.InnerException;
            do
            {
                yield return innerException;
                innerException = innerException.InnerException;
            } while (innerException != null);
        }
    }
}
