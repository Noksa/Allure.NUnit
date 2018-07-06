using System;
using NUnit.Framework;

namespace Allure.NUnit.Attributes
{
    [AttributeUsage(AttributeTargets.Method)]
    public class AllureOwnerAttribute : NUnitAttribute
    {
        public AllureOwnerAttribute(string owner)
        {
            Owner = owner;
        }

        internal string Owner { get; }
    }
}