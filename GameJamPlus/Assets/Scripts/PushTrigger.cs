using UnityEngine;

public class PushTrigger : MonoBehaviour
{
    float pushForceButton = 10f; // Adjust the force as needed
    
    private void OnTriggerEnter2D(Collider2D collision)
    {
        //Button push interpolation between two end points smoothly
       Debug.Log("Button push trigger activated!");
       PositionPush(pushForceButton);
    }

    public void PositionPush( float pushForce)
    {
        // Apply the push force to the object towards the parent's position
        Vector2 pushDirection = (transform.parent.position - transform.position).normalized;
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.AddForce(pushDirection * pushForce, ForceMode2D.Impulse);

    }
    }
}
