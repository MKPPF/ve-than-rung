using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace TopDownGame.Tests.PlayMode
{
    /// <summary>Lets synthetic input reach tests without Game View focus or physical input, then restores the user's settings and devices.</summary>
    internal sealed class PlayModeInputIsolation
    {
        private readonly List<InputDevice> nativeDevices = new List<InputDevice>();
        private InputSettings originalSettings;
        private InputSettings testSettings;
        private HideFlags originalFlags;

        public void Begin()
        {
            Restore();
            originalSettings = InputSystem.settings;
            testSettings = Object.Instantiate(originalSettings);
            originalFlags = originalSettings.hideFlags;
            // InputManager destroys HideAndDontSave settings when replacing them.
            // Keep the original temporary object alive until it can be restored.
            if (originalFlags == HideFlags.HideAndDontSave)
                originalSettings.hideFlags = HideFlags.DontSave;
            testSettings.hideFlags = HideFlags.HideAndDontSave;
            testSettings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            testSettings.editorInputBehaviorInPlayMode =
                InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
            InputSystem.settings = testSettings;

            foreach (InputDevice device in InputSystem.devices)
            {
                if (!device.native || !device.enabled) continue;
                nativeDevices.Add(device);
                InputSystem.DisableDevice(device);
            }
        }

        public void Restore()
        {
            if (originalSettings != null)
            {
                InputSystem.settings = originalSettings;
                originalSettings.hideFlags = originalFlags;
            }
            originalSettings = null;
            foreach (InputDevice device in nativeDevices)
                if (device.added) InputSystem.EnableDevice(device);
            nativeDevices.Clear();
            if (testSettings != null)
            {
                if (Application.isPlaying) Object.Destroy(testSettings);
                else Object.DestroyImmediate(testSettings);
                testSettings = null;
            }
        }
    }
}
