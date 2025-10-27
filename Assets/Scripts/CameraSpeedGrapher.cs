using UnityEngine;

[DisallowMultipleComponent]
public class CameraSpeedGrapher : MonoBehaviour
{
    [Header("Target")]
    public Transform target;               // Assign captureCamera1 here.

    [Header("Speed Options")]
    [Tooltip("If enabled, projects velocity onto this direction to get a signed speed. If zero-length, falls back to magnitude.")]
    public bool projectOnDirection = true;
    public Vector3 direction = Vector3.forward; // Axis for signed speed (e.g., your motion axis)

    [Tooltip("Clamp negative values to zero (useful if motion is one-way).")]
    public bool clampNonNegative = false;

    [Header("Chart Settings")]
    [Tooltip("Number of samples kept for the sparkline.")]
    [Range(32, 4096)]
    public int capacity = 512;

    [Tooltip("Optional fixed Y range. Disable to auto-scale.")]
    public bool useFixedYRange = false;
    public float yMin = -2f;
    public float yMax =  2f;

    [Tooltip("Update even in Edit Mode (when not playing).")]
    public bool runInEditMode = false;

    // Exposed for other scripts: you can set the “computed v” directly if desired.
    [System.NonSerialized] public float externalSpeed = float.NaN;

    // Readonly current values
    public float CurrentSpeed { get; private set; }
    public float MinSeen { get; private set; } = float.PositiveInfinity;
    public float MaxSeen { get; private set; } = float.NegativeInfinity;

    // Ring buffer
    private float[] _samples;
    private int _head;     // next write index
    private int _count;    // number of valid samples

    private Vector3 _prevPos;
    private bool _initialized;

    void OnEnable()
    {
        if (_samples == null || _samples.Length != capacity)
            _samples = new float[capacity];

        _head = 0;
        _count = 0;
        MinSeen = float.PositiveInfinity;
        MaxSeen = float.NegativeInfinity;

        if (target != null)
        {
            _prevPos = target.position;
            _initialized = true;
        }
        else
        {
            _initialized = false;
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        if (capacity < 32) capacity = 32;
        if (_samples == null || _samples.Length != capacity)
        {
            _samples = new float[capacity];
            _head = 0;
            _count = 0;
        }
    }
#endif

    void Update()
    {
#if UNITY_EDITOR
        if (!Application.isPlaying && !runInEditMode) return;
#endif
        // 仅接受外部速度；如果这一帧没有外部速度则直接跳过（不使用位置差分）
        if (float.IsNaN(externalSpeed)) return;

        float spd = externalSpeed;

        if (clampNonNegative && spd < 0f) spd = 0f;

        CurrentSpeed = spd;
        MinSeen = Mathf.Min(MinSeen, spd);
        MaxSeen = Mathf.Max(MaxSeen, spd);

        AddSample(spd);

        // 重置外部速度槽，下一帧除非再次设置否则会被跳过
        externalSpeed = float.NaN;
    }

    private void AddSample(float v)
    {
        _samples[_head] = v;
        _head = (_head + 1) % capacity;
        if (_count < capacity) _count++;
    }

    /// <summary>
    /// Copy samples into the provided array (oldest to newest). Returns count written.
    /// </summary>
    public int GetSamples(ref float[] dst)
    {
        if (dst == null || dst.Length < _count) dst = new float[_count];
        int n = _count;
        int start = (_head - n + capacity) % capacity;
        for (int i = 0; i < n; i++)
        {
            int idx = (start + i) % capacity;
            dst[i] = _samples[idx];
        }
        return n;
    }

    public (float y0, float y1) GetYRange()
    {
        if (useFixedYRange) return (yMin, yMax);

        // auto-scale with small padding
        float lo = (_count > 0) ? float.PositiveInfinity : 0f;
        float hi = (_count > 0) ? float.NegativeInfinity : 1f;
        int n = _count;
        int start = (_head - n + capacity) % capacity;
        for (int i = 0; i < n; i++)
        {
            int idx = (start + i) % capacity;
            float v = _samples[idx];
            if (v < lo) lo = v;
            if (v > hi) hi = v;
        }

        if (float.IsInfinity(lo) || float.IsInfinity(hi))
            return (0f, 1f);

        float pad = Mathf.Max(1e-3f, (hi - lo) * 0.1f);
        return (lo - pad, hi + pad);
    }
}
