using System;
using NUnit.Framework;

namespace Allure.NUnit.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class AllureEpicAttribute : NUnitAttribute
    {
        public AllureEpicAttribute(string epic)
        {
            Epic = epic;
        }

        internal string Epic { get; }
    }
}