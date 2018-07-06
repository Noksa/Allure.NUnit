using System;
using NUnit.Framework;

namespace Allure.NUnit.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AllureSuiteAttribute : NUnitAttribute
    {
        public AllureSuiteAttribute(string suite)
        {
            Suite = suite;
        }

        internal string Suite { get; }
    }
}