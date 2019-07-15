using System.IO;
using System.Runtime.CompilerServices;
using NUnit.Framework.Internal;

[assembly:InternalsVisibleTo("Allure.SpecFlowPlugin")]

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
                    pathOfDir = ReflectionHelper.GetMemberValueFromAssembly(assemblyWithTests, AllureConstants.AllureConfigFileCustomDirMemberName)?.ToString();
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
    }
}