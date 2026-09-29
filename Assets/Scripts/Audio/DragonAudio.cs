using Dragon.Core;
using UnityEngine;

namespace Dragon.Audio
{
    /// <summary>
    /// Combat audio for a single dragon.
    ///
    /// Attack sounds are driven by Animator state entry — the same seam
    /// <see cref="Dragon.VFX.AbilityVFXController"/> already uses to drive its particles — and
    /// damage sounds are driven by <see cref="Health.Damaged"/>. Between them those two seams
    /// cover every combat event without a line changing in PlayerController, AIController,
    /// CombatController or Health.
    ///
    /// Every clip slot is optional. An unassigned clip is skipped silently, so the scene can be
    /// wired up now and clips dropped in whenever an SFX pack is imported — no code change is
    /// needed at that point.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class DragonAudio : MonoBehaviour
    {
        [Header("Attack Clips")]
        [Tooltip("Audio clip for the fire breath attack.")]
        [SerializeField] private AudioClip fireBreathStartClip;

        [Tooltip("Tail swipe whoosh. Covers both the Tail Swipe ability and CombatController's " +
                 "melee auto-attack — they share the TailAttack animator state.")]
        [SerializeField] private AudioClip meleeSwingClip;

        [Tooltip("Fly Strike launch — wing beat or dive whoosh.")]
        [SerializeField] private AudioClip flyStrikeClip;

        [Header("Reaction Clips")]
        [Tooltip("Played when an auto-attack lands on this dragon.")]
        [SerializeField] private AudioClip lightHitClip;

        [Tooltip("Played when an ability lands on this dragon. DamageInfo.IsHeavy picks this " +
                 "over lightHitClip, so it stays in step with ability damage automatically.")]
        [SerializeField] private AudioClip heavyHitClip;

        [Tooltip("Death roar. Played once, on the killing blow.")]
        [SerializeField] private AudioClip deathRoarClip;

        private Animator animator;
        private Health health;

        /// <summary>Layered one-shots. PlayOneShot lets overlapping hits stack naturally.</summary>
        private AudioSource oneShotSource;

        private int previousState;

        private static readonly int FlameAttackState = Animator.StringToHash("FlameAttack");
        private static readonly int TailAttackState = Animator.StringToHash("TailAttack");
        private static readonly int FlyAttackState = Animator.StringToHash("FlyAttack");

        private void Awake()
        {
            animator = GetComponent<Animator>();
            health = GetComponent<Health>();

            oneShotSource = CreateSource();

            if (health != null)
            {
                health.Damaged += HandleDamaged;
                health.Died += HandleDied;
            }
        }

        private void OnDestroy()
        {
            if (health == null) return;

            health.Damaged -= HandleDamaged;
            health.Died -= HandleDied;
        }

        /// <summary>
        /// Build the sources in code rather than asking the scene to carry two hand-configured
        /// AudioSources that then have to be kept in sync between both dragons.
        /// </summary>
        private AudioSource CreateSource()
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();

            source.playOnAwake = false;
            source.loop = false;
            source.spatialBlend = 0.85f;
            source.rolloffMode = AudioRolloffMode.Linear;
            source.minDistance = 4f;
            source.maxDistance = 45f;
            source.dopplerLevel = 0f; // Dragons move fast enough that Doppler reads as a bug.

            return source;
        }

        /// <summary>
        /// Watch for Animator state transitions. Only the frame a state is entered or left is
        /// interesting, so every other frame costs a single hash compare.
        /// </summary>
        private void Update()
        {
            if (animator == null) return;

            int state = animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
            if (state == previousState) return;

            HandleStateChanged(state, previousState);
            previousState = state;
        }

        private void HandleStateChanged(int newState, int oldState)
        {
            if (newState == FlameAttackState)
            {
                PlayOneShot(fireBreathStartClip, 1.0f);
            }
            else if (newState == TailAttackState)
            {
                PlayOneShot(meleeSwingClip, 1.0f);
            }
            else if (newState == FlyAttackState)
            {
                PlayOneShot(flyStrikeClip, 1.0f);
            }
        }

        private void HandleDamaged(DamageInfo info)
        {
            PlayOneShot(info.IsHeavy ? heavyHitClip : lightHitClip, 1.0f);
        }

        private void HandleDied(Health _)
        {
            PlayOneShot(deathRoarClip, 1.0f);
        }

        private void PlayOneShot(AudioClip clip, float volume)
        {
            if (clip == null || oneShotSource == null) return;

            oneShotSource.PlayOneShot(clip, volume);
        }
    }
}
