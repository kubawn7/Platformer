using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Universal "hover engines" rig. Add it to any floating object (platform, pad, crate...).
/// It places engines + VFX under the object and drives their intensity.
/// </summary>
public class HoverThrusterRig : MonoBehaviour
{
    public enum Layout { Corners, Ring, Center, Custom }

    [Header("Prefabs")]
    [Tooltip("Optional engine mesh. Pivot = mounting point, nozzle points down.")]
    public GameObject thrusterModel;
    [Tooltip("Particles + light. The particle shape should emit along local +Z.")]
    public GameObject vfxPrefab;

    [Header("Layout")]
    public Layout layout = Layout.Corners;
    [Range(1, 12)] public int ringCount = 4;
    [Range(0.1f, 1f)] public float inset = 0.7f;
    public float thrusterScale = 1f;
    [Tooltip("Distance from the mounting point to the nozzle exit (meters, before scaling).")]
    public float nozzleDepth = 0.28f;
    [Tooltip("Custom layout: nozzle exits in this object's local space.")]
    public Vector3[] customPoints;

    [Header("Thrust")]
    public float idleThrust = 0.6f;
    public float boostThrust = 1.6f;
    public float boostSpeed = 4f;
    public float pulseThrust = 3f;
    public float pulseTime = 0.35f;

    const string ContainerName = "_Thrusters";

    ParticleSystem[] systems;
    float[] baseRates;
    Light[] lights;
    float[] baseIntensity;
    Vector3 lastPos;
    float pulseTimer;

    void Awake()
    {
        CacheEffects();
        lastPos = transform.position;
    }

    void Update()
    {
        if (systems == null) return;
        float dt = Time.deltaTime;
        if (dt <= 0f) return;

        float speed = (transform.position - lastPos).magnitude / dt;
        lastPos = transform.position;

        float thrust = Mathf.Lerp(idleThrust, boostThrust, Mathf.Clamp01(speed / Mathf.Max(0.01f, boostSpeed)));
        if (pulseTimer > 0f)
        {
            pulseTimer -= dt;
            thrust = Mathf.Max(thrust, pulseThrust);
        }
        Apply(thrust);
    }

    /// <summary>Short thrust boost, e.g. when the player is launched.</summary>
    public void Pulse() { pulseTimer = pulseTime; }

    void Apply(float thrust)
    {
        for (int i = 0; i < systems.Length; i++)
        {
            if (systems[i] == null) continue;
            var em = systems[i].emission;
            em.rateOverTimeMultiplier = baseRates[i] * thrust;
        }
        for (int i = 0; i < lights.Length; i++)
            if (lights[i] != null) lights[i].intensity = baseIntensity[i] * thrust;
    }

    void CacheEffects()
    {
        systems = GetComponentsInChildren<ParticleSystem>(true);
        baseRates = new float[systems.Length];
        for (int i = 0; i < systems.Length; i++) baseRates[i] = systems[i].emission.rateOverTimeMultiplier;

        lights = GetComponentsInChildren<Light>(true);
        baseIntensity = new float[lights.Length];
        for (int i = 0; i < lights.Length; i++) baseIntensity[i] = lights[i].intensity;
    }

    public void ApplyJumpPadPreset()
    {
        layout = Layout.Custom;
        thrusterModel = null; // engines are part of the jump pad mesh
        const float d = 0.4243f; // 0.6 * cos(45 deg)
        customPoints = new[]
        {
            new Vector3( d, -0.42f,  d), new Vector3(-d, -0.42f,  d),
            new Vector3( d, -0.42f, -d), new Vector3(-d, -0.42f, -d),
            new Vector3( 0f, -0.56f, 0f)
        };
    }

    [ContextMenu("Preset: Jump Pad (baked engines)")]
    void PresetMenu() { ApplyJumpPadPreset(); }

    [ContextMenu("Rebuild Thrusters")]
    public void Rebuild()
    {
        var old = transform.Find(ContainerName);
        if (old != null) DestroyImmediate(old.gameObject);

        var container = new GameObject(ContainerName).transform;
        container.SetParent(transform, false);
        Vector3 ls = transform.lossyScale;
        // cancels the parent's scale so engines are never stretched
        container.localScale = new Vector3(Inv(ls.x), Inv(ls.y), Inv(ls.z));

        foreach (var p in GetPoints())
        {
            Vector3 mountPos = Vector3.Scale(p, ls);
            Vector3 exitPos = mountPos;

            if (layout != Layout.Custom)
            {
                if (thrusterModel != null)
                {
                    var m = Instantiate(thrusterModel, container);
                    m.name = "Engine";
                    m.transform.localPosition = mountPos;
                    m.transform.localRotation = Quaternion.identity;
                    m.transform.localScale = Vector3.one * thrusterScale;
                }
                exitPos = mountPos + Vector3.down * nozzleDepth * thrusterScale;
            }

            var anchor = new GameObject("VFX_Anchor").transform;
            anchor.SetParent(container, false);
            anchor.localPosition = exitPos;

            if (vfxPrefab != null)
            {
                var v = Instantiate(vfxPrefab, anchor);
                v.transform.localPosition = Vector3.zero;
                // particles emit along local +Z, so aim +Z along the object's down direction
                v.transform.rotation = Quaternion.LookRotation(-transform.up, transform.forward);
            }
        }
        CacheEffects();
    }

    List<Vector3> GetPoints()
    {
        var pts = new List<Vector3>();
        if (layout == Layout.Custom)
        {
            if (customPoints != null) pts.AddRange(customPoints);
            return pts;
        }

        Renderer r = FindBodyRenderer();
        if (r == null)
        {
            Debug.LogWarning("HoverThrusterRig: no Renderer found on " + name, this);
            return pts;
        }

        Bounds b = r.localBounds;
        Transform rt = r.transform;
        float ex = b.extents.x * inset;
        float ez = b.extents.z * inset;
        float y = b.min.y;

        void Add(float x, float z)
        {
            Vector3 world = rt.TransformPoint(new Vector3(b.center.x + x, y, b.center.z + z));
            pts.Add(transform.InverseTransformPoint(world));
        }

        switch (layout)
        {
            case Layout.Corners:
                Add(-ex, -ez); Add(ex, -ez); Add(-ex, ez); Add(ex, ez);
                break;
            case Layout.Center:
                Add(0f, 0f);
                break;
            case Layout.Ring:
                for (int i = 0; i < ringCount; i++)
                {
                    float a = i * Mathf.PI * 2f / ringCount;
                    Add(Mathf.Cos(a) * ex, Mathf.Sin(a) * ez);
                }
                break;
        }
        return pts;
    }

    Renderer FindBodyRenderer()
    {
        var container = transform.Find(ContainerName);
        foreach (var r in GetComponentsInChildren<Renderer>())
        {
            if (r is ParticleSystemRenderer) continue;
            if (container != null && r.transform.IsChildOf(container)) continue;
            return r;
        }
        return null;
    }

    static float Inv(float v) { return Mathf.Abs(v) < 1e-4f ? 1f : 1f / v; }
}
