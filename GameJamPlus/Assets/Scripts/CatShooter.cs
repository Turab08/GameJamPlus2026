using DG.Tweening;
using UnityEngine;

public class CatShooter : MonoBehaviour
{
    [Header("Harpoon Setup")]
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject harpoonPrefab;
    [SerializeField] private float maxRange = 10f;
    [SerializeField] private float launchDuration = 0.4f; // Time to reach max range
    [SerializeField] private Ease outEase = Ease.OutQuad;
    [SerializeField] private LayerMask hitLayers = ~0; // Layers the harpoon can hit

    [Header("Harpoon Collision Tip")]
    [Tooltip("Local offset from harpoon base to the tip collider (matches harpoon tip position).")]
    [SerializeField] private Vector2 harpoonTipOffset = new Vector2(1.5f, 0f);
    [Tooltip("Radius of the collision trigger at the harpoon tip.")]
    [SerializeField] private float harpoonTipRadius = 0.35f;

    [Header("Rope / Harpoon Cable")]
    [SerializeField] private LineRenderer ropeLine;
    [SerializeField] private float ropeWidth = 0.06f;
    [SerializeField] private Color ropeColor = new Color(0.85f, 0.85f, 0.85f, 1f);

    [Header("Crosshair (Approach A)")]
    [Tooltip("Enable custom crosshair that locks/hides system cursor and follows the mouse.")]
    [SerializeField] private bool useCustomCrosshair = true;
    [Tooltip("Lock and confine system cursor to game window and hide OS pointer.")]
    [SerializeField] private bool lockSystemCursor = true;
    [Tooltip("Rotate the crosshair to point along the hook direction from firePoint to mouse.")]
    [SerializeField] private bool rotateCrosshairToHook = true;
    [Tooltip("Optional custom crosshair transform. If unassigned, one will be created automatically.")]
    [SerializeField] private Transform crosshairTransform;
    [Tooltip("Optional custom crosshair sprite. If null, a clean procedural reticle is generated.")]
    [SerializeField] private Sprite crosshairSprite;
    [SerializeField] private Color crosshairColor = new Color(1f, 0.35f, 0.35f, 0.95f);
    [SerializeField] private float crosshairScale = 0.5f;
    [SerializeField] private int crosshairSortingOrder = 100;

    private bool isFiring = false;
    private bool isRetracting = false;
    private Tween harpoonTween;
    private GameObject currentHarpoon;
    private bool createdRuntimeCrosshair = false;

    void OnEnable()
    {
        if (useCustomCrosshair)
        {
            SetCursorLock(true);
        }
    }

    void OnDisable()
    {
        SetCursorLock(false);
    }

    void OnApplicationFocus(bool hasFocus)
    {
        if (useCustomCrosshair)
        {
            SetCursorLock(hasFocus);
        }
    }

