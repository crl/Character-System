using UnityEngine;
using com.ootii.Base;

namespace com.ootii.Actors.LifeCores
{
    /// <summary>
    /// Determines the capabilities of the item and provides access to
    /// core specific functionality.
    /// </summary>
    public partial class ItemCore : BaseMonoObject, IItemCore
    {
        /// <summary>
        /// Determines if the GameObject is allocated from the pool. If so,
        /// this is the template that allocated it.
        /// </summary>
        protected GameObject mPrefab;
        public GameObject Prefab
        {
            get { return mPrefab; }
            set { mPrefab = value; }
        }

        /// <summary>
        /// Game object that owns the item. This is not the item itself.
        /// </summary>
        protected GameObject mOwner = null;
        public virtual GameObject Owner
        {
            get { return mOwner; }
            set { mOwner = value; }
        }

        /// <summary>
        /// Local position when attached to the character's body
        /// </summary>
        public Vector3 _LocalPosition = Vector3.zero;
        public virtual Vector3 LocalPosition
        {
            get { return _LocalPosition; }
            set { _LocalPosition = value; }
        }

        /// <summary>
        /// Local rotation when attached to the character's body
        /// </summary>
        public Quaternion _LocalRotation = Quaternion.identity;
        public virtual Quaternion LocalRotation
        {
            get { return _LocalRotation; }

            set
            {
                _LocalRotation = value;
                _LocalRotationEuler = _LocalRotation.eulerAngles;
            }
        }

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
                _LocalRotation = Quaternion.Euler(_LocalRotationEuler);
            }
        }

        ///// <summary>
        ///// Max health the item has before being destroyed
        ///// </summary>
        //public float _MaxHealth = 0f;
        //public virtual float MaxHealth
        //{
        //    get { return _MaxHealth; }
        //    set { _MaxHealth = value; }
        //}

        ///// <summary>
        ///// Current health of the item
        ///// </summary>
        //protected float mHealth = 0f;
        //public virtual float Health
        //{
        //    get { return mHealth; }
        //    set { mHealth = value; }
        //}

        ///// <summary>
        ///// Sound to play when the item is equipped
        ///// </summary>
        //public AudioClip _EquipSound = null;
        //public AudioClip EquipSound
        //{
        //    get { return _EquipSound; }
        //    set { _EquipSound = value; }
        //}

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        protected virtual void OnEnable()
        {
            // Temporary to ensure we have a the euler value set
            if (_LocalRotationEuler.sqrMagnitude == 0f)
            {
                _LocalRotationEuler = _LocalRotation.eulerAngles;
            }
            // Force the quaternion to the euler values
            else
            {
                _LocalRotation = Quaternion.Euler(_LocalRotationEuler);
            }
        }

        /// <summary>
        /// Raised when the item is equipped
        /// </summary>
        public virtual void OnEquipped()
        {
        }

        /// <summary>
        /// Rased when the item is stored
        /// </summary>
        public virtual void OnStored()
        {
        }
    }
}
