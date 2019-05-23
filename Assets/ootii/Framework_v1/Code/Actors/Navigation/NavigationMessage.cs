using UnityEngine;
using com.ootii.Collections;
using com.ootii.Messages;

namespace com.ootii.Actors.Navigation
{
    /// <summary>
    /// Message
    /// </summary>
    public class NavigationMessage : Message
    {
        /// <summary>
        /// Message type to send to the MC
        /// </summary>
        public static int MSG_UNKNOWN = 0;
        public static int MSG_NAVIGATE_ARRIVED = 1;
        public static int MSG_NAVIGATE_SLOW_ENTERED = 2;
        public static int MSG_NAVIGATE_WALK = 5;
        public static int MSG_NAVIGATE_JUMP = 10;
        public static int MSG_NAVIGATE_CLIMB = 15;
        public static int MSG_NAVIGATE_PUSHED_BACK = 20;
        public static int MSG_NAVIGATE_KNOCKED_DOWN = 25;

        /// <summary>
        /// Owner that this message is about
        /// </summary>
        public GameObject Owner = null;

        /// <summary>
        /// Clear this instance.
        /// </summary>
        public override void Clear()
        {
            Owner = null;

            base.Clear();
        }

        /// <summary>
        /// Release this instance.
        /// </summary>
        public new virtual void Release()
        {
            // We should never release an instance unless we're
            // sure we're done with it. So clearing here is fine
            Clear();

            // Reset the sent flags. We do this so messages are flagged as 'completed'
            // and removed by default.
            IsSent = true;
            IsHandled = true;

            // Make it available to others.
            if (this is NavigationMessage)
            {
                sPool.Release(this);
            }
        }

        // ******************************** OBJECT POOL ********************************

        /// <summary>
        /// Allows us to reuse objects without having to reallocate them over and over
        /// </summary>
        private static ObjectPool<NavigationMessage> sPool = new ObjectPool<NavigationMessage>(40, 10);

        /// <summary>
        /// Pulls an object from the pool.
        /// </summary>
        /// <returns></returns>
        public new static NavigationMessage Allocate()
        {
            // Grab the next available object
            NavigationMessage lInstance = sPool.Allocate();

            // Reset the sent flags. We do this so messages are flagged as 'completed'
            // by default.
            lInstance.IsSent = false;
            lInstance.IsHandled = false;

            // For this type, guarentee we have something
            // to hand back tot he caller
            if (lInstance == null) { lInstance = new NavigationMessage(); }
            return lInstance;
        }

        /// <summary>
        /// Returns an element back to the pool.
        /// </summary>
        /// <param name="rEdge"></param>
        public static void Release(NavigationMessage rInstance)
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
            if (rInstance is NavigationMessage)
            {
                sPool.Release((NavigationMessage)rInstance);
            }
        }
    }
}
