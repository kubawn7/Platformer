using System.Collections;
using UnityEngine;

public class PhasingPlatform : MonoBehaviour
{
    [Header("Czasy dzia³ania (w sekundach)")]
    [Tooltip("Ile czasu gracz mo¿e staæ na platformie, zanim zniknie")]
    public float timeBeforeDisappear = 0.8f;
    [Tooltip("Po jakim czasie platforma pojawia siê ponownie")]
    public float respawnTime = 2.5f;

    [Header("Kolory stanu i Emisji")]
    public Color warningColor = Color.red;
    [Tooltip("Ile razy mocniej zaœwieci materia³ po nadepniêciu")]
    public float emissionMultiplier = 4.5f;
    [Tooltip("Czy rozb³ysk emisji ma u¿yæ oryginalnego koloru œwiecenia (false = u¿yje warningColor)")]
    public bool keepOriginalEmissionHue = true;

    private MeshRenderer _meshRenderer;
    private Collider _collider;
    private Material _platformMaterial;

    private Color _originalColor;
    private Color _baseEmissionColor;
    private bool _hasEmission;
    private bool _isTriggered = false;

    // Opcjonalna referencja do kontenera silników z HoverThrusterRig
    private Transform _thrustersContainer;

    private static readonly int EmissionColorID = Shader.PropertyToID("_EmissionColor");

    void Start()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        _collider = GetComponent<Collider>();
        _thrustersContainer = transform.Find("_Thrusters");

        if (_meshRenderer != null)
        {
            // Tworzy indywidualn¹ instancjê materia³u tylko dla tej platformy
            _platformMaterial = _meshRenderer.material;
            _originalColor = _platformMaterial.color;

            if (_platformMaterial.HasProperty(EmissionColorID))
            {
                _platformMaterial.EnableKeyword("_EMISSION");
                _baseEmissionColor = _platformMaterial.GetColor(EmissionColorID);
                _hasEmission = true;
            }
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!_isTriggered && collision.gameObject.GetComponent<Movement>() != null)
        {
            StartCoroutine(PhaseOutRoutine());
        }
    }

    private IEnumerator PhaseOutRoutine()
    {
        _isTriggered = true;

        // 1. Ostrze¿enie – zmiana koloru i podbicie Emission po nadepniêciu
        if (_platformMaterial != null)
        {
            _platformMaterial.color = warningColor;

            if (_hasEmission)
            {
                Color boostBase = (keepOriginalEmissionHue && _baseEmissionColor.maxColorComponent > 0.05f)
                    ? _baseEmissionColor
                    : warningColor;

                _platformMaterial.SetColor(EmissionColorID, boostBase * emissionMultiplier);
            }
        }

        yield return new WaitForSeconds(timeBeforeDisappear);

        // 2. Znikniêcie – wy³¹czamy wygl¹d, kolizjê oraz ewentualne silniki pod spodem
        if (_meshRenderer != null) _meshRenderer.enabled = false;
        if (_collider != null) _collider.enabled = false;
        if (_thrustersContainer != null) _thrustersContainer.gameObject.SetActive(false);

        yield return new WaitForSeconds(respawnTime);

        // 3. Powrót – w³¹czamy z powrotem i przywracamy zarówno kolor, jak i bazow¹ emisjê
        if (_meshRenderer != null) _meshRenderer.enabled = true;
        if (_collider != null) _collider.enabled = true;
        if (_thrustersContainer != null) _thrustersContainer.gameObject.SetActive(true);

        if (_platformMaterial != null)
        {
            _platformMaterial.color = _originalColor;

            if (_hasEmission)
            {
                _platformMaterial.SetColor(EmissionColorID, _baseEmissionColor);
            }
        }

        _isTriggered = false;
    }
}