    private void SetCursorLock(bool locked)
    {
        if (!lockSystemCursor) return;

        if (locked)
        {
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    void Start()
    {
        if (firePoint == null)
        {
            Transform found = transform.Find("FirePoint");
            firePoint = found != null ? found : transform;
        }

        // Initialize Rope Line
        if (ropeLine == null)
        {
            GameObject ropeObj = new GameObject("HarpoonRope");
            ropeObj.transform.SetParent(transform);
            ropeLine = ropeObj.AddComponent<LineRenderer>();
        }
        SetupLineRenderer(ropeLine, ropeColor, ropeWidth, ropeWidth);
        ropeLine.enabled = false;

        // Initialize Crosshair
        if (useCustomCrosshair)
        {
            InitializeCrosshair();
            SetCursorLock(true);
        }
    }

    private void InitializeCrosshair()
    {
        if (crosshairTransform == null)
        {
            GameObject crosshairObj = new GameObject("CatShooter_Crosshair");
            createdRuntimeCrosshair = true;
            crosshairTransform = crosshairObj.transform;

            SpriteRenderer sr = crosshairObj.AddComponent<SpriteRenderer>();
            sr.sprite = crosshairSprite != null ? crosshairSprite : CreateDefaultCrosshairSprite();
            sr.color = crosshairColor;
            sr.sortingOrder = crosshairSortingOrder;
            crosshairObj.transform.localScale = Vector3.one * crosshairScale;
        }
        else
        {
            SpriteRenderer sr = crosshairTransform.GetComponent<SpriteRenderer>();
            if (sr != null)
            {
                if (crosshairSprite != null) sr.sprite = crosshairSprite;
                sr.sortingOrder = crosshairSortingOrder;
                sr.color = crosshairColor;
            }
        }
    }

    void Update()
    {
        Aim();

        if (Input.GetMouseButtonDown(0) && !isFiring)
        {
            LaunchHarpoon();
        }

        // Keep the rope connected between the firePoint and harpoon head while in flight
        if (isFiring && currentHarpoon != null && ropeLine != null && firePoint != null)
        {
            ropeLine.SetPosition(0, firePoint.position);
            ropeLine.SetPosition(1, currentHarpoon.transform.position);
        }
    }

    void LateUpdate()
    {
        UpdateCrosshair();
    }

    private void UpdateCrosshair()
    {
        if (!useCustomCrosshair || crosshairTransform == null) return;

        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;
        crosshairTransform.position = mousePos;

        if (rotateCrosshairToHook && firePoint != null)
        {
            Vector2 dir = ((Vector2)(mousePos - firePoint.position)).normalized;
            if (dir != Vector2.zero)
            {
                float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                crosshairTransform.rotation = Quaternion.Euler(0f, 0f, angle);
            }
        }
    }

    private void Aim()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null || firePoint == null) return;

        Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = firePoint.position.z;

        Vector2 direction = ((Vector2)(mousePos - firePoint.position)).normalized;

        if (direction != Vector2.zero)
        {
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
            firePoint.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    public void LaunchHarpoon()
    {
        if (firePoint == null) return;

        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = firePoint.position.z;

        Vector2 dir = ((Vector2)(mousePos - firePoint.position)).normalized;
        if (dir == Vector2.zero) dir = firePoint.right;

        float targetDistance = Mathf.Min(Vector2.Distance(firePoint.position, mousePos), maxRange);
        if (targetDistance < 1f) targetDistance = maxRange;

        Vector3 targetPos = firePoint.position + (Vector3)(dir * targetDistance);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        // Instantiate harpoon projectile pointing forward along aim direction
        if (harpoonPrefab != null)
        {
            currentHarpoon = Instantiate(harpoonPrefab, firePoint.position, Quaternion.Euler(0f, 0f, angle));
        }
        else
        {
            currentHarpoon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            currentHarpoon.transform.localScale = Vector3.one * 0.3f;
            currentHarpoon.transform.position = firePoint.position;
            currentHarpoon.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            Destroy(currentHarpoon.GetComponent<Collider>());
        }

        // Ensure harpoon sprites render cleanly above the rope
        SpriteRenderer[] srs = currentHarpoon.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in srs)
        {
            if (sr.sortingOrder < 6) sr.sortingOrder = 6;
        }

        // Ensure harpoon has a 2D Trigger Collider positioned at the tip
        Collider2D col = currentHarpoon.GetComponent<Collider2D>();
        if (col == null)
        {
            CircleCollider2D circleCol = currentHarpoon.AddComponent<CircleCollider2D>();
            circleCol.offset = harpoonTipOffset;
            circleCol.radius = harpoonTipRadius;
            col = circleCol;
        }
        col.isTrigger = true;

        // Attach HarpoonCollisionRelay to notify CatShooter upon impact
        HarpoonCollisionRelay relay = currentHarpoon.GetComponent<HarpoonCollisionRelay>();
        if (relay == null)
        {
            relay = currentHarpoon.AddComponent<HarpoonCollisionRelay>();
        }
        relay.Initialize(this, transform, hitLayers);

        // Kinematic Rigidbody2D to allow trigger detection without physics displacement
        Rigidbody2D rb = currentHarpoon.GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            rb = currentHarpoon.AddComponent<Rigidbody2D>();
        }
        rb.bodyType = RigidbodyType2D.Kinematic;

        isFiring = true;
        isRetracting = false;

        if (ropeLine != null)
        {
            ropeLine.enabled = true;
            ropeLine.SetPosition(0, firePoint.position);
            ropeLine.SetPosition(1, currentHarpoon.transform.position);
        }

        // DOTween Yoyo model: Moves out to target, then yoyos back to firePoint
        harpoonTween?.Kill();
        harpoonTween = currentHarpoon.transform.DOMove(targetPos, launchDuration)
            .SetEase(outEase)
            .SetLoops(2, LoopType.Yoyo)
            .OnStepComplete(() =>
            {
                // Halfway point reached (started returning)
                isRetracting = true;
            })
            .OnUpdate(() =>
            {
                if (currentHarpoon != null && ropeLine != null && firePoint != null)
                {
                    ropeLine.SetPosition(0, firePoint.position);
                    ropeLine.SetPosition(1, currentHarpoon.transform.position);
                }
            })
            .OnComplete(EndHarpoon);
    }

