using DG.Tweening;
using UnityEngine;

public class PushTrigger : MonoBehaviour
{
    [SerializeField] private float pushDuration = 0.3f;
    [SerializeField] private Ease pushEase = Ease.OutQuad;
    [SerializeField] private bool returnOnExit = false; 
    [SerializeField] private float returnDuration = 0.3f;
    [SerializeField] private Ease returnEase = Ease.InQuad;

    [SerializeField] private Vector3 pushedLocalPosition = Vector3.zero;
    public bool canDoorSlideOpen = false;

    private Vector3 initialLocalPosition;
    private Rigidbody2D rb;
    private Tween currentTween;
    private bool isPushed = false;

    public bool isHeavy = false;

    private void Awake()
    {
        if (TryGetComponent<Rigidbody2D>(out rb))
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
        }

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

            Collider2D thisCollider = GetComponent<Collider2D>();
            Collider2D parentCollider = transform.parent.GetComponent<Collider2D>();
            if (thisCollider != null && parentCollider != null)
            {
                Physics2D.IgnoreCollision(thisCollider, parentCollider, true);
            }
        }

        initialLocalPosition = transform.localPosition;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isPushed) return;

        if (transform.parent != null && collision.transform.IsChildOf(transform.parent)) return;

        if (collision.GetComponentInParent<Mine>() != null || collision.CompareTag("Mine") || (collision.CompareTag("Submarine") || collision.CompareTag("Player") && isHeavy))
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
