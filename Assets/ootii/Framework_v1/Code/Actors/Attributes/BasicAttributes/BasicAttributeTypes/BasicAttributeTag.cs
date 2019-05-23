using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using com.ootii.Helpers;
#endif

namespace com.ootii.Actors.Attributes
{
    /// <summary>
    /// Basic class for all Attribute tag items. Tags represent a simple "does 
    /// it exist" attribute
    /// </summary>
    public class BasicAttributeTag : BasicAttributeTyped<bool>
    {
        /// <summary>
        /// Default constructor
        /// </summary>
        public BasicAttributeTag()
        {
            _Value = true;
            mValueType = null;
        }
    }
}
