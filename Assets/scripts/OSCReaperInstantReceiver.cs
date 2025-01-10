using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using extOSC; // Use the library of your choice
using Lofelt.NiceVibrations;
using TMPro;
using UnityEngine.UI;
using System.Text;
using UnityEngine.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
public class HapticData
{
    public float time;
    public float amplitude;
    public float frequency;
    public Emphasis emphasis;

    public class Emphasis
    {
        public float amplitude;
        public float frequency;
    }
}

public class OSCReaperInstantReceiver : MonoBehaviour
{
    [FormerlySerializedAs("Text")] [SerializeField]
    private TextMeshProUGUI text;

    public string address = "/InstantHapticJson";
    public int port = 7401; // Match this to the port Reaper sends to

    private OSCReceiver _receiver;
    
    private HapticClip _hapticMaterial;
    
    public Button playHapticButton;
    private string _hapticData;
    
    void Start()
    {
        playHapticButton.onClick.AddListener(PlayHaptic);
        _hapticMaterial = ScriptableObject.CreateInstance<HapticClip>();
        
        if (DeviceCapabilities.meetsAdvancedRequirements == true)
        {
            Debug.Log("Haptics supported");
        }
        else
        {
            Debug.Log("Haptics not supported");
        }
        
        _receiver = gameObject.AddComponent<OSCReceiver>();
        _receiver.LocalPort = port;

        // Bind handler for received messages
        _receiver.Bind(address, message => ReceivedMessage(message));
        
        Debug.Log($"Listening for OSC messages on port {port}");
    }

    private void PlayHaptic()
    {
        HapticController.Play(_hapticMaterial);
    }

    private void ReceivedMessage(OSCMessage message)
    {
        var parsedJson = ConvertToJson(message.Values[0].StringValue);

        Debug.Log(parsedJson);
        _hapticData = message.Values[0].StringValue;
        
        _hapticMaterial.name = "ReaperHaptic";
        text.text = "ReaperHaptic";
    }
    public static string ConvertToJson(string input)
    {
        // Parse the input string into a JObject
        JObject inputObject = JObject.Parse(input);

        // Extract amplitude and frequency arrays
        JArray amplitudeArray = (JArray)inputObject["amplitude"];
        JArray frequencyArray = (JArray)inputObject["frequency"];

        // Create the target JSON structure
        var output = new
        {
            version = new { major = 1, minor = 0, patch = 0 },
            metadata = new
            {
                editor = "Meta Haptics Studio",
                source = "..\\..\\ReaperSessions\\QuickMatch\\RenderedFiles\\sfx_quickMatch_victory.wav",
                project = "hap_quickMatch_victory",
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
