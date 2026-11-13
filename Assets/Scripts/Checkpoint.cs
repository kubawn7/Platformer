using UnityEngine;

public class Checkpoint : MonoBehaviour
{
    [Header("Ustawienia Stacji Dokuj¹cej")]
    [Tooltip("O ile wy¿ej nad punktem stacji ma siê pojawiæ gracz po odrodzeniu")]
    public Vector3 spawnOffset = new Vector3(0f, 1.5f, 0f);
    public Color activeColor = Color.cyan;

    private bool _isActivated = false;
    private MeshRenderer _meshRenderer;

    void Start()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_isActivated) return;

        Movement player = other.GetComponent<Movement>();
        if (player != null)
        {
            _isActivated = true;

            // Zapisujemy pozycjê stacji + lekkie przesuniêcie w górê, ¿eby kapsu³a nie wbi³a siê w pod³ogê
            player.SetCheckpoint(transform.position + spawnOffset);

            // Zmieniamy kolor stacji na znak, ¿e zosta³a aktywowana
            if (_meshRenderer != null)
            {
                _meshRenderer.material.color = activeColor;
            }

            Debug.Log("Stacja Dokuj¹ca aktywowana!");
        }
    }
}