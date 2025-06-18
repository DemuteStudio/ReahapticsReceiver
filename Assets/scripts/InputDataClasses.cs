using System;
using System.Collections.Generic;

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