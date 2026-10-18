using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class RopeGenerator : MonoBehaviour
{
    [Header("Rope Settings")]
    [Tooltip("Prefab representing a single link in the rope.")]
    [SerializeField] private GameObject linkPrefab;
    [Tooltip("Number of links in the rope chain.")]
    [SerializeField] private int ropeLength = 10;
    [Tooltip("Length/spacing of each link. Leave at 0 to automatically calculate from the link sprite or collider.")]
    [SerializeField] private float linkDistance = 0f;
    [Tooltip("Anchor offset on the generator root object.")]
    [SerializeField] private Vector2 anchorOffset = Vector2.zero;
    [Tooltip("Parent generated links under this GameObject to keep the hierarchy clean.")]
    [SerializeField] private bool parentLinks = true;

    [Header("Physics Settings")]
    [SerializeField] private float linkMass = 0.5f;
    [SerializeField] private float angularDrag = 1f;
    [SerializeField] private float linearDrag = 0.1f;

    [Header("End Attachment (Optional)")]
    [Tooltip("Optional Rigidbody2D to attach to the bottom of the last link (e.g. mine, weight, diver, hook).")]
    [SerializeField] private Rigidbody2D endAttachedBody;

    [Header("Visual Rendering (Optional)")]
    [Tooltip("Optional LineRenderer to draw a continuous rope connecting the links.")]
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Sever / Break Settings")]
    [Tooltip("Optional effect spawned at the cut point when the rope is severed.")]
    [SerializeField] private GameObject cutEffectPrefab;
    [Tooltip("Invoked whenever any link in the rope is severed.")]
    public UnityEvent OnRopeCut;
    [Tooltip("Invoked when the attached end body is detached from the rope.")]
    public UnityEvent OnEndBodyDetached;

    private readonly List<GameObject> activeLinks = new List<GameObject>();

    void Start()
    {
        GenerateRope();
    }

    void LateUpdate()
    {
        UpdateLineRenderer();
    }

    [ContextMenu("Generate Rope")]
    public void GenerateRope()
    {
        ClearRope();

        if (linkPrefab == null)
        {
            Debug.LogWarning("[RopeGenerator] Link Prefab is not assigned! Please assign a link prefab in the Inspector.", this);
            return;
        }

        if (ropeLength <= 0)
        {
            Debug.LogWarning("[RopeGenerator] Rope Length must be greater than 0.", this);
            return;
        }

        // Get or add anchor Rigidbody2D
        Rigidbody2D previousRigidbody = GetComponent<Rigidbody2D>();
        if (previousRigidbody == null)
        {
            previousRigidbody = gameObject.AddComponent<Rigidbody2D>();
            previousRigidbody.bodyType = RigidbodyType2D.Static;
        }

        // Determine link size
        float linkSize = CalculateLinkSize();

        Collider2D previousCollider = GetComponent<Collider2D>();

        for (int i = 0; i < ropeLength; i++)
        {
            // Spawn each link staggered along -transform.up so they hang naturally
            Vector3 spawnPos = transform.position + (Vector3)anchorOffset - transform.up * (linkSize * (i + 0.5f));
            GameObject newLink = Instantiate(linkPrefab, spawnPos, transform.rotation);
            activeLinks.Add(newLink);

            if (parentLinks)
            {
                newLink.transform.SetParent(transform);
            }

            // Setup Rigidbody2D
            Rigidbody2D linkRb = newLink.GetComponent<Rigidbody2D>();
            if (linkRb == null)
            {
                linkRb = newLink.AddComponent<Rigidbody2D>();
            }
            linkRb.mass = linkMass;
#if UNITY_6000_0_OR_NEWER
            linkRb.angularDamping = angularDrag;
            linkRb.linearDamping = linearDrag;
#else
            linkRb.angularDrag = angularDrag;
            linkRb.drag = linearDrag;
#endif

            // Setup HingeJoint2D
            HingeJoint2D joint = newLink.GetComponent<HingeJoint2D>();
            if (joint == null)
            {
                joint = newLink.AddComponent<HingeJoint2D>();
            }

            // CRITICAL: Disable autoConfigureConnectedAnchor so our anchors are respected
            joint.autoConfigureConnectedAnchor = false;
            joint.enableCollision = false; // Never collide with the connected body
            joint.connectedBody = previousRigidbody;

            // Joint pivot is at the top of the new link
            joint.anchor = new Vector2(0f, linkSize * 0.5f);

            // Connect to root center for link 0, or bottom of previous link for subsequent links
            if (i == 0)
            {
                joint.connectedAnchor = anchorOffset;
            }
            else
            {
                joint.connectedAnchor = new Vector2(0f, -linkSize * 0.5f);
            }

            // Ensure the link has a Collider2D so the harpoon can detect hits
            Collider2D currentCol = newLink.GetComponent<Collider2D>();
            if (currentCol == null)
            {
                CapsuleCollider2D capCol = newLink.AddComponent<CapsuleCollider2D>();
                capCol.size = new Vector2(linkSize * 0.4f, linkSize);
                currentCol = capCol;
            }

            // Prevent collision jitter between adjacent links
            if (previousCollider != null && currentCol != null)
            {
                Physics2D.IgnoreCollision(previousCollider, currentCol);
            }
            previousCollider = currentCol;

            // Attach RopeLink component for hit detection and severing
            RopeLink ropeLink = newLink.GetComponent<RopeLink>();
            if (ropeLink == null)
            {
                ropeLink = newLink.AddComponent<RopeLink>();
            }
            ropeLink.Initialize(this, i, joint, linkRb);

            previousRigidbody = linkRb;
        }

        // Optionally attach end body to the last link
        if (endAttachedBody != null && activeLinks.Count > 0)
        {
            AttachEndBody(previousRigidbody, linkSize);
        }
    }

    private void AttachEndBody(Rigidbody2D lastLinkRb, float linkSize)
    {
        HingeJoint2D endJoint = endAttachedBody.GetComponent<HingeJoint2D>();
        if (endJoint == null)
        {
            endJoint = endAttachedBody.gameObject.AddComponent<HingeJoint2D>();
        }

        endJoint.autoConfigureConnectedAnchor = false;
        endJoint.enableCollision = false;
        endJoint.connectedBody = lastLinkRb;
        endJoint.anchor = Vector2.zero;
        endJoint.connectedAnchor = new Vector2(0f, -linkSize * 0.5f);
    }

    /// <summary>
    /// Severs the rope at the specified link. Destroys the hinge connecting it to the upper chain,
    /// causing the link and all downstream links to drop freely under gravity.
    /// </summary>
    public void SeverAt(RopeLink link, Vector2 impulse = default)
    {
        if (link == null) return;

        int k = activeLinks.IndexOf(link.gameObject);
        if (k < 0) return; // Not an active link in this generator

        // Spawn cut visual effect if assigned
        if (cutEffectPrefab != null)
        {
            Instantiate(cutEffectPrefab, link.transform.position, Quaternion.identity);
        }

        // Break the joint on this link
        if (link.joint != null)
        {
            Destroy(link.joint);
            link.joint = null;
        }

        // Apply physical cut impulse to the severed link
        if (link.rb != null && impulse != Vector2.zero)
        {
            link.rb.AddForce(impulse, ForceMode2D.Impulse);
        }

        // Separate the falling links from the active top chain
        int countToSever = activeLinks.Count - k;
        List<GameObject> fallingLinks = activeLinks.GetRange(k, countToSever);
        activeLinks.RemoveRange(k, countToSever);

        // Unparent falling links so they move completely independently in world space
        foreach (var fl in fallingLinks)
        {
            if (fl != null)
            {
                fl.transform.SetParent(null);
            }
        }

        // If using a LineRenderer, create a separate visual cable for the falling segment
        if (lineRenderer != null && fallingLinks.Count > 1)
        {
            CreateSeveredLineVisual(fallingLinks);
        }

        // Invoke gameplay events
        OnRopeCut?.Invoke();
        if (endAttachedBody != null)
        {
            OnEndBodyDetached?.Invoke();
        }
    }

    private void CreateSeveredLineVisual(List<GameObject> fallingLinks)
    {
        GameObject severedObj = new GameObject("SeveredRopeLine");
        LineRenderer fallingLine = severedObj.AddComponent<LineRenderer>();

        // Copy visual appearance from the main line renderer
        fallingLine.sharedMaterial = lineRenderer.sharedMaterial;
        fallingLine.startColor = lineRenderer.startColor;
        fallingLine.endColor = lineRenderer.endColor;
        fallingLine.startWidth = lineRenderer.startWidth;
        fallingLine.endWidth = lineRenderer.endWidth;
        fallingLine.sortingLayerID = lineRenderer.sortingLayerID;
        fallingLine.sortingOrder = lineRenderer.sortingOrder;

        SeveredRopeVisual visual = severedObj.AddComponent<SeveredRopeVisual>();
        visual.Initialize(fallingLine, fallingLinks);
    }

    private float CalculateLinkSize()
    {
        if (linkDistance > 0f) return linkDistance;

        SpriteRenderer sr = linkPrefab.GetComponentInChildren<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            float spriteHeight = (sr.sprite.rect.height / sr.sprite.pixelsPerUnit) * Mathf.Abs(linkPrefab.transform.localScale.y);
            if (spriteHeight > 0.001f) return spriteHeight;
        }

        Collider2D col = linkPrefab.GetComponentInChildren<Collider2D>();
        if (col != null && col.bounds.size.y > 0.001f)
        {
            return col.bounds.size.y;
        }

        return 0.5f; // Safe fallback
    }

    private void UpdateLineRenderer()
    {
        if (lineRenderer == null) return;

        if (activeLinks.Count == 0)
        {
            lineRenderer.positionCount = 0;
            return;
        }

        lineRenderer.positionCount = activeLinks.Count + 1;
        lineRenderer.SetPosition(0, transform.position + (Vector3)anchorOffset);

        for (int i = 0; i < activeLinks.Count; i++)
        {
            if (activeLinks[i] != null)
            {
                lineRenderer.SetPosition(i + 1, activeLinks[i].transform.position);
            }
        }
    }

    [ContextMenu("Clear Rope")]
    public void ClearRope()
    {
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = 0;
        }

        foreach (var link in activeLinks)
        {
            if (link != null)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    DestroyImmediate(link);
                else
                    Destroy(link);
#else
                Destroy(link);
#endif
            }
        }
        activeLinks.Clear();
    }

    private void OnDestroy()
    {
        ClearRope();
    }
}

/// <summary>
/// Helper component that draws a continuous LineRenderer for a severed falling rope segment.
/// </summary>
public class SeveredRopeVisual : MonoBehaviour
{
    private LineRenderer line;
    private List<GameObject> links;

    public void Initialize(LineRenderer line, List<GameObject> links)
    {
        this.line = line;
        this.links = links;
    }

    void LateUpdate()
    {
        if (line == null || links == null)
        {
            Destroy(gameObject);
            return;
        }

        links.RemoveAll(item => item == null);
        if (links.Count == 0)
        {
            Destroy(gameObject);
            return;
        }

        line.positionCount = links.Count;
        for (int i = 0; i < links.Count; i++)
        {
            line.SetPosition(i, links[i].transform.position);
        }
    }
}

