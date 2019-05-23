using System;
using System.Reflection;
using UnityEngine;

namespace com.ootii.Geometry
{
    /// <summary>
    /// When a parent has child objects and those children have colliders,
    /// the parent DOES NOT get the collision or trigger events called without
    /// a rigidbody. I don't like that. So, this proxy object can be used
    /// to pass the information back to the parent object
    /// </summary>
    public class ColliderProxy : MonoBehaviour
    {
        /// <summary>
        /// Object whose collision functions we want to call
        /// </summary>
        public GameObject _Target = null;
        public GameObject Target
        {
            get { return _Target; }

            set
            {
                _Target = value;
                BindTarget(_Target);
            }
        }

        // Component that we will be calling functions on
        protected Component mTarget = null;

        // Functions we'll call
        protected MethodInfo mOnTriggerEnter = null;
        protected MethodInfo mOnTriggerStay = null;
        protected MethodInfo mOnTriggerExit = null;

        /// <summary>
        /// Run before any Start or Update functions are run
        /// </summary>
        protected virtual void Awake()
        {
            if (_Target != null)
            {
                BindTarget(_Target);
            }
        }

        /// <summary>
        /// Allows us to reset the proxy
        /// </summary>
        public virtual void Reset()
        {
        }

        /// <summary>
        /// Enables/disables the colliders based on the speed
        /// </summary>
        public virtual void EnableColliders(bool rEnable, float rSpeed = 0f)
        {
        }

        /// <summary>
        /// Extracts the targets methods so we can bind it to this proxy
        /// </summary>
        /// <param name="rTarget"></param>
        protected void BindTarget(GameObject rTarget)
        {
            mOnTriggerEnter = null;
            mOnTriggerStay = null;
            mOnTriggerExit = null;

            if (rTarget != null)
            {
                BindingFlags lBindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

                // Check each of the components
                Component[] lComponents = rTarget.GetComponents<Component>();
                for (int i = 0; i < lComponents.Length; i++)
                {
                    Type lType = lComponents[i].GetType();
                    if (lType == typeof(Transform)) { continue; }
                    if (lType == typeof(Rigidbody)) { continue; }
                    if (lType == typeof(Collider)) { continue; }
                    if (lType == typeof(AudioSource)) { continue; }

#if !UNITY_EDITOR && (NETFX_CORE || WINDOWS_UWP || UNITY_WP8 || UNITY_WP_8_1 || UNITY_WSA || UNITY_WSA_8_0 || UNITY_WSA_8_1 || UNITY_WSA_10_0)
                    mOnTriggerEnter = lType.GetMethod("OnTriggerEnter", lBindingFlags);
                    if (mOnTriggerEnter != null)
                    {
                        mTarget = lComponents[i];

                        mOnTriggerStay = lType.GetMethod("OnTriggerStay", lBindingFlags);

                        mOnTriggerExit = lType.GetMethod("OnTriggerExit", lBindingFlags);
                    }
#else
                    mOnTriggerEnter = lType.GetMethod("OnTriggerEnter", lBindingFlags, null, new Type[] { typeof(Collider) }, null);
                    if (mOnTriggerEnter != null)
                    {
                        mTarget = lComponents[i];

                        mOnTriggerStay = lType.GetMethod("OnTriggerStay", lBindingFlags, null, new Type[] { typeof(Collider) }, null);

                        mOnTriggerExit = lType.GetMethod("OnTriggerExit", lBindingFlags, null, new Type[] { typeof(Collider) }, null);
                    }
#endif

                    if (mTarget != null) { break; }
                }
            }
        }

        /// <summary>
        /// Capture Unity's collision event. We use triggers since IsKinematic Rigidbodies don't
        /// raise collisions... only triggers.
        /// </summary>
        protected virtual void OnTriggerEnter(Collider rCollider)
        {
            //Debug.Log("[" + Time.time.ToString("f3") + "] ColliderProxy.OnTriggerEnter col:" + rCollider.GetInstanceID() + " this:" + this.GetInstanceID());
            if (mOnTriggerEnter != null) { mOnTriggerEnter.Invoke(mTarget, new object[] { rCollider }); }
        }

        /// <summary>
        /// Capture Unity's collision event
        /// </summary>
        protected virtual void OnTriggerStay(Collider rCollider)
        {
            //Debug.Log("[" + Time.time.ToString("f3") + "] ColliderProxy.OnTriggerStay col:" + rCollider.GetInstanceID() + " this:" + this.GetInstanceID());
            if (mOnTriggerStay != null) { mOnTriggerStay.Invoke(mTarget, new object[] { rCollider }); }
        }

        /// <summary>
        /// Capture Unity's collision event
        /// </summary>
        protected virtual void OnTriggerExit(Collider rCollider)
        {
            //Debug.Log("[" + Time.time.ToString("f3") + "] ColliderProxy.OnTriggerExit col:" + rCollider.GetInstanceID() + " this:" + this.GetInstanceID());
            if (mOnTriggerExit != null) { mOnTriggerExit.Invoke(mTarget, new object[] { rCollider }); }
        }
    }
}
