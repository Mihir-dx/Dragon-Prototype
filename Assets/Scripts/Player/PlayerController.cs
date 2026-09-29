using System.Collections.Generic;
using Dragon.Core;
using Dragon.UI;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.InputSystem;

namespace Dragon.Player
{
    [RequireComponent(typeof(NavMeshAgent), typeof(Animator))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float rotationSpeed = 720f;

        [Tooltip("How far from an enemy's centre to stop when right-clicking it. " +
                 "Keeps the dragon at melee reach instead of walking into the model.")]
        [SerializeField] private float enemyApproachDistance = 2.5f;

        [Header("Click Indicator")]
        [SerializeField] private GameObject clickIndicatorPrefab;

        [Tooltip("Lift of the ground marker above the clicked point, so it does not z-fight " +
                 "with the terrain.")]
        [SerializeField] private float markerHeightOffset = 0.05f;

        [Header("Clicking")]
        [Tooltip("How far the cursor ray is allowed to travel when looking for ground or an enemy.")]
        [SerializeField] private float clickRayDistance = 200f;

        [Tooltip("Which layers a click can land on. Defaults to everything except Ignore Raycast.")]
        [SerializeField] private LayerMask clickableLayers = ~(1 << 2);

        [Tooltip("Tag that marks a collider as a hostile dragon.")]
        [SerializeField] private string enemyTag = "Enemy";

        [Header("Abilities")]
        [Tooltip("Max distance at which an ability can land on the locked-on target.")]
        [SerializeField] private float abilityRange = 8f;

        [Tooltip("One entry per ability. Add an entry and a matching trigger on the " +
                 "Animator to add a fourth — no code change needed.")]
        [SerializeField]
        private Ability[] abilities =
        {
            new Ability("Flame Breath", 25f, 8f, "FlameAttack", "<Keyboard>/1"),
            new Ability("Tail Swipe", 18f, 4f, "TailAttack", "<Keyboard>/2"),
            new Ability("Fly Strike", 30f, 12f, "FlyAttack", "<Keyboard>/3"),
        };

        [Header("Input")]
        [SerializeField]
        private InputAction moveCommand =
            new InputAction("Move", InputActionType.Button, "<Mouse>/rightButton");

        [SerializeField]
        private InputAction selectCommand =
            new InputAction("Select", InputActionType.Button, "<Mouse>/leftButton");

        [SerializeField]
        private InputAction lockOnCommand = BuildLockOnAction();

        public IReadOnlyList<Ability> Abilities => abilities;

        private NavMeshAgent agent;
        private Animator animator;
        private TargetingSystem targeting;
        private Health health;
        private Camera mainCamera;
        private Ability lastCastAbility;
        private ClickIndicator clickIndicator;

        // Cached hash for the FlyAttack state so we can grant invulnerability while airborne.
        private static readonly int FlyAttackState = Animator.StringToHash("FlyAttack");
        private static readonly int LandingState = Animator.StringToHash("Landing");
        private bool isInFlyAttack;

        private static InputAction BuildLockOnAction()
        {
            var action = new InputAction("LockOn", InputActionType.Button, "<Keyboard>/tab");
            action.AddBinding("<Mouse>/middleButton");
            return action;
        }

        private void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            animator = GetComponent<Animator>();
            targeting = GetComponent<TargetingSystem>();
            health = GetComponent<Health>();
            mainCamera = Camera.main;

            if (abilities == null) abilities = System.Array.Empty<Ability>();

            EnsureBinding(moveCommand, "<Mouse>/rightButton");
            EnsureBinding(selectCommand, "<Mouse>/leftButton");
            EnsureBinding(lockOnCommand, "<Keyboard>/tab", "<Mouse>/middleButton");

            foreach (Ability ability in abilities)
            {
                ability?.Initialise();
            }

            if (health != null) health.Died += HandleDied;
        }

