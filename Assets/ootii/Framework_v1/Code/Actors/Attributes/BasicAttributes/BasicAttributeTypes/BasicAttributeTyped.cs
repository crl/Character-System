using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.ootii.Actors.Attributes
{
    /// <summary>
    /// Basic class for all attributes.
    /// 
    /// This one inhertis from BasicAttribute and is typed. This allows
    /// us to used typed memory items for faster data access.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [Serializable]
    public abstract class BasicAttributeTyped<T> : BasicAttribute
    {
        /// <summary>
        ///  Defines the type that this attribute represents
        /// </summary>
        protected Type mValueType = null;
        public override Type ValueType
        {
            get { return mValueType; }
        }

        /// <summary>
        /// Value of the attribute. This is faster than the functions below,
        /// but not usable when referencing the base class.
        /// </summary>
        public T _Value;
        public virtual T Value
        {
            get { return _Value; }

            set
            {
                T lOldValue = _Value;

                _Value = value;

                if (Application.isPlaying && mAttributes != null)
                {
                    if (!EqualityComparer<T>.Default.Equals(lOldValue, _Value))
                    {
                        // Inform the event handlers
                        if (mAttributes.OnAttributeValueChangedEvent != null)
                        {
                            mAttributes.OnAttributeValueChangedEvent(this, lOldValue);
                        }

                        // Send the message
                        AttributeMessage lMessage = AttributeMessage.Allocate();
                        lMessage.ID = AttributeMessage.MSG_VALUE_CHANGED;
                        lMessage.Attribute = this;
                        lMessage.Value = lOldValue;

                        if (mAttributes.AttributeValueChangedEvent != null)
                        {
                            mAttributes.AttributeValueChangedEvent.Invoke(lMessage);
                        }

#if USE_MESSAGE_DISPATCHER || OOTII_MD
                        com.ootii.Messages.MessageDispatcher.SendMessage(lMessage);
#endif

                        AttributeMessage.Release(lMessage);
                    }
                }
            }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public BasicAttributeTyped()
        {
            mValueType = typeof(T);
        }

        /// <summary>
        /// Retrieves the value of the attribute. Since this is the base
        /// class, the value type must be specified and must match the actual type
        /// </summary>
        /// <typeparam name="T1">Type of the attribute</typeparam>
        /// <returns>Value of the attribute</returns>
        public override T1 GetValue<T1>()
        {
            if (typeof(T1) == mValueType)
            {
                return (T1)(object)Value;
            }
            else
            {
                throw new Exception("BasicAttributeType.GetValue() - Requested type does not match attribute type.");
            }
        }

        /// <summary>
        /// Sets the value of the attribute. Since this is the base
        /// class, the value type must be specified and must match the actual type
        /// </summary>
        /// <typeparam name="T1">Type of the attribute</typeparam>
        /// <param name="rValue">Value to set</param>
        public override void SetValue<T1>(T1 rValue)
        {
            if (typeof(T1) == mValueType)
            {
                Value = (T)(object)rValue;
            }
            else
            {
                throw new Exception("BasicAttributeTyped.SetValue() - Requested type does not match attribute type.");
            }
        }
    }
}
