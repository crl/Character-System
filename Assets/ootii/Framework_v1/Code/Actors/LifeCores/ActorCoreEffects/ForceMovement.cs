using UnityEngine;
using com.ootii.Collections;
using com.ootii.Helpers;

#if !(UNITY_4 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4)
using UnityEngine.AI;
#endif

namespace com.ootii.Actors.LifeCores
{
    /// <summary>
    /// Effect that changes a character's movement
    /// </summary>
    public class ForceMovement : ActorCoreEffect
    {
        /// <summary>
        /// Defines how much we'll move
        /// </summary>
        public Vector3 _Movement = Vector3.zero;
        public Vector3 Movement
        {
            get { return _Movement; }
            set { _Movement = value; }
        }

        /// <summary>
        /// Determines if we reduce the movement based on the elapsed time
        /// </summary>
        public bool _ReduceMovementOverTime = true;
        public bool ReduceMovementOverTime
        {
            get { return _ReduceMovementOverTime; }
            set { _ReduceMovementOverTime = value; }
        }

        // Actor controller we want to tap into
        protected ActorController mActorController = null;

        // Stores values so we can return them
        protected bool mStoredUseTransformPosition = false;
        protected bool mStoredUseTransformRotation = false;

        /// <summary>
        /// Default constructor
        /// </summary>
        public ForceMovement() : base()
        {
        }

        /// <summary>
        /// ActorCore constructor
        /// </summary>
        public ForceMovement(ActorCore rActorCore) : base(rActorCore)
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
            // Check if we can modify the movement each frame
            mActorController = mActorCore.gameObject.GetComponent<ActorController>();
            if (mActorController != null)
            {
                 mStoredUseTransformPosition = mActorController.UseTransformPosition;
                 mActorController.UseTransformPosition = true;

                 mStoredUseTransformRotation = mActorController.UseTransformRotation;
                 mActorController.UseTransformRotation = true;
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
                mActorController.UseTransformPosition = mStoredUseTransformPosition;
                mActorController.UseTransformRotation = mStoredUseTransformRotation;
                mActorController = null;
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

            bool lIsValid = base.Update();
            if (!lIsValid) { return false; }

            Vector3 lMovement = Movement;
            if (ReduceMovementOverTime)
            {
                float lPercentLeft = 1f - (mAge / MaxAge);
                lMovement = lMovement * lPercentLeft;
            }

            mActorController._Transform.position = mActorController._Transform.position + (lMovement * Time.deltaTime);

            return true;
        }

        /// <summary>
        /// Releases the effect as an allocation
        /// </summary>
        public override void Release()
        {
            ForceMovement.Release(this);
        }

        #region Editor Functions

#if UNITY_EDITOR

        /// <summary>
        /// Called when the inspector needs to draw
        /// </summary>
        public override bool OnInspectorGUI(UnityEngine.Object rTarget)
        {
            bool lIsDirty = base.OnInspectorGUI(rTarget);

            if (EditorHelper.Vector3Field("Movement", "Movement that is the direction and speed (per second) that the character should move.", Movement, rTarget))
            {
                lIsDirty = true;
                Movement = EditorHelper.FieldVector3Value;
            }

            if (EditorHelper.FloatField("Max Age", "Time (in seconds) that the movement should continue for.", MaxAge, rTarget))
            {
                lIsDirty = true;
                MaxAge = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.BoolField("Reduce Over Time", "Determines if we reduce movement over time", ReduceMovementOverTime, rTarget))
            {
                lIsDirty = true;
                ReduceMovementOverTime = EditorHelper.FieldBoolValue;
            }

            return lIsDirty;
        }

#endif

        #endregion

        // ******************************** OBJECT POOL ********************************

        /// <summary>
        /// Allows us to reuse objects without having to reallocate them over and over
        /// </summary>
        private static ObjectPool<ForceMovement> sPool = new ObjectPool<ForceMovement>(10, 10);

        /// <summary>
        /// Pulls an object from the pool.
        /// </summary>
        /// <returns></returns>
        public static ForceMovement Allocate()
        {
            ForceMovement lInstance = sPool.Allocate();
            return lInstance;
        }

        /// <summary>
        /// Returns an element back to the pool.
        /// </summary>
        /// <param name="rEdge"></param>
        public static void Release(ForceMovement rInstance)
        {
            if (rInstance == null) { return; }

            rInstance.Clear();
            sPool.Release(rInstance);
        }
    }
}
