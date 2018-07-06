using System;
using NUnit.Framework;

namespace Allure.NUnit.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AllureParentSuiteAttribute : NUnitAttribute
    {
        public AllureParentSuiteAttribute(string parentSuite)
        {
            ParentSuite = parentSuite;
        }

        internal string ParentSuite { get; }
    }
}