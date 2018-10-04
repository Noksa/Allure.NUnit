using System;
using NUnit.Framework;

namespace Allure.NUnit.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AllureFlakyAttribute : NUnitAttribute
    {
        public AllureFlakyAttribute()
        {
            Flaky = true;
        }

        internal bool Flaky { get; }
    }
}