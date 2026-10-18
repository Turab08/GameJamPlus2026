using System;
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

    public bool isFacingRight = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        movementInput = Vector2.zero;

        if (Input.GetKey(KeyCode.W)) movementInput.y = 1;
        if (Input.GetKey(KeyCode.S)) movementInput.y = -1;

        if (Input.GetKey(KeyCode.A)) 
        { 
            movementInput.x = -1;
            if (isFacingRight) {
                isFacingRight = false;
                transform.Rotate(new Vector2(0, 180));
            }

        }
        if (Input.GetKey(KeyCode.D)) 
        {
            movementInput.x = 1;
            if (!isFacingRight) {
                isFacingRight = true;
                transform.Rotate(new Vector2(0, 180));
            }
        }

        movementInput = movementInput.normalized;
    }

    private void FixedUpdate()
    {
        rb.linearVelocity = movementInput * speed;

        if (rb.linearVelocity.magnitude > 0.01f)
        {
            float speedRatio = Mathf.Clamp(rb.linearVelocityY / velocitySensitivity, -1f, 1f);
            
            float targetAngle = speedRatio * maxTiltAngle;
            
            if (!isFacingRight) targetAngle = -targetAngle;
            rb.DORotate(targetAngle, 0.1f).SetUpdate(UpdateType.Fixed).SetEase(Ease.OutQuad);

            if (!AudioManager.Instance.submarineSource.isPlaying)
                AudioManager.Instance.PlaySubmarine();
        }
        else
        {
            AudioManager.Instance.StopSubmarine();
        }
    }
}

// using DG.Tweening;
// using UnityEngine;

// public class SubmarineMovementScript : MonoBehaviour
// {
//     [SerializeField] private float speed;

//     private Rigidbody2D rb;

//     Vector2 movementInput;

//     public float maxTiltAngle = 30f;    
//     public float rotationDuration = 0.4f; 
//     public float velocitySensitivity = 5f; 

//     private void Awake()
//     {
//         rb = GetComponent<Rigidbody2D>();
//         DOTween.SetTweensCapacity( 200,50);
//     }

//     private void Update()
//     {
//         movementInput = Vector2.zero;

//         if (Input.GetKey(KeyCode.W)) movementInput.y = 1;
//         if (Input.GetKey(KeyCode.S)) movementInput.y = -1;
//         if (Input.GetKey(KeyCode.A)) movementInput.x = -1;
//         if (Input.GetKey(KeyCode.D)) movementInput.x = 1;

//         movementInput = movementInput.normalized;
//     }

//     private void FixedUpdate()
//     {
//         rb.linearVelocity = movementInput * speed;

//         if (rb.linearVelocity.magnitude > 0.01f)
//         {
//             float speedRatio = Mathf.Clamp(rb.linearVelocityY / velocitySensitivity, -1f, 1f);
            
//             float targetAngle = speedRatio * maxTiltAngle;
            

//             rb.DORotate(targetAngle, 0.1f).SetUpdate(UpdateType.Fixed).SetEase(Ease.OutQuad);
//         }
//     }
// }

