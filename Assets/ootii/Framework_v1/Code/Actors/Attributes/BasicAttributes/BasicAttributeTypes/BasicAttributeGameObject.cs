using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using com.ootii.Helpers;
#endif

namespace com.ootii.Actors.Attributes
{
    /// <summary>
    /// Basic class for all Attribute GameObject items.
    /// </summary>
    public class BasicAttributeGameObject : BasicAttributeTyped<GameObject>
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

            GameObject lNewValue = EditorGUI.ObjectField(rRect, Value, typeof(GameObject), true) as GameObject;
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
            bool lIsDirty = false;

            EditorHelper.DrawInspectorDescription("GameObjects are saved as paths. If you change the name of the GameObject, reset this value to update the path.", MessageType.None);

            if (EditorHelper.TextField("Name", "Name of the attribute. Used to retrieve the value as well.", ID, rTarget))
            {
                if (!rTarget.AttributeExists(EditorHelper.FieldStringValue))
                {
                    lIsDirty = true;
                    rTarget.RenameAttribute(ID, EditorHelper.FieldStringValue);
                }
            }

            if (EditorHelper.ObjectField<GameObject>("Value", "Value of the Attribute", Value, rTarget))
            {
                lIsDirty = true;
                Value = EditorHelper.FieldObjectValue as GameObject;
            }

            return lIsDirty;
        }

#endif

    }
}
