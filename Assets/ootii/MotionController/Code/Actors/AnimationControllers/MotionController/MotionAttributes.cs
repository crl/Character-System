using System;
using System.Linq;
using com.ootii.Helpers;

namespace com.ootii.Actors.AnimationControllers
{
    /// <summary>
    /// Defines the tooltip value for motion properties
    /// </summary>
    public class MotionNameAttribute : Attribute
    {
        /// <summary>
        /// Default tooltip value
        /// </summary>
        protected string mValue;
        public string Value
        {
            get { return mValue; }
        }

        /// <summary>
        /// Constructor for the attribute
        /// </summary>
        /// <param name="rValue">Value that is the tooltip</param>
        public MotionNameAttribute(string rValue)
        {
            mValue = rValue;
        }

        /// <summary>
        /// Attempts to find a fiendly name. If found, returns it.
        /// </summary>
        /// <param name="rType">Type whose friendly name we want</param>
        /// <returns>String that is the friendly name for class name</returns>
        public static string GetName(Type rType)
        {
            string lTypeName = "";

            MotionNameAttribute lAttribute = ReflectionHelper.GetAttribute<MotionNameAttribute>(rType);
            if (lAttribute != null) { lTypeName = lAttribute.Value; }

            return lTypeName;
        }
    }

    /// <summary>
    /// Defines the tooltip value for motion properties
    /// </summary>
    public class MotionDescriptionAttribute : Attribute
    {
        /// <summary>
        /// Default tooltip value
        /// </summary>
        protected string mValue;
        public string Value
        {
            get { return mValue; }
        }

        /// <summary>
        /// Constructor for the attribute
        /// </summary>
        /// <param name="rValue">Value that is the tooltip</param>
        public MotionDescriptionAttribute(string rValue)
        {
            this.mValue = rValue;
        }

        /// <summary>
        /// Attempts to find a fiendly description. If found, returns it.
        /// </summary>
        /// <param name="rType">Type whose friendly name we want</param>
        /// <returns>String that is the friendly name for class name</returns>
        public static string GetDescription(Type rType)
        {
            string lTypeName = "";

            MotionDescriptionAttribute lAttribute = ReflectionHelper.GetAttribute<MotionDescriptionAttribute>(rType);
            if (lAttribute != null) { lTypeName = lAttribute.Value; }

            return lTypeName;
        }
    }

    /// <summary>
    /// Defines the search categories for the motion type
    /// </summary>
    public class MotionTypeTagsAttribute : Attribute
    {
        /// <summary>
        /// Default tooltip value
        /// </summary>
        protected string mValue;
        public string Value
        {
            get { return mValue; }
        }

        /// <summary>
        /// Constructor for the attribute
        /// </summary>
        /// <param name="rValue">Value that is the tooltip</param>
        public MotionTypeTagsAttribute(string rValue)
        {
            this.mValue = rValue;
        }

        /// <summary>
        /// Attempts to find a category. If found, returns it.
        /// </summary>
        /// <param name="rType">Type whose friendly name we want</param>
        /// <returns>String that is the friendly name for class name</returns>
        public static string GetTypeTags(Type rType)
        {
            string lTypeName = "";

            MotionTypeTagsAttribute lAttribute = ReflectionHelper.GetAttribute<MotionTypeTagsAttribute>(rType);
            if (lAttribute != null) { lTypeName = lAttribute.Value; }

            return lTypeName;
        }

        /// <summary>
        /// Attempts to find a category. If found, returns it.
        /// </summary>
        /// <param name="rType">Type whose friendly name we want</param>
        /// <returns>String that is the friendly name for class name</returns>
        public static bool Contains(Type rType, string rTypeTag)
        {
            MotionTypeTagsAttribute lAttribute = ReflectionHelper.GetAttribute<MotionTypeTagsAttribute>(rType);
            if (lAttribute == null) { return false; }

            string[] lTags = lAttribute.Value.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); 
            if (lTags.Contains(rTypeTag, StringComparer.OrdinalIgnoreCase)) { return true; }

            return false;
        }
    }        
}
