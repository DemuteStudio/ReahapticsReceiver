using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class HapticConverter
{
    public static string ConvertToJsonNiceVibrations(string input)
    {
        JObject inputObject = JObject.Parse(input);
        JArray amplitudeArray = (JArray)inputObject["amplitude"];
        JArray frequencyArray = (JArray)inputObject["frequency"];

        var output = new
        {
            version = new { major = 1, minor = 0, patch = 0 },
            metadata = new
            {
                editor = "ReaHaptic",
                source = "",
                project = "",
                tags = new List<string>(),
                description = ""
            },
            signals = new
            {
                continuous = new
                {
                    envelopes = new
                    {
                        amplitude = ProcessAmplitude(amplitudeArray),
                        frequency = ProcessFrequency(frequencyArray)
                    }
                }
            }
        };

        return JsonConvert.SerializeObject(output, Formatting.Indented);
    }

    public static string ConvertToJsonInterHaptics(string jsonInput)
    {
        Input input = JsonUtility.FromJson<Input>(jsonInput);
        HapsFormat output = new HapsFormat();

        HapsFormatMelody emphasisMelody = new HapsFormatMelody();
        HapsFormatMelody mainMelody = new HapsFormatMelody();

        foreach (var amp in input.amplitude)
        {
            if (amp.emphasis.amplitude != 0 || amp.emphasis.frequency != 0)
            {
                emphasisMelody.m_notes.Add(new HapsFormatNote
                {
                    m_startingPoint = (float)Math.Round(amp.time, 3),
                    m_gain = (float)Math.Round(amp.emphasis.amplitude, 3)
                });
            }
        }

        HapsFormatHapticEffect hapticEffect = new HapsFormatHapticEffect
        {
            m_amplitudeModulation = new HapsFormatModulation(),
            m_frequencyModulation = new HapsFormatModulation()
        };

        foreach (var amp in input.amplitude)
        {
            hapticEffect.m_amplitudeModulation.m_keyframes.Add(new HapsFormatKeyframe { m_time = amp.time, m_value = amp.amplitude });
        }

        foreach (var freq in input.frequency)
        {
            hapticEffect.m_frequencyModulation.m_keyframes.Add(new HapsFormatKeyframe { m_time = freq.time, m_value = freq.frequency * 700f + 60f });
        }

        mainMelody.m_notes.Add(new HapsFormatNote
        {
            m_startingPoint = 0.0f,
            m_length = 1.0f,
            m_priority = 1,
            m_gain = 1.0f,
            m_hapticEffect = hapticEffect
        });

        output.m_vibration.m_melodies.Add(emphasisMelody);
        output.m_vibration.m_melodies.Add(mainMelody);

        return JsonUtility.ToJson(output, true);
    }

    private static List<object> ProcessAmplitude(JArray amplitudeArray)
    {
        var processedAmplitude = new List<object>();

        foreach (var item in amplitudeArray)
        {
            var amplitudeObject = new Dictionary<string, object>
            {
                { "time", (float)item["time"] },
                { "amplitude", (float)item["amplitude"] }
            };

            if (item["emphasis"] != null)
            {
                amplitudeObject["emphasis"] = new
                {
                    amplitude = (float)item["emphasis"]["amplitude"],
                    frequency = (float)item["emphasis"]["frequency"]
                };
            }

            processedAmplitude.Add(amplitudeObject);
        }

        return processedAmplitude;
    }

    private static List<object> ProcessFrequency(JArray frequencyArray)
    {
        var processedFrequency = new List<object>();

        foreach (var item in frequencyArray)
        {
            var frequencyObject = new Dictionary<string, object>
            {
                { "time", (float)item["time"] },
                { "frequency", (float)item["frequency"] }
            };

            processedFrequency.Add(frequencyObject);
        }

        return processedFrequency;
    }
}