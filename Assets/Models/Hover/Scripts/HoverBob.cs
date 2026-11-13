using UnityEngine;

public class HoverBob : MonoBehaviour
{
    public float amplitude = 0.08f;
    public float frequency = 0.8f;
    public float tiltDegrees = 1.5f;
    public float tiltFrequency = 0.6f;

    [Header("Kołysanie gracza stojącego na platformie")]
    [Tooltip("Mnożnik przechyłu modelu robota na boki")]
    public float playerTiltMultiplier = 1.5f;

    Vector3 startPos;
    Quaternion startRot;
    float phase;

    private BoxCollider _parentCollider;
    private Transform _currentPlayerVisual;
    private Vector3 _playerVisualDefaultLocalPos;
    private bool _hasSavedPlayerDefaultPos;

    void Awake()
    {
        startPos = transform.localPosition;
        startRot = transform.localRotation;
        phase = Random.value * Mathf.PI * 2f;

        // Pobiera BoxCollider z rodzica (MovingPlatform)
        _parentCollider = GetComponentInParent<BoxCollider>();
    }

    void LateUpdate()
    {
        float t = Time.time;
        float bob = Mathf.Sin(t * frequency * Mathf.PI * 2f + phase) * amplitude;
        float tx = Mathf.Sin(t * tiltFrequency * Mathf.PI * 2f + phase) * tiltDegrees;
        float tz = Mathf.Cos(t * tiltFrequency * Mathf.PI * 2f + phase * 1.3f) * tiltDegrees;

        Quaternion tiltRot = Quaternion.Euler(tx, 0f, tz);

        transform.localPosition = startPos + Vector3.up * bob;
        transform.localRotation = startRot * tiltRot;

        SyncPlayerVisualWithBob(bob, tx, tz, tiltRot);
    }

    private void SyncPlayerVisualWithBob(float bob, float tx, float tz, Quaternion tiltRot)
    {
        if (_parentCollider == null) return;

        Transform colTransform = _parentCollider.transform;
        Vector3 boxCenter = colTransform.TransformPoint(
            _parentCollider.center + new Vector3(0f, _parentCollider.size.y * 0.5f + 0.15f, 0f)
        );
        Vector3 halfExtents = Vector3.Scale(_parentCollider.size, colTransform.lossyScale) * 0.48f;
        halfExtents.y = 0.2f;

        Collider[] hits = Physics.OverlapBox(boxCenter, halfExtents, colTransform.rotation);
        Movement foundPlayer = null;

        foreach (var hit in hits)
        {
            foundPlayer = hit.GetComponent<Movement>();
            if (foundPlayer != null) break;
        }

        if (foundPlayer != null && foundPlayer.animator != null)
        {
            Transform visualModel = foundPlayer.animator.transform;

            if (_currentPlayerVisual != visualModel)
            {
                _currentPlayerVisual = visualModel;
                if (!_hasSavedPlayerDefaultPos)
                {
                    _playerVisualDefaultLocalPos = visualModel.localPosition;
                    _hasSavedPlayerDefaultPos = true;
                }
            }

            // Obliczamy różnicę wysokości na powierzchni platformy w miejscu, gdzie stoi gracz (bob + przechył krawędzi)
            Vector3 relPos = foundPlayer.transform.position - colTransform.position;
            relPos.y = 0f;
            float tiltHeightOffset = (tiltRot * relPos).y;

            Vector3 targetLocalPos = _playerVisualDefaultLocalPos + new Vector3(0f, bob + tiltHeightOffset, 0f);
            visualModel.localPosition = Vector3.Lerp(visualModel.localPosition, targetLocalPos, 20f * Time.deltaTime);

            // Pochylamy model 3D robota zgodnie z kątem nachylenia platformy
            Quaternion worldTilt = Quaternion.Euler(tx * playerTiltMultiplier, 0f, tz * playerTiltMultiplier);
            Quaternion playerFacing = Quaternion.Euler(0f, foundPlayer.transform.eulerAngles.y, 0f);
            visualModel.rotation = Quaternion.Slerp(visualModel.rotation, worldTilt * playerFacing, 15f * Time.deltaTime);
        }
        else if (_currentPlayerVisual != null)
        {
            // Gdy gracz zejdzie z platformy, płynnie wracamy modelem 3D do bazowej pozycji i pionu
            _currentPlayerVisual.localPosition = Vector3.Lerp(
                _currentPlayerVisual.localPosition,
                _playerVisualDefaultLocalPos,
                12f * Time.deltaTime
            );
            _currentPlayerVisual.localRotation = Quaternion.Slerp(
                _currentPlayerVisual.localRotation,
                Quaternion.identity,
                12f * Time.deltaTime
            );

            if (Quaternion.Angle(_currentPlayerVisual.localRotation, Quaternion.identity) < 0.1f &&
                Vector3.Distance(_currentPlayerVisual.localPosition, _playerVisualDefaultLocalPos) < 0.005f)
            {
                _currentPlayerVisual.localPosition = _playerVisualDefaultLocalPos;
                _currentPlayerVisual.localRotation = Quaternion.identity;
                _currentPlayerVisual = null;
            }
        }
    }
}