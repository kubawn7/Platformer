using UnityEngine;
using UnityEngine.VFX;

public class LaserBeam : MonoBehaviour
{
    [Header("Stan lasera")]
    public bool isActive = true;

    private Collider[] _colliders;
    private MeshRenderer[] _renderers;
    private VisualEffect[] _vfxEffects;

    void Awake()
    {
        // Automatycznie zbiera wszystkie kolidery, modele i efekty VFX z tego obiektu oraz wszystkich jego dzieci
        _colliders = GetComponentsInChildren<Collider>(true);
        _renderers = GetComponentsInChildren<MeshRenderer>(true);
        _vfxEffects = GetComponentsInChildren<VisualEffect>(true);
    }

    void Start()
    {
        SetLaserActive(isActive);
    }

    public void SetLaserActive(bool state)
    {
        isActive = state;

        // 1. W³¹cza/wy³¹cza wszystkie kolidery wewn¹trz lasera lub bramy (równie¿ te u¿ywane przez skrypt Hazard!)
        foreach (Collider col in _colliders)
        {
            if (col != null) col.enabled = isActive;
        }

        // 2. W³¹cza/wy³¹cza widocznoœæ wszystkich cylindrów Laser
        foreach (MeshRenderer rend in _renderers)
        {
            if (rend != null) rend.enabled = isActive;
        }

        // 3. P³ynnie w³¹cza/wygasza wszystkie efekty Visual Effect
        foreach (VisualEffect vfx in _vfxEffects)
        {
            if (vfx != null)
            {
                if (isActive) vfx.Play();
                else vfx.Stop();
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isActive) return;

        Movement player = other.GetComponent<Movement>();
        if (player != null)
        {
            player.Respawn();
        }
    }
}