using UnityEngine;
using com.ootii.Geometry;
using com.ootii.Timing;

#if !(UNITY_4 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4)
using UnityEngine.AI;
#endif

#if UNITY_EDITOR
using UnityEditor;
#endif 

namespace com.ootii.Actors
{
    /// <summary>
    /// Has the AC simply follow the nav mesh agent
    /// </summary>
    [AddComponentMenu("ootii/Actor Drivers/Nav Mesh Follower")]
    public class NavMeshFollower : MonoBehaviour
    {
        /// <summary>
        /// Provides an error amount for determining distance
        /// </summary>
        public const float EPSILON = 0.01f;

        /// <summary>
        /// Determines if the driver is enabled
        /// </summary>
        public bool _IsEnabled = true;
        public bool IsEnabled
        {
            get { return _IsEnabled; }

            set
            {
                if (_IsEnabled && !value)
                {
                    if (mIsTargetSet)
                    {
#if UNITY_4 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4 || UNITY_5_5
                        mNavMeshAgent.Stop();
#else
                        mNavMeshAgent.isStopped = true;
#endif
                    }
                }
                else if (!_IsEnabled && value)
                {
                    if (mIsTargetSet)
                    {
                        SetDestination(_TargetPosition);
                    }
                }

                _IsEnabled = value;
            }
        }

        /// <summary>
        /// Target we're moving towards
        /// </summary>
        public Transform _Target = null;
        public Transform Target
        {
            get { return _Target; }

            set
            {
                _Target = value;
                if (_Target == null)
                {
#if UNITY_4 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4 || UNITY_5_5
                    mNavMeshAgent.Stop();
#else
                    mNavMeshAgent.isStopped = true;
#endif
                    mHasArrived = false;

                    mIsTargetSet = false;
                    _TargetPosition = Vector3Ext.Null;
                }
                else
                {
                    mIsTargetSet = true;
                    _TargetPosition = _Target.position;

                    if (Application.isPlaying)
                    {
                        SetDestination(_TargetPosition);
                    }
                }
            }
        }

        /// <summary>
        /// Target we're moving towards
        /// </summary>
        public Vector3 _TargetPosition = Vector3.zero;
        public Vector3 TargetPosition
        {
            get { return _TargetPosition; }

            set
            {
                _Target = null;
                _TargetPosition = value;

                if (_TargetPosition == Vector3Ext.Null)
                {
#if UNITY_4 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4 || UNITY_5_5
                    mNavMeshAgent.Stop();
#else
                    mNavMeshAgent.isStopped = true;
#endif
                    mHasArrived = false;

                    mIsTargetSet = false;
                }
                else
                {
                    mIsTargetSet = true;
                }
            }
        }

        /// <summary>
        /// Determines if we clear the target object and position when the 
        /// actor reaches the target
        /// </summary>
        public bool _ClearTargetOnStop = true;
        public bool ClearTargetOnStop
        {
            get { return _ClearTargetOnStop; }
            set { _ClearTargetOnStop = value; }
        }

        /// <summary>
        /// Determines if a target is currently set
        /// </summary>
        protected bool mIsTargetSet = false;
        public bool IsTargetSet
        {
            get { return mIsTargetSet; }
        }

        /// <summary>
        /// Determines if we've arrived at the final destination
        /// </summary>
        protected bool mHasArrived = false;
        public bool HasArrived
        {
            get { return mHasArrived; }
        }

        /// <summary>
        /// NavMeshAgent we'll use to help manage the AI based
        /// navigation of the actor.
        /// </summary>
        protected NavMeshAgent mNavMeshAgent = null;

        /// <summary>
        /// Destination that is set on the NMA. This will
        /// equal _TargetPosition once it's set on the NMA.
        /// </summary>
        protected Vector3 mAgentDestination = Vector3.zero;

        /// <summary>
        /// Used for initialization before any Start or Updates are called
        /// </summary>
        protected void Awake()
        {
            // Grab the nav mesh agent
            mNavMeshAgent = gameObject.GetComponent<NavMeshAgent>();
        }

        /// <summary>
        /// Allows us to initialize before updates are called
        /// </summary>
        protected virtual void Start()
        {
            // Initialize the target if it exists
            if (_Target != null)
            {
                Target = _Target;
            }
            else if (_TargetPosition.magnitude > 0f)
            {
                TargetPosition = _TargetPosition;
            }
        }

        /// <summary>
        /// Clears all the target properties
        /// </summary>
        public void ClearTarget()
        {
            if (_ClearTargetOnStop)
            {
                _Target = null;
                _TargetPosition = Vector3Ext.Null;
                mIsTargetSet = false;
            }

#if UNITY_4 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4 || UNITY_5_5
            mNavMeshAgent.Stop();
#else
            mNavMeshAgent.isStopped = true;
#endif

            mHasArrived = false;
        }

        /// <summary>
        /// Update is called once per frame
        /// </summary>
        protected void Update()
        {
            if (!_IsEnabled) { return; }
            if (mNavMeshAgent == null) { return; }
            if (!mIsTargetSet) { return; }

            // Simulated input for the animator
            //Vector3 lMovement = Vector3.zero;
            //Quaternion lRotation = Quaternion.identity;

            // Set the destination
            if (_Target != null) { _TargetPosition = _Target.position; }
            SetDestination(_TargetPosition);

            if (!mNavMeshAgent.pathPending &&
                mNavMeshAgent.pathStatus == NavMeshPathStatus.PathComplete &&
                mNavMeshAgent.remainingDistance == 0f)
            {
                ClearTarget();
                mHasArrived = true;

                OnArrived();
            }
        }

        /// <summary>
        /// Sets the new destination that we need to travel to
        /// </summary>
        /// <param name="rDestination"></param>
        protected virtual void SetDestination(Vector3 rDestination)
        {
            if (!mHasArrived && mAgentDestination == rDestination) { return; }

            // Reset the properties
            mHasArrived = false;

            // Set the new destination
            mAgentDestination = rDestination;

            // Recalculate the path
            if (!mNavMeshAgent.pathPending)
            {
                mNavMeshAgent.ResetPath();
                mNavMeshAgent.SetDestination(mAgentDestination);
            }
        }

        /// <summary>
        /// Event function for when we arrive at the destination
        /// </summary>
        protected virtual void OnArrived()
        {
        }

        /// <summary>
        /// Event function for when we are within the slow distance
        /// </summary>
        protected virtual void OnSlowDistanceEntered()
        {
        }
    }
}
