using UnityEngine;

public class SubmarineScript : MonoBehaviour
{
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Mine"))
        {   
            GameManager.Instance.OnGameOver();
        }
    }
}
