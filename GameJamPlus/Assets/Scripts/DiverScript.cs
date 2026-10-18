using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DiverScript : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Bubble"))
        {   
            GameManager.Instance.OnGameOver();
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Mine"))
        {   
            GameManager.Instance.OnGameOver();
        }
    }
}