        private void OnDestroy()
        {
            if (health != null) health.Died -= HandleDied;
        }

        private static void EnsureBinding(InputAction action, params string[] paths)
        {
            if (action == null || action.bindings.Count > 0) return;

            foreach (string path in paths)
            {
                action.AddBinding(path);
            }
        }

        private void OnEnable()
        {
            moveCommand.performed += OnMove;
            selectCommand.performed += OnSelect;
            lockOnCommand.performed += OnLockOn;

            moveCommand.Enable();
            selectCommand.Enable();
            lockOnCommand.Enable();

            foreach (Ability ability in abilities)
            {
                if (ability?.Input == null) continue;

                ability.Input.performed += OnAbilityPerformed;
                ability.Input.Enable();
            }
        }

        private void OnDisable()
        {
            moveCommand.performed -= OnMove;
            selectCommand.performed -= OnSelect;
            lockOnCommand.performed -= OnLockOn;

            moveCommand.Disable();
            selectCommand.Disable();
            lockOnCommand.Disable();

            foreach (Ability ability in abilities)
            {
                if (ability?.Input == null) continue;

                ability.Input.performed -= OnAbilityPerformed;
                ability.Input.Disable();
            }
        }

        private void HandleDied(Health _)
        {
            enabled = false;

            if (animator != null) animator.SetFloat(AnimatorParams.Speed, 0f);

            if (agent != null)
            {
                if (agent.enabled && agent.isOnNavMesh) agent.ResetPath();
                agent.enabled = false;
            }
        }

        private bool IsControllable()
        {
            if (health != null && health.IsDead) return false;
            return agent != null && agent.enabled && agent.isOnNavMesh;
        }

        private bool TryGetCursorRay(out Ray ray)
        {
            if (mainCamera == null) mainCamera = Camera.main;

            if (mainCamera == null || Mouse.current == null)
            {
                ray = default;
                return false;
            }

            ray = mainCamera.ScreenPointToRay(Mouse.current.position.ReadValue());
            return true;
        }

        private bool TryRaycastFromCursor(out RaycastHit hit)
        {
            hit = default;
            return TryGetCursorRay(out Ray ray)
                   && Physics.Raycast(ray, out hit, clickRayDistance, clickableLayers);
        }

        // Right click to move to ground position or approach an enemy
        private void OnMove(InputAction.CallbackContext context)
        {
            if (!IsControllable())
            {
                Debug.Log($"[OnMove] Ignored because IsControllable is false. Dead: {health?.IsDead}, AgentEnabled: {agent?.enabled}, OnNavMesh: {agent?.isOnNavMesh}");
                return;
            }
            if (!TryRaycastFromCursor(out RaycastHit hit))
            {
                Debug.Log($"[OnMove] Raycast failed. Cursor: {Mouse.current?.position.ReadValue()}");
                return;
            }

            Debug.Log($"[OnMove] Hit {hit.collider.name} on layer {hit.collider.gameObject.layer} at {hit.point}");

            if (hit.collider.CompareTag(enemyTag))
            {
                if (targeting != null) targeting.SetTarget(hit.collider.transform);
                Vector3 dest = GetApproachPoint(hit.collider.transform.position);
                agent.SetDestination(dest);
                Debug.Log($"[OnMove] Approaching enemy at {dest}");
            }
            else
            {
                agent.SetDestination(hit.point);
                PingClickIndicator(hit.point);
                Debug.Log($"[OnMove] Moving to ground at {hit.point}");
            }
        }

        private Vector3 GetApproachPoint(Vector3 targetPosition)
        {
            Vector3 fromTarget = transform.position - targetPosition;
            fromTarget.y = 0f;

            if (fromTarget.sqrMagnitude < 0.0001f) return targetPosition;

            return targetPosition + fromTarget.normalized * enemyApproachDistance;
        }

