using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

public class Mine : MonoBehaviour
{
    public float MineTime = 5f;

    [SerializeField] private GameObject explosionParticle;
    
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (MineTime > 0)
        {
            Destroy(gameObject,5f);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void OnDestroy()
    {
        if (!gameObject.scene.isLoaded) return;

        AudioManager.Instance.PlaySFX(AudioManager.Instance.MineExplosion);

        GameObject particle = Instantiate(explosionParticle, transform.position, Quaternion.identity);
        Destroy(particle, 0.5f);
    }
}
