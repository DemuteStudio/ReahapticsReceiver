using System;
using System.Collections.Generic;
using System.Linq;
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
        HapticInputData input = JsonConvert.DeserializeObject<HapticInputData>(jsonInput);

        if (input == null)
        {
            Debug.LogError("[HapticConverter] Failed to deserialize haptic input data");
            return JsonConvert.SerializeObject(new HapsFormat(), Formatting.Indented);
        }

        if (input.amplitude == null)
        {
            Debug.LogError("[HapticConverter] Input amplitude is null");
            return JsonConvert.SerializeObject(new HapsFormat(), Formatting.Indented);
        }

        HapsFormat output = new HapsFormat();

        HapsFormatMelody emphasisMelody = new HapsFormatMelody();
        HapsFormatMelody mainMelody = new HapsFormatMelody();
        HapsFormatHapticEffect hapticEffectEmphasis = new HapsFormatHapticEffect
        {
            m_type = 0
        };
        foreach (var amp in input.amplitude)
        {
            if (amp.emphasis != null && (amp.emphasis.amplitude != 0 || amp.emphasis.frequency != 0))
            {
                emphasisMelody.m_notes.Add(new HapsFormatNote
                {
                    m_startingPoint = (float)Math.Round(amp.time, 3),
                    m_gain = (float)Math.Round(amp.emphasis.amplitude, 3),
                    m_hapticEffect = hapticEffectEmphasis
                });
            }
        }

        HapsFormatHapticEffect hapticEffect = new HapsFormatHapticEffect
        {
            m_amplitudeModulation = new HapsFormatModulation(),
            m_frequencyModulation = new HapsFormatModulation(),
        };

        float max_time = 0f;
        foreach (var amp in input.amplitude)
        {
            hapticEffect.m_amplitudeModulation.m_keyframes.Add(new HapsFormatKeyframe { m_time = amp.time, m_value = (float)Math.Round(amp.amplitude,3) });
            max_time = amp.time;
        }

        if (input.frequency != null)
        {
            foreach (var freq in input.frequency)
            {
                hapticEffect.m_frequencyModulation.m_keyframes.Add(new HapsFormatKeyframe { m_time = freq.time, m_value = (float)Math.Round(freq.frequency * 700f + 60f, 3 ) });
            }
        }
        

        mainMelody.m_notes.Add(new HapsFormatNote
        {
            m_startingPoint = 0.0f,
            m_length = max_time,
            m_priority = 1,
            m_gain = 1.0f,
            m_hapticEffect = hapticEffect
        });

        output.m_vibration.m_melodies.Add(emphasisMelody);
        output.m_vibration.m_melodies.Add(mainMelody);

        return JsonConvert.SerializeObject(output, Formatting.Indented);
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
    
    public static string ConvertToJsonRichTap(string input)
    {
        JObject inputObject = JObject.Parse(input);
        JArray amplitudeArray = (JArray)inputObject["amplitude"];
        JArray frequencyArray = (JArray)inputObject["frequency"];

        var patternEvents = new List<object>();

        // Process emphasis points as transient events
        foreach (var item in amplitudeArray)
        {
            if (item["emphasis"] != null)
            {
                float time = (float)item["time"];
                float emphasisAmplitude = (float)item["emphasis"]["amplitude"];
                float emphasisFrequency = (float)item["emphasis"]["frequency"];

                var transientEvent = new
                {
                    Event = new
                    {
                        Parameters = new
                        {
                            Frequency = (int)(emphasisFrequency * 100), // Convert 0-1 to 0-100
                            Intensity = (int)(emphasisAmplitude * 100)  // Convert 0-1 to 0-100
                        },
                        Type = "transient",
                        Index = 0,
                        RelativeTime = (int)(time * 1000) // Convert seconds to milliseconds
                    }
                };

                patternEvents.Add(transientEvent);
            }
        }

        // Create continuous events from amplitude and frequency curves
        if (amplitudeArray.Count > 0 || frequencyArray.Count > 0)
        {
            // Use a custom class to store curve point data
            var timePoints = new Dictionary<float, CurvePointData>();

            // Process amplitude points
            foreach (var item in amplitudeArray)
            {
                float time = (float)item["time"];
                float amplitude = (float)item["amplitude"];
                
                timePoints[time] = new CurvePointData
                {
                    Frequency = 0, // Default frequency offset
                    Intensity = amplitude,
                    Time = (int)(time * 1000) // Convert to milliseconds
                };
            }

            // Merge frequency points
            foreach (var item in frequencyArray)
            {
                float time = (float)item["time"];
                float frequency = (float)item["frequency"];
                int frequencyOffset = (int)((frequency - 0.5f) * 200); // Map -1 to 1 range to -100 to 100

                if (timePoints.ContainsKey(time))
                {
                    // Update existing point
                    timePoints[time].Frequency = frequencyOffset;
                }
                else
                {
                    // Add new point
                    timePoints[time] = new CurvePointData
                    {
                        Frequency = frequencyOffset,
                        Intensity = 0.0f,
                        Time = (int)(time * 1000)
                    };
                }
            }

            // Convert to list and sort by time
            var sortedPoints = new List<object>();
            foreach (var kvp in timePoints)
            {
                var point = kvp.Value;
                sortedPoints.Add(new
                {
                    Frequency = point.Frequency,
                    Intensity = point.Intensity,
                    Time = point.Time
                });
            }
            sortedPoints.Sort((a, b) => 
            {
                var aTime = (int)a.GetType().GetProperty("Time").GetValue(a);
                var bTime = (int)b.GetType().GetProperty("Time").GetValue(b);
                return aTime.CompareTo(bTime);
            });

            // Create continuous event if we have curve points
            if (sortedPoints.Count > 0)
            {
                var firstPoint = sortedPoints[0];
                var lastPoint = sortedPoints[sortedPoints.Count - 1];
                
                int startTime = (int)firstPoint.GetType().GetProperty("Time").GetValue(firstPoint);
                int endTime = (int)lastPoint.GetType().GetProperty("Time").GetValue(lastPoint);
                int duration = Math.Max(endTime - startTime, 100); // Minimum duration of 100ms

                var continuousEvent = new
                {
                    Event = new
                    {
                        Duration = duration,
                        Parameters = new
                        {
                            Curve = sortedPoints,
                            Frequency = 30, // Base frequency
                            Intensity = 89  // Base intensity
                        },
                        Type = "continuous",
                        Index = 0,
                        RelativeTime = startTime
                    }
                };

                patternEvents.Add(continuousEvent);
            }
        }

        // Sort events by RelativeTime
        patternEvents.Sort((a, b) => 
        {
            var aTime = (int)a.GetType().GetProperty("Event").GetValue(a).GetType().GetProperty("RelativeTime").GetValue(a.GetType().GetProperty("Event").GetValue(a));
            var bTime = (int)b.GetType().GetProperty("Event").GetValue(b).GetType().GetProperty("RelativeTime").GetValue(b.GetType().GetProperty("Event").GetValue(b));
            return aTime.CompareTo(bTime);
        });

        var output = new
        {
            Metadata = new
            {
                Created = DateTime.Now.ToString("yyyy-MM-dd"),
                Description = "Exported from ReaHaptic",
                Version = 2
            },
            PatternList = new[]
            {
                new
                {
                    AbsoluteTime = 0,
                    Pattern = patternEvents
                }
            }
        };

        return JsonConvert.SerializeObject(output, Formatting.Indented);
    }

    // Helper class to store curve point data without using dynamic
    private class CurvePointData
    {
        public int Frequency { get; set; }
        public float Intensity { get; set; }
        public int Time { get; set; }
    }

    /// <summary>
    /// Converts from Nice Vibrations .haptic format to HapticInputData format.
    /// </summary>
    public static string ConvertFromJsonNiceVibrations(string jsonInput)
    {
        try
        {
            JObject inputObject = JObject.Parse(jsonInput);

            // Navigate to the envelopes
            JToken signals = inputObject["signals"];
            if (signals == null)
            {
                Debug.LogError("[HapticConverter] Missing 'signals' field in Nice Vibrations format");
                return JsonConvert.SerializeObject(new HapticInputData { amplitude = new List<InputAmplitude>(), frequency = new List<InputFrequency>() }, Formatting.Indented);
            }

            JToken continuous = signals["continuous"];
            if (continuous == null)
            {
                Debug.LogError("[HapticConverter] Missing 'continuous' field in Nice Vibrations format");
                return JsonConvert.SerializeObject(new HapticInputData { amplitude = new List<InputAmplitude>(), frequency = new List<InputFrequency>() }, Formatting.Indented);
            }

            JToken envelopes = continuous["envelopes"];
            if (envelopes == null)
            {
                Debug.LogError("[HapticConverter] Missing 'envelopes' field in Nice Vibrations format");
                return JsonConvert.SerializeObject(new HapticInputData { amplitude = new List<InputAmplitude>(), frequency = new List<InputFrequency>() }, Formatting.Indented);
            }

            JArray amplitudeArray = (JArray)envelopes["amplitude"];
            JArray frequencyArray = (JArray)envelopes["frequency"];

            HapticInputData output = new HapticInputData
            {
                amplitude = new List<InputAmplitude>(),
                frequency = new List<InputFrequency>()
            };

            // Process amplitude array (includes optional emphasis data)
            if (amplitudeArray != null)
            {
                foreach (var item in amplitudeArray)
                {
                    InputAmplitude amp = new InputAmplitude
                    {
                        time = (float)item["time"],
                        amplitude = (float)item["amplitude"]
                    };

                    // Check for emphasis data
                    if (item["emphasis"] != null)
                    {
                        amp.emphasis = new InputEmphasis
                        {
                            amplitude = (float)item["emphasis"]["amplitude"],
                            frequency = (float)item["emphasis"]["frequency"]
                        };
                    }

                    output.amplitude.Add(amp);
                }
            }

            // Process frequency array
            if (frequencyArray != null)
            {
                foreach (var item in frequencyArray)
                {
                    InputFrequency freq = new InputFrequency
                    {
                        time = (float)item["time"],
                        frequency = (float)item["frequency"]
                    };

                    output.frequency.Add(freq);
                }
            }

            Debug.Log($"[HapticConverter] Converted Nice Vibrations format: {output.amplitude.Count} amplitude points, {output.frequency.Count} frequency points");
            return JsonConvert.SerializeObject(output, Formatting.Indented);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[HapticConverter] Failed to convert Nice Vibrations format: {ex.Message}");
            return JsonConvert.SerializeObject(new HapticInputData { amplitude = new List<InputAmplitude>(), frequency = new List<InputFrequency>() }, Formatting.Indented);
        }
    }

    /// <summary>
    /// Converts from InterHaptics .haps format to HapticInputData format.
    /// </summary>
    public static string ConvertFromJsonInterHaptics(string jsonInput)
    {
        try
        {
            HapsFormat input = JsonConvert.DeserializeObject<HapsFormat>(jsonInput);

            if (input == null)
            {
                Debug.LogError("[HapticConverter] Failed to deserialize InterHaptics format");
                return JsonConvert.SerializeObject(new HapticInputData { amplitude = new List<InputAmplitude>(), frequency = new List<InputFrequency>() }, Formatting.Indented);
            }

            HapticInputData output = new HapticInputData
            {
                amplitude = new List<InputAmplitude>(),
                frequency = new List<InputFrequency>()
            };

            // Extract emphasis points from melody[0]
            Dictionary<float, InputEmphasis> emphasisMap = new Dictionary<float, InputEmphasis>();
            if (input.m_vibration != null && input.m_vibration.m_melodies != null && input.m_vibration.m_melodies.Count > 0)
            {
                HapsFormatMelody emphasisMelody = input.m_vibration.m_melodies[0];
                if (emphasisMelody.m_notes != null)
                {
                    foreach (var note in emphasisMelody.m_notes)
                    {
                        float time = (float)Math.Round(note.m_startingPoint, 3);
                        emphasisMap[time] = new InputEmphasis
                        {
                            amplitude = note.m_gain,
                            frequency = 0f // InterHaptics doesn't store emphasis frequency in this location
                        };
                    }
                }
            }

            // Extract amplitude and frequency from melody[1]
            if (input.m_vibration != null && input.m_vibration.m_melodies != null && input.m_vibration.m_melodies.Count > 1)
            {
                HapsFormatMelody mainMelody = input.m_vibration.m_melodies[1];
                if (mainMelody.m_notes != null && mainMelody.m_notes.Count > 0)
                {
                    HapsFormatNote mainNote = mainMelody.m_notes[0];
                    if (mainNote.m_hapticEffect != null)
                    {
                        // Process amplitude keyframes
                        if (mainNote.m_hapticEffect.m_amplitudeModulation != null &&
                            mainNote.m_hapticEffect.m_amplitudeModulation.m_keyframes != null)
                        {
                            foreach (var keyframe in mainNote.m_hapticEffect.m_amplitudeModulation.m_keyframes)
                            {
                                float time = (float)Math.Round(keyframe.m_time, 3);
                                InputAmplitude amp = new InputAmplitude
                                {
                                    time = keyframe.m_time,
                                    amplitude = keyframe.m_value
                                };

                                // Check if there's emphasis data for this time
                                if (emphasisMap.ContainsKey(time))
                                {
                                    amp.emphasis = emphasisMap[time];
                                }

                                output.amplitude.Add(amp);
                            }
                        }

                        // Process frequency keyframes with reverse transformation
                        if (mainNote.m_hapticEffect.m_frequencyModulation != null &&
                            mainNote.m_hapticEffect.m_frequencyModulation.m_keyframes != null)
                        {
                            foreach (var keyframe in mainNote.m_hapticEffect.m_frequencyModulation.m_keyframes)
                            {
                                // Reverse the transformation: original was (freq * 700 + 60)
                                // So reverse is: (value - 60) / 700
                                float normalizedFrequency = (keyframe.m_value - 60f) / 700f;

                                // Clamp to [0, 1] range
                                normalizedFrequency = Mathf.Clamp01(normalizedFrequency);

                                InputFrequency freq = new InputFrequency
                                {
                                    time = keyframe.m_time,
                                    frequency = (float)Math.Round(normalizedFrequency, 3)
                                };

                                output.frequency.Add(freq);
                            }
                        }
                    }
                }
            }

            Debug.Log($"[HapticConverter] Converted InterHaptics format: {output.amplitude.Count} amplitude points, {output.frequency.Count} frequency points, {emphasisMap.Count} emphasis points");
            return JsonConvert.SerializeObject(output, Formatting.Indented);
        }
        catch (Exception ex)
        {
            Debug.LogError($"[HapticConverter] Failed to convert InterHaptics format: {ex.Message}");
            return JsonConvert.SerializeObject(new HapticInputData { amplitude = new List<InputAmplitude>(), frequency = new List<InputFrequency>() }, Formatting.Indented);
        }
    }
}