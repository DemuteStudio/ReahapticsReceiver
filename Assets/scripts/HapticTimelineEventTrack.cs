using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Timeline;

[TrackColor(0.5f,0,0.5f)]
[TrackBindingType(typeof(GameObject))]
[TrackClipType(typeof(HapticEventClip))]
public class HapticTimelineEventTrack : TrackAsset
{
}
