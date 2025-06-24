using System;
using System.Runtime.InteropServices;
using System.IO;
using System;
using UnityEngine;
using System.Text;

namespace Lofelt.NiceVibrations
{
    public static class NiceVibrationsNative
    {
#if !NICE_VIBRATIONS_DISABLE_GAMEPAD_SUPPORT
        [DllImport("nice_vibrations_editor_plugin")]
        private static extern IntPtr nv_plugin_convert_haptic_to_gamepad_rumble([In] byte[] bytes, long size);

        [DllImport("nice_vibrations_editor_plugin")]
        private static extern void nv_plugin_destroy(IntPtr gamepadRumble);

        [DllImport("nice_vibrations_editor_plugin")]
        private static extern UIntPtr nv_plugin_get_length(IntPtr gamepadRumble);

        [DllImport("nice_vibrations_editor_plugin")]
        private static extern void nv_plugin_get_durations(IntPtr gamepadRumble, [Out] int[] durations);

        [DllImport("nice_vibrations_editor_plugin")]
        private static extern void nv_plugin_get_low_frequency_motor_speeds(IntPtr gamepadRumble, [Out] float[] lowFrequencies);

        [DllImport("nice_vibrations_editor_plugin")]
        private static extern void nv_plugin_get_high_frequency_motor_speeds(IntPtr gamepadRumble, [Out] float[] highFrequencies);

        [DllImport("nice_vibrations_editor_plugin")]
        private static extern IntPtr nv_plugin_get_last_error();

        [DllImport("nice_vibrations_editor_plugin")]
        private static extern UIntPtr nv_plugin_get_last_error_length();
#endif
        static public HapticClip JsonToHapticClip(byte[] jsonBytes)
        {
            // Load .haptic clip from file
            var hapticClip = HapticClip.CreateInstance<HapticClip>();
            hapticClip.json = jsonBytes;

    #if !NICE_VIBRATIONS_DISABLE_GAMEPAD_SUPPORT
            // Convert JSON to a GamepadRumble struct. The conversion algorithm is inside the native
            // library nice_vibrations_editor_plugin. That plugin is only used in the Unity editor, and
            // not at runtime.
            GamepadRumble rumble = default;
            IntPtr nativeRumble = nv_plugin_convert_haptic_to_gamepad_rumble(jsonBytes, jsonBytes.Length);
            if (nativeRumble != IntPtr.Zero)
            {
                try
                {
                    uint length = (uint)nv_plugin_get_length(nativeRumble);
                    rumble.durationsMs = new int[length];
                    rumble.lowFrequencyMotorSpeeds = new float[length];
                    rumble.highFrequencyMotorSpeeds = new float[length];

                    nv_plugin_get_durations(nativeRumble, rumble.durationsMs);
                    nv_plugin_get_low_frequency_motor_speeds(nativeRumble, rumble.lowFrequencyMotorSpeeds);
                    nv_plugin_get_high_frequency_motor_speeds(nativeRumble, rumble.highFrequencyMotorSpeeds);

                    int totalDurationMs = 0;
                    foreach (int duration in rumble.durationsMs)
                    {
                        totalDurationMs += duration;
                    }
                    rumble.totalDurationMs = totalDurationMs;
                }
                finally
                {
                    nv_plugin_destroy(nativeRumble);
                }
            }
            else
            {
                var lastErrorPtr = nv_plugin_get_last_error();
                var lastErrorLength = (int)nv_plugin_get_last_error_length();
            }

            hapticClip.gamepadRumble = rumble;
    #endif
            return hapticClip;
        }
    }
}

