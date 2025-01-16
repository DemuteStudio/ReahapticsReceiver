using System;
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

public class OSCReaperContinuesReceiver : MonoBehaviour
{
    [FormerlySerializedAs("Text")] [SerializeField]
    private TextMeshProUGUI text;

    
    public string hapticAddress = "/HapticJson";
    public string instantHapticAddress = "/InstantHapticJson";
    public string timeAddress = "/CursorPos";
    public string startStopAddress = "/StartStop";
    public int port = 7401; // Match this to the port Reaper sends to

    private OSCReceiver _receiver;
    
    private HapticClip _hapticMaterial;
    private HapticClip _instantHapticMaterial;
    
    private string _hapticData;
    
    private bool _isCursorMoving = false;
    private float _timePos = 0;
    
    private bool _toPlayHaptic = false;
    private float _timeToPlayHaptic = 0;
    private bool _isListeneing = false;
    
    public Button playHapticButton;
    public Button toggleConnectButton;
    public Button loadHapticButton;
    public GameObject connectedLight;
    public TextMeshProUGUI hapticNameText;
    
    void Start()
    {
        toggleConnectButton.onClick.AddListener(ToggleListening);
        playHapticButton.onClick.AddListener(PlayInstandHaptic);
        
        _hapticMaterial = ScriptableObject.CreateInstance<HapticClip>();
        _instantHapticMaterial = ScriptableObject.CreateInstance<HapticClip>();
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
        _receiver.Bind(startStopAddress, message => ReceivedMessage(message));
        _receiver.Bind(hapticAddress, message => ReceivedMessage(message));
        _receiver.Bind(timeAddress, message => ReceivedMessage(message));
        _receiver.Bind(instantHapticAddress, message => ReceivedMessage(message));
        Debug.Log($"Listening for OSC messages on port {port}");
    }

    void ToggleListening()
    {
        if (_isListeneing == false)
        {
            playHapticButton.gameObject.SetActive(false);
            loadHapticButton.gameObject.SetActive(false);

            _isListeneing = true;
            connectedLight.SetActive(true);
            HapticController.Stop();
        }
        else
        {
            playHapticButton.gameObject.SetActive(true);
            loadHapticButton.gameObject.SetActive(true);

            _isListeneing = false;
            connectedLight.SetActive(false);
            HapticController.Stop();
        }
    }
    
    
    private void Update()
    {
        if (_isCursorMoving && _isListeneing)
        {
            _timePos += Time.deltaTime;
            //Debug.Log(_timePos);
            
            if (_timePos >= _timeToPlayHaptic && _toPlayHaptic)
            {
                PlayHaptic();
                _timeToPlayHaptic = 0;
                _toPlayHaptic = false;
            }
        }
    }

    private void PlayHaptic()
    {
        Debug.Log("play haptic at: " + _timePos);
        HapticController.Play(_hapticMaterial);
    }
    
    private void PlayInstandHaptic()
    {
        Debug.Log("play haptic at: " + _timePos);
        HapticController.Play(_instantHapticMaterial);
    }
    
    private void StopHaptic()
    {
        HapticController.Stop();
    }

    private void ReceivedMessage(OSCMessage message)
    {
        if (!_isListeneing && message.Address == instantHapticAddress)
        {
            string input = message.Values[0].StringValue;
            int newlineIndex = input.IndexOf('\n');
            
            string firstPart = input.Substring(0, newlineIndex);
            string secondPart = input.Substring(newlineIndex + 1);
            
            // Extract the float value
            string namePart = firstPart.Replace("name: ", "").Trim();
            Debug.Log(namePart);
            
            var parsedJson = ConvertToJson(secondPart);

            Debug.Log(namePart);
            Debug.Log(secondPart);
        
            _instantHapticMaterial.name = namePart;
            _instantHapticMaterial.json = Encoding.UTF8.GetBytes(parsedJson);
            hapticNameText.text = namePart;
        }
        
        if (_isListeneing == false) return;
        
        if (message.Address == hapticAddress)
        {
            string input = message.Values[0].StringValue;
            int newlineIndex = input.IndexOf('\n');
            
            string firstPart = input.Substring(0, newlineIndex);
            string secondPart = input.Substring(newlineIndex + 1);
            
            // Extract the float value
            string floatPart = firstPart.Replace("SendTime: ", "").Trim();
            float.TryParse(floatPart, out float sendTime);
            _timeToPlayHaptic = sendTime;
            _toPlayHaptic = true;
            var parsedJson = ConvertToJson(secondPart);
            Debug.Log(parsedJson);
            //Debug.Log(parsedJson);
            _hapticData = parsedJson;

            _hapticMaterial.name = "ReaperHaptic";
            _hapticMaterial.json = Encoding.UTF8.GetBytes(parsedJson);
        }
        if (message.Address == timeAddress)
        {
            _timePos = message.Values[0].FloatValue;
            //Debug.Log("time received: " + _timePos);
        }
        if (message.Address == startStopAddress)
        {
            string s = message.Values[0].StringValue;
            if (s == "started")
            {
                _isCursorMoving = true;
                Debug.Log(s);
            }
            else if (s == "stopped")
            {
                _toPlayHaptic = false;
                _isCursorMoving = false;
                StopHaptic();
                Debug.Log(s);
            }
            else if (s == "moved")
            {
                _toPlayHaptic = false;
                StopHaptic();
                Debug.Log(s);
            }
        }
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
