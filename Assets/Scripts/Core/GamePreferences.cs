using System;
using System.Reflection;
using UnityEngine;

namespace TopDownGame.Core
{
    /// <summary>Separates automated settings checks from the user's persisted preferences.</summary>
    public static class GamePreferences
    {
        public static bool IsAutomatedTestRun
        {
            get
            {
#if UNITY_EDITOR
                foreach (string argument in Environment.GetCommandLineArgs())
                    if (string.Equals(argument, "-runTests", StringComparison.OrdinalIgnoreCase)) return true;
                Type api = Type.GetType("UnityEditor.TestTools.TestRunner.Api.TestRunnerApi, UnityEditor.TestRunner");
                MethodInfo check = api?.GetMethod("IsRunActive", BindingFlags.Static | BindingFlags.NonPublic);
                if (check?.Invoke(null, null) is bool running && running) return true;
#endif
                return false;
            }
        }

        public static string Key(string key) => IsAutomatedTestRun ? key.Replace("TopDownGame.", "TopDownGame.Tests.") : key;
    }
}
