using DG.Tweening;
using UnityEngine;

public class PushTrigger : MonoBehaviour
{
    [SerializeField] private float pushDuration = 0.3f;
    [SerializeField] private Ease pushEase = Ease.OutQuad;
    [SerializeField] private bool returnOnExit = true;
    [SerializeField] private float returnDuration = 0.3f;
    [SerializeField] private Ease returnEase = Ease.InQuad;

    [SerializeField] private Vector3 pushedLocalPosition = Vector3.zero;
    public  bool canDoorSlideOpen=false;

    private Vector3 initialLocalPosition;
    private Rigidbody2D rb;
    private Tween currentTween;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        initialLocalPosition = transform.localPosition;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player") || collision.attachedRigidbody != null)
        {
            Debug.Log("Button push trigger activated!");
            canDoorSlideOpen = true;
            PushTowardsParent();
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (returnOnExit && (collision.CompareTag("Player") || collision.attachedRigidbody != null))
        {
            ReleaseButton();
        }
    }

    public void PushTowardsParent()
    {
        if (transform.parent == null) return;

        currentTween?.Kill();

        if (rb != null)
        {
            Vector3 targetWorldPos = transform.parent.TransformPoint(pushedLocalPosition);
            currentTween = rb.DOMove(targetWorldPos, pushDuration).SetEase(pushEase);
        }
        else
        {
            currentTween = transform.DOLocalMove(pushedLocalPosition, pushDuration).SetEase(pushEase);
        }
    }

    public void ReleaseButton()
    {
        currentTween?.Kill();

        if (rb != null)
        {
            Vector3 initialWorldPos = transform.parent != null
                ? transform.parent.TransformPoint(initialLocalPosition)
                : initialLocalPosition;
            currentTween = rb.DOMove(initialWorldPos, returnDuration).SetEase(returnEase);
        }
        else
        {
            currentTween = transform.DOLocalMove(initialLocalPosition, returnDuration).SetEase(returnEase);
        }
    }

    private void OnDestroy()
    {
        currentTween?.Kill();
    }
}
