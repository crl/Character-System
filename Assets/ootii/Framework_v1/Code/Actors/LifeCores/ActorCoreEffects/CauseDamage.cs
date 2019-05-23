using com.ootii.Actors.Combat;
using com.ootii.Collections;
using com.ootii.Helpers;
using com.ootii.Messages;

namespace com.ootii.Actors.LifeCores
{
    /// <summary>
    /// Effect that causes damage over time
    /// </summary>
    public class CauseDamage : ActorCoreEffect
    {
        /// <summary>
        /// Message that contains information about the damage
        /// </summary>
        protected DamageMessage mMessage;

        /// <summary>
        /// Default constructor
        /// </summary>
        public CauseDamage() : base()
        {
        }

        /// <summary>
        /// ActorCore constructor
        /// </summary>
        public CauseDamage(ActorCore rActorCore) : base(rActorCore)
        {
            mActorCore = rActorCore;
        }

        /// <summary>
        /// Sets the message that will be run each time damage should be processed
        /// </summary>
        /// <param name="rMessage">Message containing information about the damage</param>
        /// <param name="rTriggerDelay">Time in seconds between triggering</param>
        /// <param name="rMaxAge">Max amount of time the effect can last</param>
        public void Activate(float rTriggerDelay, float rMaxAge, DamageMessage rMessage)
        {
            mMessage = rMessage;
            base.Activate(rTriggerDelay, rMaxAge);
        }

        /// <summary>
        /// Called when the effect is meant to be deactivated
        /// </summary>
        public override void Deactivate()
        {
            if (mMessage != null)
            {
                mMessage.Release();
                mMessage = null;
            }

            base.Deactivate();
        }

        /// <summary>
        /// Raised when the effect should be triggered
        /// </summary>
        public override void TriggerEffect()
        {
            base.TriggerEffect();

            if (mActorCore != null)
            {
                int lStoredID = mMessage.ID;
                bool lStoredIsHandled = mMessage.IsHandled;

                mActorCore.SendMessage(mMessage);

#if USE_MESSAGE_DISPATCHER || OOTII_MD
                MessageDispatcher.SendMessage(mMessage);
#endif

                mMessage.ID = lStoredID;
                mMessage.IsHandled = lStoredIsHandled;
            }
        }

        /// <summary>
        /// Releases the effect as an allocation
        /// </summary>
        public override void Release()
        {
            CauseDamage.Release(this);
        }

        #region Editor Functions

#if UNITY_EDITOR

        /// <summary>
        /// Called when the inspector needs to draw
        /// </summary>
        public override bool OnInspectorGUI(UnityEngine.Object rTarget)
        {
            bool lIsDirty = base.OnInspectorGUI(rTarget);

            if (mMessage != null)
            {
                if (EditorHelper.IntField("Damage Type", "Type of damage being applied", mMessage.DamageType, rTarget))
                {
                    lIsDirty = true;
                    mMessage.DamageType = EditorHelper.FieldIntValue;
                }

                if (EditorHelper.FloatField("Damage", "Damage to apply to the actor each trigger", mMessage.Damage, rTarget))
                {
                    lIsDirty = true;
                    mMessage.Damage = EditorHelper.FieldFloatValue;
                }
            }

            return lIsDirty;
        }

#endif

        #endregion

        // ******************************** OBJECT POOL ********************************

        /// <summary>
        /// Allows us to reuse objects without having to reallocate them over and over
        /// </summary>
        private static ObjectPool<CauseDamage> sPool = new ObjectPool<CauseDamage>(10, 10);

        /// <summary>
        /// Pulls an object from the pool.
        /// </summary>
        /// <returns></returns>
        public static CauseDamage Allocate()
        {
            CauseDamage lInstance = sPool.Allocate();
            return lInstance;
        }

        /// <summary>
        /// Returns an element back to the pool.
        /// </summary>
        /// <param name="rEdge"></param>
        public static void Release(CauseDamage rInstance)
        {
            if (rInstance == null) { return; }

            rInstance.Clear();
            sPool.Release(rInstance);
        }
    }
}
