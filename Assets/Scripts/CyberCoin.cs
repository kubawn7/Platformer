using UnityEngine;

public class CyberCoin : MonoBehaviour
{
    [Header("Wartoœæ i Animacja")]
    public int value = 1;
    public float rotationSpeed = 120.0f;
    public float floatAmplitude = 0.15f;
    public float floatFrequency = 3.0f;

    [Header("Efekt po zebraniu (Opcjonalnie)")]
    [Tooltip("Mo¿esz tu podpi¹æ prefab krótkiego wybuchu cz¹steczek")]
    public ParticleSystem pickupParticlesPrefab;

    private Vector3 _startPos;

    void Start()
    {
        _startPos = transform.position;
    }

    void Update()
    {
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime, Space.World);
        float newY = _startPos.y + Mathf.Sin(Time.time * floatFrequency) * floatAmplitude;
        transform.position = new Vector3(_startPos.x, newY, _startPos.z);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.GetComponent<Movement>() != null)
        {
            if (CoinManager.Instance != null)
            {
                CoinManager.Instance.AddCoin(value);
            }

            if (pickupParticlesPrefab != null)
            {
                Instantiate(pickupParticlesPrefab, transform.position, Quaternion.identity);
            }

            Destroy(gameObject);
        }
    }
}
