using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Dragon.Player
{
    /// <summary>
    /// One player ability: its key binding, damage, cooldown and animation trigger.
    ///
    /// Serialized as an array on <see cref="PlayerController"/> so abilities are authored
    /// entirely in the Inspector — a fourth ability needs a new array entry and a matching
    /// trigger on DragonAnimatorController, not a new method. This replaces the three
    /// near-identical OnFlameAttack / OnTailAttack / OnFlyAttack handlers.
    ///
    /// Cooldown state lives here rather than on the controller, which is what lets the
    /// ability HUD read <see cref="CooldownNormalized"/> straight off the ability it draws.
    ///
    /// This is a class, not a struct, on purpose: PlayerController iterates the array with
    /// foreach and calls <see cref="MarkUsed"/>, and a struct would be copied so the
    /// cooldown stamp would be silently discarded.
    /// </summary>
    [Serializable]
    public class Ability
    {
        [Tooltip("Name shown in the ability HUD.")]
        [SerializeField] private string displayName = "Ability";

        [Tooltip("Icon for the ability HUD. Optional — unused until the cooldown UI exists.")]
        [SerializeField] private Sprite icon;

        [Tooltip("Damage dealt to the locked-on target when this ability is cast.")]
        [SerializeField] private float damage = 20f;

        [Tooltip("Seconds before this ability can be cast again. 0 disables the cooldown.")]
        [SerializeField] private float cooldown = 5f;

        [Tooltip("Trigger parameter on DragonAnimatorController to fire when cast. " +
                 "Must match the controller exactly — a typo plays no animation and " +
                 "reports no error.")]
        [SerializeField] private string animatorTrigger = "FlameAttack";

        [Tooltip("Key or button that casts this ability.")]
        [SerializeField] private InputAction input = new InputAction(type: InputActionType.Button);

        /// <summary>
        /// Time.time of the last cast. Not serialized — cooldowns are runtime state and
        /// should not persist into the scene file. Negative infinity means "never cast",
        /// which makes every ability ready on the first frame without a special case.
        /// </summary>
        private float lastUsedTime = float.NegativeInfinity;

        public string DisplayName => displayName;
        public Sprite Icon => icon;
        public float Damage => damage;
        public float Cooldown => cooldown;
        public InputAction Input => input;

        /// <summary>Cached hash of <see cref="animatorTrigger"/>. Valid after <see cref="Initialise"/>.</summary>
        public int TriggerId { get; private set; }

        public bool IsReady => CooldownRemaining <= 0f;

        /// <summary>Seconds left on cooldown; 0 once ready.</summary>
        public float CooldownRemaining => Mathf.Max(0f, lastUsedTime + cooldown - Time.time);

        /// <summary>
        /// Cooldown as 1 → 0, ready at 0. Drop straight into an Image.fillAmount for a
        /// radial sweep. Always 0 when the ability has no cooldown.
        /// </summary>
        public float CooldownNormalized => cooldown <= 0f ? 0f : CooldownRemaining / cooldown;

        /// <summary>Required by Unity when the Inspector grows the ability array.</summary>
        public Ability() { }

        /// <summary>
        /// Used only by <see cref="PlayerController"/>'s field initializer to seed the
        /// project's three abilities with working bindings. Everything here is editable in
        /// the Inspector afterwards.
        /// </summary>
        public Ability(string displayName, float damage, float cooldown,
                       string animatorTrigger, string binding)
        {
            this.displayName = displayName;
            this.damage = damage;
            this.cooldown = cooldown;
            this.animatorTrigger = animatorTrigger;
            input = new InputAction(displayName, InputActionType.Button, binding);
        }

        /// <summary>Resolve the animator trigger hash once. Call from Awake.</summary>
        public void Initialise() => TriggerId = Animator.StringToHash(animatorTrigger);

        /// <summary>Stamp the cooldown. Called after a cast is accepted.</summary>
        public void MarkUsed() => lastUsedTime = Time.time;
    }
}
