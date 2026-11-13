using TMPro;
using UnityEngine;

public class CoinManager : MonoBehaviour
{
    public static CoinManager Instance { get; private set; }

    [Header("Elementy HUD")]
    [Tooltip("Przeci¹gnij tutaj tekst TextMeshPro z licznikiem")]
    public TextMeshProUGUI coinText;
    [Tooltip("Obiekt ikony lub ca³ego panelu, który ma podskoczyæ przy zebraniu")]
    public RectTransform hudContainer;

    [Header("Animacja HUD przy zebraniu")]
    public float punchScale = 1.25f;
    public float scaleReturnSpeed = 8.0f;

    private int _coinCount = 0;
    private Vector3 _defaultScale = Vector3.one;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        if (hudContainer != null)
            _defaultScale = hudContainer.localScale;

        UpdateHUD();
    }

    void Update()
    {
        // P³ynny powrót licznika do normalnego rozmiaru po "podbiciu"
        if (hudContainer != null)
        {
            hudContainer.localScale = Vector3.Lerp(
                hudContainer.localScale,
                _defaultScale,
                scaleReturnSpeed * Time.deltaTime
            );
        }
    }

    public void AddCoin(int amount = 1)
    {
        _coinCount += amount;
        UpdateHUD();

        // Efekt powiêkszenia licznika w momencie zebrania monety
        if (hudContainer != null)
        {
            hudContainer.localScale = _defaultScale * punchScale;
        }
    }

    private void UpdateHUD()
    {
        if (coinText != null)
        {
            // Format "000" wyœwietla np. 001, 012, co wygl¹da bardziej cybernetycznie
            coinText.text = _coinCount.ToString("000");
        }
    }
}
