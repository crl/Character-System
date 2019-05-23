using com.ootii.Collections;
using com.ootii.Messages;

namespace com.ootii.Actors.AnimationControllers
{
    /// <summary>
    /// Basic message for sending instructions to motions
    /// </summary>
    public class MotionMessage : Message
    {
        /// <summary>
        /// Message type to send to the MC
        /// </summary>
        public static int MSG_UNKNOWN = 100;
        public static int MSG_MOTION_ACTIVATE = 101;
        public static int MSG_MOTION_CONTINUE = 102;
        public static int MSG_MOTION_DEACTIVATE = 103;
        public static int MSG_MOTION_TEST = 104;

        public static string[] Names = new string[]
        {
            "Unknown",
            "Motion Activate",
            "Motion Continue",
            "Motion Deactivate",
            "Motion Test"
        };

        /// <summary>
        /// Motion the message is referencing
        /// </summary>
        public MotionControllerMotion Motion = null;

        /// <summary>
        /// Form or style to use with the motion
        /// </summary>
        public int Form = -1;

        /// <summary>
        /// Determines if the motion should continue
        /// </summary>
        public bool Continue = false;

        /// <summary>
        /// Clear this instance.
        /// </summary>
        public override void Clear()
        {
            Motion = null;
            Continue = false;

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
            if (this is MotionMessage)
            {
                sPool.Release(this);
            }
        }

        // ******************************** OBJECT POOL ********************************

        /// <summary>
        /// Allows us to reuse objects without having to reallocate them over and over
        /// </summary>
        private static ObjectPool<MotionMessage> sPool = new ObjectPool<MotionMessage>(10, 10);

        /// <summary>
        /// Pulls an object from the pool.
        /// </summary>
        /// <returns></returns>
        public new static MotionMessage Allocate()
        {
            // Grab the next available object
            MotionMessage lInstance = sPool.Allocate();
            if (lInstance == null) { lInstance = new MotionMessage(); }

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
        public static MotionMessage Allocate(MotionMessage rSource)
        {
            // Grab the next available object
            MotionMessage lInstance = sPool.Allocate();
            if (lInstance == null) { lInstance = new MotionMessage(); }

            lInstance.ID = rSource.ID;
            lInstance.Motion = rSource.Motion;
            lInstance.Continue = rSource.Continue;

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
        public static void Release(MotionMessage rInstance)
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
            if (rInstance is MotionMessage)
            {
                sPool.Release((MotionMessage)rInstance);
            }
        }
    }
}
