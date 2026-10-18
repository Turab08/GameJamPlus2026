using DG.Tweening;
using UnityEngine;

public class SubmarineMovementScript1 : MonoBehaviour
{
    [SerializeField] private float speed;

    private Rigidbody2D rb;

    Vector2 movementInput;

    private bool isFacingRight = true;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Update()
    {
        movementInput = Vector2.zero;

        if (Input.GetKey(KeyCode.UpArrow)) movementInput.y = 1;
        if (Input.GetKey(KeyCode.DownArrow)) movementInput.y = -1;

        if (Input.GetKey(KeyCode.LeftArrow)) 
        { 
            movementInput.x = -1;
            if (isFacingRight) {
                isFacingRight = false;
                transform.Rotate(new Vector2(0, 180));
            }

        }
        if (Input.GetKey(KeyCode.RightArrow)) 
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
    }
}
