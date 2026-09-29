using System.Collections;
using Dragon.Core;
using UnityEngine;

namespace Dragon.Feedback
{
    /// <summary>
    /// Physical feedback for hits landing on this dragon: screen shake, and a brief hit-stop on
    /// heavy hits so a landed ability has weight.
    ///
    /// Subscribes to <see cref="Health.Damaged"/> and <see cref="Health.Died"/>, so it adds no
    /// coupling to the combat scripts. Knockback is deliberately not attempted — both dragons
    /// are driven by a NavMeshAgent, and shoving the transform would fight the agent.
    ///
    /// Hit-stop is the only global state anything in this feature touches, so it is guarded
    /// carefully: capped at a tenth of a second of real time, owned by one instance at a time,
    /// and always hard-restored to 1 rather than to a cached value that could latch a bad state.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class HitFeedback : MonoBehaviour
    {
        [Header("Screen Shake")]
        [Tooltip("Shake strength for auto-attack hits. Deliberately subtle — these land often.")]
        [Range(0f, 1f)]
        [SerializeField] private float lightHitShake = 0.18f;

        [Tooltip("Shake strength for ability hits.")]
        [Range(0f, 1f)]
        [SerializeField] private float heavyHitShake = 0.55f;

        [Tooltip("Shake strength for the killing blow.")]
        [Range(0f, 1f)]
        [SerializeField] private float deathShake = 0.8f;

        [Tooltip("Seconds a hit shake decays over.")]
        [SerializeField] private float shakeDuration = 0.25f;

        [Tooltip("Seconds the death shake decays over.")]
        [SerializeField] private float deathShakeDuration = 0.5f;

        [Header("Hit-Stop")]
        [Tooltip("Briefly slow time when a heavy hit lands. Turn off to leave Time.timeScale " +
                 "completely untouched.")]
        [SerializeField] private bool hitStopEnabled = true;

        [Tooltip("Time scale held during the stop. Not zero, so animation still creeps forward " +
                 "and the freeze reads as impact rather than a hitch.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float hitStopTimeScale = 0.15f;

        [Tooltip("Real-time length of the stop on an ability hit. Capped low on purpose: " +
                 "WinScreenManager's reveal delay is scaled time, so a long stop would push " +
                 "the win screen back.")]
        [Range(0f, 0.1f)]
        [SerializeField] private float hitStopDuration = 0.06f;

        [Tooltip("Real-time length of the stop on the killing blow.")]
        [Range(0f, 0.1f)]
        [SerializeField] private float deathHitStopDuration = 0.1f;

        /// <summary>
        /// Whoever is currently holding time down. A reference rather than a bool because Unity
        /// reports a destroyed object as null, so a scene reload mid-stop releases the claim by
        /// itself instead of latching it for the rest of the session.
        /// </summary>
        private static HitFeedback hitStopOwner;

        private Health health;
        private Coroutine hitStopRoutine;

        private void Awake()
        {
            health = GetComponent<Health>();

            health.Damaged += HandleDamaged;
            health.Died += HandleDied;
        }

        private void OnDestroy()
        {
            if (health != null)
            {
                health.Damaged -= HandleDamaged;
                health.Died -= HandleDied;
            }

            ReleaseHitStop();
        }

        private void OnDisable()
        {
            // A disabled component cannot finish its coroutine, so hand time back here rather
            // than leaving the game running at 0.15 speed.
            ReleaseHitStop();
        }

        private void HandleDamaged(DamageInfo info)
        {
            if (info.IsHeavy)
            {
                CameraShake.Instance?.Shake(heavyHitShake, shakeDuration);
                BeginHitStop(hitStopDuration);
            }
            else
            {
                CameraShake.Instance?.Shake(lightHitShake, shakeDuration);
            }
        }

        /// <summary>
        /// Deliberately does not disable this component, unlike the combat scripts — the death
        /// shake and stop have to outlive the moment of death.
        /// </summary>
        private void HandleDied(Health _)
        {
            CameraShake.Instance?.Shake(deathShake, deathShakeDuration);

            // The killing blow raised Damaged a moment ago, so a short stop may already be
            // running on this same dragon. Replace it with the longer death stop instead of
            // being turned away by its own claim.
            CancelHitStop();
            BeginHitStop(deathHitStopDuration);
        }

        private void BeginHitStop(float duration)
        {
            if (!hitStopEnabled || duration <= 0f) return;
            if (!isActiveAndEnabled) return;

            // One dragon at a time. Two overlapping stops would race to restore the time scale.
            if (hitStopOwner != null) return;

            hitStopOwner = this;
            hitStopRoutine = StartCoroutine(HitStopRoutine(duration));
        }

        private IEnumerator HitStopRoutine(float duration)
        {
            Time.timeScale = hitStopTimeScale;

            // Real time, so the stop is not stretched by the slowdown it just applied.
            yield return new WaitForSecondsRealtime(duration);

            hitStopRoutine = null;
            ReleaseHitStop();
        }

        private void CancelHitStop()
        {
            if (hitStopRoutine != null)
            {
                StopCoroutine(hitStopRoutine);
                hitStopRoutine = null;
            }

            ReleaseHitStop();
        }

        private void ReleaseHitStop()
        {
            if (hitStopOwner != this) return;

            hitStopOwner = null;
            Time.timeScale = 1f; // Hard restore — never to a remembered value.
        }
    }
}
