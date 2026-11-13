using UnityEngine;

public class LaserSwitch : MonoBehaviour
{
    [Header("Powi¹zane Lasery lub Grupy (np. ca³y LaserGate)")]
    [Tooltip("Przeci¹gnij tutaj ca³y obiekt LaserGate, pojedynczy LaserBeam lub sam Laser")]
    public GameObject[] connectedLaserObjects;

    [Header("Typ przycisku")]
    [Tooltip("Jeœli zaznaczone, gracz musi uderzyæ przycisk Ground Poundem")]
    public bool requiresGroundPound = false;

    [Header("Wygl¹d przycisku")]
    public Transform buttonTop;
    public float pressDepth = 0.1f;
    public Color activatedColor = Color.cyan;

    private bool _isPressed = false;
    private MeshRenderer _renderer;

    void Start()
    {
        if (buttonTop == null) buttonTop = transform;
        _renderer = buttonTop.GetComponent<MeshRenderer>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isPressed) return;

        Movement player = other.GetComponent<Movement>();
        if (player != null)
        {
            if (requiresGroundPound && !player.IsGroundPounding)
            {
                return;
            }

            ActivateSwitch();
        }
    }

    private void ActivateSwitch()
    {
        _isPressed = true;

        // 1. Wizualne wciœniêcie guzika i zmiana koloru
        buttonTop.localPosition -= new Vector3(0f, pressDepth, 0f);
        if (_renderer != null)
        {
            _renderer.material.color = activatedColor;
        }

        // 2. Automatyczne wyszukanie i wy³¹czenie WSZYSTKICH skryptów LaserBeam wewn¹trz podpiêtych obiektów
        if (connectedLaserObjects == null) return;

        foreach (GameObject targetObj in connectedLaserObjects)
        {
            if (targetObj != null)
            {
                // Przeszukuje sam obiekt oraz wszystkie jego dzieci i wnuki (np. LaserGate -> LaserBeam -> Laser)
                LaserBeam[] beamsInGroup = targetObj.GetComponentsInChildren<LaserBeam>(true);
                foreach (LaserBeam beam in beamsInGroup)
                {
                    beam.SetLaserActive(false);
                }
            }
        }
    }

    // Rysuje w edytorze linie od przycisku do podpiêtych bram laserowych
    private void OnDrawGizmos()
    {
        if (connectedLaserObjects == null) return;

        Gizmos.color = _isPressed ? Color.cyan : Color.red;
        foreach (GameObject targetObj in connectedLaserObjects)
        {
            if (targetObj != null)
            {
                Gizmos.DrawLine(transform.position, targetObj.transform.position);
            }
        }
    }
}