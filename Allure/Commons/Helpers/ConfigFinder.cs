using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework.Internal;

namespace Allure.Commons.Helpers
{
    internal static class ConfigFinder
    {
        internal static string DefaultConfigDir =
            Path.GetDirectoryName(typeof(AllureLifecycle).Assembly.Location);

        internal static string AllureConfigFilePath
        {
            get
            {
                string pathOfDir = null;

                var assemblyWithTests = TestExecutionContext.CurrentContext?.CurrentTest?.TypeInfo?.Assembly;
                if (assemblyWithTests != null)
                {
                    pathOfDir = GetDirFromAssembly(assemblyWithTests);
                }

                if (string.IsNullOrEmpty(pathOfDir))
                    pathOfDir = DefaultConfigDir;

                var filePath = Path.Combine(pathOfDir, AllureConstants.ConfigFilename);
                var fileExists = File.Exists(filePath);

                if (!fileExists && pathOfDir == DefaultConfigDir)
                {
                    var msg =
                        $"allureConfig.json not found at \"{DefaultConfigDir}\".\nMake sure the allureConfig.json's property \"Copy to Output Directory\" is set to \"Always\" or \"Copy if newer\"";
                    throw new FileNotFoundException(msg);
                }

                if (!fileExists)
                {
                    var msg = $"allureConfig.json not found at \"{pathOfDir}\"";
                    throw new FileNotFoundException(msg);
                }

                return filePath;
            }
        }


        private static string GetDirFromAssembly(Assembly assembly)
        {
            string pathOfDir = null;
            var types = assembly.GetTypes();

            foreach (var type in types)
            {
                var member = type.GetMember(AllureConstants.AllureConfigFileCustomDirMemberName,
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy |
                    BindingFlags.NonPublic).FirstOrDefault();
                if (member == null)

                    member = type
                        .GetFields(BindingFlags.Public | BindingFlags.Static |
                                   BindingFlags.FlattenHierarchy | BindingFlags.NonPublic)
                        .FirstOrDefault(fi =>
                            fi.IsLiteral && !fi.IsInitOnly &&
                            fi.Name == AllureConstants.AllureConfigFileCustomDirMemberName);
                if (member != null)
                {
                    var memberType = member.MemberType;
                    object value = null;
                    switch (memberType)
                    {
                        case MemberTypes.Field:
                            value = ((FieldInfo) member).GetValue(null);
                            break;
                        case MemberTypes.Property:
                            value = ((PropertyInfo) member).GetValue(null);
                            break;
                    }

                    pathOfDir = value?.ToString();
                    break;
                }
            }

            return pathOfDir;
        }
    }
}