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
    [SerializeField]
    private TextMeshProUGUI SendsPerSecondText;
    
    [SerializeField]
    private TextMeshProUGUI HighestAmplitudeValueText;
    
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

    private int SendsRecievedPerSecond = 0;
    private float timer = 0;
    
    bool hapticsSupported = DeviceCapabilities.isVersionSupported;
    bool amplitudeModulationSupported = DeviceCapabilities.hasAmplitudeModulation;
    bool frequencyModulationSupported = DeviceCapabilities.hasFrequencyModulation;
    
    float DebugHigheAmpValue = 0;
    float DebugHigheFreqValue = 0;
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
        timer += Time.deltaTime;
        if (timer > 1)
        {
            SendsPerSecondText.text = SendsRecievedPerSecond.ToString();
            SendsRecievedPerSecond = 0;
            timer = 0;
        }
        activeValueText.text = HapticController.IsPlaying().ToString();
        
        if (_targetAmplitude < 0.01f) _targetAmplitude = 0;
        
        if (_targetFrequency < 0.01f) _targetFrequency = 0;
        
        
        if (Mathf.Abs(_currentAmplitude - _targetAmplitude) < 0.01f)
        {
            _currentAmplitude = _targetAmplitude;
            HapticController.clipLevel = _currentAmplitude;
        }
        else
        {
            _currentAmplitude = _targetAmplitude;
            HapticController.clipLevel = _currentAmplitude;
        }
        if (Mathf.Abs(_currentFrequency - _targetFrequency) < 0.01f)
        {
            _currentFrequency = _targetFrequency;
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

    void resetHighestValue()
    {
        DebugHigheAmpValue = 0;
        HighestAmplitudeValueText.text = DebugHigheAmpValue.ToString();
    }
    private void ReceivedMessage(OSCMessage message, string address)
    {
        if (message.ToFloat(out float value))
        {
            float v = value;
            switch (address)
            {
                case "/amplitude/pan":
                    SendsRecievedPerSecond++;
                    if (_targetAmplitude < v)
                    {
                        DebugHigheAmpValue = v;
                        Invoke(nameof(resetHighestValue),2);
                        HighestAmplitudeValueText.text = DebugHigheAmpValue.ToString();
                    }
                    _targetAmplitude = v;
                    HapticController.clipLevel = v;
                    // Do something with the value
                    //Debug.Log($"Received value {v} for Amplitude");
                    break;
                case "/frequency/pan":
                    _targetFrequency = v;
                    HapticController.clipFrequencyShift = v;
                    //Debug.Log($"Received value {v} for Frequency");
                    break;
                case "/emphasis/pan":
                    if (v > 0)
                    {
                        transientValueText.text = v.ToString();
                        HapticPatterns.PlayEmphasis(v, 1.0f);
                        Invoke(nameof(ContinueHaptic),0.02f);
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
