using UnityEngine;

namespace com.ootii.Items
{
    /// <summary>
    /// Components represent features or functionality that can be associated with
    /// and Item Cores. These components can be created and used as needed.
    /// </summary>
    public interface IItemComponent : IItem
    {
        /// <summary>
        /// Determines if the component is capable of being active
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Determines if the component is active
        /// </summary>
        bool IsActive { get; set; }

        /// <summary>
        /// ComponentUpdate is called every frame, if the component is enabled.
        /// </summary>
        /// <param name="rItem">Item this component is attached to.</param>
        void UpdateComponent(IItem rItem);

        /// <summary>
        /// Raised when the component is equipped
        /// </summary>
        /// <param name="rItem">Item this component is attached to.</param>
        void OnEquipped(IItem rItem);

        /// <summary>
        /// Raised when the component is stored
        /// </summary>
        /// <param name="rItem">Item this component is attached to.</param>
        void OnStored(IItem rItem);
    }
}
