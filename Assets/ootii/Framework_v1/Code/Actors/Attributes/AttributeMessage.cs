using com.ootii.Collections;
using com.ootii.Messages;

namespace com.ootii.Actors.Attributes
{
    /// <summary>
    /// Basic message for modifying an attribute
    /// </summary>
    public class AttributeMessage : Message
    {
        /// <summary>
        /// Message type to send to the MC
        /// </summary>
        public static int MSG_VALUE_CHANGED = 5200;

        /// <summary>
        /// Attribute that is being messaged
        /// </summary>
        public BasicAttribute Attribute = null;

        /// <summary>
        /// Value (or old value) of the attribute
        /// </summary>
        public object Value;

        /// <summary>
        /// Clear this instance.
        /// </summary>
        public override void Clear()
        {
            Attribute = null;
            Value = null;

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
            if (this is AttributeMessage)
            {
                sPool.Release(this);
            }
        }

        // ******************************** OBJECT POOL ********************************

        /// <summary>
        /// Allows us to reuse objects without having to reallocate them over and over
        /// </summary>
        private static ObjectPool<AttributeMessage> sPool = new ObjectPool<AttributeMessage>(10, 10);

        /// <summary>
        /// Pulls an object from the pool.
        /// </summary>
        /// <returns></returns>
        public new static AttributeMessage Allocate()
        {
            // Grab the next available object
            AttributeMessage lInstance = sPool.Allocate();
            if (lInstance == null) { lInstance = new AttributeMessage(); }

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
        public static AttributeMessage Allocate(AttributeMessage rSource)
        {
            // Grab the next available object
            AttributeMessage lInstance = sPool.Allocate();
            if (lInstance == null) { lInstance = new AttributeMessage(); }

            lInstance.Attribute = rSource.Attribute;
            lInstance.Value = rSource.Value;

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
        public static void Release(AttributeMessage rInstance)
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
            if (rInstance is AttributeMessage)
            {
                sPool.Release((AttributeMessage)rInstance);
            }
        }
    }
}
