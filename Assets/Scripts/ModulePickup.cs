using UnityEngine;
using UnityEngine.UIElements;

public class ModulePickup:MonoBehaviour
{
    public enum ModuleType { Dash, GroundPound, DoubleJump }

    [Header("Typ odblokowywanego modu³u")]
    public ModuleType moduleToUnlock = ModuleType.Dash;

    [Header("Animacja lewitacji modu³u")]
    public float rotationSpeed = 90.0f;
    public float floatAmplitude = 0.25f;
    public float floatFrequency = 2.0f;

    private Vector3 _startPos;

    void Start()
    {
        _startPos = transform.position;
    }

    void Update()
    {
        // Obracanie modu³u wokó³ w³asnej osi
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);

        // P³ynne unoszenie siê góra-dó³ (funkcja sinus)
        float newY = _startPos.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(_startPos.x, newY, _startPos.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        Movement player = other.GetComponent<Movement>();

        if (player != null)
        {
            if (moduleToUnlock == ModuleType.Dash)
                player.UnlockDashModule();
            else if (moduleToUnlock == ModuleType.GroundPound)
                player.UnlockGroundPoundModule();
            else if (moduleToUnlock == ModuleType.DoubleJump)
                player.UnlockDoubleJumpModule();

            Destroy(gameObject);
        }
    }
}
