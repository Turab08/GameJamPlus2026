using DG.Tweening;
using UnityEngine;

public class CatShooter : MonoBehaviour
{
    [SerializeField] private Transform firePoint;
    [SerializeField] private GameObject harpoonPrefab;
    [SerializeField] private float maxRange = 10f;
    [SerializeField] private float launchDuration = 0.4f; 
    [SerializeField] private Ease outEase = Ease.OutQuad;
    [SerializeField] private LayerMask hitLayers = ~0;

    [SerializeField] private Transform gunTransform;
    [SerializeField] private Transform gunTip;
    [SerializeField] private float gunTipOffset = 0.8f;
    [SerializeField] private bool autoCalculateGunTip = true;
    [SerializeField] private float gunAngleOffset = 0f;

    private SpriteRenderer gunSpriteRenderer;

    [SerializeField] private Vector2 harpoonTipOffset = new Vector2(1.5f, 0f);
    [SerializeField] private float harpoonTipRadius = 0.35f;

    [SerializeField] private LineRenderer ropeLine;
    [SerializeField] private float ropeWidth = 0.06f;
    [SerializeField] private Color ropeColor = new Color(0.85f, 0.85f, 0.85f, 1f);

    [SerializeField] private bool useCustomCrosshair = true;
    [SerializeField] private bool lockSystemCursor = true;
    [SerializeField] private bool rotateCrosshairToHook = true;
    [SerializeField] private Transform crosshairTransform;
    [SerializeField] private Sprite crosshairSprite;
    [SerializeField] private Color crosshairColor = new Color(1f, 0.35f, 0.35f, 0.95f);
    [SerializeField] private float crosshairScale = 0.5f;
    [SerializeField] private int crosshairSortingOrder = 100;

    public enum FacingMode
    {
        YRotation,      
        LocalScaleX,     
        SpriteRenderer   
    }

    [SerializeField] private bool rotatePlayerToMouse = true;
    [SerializeField] private Transform playerTransform;
    [SerializeField] private FacingMode horizontalFacingMode = FacingMode.YRotation;
    [SerializeField] private bool smoothHorizontalRotation = false;
    [SerializeField] private float rotationSpeed = 720f;
    [SerializeField] private float horizontalDeadzone = 0.1f;

    private bool isFacingRight = true;
    public bool IsFacingRight => isFacingRight;

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
        if (playerTransform == null)
        {
            playerTransform = transform;
        }
        isFacingRight = Mathf.Abs(Mathf.DeltaAngle(playerTransform.eulerAngles.y, 180f)) > 90f;

        FindGunTransform();

        if (firePoint == null)
        {
            Transform found = transform.Find("FirePoint");
            if (found == null) found = transform.Find("ShootPosition");
            firePoint = found != null ? found : (gunTransform != null ? gunTransform : transform);
        }

        if (ropeLine == null)
        {
            GameObject ropeObj = new GameObject("HarpoonRope");
            ropeObj.transform.SetParent(transform);
            ropeLine = ropeObj.AddComponent<LineRenderer>();
        }
        SetupLineRenderer(ropeLine, ropeColor, ropeWidth, ropeWidth);
        ropeLine.enabled = false;

