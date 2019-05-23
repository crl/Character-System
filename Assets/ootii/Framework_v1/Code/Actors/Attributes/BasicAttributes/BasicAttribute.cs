using System;
using UnityEngine;
using com.ootii.Helpers;

namespace com.ootii.Actors.Attributes
{
    /// <summary>
    /// Very basic inventory item
    /// </summary>
    [Serializable]
    public abstract class BasicAttribute : IAttribute
    {
        /// <summary>
        /// Unique identifier. This should not be changed once added to the Basic Attributes
        /// </summary>
        public string _ID = "";
        public string ID
        {
            get { return _ID; }
        }

        /// <summary>
        ///  Defines the type that this attribute item represents
        /// </summary>
        public virtual Type ValueType
        {
            get { return null; }
        }

        /// <summary>
        /// Determines if the attribute is valid. If not, it may have been
        /// destroyed and we shouldn't hold a reference to it.
        /// </summary>
        protected bool mIsValid = true;
        public bool IsValid
        {
            get { return mIsValid; }
            set { mIsValid = value; }
        }

        /// <summary>
        /// List that manages this specific attribute
        /// </summary>
        [NonSerialized]
        protected BasicAttributes mAttributes = null;
        public BasicAttributes Attributes
        {
            get { return mAttributes; }
            set { mAttributes = value; }
        }

        /// <summary>
        /// Retrieves the value of the attribute item. Since this is the base
        /// class, the value type must be specified and must match the actual type
        /// </summary>
        /// <typeparam name="T1">Type of the attribute item</typeparam>
        /// <returns>Value of the attribute item</returns>
        public virtual T1 GetValue<T1>()
        {
            return default(T1);
        }

        /// <summary>
        /// Sets the value of the attribute item. Since this is the base
        /// class, the value type must be specified and must match the actual type
        /// </summary>
        /// <typeparam name="T1">Type of the attribute item</typeparam>
        /// <param name="rValue">Value to set</param>
        public virtual void SetValue<T1>(T1 rValue)
        {
        }

        // **************************************************************************************************
        // Following properties and function only valid while editing
        // **************************************************************************************************

#if UNITY_EDITOR

        /// <summary>
        /// Allows each attribute item to render thier own GUI
        /// </summary>
        /// <param name="rTarget"></param>
        /// <returns></returns>
        public virtual bool OnInspectorGUI(Rect rRect)
        {
            return false;
        }

        /// <summary>
        /// Allows each attribute item to render thier own GUI
        /// </summary>
        /// <param name="rTarget"></param>
        /// <returns></returns>
        public virtual bool OnInspectorGUI(BasicAttributes rTarget)
        {
            bool lIsDirty = false;

            if (EditorHelper.TextField("Name", "Name of the attribute. Used to retrieve the value as well.", ID, rTarget))
            {
                if (!rTarget.AttributeExists(EditorHelper.FieldStringValue))
                {
                    lIsDirty = true;
                    rTarget.RenameAttribute(ID, EditorHelper.FieldStringValue);
                }
            }

            return lIsDirty;
        }

#endif

    }
}
