using UnityEngine;

public class HapticVisualizerIntegration : MonoBehaviour
{
    [SerializeField] private HapticWaveformVisualizer visualizer;
    [SerializeField] private OSCReaperContinuesReceiver hapticReceiver;

    [Header("Auto-Play Settings")]
    [SerializeField] private bool autoPlayOnLoad = true;
    [SerializeField] private bool syncWithPlayback = true;

    private bool isTracking = false;
    private float trackingTime = 0f;
    private float lastExternalUpdateTime = 0f;
    private const float EXTERNAL_UPDATE_TIMEOUT = 0.1f; // If no external update for 0.1s, resume auto-increment

    private void Start()
    {
        if (visualizer == null)
        {
            visualizer = GetComponent<HapticWaveformVisualizer>();
        }
    }
    
    /// Load and display haptic data
    public void LoadHapticData(string jsonData)
    {
        if (visualizer == null)
        {
            Debug.LogError("Visualizer not assigned");
            return;
        }

        Debug.Log($"[HapticVisualizerIntegration] LoadHapticData called - this will reset the visualizer");

        visualizer.LoadHapticData(jsonData);

        // Only auto-play if we don't have a receiver controlling playback
        if (autoPlayOnLoad && hapticReceiver == null)
        {
            visualizer.Play();
        }
    }
    
    public void StartTracking()
    {
        isTracking = true;
        trackingTime = 0f;
        lastExternalUpdateTime = 0f;

        if (visualizer != null)
        {
            // Enable cursor but let external updates control position
            visualizer.Play();
        }

        Debug.Log("[HapticVisualizerIntegration] Started tracking playback");
    }
    
    public void StopTracking()
    {
        Debug.LogWarning("[HapticVisualizerIntegration] StopTracking called - this hides the cursor!");
        isTracking = false;
        if (visualizer != null)
        {
            visualizer.Stop();
        }
    }
    
    public void UpdateTime(float time)
    {
        lastExternalUpdateTime = Time.time;
        trackingTime = time;

        if (visualizer != null)
        {
            visualizer.SetTime(time);
        }
    }

    private void Update()
    {
        // Only auto-increment if tracking, no recent external updates, and sync enabled
        bool hasRecentExternalUpdate = (Time.time - lastExternalUpdateTime) < EXTERNAL_UPDATE_TIMEOUT;

        if (isTracking && syncWithPlayback && !hasRecentExternalUpdate)
        {
            trackingTime += Time.deltaTime;
            if (visualizer != null)
            {
                visualizer.SetTime(trackingTime);
            }
        }
    }
    
    public void Clear()
    {
        if (visualizer != null)
        {
            visualizer.Clear();
        }
        isTracking = false;
        trackingTime = 0f;
    }
}
