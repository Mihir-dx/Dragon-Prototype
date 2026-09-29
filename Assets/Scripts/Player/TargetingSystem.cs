using Dragon.Core;
using UnityEngine;

namespace Dragon.Player
{
    /// <summary>
    /// Lock-on targeting for the player.
    /// Left-click an enemy to lock, left-click the ground to unlock.
    /// Tab / middle-mouse also toggles lock to the nearest enemy.
    ///
    /// Input is not read here — <see cref="PlayerController"/> owns the bindings and calls
    /// <see cref="ToggleLock"/>, so the actions are enabled in exactly one place.
    /// </summary>
    public class TargetingSystem : MonoBehaviour
    {
        [Header("Targeting")]
        [Tooltip("Ring spawned under the locked-on enemy.")]
        [SerializeField] private GameObject targetIndicatorPrefab;

        [Tooltip("Tag used to find candidates when toggling lock to the nearest enemy.")]
        [SerializeField] private string enemyTag = "Enemy";

        public Transform CurrentTarget { get; private set; }

        private GameObject indicatorInstance;
        private Health targetHealth;
        private Health ownHealth;

        private void Awake()
        {
            ownHealth = GetComponent<Health>();
            if (ownHealth != null) ownHealth.Died += HandleOwnerDied;
        }

        private void OnDestroy()
        {
            if (ownHealth != null) ownHealth.Died -= HandleOwnerDied;
        }

        /// <summary>Dropping the lock on death is handled by OnDisable, so this only switches off.</summary>
        private void HandleOwnerDied(Health _) => enabled = false;

        /// <summary>
        /// Disabling this component drops the lock, so nothing else has to remember to.
        /// That is what stops the ring outliving a dead player.
        /// </summary>
        private void OnDisable() => ClearTarget();

        /// <summary>Tab / middle-mouse: lock to the nearest enemy, or drop the current lock.</summary>
        public void ToggleLock()
        {
            if (CurrentTarget != null)
            {
                ClearTarget();
            }
            else
            {
                LockNearestEnemy();
            }
        }

        /// <summary>Called by <see cref="PlayerController"/> when an enemy is clicked.</summary>
        public void SetTarget(Transform target)
        {
            ClearTarget();

            if (target == null) return;

            CurrentTarget = target;

            // Subscribe rather than poll IsDead every frame: the target tells us when it dies.
            targetHealth = target.GetComponentInParent<Health>();
            if (targetHealth != null) targetHealth.Died += HandleTargetDied;

            ShowIndicator();
        }

        /// <summary>Drop the lock and remove the ring. Safe to call when nothing is locked.</summary>
        public void ClearTarget()
        {
            if (targetHealth != null)
            {
                targetHealth.Died -= HandleTargetDied;
                targetHealth = null;
            }

            CurrentTarget = null;

            if (indicatorInstance != null)
            {
                Destroy(indicatorInstance);
                indicatorInstance = null;
            }
        }

        private void HandleTargetDied(Health _) => ClearTarget();

        private void LockNearestEnemy()
        {
            GameObject[] enemies = GameObject.FindGameObjectsWithTag(enemyTag);
            float nearestDist = float.MaxValue;
            Transform nearestEnemy = null;

            foreach (GameObject enemy in enemies)
            {
                // Skip corpses — locking onto a dead dragon is never what the player wants.
                Health enemyHealth = enemy.GetComponent<Health>();
                if (enemyHealth != null && enemyHealth.IsDead) continue;

                float dist = Vector3.Distance(transform.position, enemy.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearestEnemy = enemy.transform;
                }
            }

            if (nearestEnemy != null) SetTarget(nearestEnemy);
        }

        private void ShowIndicator()
        {
            if (targetIndicatorPrefab == null || CurrentTarget == null) return;

            indicatorInstance = Instantiate(targetIndicatorPrefab, CurrentTarget);
            indicatorInstance.transform.localPosition = Vector3.zero;
        }

        /// <summary>
        /// Death is event-driven, but a target that is *destroyed* raises nothing, so this
        /// catches the Transform going null and tidies up the ring it left behind.
        /// </summary>
        private void Update()
        {
            if (CurrentTarget == null && indicatorInstance != null)
            {
                ClearTarget();
            }
        }
    }
}
