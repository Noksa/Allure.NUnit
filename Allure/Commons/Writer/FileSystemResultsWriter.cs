using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Allure.Commons.Model;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

[assembly: InternalsVisibleTo("DynamicProxyGenAssembly2")]

namespace Allure.Commons.Writer
{
    internal sealed class FileSystemResultsWriter : IAllureResultsWriter
    {
        private readonly string _outputDirectory;
        private readonly JsonSerializer _serializer = new JsonSerializer();

        internal FileSystemResultsWriter(string outputDirectory)
        {
            _outputDirectory = GetResultsDirectory(outputDirectory);
            CleanUp(true);
            _serializer.NullValueHandling = NullValueHandling.Ignore;
            _serializer.Formatting = Formatting.Indented;
            _serializer.Converters.Add(new StringEnumConverter());
        }

        public DirectoryInfo Dir => new DirectoryInfo(_outputDirectory);


        public void Write(TestResult testResult)
        {
            Write(testResult, AllureConstants.TestResultFileSuffix);
        }

        public void Write(TestResultContainer testResult)
        {
            Write(testResult, AllureConstants.TestResultContainerFileSuffix);
        }

        public void Write(string source, byte[] content)
        {
            var filePath = Path.Combine(_outputDirectory, source);
            File.WriteAllBytes(filePath, content);
        }

        public void CleanUp(bool deleteHistoryAndCategories)
        {
            using (var mutex = new Mutex(false, "729dc988-0e9c-49d0-9e50-17e0df3cd82b"))
            {
                mutex.WaitOne();
                var directory = new DirectoryInfo(_outputDirectory);
                foreach (var file in directory.GetFiles())
                {
                    if (!deleteHistoryAndCategories)
                        if (file.Name.ToLower().Equals("categories.json"))
                            continue;

                    file.Delete();
                }

                mutex.ReleaseMutex();
            }
        }

        public override string ToString()
        {
            return _outputDirectory;
        }

        private string Write(object allureObject, string fileSuffix)
        {
            var type = allureObject.GetType();
            var fileName = Guid.NewGuid().ToString("N");
            if (type == typeof(TestResult))
                fileName = ((TestResult) allureObject).uuid + $"-{Guid.NewGuid()}";
            else if (type == typeof(TestResultContainer))
                fileName = ((TestResultContainer) allureObject).uuid + $"-{Guid.NewGuid()}";
            var filePath = Path.Combine(_outputDirectory, $"{fileName}{fileSuffix}");
            using (var fileStream = File.CreateText(filePath))
            {
                _serializer.Serialize(fileStream, allureObject);
            }

            return filePath;
        }

        internal bool HasDirectoryAccess(string directory)
        {
            var tempFile = Path.Combine(directory, Guid.NewGuid().ToString());
            try
            {
                File.WriteAllText(tempFile, string.Empty);
                File.Delete(tempFile);
                return true;
            }
            catch (UnauthorizedAccessException)
            {
                return false;
            }
        }

        private string GetResultsDirectory(string outputDirectory)
        {
            var parentDir = new DirectoryInfo(outputDirectory).Parent.FullName;
            outputDirectory = HasDirectoryAccess(parentDir)
                ? outputDirectory
                : Path.Combine(
                    Path.GetTempPath(), AllureConstants.DefaultResultsFolder);

            Directory.CreateDirectory(outputDirectory);

            return new DirectoryInfo(outputDirectory).FullName;
        }
    }
}