    public void OnHarpoonHit(Collider2D hitCollider)
    {
        if (isRetracting || !isFiring) return;

        Debug.Log("Harpoon hit: " + hitCollider.gameObject.name);

        // Retract early back to firePoint upon impact
        RetractEarly();
    }

    private void RetractEarly()
    {
        if (currentHarpoon == null || firePoint == null) return;

        isRetracting = true;
        harpoonTween?.Kill();

        float remainingDist = Vector2.Distance(currentHarpoon.transform.position, firePoint.position);
        float returnTime = Mathf.Clamp(launchDuration * (remainingDist / maxRange), 0.1f, launchDuration);

        harpoonTween = currentHarpoon.transform.DOMove(firePoint.position, returnTime)
            .SetEase(Ease.InQuad)
            .OnUpdate(() =>
            {
                if (currentHarpoon != null && ropeLine != null && firePoint != null)
                {
                    ropeLine.SetPosition(0, firePoint.position);
                    ropeLine.SetPosition(1, currentHarpoon.transform.position);
                }
            })
            .OnComplete(EndHarpoon);
    }

    private void EndHarpoon()
    {
        isFiring = false;
        isRetracting = false;

        if (ropeLine != null)
        {
            ropeLine.enabled = false;
        }

        if (currentHarpoon != null)
        {
            Destroy(currentHarpoon);
            currentHarpoon = null;
        }
    }

    private void SetupLineRenderer(LineRenderer line, Color color, float startWidth, float endWidth)
    {
        line.positionCount = 2;
        line.startWidth = startWidth;
        line.endWidth = endWidth;
        line.material = new Material(Shader.Find("Sprites/Default"));
        line.startColor = color;
        line.endColor = color;
        line.sortingOrder = 5;
    }

    private Sprite CreateDefaultCrosshairSprite()
    {
        int size = 64;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Bilinear;
        Color transparent = new Color(0, 0, 0, 0);
        Color white = Color.white;
        Color[] pixels = new Color[size * size];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = transparent;

        float center = (size - 1) * 0.5f;
        float ringRadius = 13f;
        float ringThickness = 2f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = x - center;
                float dy = y - center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Center pip dot
                if (dist <= 2.2f)
                {
                    pixels[y * size + x] = white;
                    continue;
                }

                // Circular reticle ring
                if (Mathf.Abs(dist - ringRadius) <= ringThickness * 0.5f)
                {
                    // Leave small gap on the right for arrow
                    if (dx < ringRadius * 0.6f || Mathf.Abs(dy) > 3f)
                    {
                        pixels[y * size + x] = white;
                        continue;
                    }
                }

                // Left tick
                if (Mathf.Abs(dy) <= 1f && dx <= -ringRadius && dx >= -ringRadius - 5f)
                {
                    pixels[y * size + x] = white;
                    continue;
                }

                // Top & Bottom ticks
                if (Mathf.Abs(dx) <= 1f && ((dy <= -ringRadius && dy >= -ringRadius - 5f) || (dy >= ringRadius && dy <= ringRadius + 5f)))
                {
                    pixels[y * size + x] = white;
                    continue;
                }

                // Directional pointer arrow head on +X (pointing along forward aim direction)
                float tipX = ringRadius + 11f;
                float baseX = ringRadius + 2f;
                if (dx >= baseX && dx <= tipX)
                {
                    float maxDy = (tipX - dx) * 0.7f;
                    if (Mathf.Abs(dy) <= maxDy)
                    {
                        pixels[y * size + x] = white;
                    }
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 64f);
    }

    private void OnDestroy()
    {
        SetCursorLock(false);
        harpoonTween?.Kill();
        if (currentHarpoon != null)
        {
            Destroy(currentHarpoon);
        }
        if (createdRuntimeCrosshair && crosshairTransform != null)
        {
            Destroy(crosshairTransform.gameObject);
        }
    }
}

/// <summary>
/// Helper component attached to the active harpoon to route trigger events back to CatShooter.
/// </summary>
public class HarpoonCollisionRelay : MonoBehaviour
{
    private CatShooter shooter;
    private Transform shooterRoot;
    private LayerMask hitLayers;

    public void Initialize(CatShooter shooter, Transform shooterRoot, LayerMask hitLayers)
    {
        this.shooter = shooter;
        this.shooterRoot = shooterRoot;
        this.hitLayers = hitLayers;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (shooter == null) return;

        // Ignore shooter's own hierarchy
        if (shooterRoot != null && collision.transform.IsChildOf(shooterRoot)) return;

        // Check LayerMask
        if ((hitLayers.value & (1 << collision.gameObject.layer)) != 0)
        {
            shooter.OnHarpoonHit(collision);
        }
    }
}
