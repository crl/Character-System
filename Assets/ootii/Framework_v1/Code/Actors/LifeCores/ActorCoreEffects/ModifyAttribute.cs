using UnityEngine;
using com.ootii.Actors.Attributes;
using com.ootii.Collections;
using com.ootii.Helpers;

namespace com.ootii.Actors.LifeCores
{
    /// <summary>
    /// Effect that changes an attribute over time
    /// </summary>
    public class ModifyAttribute : ActorCoreEffect
    {
        /// <summary>
        /// Determines if we reset the attribute value when the effect expires
        /// </summary>
        public bool _ResetOnDeactivate = false;
        public bool ResetOnDeactivate
        {
            get { return _ResetOnDeactivate; }
            set { _ResetOnDeactivate = value; }
        }

        // Message that contains information about the attribute change
        protected AttributeMessageOld mMessage;

        // Amount of change the modification has done
        protected float mChange = 0f;

        /// <summary>
        /// Default constructor
        /// </summary>
        public ModifyAttribute() : base()
        {
        }

        /// <summary>
        /// ActorCore constructor
        /// </summary>
        public ModifyAttribute(ActorCore rActorCore) : base(rActorCore)
        {
            mActorCore = rActorCore;
        }

        /// <summary>
        /// Sets the message that will be run each time damage should be processed
        /// </summary>
        /// <param name="rMessage">Message containing information about the damage</param>
        /// <param name="rTriggerDelay">Time in seconds between triggering</param>
        /// <param name="rMaxAge">Max amount of time the effect can last</param>
        public void Activate(float rTriggerDelay, float rMaxAge, AttributeMessageOld rMessage)
        {
            mChange = 0f;
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
                if (ResetOnDeactivate)
                {
                    float lCurrentValue = mActorCore.AttributeSource.GetAttributeValue<float>(mMessage.AttributeID);
                    mActorCore.AttributeSource.SetAttributeValue(mMessage.AttributeID, lCurrentValue - mChange);
                }

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

            if (mActorCore != null && mActorCore.AttributeSource != null)
            {
                bool lAttributeExists = false;

                try
                {
                    lAttributeExists = mActorCore.AttributeSource.AttributeExists(mMessage.AttributeID);
                }
                catch
                {
                    lAttributeExists = false;
                }

                if (lAttributeExists)
                {

                    float lCurrentValue = mActorCore.AttributeSource.GetAttributeValue<float>(mMessage.AttributeID);
                    float lNewValue = lCurrentValue + mMessage.Value;

                    if (mMessage.MinAttributeID.Length > 0)
                    {
                        float lDefault = 0f;

                        try
                        {
                            lDefault = float.Parse(mMessage.MinAttributeID);
                        }
                        catch { }

                        float lMinValue = mActorCore.AttributeSource.GetAttributeValue<float>(mMessage.MinAttributeID, lDefault);
                        lNewValue = Mathf.Max(lNewValue, lMinValue);
                    }

                    if (mMessage.MaxAttributeID.Length > 0)
                    {
                        float lDefault = lNewValue;

                        try
                        {
                            lDefault = float.Parse(mMessage.MaxAttributeID);
                        }
                        catch { }

                        float lMaxValue = mActorCore.AttributeSource.GetAttributeValue<float>(mMessage.MaxAttributeID, lDefault);
                        lNewValue = Mathf.Min(lNewValue, lMaxValue);
                    }

                    mChange = mChange + (lNewValue - lCurrentValue);

                    mActorCore.AttributeSource.SetAttributeValue(mMessage.AttributeID, lNewValue);
                }
            }
        }

        /// <summary>
        /// Releases the effect as an allocation
        /// </summary>
        public override void Release()
        {
            ModifyAttribute.Release(this);
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
                if (EditorHelper.TextField("Attribute ID", "Attribute being modified.", mMessage.AttributeID, rTarget))
                {
                    lIsDirty = true;
                    mMessage.AttributeID = EditorHelper.FieldStringValue;
                }

                if (EditorHelper.FloatField("Value", "Value to modify the attribute by.", mMessage.Value, rTarget))
                {
                    lIsDirty = true;
                    mMessage.Value = EditorHelper.FieldFloatValue;
                }

                if (EditorHelper.BoolField("Reset On Deactivate", "Determines if we put back all the changes when the effect deactivates.", ResetOnDeactivate, rTarget))
                {
                    lIsDirty = true;
                    ResetOnDeactivate = EditorHelper.FieldBoolValue;
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
        private static ObjectPool<ModifyAttribute> sPool = new ObjectPool<ModifyAttribute>(10, 10);

        /// <summary>
        /// Pulls an object from the pool.
        /// </summary>
        /// <returns></returns>
        public static ModifyAttribute Allocate()
        {
            ModifyAttribute lInstance = sPool.Allocate();
            return lInstance;
        }

        /// <summary>
        /// Returns an element back to the pool.
        /// </summary>
        /// <param name="rEdge"></param>
        public static void Release(ModifyAttribute rInstance)
        {
            if (rInstance == null) { return; }

            rInstance.Clear();
            sPool.Release(rInstance);
        }
    }
}
