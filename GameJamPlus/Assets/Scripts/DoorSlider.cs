using UnityEngine;
using DG.Tweening;

public class DoorSlider : MonoBehaviour
{
    
    public float slideDuration = 1f;

    public float slideDistance = 5f;

    public enum SlideDirection { Horizontal, Vertical }
    public SlideDirection slideDirection = SlideDirection.Vertical;

    [SerializeField] private Ease slideEase = Ease.InOutSine;

    [SerializeField] private bool useLocalSpace = true;

    [SerializeField] private bool isOpen = false;

    [SerializeField] private bool openOnStart = false;

    [SerializeField] private bool pingPongLoop = false;

    [SerializeField] private PushTrigger triggerButton;

    private Vector3 closedPosition;
    private Tween currentTween;
    private bool initialized = false;

    private void Awake()
    {
        DOTween.SetTweensCapacity(500, 100);

        closedPosition = useLocalSpace ? transform.localPosition : transform.position;
        initialized = true;
    }

    private void Start()
    {
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
        if (!pingPongLoop && triggerButton != null)
        {
            if (triggerButton.canDoorSlideOpen != isOpen)
            {
                SetOpen(triggerButton.canDoorSlideOpen);
            }
        }
    }

    
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
