using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class BreakableObject : MonoBehaviour
{
    public enum BreakType
    {
        Floor_GroundPound,       // Tylko Ground Pound
        Wall_Dash,               // Tylko Dash
        Any_DashOrGroundPound    // Zarówno Dash, jak i Ground Pound
    }

    [Header("Ustawienia zniszczenia")]
    public BreakType breakType = BreakType.Wall_Dash;
    public bool bounceOnBreak = false;
    public float bounceForce = 10.0f;

    [Header("1. Efekt Particle / VFX (np. z Asset Store)")]
    public GameObject destroyParticlesPrefab;
    public float particlesScale = 1.5f;

    [Header("2. Efekt rozpadu na bryły 3D (Cyber-Shatter)")]
    public int debrisCount = 14;
    public float debrisSize = 0.35f;
    public float explosionForce = 8.0f;
    public Material debrisMaterial;

    [Header("3. Hitstop i Trzęsienie Ekranu (Screen Shake)")]
    public float hitStopDuration = 0.04f;
    public float screenShakeForce = 1.2f;

    private Collider _collider;
    private MeshRenderer _renderer;
    private bool _isBroken = false;

    void Start()
    {
        _collider = GetComponent<Collider>();
        _renderer = GetComponent<MeshRenderer>();
    }

    // Wykrywa uderzenie w biegu / z daleka
    private void OnCollisionEnter(Collision collision)
    {
        TryBreak(collision);
    }

    // Wykrywa odpalenie Dasha, gdy gracz już stoi przytulony do obiektu
    private void OnCollisionStay(Collision collision)
    {
        TryBreak(collision);
    }

    private void TryBreak(Collision collision)
    {
        if (_isBroken) return;

        Movement player = collision.gameObject.GetComponent<Movement>();
        if (player == null) return;

        bool hitByPound = (breakType == BreakType.Floor_GroundPound || breakType == BreakType.Any_DashOrGroundPound)
                          && player.IsGroundPounding;

        bool hitByDash = (breakType == BreakType.Wall_Dash || breakType == BreakType.Any_DashOrGroundPound)
                         && player.IsDashing;

        if (hitByPound || hitByDash)
        {
            StartCoroutine(BreakRoutine(player, hitByDash));
        }
    }

    private IEnumerator BreakRoutine(Movement player, bool brokenByDash)
    {
        _isBroken = true;

        // Zapamiętujemy kierunek gracza przed zderzeniem, aby ściana go nie wyhamowała
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        Vector3 savedDashVelocity = player.transform.forward * player.dashVelocity;

        // 1. Natychmiast wyłączamy kolizję i siatkę bloku
        if (_collider != null) _collider.enabled = false;
        if (_renderer != null) _renderer.enabled = false;

        // 2. Opcjonalne podbicie gracza po rozbiciu podłogi Ground Poundem
        if (!brokenByDash && bounceOnBreak)
        {
            player.Bounce(bounceForce);
        }
        else if (brokenByDash && playerRb != null)
        {
            // Przywracamy pęd Dasha, żeby robocik gładko przeleciał przez zburzoną ścianę
            playerRb.linearVelocity = new Vector3(savedDashVelocity.x, 0f, savedDashVelocity.z);
        }

        // 3. Tworzymy efekt Particle / VFX w miejscu zniszczonego obiektu
        if (destroyParticlesPrefab != null)
        {
            GameObject vfx = Instantiate(destroyParticlesPrefab, transform.position, Quaternion.identity);
            vfx.transform.localScale = Vector3.one * particlesScale;
            Destroy(vfx, 3.0f);
        }

        // 4. Generujemy fizyczne odłamki 3D rozpryskujące się na boki
        SpawnDebrisCubes();

        // 5. Hitstop
        if (hitStopDuration > 0f)
        {
            Time.timeScale = 0.05f;
            yield return new WaitForSecondsRealtime(hitStopDuration);
            Time.timeScale = 1.0f;
        }

        // 6. Trzęsienie ekranu
        CinemachineImpulseSource impulse = player.GetComponent<CinemachineImpulseSource>();
        if (impulse == null)
        {
            impulse = Object.FindFirstObjectByType<CinemachineImpulseSource>();
        }

        if (impulse != null && screenShakeForce > 0f)
        {
            impulse.GenerateImpulseWithForce(screenShakeForce);
        }

        Destroy(gameObject);
    }

    private void SpawnDebrisCubes()
    {
        Material matToUse = debrisMaterial != null ? debrisMaterial : (_renderer != null ? _renderer.sharedMaterial : null);
        Vector3 boundsSize = transform.localScale;

        for (int i = 0; i < debrisCount; i++)
        {
            GameObject piece = GameObject.CreatePrimitive(PrimitiveType.Cube);

            Vector3 randomOffset = new Vector3(
                Random.Range(-boundsSize.x * 0.4f, boundsSize.x * 0.4f),
                Random.Range(-boundsSize.y * 0.4f, boundsSize.y * 0.4f),
                Random.Range(-boundsSize.z * 0.4f, boundsSize.z * 0.4f)
            );

            piece.transform.position = transform.position + transform.rotation * randomOffset;
            piece.transform.localScale = Vector3.one * (debrisSize * Random.Range(0.7f, 1.3f));
            piece.transform.rotation = Random.rotation;

            if (matToUse != null)
            {
                piece.GetComponent<MeshRenderer>().sharedMaterial = matToUse;
            }

            Rigidbody rb = piece.AddComponent<Rigidbody>();
            rb.mass = 0.2f;
            Vector3 explosionDir = (piece.transform.position - transform.position).normalized + Vector3.up * 0.5f;
            rb.AddForce(explosionDir * explosionForce, ForceMode.Impulse);
            rb.AddTorque(Random.insideUnitSphere * explosionForce, ForceMode.Impulse);

            piece.AddComponent<DebrisShrinker>();
        }
    }
}

public class DebrisShrinker : MonoBehaviour
{
    private float _lifeTime = 1.2f;
    private float _timer = -0.6f;
    private Vector3 _initialScale;

    void Start()
    {
        _initialScale = transform.localScale;
    }

    void Update()
    {
        _timer += Time.deltaTime;
        if (_timer > 0f)
        {
            float progress = _timer / _lifeTime;
            transform.localScale = Vector3.Lerp(_initialScale, Vector3.zero, progress);

            if (progress >= 1f)
            {
                Destroy(gameObject);
            }
        }
    }
}