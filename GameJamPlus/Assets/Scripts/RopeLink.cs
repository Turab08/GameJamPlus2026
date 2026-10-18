using UnityEngine;


public class RopeLink : MonoBehaviour
{
    [HideInInspector] public RopeGenerator generator;
    [HideInInspector] public int linkIndex;
    [HideInInspector] public HingeJoint2D joint;
    [HideInInspector] public Rigidbody2D rb;

    private bool isSevered = false;

    public void Initialize(RopeGenerator generator, int index, HingeJoint2D joint, Rigidbody2D rb)
    {
        this.generator = generator;
        this.linkIndex = index;
        this.joint = joint;
        this.rb = rb;
        this.isSevered = false;
    }

    
    public void Sever(Vector2 impulse = default)
    {
        if (isSevered) return;
        isSevered = true;

        if (generator != null)
        {
            generator.SeverAt(this, impulse);
        }
        else
        {
            if (joint != null)
            {
                Destroy(joint);
                joint = null;
            }

            if (rb != null && impulse != Vector2.zero)
            {
                rb.AddForce(impulse, ForceMode2D.Impulse);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<HarpoonCollisionRelay>() != null || collision.CompareTag("Harpoon"))
        {
            Vector2 impulse = collision.attachedRigidbody != null
                ? collision.attachedRigidbody.linearVelocity.normalized * 3f
                : (Vector2)collision.transform.right * 3f;

            Sever(impulse);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.GetComponent<HarpoonCollisionRelay>() != null || collision.collider.CompareTag("Harpoon"))
        {
            Vector2 impulse = collision.relativeVelocity.normalized * 3f;
            Sever(impulse);
        }
    }
}