        // Left click to lock on to enemy
        private void OnSelect(InputAction.CallbackContext context)
        {
            if (!IsControllable() || targeting == null) return;
            if (!TryRaycastFromCursor(out RaycastHit hit)) return;

            if (hit.collider.CompareTag(enemyTag))
            {
                targeting.SetTarget(hit.collider.transform);
            }
            else
            {
                targeting.ClearTarget();
            }
        }

        // Tab or middle mouse to toggle lock to nearest enemy
        private void OnLockOn(InputAction.CallbackContext context)
        {
            if (!IsControllable()) return;

            if (targeting != null) targeting.ToggleLock();
        }

        private void PingClickIndicator(Vector3 position)
        {
            if (clickIndicator == null)
            {
                if (clickIndicatorPrefab == null) return;

                GameObject instance = Instantiate(clickIndicatorPrefab, position, Quaternion.identity);

                clickIndicator = instance.GetComponent<ClickIndicator>();
                if (clickIndicator == null) clickIndicator = instance.AddComponent<ClickIndicator>();
            }

            clickIndicator.Ping(position + Vector3.up * markerHeightOffset);
        }

        // Abilities Functions
        private void OnAbilityPerformed(InputAction.CallbackContext context)
        {
            foreach (Ability ability in abilities)
            {
                if (ability?.Input != context.action) continue;

                TryCastAbility(ability);
                return;
            }
        }

        private void TryCastAbility(Ability ability)
        {
            if (!IsControllable()) return;

            // Doesn't interrupt an attack.
            if (!AnimatorParams.IsInterruptible(animator)) return;

            if (!ability.IsReady) return;

            StopAndAim();
            animator.SetTrigger(ability.TriggerId);
            ability.MarkUsed();
            lastCastAbility = ability;

            // Grant invulnerability during the FlyAttack (airborne) animation.
            if (ability.TriggerId == FlyAttackState && health != null)
            {
                isInFlyAttack = true;
                health.IsInvulnerable = true;
            }
        }

        public void OnAbilityHit()
        {
            if (lastCastAbility != null)
            {
                DamageLockedTarget(lastCastAbility.Damage);
                lastCastAbility = null;
            }
        }

        private void AutoAim()
        {
            if (targeting == null || targeting.CurrentTarget == null) return;

            Vector3 direction = (targeting.CurrentTarget.position - transform.position).normalized;
            direction.y = 0f;

            if (direction.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }

        private void DamageLockedTarget(float damage)
        {
            if (targeting == null || targeting.CurrentTarget == null) return;

            if (Vector3.Distance(transform.position, targeting.CurrentTarget.position) > abilityRange) return;

            Health targetHealth = targeting.CurrentTarget.GetComponentInParent<Health>();
            if (targetHealth != null && !targetHealth.IsDead)
            {
                targetHealth.TakeDamage(damage, forceHitReaction: true);
            }
        }

        private void StopAndAim()
        {
            agent.ResetPath(); // Stop moving to cast.
            AutoAim();
        }

        private void Update()
        {
            if (!IsControllable()) return;

            // Clear invulnerability once the FlyAttack + Landing sequence is over.
            if (isInFlyAttack && health != null)
            {
                int state = animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
                bool stillAirborne = state == FlyAttackState || state == LandingState;
                if (!stillAirborne)
                {
                    isInFlyAttack = false;
                    health.IsInvulnerable = false;
                }
            }

            if (!AnimatorParams.IsInterruptible(animator))
            {
                agent.isStopped = true;
                animator.SetFloat(AnimatorParams.Speed, 0f);
                return;
            }

            agent.isStopped = false;
            animator.SetFloat(AnimatorParams.Speed, agent.velocity.magnitude);

            if (agent.velocity.sqrMagnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(agent.velocity.normalized);
                transform.rotation = Quaternion.RotateTowards(
                    transform.rotation,
                    targetRot,
                    rotationSpeed * Time.deltaTime);
            }
        }
    }
}

// Code by Mihir with the help of AI