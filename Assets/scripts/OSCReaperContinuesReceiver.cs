using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Net.NetworkInformation;
using UnityEngine;
using extOSC; // Use the library of your choice
using Lofelt.NiceVibrations;
using TMPro;
using UnityEngine.UI;
using System.Text;
using Interhaptics;
using UnityEngine.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine.Serialization;
using Interhaptics.Internal;
using Interhaptics.Utils;

public class OSCReaperContinuesReceiver : MonoBehaviour
{
    [FormerlySerializedAs("Text")] [SerializeField]
    private TextMeshProUGUI text;

    [Header("OSC variables")]
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
    [Header("UI elements")]
    public Button playHapticButton;
    public Button toggleConnectButton;
    public Button loadHapticButton;
    public GameObject connectedLight;
    public GameObject connectedLightGreen;
    public TextMeshProUGUI hapticNameText;
    public TextMeshProUGUI IPText;
    public TMP_InputField PortInput;
    public GameObject WarningPanel;
    public Button WarningPanelButton;
    [Header("interhaptics")]
    public EventHapticSource eventHapticSource;
    public static string GetLocalIPAddress()
    {
        string localIP = "No network found";

        try
        {
            foreach (IPAddress ip in Dns.GetHostAddresses(Dns.GetHostName()))
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork) // IPv4 only
                {
                    localIP = ip.ToString();
                    break; // Take the first valid IP
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("Error getting IP: " + e.Message);
        }

        return localIP;
    }
    void Start()
    {
        IPText.text = GetLocalIPAddress();
        Debug.Log($"IP:  {GetLocalIPAddress()}");
        
        toggleConnectButton.onClick.AddListener(ToggleListening);
        playHapticButton.onClick.AddListener(PlayInstandHaptic);
        WarningPanelButton.onClick.AddListener(CloseWarningPanel);
        PortInput.onEndEdit.AddListener(OnPortIntputChanged);
        
        _hapticMaterial = ScriptableObject.CreateInstance<HapticClip>();
        _instantHapticMaterial = ScriptableObject.CreateInstance<HapticClip>();
        if (DeviceCapabilities.meetsAdvancedRequirements == true)
        {
            Debug.Log("Haptics supported");
        }
        else
        {
            Debug.Log("Haptics not supported");
            WarningPanel.SetActive(true);
        }
        
        _receiver = gameObject.AddComponent<OSCReceiver>();
        _receiver.LocalPort = port;
        PortInput.text = port.ToString();
        // Bind handler for received messages
        _receiver.Bind(startStopAddress, message => ReceivedMessage(message));
        _receiver.Bind(hapticAddress, message => ReceivedMessage(message));
        _receiver.Bind(timeAddress, message => ReceivedMessage(message));
        _receiver.Bind(instantHapticAddress, message => ReceivedMessage(message));
        Debug.Log($"Listening for OSC messages on port {port}");
    }

    private void CloseWarningPanel()
    {
        WarningPanel.SetActive(false);
    }
    private void OnPortIntputChanged(string value)
    {
        if (int.TryParse(value, out int port))
        {
            _receiver.LocalPort = port;
            Debug.Log($"Port changed to {port}");
        }
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
        HapticController.fallbackPreset = HapticPatterns.PresetType.Success;
        HapticController.Play(_hapticMaterial);
        eventHapticSource.Play();
    }
    
    private void PlayInstandHaptic()
    {
        Debug.Log("play haptic at: " + _timePos);
        HapticController.Play(_instantHapticMaterial);
        eventHapticSource.Play();
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
            
            HapticMaterial hm = HapticMaterial.CreateInstanceFromString("InstantHaptic");
            eventHapticSource.hapticMaterial = hm;
            
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
                connectedLightGreen.SetActive(true);
            }
            else if (s == "stopped")
            {
                _toPlayHaptic = false;
                _isCursorMoving = false;
                StopHaptic();
                Debug.Log(s);
                connectedLightGreen.SetActive(false);
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
    
    public static string ConvertToIHJson(string input)
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
