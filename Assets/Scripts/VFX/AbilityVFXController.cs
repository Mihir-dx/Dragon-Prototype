using Dragon.Core;
using UnityEngine;

namespace Dragon.VFX
{
    /// <summary>
    /// Cosmetic VFX layer for dragon abilities.
    /// Watches the Animator for the FlameAttack and FlyAttack states, then spawns and
    /// manages fire particle effects from the ParticlePack.
    ///
    /// Purely cosmetic — it does not touch gameplay, damage or input.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class AbilityVFXController : MonoBehaviour
    {
        [Header("VFX Prefabs (from ParticlePack)")]
        [Tooltip("Fire breath effect for FlameAttack (e.g. FlameThrower prefab).")]
        [SerializeField] private GameObject flameAttackVFXPrefab;

        [Tooltip("Fire burst effect for FlyAttack (e.g. FireBall prefab).")]
        [SerializeField] private GameObject flyAttackVFXPrefab;

        [Header("Spawn Points")]
        [Tooltip("Transform at or near the dragon's mouth where fire breath spawns. " +
                 "If left empty, the Head bone is found automatically.")]
        [SerializeField] private Transform mouthPoint;

        [Header("Flame Attack Settings")]
        [Tooltip("Local-space offset from the mouth point to fine-tune fire breath position.")]
        [SerializeField] private Vector3 flameOffset = new Vector3(0f, 0f, 0.5f);

        [Tooltip("Euler rotation offset for the flame breath (e.g. tilt downward).")]
        [SerializeField] private Vector3 flameRotationOffset = Vector3.zero;

        [Header("Fly Attack Settings")]
        [Tooltip("Local-space offset from the mouth point for the fly-attack fire.")]
        [SerializeField] private Vector3 flyOffset = new Vector3(0f, 0f, 0.5f);

        [Tooltip("Euler rotation offset for the fly-attack fire. " +
                 "Default 45 degrees on X tilts the fire downward toward the ground.")]
        [SerializeField] private Vector3 flyRotationOffset = new Vector3(45f, 0f, 0f);

        private Animator animator;

        // Cached instances — reused across attacks so we don't Instantiate every frame.
        private GameObject flameVFXInstance;
        private GameObject flyVFXInstance;

        // State tracking to detect animation transitions.
        private bool wasInFlameAttack;
        private bool wasInFlyAttack;

        private static readonly int FlameAttackState = Animator.StringToHash("FlameAttack");
        private static readonly int FlyAttackState = Animator.StringToHash("FlyAttack");

        private void Awake()
        {
            animator = GetComponent<Animator>();
            AutoFindMouthPoint();
        }

        /// <summary>
        /// If the designer has not assigned a mouth point, fall back to the Head bone, and
        /// to the root if the skeleton has no Head at all.
        /// </summary>
        private void AutoFindMouthPoint()
        {
            if (mouthPoint != null) return;

            mouthPoint = TransformUtility.FindDeepChild(transform, "Head");

            if (mouthPoint == null)
            {
                Debug.LogWarning($"[AbilityVFX] No 'Head' bone found on {name}. " +
                                 "VFX will spawn at the root — assign MouthPoint manually.");
                mouthPoint = transform;
            }
        }

        private void Update()
        {
            if (animator == null) return;

            int state = animator.GetCurrentAnimatorStateInfo(0).shortNameHash;

            bool isInFlameAttack = state == FlameAttackState;
            bool isInFlyAttack = state == FlyAttackState;

            HandleVFXState(isInFlameAttack, wasInFlameAttack, ref flameVFXInstance,
                flameAttackVFXPrefab, flameOffset, flameRotationOffset);

            HandleVFXState(isInFlyAttack, wasInFlyAttack, ref flyVFXInstance,
                flyAttackVFXPrefab, flyOffset, flyRotationOffset);

            wasInFlameAttack = isInFlameAttack;
            wasInFlyAttack = isInFlyAttack;
        }

        /// <summary>
        /// Runs after the Animator has evaluated, so bone positions are final. This is what
        /// makes the VFX track the mouth exactly rather than lagging a frame behind.
        /// </summary>
        private void LateUpdate()
        {
            if (wasInFlameAttack && flameVFXInstance != null && flameVFXInstance.activeSelf)
            {
                SnapToMouth(flameVFXInstance.transform, flameOffset, flameRotationOffset);
            }

            if (wasInFlyAttack && flyVFXInstance != null && flyVFXInstance.activeSelf)
            {
                SnapToMouth(flyVFXInstance.transform, flyOffset, flyRotationOffset);
            }
        }

        /// <summary>Start the effect on the frame the state is entered, stop it on the frame it is left.</summary>
        private void HandleVFXState(bool isActive, bool wasActive, ref GameObject instance,
            GameObject prefab, Vector3 offset, Vector3 rotationOffset)
        {
            if (isActive && !wasActive)
            {
                StartVFX(ref instance, prefab, offset, rotationOffset);
            }
            else if (!isActive && wasActive)
            {
                StopVFX(instance);
            }
        }

        /// <summary>
        /// Position and rotate a VFX instance to follow the mouth point in world space.
        /// Deliberately not parented — world-space positioning keeps the effect tracking
        /// the bone even through complex animations like the fly loop.
        /// </summary>
        private void SnapToMouth(Transform vfx, Vector3 localOffset, Vector3 eulerOffset)
        {
            vfx.position = mouthPoint.TransformPoint(localOffset);
            vfx.rotation = mouthPoint.rotation * Quaternion.Euler(eulerOffset);
        }

        /// <summary>Activate (or create on first use) a VFX instance at the mouth point.</summary>
        private void StartVFX(ref GameObject instance, GameObject prefab,
            Vector3 localOffset, Vector3 eulerOffset)
        {
            if (prefab == null) return;

            if (instance == null)
            {
                instance = Instantiate(prefab);
                ForceLocalSimulationSpace(instance);
            }

            // Position at the mouth immediately so the first frame isn't at the origin.
            SnapToMouth(instance.transform, localOffset, eulerOffset);

            instance.SetActive(true);
            SetParticlesPlaying(instance, true);
        }

        private static void StopVFX(GameObject instance)
        {
            if (instance == null) return;

            SetParticlesPlaying(instance, false);
            instance.SetActive(false);
        }

        /// <summary>
        /// Force every ParticleSystem to Local simulation space. The ParticlePack prefabs
        /// default to World, which leaves particles at their birth position instead of
        /// travelling with the dragon.
        /// </summary>
        private static void ForceLocalSimulationSpace(GameObject root)
        {
            if (root == null) return;

            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
            foreach (ParticleSystem ps in systems)
            {
                ParticleSystem.MainModule main = ps.main;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
            }
        }

        /// <summary>
        /// Play or stop every ParticleSystem in a hierarchy. Uses Play/Stop rather than
        /// enable/disable so emission curves reset properly between casts.
        /// </summary>
        private static void SetParticlesPlaying(GameObject root, bool play)
        {
            if (root == null) return;

            ParticleSystem[] systems = root.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
            foreach (ParticleSystem ps in systems)
            {
                if (play)
                {
                    ps.Clear();
                    ps.Play();
                }
                else
                {
                    ps.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }

        private void OnDestroy()
        {
            if (flameVFXInstance != null) Destroy(flameVFXInstance);
            if (flyVFXInstance != null) Destroy(flyVFXInstance);
        }
    }
}
