using System.Collections.Generic;
using UnityEngine;
using com.ootii.Actors.Combat;

namespace com.ootii.Actors.LifeCores
{
    /// <summary>
    /// Foundation for weapons that need basic information
    /// </summary>
    public interface IWeaponCore : IItemCore
    {
        /// <summary>
        /// Determines if the weapon is actively looking for contact
        /// </summary>
        bool IsActive { get; set; }

        /// <summary>
        /// Determines if the weapon uses colliders
        /// </summary>
        bool HasColliders { get; }

        /// <summary>
        /// Minimum range the weapon can apply damage
        /// </summary>
        float MinRange { get; }

        /// <summary>
        /// Maximum range the weapon can apply damage
        /// </summary>
        float MaxRange { get; }
    }
}
