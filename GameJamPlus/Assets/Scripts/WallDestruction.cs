using System.Collections;
using UnityEngine;

public class WallDestruction : MonoBehaviour
{
    [SerializeField] private GameObject destructionParticle;
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Submarine"))
        {
            Destruction();
        }
    }

    private void Destruction()
    {
        GameObject particle = Instantiate(destructionParticle, transform.position, Quaternion.identity);
        ParticleSystem particleSystem = particle.GetComponent<ParticleSystem>();
        particleSystem.Play();
        Destroy(particle, 1f);
        Destroy(gameObject);
    }
}
