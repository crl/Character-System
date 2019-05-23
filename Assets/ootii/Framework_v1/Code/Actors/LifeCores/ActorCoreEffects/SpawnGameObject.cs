using System;
using UnityEngine;
using com.ootii.Actors.Combat;
using com.ootii.Collections;
using com.ootii.Geometry;
using com.ootii.Helpers;

namespace com.ootii.Actors.LifeCores
{
    /// <summary>
    /// Instantiates a GameObject and attaches it to the actor
    /// </summary>
    public class SpawnGameObject : ActorCoreEffect
    {
        /// <summary>
        /// Determines if we make the GameObject a child of the target
        /// </summary>
        public bool _ParentToTarget = true;
        public bool ParentToTarget
        {
            get { return _ParentToTarget; }
            set { _ParentToTarget = value; }
        }

        /// <summary>
        /// Parent object we'll tie the spawned object to. Typically this is the
        /// character casting the spell.
        /// </summary>
        protected Transform mTarget = null;
        public Transform Target
        {
            get { return mTarget; }
            set { mTarget = null; }
        }

        /// <summary>
        /// Bone index on the target we'll tie the spawned object to. A value of
        /// 0 means there is no target.
        /// </summary>
        public int _BoneIndex = 0;
        public int BoneIndex
        {
            get { return _BoneIndex; }
            set { _BoneIndex = value; }
        }

        /// <summary>
        /// Name of the bone we're tying the spawned object to.
        /// </summary>
        public string _BoneName = "";
        public string BoneName
        {
            get { return _BoneName; }
            set { _BoneName = value; }
        }

        /// <summary>
        /// Local position relative to the parent
        /// </summary>
        public Vector3 _LocalPosition = Vector3.zero;
        public Vector3 LocalPosition
        {
            get { return _LocalPosition; }
            set { _LocalPosition = value; }
        }

        /// <summary>
        /// Prefab transform that represents the effect
        /// </summary>
        public GameObject _Prefab = null;
        public virtual GameObject Prefab
        {
            get { return _Prefab; }
            set { _Prefab = value; }
        }

        /// <summary>
        /// Instance of the prefab that is active
        /// </summary>
        protected GameObject mInstance = null;
        public GameObject Instance
        {
            get { return mInstance; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public SpawnGameObject() : base()
        {
        }

        /// <summary>
        /// ActorCore constructor
        /// </summary>
        public SpawnGameObject(ActorCore rActorCore) : base(rActorCore)
        {
            mActorCore = rActorCore;
        }

        /// <summary>
        /// Sets the message that will be run each time damage should be processed
        /// </summary>
        /// <param name="rMessage">Message containing information about the damage</param>
        /// <param name="rTriggerDelay">Time in seconds between triggering</param>
        /// <param name="rMaxAge">Max amount of time the effect can last</param>
        public virtual void Activate(float rMaxAge, GameObject rPrefab)
        {
            if (_Prefab != null)
            {
                mInstance = GameObject.Instantiate(_Prefab);
                if (mInstance != null)
                {
                    if (ParentToTarget)
                    {
                        if (mTarget == null) { mTarget = mActorCore.Transform; }
                        Transform lParent = (mTarget != null ? mTarget.transform : null);

                        if (_BoneIndex == 1 && _BoneName.Length > 0)
                        {
                            Transform lBone = lParent.FindTransform(_BoneName);
                            if (lBone != null) { lParent = lBone; }
                        }
                        else if (_BoneIndex > 1 && _BoneName.Length > 0)
                        {
                            try
                            {
                                HumanBodyBones lHumanBodyBone = (HumanBodyBones)Enum.Parse(typeof(HumanBodyBones), _BoneName, true);
                                Transform lBone = lParent.FindTransform(lHumanBodyBone);
                                if (lBone != null) { lParent = lBone; }
                            }
                            catch { }
                        }

                        mInstance.transform.parent = lParent;
                    }

                    mInstance.transform.localPosition = _LocalPosition;
                    mInstance.SetActive(true);
                }
            }

            base.Activate(0f, rMaxAge);
        }

        /// <summary>
        /// Called when the effect is meant to be deactivated
        /// </summary>
        public override void Deactivate()
        {
            if (mInstance != null)
            {
                GameObject.Destroy(mInstance);

                mInstance = null;
            }

            base.Deactivate();
        }

        /// <summary>
        /// Raised when the effect should be triggered
        /// </summary>
        public override void TriggerEffect()
        {
            base.TriggerEffect();
        }

        /// <summary>
        /// Releases the effect as an allocation
        /// </summary>
        public override void Release()
        {
            // We clear this prefab because this isn't the prefab for the effect,
            // but the prefab of the GameObject the effect is creating
            _Prefab = null;

            SpawnGameObject.Release(this);
        }

        ///// <summary>
        ///// Utility function used to get the target given some different options
        ///// </summary>
        ///// <param name="rData">Data that typically comes from activation</param>
        ///// <param name="rSpellData">SpellData belonging to the spell</param>
        ///// <returns>Transform that is the expected target or null</returns>
        //public virtual Transform GetBestTarget(object rData)
        //{
        //    Transform lTarget = null;

        //    if (rData != null)
        //    {
        //        if (rData is Collider)
        //        {
        //            lTarget = ((Collider)rData).gameObject.transform;
        //        }
        //        else if (rData is Transform)
        //        {
        //            lTarget = (Transform)rData;
        //        }
        //        else if (rData is GameObject)
        //        {
        //            lTarget = ((GameObject)rData).transform;
        //        }
        //        else if (rData is MonoBehaviour)
        //        {
        //            lTarget = ((MonoBehaviour)rData).gameObject.transform;
        //        }
        //    }

        //    return lTarget;
        //}

        #region Editor Functions

#if UNITY_EDITOR

        /// <summary>
        /// Called when the inspector needs to draw
        /// </summary>
        public override bool OnInspectorGUI(UnityEngine.Object rTarget)
        {
            bool lIsDirty = base.OnInspectorGUI(rTarget);

            return lIsDirty;
        }

#endif

        #endregion

        // ******************************** OBJECT POOL ********************************

        /// <summary>
        /// Allows us to reuse objects without having to reallocate them over and over
        /// </summary>
        private static ObjectPool<SpawnGameObject> sPool = new ObjectPool<SpawnGameObject>(5, 5);

        /// <summary>
        /// Pulls an object from the pool.
        /// </summary>
        /// <returns></returns>
        public static SpawnGameObject Allocate()
        {
            SpawnGameObject lInstance = sPool.Allocate();
            return lInstance;
        }

        /// <summary>
        /// Returns an element back to the pool.
        /// </summary>
        /// <param name="rEdge"></param>
        public static void Release(SpawnGameObject rInstance)
        {
            if (rInstance == null) { return; }

            rInstance.Clear();
            sPool.Release(rInstance);
        }
    }
}
