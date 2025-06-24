using Interhaptics;
using Lofelt.NiceVibrations;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public static class HapticFileManager
{
    public static void SaveHapticsDataToPersistentStorage(List<HapticPreviewData> hapticDataList, string filePath)
    {
        try
        {
            string json = JsonUtility.ToJson(new HapticDataListWrapper(hapticDataList));
            File.WriteAllText(filePath, json);
            Debug.Log("Haptic data saved successfully.");
        }
        catch (Exception e)

        {
            Debug.LogError("Failed to save haptic data: " + e.Message);
        }
    }

    public static List<HapticPreviewData> LoadHapticsDataFromFile(string filePath)
    {
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            HapticDataListWrapper wrapper = JsonUtility.FromJson<HapticDataListWrapper>(json);
            return wrapper.hapticDataList;
        }

        Debug.LogWarning("No haptic data found at " + filePath);
        return new List<HapticPreviewData>();
    }

    public static void LoadAndParseHapticFile(string path, string type, ref HapticClip hapticClip, ref HapticMaterial hapticMaterial)
    {
        if (!File.Exists(path))
        {
            Debug.LogError("Haptic file not found: " + path);
            return;
        }

        string json = File.ReadAllText(path);

        if (type == ".haptic")
        {
            hapticClip = NiceVibrationsNative.JsonToHapticClip(Encoding.UTF8.GetBytes(json));
            hapticClip.name = Path.GetFileNameWithoutExtension(path);
            Debug.Log("Haptic data loaded: " + hapticClip.json);
        }
        else if (type == ".haps")
        {
            hapticMaterial = HapticMaterial.CreateInstanceFromString(json);
        } 
    }
}
