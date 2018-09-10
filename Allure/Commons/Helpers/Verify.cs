using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Allure.Commons.Model;
using NUnit.Framework;
using NUnit.Framework.Constraints;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace Allure.Commons.Helpers
{
    public class Verify
    {
        #region Assert.Fail

        public void Fail(string stepName)
        {
            VerifyRunner(stepName, () => Assert.Fail(stepName));
        }

        #endregion

        private Action _actionInException;
        [ThreadStatic] private static Action _localActionInException;

        #region Help members

        private static readonly ThreadLocal<List<Exception>>
            AssertsListThreadLocal = new ThreadLocal<List<Exception>>();

        public static List<Exception> CurrentTestAsserts =>
            AssertsListThreadLocal.Value ?? (AssertsListThreadLocal.Value = new List<Exception>());

        public Verify SetGlobalActionInException(Action action)
        {
            _actionInException = action;
            return this;
        }

        public Verify SetCurrentTestActionInException(Action action)
        {
            _localActionInException = action;
            return this;
        }

        private bool VerifyRunner(string stepName, Action action)
        {
            bool result;
            var assertsBefore = TestContext.CurrentContext.Result.Assertions.Count();
            var uuid = $"{Guid.NewGuid():N}";
            AllureLifecycle.Instance.StartStep(stepName, uuid);
            try
            {
                action.Invoke();
                AllureLifecycle.Instance.UpdateStep(uuid, _ => _.status = Status.passed);
                result = true;
            }
            catch (Exception e)
            {
                result = false;
                AllureLifecycle.Instance.MakeStepWithExMessage(assertsBefore, stepName, e);
                _actionInException?.Invoke();
                _localActionInException?.Invoke();
                CurrentTestAsserts.Add(e);
                AllureLifecycle.Instance.UpdateStep(uuid, _ => _.status = Status.failed);
            }

            AllureLifecycle.Instance.StopStep(uuid);
            return result;
        }

        private void ReportFailure(ConstraintResult result, string message)
        {
            ReportFailure(result, message, null);
        }

        private void ReportFailure(ConstraintResult result, string message, params object[] args)
        {
            MessageWriter writer = new TextMessageWriter(message, args);
            result.WriteMessageTo(writer);

            ReportFailure(writer.ToString());
        }

        private void ReportFailure(string message)
        {
            // Record the failure in an <assertion> element
            var result = TestExecutionContext.CurrentContext.CurrentResult;
            result.RecordAssertion(AssertionStatus.Failed, message, GetStackTrace());
            result.RecordTestCompletion();
            StackFilter.DefaultFilter.Filter(SystemEnvironmentFilter.Filter(Environment.StackTrace));
            throw new AssertionException(result.Message);
        }

        private readonly StackFilter SystemEnvironmentFilter = new StackFilter(@" System\.Environment\.");

        private string GetStackTrace()
        {
            return StackFilter.DefaultFilter.Filter(SystemEnvironmentFilter.Filter(Environment.StackTrace));
        }

        internal static void Clear()
        {
            CurrentTestAsserts.Clear();
        }

        #endregion

        #region Assert.That

        #region Boolean

        /// <summary>
        ///     Asserts that a condition is true. If the condition is false the method throws
        ///     an <see cref="AssertionException" />.
        /// </summary>
        /// <param name="stepName">The Step Name to Allure report</param>
        /// <param name="condition">The evaluated condition</param>
        /// <param name="message">The message to display if the condition is false</param>
        /// <param name="args">Arguments to be used in formatting the message</param>
        public bool That(string stepName, bool condition, string message, params object[] args)
        {
            return VerifyRunner(stepName, () => Assert.That(condition, Is.True, message, args));
        }

        /// <summary>
        ///     Asserts that a condition is true. If the condition is false the method throws
        ///     an <see cref="AssertionException" />.
        /// </summary>
        /// <param name="stepName">The Step Name to Allure report</param>
        /// <param name="condition">The evaluated condition</param>
        public bool That(string stepName, bool condition)
        {
            return VerifyRunner(stepName, () => Assert.That(condition, stepName));
        }

#if !NET20
        /// <summary>
        ///     Asserts that a condition is true. If the condition is false the method throws
        ///     an <see cref="AssertionException" />.
        /// </summary>
        /// <param name="stepName">The Step Name to Allure report</param>
        /// <param name="condition">The evaluated condition</param>
        /// <param name="getExceptionMessage">A function to build the message included with the Exception</param>
        public bool That(string stepName, bool condition, Func<string> getExceptionMessage)
        {
            return VerifyRunner(stepName, () => Assert.That(condition, Is.True, getExceptionMessage));
        }
#endif

        #endregion

        #region Lambda returning Boolean

#if !NET20
        /// <summary>
        ///     Asserts that a condition is true. If the condition is false the method throws
        ///     an <see cref="AssertionException" />.
        /// </summary>
        /// <param name="stepName">The Step Name to Allure report</param>
        /// <param name="condition">A lambda that returns a Boolean</param>
        /// <param name="message">The message to display if the condition is false</param>
        /// <param name="args">Arguments to be used in formatting the message</param>
        public bool That(string stepName, Func<bool> condition, string message, params object[] args)
        {
            return That(stepName, condition.Invoke(), Is.True, message, args);
        }

        /// <summary>
        ///     Asserts that a condition is true. If the condition is false the method throws
        ///     an <see cref="AssertionException" />.
        /// </summary>
        /// <param name="stepName">The Step Name to Allure report</param>
        /// <param name="condition">A lambda that returns a Boolean</param>
        public bool That(string stepName, Func<bool> condition)
        {
            return That(stepName, condition.Invoke(), Is.True, null, null);
        }

        /// <summary>
        ///     Asserts that a condition is true. If the condition is false the method throws
        ///     an <see cref="AssertionException" />.
        /// </summary>
        /// <param name="stepName">The Step Name to Allure report</param>
        /// <param name="condition">A lambda that returns a Boolean</param>
        /// <param name="getExceptionMessage">A function to build the message included with the Exception</param>
        public bool That(string stepName, Func<bool> condition, Func<string> getExceptionMessage)
        {
            return VerifyRunner(stepName, () => Assert.That(condition.Invoke(), Is.True, getExceptionMessage));
        }
#endif

        #endregion

        #region ActualValueDelegate

        /// <summary>
        ///     Apply a constraint to an actual value, succeeding if the constraint
        ///     is satisfied and throwing an assertion exception on failure.
        /// </summary>
        /// <param name="stepName">The Step Name to Allure report</param>
        /// <typeparam name="TActual">The Type being compared.</typeparam>
        /// <param name="del">An ActualValueDelegate returning the value to be tested</param>
        /// <param name="expr">A Constraint expression to be applied</param>
        public bool That<TActual>(string stepName, ActualValueDelegate<TActual> del, IResolveConstraint expr)
        {
            return VerifyRunner(stepName, () => Assert.That(del, expr.Resolve(), null, null));
        }

        /// <summary>
        ///     Apply a constraint to an actual value, succeeding if the constraint
        ///     is satisfied and throwing an assertion exception on failure.
        /// </summary>
        /// <param name="stepName">The Step Name to Allure report</param>
        /// <typeparam name="TActual">The Type being compared.</typeparam>
        /// <param name="del">An ActualValueDelegate returning the value to be tested</param>
        /// <param name="expr">A Constraint expression to be applied</param>
        /// <param name="message">The message that will be displayed on failure</param>
        /// <param name="args">Arguments to be used in formatting the message</param>
        public bool That<TActual>(string stepName, ActualValueDelegate<TActual> del, IResolveConstraint expr,
            string message, params object[] args)
        {
            return VerifyRunner(stepName, () =>
            {
                var constraint = expr.Resolve();
                TestExecutionContext.CurrentContext.IncrementAssertCount();
                var result = constraint.ApplyTo(del);
                if (!result.IsSuccess)
                    ReportFailure(result, message, args);
            });
        }

#if !NET20
        /// <summary>
        ///     Apply a constraint to an actual value, succeeding if the constraint
        ///     is satisfied and throwing an assertion exception on failure.
        /// </summary>
        /// <param name="stepName">The Step Name to Allure report</param>
        /// <typeparam name="TActual">The Type being compared.</typeparam>
        /// <param name="del">An ActualValueDelegate returning the value to be tested</param>
        /// <param name="expr">A Constraint expression to be applied</param>
        /// <param name="getExceptionMessage">A function to build the message included with the Exception</param>
        public bool That<TActual>(string stepName,
            ActualValueDelegate<TActual> del,
            IResolveConstraint expr,
            Func<string> getExceptionMessage)
        {
            return VerifyRunner(stepName, () =>
            {
                var constraint = expr.Resolve();
                TestExecutionContext.CurrentContext.IncrementAssertCount();
                var result = constraint.ApplyTo(del);
                if (!result.IsSuccess)
                    ReportFailure(result, getExceptionMessage());
            });
        }
#endif

        #endregion

        #region TestDelegate

        /// <summary>
        ///     Asserts that the code represented by a delegate throws an exception
        ///     that satisfies the constraint provided.
        /// </summary>
        /// <param name="stepName">The Step Name to Allure report</param>
        /// <param name="code">A TestDelegate to be executed</param>
        /// <param name="constraint">A ThrowsConstraint used in the test</param>
        public bool That(string stepName, TestDelegate code, IResolveConstraint constraint)
        {
            return VerifyRunner(stepName, () => Assert.That(code, constraint, null, null));
        }

        /// <summary>
        ///     Asserts that the code represented by a delegate throws an exception
        ///     that satisfies the constraint provided.
        /// </summary>
        /// <param name="stepName">The Step Name to Allure report</param>
        /// <param name="code">A TestDelegate to be executed</param>
        /// <param name="constraint">A ThrowsConstraint used in the test</param>
        /// <param name="message">The message that will be displayed on failure</param>
        /// <param name="args">Arguments to be used in formatting the message</param>
        public bool That(string stepName, TestDelegate code, IResolveConstraint constraint, string message,
            params object[] args)
        {
            return VerifyRunner(stepName, () => Assert.That((object)code, constraint, message, args));
        }

#if !NET20
        /// <summary>
        ///     Asserts that the code represented by a delegate throws an exception
        ///     that satisfies the constraint provided.
        /// </summary>
        /// <param name="stepName">The Step Name to Allure report</param>
        /// <param name="code">A TestDelegate to be executed</param>
        /// <param name="constraint">A ThrowsConstraint used in the test</param>
        /// <param name="getExceptionMessage">A function to build the message included with the Exception</param>
        public bool That(string stepName, TestDelegate code, IResolveConstraint constraint,
            Func<string> getExceptionMessage)
        {
            return VerifyRunner(stepName, () => Assert.That((object)code, constraint, getExceptionMessage));
        }
#endif

        #endregion

        #endregion

        #region Assert.That<TActual>

        /// <summary>
        ///     Apply a constraint to an actual value, succeeding if the constraint
        ///     is satisfied and throwing an assertion exception on failure.
        /// </summary>
        /// <param name="stepName">The Step Name to Allure report</param>
        /// <typeparam name="TActual">The Type being compared.</typeparam>
        /// <param name="actual">The actual value to test</param>
        /// <param name="expression">A Constraint to be applied</param>
        public bool That<TActual>(string stepName, TActual actual, IResolveConstraint expression)
        {
            return VerifyRunner(stepName, () => Assert.That(actual, expression, stepName, null));
        }

        /// <summary>
        ///     Apply a constraint to an actual value, succeeding if the constraint
        ///     is satisfied and throwing an assertion exception on failure.
        /// </summary>
        /// <param name="stepName">The Step Name to Allure report</param>
        /// <typeparam name="TActual">The Type being compared.</typeparam>
        /// <param name="actual">The actual value to test</param>
        /// <param name="expression">A Constraint expression to be applied</param>
        /// <param name="message">The message that will be displayed on failure</param>
        /// <param name="args">Arguments to be used in formatting the message</param>
        public bool That<TActual>(string stepName, TActual actual, IResolveConstraint expression, string message,
            params object[] args)
        {
            return VerifyRunner(stepName, () =>
            {
                var constraint = expression.Resolve();

                TestExecutionContext.CurrentContext.IncrementAssertCount();
                var result = constraint.ApplyTo(actual);
                if (!result.IsSuccess)
                    ReportFailure(result, message, args);
            });
        }

#if !NET20
        /// <summary>
        ///     Apply a constraint to an actual value, succeeding if the constraint
        ///     is satisfied and throwing an assertion exception on failure.
        /// </summary>
        /// <param name="stepName">The Step Name to Allure report</param>
        /// <typeparam name="TActual">The Type being compared.</typeparam>
        /// <param name="actual">The actual value to test</param>
        /// <param name="expression">A Constraint expression to be applied</param>
        /// <param name="getExceptionMessage">A function to build the message included with the Exception</param>
        public bool That<TActual>(string stepName,
            TActual actual,
            IResolveConstraint expression,
            Func<string> getExceptionMessage)
        {
            return VerifyRunner(stepName, () =>
            {
                var constraint = expression.Resolve();

                TestExecutionContext.CurrentContext.IncrementAssertCount();
                var result = constraint.ApplyTo(actual);
                if (!result.IsSuccess)
                    ReportFailure(result, getExceptionMessage());
            });
        }
#endif

        #endregion

        #region Assert.ByVal

        /// <summary>
        ///     Apply a constraint to an actual value, succeeding if the constraint
        ///     is satisfied and throwing an assertion exception on failure.
        ///     Used as a synonym for That in rare cases where a private setter
        ///     causes a Visual Basic compilation error.
        /// </summary>
        /// <param name="stepName">The Step Name to Allure report</param>
        /// <param name="actual">The actual value to test</param>
        /// <param name="expression">A Constraint to be applied</param>
        public bool ByVal(string stepName, object actual, IResolveConstraint expression)
        {
            return VerifyRunner(stepName, () => Assert.That(actual, expression, null, null));
        }

        /// <summary>
        ///     Apply a constraint to an actual value, succeeding if the constraint
        ///     is satisfied and throwing an assertion exception on failure.
        ///     Used as a synonym for That in rare cases where a private setter
        ///     causes a Visual Basic compilation error.
        /// </summary>
        /// <remarks>
        ///     This method is provided for use by VB developers needing to test
        ///     the value of properties with private setters.
        /// </remarks>
        /// <param name="stepName">The Step Name to Allure report</param>
        /// <param name="actual">The actual value to test</param>
        /// <param name="expression">A Constraint expression to be applied</param>
        /// <param name="message">The message that will be displayed on failure</param>
        /// <param name="args">Arguments to be used in formatting the message</param>
        public bool ByVal(string stepName, object actual, IResolveConstraint expression, string message,
            params object[] args)
        {
            return VerifyRunner(stepName, () => Assert.That(actual, expression, message, args));
        }

        #endregion
    }
}