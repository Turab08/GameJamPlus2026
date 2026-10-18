using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DiverScript : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Bubble") || collision.gameObject.CompareTag("Mine"))
        {   
            GameManager.Instance.OnGameOver();
        }
    }
}
