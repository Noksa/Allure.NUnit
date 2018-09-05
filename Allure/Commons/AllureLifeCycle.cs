using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Allure.Commons.Model;
using Allure.Commons.Storage;
using Allure.Commons.Utils;
using Allure.Commons.Writer;
using Newtonsoft.Json;
using NUnit.Framework;

namespace Allure.Commons
{
    public sealed class AllureLifecycle
    {
        private static readonly object Lockobj = new object();
        private static AllureLifecycle _instance;
        private readonly AllureStorage _storage;
        private IAllureResultsWriter _writer;
        internal readonly Configuration Config;
        internal string AllureAssemblyDir => Path.GetDirectoryName(GetType().Assembly.Location);

        private AllureLifecycle()
        {
            using (var r = new StreamReader(Path.Combine(AllureAssemblyDir, AllureConstants.ConfigFilename)))
            {
                var json = r.ReadToEnd();
                Config = JsonConvert.DeserializeObject<Configuration>(json);
            }

            _writer = GetDefaultResultsWriter(Config.Allure.Directory);
            _storage = new AllureStorage();
        }

        public string ResultsDirectory => _writer.ToString();

        public static bool IsMainThread => AllureStorage.IsMainThread;
        internal static int MainThreadId => AllureStorage.MainThreadId;

        public static AllureLifecycle Instance
        {
            get
            {
                if (_instance != null) return _instance;
                lock (Lockobj)
                {
                    _instance = _instance ?? new AllureLifecycle();
                }

                return _instance;
            }
        }

        [Obsolete("In the following releases, this method will be removed. Use json config file instead.")]
        public AllureLifecycle ChangeResultsDirectory(string dirPath)
        {
            _writer = GetDefaultResultsWriter(dirPath);
            return this;
        }

        [Obsolete("In the following releases, this method will be removed. Use json config file instead.")]
        public bool AllowEmptySuites
        {
            get => Config.Allure.AllowEmptySuites;
            set => Config.Allure.AllowEmptySuites = value;
        }

        public static long ToUnixTimestamp(DateTimeOffset value = default(DateTimeOffset))
        {
            if (value == default(DateTimeOffset)) value = DateTimeOffset.Now;
#if NET45 || NET451 || NET452
            return (long) value.UtcDateTime.Subtract(new DateTime(1970, 1, 1)).TotalMilliseconds;
#else
            return value.ToUnixTimeMilliseconds();
#endif
        }

        #region TestContainer

        public AllureLifecycle StartTestContainer(TestResultContainer container)
        {
            container.start = ToUnixTimestamp();
            _storage.Put(container.uuid, container);
            return this;
        }

        public AllureLifecycle StartTestContainer(string parentUuid, TestResultContainer container)
        {
            UpdateTestContainer(parentUuid, c => c.children.Add(container.uuid));
            StartTestContainer(container);
            return this;
        }

        public AllureLifecycle UpdateTestContainer(string uuid, Action<TestResultContainer> update)
        {
            update.Invoke(_storage.Get<TestResultContainer>(uuid));
            return this;
        }

        public AllureLifecycle StopTestContainer(string uuid)
        {
            UpdateTestContainer(uuid, c => c.stop = ToUnixTimestamp());
            return this;
        }

        public AllureLifecycle WriteTestContainer(string uuid)
        {
            _writer.Write(_storage.Remove<TestResultContainer>(uuid));
            return this;
        }

        #endregion

        #region Fixture

        public AllureLifecycle StartBeforeFixture(string parentUuid, string uuid, FixtureResult result)
        {
            UpdateTestContainer(parentUuid, container => container.befores.Add(result));
            StartFixture(uuid, result);
            return this;
        }

        public AllureLifecycle StartAfterFixture(string parentUuid, string uuid, FixtureResult result)
        {
            UpdateTestContainer(parentUuid, container => container.afters.Add(result));
            StartFixture(uuid, result);
            return this;
        }

        public AllureLifecycle UpdateFixture(Action<FixtureResult> update)
        {
            UpdateFixture(_storage.GetRootStep(), update);
            return this;
        }

        public AllureLifecycle UpdateFixture(string uuid, Action<FixtureResult> update)
        {
            update.Invoke(_storage.Get<FixtureResult>(uuid));
            return this;
        }

        public AllureLifecycle StopFixture(Action<FixtureResult> beforeStop)
        {
            UpdateFixture(beforeStop);
            return StopFixture(_storage.GetRootStep());
        }

