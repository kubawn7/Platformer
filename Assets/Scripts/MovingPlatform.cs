using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class MovingPlatform : MonoBehaviour
{
    [Header("Trasa ruchu")]
    public Vector3 moveOffset = new Vector3(0f, 0f, 8f);
    public float speed = 3.0f;

    // Publiczna w³aœciwoœæ, z której gracz odczyta prêdkoœæ platformy
    public Vector3 CurrentVelocity { get; private set; }

    private Vector3 _startPos;
    private Vector3 _targetPos;
    private Rigidbody _rb;
    private Vector3 _lastPosition;

    void Start()
    {
        _startPos = transform.position;
        _targetPos = _startPos + moveOffset;

        _rb = GetComponent<Rigidbody>();
        _rb.isKinematic = true;
        _rb.interpolation = RigidbodyInterpolation.Interpolate;

        _lastPosition = transform.position;
    }

    void FixedUpdate()
    {
        float distance = moveOffset.magnitude;
        if (distance < 0.01f) return;

        float t = Mathf.PingPong(Time.fixedTime * speed / distance, 1f);
        Vector3 newPos = Vector3.Lerp(_startPos, _targetPos, t);

        // Obliczamy dok³adn¹ prêdkoœæ platformy w tej klatce fizyki
        CurrentVelocity = (newPos - _lastPosition) / Time.fixedDeltaTime;
        _lastPosition = newPos;

        _rb.MovePosition(newPos);
    }

    private void OnDrawGizmosSelected()
    {
        Vector3 start = Application.isPlaying ? _startPos : transform.position;
        Vector3 end = start + moveOffset;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(start, end);
        Gizmos.DrawWireCube(end, transform.localScale);
    }
}