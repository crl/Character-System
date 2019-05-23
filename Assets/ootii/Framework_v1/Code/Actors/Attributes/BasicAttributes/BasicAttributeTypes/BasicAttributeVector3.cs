using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using com.ootii.Helpers;
#endif

namespace com.ootii.Actors.Attributes
{
    /// <summary>
    /// Basic class for all Attribute Vector3 items.
    /// </summary>
    public class BasicAttributeVector3 : BasicAttributeTyped<Vector3>
    {

        // **************************************************************************************************
        // Following properties and function only valid while editing
        // **************************************************************************************************

#if UNITY_EDITOR

        /// <summary>
        /// Allows each attribute item to render thier own GUI
        /// </summary>
        /// <param name="rTarget"></param>
        /// <returns></returns>
        public override bool OnInspectorGUI(Rect rRect)
        {
            bool lIsDirty = false;

            Vector3 lNewValue = EditorGUI.Vector3Field(rRect, "", Value);
            if (lNewValue != Value)
            {
                lIsDirty = true;
                Value = lNewValue;
            }

            return lIsDirty;
        }

        /// <summary>
        /// Allows each attribute item to render thier own GUI
        /// </summary>
        /// <param name="rTarget"></param>
        /// <returns></returns>
        public override bool OnInspectorGUI(BasicAttributes rTarget)
        {
            bool lIsDirty = base.OnInspectorGUI(rTarget);

            if (EditorHelper.Vector3Field("Value", "Value of the Attribute", Value, rTarget))
            {
                lIsDirty = true;
                Value = EditorHelper.FieldVector3Value;
            }

            return lIsDirty;
        }

#endif

    }
}
