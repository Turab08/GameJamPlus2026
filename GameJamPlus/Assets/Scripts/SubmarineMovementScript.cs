using DG.Tweening;
using UnityEngine;

public class SubmarineMovementScript : MonoBehaviour
{
    [SerializeField] private float speed;
    private Rigidbody2D rb;


    public float maxTiltAngle = 30f;    
    public float rotationDuration = 0.4f; 
    public float velocitySensitivity = 5f; 

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        rb.linearVelocity = new Vector2(horizontal, vertical) * speed;
    }

    private void FixedUpdate()
    {
        if (rb.linearVelocity.magnitude > 0.01f)
        {
            float speedRatio = Mathf.Clamp(rb.linearVelocityY / velocitySensitivity, -1f, 1f);
            
            float targetAngle = speedRatio * maxTiltAngle;
            

            rb.DORotate(targetAngle, 0.1f).SetUpdate(UpdateType.Fixed).SetEase(Ease.OutQuad);
        }
    }
}
