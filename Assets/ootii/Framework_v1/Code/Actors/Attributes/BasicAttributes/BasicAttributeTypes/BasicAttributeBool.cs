using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using com.ootii.Helpers;
#endif

namespace com.ootii.Actors.Attributes
{
    /// <summary>
    /// Basic class for all Attribute bool items.
    /// </summary>
    public class BasicAttributeBool : BasicAttributeTyped<bool>
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

            bool lNewValue = EditorGUI.Toggle(rRect, Value);
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

            bool lNewValue = EditorGUILayout.Toggle("Value", Value);
            if (lNewValue != Value)
            {
                lIsDirty = true;
                Value = lNewValue;
            }

            return lIsDirty;
        }

#endif

    }
}
