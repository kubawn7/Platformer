using UnityEngine;

public class Hazard : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        Movement player = other.GetComponent<Movement>();
        if (player != null)
        {
            player.Respawn();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        Movement player = collision.gameObject.GetComponent<Movement>();
        if (player != null)
        {
            player.Respawn();
        }
    }
}
