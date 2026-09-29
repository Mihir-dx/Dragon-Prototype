using System;
using UnityEngine;

namespace Dragon.Core
{
    /// <summary>
    /// What a single hit did. Passed to <see cref="Health.Damaged"/> subscribers so the
    /// health bar and damage numbers are *told* what happened instead of diffing health
    /// every frame to work it out.
    /// </summary>
    public readonly struct DamageInfo
    {
        /// <summary>Damage requested by the attacker, before the clamp at zero health.</summary>
        public readonly float Amount;

        /// <summary>
        /// True for ability hits, false for auto-attacks. This is what lets the damage
        /// popup colour heavy hits without a threshold it has to keep in sync with
        /// ability damage by hand.
        /// </summary>
        public readonly bool IsHeavy;

        /// <summary>Health left after the hit, already clamped to zero.</summary>
        public readonly float RemainingHealth;

        public DamageInfo(float amount, bool isHeavy, float remainingHealth)
        {
            Amount = amount;
            IsHeavy = isHeavy;
            RemainingHealth = remainingHealth;
        }
    }

    /// <summary>
    /// Hit points, and nothing else.
    ///
    /// This component deliberately knows about no other gameplay script. It raises
    /// <see cref="Damaged"/> and <see cref="Died"/>; whoever cares subscribes and shuts
    /// itself down. Previously Die() reached into PlayerController, TargetingSystem,
    /// CombatController, AIController, the NavMeshAgent and the Collider by name, which
    /// meant a health script had to be edited every time a new component was added.
    ///
    /// The only things it still touches are on its own GameObject: the Animator, for the
    /// flinch and death clips, and its own Collider, which stops being hittable once dead.
    /// </summary>
    public class Health : MonoBehaviour
    {
        [Header("Health")]
        [Tooltip("Starting and maximum hit points. Both dragons use 100 by default.")]
        [SerializeField] private float maxHealth = 100f;

        private float currentHealth;
        private bool isDead;
        private Animator animator;
        private Collider ownCollider;

        /// <summary>Raised on every hit that lands, including the killing blow.</summary>
        public event Action<DamageInfo> Damaged;

        /// <summary>Raised once, after <see cref="IsDead"/> is already true.</summary>
        public event Action<Health> Died;

        public float CurrentHealth => currentHealth;
        public float MaxHealth => maxHealth;
        public bool IsDead => isDead;

        /// <summary>
        /// When true, all incoming damage is silently ignored. Used by PlayerController
        /// to protect the player during the FlyAttack animation (airborne).
        /// </summary>
        public bool IsInvulnerable { get; set; }

        /// <summary>Health as 1 → 0. Ready to drop into an Image.fillAmount.</summary>
        public float HealthNormalized => maxHealth <= 0f ? 0f : currentHealth / maxHealth;

        private void Awake()
        {
            currentHealth = maxHealth;
            animator = GetComponent<Animator>();
            ownCollider = GetComponent<Collider>();
        }

        /// <summary>
        /// Apply damage. <paramref name="forceHitReaction"/> is true for ability hits, which
        /// flinch the victim; auto-attacks pass false so they cannot stun-lock.
        /// </summary>
        public void TakeDamage(float amount, bool forceHitReaction = false)
        {
            if (isDead || IsInvulnerable) return;

            currentHealth = Mathf.Max(currentHealth - amount, 0f);

            // Raised before the death check so the killing blow still shows its damage number.
            Damaged?.Invoke(new DamageInfo(amount, forceHitReaction, currentHealth));

            if (currentHealth <= 0f)
            {
                Die();
                return;
            }

            if (forceHitReaction && animator != null)
            {
                animator.SetTrigger(AnimatorParams.GetHit);
            }
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            if (animator != null) animator.SetTrigger(AnimatorParams.Die);

            // Stop being a valid target for attacks and click-to-target raycasts.
            if (ownCollider != null) ownCollider.enabled = false;

            Died?.Invoke(this);
        }
    }
}
