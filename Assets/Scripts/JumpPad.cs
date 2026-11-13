using UnityEngine;
using Unity.Cinemachine;

public class JumpPad : MonoBehaviour
{
    [Header("Ustawienia wyrzutni")]
    public float bounceForce = 18.0f;

    [Header("1. Rozbłysk Emission (HDR)")]
    [Tooltip("Ile razy mocniej zaświeci materiał w momencie wybicia")]
    public float emissionMultiplier = 4.5f;
    [Tooltip("Szybkość powrotu jasności do normy")]
    public float emissionFadeSpeed = 5.0f;

    [Header("2. Sprężyste ugięcie (Squash & Dip)")]
    [Tooltip("Mnożnik skali przy odbiciu (X/Z szersze, Y lekko spłaszczone)")]
    public Vector3 punchScaleMultiplier = new Vector3(1.18f, 0.75f, 1.18f);
    [Tooltip("O ile jednostek pad ugina się w dół pod ciężarem robota")]
    public float dipDistance = 0.15f;
    [Tooltip("Szybkość sprężystego powrotu do kształtu początkowego")]
    public float springReturnSpeed = 9.0f;

    [Header("3. Dodatkowy VFX i Wstrząs Kamery")]
    [Tooltip("Przeciągnij tu np. swój prefab SonicBoomVFX lub pierścień z Asset Store")]
    public GameObject bounceVfxPrefab;
    [Tooltip("Siła lekkiego kopnięcia kamery przy wybiciu (0 = wyłączone)")]
    public float cameraShakeForce = 0.4f;

    private Material _padMaterial;
    private Color _baseEmissionColor;
    private Color _currentEmissionColor;
    private bool _hasEmission;

    private Vector3 _initialScale;
    private Vector3 _initialLocalPos;
    private HoverThrusterRig _thrusterRig;

    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    void Start()
    {
        _initialScale = transform.localScale;
        _initialLocalPos = transform.localPosition;
        _thrusterRig = GetComponent<HoverThrusterRig>();

        // Szuka MeshRenderera na głównym obiekcie lub w jego dzieciach (np. w modelu "default")
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend != null)
        {
            // Tworzy indywidualną instancję materiału, aby rozbłyskał tylko ten jeden Jump Pad
            _padMaterial = rend.material;
            if (_padMaterial.HasProperty(EmissionColorID))
            {
                _padMaterial.EnableKeyword("_EMISSION");
                _baseEmissionColor = _padMaterial.GetColor(EmissionColorID);
                _currentEmissionColor = _baseEmissionColor;
                _hasEmission = true;
            }
        }
    }

    void Update()
    {
        // 1. Płynne wygaszanie rozbłysku Emission z powrotem do wartości bazowej
        if (_hasEmission && _padMaterial != null)
        {
            _currentEmissionColor = Color.Lerp(_currentEmissionColor, _baseEmissionColor, emissionFadeSpeed * Time.deltaTime);
            _padMaterial.SetColor(EmissionColorID, _currentEmissionColor);
        }

        // 2. Sprężysty powrót skali oraz pozycji po ugięciu
        transform.localScale = Vector3.Lerp(transform.localScale, _initialScale, springReturnSpeed * Time.deltaTime);
        transform.localPosition = Vector3.Lerp(transform.localPosition, _initialLocalPos, springReturnSpeed * Time.deltaTime);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.GetComponent<Movement>() != null)
        {
            // Sprawdzamy wszystkie punkty styku z koliderem
            foreach (ContactPoint contact in collision.contacts)
            {
                // contact.normal wskazuje kierunek OD gracza DO Jump Pada.
                // Jeśli gracz wylądował na górze pada, wektor normalny jest skierowany w dół (przeciwnie do transform.up pada).
                // Wartość < -0.5f oznacza, że gracz musiał nadepnąć na górną powierzchnię.
                if (Vector3.Dot(contact.normal, transform.up) < -0.5f)
                {
                    Movement player = collision.gameObject.GetComponent<Movement>();
                    Rigidbody playerRb = collision.gameObject.GetComponent<Rigidbody>();
                    Animator animator = playerRb.GetComponent<Animator>();
                    if (animator != null) animator.SetTrigger("Jump");
                    if (playerRb != null)
                    {
                        Vector3 currentVel = playerRb.linearVelocity;
                        playerRb.linearVelocity = new Vector3(currentVel.x, 0f, currentVel.z);
                        playerRb.AddForce(transform.up * bounceForce, ForceMode.Impulse);
                    }
                    TriggerJumpPad(player);
                    break; // Wybijamy tylko raz i przerywamy pętlę
                }
            }
        }
    }

    private void TriggerJumpPad(Movement player)
    {
        

        // 2. Impuls silniczków HoverThrusterRig (jeśli używasz nowego modelu)
        if (_thrusterRig != null)
        {
            _thrusterRig.Pulse();
        }

        // 3. Natychmiastowy skok jasności Emission
        if (_hasEmission && _padMaterial != null)
        {
            // Jeśli bazowy kolor emisji był zbyt ciemny, używamy jaskrawej zieleni jako bazy błysku
            Color boostBase = (_baseEmissionColor.maxColorComponent > 0.05f) ? _baseEmissionColor : new Color(0.2f, 1f, 0.3f);
            _currentEmissionColor = boostBase * emissionMultiplier;
            _padMaterial.SetColor(EmissionColorID, _currentEmissionColor);
        }

        // 4. Fizyczne ugięcie w dół i spłaszczenie przy uderzeniu
        transform.localScale = new Vector3(
            _initialScale.x * punchScaleMultiplier.x,
            _initialScale.y * punchScaleMultiplier.y,
            _initialScale.z * punchScaleMultiplier.z
        );
        transform.localPosition = _initialLocalPos - transform.up * dipDistance;

        // 5. Wystrzał pierścienia cząsteczek na powierzchni tarczy
        if (bounceVfxPrefab != null)
        {
            GameObject vfx = Instantiate(bounceVfxPrefab, transform.position + transform.up * 0.1f, Quaternion.Euler(90f, 0f, 0f));
            Destroy(vfx, 2.0f);
        }

        // 6. Delikatne drgnięcie kamery przy wybiciu
        if (cameraShakeForce > 0f)
        {
            CinemachineImpulseSource impulse = Object.FindFirstObjectByType<CinemachineImpulseSource>();
            if (impulse != null)
            {
                impulse.GenerateImpulseWithForce(cameraShakeForce);
            }
        }
    }
}