using DG.Tweening;
using UnityEngine;

public class SubmarineMovementScript : MonoBehaviour
{
    [SerializeField] private float speed;

    private Rigidbody2D rb;

    Vector2 movementInput;

    public float maxTiltAngle = 30f;    
    public float rotationDuration = 0.4f; 
    public float velocitySensitivity = 5f; 

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        movementInput = Vector2.zero;

        if (Input.GetKey(KeyCode.W)) movementInput.y = 1;
        if (Input.GetKey(KeyCode.S)) movementInput.y = -1;
        if (Input.GetKey(KeyCode.A)) movementInput.x = -1;
        if (Input.GetKey(KeyCode.D)) movementInput.x = 1;

        movementInput = movementInput.normalized;
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = movementInput * speed;

        if (rb.linearVelocity.magnitude > 0.01f)
        {
            float speedRatio = Mathf.Clamp(rb.linearVelocityY / velocitySensitivity, -1f, 1f);
            
            float targetAngle = speedRatio * maxTiltAngle;
            

            rb.DORotate(targetAngle, 0.1f).SetUpdate(UpdateType.Fixed).SetEase(Ease.OutQuad);
        }
    }
}
