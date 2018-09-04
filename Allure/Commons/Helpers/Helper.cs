using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework.Interfaces;

namespace Allure.Commons.Helpers
{
    internal static class Helper
    {
        internal static List<ITest> GetAllTestsInSuite(ITest suite)
        {
            var list = new List<ITest>();
            foreach (var nestedTests1 in suite.Tests)
            {
                if (nestedTests1.HasChildren)
                {
                    foreach (var nestedTests2 in nestedTests1.Tests)
                    {
                        if (nestedTests2.HasChildren)
                        {
                            foreach (var nestedTests3 in nestedTests2.Tests)
                            {
                                if (nestedTests3.HasChildren)
                                {
                                    foreach (var nestedTests4 in nestedTests3.Tests)
                                    {
                                        list.Add(nestedTests4);
                                    }
                                }
                                else list.Add(nestedTests3);
                            }
                        }
                        else list.Add(nestedTests2);
                    }
                }
                else list.Add(nestedTests1);
            }

            return list;
        }
    }
}
