using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class HapticWaveformVisualizer : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Leave empty to use this GameObject as the container")]
    [SerializeField] private RectTransform waveformContainer;

    [Header("Waveform Settings")]
    [SerializeField] private int barCount = 64;
    [SerializeField] private Color amplitudeColor = new Color(0.2f, 0.8f, 1f, 1f);
    [SerializeField] private Color frequencyColor = new Color(1f, 0.5f, 0.2f, 1f);
    [SerializeField] private Color emphasisColor = new Color(1f, 1f, 0.2f, 1f); // Yellow for emphasis
    [SerializeField] private float horizontalPadding = 20f;
    [SerializeField] private float barSpacingRatio = 0.2f; // Spacing as a ratio of bar width (0.2 = 20%)
    [SerializeField] private float maxHeight = 600f;
    [SerializeField] private float emphasisMarkerWidth = 6f;
    [SerializeField] private bool showEmphasisPoints = true;

    // Dynamically calculated
    private float barWidth;
    private float barSpacing;

    [Header("Animation Settings")]
    [SerializeField] private bool animate = true;
    [SerializeField] private float animationSpeed = 1f;
    [SerializeField] private bool smoothTransitions = true;
    [SerializeField] private float smoothSpeed = 10f;

    [Header("Playback Cursor")]
    [SerializeField] private bool showPlaybackCursor = true;
    [SerializeField] private Color cursorColor = Color.yellow;
    [SerializeField] private float cursorWidth = 4f;

    private List<Image> amplitudeBars = new List<Image>();
    private List<Image> frequencyBars = new List<Image>();
    private List<float> targetAmplitudes = new List<float>();
    private List<float> targetFrequencies = new List<float>();
    private List<float> currentAmplitudes = new List<float>();
    private List<float> currentFrequencies = new List<float>();
    private List<Image> emphasisMarkers = new List<Image>();

    private Image playbackCursor;
    private HapticInputData hapticData;
    private float duration = 0f;
    private float currentTime = 0f;
    private bool isPlaying = false;
    private bool isExternallyControlled = false;
    private Sprite whiteSprite;
    private string lastLoadedJsonData = "";
    private float lastKnownContainerWidth = 0f;

    private void Awake()
    {
        if (waveformContainer == null)
        {
            waveformContainer = GetComponent<RectTransform>();
        }

        InitializeWaveform();
    }

    private void InitializeWaveform()
    {
        Debug.LogWarning($"[HapticWaveformVisualizer] InitializeWaveform called at {Time.time:F3}! This destroys all bars and cursor!");

        // Create shared white sprite for all UI elements
        if (whiteSprite == null)
        {
            Texture2D tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, Color.white);
            tex.Apply();
            whiteSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        }

        // Clear existing bars and cursor
        foreach (Transform child in waveformContainer)
        {
            Destroy(child.gameObject);
        }
        amplitudeBars.Clear();
        frequencyBars.Clear();
        targetAmplitudes.Clear();
        targetFrequencies.Clear();
        currentAmplitudes.Clear();
        currentFrequencies.Clear();
        emphasisMarkers.Clear();
        playbackCursor = null; // Cursor was destroyed too

        // Calculate dynamic bar width and spacing to fill container
        float containerWidth = waveformContainer.rect.width;
        float availableWidth = containerWidth - (2f * horizontalPadding);

        barWidth = availableWidth / (barCount + barSpacingRatio * (barCount - 1));
        barSpacing = barWidth * barSpacingRatio;

        lastKnownContainerWidth = containerWidth;

        Debug.Log($"[HapticWaveformVisualizer] Container width: {containerWidth:F1}, Available: {availableWidth:F1}, BarWidth: {barWidth:F2}, BarSpacing: {barSpacing:F2}");

        // Create bars first
        float totalWidth = (barWidth + barSpacing) * barCount - barSpacing; // No spacing after last bar
        float startX = -totalWidth / 2f;

        for (int i = 0; i < barCount; i++)
        {
            float xPos = startX + (barWidth + barSpacing) * i + barWidth / 2f;

            // Create amplitude bar (background)
            GameObject ampBar = CreateBar("AmpBar_" + i, xPos, amplitudeColor, 1.0f);
            amplitudeBars.Add(ampBar.GetComponent<Image>());

            // Create frequency bar (foreground)
            GameObject freqBar = CreateBar("FreqBar_" + i, xPos, frequencyColor, 0.6f);
            frequencyBars.Add(freqBar.GetComponent<Image>());

            targetAmplitudes.Add(0f);
            targetFrequencies.Add(0f);
            currentAmplitudes.Add(0f);
            currentFrequencies.Add(0f);
        }

        // Create playback cursor AFTER bars so it renders on top
        if (showPlaybackCursor)
        {
            GameObject cursorObj = new GameObject("PlaybackCursor");
            cursorObj.transform.SetParent(waveformContainer, false);
            playbackCursor = cursorObj.AddComponent<Image>();
            playbackCursor.sprite = whiteSprite;
            playbackCursor.color = cursorColor;
            playbackCursor.raycastTarget = false; // Disable raycasts for performance

            RectTransform cursorRect = playbackCursor.rectTransform;
            cursorRect.anchorMin = new Vector2(0.5f, 0);
            cursorRect.anchorMax = new Vector2(0.5f, 1);
            cursorRect.pivot = new Vector2(0.5f, 0.5f);
            cursorRect.sizeDelta = new Vector2(cursorWidth, 0);
            cursorRect.anchoredPosition = Vector2.zero;

            // Ensure cursor is the last child (renders on top)
            cursorObj.transform.SetAsLastSibling();

            playbackCursor.gameObject.SetActive(false);

            Debug.Log($"[HapticWaveformVisualizer] Created cursor with color {cursorColor}");
        }

        Debug.Log($"[HapticWaveformVisualizer] Created {amplitudeBars.Count} amplitude bars and {frequencyBars.Count} frequency bars");
    }

    private GameObject CreateBar(string name, float xPos, Color color, float alpha)
    {
        GameObject bar = new GameObject(name);
        bar.transform.SetParent(waveformContainer, false);

        Image img = bar.AddComponent<Image>();
        img.sprite = whiteSprite;
        img.raycastTarget = false; // Disable raycasts for performance

        Color barColor = color;
        barColor.a = alpha;
        img.color = barColor;

        RectTransform rect = img.rectTransform;
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(barWidth, 0f);
        rect.anchoredPosition = new Vector2(xPos, 0f);

        return bar;
    }
    
    /// Load haptic data from JSON string
    public void LoadHapticData(string jsonData)
    {
        // Skip if we're loading the exact same data
        if (jsonData == lastLoadedJsonData)
        {
            Debug.Log($"[HapticWaveformVisualizer] Ignoring duplicate LoadHapticData call");
            return;
        }

        lastLoadedJsonData = jsonData;
        Debug.LogWarning($"[HapticWaveformVisualizer] LoadHapticData called at {Time.time:F3} - Loading NEW haptic data!");
        Debug.Log($"[HapticWaveformVisualizer] LoadHapticData called with data length: {jsonData?.Length ?? 0}");

        // Check if container width has changed and reinitialize if needed
        float currentContainerWidth = waveformContainer.rect.width;
        if (Mathf.Abs(currentContainerWidth - lastKnownContainerWidth) > 1f)
        {
            Debug.Log($"[HapticWaveformVisualizer] Container width changed from {lastKnownContainerWidth:F1} to {currentContainerWidth:F1}, reinitializing");
            InitializeWaveform();
        }

        try
        {
            hapticData = JsonConvert.DeserializeObject<HapticInputData>(jsonData);

            if (hapticData == null)
            {
                Debug.LogError("[HapticWaveformVisualizer] hapticData is null after parsing");
                return;
            }

            if (hapticData.amplitude == null)
            {
                Debug.LogError("[HapticWaveformVisualizer] hapticData.amplitude is null");
                return;
            }

            Debug.Log($"[HapticWaveformVisualizer] Parsed successfully! Amplitude count: {hapticData.amplitude.Count}, Frequency count: {hapticData.frequency?.Count ?? 0}");

            // Calculate duration
            duration = 0f;
            if (hapticData.amplitude.Count > 0)
            {
                float ampLastTime = hapticData.amplitude[hapticData.amplitude.Count - 1].time;
                duration = Mathf.Max(duration, ampLastTime);
                Debug.Log($"[HapticWaveformVisualizer] Last amplitude time: {ampLastTime:F3}");
            }
            if (hapticData.frequency != null && hapticData.frequency.Count > 0)
            {
                float freqLastTime = hapticData.frequency[hapticData.frequency.Count - 1].time;
                duration = Mathf.Max(duration, freqLastTime);
                Debug.Log($"[HapticWaveformVisualizer] Last frequency time: {freqLastTime:F3}");
            }

            Debug.Log($"[HapticWaveformVisualizer] Calculated Duration: {duration:F3}s, Bar count: {barCount}");

            UpdateWaveformDisplay();
            CreateEmphasisMarkers();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[HapticWaveformVisualizer] Error loading haptic data: {e.Message}\nStack: {e.StackTrace}");
        }
    }
    
    /// Update the waveform bars based on loaded data
    private void UpdateWaveformDisplay()
    {
        if (hapticData == null)
        {
            Debug.LogError("[HapticWaveformVisualizer] UpdateWaveformDisplay: hapticData is null");
            return;
        }

        Debug.Log($"[HapticWaveformVisualizer] UpdateWaveformDisplay called. Duration: {duration}, BarCount: {barCount}");

        // Sample the data across all bars
        for (int i = 0; i < barCount; i++)
        {
            float normalizedPosition = i / (float)(barCount - 1);
            float timePoint = normalizedPosition * duration;

            // Sample amplitude at this time point
            float amplitude = SampleAmplitude(timePoint);
            targetAmplitudes[i] = amplitude;

            // Sample frequency at this time point
            float frequency = SampleFrequency(timePoint);
            targetFrequencies[i] = frequency;

            if (!smoothTransitions)
            {
                currentAmplitudes[i] = amplitude;
                currentFrequencies[i] = frequency;
                UpdateBarHeight(i);
            }
        }

        Debug.Log($"[HapticWaveformVisualizer] First bar values: amp={targetAmplitudes[0]:F3}, freq={targetFrequencies[0]:F3}");
    }

    private void CreateEmphasisMarkers()
    {
        if (!showEmphasisPoints || hapticData?.amplitude == null || duration <= 0f)
            return;

        // Clear existing emphasis markers
        foreach (var marker in emphasisMarkers)
        {
            if (marker != null)
                Destroy(marker.gameObject);
        }
        emphasisMarkers.Clear();

        float totalWidth = (barWidth + barSpacing) * barCount - barSpacing;
        float startX = -totalWidth / 2f;

        int emphasisCount = 0;

        // Create markers for each emphasis point
        foreach (var amp in hapticData.amplitude)
        {
            if (amp.emphasis != null && (amp.emphasis.amplitude > 0f || amp.emphasis.frequency > 0f))
            {
                // Find which bar this emphasis point corresponds to
                float normalizedTime = amp.time / duration;
                int barIndex = Mathf.RoundToInt(normalizedTime * (barCount - 1));
                barIndex = Mathf.Clamp(barIndex, 0, barCount - 1);

                // Use the exact same position as the corresponding bar
                float xPos = startX + (barWidth + barSpacing) * barIndex + barWidth / 2f;

                // Calculate marker height based on emphasis strength
                float emphasisStrength = Mathf.Max(amp.emphasis.amplitude, amp.emphasis.frequency);
                float markerHeight = emphasisStrength * maxHeight;

                GameObject markerObj = new GameObject($"EmphasisMarker_{emphasisCount}");
                markerObj.transform.SetParent(waveformContainer, false);

                Image markerImg = markerObj.AddComponent<Image>();
                markerImg.sprite = whiteSprite;
                markerImg.color = emphasisColor;
                markerImg.raycastTarget = false;

                RectTransform markerRect = markerImg.rectTransform;
                markerRect.anchorMin = new Vector2(0.5f, 0f);
                markerRect.anchorMax = new Vector2(0.5f, 0f);
                markerRect.pivot = new Vector2(0.5f, 0f);
                markerRect.sizeDelta = new Vector2(barWidth, markerHeight);
                markerRect.anchoredPosition = new Vector2(xPos, 0f);

                // Place emphasis markers on top of all bars (but still behind cursor)
                markerObj.transform.SetAsFirstSibling();

                emphasisMarkers.Add(markerImg);
                emphasisCount++;
            }
        }

        Debug.Log($"[HapticWaveformVisualizer] Created {emphasisCount} emphasis markers");

        // Ensure cursor stays on top after creating emphasis markers
        if (playbackCursor != null)
        {
            playbackCursor.transform.SetAsLastSibling();
        }
    }

    private float SampleAmplitude(float time)
    {
        if (hapticData?.amplitude == null || hapticData.amplitude.Count == 0)
            return 0f;

        // Find surrounding keyframes
        InputAmplitude before = hapticData.amplitude[0];
        InputAmplitude after = hapticData.amplitude[hapticData.amplitude.Count - 1];

        for (int i = 0; i < hapticData.amplitude.Count - 1; i++)
        {
            if (hapticData.amplitude[i].time <= time && hapticData.amplitude[i + 1].time >= time)
            {
                before = hapticData.amplitude[i];
                after = hapticData.amplitude[i + 1];
                break;
            }
        }

        // Linear interpolation
        if (Mathf.Approximately(before.time, after.time))
            return before.amplitude;

        float t = (time - before.time) / (after.time - before.time);
        return Mathf.Lerp(before.amplitude, after.amplitude, t);
    }

    private float SampleFrequency(float time)
    {
        if (hapticData?.frequency == null || hapticData.frequency.Count == 0)
            return 0f;

        // Find surrounding keyframes
        InputFrequency before = hapticData.frequency[0];
        InputFrequency after = hapticData.frequency[hapticData.frequency.Count - 1];

        for (int i = 0; i < hapticData.frequency.Count - 1; i++)
        {
            if (hapticData.frequency[i].time <= time && hapticData.frequency[i + 1].time >= time)
            {
                before = hapticData.frequency[i];
                after = hapticData.frequency[i + 1];
                break;
            }
        }

        // Linear interpolation
        if (Mathf.Approximately(before.time, after.time))
            return before.frequency;

        float t = (time - before.time) / (after.time - before.time);
        return Mathf.Lerp(before.frequency, after.frequency, t);
    }
    
    public void Play()
    {
        isPlaying = true;
        isExternallyControlled = false;
        currentTime = 0f;
        if (playbackCursor != null)
        {
            playbackCursor.gameObject.SetActive(true);
        }
        Debug.Log($"[HapticWaveformVisualizer] Play() called - Auto-playback enabled");
    }
    
    public void Stop()
    {
        Debug.LogWarning($"[HapticWaveformVisualizer] Stop() called at {Time.time:F3} - Hiding cursor!");
        isPlaying = false;
        currentTime = 0f;
        if (playbackCursor != null)
        {
            playbackCursor.gameObject.SetActive(false);
        }
    }

    public void SetTime(float time)
    {
        isExternallyControlled = true;
        isPlaying = true; // Keep cursor visible

        float newTime = Mathf.Clamp(time, 0f, duration);

        // Only update if time changed significantly (> 0.001s = 1ms)
        if (Mathf.Abs(newTime - currentTime) > 0.001f)
        {
            currentTime = newTime;
            UpdatePlaybackCursor();
        }

        // Only activate cursor once, not every frame
        if (playbackCursor != null && !playbackCursor.gameObject.activeSelf)
        {
            playbackCursor.gameObject.SetActive(true);
            Debug.Log("[HapticWaveformVisualizer] Cursor activated");
        }
    }

    private void Update()
    {
        // Only auto-increment time if playing and NOT externally controlled
        if (isPlaying && !isExternallyControlled)
        {
            currentTime += Time.deltaTime * animationSpeed;

            if (currentTime >= duration)
            {
                currentTime = 0f; // Loop
            }

            UpdatePlaybackCursor();
        }

        if (smoothTransitions)
        {
            // Smooth lerp to target values
            for (int i = 0; i < barCount; i++)
            {
                currentAmplitudes[i] = Mathf.Lerp(currentAmplitudes[i], targetAmplitudes[i], Time.deltaTime * smoothSpeed);
                currentFrequencies[i] = Mathf.Lerp(currentFrequencies[i], targetFrequencies[i], Time.deltaTime * smoothSpeed);
                UpdateBarHeight(i);
            }
        }
    }

    private void UpdateBarHeight(int index)
    {
        if (index < 0 || index >= amplitudeBars.Count)
        {
            Debug.LogWarning($"[HapticWaveformVisualizer] UpdateBarHeight: Invalid index {index}");
            return;
        }

        // Update amplitude bar
        float ampHeight = currentAmplitudes[index] * maxHeight;
        amplitudeBars[index].rectTransform.sizeDelta = new Vector2(barWidth, ampHeight);

        // Update frequency bar (slightly smaller for layered effect)
        float freqHeight = currentFrequencies[index] * maxHeight * 0.7f;
        frequencyBars[index].rectTransform.sizeDelta = new Vector2(barWidth, freqHeight);
    }

    private void UpdatePlaybackCursor()
    {
        if (playbackCursor == null)
        {
            Debug.LogWarning("[HapticWaveformVisualizer] UpdatePlaybackCursor called but cursor is NULL!");
            return;
        }

        if (duration <= 0f)
        {
            Debug.LogWarning("[HapticWaveformVisualizer] UpdatePlaybackCursor called but duration is 0!");
            return;
        }

        // Hide cursor if we've reached the end
        if (currentTime >= duration)
        {
            if (playbackCursor.gameObject.activeSelf)
            {
                playbackCursor.gameObject.SetActive(false);
                Debug.Log("[HapticWaveformVisualizer] Hiding cursor - reached end of haptic");
            }
            return;
        }

        // Show cursor if it was hidden
        if (!playbackCursor.gameObject.activeSelf)
        {
            playbackCursor.gameObject.SetActive(true);
        }

        float normalizedTime = currentTime / duration;
        float totalWidth = (barWidth + barSpacing) * barCount - barSpacing; // No spacing after last bar
        float startX = -totalWidth / 2f;
        float xPos = startX + (normalizedTime * totalWidth);

        playbackCursor.rectTransform.anchoredPosition = new Vector2(xPos, 0f);
    }
    
    public void Clear()
    {
        for (int i = 0; i < barCount; i++)
        {
            targetAmplitudes[i] = 0f;
            targetFrequencies[i] = 0f;
            currentAmplitudes[i] = 0f;
            currentFrequencies[i] = 0f;
            UpdateBarHeight(i);
        }
        Stop();
    }

    public float GetDuration() => duration;
    public bool IsPlaying() => isPlaying;
    public float GetCurrentTime() => currentTime;
}
