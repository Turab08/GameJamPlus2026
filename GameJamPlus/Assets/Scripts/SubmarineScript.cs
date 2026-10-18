using UnityEngine;

public class SubmarineScript : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Mine"))
        {   
            GameManager.Instance.OnGameOver();
        }
    }
}
