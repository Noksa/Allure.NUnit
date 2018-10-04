using System;
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

        public Verify Fail(string stepName, params object[] stepParams)
        {
            VerifyRunner(stepName, () => Assert.Fail(stepName), Status.failed, stepParams);
            return this;
        }

        public Verify Warn(string stepName, params object[] stepParams)
        {
            VerifyRunner(stepName, () => Assert.Warn(stepName), Status.broken, stepParams);
            return this;
        }

        public Verify Pass(string stepName, params object[] stepParams)
        {
            VerifyRunner(stepName, () => { }, Status.failed, stepParams);
            return this;
        }

        #endregion

        #region Help members

        private bool VerifyRunner(string stepName, Action action, Status stepStatusIfFailed, params object[] stepParams)
        {
            var result = AllureLifecycle.StepRunner<bool>(stepName, action, false, stepStatusIfFailed, stepParams);
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
            return VerifyRunner(stepName, () => Assert.That(condition, Is.True, message, args), Status.failed);
        }

        /// <summary>
        ///     Asserts that a condition is true. If the condition is false the method throws
        ///     an <see cref="AssertionException" />.
        /// </summary>
        /// <param name="stepName">The Step Name to Allure report</param>
        /// <param name="condition">The evaluated condition</param>
        public bool That(string stepName, bool condition, params object[] stepParams)
        {
            return VerifyRunner(stepName, () => Assert.That(condition, stepName), Status.failed, stepParams);
        }

#if !NET20
        /// <summary>
        ///     Asserts that a condition is true. If the condition is false the method throws
        ///     an <see cref="AssertionException" />.
        /// </summary>
        /// <param name="stepName">The Step Name to Allure report</param>
        /// <param name="condition">The evaluated condition</param>
        /// <param name="getExceptionMessage">A function to build the message included with the Exception</param>
        public bool That(string stepName, bool condition, Func<string> getExceptionMessage, params object[] stepParams)
        {
            return VerifyRunner(stepName, () => Assert.That(condition, Is.True, getExceptionMessage), Status.failed,
                stepParams);
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
        public bool That(string stepName, Func<bool> condition, params object[] stepParams)
        {
            return That(stepName, condition.Invoke(), Is.True, stepParams);
        }

        /// <summary>
        ///     Asserts that a condition is true. If the condition is false the method throws
        ///     an <see cref="AssertionException" />.
        /// </summary>
        /// <param name="stepName">The Step Name to Allure report</param>
        /// <param name="condition">A lambda that returns a Boolean</param>
        /// <param name="getExceptionMessage">A function to build the message included with the Exception</param>
        public bool That(string stepName, Func<bool> condition, Func<string> getExceptionMessage,
            params object[] stepParams)
        {
            return VerifyRunner(stepName, () => Assert.That(condition.Invoke(), Is.True, getExceptionMessage),
                Status.failed, stepParams);
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
        public bool That<TActual>(string stepName, ActualValueDelegate<TActual> del, IResolveConstraint expr,
            params object[] stepParams)
        {
            return VerifyRunner(stepName, () => Assert.That(del, expr.Resolve(), null, null), Status.failed,
                stepParams);
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
            }, Status.failed);
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
            Func<string> getExceptionMessage, params object[] stepParams)
        {
            return VerifyRunner(stepName, () =>
            {
                var constraint = expr.Resolve();
                TestExecutionContext.CurrentContext.IncrementAssertCount();
                var result = constraint.ApplyTo(del);
                if (!result.IsSuccess)
                    ReportFailure(result, getExceptionMessage());
            }, Status.failed, stepParams);
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
        public bool That(string stepName, TestDelegate code, IResolveConstraint constraint, params object[] stepParams)
        {
            return VerifyRunner(stepName, () => Assert.That(code, constraint, null, null), Status.failed, stepParams);
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
            return VerifyRunner(stepName, () => Assert.That((object) code, constraint, message, args), Status.failed);
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
            Func<string> getExceptionMessage, params object[] stepParams)
        {
            return VerifyRunner(stepName, () => Assert.That((object) code, constraint, getExceptionMessage),
                Status.failed, stepParams);
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
        public bool That<TActual>(string stepName, TActual actual, IResolveConstraint expression,
            params object[] stepParams)
        {
            return VerifyRunner(stepName, () => Assert.That(actual, expression, stepName, null), Status.failed,
                stepParams);
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
        public bool That<TActual>(string stepName, string message, TActual actual, IResolveConstraint expression,
            params object[] args)
        {
            return VerifyRunner(stepName, () =>
            {
                var constraint = expression.Resolve();

                TestExecutionContext.CurrentContext.IncrementAssertCount();
                var result = constraint.ApplyTo(actual);
                if (!result.IsSuccess)
                    ReportFailure(result, message, args);
            }, Status.failed);
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
            Func<string> getExceptionMessage, params object[] stepParams)
        {
            return VerifyRunner(stepName, () =>
            {
                var constraint = expression.Resolve();

                TestExecutionContext.CurrentContext.IncrementAssertCount();
                var result = constraint.ApplyTo(actual);
                if (!result.IsSuccess)
                    ReportFailure(result, getExceptionMessage());
            }, Status.failed, stepParams);
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
        public bool ByVal(string stepName, object actual, IResolveConstraint expression, params object[] stepParams)
        {
            return VerifyRunner(stepName, () => Assert.That(actual, expression, null, null), Status.failed, stepParams);
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
            return VerifyRunner(stepName, () => Assert.That(actual, expression, message, args), Status.failed);
        }

        #endregion
    }
}