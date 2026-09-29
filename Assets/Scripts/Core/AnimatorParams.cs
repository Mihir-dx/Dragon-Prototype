using UnityEngine;

namespace Dragon.Core
{
    /// <summary>
    /// Cached Animator parameter and state IDs for DragonAnimatorController.
    ///
    /// Two reasons this exists rather than string literals at each call site:
    /// hashes are resolved once at static init instead of on every SetFloat/SetTrigger,
    /// and renaming a state in the controller becomes a single edit here instead of a
    /// hunt through five scripts for a string that fails silently when it goes stale.
    /// </summary>
    public static class AnimatorParams
    {
        // ── Parameters ──
        public static readonly int Speed = Animator.StringToHash("Speed");
        public static readonly int GetHit = Animator.StringToHash("GetHit");
        public static readonly int Die = Animator.StringToHash("Die");

        // ── States on layer 0 ──
        private static readonly int IdleState = Animator.StringToHash("Idle");
        private static readonly int RunState = Animator.StringToHash("Run");

        /// <summary>
        /// True while the dragon is in a locomotion state and may start a new action.
        /// False during attacks, GetHit and Die, which must play out uninterrupted —
        /// this is what stops an ability from cancelling itself or a flinch.
        ///
        /// Compares cached short-name hashes rather than calling AnimatorStateInfo.IsName,
        /// which string-compares against the full state path on every call.
        /// </summary>
        public static bool IsInterruptible(Animator animator)
        {
            if (animator == null) return false;

            int state = animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
            return state == IdleState || state == RunState;
        }
    }
}
