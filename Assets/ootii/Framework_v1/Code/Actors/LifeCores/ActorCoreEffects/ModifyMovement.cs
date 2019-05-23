using UnityEngine;
using com.ootii.Collections;

#if !(UNITY_4 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4)
using UnityEngine.AI;
#endif

namespace com.ootii.Actors.LifeCores
{
    /// <summary>
    /// Effect that changes a character's movement
    /// </summary>
    public class ModifyMovement : ActorCoreEffect
    {
        /// <summary>
        /// Defines how much we'll modify the movement
        /// </summary>
        public float _MovementFactor = 1f;
        public float MovementFactor
        {
            get { return _MovementFactor; }
            set { _MovementFactor = value; }
        }

        // Stored speed to reset
        protected float mOriginalSpeed = 0f;

        // Actor controller we want to tap into
        protected ActorController mActorController = null;

        // NavMesh driver that we'll change
        protected NavMeshDriver mNavMeshDriver = null;

        // NavMesh agent we will tap into
        protected NavMeshAgent mNavMeshAgent = null;

        /// <summary>
        /// Default constructor
        /// </summary>
        public ModifyMovement() : base()
        {
        }

        /// <summary>
        /// ActorCore constructor
        /// </summary>
        public ModifyMovement(ActorCore rActorCore) : base(rActorCore)
        {
            mActorCore = rActorCore;
        }

        /// <summary>
        /// Sets the message that will be run each time damage should be processed
        /// </summary>
        /// <param name="rMessage">Message containing information about the damage</param>
        /// <param name="rTriggerDelay">Time in seconds between triggering</param>
        /// <param name="rMaxAge">Max amount of time the effect can last</param>
        public override void Activate(float rTriggerDelay, float rMaxAge)
        {
            bool lActivated = false;

            // Check if we can modify the movement each frame
            mActorController = mActorCore.gameObject.GetComponent<ActorController>();
            if (mActorController != null)
            {
                // If we are in control of movement, this is a bit easier
                if (!mActorController.UseTransformPosition)
                {
                    lActivated = true;
                    mActorController.OnPreControllerMove += OnControllerMoved;
                }                    
            }

            // If we couldn't use the Actor Controller, let's see if we can use a nav mesh angent
            if (!lActivated)
            {
                mNavMeshDriver = mActorCore.gameObject.GetComponent<NavMeshDriver>();
                if (mNavMeshDriver != null)
                {
                    lActivated = true;
                    mOriginalSpeed = mNavMeshDriver.MovementSpeed;

                    mNavMeshDriver.MovementSpeed = mNavMeshDriver.MovementSpeed * _MovementFactor;
                }
            }

            // If we couldn't use the Actor Controller, let's see if we can use a nav mesh angent
            if (!lActivated)
            {
                mNavMeshAgent = mActorCore.gameObject.GetComponent<NavMeshAgent>();
                if (mNavMeshAgent != null)
                {
                    lActivated = true;
                    mOriginalSpeed = mNavMeshAgent.speed;

                    mNavMeshAgent.speed = mNavMeshAgent.speed * _MovementFactor;
                }
            }

            base.Activate(rTriggerDelay, rMaxAge);
        }

        /// <summary>
        /// Called when the effect is meant to be deactivated
        /// </summary>
        public override void Deactivate()
        {
            if (mActorController != null)
            {
                mActorController.OnPreControllerMove -= OnControllerMoved;
                mActorController = null;
            }

            if (mNavMeshDriver != null)
            {
                mNavMeshDriver.MovementSpeed = mOriginalSpeed;
                mNavMeshDriver = null;
            }

            if (mNavMeshAgent != null)
            {
                mNavMeshAgent.speed = mOriginalSpeed;
                mNavMeshAgent = null;
            }

            base.Deactivate();
        }

        /// <summary>
        /// Called each frame that the effect is active
        /// </summary>
        /// <returns>Boolean that determines if the effect is still active or not</returns>
        public override bool Update()
        {
            if (mActorController == null) { return false; }

            return true;
        }

        /// <summary>
        /// Releases the effect as an allocation
        /// </summary>
        public override void Release()
        {
            ModifyMovement.Release(this);
        }

        /// <summary>
        /// Gives us an opportunity to modify the AC's movement prior to having it applied
        /// </summary>
        /// <param name="rController"></param>
        /// <param name="rFinalPosition"></param>
        /// <param name="rFinalRotation"></param>
        protected void OnControllerMoved(ICharacterController rController, ref Vector3 rFinalPosition, ref Quaternion rFinalRotation)
        {
            if (_MovementFactor != 1f)
            {
                Vector3 lMovement = rFinalPosition - mActorController._Transform.position;
                rFinalPosition = mActorController._Transform.position + (lMovement.normalized * (lMovement.magnitude * _MovementFactor));
            }
        }

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
        private static ObjectPool<ModifyMovement> sPool = new ObjectPool<ModifyMovement>(10, 10);

        /// <summary>
        /// Pulls an object from the pool.
        /// </summary>
        /// <returns></returns>
        public static ModifyMovement Allocate()
        {
            ModifyMovement lInstance = sPool.Allocate();
            return lInstance;
        }

        /// <summary>
        /// Returns an element back to the pool.
        /// </summary>
        /// <param name="rEdge"></param>
        public static void Release(ModifyMovement rInstance)
        {
            if (rInstance == null) { return; }

            rInstance.Clear();
            sPool.Release(rInstance);
        }
    }
}
