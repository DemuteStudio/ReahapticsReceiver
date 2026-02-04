using System;
using System.IO;
using UnityEngine;
using Newtonsoft.Json;

/// <summary>
/// Test script to verify reverse haptic converters work correctly.
/// Attach to a GameObject and call TestConversions() to run tests.
/// </summary>
public class HapticConverterTest : MonoBehaviour
{
    [Header("Test Files")]
    public string niceVibrationsTestFile = "Assets/Feel/NiceVibrations/HapticSamples/ApplicationUX/Beep2.haptic";
    public string interHapticsTestFile = "Assets/HapticEffects/Body Hit.haps";

    [ContextMenu("Test Conversions")]
    public void TestConversions()
    {
        Debug.Log("=== Starting Haptic Converter Tests ===");

        TestNiceVibrationsConverter();
        TestInterHapticsConverter();
        TestRoundTripConversion();

        Debug.Log("=== Haptic Converter Tests Complete ===");
    }

    private void TestNiceVibrationsConverter()
    {
        Debug.Log("\n--- Testing Nice Vibrations Converter ---");

        try
        {
            if (!File.Exists(niceVibrationsTestFile))
            {
                Debug.LogError($"Test file not found: {niceVibrationsTestFile}");
                return;
            }

            string niceVibrationsJson = File.ReadAllText(niceVibrationsTestFile);
            Debug.Log($"Loaded Nice Vibrations file: {Path.GetFileName(niceVibrationsTestFile)}");

            string convertedJson = HapticConverter.ConvertFromJsonNiceVibrations(niceVibrationsJson);

            HapticInputData result = JsonConvert.DeserializeObject<HapticInputData>(convertedJson);

            if (result == null)
            {
                Debug.LogError("Conversion failed: result is null");
                return;
            }

            Debug.Log($"✓ Successfully converted Nice Vibrations format");
            Debug.Log($"  Amplitude points: {result.amplitude?.Count ?? 0}");
            Debug.Log($"  Frequency points: {result.frequency?.Count ?? 0}");

            // Count emphasis points
            int emphasisCount = 0;
            if (result.amplitude != null)
            {
                foreach (var amp in result.amplitude)
                {
                    if (amp.emphasis != null && (amp.emphasis.amplitude != 0 || amp.emphasis.frequency != 0))
                    {
                        emphasisCount++;
                    }
                }
            }
            Debug.Log($"  Emphasis points: {emphasisCount}");

            // Validate data ranges
            if (result.amplitude != null)
            {
                foreach (var amp in result.amplitude)
                {
                    if (amp.amplitude < 0 || amp.amplitude > 1)
                    {
                        Debug.LogWarning($"Amplitude out of range [0,1]: {amp.amplitude} at time {amp.time}");
                    }
                }
            }

            if (result.frequency != null)
            {
                foreach (var freq in result.frequency)
                {
                    if (freq.frequency < 0 || freq.frequency > 1)
                    {
                        Debug.LogWarning($"Frequency out of range [0,1]: {freq.frequency} at time {freq.time}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Test failed with exception: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void TestInterHapticsConverter()
    {
        Debug.Log("\n--- Testing InterHaptics Converter ---");

        try
        {
            if (!File.Exists(interHapticsTestFile))
            {
                Debug.LogError($"Test file not found: {interHapticsTestFile}");
                return;
            }

            string interHapticsJson = File.ReadAllText(interHapticsTestFile);
            Debug.Log($"Loaded InterHaptics file: {Path.GetFileName(interHapticsTestFile)}");

            string convertedJson = HapticConverter.ConvertFromJsonInterHaptics(interHapticsJson);

            HapticInputData result = JsonConvert.DeserializeObject<HapticInputData>(convertedJson);

            if (result == null)
            {
                Debug.LogError("Conversion failed: result is null");
                return;
            }

            Debug.Log($"✓ Successfully converted InterHaptics format");
            Debug.Log($"  Amplitude points: {result.amplitude?.Count ?? 0}");
            Debug.Log($"  Frequency points: {result.frequency?.Count ?? 0}");

            // Count emphasis points
            int emphasisCount = 0;
            if (result.amplitude != null)
            {
                foreach (var amp in result.amplitude)
                {
                    if (amp.emphasis != null && (amp.emphasis.amplitude != 0 || amp.emphasis.frequency != 0))
                    {
                        emphasisCount++;
                    }
                }
            }
            Debug.Log($"  Emphasis points: {emphasisCount}");

            // Validate data ranges
            if (result.amplitude != null)
            {
                foreach (var amp in result.amplitude)
                {
                    if (amp.amplitude < 0 || amp.amplitude > 1)
                    {
                        Debug.LogWarning($"Amplitude out of range [0,1]: {amp.amplitude} at time {amp.time}");
                    }
                }
            }

            if (result.frequency != null)
            {
                foreach (var freq in result.frequency)
                {
                    if (freq.frequency < 0 || freq.frequency > 1)
                    {
                        Debug.LogWarning($"Frequency out of range [0,1]: {freq.frequency} at time {freq.time}");
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Test failed with exception: {ex.Message}\n{ex.StackTrace}");
        }
    }

    private void TestRoundTripConversion()
    {
        Debug.Log("\n--- Testing Round-Trip Conversions ---");

        // Create test data
        HapticInputData testData = new HapticInputData
        {
            amplitude = new System.Collections.Generic.List<InputAmplitude>
            {
                new InputAmplitude { time = 0f, amplitude = 0f },
                new InputAmplitude { time = 0.1f, amplitude = 0.5f, emphasis = new InputEmphasis { amplitude = 0.8f, frequency = 0.6f } },
                new InputAmplitude { time = 0.2f, amplitude = 1f },
                new InputAmplitude { time = 0.3f, amplitude = 0.5f },
                new InputAmplitude { time = 0.4f, amplitude = 0f }
            },
            frequency = new System.Collections.Generic.List<InputFrequency>
            {
                new InputFrequency { time = 0f, frequency = 0.3f },
                new InputFrequency { time = 0.2f, frequency = 0.7f },
                new InputFrequency { time = 0.4f, frequency = 0.5f }
            }
        };

        string originalJson = JsonConvert.SerializeObject(testData, Formatting.Indented);

        // Test Nice Vibrations round-trip
        Debug.Log("Testing Nice Vibrations round-trip...");
        try
        {
            string niceVibrationsFormat = HapticConverter.ConvertToJsonNiceVibrations(originalJson);
            string backToInputData = HapticConverter.ConvertFromJsonNiceVibrations(niceVibrationsFormat);
            HapticInputData result = JsonConvert.DeserializeObject<HapticInputData>(backToInputData);

            if (result.amplitude.Count == testData.amplitude.Count &&
                result.frequency.Count == testData.frequency.Count)
            {
                Debug.Log("✓ Nice Vibrations round-trip successful");
                Debug.Log($"  Original: {testData.amplitude.Count} amplitude, {testData.frequency.Count} frequency");
                Debug.Log($"  Result: {result.amplitude.Count} amplitude, {result.frequency.Count} frequency");
            }
            else
            {
                Debug.LogWarning("⚠ Nice Vibrations round-trip data count mismatch");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Nice Vibrations round-trip failed: {ex.Message}");
        }

        // Test InterHaptics round-trip
        Debug.Log("Testing InterHaptics round-trip...");
        try
        {
            string interHapticsFormat = HapticConverter.ConvertToJsonInterHaptics(originalJson);
            string backToInputData = HapticConverter.ConvertFromJsonInterHaptics(interHapticsFormat);
            HapticInputData result = JsonConvert.DeserializeObject<HapticInputData>(backToInputData);

            if (result.amplitude.Count == testData.amplitude.Count &&
                result.frequency.Count == testData.frequency.Count)
            {
                Debug.Log("✓ InterHaptics round-trip successful");
                Debug.Log($"  Original: {testData.amplitude.Count} amplitude, {testData.frequency.Count} frequency");
                Debug.Log($"  Result: {result.amplitude.Count} amplitude, {result.frequency.Count} frequency");

                // Test frequency transformation
                float originalFreq = testData.frequency[0].frequency;
                float resultFreq = result.frequency[0].frequency;
                float difference = Mathf.Abs(originalFreq - resultFreq);
                if (difference < 0.01f)
                {
                    Debug.Log($"✓ Frequency transformation accurate (diff: {difference:F4})");
                }
                else
                {
                    Debug.LogWarning($"⚠ Frequency transformation inaccurate (diff: {difference:F4})");
                }
            }
            else
            {
                Debug.LogWarning("⚠ InterHaptics round-trip data count mismatch");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"InterHaptics round-trip failed: {ex.Message}");
        }
    }
}
