using System;
using NUnit.Framework;

namespace Allure.NUnit.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AllureTestAttribute : NUnitAttribute
    {
        public AllureTestAttribute(string description = "")
        {
            Description = description;
        }

        internal string Description { get; }
    }
}