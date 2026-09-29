using UnityEngine;

namespace Dragon.UI
{
    /// <summary>
    /// Ground "ping" marker shown where the player ordered a move.
    ///
    /// Designed to be REUSED rather than spawned per click: call <see cref="Ping"/> to
    /// move the ring to a new spot and restart its fade from full opacity. When the fade
    /// finishes the object deactivates itself instead of being destroyed, so one instance
    /// serves every click for the whole session (no allocation churn, no leaked material
    /// instances, and no stacking of stale rings).
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    public class ClickIndicator : MonoBehaviour
    {
        [Header("Fade")]
        [Tooltip("Seconds for the ring to fade from fully opaque to invisible.")]
        [SerializeField] private float lifetime = 1.25f;

        [Header("Pulse")]
        [Tooltip("Ring scale at the moment of the click.")]
        [SerializeField] private float startScale = 1.15f;

        [Tooltip("Ring scale as the fade completes.")]
        [SerializeField] private float endScale = 0.85f;

        private LineRenderer lr;
        private Material mat;
        private Color baseColor;
        private float pingTime;
        private bool isPlaying;

        private void Awake()
        {
            Initialize();
        }

        /// <summary>
        /// Grab the renderer + its instanced material once. Safe to call repeatedly:
        /// Ping() may arrive before Awake() when the object is created and used in the
        /// same frame, so this guards against a null material.
        /// </summary>
        private void Initialize()
        {
            if (lr != null) return;

            lr = GetComponent<LineRenderer>();
            if (lr == null) return;

            // Touching .material instantiates a per-object copy, so tinting alpha here
            // never bleeds into the shared asset. Done once for the lifetime of the pool.
            mat = lr.material;
            if (mat != null) baseColor = mat.color;
        }

        /// <summary>
        /// Move the ring to <paramref name="position"/> and restart the fade from the top.
        /// This is the "reset" behaviour: re-pinging while a fade is still running snaps
        /// the marker to the new spot and begins again rather than leaving the old ring up.
        /// </summary>
        public void Ping(Vector3 position)
        {
            Initialize();

            transform.position = position;
            pingTime = Time.time;
            isPlaying = true;

            if (!gameObject.activeSelf) gameObject.SetActive(true);

            ApplyFade(0f);
        }

        private void OnEnable()
        {
            // If something enables this without calling Ping (e.g. it was placed in the
            // scene by hand), still run one clean fade cycle rather than sitting opaque.
            if (isPlaying) return;

            pingTime = Time.time;
            isPlaying = true;
            ApplyFade(0f);
        }

        private void Update()
        {
            if (!isPlaying) return;

            float t = lifetime <= 0f ? 1f : (Time.time - pingTime) / lifetime;

            if (t >= 1f)
            {
                // Fade complete: fully transparent, then park the object for reuse.
                ApplyFade(1f);
                isPlaying = false;
                gameObject.SetActive(false);
                return;
            }

            ApplyFade(t);
        }

        /// <summary>Drive alpha and the subtle scale pulse from normalised time t (0..1).</summary>
        private void ApplyFade(float t)
        {
            if (mat != null)
            {
                Color c = baseColor;
                c.a = Mathf.Lerp(1f, 0f, t);
                mat.color = c;
            }

            transform.localScale = Vector3.one * Mathf.Lerp(startScale, endScale, t);
        }
    }
}
