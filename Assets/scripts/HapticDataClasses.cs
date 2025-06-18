using System;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class HapticPreviewData
{
    public string name = "NoHapticSet";
    public string videoPath;
    public string hapticPath;
    public float triggerTime;
    public string type;
}

[Serializable]
public class HapticDataListWrapper
{
    public List<HapticPreviewData> hapticDataList;

    public HapticDataListWrapper(List<HapticPreviewData> hapticDataList)
    {
        this.hapticDataList = hapticDataList;
    }
}
