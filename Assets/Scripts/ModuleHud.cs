using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ModuleHUD : MonoBehaviour
{
    public static ModuleHUD Instance { get; private set; }

    [Header("Ikony Modułów w HUD (Obiekty Image w trybie Filled)")]
    public Image dashIcon;
    public Image groundPoundIcon;
    public Image doubleJumpIcon;

    [Header("Kolory Stanu")]
    public Color lockedColor = new Color(0.2f, 0.25f, 0.35f, 0.35f);
    public Color unlockedDashColor = Color.cyan;
    public Color unlockedPoundColor = new Color(1f, 0.5f, 0f, 1f);
    public Color unlockedJumpColor = new Color(0.2f, 1f, 0.5f, 1f);

    [Header("Komunikat i Animacja")]
    public TextMeshProUGUI notificationText;
    public float punchScale = 1.35f;
    public float scaleReturnSpeed = 6.0f;

    // Flagi pilnujące, by efekt błysku po naładowaniu wykonał się tylko raz
    private bool _dashReady = true;
    private bool _poundReady = true;
    private bool _jumpReady = true;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        SetIconState(dashIcon, lockedColor, 1f);
        SetIconState(groundPoundIcon, lockedColor, 1f);
        SetIconState(doubleJumpIcon, lockedColor, 1f);

        if (notificationText != null)
            notificationText.text = "";
    }

    void Update()
    {
        SmoothReturnScale(dashIcon);
        SmoothReturnScale(groundPoundIcon);
        SmoothReturnScale(doubleJumpIcon);
    }

    public void UnlockModuleUI(ModulePickup.ModuleType type)
    {
        switch (type)
        {
            case ModulePickup.ModuleType.Dash:
                ActivateIcon(dashIcon, unlockedDashColor, "MODUŁ ODBLOKOWANY: AIR DASH");
                break;
            case ModulePickup.ModuleType.GroundPound:
                ActivateIcon(groundPoundIcon, unlockedPoundColor, "MODUŁ ODBLOKOWANY: GROUND POUND");
                break;
            case ModulePickup.ModuleType.DoubleJump:
                ActivateIcon(doubleJumpIcon, unlockedJumpColor, "MODUŁ ODBLOKOWANY: DOUBLE JUMP");
                break;
        }
    }

    // Nowa metoda aktualizująca pasek ładowania (progress od 0.0 do 1.0)
    public void UpdateCooldownUI(ModulePickup.ModuleType type, float progress)
    {
        progress = Mathf.Clamp01(progress);

        switch (type)
        {
            case ModulePickup.ModuleType.Dash:
                UpdateSingleSlot(dashIcon, unlockedDashColor, progress, ref _dashReady);
                break;
            case ModulePickup.ModuleType.GroundPound:
                UpdateSingleSlot(groundPoundIcon, unlockedPoundColor, progress, ref _poundReady);
                break;
            case ModulePickup.ModuleType.DoubleJump:
                UpdateSingleSlot(doubleJumpIcon, unlockedJumpColor, progress, ref _jumpReady);
                break;
        }
    }

    private void UpdateSingleSlot(Image icon, Color fullColor, float progress, ref bool isReadyFlag)
    {
        if (icon == null) return;

        icon.fillAmount = progress;

        if (progress < 1f)
        {
            isReadyFlag = false;
            // Podczas ładowania ikona jest nieco ciemniejsza i lekko przezroczysta
            icon.color = new Color(fullColor.r * 0.5f, fullColor.g * 0.5f, fullColor.b * 0.5f, 0.7f);
        }
        else if (!isReadyFlag)
        {
            // Właśnie się naładowało do 100% -> pełny kolor i lekkie podbicie ikony!
            isReadyFlag = true;
            icon.color = fullColor;
            icon.rectTransform.localScale = Vector3.one * (punchScale * 0.9f);
        }
    }

    private void ActivateIcon(Image icon, Color activeColor, string message)
    {
        if (icon != null)
        {
            icon.fillAmount = 1f;
            icon.color = activeColor;
            icon.rectTransform.localScale = Vector3.one * punchScale;
        }

        if (notificationText != null)
        {
            StopAllCoroutines();
            StartCoroutine(ShowNotificationRoutine(message, activeColor));
        }
    }

    private IEnumerator ShowNotificationRoutine(string message, Color textColor)
    {
        notificationText.color = textColor;
        notificationText.text = message;
        yield return new WaitForSeconds(3.0f);
        notificationText.text = "";
    }

    private void SetIconState(Image icon, Color color, float fill)
    {
        if (icon != null)
        {
            icon.color = color;
            icon.fillAmount = fill;
        }
    }

    private void SmoothReturnScale(Image icon)
    {
        if (icon != null)
        {
            icon.rectTransform.localScale = Vector3.Lerp(
                icon.rectTransform.localScale,
                Vector3.one,
                scaleReturnSpeed * Time.deltaTime
            );
        }
    }
}