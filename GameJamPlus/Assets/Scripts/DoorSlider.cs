using UnityEngine;
using DG.Tweening;

public class DoorSlider : MonoBehaviour
{
    [Header("Door Movement Settings")]
    [Tooltip("Time in seconds to complete the slide.")]
    public float slideDuration = 1f;

    [Tooltip("Total distance the door slides. Can be positive or negative.")]
    public float slideDistance = 5f;

    public enum SlideDirection { Horizontal, Vertical }
    [Tooltip("Axis along which the door slides.")]
    public SlideDirection slideDirection = SlideDirection.Vertical;

    [Tooltip("Easing curve for the door movement.")]
    [SerializeField] private Ease slideEase = Ease.InOutSine;

    [Tooltip("If true, moves in local space (recommended if the door is a child of a wall or frame).")]
    [SerializeField] private bool useLocalSpace = true;

    [Header("Door State & Automation")]
    [Tooltip("Current open state of the door.")]
    [SerializeField] private bool isOpen = false;

    [Tooltip("If true, the door automatically opens on Start.")]
    [SerializeField] private bool openOnStart = false;

    [Tooltip("If true, continuously moves back and forth in a loop without needing triggers.")]
    [SerializeField] private bool pingPongLoop = false;

    [Header("Trigger Settings (Optional)")]
    [Tooltip("Optional PushTrigger. If assigned, the door automatically opens when the button is pushed.")]
    [SerializeField] private PushTrigger triggerButton;

    private Vector3 closedPosition;
    private Tween currentTween;
    private bool initialized = false;

    private void Awake()
    {
        // Increase DOTween tween capacity to prevent limit errors
        DOTween.SetTweensCapacity(500, 100);

        closedPosition = useLocalSpace ? transform.localPosition : transform.position;
        initialized = true;
    }

    private void Start()
    {
        // Auto-find trigger in local hierarchy if not explicitly assigned
        if (triggerButton == null)
        {
            triggerButton = GetComponentInParent<PushTrigger>() ?? GetComponentInChildren<PushTrigger>();
        }

        if (pingPongLoop)
        {
            StartPingPongLoop();
        }
        else if (openOnStart)
        {
            SetOpen(true, instant: false);
        }
        else if (isOpen)
        {
            SetOpen(true, instant: true);
        }
    }

    private void Update()
    {
        // Only trigger a slide when the trigger state actually changes (never spam DOTween every frame)
        if (!pingPongLoop && triggerButton != null)
        {
            if (triggerButton.canDoorSlideOpen != isOpen)
            {
                SetOpen(triggerButton.canDoorSlideOpen);
            }
        }
    }

    /// <summary>
    /// Opens or closes the door smoothly using DOTween.
    /// </summary>
    public void SetOpen(bool open, bool instant = false)
    {
        if (!initialized)
        {
            closedPosition = useLocalSpace ? transform.localPosition : transform.position;
            initialized = true;
        }

        isOpen = open;
        Vector3 targetPos = GetTargetPosition(isOpen);

        currentTween?.Kill();

        if (instant || slideDuration <= 0f)
        {
            if (useLocalSpace) transform.localPosition = targetPos;
            else transform.position = targetPos;
            return;
        }

        if (useLocalSpace)
        {
            currentTween = transform.DOLocalMove(targetPos, slideDuration).SetEase(slideEase);
        }
        else
        {
            currentTween = transform.DOMove(targetPos, slideDuration).SetEase(slideEase);
        }
    }

    private Vector3 GetTargetPosition(bool open)
    {
        if (!open) return closedPosition;

        Vector3 offset = (slideDirection == SlideDirection.Vertical)
            ? new Vector3(0f, slideDistance, 0f)
            : new Vector3(slideDistance, 0f, 0f);

        return closedPosition + offset;
    }

    private void StartPingPongLoop()
    {
        currentTween?.Kill();
        Vector3 openPos = GetTargetPosition(true);

        if (useLocalSpace)
        {
            currentTween = transform.DOLocalMove(openPos, slideDuration)
                .SetEase(slideEase)
                .SetLoops(-1, LoopType.Yoyo);
        }
        else
        {
            currentTween = transform.DOMove(openPos, slideDuration)
                .SetEase(slideEase)
                .SetLoops(-1, LoopType.Yoyo);
        }
    }

    [ContextMenu("Open Door")]
    public void OpenDoor() => SetOpen(true);

    [ContextMenu("Close Door")]
    public void CloseDoor() => SetOpen(false);

    [ContextMenu("Toggle Door")]
    public void ToggleDoor() => SetOpen(!isOpen);

    private void OnDestroy()
    {
        currentTween?.Kill();
    }
}
