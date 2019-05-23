using System;
using UnityEngine;

namespace com.ootii.Actors.Inventory
{
    /// <summary>
    /// Very basic inventory item
    /// </summary>
    [Serializable]
    public class BasicInventoryItem
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string ID = "";

        /// <summary>
        /// Number of items that exist
        /// </summary>
        public int Quantity = 1;

        /// <summary>
        /// Allows us to categorizes item types
        /// </summary>
        public string _ItemType = "";
        public string ItemType
        {
            get { return _ItemType; }
            set { _ItemType = value; }
        }

        /// <summary>
        /// Resource path to the item
        /// </summary>
        public string ResourcePath = "";

        /// <summary>
        /// Motion to use to equip the item
        /// </summary>
        public string EquipMotion = "";

        /// <summary>
        /// Style to use with the equip motion
        /// </summary>
        public int EquipStyle = 0;

        /// <summary>
        /// Motion to use to unequip the item
        /// </summary>
        public string StoreMotion = "";

        /// <summary>
        /// Style to use with the store motion
        /// </summary>
        public int StoreStyle = 0;

        /// <summary>
        /// Determines if we destroy the item when we store it
        /// </summary>
        public bool DestroyOnStore = false;

        /// <summary>
        /// Scene object representing the item. This is useful if the item
        /// already exists as a child of the character and doesn't need to be re-created.
        /// For example, a sheathed sword or bow on the back.
        /// </summary>
        public GameObject Instance = null;

        /// <summary>
        /// Local position when attached to the character's body
        /// </summary>
        public Vector3 LocalPosition = Vector3.zero;

        /// <summary>
        /// Local rotation when attached to the character's body
        /// </summary>
        public Quaternion LocalRotation = Quaternion.identity;

        /// <summary>
        /// Store the euler version of the rotation so that the quaternion doesn't change the value
        /// </summary>
        public Vector3 _LocalRotationEuler = Vector3.zero;
        public virtual Vector3 LocalRotationEuler
        {
            get { return _LocalRotationEuler; }

            set
            {
                _LocalRotationEuler = value;
                LocalRotation = Quaternion.Euler(_LocalRotationEuler);
            }
        }

        /// <summary>
        /// When stored on the character, the bone transform that is the parent
        /// </summary>
        [NonSerialized]
        public Transform StoredParent = null;

        /// <summary>
        /// When stored on the character, the relative position
        /// </summary>
        [NonSerialized]
        public Vector3 StoredPosition = Vector3.zero;

        /// <summary>
        /// When stored on the character, the relative rotation
        /// </summary>
        [NonSerialized]
        public Quaternion StoredRotation = Quaternion.identity;
    }
}
