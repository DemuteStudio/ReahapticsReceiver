using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using extOSC; // Use the library of your choice
using Lofelt.NiceVibrations;
using TMPro;

public class OSCReaperReceiver : MonoBehaviour
{
    [SerializeField]
    private TextMeshProUGUI amplitudeValueText;
    [SerializeField]
    private TextMeshProUGUI frequencyValueText;
    [SerializeField]
    private TextMeshProUGUI transientValueText;
    [SerializeField]
    private TextMeshProUGUI activeValueText;
    
    public string[] addresses = { "/track/2/pan", "/track/3/pan", "/track/4/pan" };
    public int port = 9000; // Match this to the port Reaper sends to
    public float emphDelay = 0.02f;
    private OSCReceiver _receiver;
    float _timeOfLastEmphasis = 0;
    float _emphasisValue = 0;
    
    float _currentAmplitude = 0;
    float _currentFrequency = 0;
    float _targetAmplitude = 0;
    float _targetFrequency = 0;
    
    bool hapticsSupported = DeviceCapabilities.isVersionSupported;
    bool amplitudeModulationSupported = DeviceCapabilities.hasAmplitudeModulation;
    bool frequencyModulationSupported = DeviceCapabilities.hasFrequencyModulation;
    
    void Start()
    {
        
        if (DeviceCapabilities.meetsAdvancedRequirements == true)
        {
            Debug.Log("Haptics supported");
            Debug.Log("Amplitude Modulation supported: " + amplitudeModulationSupported);
            Debug.Log("Frequency Modulation supported: " + frequencyModulationSupported);

            if (amplitudeModulationSupported)
                amplitudeValueText.color = Color.green;
            else
            {
                amplitudeValueText.color = Color.red;
                amplitudeValueText.text = "Not Supported";
            }
            if (frequencyModulationSupported)
                frequencyValueText.color = Color.green;
            else
            {
                frequencyValueText.color = Color.red;
                frequencyValueText.text = "Not Supported";
            }
        }
        else
        {
            Debug.Log("Haptics not supported");
            amplitudeValueText.color = Color.red;
            amplitudeValueText.text = "Not Supported";
            frequencyValueText.color = Color.red;
            frequencyValueText.text = "Not Supported";
        }
        
        _receiver = gameObject.AddComponent<OSCReceiver>();
        _receiver.LocalPort = port;

        // Bind handler for received messages
        foreach (var address in addresses)
        {
            _receiver.Bind(address, message => ReceivedMessage(message, address));
        }

        Debug.Log($"Listening for OSC messages on port {port}");
        
        HapticPatterns.PlayConstant(1,1,20);
        Invoke(nameof(ContinueHaptic),20);
    }

    void ContinueHaptic()
    {
        HapticPatterns.PlayConstant(_currentAmplitude,_currentFrequency,20);
        Invoke(nameof(ContinueHaptic),20);
        
    }
    void Update()
    {
        activeValueText.text = HapticController.IsPlaying().ToString();
        
        if (_targetAmplitude < 0.01f) _targetAmplitude = 0;
        
        if (_targetFrequency < 0.01f) _targetFrequency = 0;
        
        
        if (Mathf.Abs(_currentAmplitude - _targetAmplitude) < 0.01f)
        {
            _currentAmplitude = Mathf.Lerp(_currentAmplitude, _targetAmplitude, 0.05f);
            HapticController.clipLevel = _currentAmplitude;
        }
        else
        {
            _currentAmplitude = _targetAmplitude;
            HapticController.clipLevel = _currentAmplitude;
        }
        if (Mathf.Abs(_currentFrequency - _targetFrequency) < 0.01f)
        {
            _currentFrequency = Mathf.Lerp(_currentFrequency, _targetFrequency, 0.05f);
            HapticController.clipFrequencyShift = _currentFrequency;
        }
        else
        {
            _currentFrequency = _targetFrequency;
            HapticController.clipFrequencyShift = _currentFrequency;
        }
        amplitudeValueText.text = _currentAmplitude.ToString();
        frequencyValueText.text = _currentFrequency.ToString();
    }
    private void ReceivedMessage(OSCMessage message, string address)
    {
        if (message.ToFloat(out float value))
        {
            float v = 1-value;
            switch (address)
            {
                case "/track/2/pan":
                    _targetAmplitude = v;
                    HapticController.clipLevel = v;
                    // Do something with the value
                    Debug.Log($"Received value {v} for Amplitude");
                    break;
                case "/track/3/pan":
                    _targetFrequency = v;
                    HapticController.clipFrequencyShift = v;
                    Debug.Log($"Received value {v} for Frequency");
                    break;
                case "/track/4/pan":
                    if (v > 0)
                    {
                        transientValueText.text = v.ToString();
                        HapticPatterns.PlayEmphasis(v, 1.0f);
                        Debug.Log($"Received value {v} for Emphasis");
                    }
                    
                    break;
                default:
                    Debug.LogWarning($"Unknown address {address}");
                    break;
            }
        }
    }
}
