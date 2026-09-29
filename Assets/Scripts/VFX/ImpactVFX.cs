using System.Collections.Generic;
using Dragon.Core;
using UnityEngine;

namespace Dragon.VFX
{
    /// <summary>
    /// Plays a hit-impact particle burst on this dragon whenever it takes damage.
    ///
    /// Driven entirely by <see cref="Health.Damaged"/>, so no attacker needs to know this
    /// exists — <see cref="DamageInfo.IsHeavy"/> already says whether the hit was an ability or
    /// an auto-attack, which is what selects the heavy or light prefab.
    ///
    /// Instances are pooled the same way <see cref="Dragon.UI.DamagePopup"/> pools its numbers,
    /// so a long fight allocates nothing after the first couple of hits.
    ///
    /// Positioning caveat: <see cref="DamageInfo"/> carries no contact point, only the amount,
    /// the heavy flag and the remaining health. Rather than widen Health.TakeDamage and touch
    /// all three calling controllers, the burst plays on the victim's body at
    /// <see cref="impactHeight"/> with a small random spread — the same approach DamagePopup
    /// already takes for its spawn height. On a dragon-sized target that reads correctly for
    /// both melee and fire.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class ImpactVFX : MonoBehaviour
    {
        [Header("Impact Prefabs")]
        [Tooltip("Burst for auto-attack hits. A smaller effect from the Hit Impact pack.")]
        [SerializeField] private GameObject lightHitPrefab;

        [Tooltip("Burst for ability hits. Picked automatically via DamageInfo.IsHeavy, so it " +
                 "stays in step with ability damage without a threshold to maintain.")]
        [SerializeField] private GameObject heavyHitPrefab;

        [Header("Placement")]
        [Tooltip("Height above the dragon's pivot where the burst plays — roughly torso height.")]
        [SerializeField] private float impactHeight = 2f;

        [Tooltip("Random horizontal offset, so repeated hits do not stack in exactly one spot.")]
        [SerializeField] private float horizontalSpread = 0.45f;

        [Tooltip("Random vertical offset on top of Impact Height.")]
        [SerializeField] private float verticalSpread = 0.35f;

        [Header("Scale")]
        [Tooltip("Scale multiplier applied to light hit bursts.")]
        [SerializeField] private float lightScale = 1f;

        [Tooltip("Scale multiplier applied to heavy hit bursts, so abilities read as bigger.")]
        [SerializeField] private float heavyScale = 1.5f;

        [Header("Pooling")]
        [Tooltip("Safety net: an instance is recycled after this many seconds even if its " +
                 "particles still report as alive. Stops a looping prefab from leaking the pool.")]
        [SerializeField] private float maxEffectLifetime = 4f;

        private Health health;

        /// <summary>One pool per prefab, so a light burst never recycles a heavy instance.</summary>
        private readonly List<PooledImpact> lightPool = new List<PooledImpact>();
        private readonly List<PooledImpact> heavyPool = new List<PooledImpact>();

        private void Awake()
        {
            health = GetComponent<Health>();
            health.Damaged += HandleDamaged;
        }

        private void OnDestroy()
        {
            if (health != null) health.Damaged -= HandleDamaged;

            DestroyPool(lightPool);
            DestroyPool(heavyPool);
        }

        private void HandleDamaged(DamageInfo info)
        {
            GameObject prefab = info.IsHeavy ? heavyHitPrefab : lightHitPrefab;
            if (prefab == null) return; // Unassigned slot is simply silent, like the audio.

            List<PooledImpact> pool = info.IsHeavy ? heavyPool : lightPool;
            float scale = info.IsHeavy ? heavyScale : lightScale;

            Vector3 position = transform.position + new Vector3(
                Random.Range(-horizontalSpread, horizontalSpread),
                impactHeight + Random.Range(-verticalSpread, verticalSpread),
                Random.Range(-horizontalSpread, horizontalSpread));

            // Random yaw so identical bursts do not look copy-pasted in a long exchange.
            Quaternion rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);

            GetFreeInstance(pool, prefab).Play(position, rotation, scale);
        }

        /// <summary>
        /// Park finished bursts. Checked once a frame rather than with a per-instance coroutine,
        /// which keeps the whole pool to a single Update regardless of how many hits land.
        /// </summary>
        private void Update()
        {
            RecycleFinished(lightPool);
            RecycleFinished(heavyPool);
        }

        private void RecycleFinished(List<PooledImpact> pool)
        {
            for (int i = 0; i < pool.Count; i++)
            {
                pool[i]?.RecycleIfFinished(maxEffectLifetime);
            }
        }

        /// <summary>
        /// Hand out an idle burst, creating one only when every existing instance is still
        /// playing. In practice the pool settles at however many hits overlap at once.
        /// </summary>
        private PooledImpact GetFreeInstance(List<PooledImpact> pool, GameObject prefab)
        {
            foreach (PooledImpact impact in pool)
            {
                if (impact != null && impact.IsFree) return impact;
            }

            var created = new PooledImpact(Instantiate(prefab));
            pool.Add(created);
            return created;
        }

        private static void DestroyPool(List<PooledImpact> pool)
        {
            foreach (PooledImpact impact in pool)
            {
                impact?.Dispose();
            }

            pool.Clear();
        }

        /// <summary>
        /// One reusable impact instance. Deliberately left unparented: a hit spark belongs to
        /// the spot where the blow landed, so it should stay there if the dragon walks on.
        /// </summary>
        private sealed class PooledImpact
        {
            private readonly GameObject root;
            private readonly ParticleSystem[] systems;
            private float startTime;

            public PooledImpact(GameObject instance)
            {
                root = instance;
                systems = root.GetComponentsInChildren<ParticleSystem>(includeInactive: true);
                root.SetActive(false);
            }

            public bool IsFree => root == null || !root.activeSelf;

            public void Play(Vector3 position, Quaternion rotation, float scale)
            {
                if (root == null) return;

                Transform t = root.transform;
                t.SetPositionAndRotation(position, rotation);
                t.localScale = Vector3.one * scale;

                startTime = Time.time;
                root.SetActive(true);

                // Clear then Play, matching AbilityVFXController, so emission curves restart
                // from zero instead of resuming mid-burst on a reused instance.
                foreach (ParticleSystem ps in systems)
                {
                    if (ps == null) continue;

                    ps.Clear();
                    ps.Play();
                }
            }

            public void RecycleIfFinished(float maxLifetime)
            {
                if (root == null || !root.activeSelf) return;

                bool expired = Time.time - startTime >= maxLifetime;
                if (!expired && IsAnyParticleAlive()) return;

                foreach (ParticleSystem ps in systems)
                {
                    if (ps == null) continue;

                    ps.Stop(withChildren: true, ParticleSystemStopBehavior.StopEmittingAndClear);
                }

                root.SetActive(false);
            }

            private bool IsAnyParticleAlive()
            {
                foreach (ParticleSystem ps in systems)
                {
                    if (ps != null && ps.IsAlive(withChildren: true)) return true;
                }

                return false;
            }

            public void Dispose()
            {
                // Fully qualified because this helper is a plain class, not a MonoBehaviour,
                // so it has no inherited Destroy.
                if (root != null) UnityEngine.Object.Destroy(root);
            }
        }
    }
}
