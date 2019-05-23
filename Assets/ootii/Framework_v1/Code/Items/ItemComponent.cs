using UnityEngine;
using com.ootii.Base;

namespace com.ootii.Items
{
    public abstract class ItemComponent : BaseMonoObject, IItemComponent
    {
        /// <summary>
        /// Determines if the component can actually be activated
        /// </summary>
        public bool _IsEnabled = true;
        public bool IsEnabled
        {
            get { return _IsEnabled; }
            set { _IsEnabled = value; }
        }

        /// <summary>
        /// Determines if the component is currently active
        /// </summary>
        public bool _IsActive = true;
        public bool IsActive
        {
            get { return _IsActive; }
            set { _IsActive = value; }
        }

        /// <summary>
        /// ComponentUpdate is called every frame, if the component is enabled.
        /// </summary>
        /// <param name="rItem">Item this component is attached to.</param>
        public virtual void UpdateComponent(IItem rItem)
        {
        }

        /// <summary>
        /// Raised when the component is equipped
        /// </summary>
        /// <param name="rItem">Item this component is attached to.</param>
        public virtual void OnEquipped(IItem rItem)
        {
        }

        /// <summary>
        /// Raised when the component is stored
        /// </summary>
        /// <param name="rItem">Item this component is attached to.</param>
        public virtual void OnStored(IItem rItem)
        {
        }
    }
}