        public AllureLifecycle StopFixture(string uuid)
        {
            var fixture = _storage.Remove<FixtureResult>(uuid);
            _storage.ClearStepContext();
            fixture.stage = Stage.finished;
            fixture.stop = ToUnixTimestamp();
            return this;
        }

        #endregion

        #region TestCase

        public AllureLifecycle StartTestCase(string containerUuid, TestResult testResult)
        {
            UpdateTestContainer(containerUuid, c => c.children.Add(testResult.uuid));
            return StartTestCase(testResult);
        }

        public AllureLifecycle StartTestCase(TestResult testResult)
        {
            testResult.stage = Stage.running;
            testResult.start = ToUnixTimestamp();
            _storage.Put(testResult.uuid, testResult);
            _storage.ClearStepContext();
            _storage.StartStep(testResult.uuid);
            return this;
        }

        public AllureLifecycle UpdateTestCase(string uuid, Action<TestResult> update)
        {
            update.Invoke(_storage.Get<TestResult>(uuid));
            return this;
        }

        public AllureLifecycle UpdateTestCase(Action<TestResult> update)
        {
            return UpdateTestCase(_storage.GetRootStep(), update);
        }

        public AllureLifecycle StopTestCase(Action<TestResult> beforeStop)
        {
            UpdateTestCase(beforeStop);
            return StopTestCase(_storage.GetRootStep());
        }

        public AllureLifecycle StopTestCase(string uuid)
        {
            var testResult = _storage.Get<TestResult>(uuid);
            testResult.stage = Stage.finished;
            testResult.stop = ToUnixTimestamp();
            _storage.ClearStepContext();
            return this;
        }

        public AllureLifecycle WriteTestCase(string uuid)
        {
            _writer.Write(_storage.Remove<TestResult>(uuid));
            return this;
        }

        #endregion

        #region Step

        public AllureLifecycle StartStep(string uuid, StepResult result)
        {
            StartStep(_storage.GetCurrentStep(), uuid, result);
            return this;
        }

        public AllureLifecycle MakeStepWithExMessage(int assertsBeforeCount, string stepName, Exception ex)
        {
            var exMsg = ex.Message;
            if (assertsBeforeCount != TestContext.CurrentContext.Result.Assertions.Count())
            {
                exMsg = TestContext.CurrentContext.Result.Assertions.Last().Message.Trim();
                var firstStepIndex = exMsg.IndexOf(stepName, StringComparison.Ordinal);
                if (firstStepIndex != -1) exMsg = exMsg.Substring(stepName.Length);
                var indexOfTire = exMsg.IndexOf('-');
                if (indexOfTire != -1) exMsg = exMsg.Substring(0, indexOfTire).Trim();
                if (string.IsNullOrEmpty(exMsg)) return this;
                StartStepAndFailIt(exMsg);
            }
            else if (string.IsNullOrEmpty(exMsg))
            {
                return this;
            }
            else if (ex.InnerException == null)
            {
                StartStepAndFailIt(exMsg);
            }
            else
            {
                GetInnerExceptions(ex).ToList().ForEach(inEx => StartStepAndFailIt(inEx.Message));
            }

            return this;
        }

        private AllureLifecycle StartStepAndFailIt(string stepName)
        {
            var uuid = $"{Guid.NewGuid():N}";
            Instance.StartStep(stepName, uuid);
            Instance.UpdateStep(uuid, _ => _.status = Status.failed);
            Instance.StopStep(uuid);
            return this;
        }

        public void StartStep(string stepName, string uuid)
        {
            var stepResult = new StepResult
            {
                name = stepName,
                start = ToUnixTimestamp()
            };
            Instance.StartStep(uuid, stepResult);
        }


        public AllureLifecycle StartStep(string parentUuid, string uuid, StepResult stepResult)
        {
            stepResult.stage = Stage.running;
            stepResult.start = ToUnixTimestamp();
            _storage.StartStep(uuid);
            _storage.AddStep(parentUuid, uuid, stepResult);
            return this;
        }

        public AllureLifecycle UpdateStep(Action<StepResult> update)
        {
            update.Invoke(_storage.Get<StepResult>(_storage.GetCurrentStep()));
            return this;
        }

        public AllureLifecycle UpdateStep(string uuid, Action<StepResult> update)
        {
            update.Invoke(_storage.Get<StepResult>(uuid));
            return this;
        }

