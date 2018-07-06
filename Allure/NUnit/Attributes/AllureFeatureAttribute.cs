using System;
using NUnit.Framework;

namespace Allure.NUnit.Attributes
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public class AllureFeatureAttribute : NUnitAttribute
    {
        public AllureFeatureAttribute(params string[] feature)
        {
            Features = feature;
        }

        internal string[] Features { get; }
    }
}