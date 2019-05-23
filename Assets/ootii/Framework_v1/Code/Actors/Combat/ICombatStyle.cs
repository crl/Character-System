using UnityEngine;

namespace com.ootii.Actors.Combat
{
    /// <summary>
    /// An CombatStyle gives us details about a specific combat action. We'll use
    /// these details to determine who can be hit and when.
    /// </summary>
    public interface ICombatStyle
    {
        /// <summary>
        /// Unique ID for the attack style (within the list)
        /// </summary>
        string Name { get; set; }

        /// <summary>
        /// Helps define the animation that is tied to the style
        /// </summary>
        int Form { get; set; }

        /// <summary>
        /// Helps define the animation that is tied to the style
        /// </summary>
        //int ParameterID { get; set; }

        /// <summary>
        /// Helps define the animation that is tied to the style (this should be called Form now)
        /// </summary>
        //int Style { get; set; }

        /// <summary>
        /// Defines the inventory slot ID that holds the weapon that is doing the attack
        /// </summary>
        string InventorySlotID { get; set; }

        /// <summary>
        /// Delay before the attack can be used again
        /// </summary>
        float Delay { get; set; }

        /// <summary>
        /// Determines if the attack is able to be stopped
        /// </summary>
        bool IsInterruptible { get; set; }

        /// <summary>
        /// Flags that determine the effects that the combat style has
        /// </summary>
        int Effects { get; set; }

        /// <summary>
        /// Direction of the attack relative to the character's forward
        /// </summary>
        Vector3 Forward { get; set; }

        /// <summary>
        /// Horizontal field-of-attack centered on the Forward. This determines
        /// the horizontal range of the attack.
        /// </summary>
        float HorizontalFOA { get; set; }

        /// <summary>
        /// Vertical field-of-attack centered on the Forward. This determines
        /// the vertical range of the attack.
        /// </summary>
        float VerticalFOA { get; set; }

        /// <summary>
        /// Minimum range for the attack (0 means use the combatant + weapon)
        /// </summary>
        float MinRange { get; set; }

        /// <summary>
        /// Maximum range for the attack (0 means use the combatant + weapon)
        /// </summary>
        float MaxRange { get; set; }

        /// <summary>
        /// Amount to multiply the damage by
        /// </summary>
        float DamageModifier { get; set; }
    }
}
