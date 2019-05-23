using com.ootii.Collections;
using com.ootii.Messages;

namespace com.ootii.Actors.Inventory
{
    /// <summary>
    /// Basic message for inventory notifications
    /// </summary>
    public class InventoryMessage : Message
    {
        /// <summary>
        /// Message type to send to the MC
        /// </summary>
        public static int MSG_UNKNOWN = 1500;
        public static int MSG_ITEM_EQUIPPED = 1501;
        public static int MSG_ITEM_STORED = 1502;
        public static int MSG_WEAPON_SET_EQUIPPED = 1503;
        public static int MSG_WEAPON_SET_STORED = 1504;

        /// <summary>
        /// Inventory source that is being used
        /// </summary>
        public IInventorySource InventorySource = null;

        /// <summary>
        /// Item that the message is about
        /// </summary>
        public string ItemID = null;

        /// <summary>
        /// Slot that the message is about
        /// </summary>
        public string SlotID = null;

        /// <summary>
        /// Weapon Set the message is about
        /// </summary>
        public string WeaponSetID = null;

        /// <summary>
        /// Form value that can be used to determine animtions animations
        /// </summary>
        public int Form = 0;

        /// <summary>
        /// Clear this instance.
        /// </summary>
        public override void Clear()
        {
            InventorySource = null;
            ItemID = null;
            SlotID = null;
            WeaponSetID = null;
            Form = 0;

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
            if (this is InventoryMessage)
            {
                sPool.Release(this);
            }
        }

        // ******************************** OBJECT POOL ********************************

        /// <summary>
        /// Allows us to reuse objects without having to reallocate them over and over
        /// </summary>
        private static ObjectPool<InventoryMessage> sPool = new ObjectPool<InventoryMessage>(10, 10);

        /// <summary>
        /// Pulls an object from the pool.
        /// </summary>
        /// <returns></returns>
        public new static InventoryMessage Allocate()
        {
            // Grab the next available object
            InventoryMessage lInstance = sPool.Allocate();
            if (lInstance == null) { lInstance = new InventoryMessage(); }

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
        public static InventoryMessage Allocate(InventoryMessage rSource)
        {
            // Grab the next available object
            InventoryMessage lInstance = sPool.Allocate();
            if (lInstance == null) { lInstance = new InventoryMessage(); }

            lInstance.InventorySource = rSource.InventorySource;
            lInstance.ItemID = rSource.ItemID;
            lInstance.SlotID = rSource.SlotID;
            lInstance.WeaponSetID = rSource.WeaponSetID;
            lInstance.Form = rSource.Form;

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
        public static void Release(InventoryMessage rInstance)
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
            if (rInstance is InventoryMessage)
            {
                sPool.Release((InventoryMessage)rInstance);
            }
        }
    }
}
