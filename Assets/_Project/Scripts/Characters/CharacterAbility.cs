using FarmFuryStampede.Data;
using FarmFuryStampede.Movement;
using UnityEngine;

namespace FarmFuryStampede.Characters
{
    /// <summary>
    /// Plug-in ability slot for <see cref="CharacterController2D"/>. The controller owns input, the shared
    /// use-count-per-level limit, and the base movement; an ability only says whether it can fire, what
    /// firing does, and how it bends the controller's motion while it is active (through the controller's
    /// public modifier API: Velocity, GravityScale, InputLocked, ContactAttacking, ...).
    /// </summary>
    public abstract class CharacterAbility
    {
        public abstract AbilityType Type { get; }

        /// <summary>True while an ongoing effect (a dash, a glide) is in progress.</summary>
        public virtual bool IsActive => false;

        /// <summary>Whether pressing the ability button right now does anything. A false result costs no use.</summary>
        public virtual bool CanActivate(CharacterController2D owner) => true;

        /// <summary>Performs the ability. Called only after CanActivate; the controller spends the use.</summary>
        public abstract void Activate(CharacterController2D owner);

        /// <summary>Called every fixed step before the controller integrates motion, while the ability is active or not.</summary>
        public virtual void Tick(CharacterController2D owner, float dt)
        {
        }

        /// <summary>Called when the owner touches down after being airborne.</summary>
        public virtual void OnLanded(CharacterController2D owner)
        {
        }

        /// <summary>Clears any in-progress effect (level start, respawn, character change). Uses are not refunded.</summary>
        public virtual void Reset(CharacterController2D owner)
        {
        }

        /// <summary>Placeholder-art feedback: colour multiplier applied to the character sprite.</summary>
        public virtual Color Tint => Color.white;

        /// <summary>Placeholder-art feedback: scale applied to the character sprite.</summary>
        public virtual Vector2 VisualScale => Vector2.one;
    }
}
