using System.IO;
using Allure.Commons.Model;

namespace Allure.Commons.Writer
{
    internal interface IAllureResultsWriter
    {
        DirectoryInfo Dir { get; }
        void Write(TestResult testResult);
        void Write(TestResultContainer testResult);
        void Write(string source, byte[] attachment);
        void CleanUp(bool deleteCategories);
    }
}