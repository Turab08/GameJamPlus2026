using UnityEngine;

public class BubbleScript : MonoBehaviour
{
    [SerializeField] private float upwardSpeed = 2.5f;
    private Rigidbody2D rb;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        Destroy(gameObject, 5);
    }

    private void Update()
    {
        rb.linearVelocity = upwardSpeed * Vector2.up;
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Submarine"))
        {
            Destroy(gameObject);
        }
    }
}