        public AllureLifecycle StopStep(Action<StepResult> beforeStop)
        {
            UpdateStep(beforeStop);
            return StopStep(_storage.GetCurrentStep());
        }

        public AllureLifecycle StopStep(string uuid)
        {
            var step = _storage.Remove<StepResult>(uuid);
            step.stage = Stage.finished;
            step.stop = ToUnixTimestamp();
            _storage.StopStep();
            return this;
        }

        #endregion

        #region Attachment

        public enum AttachFormat
        {
            Xml,
            Json,
            ImagePng,
            Txt,
            Video
        }

        public AllureLifecycle AddAttachment(string name, AttachFormat type, string content, string fileExtension = "")
        {
            if (type == AttachFormat.ImagePng)
                return AddAttachment(name, "image/png", Encoding.UTF8.GetBytes(content), fileExtension);

            switch (type)
            {
                case AttachFormat.Xml:
                    content = XDocument.Parse(content).ToString();
                    return AddAttachment(name, "text/xml", Encoding.UTF8.GetBytes(content), fileExtension);
                case AttachFormat.Json:
                    var obj = JsonConvert.DeserializeObject(content);
                    content = JsonConvert.SerializeObject(obj, Formatting.Indented);
                    return AddAttachment(name, "application/json", Encoding.UTF8.GetBytes(content), fileExtension);
                case AttachFormat.Txt:
                    return AddAttachment(name, "text/txt", Encoding.UTF8.GetBytes(content), fileExtension);
                case AttachFormat.Video:
                    return AddAttachment(name, "video/mp4", content);
                default:
                    throw new ArgumentException($"You cant use \"{type}\" argument at this method.");
            }
        }

        public AllureLifecycle AddAttachment(string name, string type, string path)
        {
            var file = new FileInfo(path);
            if (!file.Exists) throw new ArgumentException($"Cant find file at path \"{path}\".\nMake sure you are using the correct path to the file.");
            var fileExtension = file.Extension;
            return AddAttachment(name, type, File.ReadAllBytes(path), fileExtension);
        }

        public AllureLifecycle AddAttachment(string name, string type, byte[] content, string fileExtension = "")
        {
            var source = $"{Guid.NewGuid():N}{AllureConstants.AttachmentFileSuffix}{fileExtension}";
            var attachment = new Attachment
            {
                name = name,
                type = type,
                source = source
            };
            _writer.Write(source, content);
            _storage.Get<ExecutableItem>(_storage.GetCurrentStep()).attachments.Add(attachment);
            return this;
        }

        public AllureLifecycle AddAttachment(string path, string name = null)
        {
            name = name ?? Path.GetFileName(path);
            var type = MimeTypesMap.GetMimeType(path);
            return AddAttachment(name, type, path);
        }

        #endregion

        #region Extensions

        public void CleanupResultDirectory()
        {
            _writer.CleanUp();
        }

        public AllureLifecycle AddScreenDiff(string testCaseUuid, string expectedPng, string actualPng, string diffPng)
        {
            AddAttachment(expectedPng, "expected")
                .AddAttachment(actualPng, "actual")
                .AddAttachment(diffPng, "diff")
                .UpdateTestCase(testCaseUuid, x => x.labels.Add(Label.TestType("screenshotDiff")));

            return this;
        }

        #endregion


        #region Privates

        private static IEnumerable<Exception> GetInnerExceptions(Exception ex)
        {
            if (ex == null) throw new ArgumentNullException(nameof(ex));

            if (ex.InnerException == null) yield break;
            var innerException = ex.InnerException;
            do
            {
                yield return innerException;
                innerException = innerException.InnerException;
            } while (innerException != null);
        }

        private void StartFixture(string uuid, FixtureResult fixtureResult)
        {
            _storage.Put(uuid, fixtureResult);
            fixtureResult.stage = Stage.running;
            fixtureResult.start = ToUnixTimestamp();
            _storage.ClearStepContext();
            _storage.StartStep(uuid);
        }

        internal IAllureResultsWriter GetDefaultResultsWriter(string outDir)
        {
            if (outDir == AllureConstants.DefaultResultsFolder) outDir = Path.Combine(AllureAssemblyDir, AllureConstants.DefaultResultsFolder);
            if (outDir == "temp") outDir = Path.Combine(Path.GetTempPath(), AllureConstants.DefaultResultsFolder);
            return new FileSystemResultsWriter(outDir);
        }

        #endregion
        
    }
}