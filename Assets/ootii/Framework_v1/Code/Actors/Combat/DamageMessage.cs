using com.ootii.Collections;
using com.ootii.Messages;

namespace com.ootii.Actors.Combat
{
    /// <summary>
    /// Basic message for sending damage
    /// </summary>
    public class DamageMessage : Message
    {
        /// <summary>
        /// Amount of damage that occured
        /// </summary>
        public float Damage = 0f;

        /// <summary>
        /// Type of damage applied
        /// </summary>
        public int DamageType = 0;

        /// <summary>
        /// Style in which damage is applied
        /// </summary>
        public int ImpactType = 0;

        /// <summary>
        /// Determines if we play the damaged animations when damaged
        /// </summary>
        public bool AnimationEnabled = true;

        /// <summary>
        /// Clear this instance.
        /// </summary>
        public override void Clear()
        {
            Damage = 0f;
            DamageType = 0;
            ImpactType = 0;
            AnimationEnabled = true;

            base.Clear();
        }

        /// <summary>
        /// Release this instance.
        /// </summary>
        public override void Release()
        {
            // We should never release an instance unless we're
            // sure we're done with it. So clearing here is fine
            Clear();

            // Reset the sent flags. We do this so messages are flagged as 'completed'
            // and removed by default.
            IsSent = true;
            IsHandled = true;

            // Make it available to others.
            if (this is DamageMessage)
            {
                sPool.Release(this);
            }
        }

        // ******************************** OBJECT POOL ********************************

        /// <summary>
        /// Allows us to reuse objects without having to reallocate them over and over
        /// </summary>
        private static ObjectPool<DamageMessage> sPool = new ObjectPool<DamageMessage>(10, 10);

        /// <summary>
        /// Pulls an object from the pool.
        /// </summary>
        /// <returns></returns>
        public new static DamageMessage Allocate()
        {
            // Grab the next available object
            DamageMessage lInstance = sPool.Allocate();
            if (lInstance == null) { lInstance = new DamageMessage(); }

            // Reset the sent flags. We do this so messages are flagged as 'completed'
            // by default.
            lInstance.IsSent = false;
            lInstance.IsHandled = false;

            // For this type, guarentee we have something
            // to hand back tot he caller
            return lInstance;
        }

        /// <summary>
        /// Pulls an object from the pool.
        /// </summary>
        /// <returns></returns>
        public static DamageMessage Allocate(DamageMessage rSource)
        {
            // Grab the next available object
            DamageMessage lInstance = sPool.Allocate();
            if (lInstance == null) { lInstance = new DamageMessage(); }

            lInstance.Damage = rSource.Damage;
            lInstance.DamageType = rSource.DamageType;
            lInstance.ImpactType = rSource.ImpactType;
            lInstance.AnimationEnabled = rSource.AnimationEnabled;

            // Reset the sent flags. We do this so messages are flagged as 'completed'
            // by default.
            lInstance.IsSent = false;
            lInstance.IsHandled = false;

            // For this type, guarentee we have something
            // to hand back tot he caller
            return lInstance;
        }

        /// <summary>
        /// Returns an element back to the pool.
        /// </summary>
        /// <param name="rEdge"></param>
        public static void Release(DamageMessage rInstance)
        {
            if (rInstance == null) { return; }

            // We should never release an instance unless we're
            // sure we're done with it. So clearing here is fine
            rInstance.Clear();

            // Reset the sent flags. We do this so messages are flagged as 'completed'
            // and removed by default.
            rInstance.IsSent = true;
            rInstance.IsHandled = true;

            // Make it available to others.
            sPool.Release(rInstance);
        }

        /// <summary>
        /// Returns an element back to the pool.
        /// </summary>
        /// <param name="rEdge"></param>
        public new static void Release(IMessage rInstance)
        {
            if (rInstance == null) { return; }

            // We should never release an instance unless we're
            // sure we're done with it. So clearing here is fine
            rInstance.Clear();

            // Reset the sent flags. We do this so messages are flagged as 'completed'
            // and removed by default.
            rInstance.IsSent = true;
            rInstance.IsHandled = true;

            // Make it available to others.
            if (rInstance is DamageMessage)
            {
                sPool.Release((DamageMessage)rInstance);
            }
        }
    }
}
