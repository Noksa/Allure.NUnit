using System;
using NUnit.Framework;

namespace Allure.NUnit.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AllureSubSuiteAttribute : NUnitAttribute
    {
        public AllureSubSuiteAttribute(string subSuite)
        {
            SubSuite = subSuite;
        }

        internal string SubSuite { get; }
    }
}