        if (useCustomCrosshair)
        {
            InitializeCrosshair();
            SetCursorLock(true);
        }
    }

    private void FindGunTransform()
    {
        if (gunTransform != null)
        {
            gunSpriteRenderer = gunTransform.GetComponentInChildren<SpriteRenderer>();
            return;
        }

        string[] candidateNames = { "Gun", "HarpoonGun", "Harpoon Gun", "ShootPosition", "Weapon", "Arm", "Shooter" };
        foreach (string cName in candidateNames)
        {
            Transform found = transform.Find(cName);
            if (found != null)
            {
                gunTransform = found;
                break;
            }
        }

        if (gunTransform == null)
        {
            SpriteRenderer[] childRenderers = GetComponentsInChildren<SpriteRenderer>();
            SpriteRenderer bodyRenderer = GetComponent<SpriteRenderer>();
            foreach (var sr in childRenderers)
            {
                if (sr != bodyRenderer && sr.transform != transform)
                {
                    gunTransform = sr.transform;
                    break;
                }
            }
        }

        if (gunTransform == null && firePoint != null && firePoint != transform)
        {
            gunTransform = firePoint;
        }

        if (gunTransform != null)
        {
            gunSpriteRenderer = gunTransform.GetComponentInChildren<SpriteRenderer>();

            if (gunTip == null)
            {
                gunTip = gunTransform.Find("Tip") ?? gunTransform.Find("Muzzle") ?? gunTransform.Find("GunTip") ?? gunTransform.Find("FirePoint");
            }
        }
    }

    public Vector3 GetLaunchPosition()
    {
        if (gunTip != null) return gunTip.position;

        Transform gun = gunTransform != null ? gunTransform : firePoint;
        if (gun == null) return transform.position;

        float dist = GetGunTipDistance();
        return gun.position + gun.right * dist;
    }

    public float GetGunTipDistance()
    {
        if (!autoCalculateGunTip && gunTipOffset > 0f)
        {
            return gunTipOffset;
        }

        Transform gun = gunTransform != null ? gunTransform : firePoint;
        if (gun != null)
        {
            SpriteRenderer sr = gun.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
            {
                float spriteWidth = (sr.sprite.rect.width / sr.sprite.pixelsPerUnit) * Mathf.Abs(gun.lossyScale.x);
                float pivotNormX = sr.sprite.pivot.x / sr.sprite.rect.width;
                float distToTip = (1f - pivotNormX) * spriteWidth;
                if (distToTip > 0.05f) return distToTip;
            }
        }

        return gunTipOffset > 0f ? gunTipOffset : 0.8f;
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

        if (isFiring && currentHarpoon != null && ropeLine != null)
        {
            ropeLine.SetPosition(0, GetLaunchPosition());
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

        if (rotateCrosshairToHook)
        {
            Vector3 origin = GetLaunchPosition();
            Vector2 dir = ((Vector2)(mousePos - origin)).normalized;
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
        if (mainCam == null) return;

        Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        RotatePlayerHorizontally(mousePos);

        Vector3 targetAimPos = (useCustomCrosshair && crosshairTransform != null) ? crosshairTransform.position : mousePos;
        targetAimPos.z = 0f;
        AimGunAt(targetAimPos);
    }

    private void AimGunAt(Vector3 targetWorldPos)
    {
        Transform gun = gunTransform != null ? gunTransform : firePoint;
        if (gun == null) return;

        Vector3 gunOrigin = gun.position;
        targetWorldPos.z = gunOrigin.z;

        Vector2 aimDir = ((Vector2)(targetWorldPos - gunOrigin)).normalized;
        if (aimDir == Vector2.zero) return;

        Transform parent = playerTransform != null ? playerTransform : transform;

        if (gun.IsChildOf(parent))
        {
            Vector3 localDir = parent.InverseTransformDirection(aimDir);
            float localAngle = Mathf.Atan2(localDir.y, localDir.x) * Mathf.Rad2Deg + gunAngleOffset;
            gun.localRotation = Quaternion.Euler(0f, 0f, localAngle);
        }
        else
        {
            float worldAngle = Mathf.Atan2(aimDir.y, aimDir.x) * Mathf.Rad2Deg + gunAngleOffset;
            gun.rotation = Quaternion.Euler(0f, 0f, worldAngle);
        }

        if (gunSpriteRenderer == null)
        {
            gunSpriteRenderer = gun.GetComponentInChildren<SpriteRenderer>();
        }

        if (gunSpriteRenderer != null)
        {
            bool isUpsideDown = Vector3.Dot(gun.up, Vector3.up) < 0f;
            gunSpriteRenderer.flipY = isUpsideDown;
        }

        if (firePoint != null && firePoint != gun)
        {
            firePoint.position = GetLaunchPosition();
            firePoint.rotation = gun.rotation;
        }
    }

    private void RotatePlayerHorizontally(Vector3 mousePos)
    {
        if (!rotatePlayerToMouse) return;

        Transform target = playerTransform != null ? playerTransform : transform;
        float diffX = mousePos.x - target.position.x;

        if (Mathf.Abs(diffX) > horizontalDeadzone)
        {
            isFacingRight = diffX > 0f;
        }

        switch (horizontalFacingMode)
        {
            case FacingMode.YRotation:
                Vector3 currentEuler = target.eulerAngles;
                float targetY = isFacingRight ? 0f : 180f;

                if (smoothHorizontalRotation)
                {
                    float currentY = Mathf.MoveTowardsAngle(currentEuler.y, targetY, rotationSpeed * Time.deltaTime);
                    target.rotation = Quaternion.Euler(currentEuler.x, currentY, currentEuler.z);
                }
                else
                {
                    target.rotation = Quaternion.Euler(currentEuler.x, targetY, currentEuler.z);
                }
                break;

            case FacingMode.LocalScaleX:
                Vector3 scale = target.localScale;
                float absX = Mathf.Abs(scale.x);
                target.localScale = new Vector3(isFacingRight ? absX : -absX, scale.y, scale.z);
                break;

            case FacingMode.SpriteRenderer:
                SpriteRenderer sr = target.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    sr.flipX = !isFacingRight;
                }
                break;
        }
    }

    public void LaunchHarpoon()
    {
        Camera mainCam = Camera.main;
        if (mainCam == null) return;

        Vector3 mousePos = mainCam.ScreenToWorldPoint(Input.mousePosition);
        mousePos.z = 0f;

        Vector3 launchPos = GetLaunchPosition();
        Vector3 targetWorld = (useCustomCrosshair && crosshairTransform != null) ? crosshairTransform.position : mousePos;
        targetWorld.z = launchPos.z;

        Vector2 dir = ((Vector2)(targetWorld - launchPos)).normalized;
        if (dir == Vector2.zero)
        {
            Transform gun = gunTransform != null ? gunTransform : firePoint;
            dir = gun != null ? (Vector2)gun.right : (Vector2)transform.right;
        }

        float targetDistance = Mathf.Min(Vector2.Distance(launchPos, targetWorld), maxRange);
        if (targetDistance < 1f) targetDistance = maxRange;

        Vector3 targetPos = launchPos + (Vector3)(dir * targetDistance);
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        if (harpoonPrefab != null)
        {
            currentHarpoon = Instantiate(harpoonPrefab, launchPos, Quaternion.Euler(0f, 0f, angle));
        }
        else
        {
            currentHarpoon = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            currentHarpoon.transform.localScale = Vector3.one * 0.3f;
            currentHarpoon.transform.position = launchPos;
            currentHarpoon.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            Destroy(currentHarpoon.GetComponent<Collider>());
        }

        SpriteRenderer[] srs = currentHarpoon.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in srs)
        {
            if (sr.sortingOrder < 6) sr.sortingOrder = 6;
        }

        Collider2D col = currentHarpoon.GetComponent<Collider2D>();
        if (col == null)
        {
            CircleCollider2D circleCol = currentHarpoon.AddComponent<CircleCollider2D>();
            circleCol.offset = harpoonTipOffset;
            circleCol.radius = harpoonTipRadius;
            col = circleCol;
        }
        col.isTrigger = true;

        HarpoonCollisionRelay relay = currentHarpoon.GetComponent<HarpoonCollisionRelay>();
        if (relay == null)
        {
            relay = currentHarpoon.AddComponent<HarpoonCollisionRelay>();
        }
        relay.Initialize(this, transform, hitLayers);

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
            ropeLine.SetPosition(0, launchPos);
            ropeLine.SetPosition(1, currentHarpoon.transform.position);
        }

        harpoonTween?.Kill();
        harpoonTween = currentHarpoon.transform.DOMove(targetPos, launchDuration)
            .SetEase(outEase)
            .SetLoops(2, LoopType.Yoyo)
            .OnStepComplete(() =>
            {
                isRetracting = true;
            })
            .OnUpdate(() =>
            {
                if (currentHarpoon != null && ropeLine != null)
                {
                    ropeLine.SetPosition(0, GetLaunchPosition());
                    ropeLine.SetPosition(1, currentHarpoon.transform.position);
                }
            })
            .OnComplete(EndHarpoon);
    }

    public void OnHarpoonHit(Collider2D hitCollider)
    {
        if (isRetracting || !isFiring) return;

        RopeLink ropeLink = hitCollider.GetComponentInParent<RopeLink>();
        if (ropeLink != null)
        {
            Vector2 cutImpulse = currentHarpoon != null
                ? (Vector2)(currentHarpoon.transform.right * 4f)
                : Vector2.down * 2f;
            ropeLink.Sever(cutImpulse);
        }

        RetractEarly();
    }

    private void RetractEarly()
    {
        if (currentHarpoon == null) return;

        isRetracting = true;
        harpoonTween?.Kill();

        Vector3 returnOrigin = GetLaunchPosition();
        float remainingDist = Vector2.Distance(currentHarpoon.transform.position, returnOrigin);
        float returnTime = Mathf.Clamp(launchDuration * (remainingDist / maxRange), 0.1f, launchDuration);

        harpoonTween = currentHarpoon.transform.DOMove(returnOrigin, returnTime)
            .SetEase(Ease.InQuad)
            .OnUpdate(() =>
            {
                if (currentHarpoon != null && ropeLine != null)
                {
                    ropeLine.SetPosition(0, GetLaunchPosition());
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

                if (dist <= 2.2f)
                {
                    pixels[y * size + x] = white;
                    continue;
                }

                if (Mathf.Abs(dist - ringRadius) <= ringThickness * 0.5f)
                {
                    if (dx < ringRadius * 0.6f || Mathf.Abs(dy) > 3f)
                    {
                        pixels[y * size + x] = white;
                        continue;
                    }
                }

                if (Mathf.Abs(dy) <= 1f && dx <= -ringRadius && dx >= -ringRadius - 5f)
                {
                    pixels[y * size + x] = white;
                    continue;
                }

                if (Mathf.Abs(dx) <= 1f && ((dy <= -ringRadius && dy >= -ringRadius - 5f) || (dy >= ringRadius && dy <= ringRadius + 5f)))
                {
                    pixels[y * size + x] = white;
                    continue;
                }

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

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 tip = GetLaunchPosition();
        Gizmos.DrawWireSphere(tip, 0.08f);

        Gizmos.color = Color.red;
        Transform gun = gunTransform != null ? gunTransform : firePoint;
        if (gun != null)
        {
            Gizmos.DrawLine(gun.position, tip);
        }
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

        if (shooterRoot != null && collision.transform.IsChildOf(shooterRoot)) return;

        if ((hitLayers.value & (1 << collision.gameObject.layer)) != 0)
        {
            shooter.OnHarpoonHit(collision);
        }
    }
}
