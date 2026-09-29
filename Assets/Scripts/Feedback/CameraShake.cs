using UnityEngine;

namespace Dragon.Feedback
{
    /// <summary>
    /// Damped noise shake for the Main Camera.
    ///
    /// The arena camera is static — its only components are Transform, Camera, AudioListener
    /// and UniversalAdditionalCameraData, with nothing driving its position — so this can own
    /// the transform outright. It caches the resting pose in Awake and writes
    /// <c>base + offset</c> every LateUpdate, restoring the exact cached pose on the frame the
    /// shake ends, so repeated shakes cannot make the camera drift.
    ///
    /// Driven by unscaled time, so the shake still reads during the hit-stop that
    /// <see cref="HitFeedback"/> applies on heavy hits.
    ///
    /// Callers reach it through <see cref="Instance"/> with a null-conditional call, so a scene
    /// without this component simply has no shake rather than an error.
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        [Header("Shake Shape")]
        [Tooltip("Positional offset in world units at full strength.")]
        [SerializeField] private float maxOffset = 0.45f;

        [Tooltip("Roll, in degrees, at full strength. Small values read as impact; large " +
                 "values read as a bug.")]
        [SerializeField] private float maxRoll = 1.2f;

        [Tooltip("How fast the noise moves. Higher is a sharper rattle, lower is a slow sway.")]
        [SerializeField] private float frequency = 24f;

        private Vector3 basePosition;
        private Quaternion baseRotation;

        /// <summary>Current shake strength, 0 to 1. Decays to zero, then the camera is parked.</summary>
        private float amplitude;

        /// <summary>Amplitude lost per second, derived from the duration the caller asked for.</summary>
        private float decayRate;

        /// <summary>Per-instance noise offset, so the two axes never sample identical values.</summary>
        private float seed;

        private void Awake()
        {
            Instance = this;

            basePosition = transform.localPosition;
            baseRotation = transform.localRotation;
            seed = Random.value * 100f;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void OnDisable()
        {
            // Leave the camera exactly where it was found, whatever state the shake was in.
            amplitude = 0f;
            transform.localPosition = basePosition;
            transform.localRotation = baseRotation;
        }

        /// <summary>
        /// Shake the camera. Overlapping calls take the stronger shake rather than summing, so
        /// a flurry of melee hits cannot accumulate into an unreadable screen.
        /// </summary>
        /// <param name="strength">0 to 1, clamped.</param>
        /// <param name="duration">Seconds of real time to decay over.</param>
        public void Shake(float strength, float duration)
        {
            strength = Mathf.Clamp01(strength);
            if (strength <= 0f || duration <= 0f) return;

            amplitude = Mathf.Max(amplitude, strength);
            decayRate = amplitude / duration;
        }

        /// <summary>Cancel any running shake and snap back to the resting pose.</summary>
        public void StopShake()
        {
            amplitude = 0f;
            transform.localPosition = basePosition;
            transform.localRotation = baseRotation;
        }

        /// <summary>
        /// LateUpdate so the offset is applied after anything that might move the camera in
        /// Update, and is the last word on where the camera sits this frame.
        /// </summary>
        private void LateUpdate()
        {
            if (amplitude <= 0f) return; // Parked. Nothing written, nothing to fight over.

            amplitude = Mathf.Max(0f, amplitude - decayRate * Time.unscaledDeltaTime);

            if (amplitude <= 0.0001f)
            {
                StopShake();
                return;
            }

            float t = (Time.unscaledTime + seed) * frequency;

            // PerlinNoise returns 0..1; remap to -1..1 and sample three uncorrelated lines
            // through the noise field so the axes do not move in lockstep.
            float x = Mathf.PerlinNoise(t, 0f) * 2f - 1f;
            float y = Mathf.PerlinNoise(0f, t) * 2f - 1f;
            float roll = Mathf.PerlinNoise(t, t) * 2f - 1f;

            // Squared falloff: the shake hits hard and settles quickly instead of trailing off.
            float eased = amplitude * amplitude;

            // Offset along the camera's own right and up, expressed in the same space as
            // localPosition, so this stays correct whether or not the camera has a parent.
            Vector3 right = baseRotation * Vector3.right;
            Vector3 up = baseRotation * Vector3.up;

            transform.localPosition = basePosition + (right * x + up * y) * (maxOffset * eased);
            transform.localRotation = baseRotation * Quaternion.Euler(0f, 0f, roll * maxRoll * eased);
        }
    }
}
