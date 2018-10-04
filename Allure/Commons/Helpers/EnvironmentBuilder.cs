using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace Allure.Commons.Helpers
{
    internal static class EnvironmentBuilder
    {
        internal static void BuildEnvFile(DirectoryInfo dir)
        {
            var filePath = Path.Combine(dir.FullName, AllureConstants.EnvironmentFileName);
            if (File.Exists(filePath)) File.Delete(filePath);

            using (var sw = new StreamWriter(filePath))
            {
                var tokens = AllureLifecycle.AllureEnvironment?.SelectToken("runtime")?.Children().Children();
                var tokenMemberNameRegex = new Regex("[^\\.]([^.]*)$");
                var tokenNameSpaceRegex = new Regex(".*(?=\\.)");
                if (tokens != null)
                {
                    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
                    foreach (var token in tokens)
                    {
                        GetTokenNameAndValue(token, out var tokenName, out var tokenValue);

                        var tokenMemberName = tokenMemberNameRegex.Match(tokenValue).Value;
                        var tokenNameSpaceName = tokenNameSpaceRegex.Match(tokenValue).Value;
                        object memberValue = null;
                        if (tokenValue.ToLower().StartsWith("system.environment."))
                        {
                            var variable = tokenMemberNameRegex.Match(tokenValue).Value;
                            memberValue = Environment.GetEnvironmentVariable(variable);
                            if (memberValue == null)
                                sw.WriteLine($"{tokenName}=Cant find system variable with name \"{variable}\"");
                        }
                        else
                        {
                            var type = assemblies.FirstOrDefault(q => q.GetType(tokenNameSpaceName) != null)
                                ?.GetType(tokenNameSpaceName);
                            if (type != null)
                            {
                                var member = type.GetMember(tokenMemberName,
                                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy |
                                    BindingFlags.NonPublic).FirstOrDefault();
                                if (member == null)

                                    member = type
                                        .GetFields(BindingFlags.Public | BindingFlags.Static |
                                                   BindingFlags.FlattenHierarchy | BindingFlags.NonPublic)
                                        .FirstOrDefault(fi =>
                                            fi.IsLiteral && !fi.IsInitOnly && fi.Name == tokenMemberName);
                                if (member != null)
                                {
                                    var memberType = member.MemberType;
                                    switch (memberType)
                                    {
                                        case MemberTypes.Field:
                                            memberValue = ((FieldInfo) member).GetValue(null);
                                            break;
                                        case MemberTypes.Property:
                                            memberValue = ((PropertyInfo) member).GetValue(null);
                                            break;
                                    }
                                }
                                else
                                {
                                    memberValue =
                                        $"Cant find member with name \"{tokenMemberName}\" at namespace \"{tokenNameSpaceName}\"";
                                }
                            }
                            else
                            {
                                sw.WriteLine($"{tokenName}=Cant find namespace \"{tokenNameSpaceName}\"");
                            }
                        }

                        if (memberValue != null) sw.WriteLine($"{tokenName}={memberValue}");
                    }
                }

                var tokensConsts = AllureLifecycle.AllureEnvironment?.Children()
                    .Where(q => q.Path != "environment.runtime");

                if (tokensConsts != null)
                    foreach (var token in tokensConsts)
                    {
                        GetTokenNameAndValue(token, out var tokenName, out var tokenValue);
                        sw.WriteLine($"{tokenName}={tokenValue}");
                    }
            }
        }

        private static void GetTokenNameAndValue(JToken token, out string tokenName, out string tokenValue)
        {
            var arr = token.ToString().Split(':');
            tokenName = arr[0].Replace("\"", "").Trim();
            tokenValue = arr[1].Replace("\"", "").Trim();
        }
    }
}