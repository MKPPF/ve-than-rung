using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework.Interfaces;
using TopDownGame.Save;
using UnityEngine;
using UnityEngine.TestRunner;

[assembly: TestRunCallback(typeof(TopDownGame.Tests.PlayMode.SaveRecoveryPreferenceIsolation))]

namespace TopDownGame.Tests.PlayMode
{
    /// <summary>Extends existing save fixtures to restore the two new recovery records after each test, including failed coroutines.</summary>
    public sealed class SaveRecoveryPreferenceIsolation : ITestRunCallback
    {
        private readonly Dictionary<string, (bool exists, string value)> records = new Dictionary<string, (bool, string)>();
        private readonly PlayModeInputIsolation input = new PlayModeInputIsolation();
        private ProgressLoadStatus status;
        private bool active;

        public void RunStarted(ITest testsToRun) { }
        public void RunFinished(ITestResult testResults) => Restore();
        public void TestStarted(ITest test)
        {
            if (test.IsSuite) return;
            input.Begin();
            records.Clear();
            foreach (string suffix in new[] { ".Backup", ".Unreadable" })
            {
                string key = "TopDownGame.Tests.Progress.v1" + suffix;
                records[key] = (PlayerPrefs.HasKey(key), PlayerPrefs.GetString(key, ""));
            }
            status = GameProgressManager.Instance != null ? GameProgressManager.Instance.LoadStatus : ProgressLoadStatus.Missing;
            active = true;
        }
        public void TestFinished(ITestResult result)
        {
            if (!result.Test.IsSuite) Restore();
        }
        private void Restore()
        {
            input.Restore();
            if (!active) return;
            active = false;
            foreach (var record in records)
                if (record.Value.exists) PlayerPrefs.SetString(record.Key, record.Value.value); else PlayerPrefs.DeleteKey(record.Key);
            if (GameProgressManager.Instance != null)
                typeof(GameProgressManager).GetField("loadStatus", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(GameProgressManager.Instance, status);
        }
    }
}
