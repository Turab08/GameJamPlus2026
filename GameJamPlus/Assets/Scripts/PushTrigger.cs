using DG.Tweening;
using UnityEngine;

public class PushTrigger : MonoBehaviour
{
    [Header("Push Animation Settings")]
    [SerializeField] private float pushDuration = 0.3f;
    [SerializeField] private Ease pushEase = Ease.OutQuad;
    [SerializeField] private bool returnOnExit = false; // Latching switch: stays pushed once activated
    [SerializeField] private float returnDuration = 0.3f;
    [SerializeField] private Ease returnEase = Ease.InQuad;

    [SerializeField] private Vector3 pushedLocalPosition = Vector3.zero;
    public bool canDoorSlideOpen = false;

    private Vector3 initialLocalPosition;
    private Rigidbody2D rb;
    private Tween currentTween;
    private bool isPushed = false;

    private void Awake()
    {
        // Check if Rigidbody2D is directly on THIS child GameObject (NOT on parent)
        if (TryGetComponent<Rigidbody2D>(out rb))
        {
            // If the child has a Rigidbody2D, make sure it is Kinematic so physics doesn't move it/its hierarchy
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

        // Ensure the child pushable button renders in front of the background, but BEHIND the parent anchor box
        SpriteRenderer childSr = GetComponent<SpriteRenderer>();
        if (transform.parent != null)
        {
            SpriteRenderer parentSr = transform.parent.GetComponent<SpriteRenderer>();
            if (childSr != null && parentSr != null)
            {
                childSr.sortingLayerID = parentSr.sortingLayerID;
                int baseOrder = Mathf.Max(parentSr.sortingOrder, 1);
                parentSr.sortingOrder = baseOrder + 1;
                childSr.sortingOrder = baseOrder;
            }

            // Ignore physical collisions between this button's collider and the parent's collider
            Collider2D thisCollider = GetComponent<Collider2D>();
            Collider2D parentCollider = transform.parent.GetComponent<Collider2D>();
            if (thisCollider != null && parentCollider != null)
            {
                Physics2D.IgnoreCollision(thisCollider, parentCollider, true);
            }
        }

        // Store initial local position relative to the parent
        initialLocalPosition = transform.localPosition;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // If already latched, do nothing
        if (isPushed) return;

        // Ignore self or parent hierarchy
        if (transform.parent != null && collision.transform.IsChildOf(transform.parent)) return;

        // Ignore mines - only submarine, player, and other non-mine objects activate the button
        if (collision.GetComponentInParent<Mine>() != null || collision.CompareTag("Mine"))
        {
            return;
        }

        Debug.Log("Button push trigger activated by: " + collision.gameObject.name);
        isPushed = true;
        canDoorSlideOpen = true;
        PushTowardsParent();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (transform.parent != null && collision.transform.IsChildOf(transform.parent)) return;

        // Only release if configured to return on exit and not permanently latched
        if (returnOnExit && !isPushed)
        {
            canDoorSlideOpen = false;
            ReleaseButton();
        }
    }

    public void PushTowardsParent()
    {    
        currentTween?.Kill();
        currentTween = transform.DOLocalMove(pushedLocalPosition, pushDuration).SetEase(pushEase);
    }

    public void ReleaseButton()
    {
        isPushed = false;
        currentTween?.Kill();
        currentTween = transform.DOLocalMove(initialLocalPosition, returnDuration).SetEase(returnEase);
    }

    private void OnDestroy()
    {
        currentTween?.Kill();
    }
}
