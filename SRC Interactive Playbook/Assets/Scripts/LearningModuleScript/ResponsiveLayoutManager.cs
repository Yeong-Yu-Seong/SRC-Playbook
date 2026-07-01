// ============================================================
// ResponsiveLayoutManager.cs
// Detects screen orientation / aspect ratio and applies
// layout variants so the scenario player looks good on both
// mobile (portrait, ~390px wide) and desktop (landscape, 16:9+).
//
// Attach to a root Canvas GameObject.
// ============================================================

using UnityEngine;
using UnityEngine.UI;

namespace RedCross.Playbook.UI
{
    [RequireComponent(typeof(CanvasScaler))]
    public class ResponsiveLayoutManager : MonoBehaviour
    {
        // ── Inspector ──────────────────────────────────────────────
        [Header("Layout Roots")]
        [SerializeField] private GameObject mobileLayout;   // Portrait UI hierarchy
        [SerializeField] private GameObject desktopLayout;  // Landscape UI hierarchy

        [Header("Breakpoint")]
        [Tooltip("Aspect ratio (width/height) below which mobile layout is used.")]
        [SerializeField] private float mobileBreakpoint = 0.75f; // ~3:4

        [Header("Canvas Scaler")]
        [SerializeField] private Vector2 mobileReferenceResolution = new(390, 844);
        [SerializeField] private Vector2 desktopReferenceResolution = new(1920, 1080);

        private CanvasScaler _scaler;
        private bool _lastWasMobile;

        // ══════════════════════════════════════════════════════════
        // Lifecycle
        // ══════════════════════════════════════════════════════════

        private void Awake()
        {
            _scaler = GetComponent<CanvasScaler>();
        }

        private void Start() => ApplyLayout(force: true);
        private void Update() => ApplyLayout(force: false);

        // ══════════════════════════════════════════════════════════
        // Layout selection
        // ══════════════════════════════════════════════════════════

        private void ApplyLayout(bool force)
        {
            float aspect = (float)Screen.width / Screen.height;
            bool isMobile = aspect < mobileBreakpoint;

            if (!force && isMobile == _lastWasMobile) return;
            _lastWasMobile = isMobile;

            if (mobileLayout != null) mobileLayout.SetActive(isMobile);
            if (desktopLayout != null) desktopLayout.SetActive(!isMobile);

            _scaler.referenceResolution = isMobile
                ? mobileReferenceResolution
                : desktopReferenceResolution;

            // Match width on mobile (pillar-box), match height on desktop (letter-box)
            _scaler.matchWidthOrHeight = isMobile ? 0f : 1f;

            Debug.Log($"[ResponsiveLayout] aspect={aspect:F2} → {(isMobile ? "mobile" : "desktop")} layout");
        }
    }
}