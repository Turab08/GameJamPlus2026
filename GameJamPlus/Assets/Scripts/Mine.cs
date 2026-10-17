using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

public class Mine : MonoBehaviour
{
     public float MineTime = 5f;
    
  
    
    


    private void OnCollisionEnter2D(Collision2D collision)
    {
        
            if (MineTime > 0)
            {
                Debug.Log("Timed mine collided with the player!");
                Destroy(gameObject,5f);
            }
            else
            {
                Debug.Log("Normal mine collided with the player!");
                Destroy(gameObject);
            }
            
        
    }


}
