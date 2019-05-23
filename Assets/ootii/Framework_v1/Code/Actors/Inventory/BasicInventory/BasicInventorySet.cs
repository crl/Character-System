using System;
using System.Collections.Generic;

namespace com.ootii.Actors.Inventory
{
    /// <summary>
    /// An inventory set is a collection of items that go together. For each
    /// item, there is a slot that the item could belong to.
    /// </summary>
    [Serializable]
    public class BasicInventorySet
    {
        /// <summary>
        /// ID of the inventory set that should be unique for the inventory
        /// </summary>
        public string _ID = "";
        public string ID
        {
            get { return _ID; }
            set { _ID = value; }
        }

        /// <summary>
        /// Determines if the weapon set is currently active
        /// </summary>
        public bool _IsEnabled = true;
        public bool IsEnabled
        {
            get { return _IsEnabled; }
            set { _IsEnabled = value; }
        }

        /// <summary>
        /// Stance that we'll force the actor to once the weapon set is selected
        /// </summary>
        public int _Stance = -1;
        public int Stance
        {
            get { return _Stance; }
            set { _Stance = value; }
        }

        /// <summary>
        /// Movement style that we'll force the actor to once the weapon set is selected
        /// </summary>
        public int _DefaultForm = -1;
        public int DefaultForm
        {
            get { return _DefaultForm; }
            set { _DefaultForm = value; }
        }

        /// <summary>
        /// List of item descriptors the set contains
        /// </summary>
        public List<BasicInventorySetItem> Items = new List<BasicInventorySetItem>();
    }
}
