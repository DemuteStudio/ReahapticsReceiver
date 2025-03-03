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


[Serializable]
public class InputAmplitude
{
    public float time;
    public float amplitude;
    public InputEmphasis emphasis;
}

[Serializable]
public class InputFrequency
{
    public float time;
    public float frequency;
}

[Serializable]
public class InputEmphasis
{
    public float amplitude;
    public float frequency;
}

[Serializable]
public class Input
{
    public List<InputAmplitude> amplitude;
    public List<InputFrequency> frequency;
}

[Serializable]
public class HapsFormatNote
{
    public float m_startingPoint;
    public float m_length = 0.022f;
    public int m_priority = 0;
    public float m_gain;
    public HapsFormatHapticEffect m_hapticEffect = new HapsFormatHapticEffect();
}

[Serializable]
public class HapsFormatHapticEffect
{
    public int m_type = 0;
    public HapsFormatModulation m_amplitudeModulation;
    public HapsFormatModulation m_frequencyModulation;
}

[Serializable]
public class HapsFormatModulation
{
    public int m_extrapolationStrategy = 0;
    public List<HapsFormatKeyframe> m_keyframes = new List<HapsFormatKeyframe>();
}

[Serializable]
public class HapsFormatKeyframe
{
    public float m_time;
    public float m_value;
}

[Serializable]
public class HapsFormatMelody
{
    public int m_mute = 0;
    public float m_gain = 1.0f;
    public List<HapsFormatNote> m_notes = new List<HapsFormatNote>();
}

[Serializable]
public class HapsFormatVibration
{
    public int m_loop = 0;
    public float m_maximum = 1.0f;
    public float m_gain = 1.0f;
    public int m_signalEvaluationMethod = 3;
    public List<HapsFormatMelody> m_melodies = new List<HapsFormatMelody>();
}

[Serializable]
public class HapsFormat
{
    public string m_version = "5";
    public string m_description = "";
    public int m_HDFlag = 0;
    public int m_time_unit = 0;
    public int m_length_unit = 7;
    public HapsFormatVibration m_vibration = new HapsFormatVibration();
    public HapsFormatVibration m_stiffness = new HapsFormatVibration();
    public HapsFormatVibration m_texture = new HapsFormatVibration();
    public float m_gain = 1.0f;
}

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

    private bool useNiceVibrations = false;
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

    public void SetHapticsMethod(int val)
    {
        if (val == 1) useNiceVibrations = true;
        else useNiceVibrations = false;
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
        if (useNiceVibrations)
        {
            Debug.Log("play haptic with NiceVibrations at: " + _timePos);
            HapticController.fallbackPreset = HapticPatterns.PresetType.Success;
            HapticController.Play(_hapticMaterial);
        }
        else
        {
            Debug.Log("play haptic with InterHaptics at: " + _timePos);
            eventHapticSource.Play();
            print(eventHapticSource.hapticMaterial.text);
        }
    }
    
    private void PlayInstandHaptic()
    {
        
        if (useNiceVibrations)
        {
            Debug.Log("play haptic with NiceVibrations at: " + _timePos);
            HapticController.fallbackPreset = HapticPatterns.PresetType.Success;
            HapticController.Play(_instantHapticMaterial);
        }
        else
        {
            Debug.Log("play haptic with InterHaptics at: " + _timePos);
            eventHapticSource.Play();
            print(eventHapticSource.hapticMaterial.text);
        }
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
            
            var parsedJson = ConvertToJsonNiceVibrations(secondPart);
            var parsedJsonInteHaptics = ConvertToJsonInterHaptics(secondPart);
            Debug.Log(namePart);
            Debug.Log(secondPart);
            
            HapticMaterial hm = HapticMaterial.CreateInstanceFromString(parsedJsonInteHaptics);
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
            var parsedJson = ConvertToJsonNiceVibrations(secondPart);
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
    public static string ConvertToJsonNiceVibrations(string input)
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
                    m_gain = (float)Math.Round(amp.emphasis.amplitude,3)
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
