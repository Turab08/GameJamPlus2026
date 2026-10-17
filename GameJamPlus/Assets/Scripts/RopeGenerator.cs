using System.Collections.Generic;
using UnityEngine;

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
            previousRigidbody.bodyType = RigidbodyType2D.Static; // Fixed: was Rigidbody2DType.Static
        }

        // Determine link size
        float linkSize = CalculateLinkSize();

        Collider2D previousCollider = GetComponent<Collider2D>();

        for (int i = 0; i < ropeLength; i++)
        {
            // Spawn each link staggered along -transform.up so they hang naturally instead of overlapping and exploding
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

            // Prevent collision jitter between adjacent links
            Collider2D currentCol = newLink.GetComponent<Collider2D>();
            if (previousCollider != null && currentCol != null)
            {
                Physics2D.IgnoreCollision(previousCollider, currentCol);
            }
            previousCollider = currentCol;

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

    private float CalculateLinkSize()
    {
        if (linkDistance > 0f) return linkDistance;

        SpriteRenderer sr = linkPrefab.GetComponentInChildren<SpriteRenderer>();
        if (sr != null && sr.sprite != null)
        {
            float spriteHeight = (sr.sprite.rect.height / sr.sprite.pixelsPerUnit) * linkPrefab.transform.localScale.y;
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
        if (lineRenderer == null || activeLinks.Count == 0) return;

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
