using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using com.ootii.Helpers;
#endif

namespace com.ootii.Actors.Attributes
{
    /// <summary>
    /// Basic class for all Attribute float items.
    /// </summary>
    public class BasicAttributeFloat : BasicAttributeTyped<float>
    {
        /// <summary>
        /// Minimum value (inclusive) of the attribute.
        /// </summary>
        public float _MinValue = float.MinValue;
        public float MinValue
        {
            get { return _MinValue; }

            set
            {
                _MinValue = value;
                if (_Value < _MinValue) { Value = _MinValue; }
            }
        }

        /// <summary>
        /// Maximum value (inclusive) of the attribute.
        /// </summary>
        public float _MaxValue = float.MaxValue;
        public float MaxValue
        {
            get { return _MaxValue; }

            set
            {
                _MaxValue = value;
                if (_Value > _MaxValue) { Value = _MaxValue; }
            }
        }

        /// <summary>
        /// Value of the attribute. This is faster than the functions below,
        /// but not usable when referencing the base class.
        /// </summary>
        public override float Value
        {
            get { return _Value; }

            set
            {
                if (value < _MinValue) { value = _MinValue; }
                else if (value > _MaxValue) { value = _MaxValue; }

                base.Value = value;
            }
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
        public override bool OnInspectorGUI(Rect rRect)
        {
            bool lIsDirty = false;

            float lNewValue = EditorGUI.FloatField(rRect, Value);
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

            if (EditorHelper.FloatField("Value", "Value of the Attribute", Value, rTarget))
            {
                lIsDirty = true;
                Value = EditorHelper.FieldFloatValue;
            }

            // Limits
            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(new GUIContent("Min/Max", "Min and max limits to the float."), GUILayout.Width(EditorGUIUtility.labelWidth - 4.5f));

            string lText = (MinValue == float.MinValue ? "" : MinValue.ToString("f3"));
            if (EditorHelper.TextField("", "", lText, rTarget, 20f))
            {
                float lValue = 0f;
                if (EditorHelper.FieldStringValue.Length == 0)
                {
                    lIsDirty = true;
                    MinValue = float.MinValue;
                }
                if (float.TryParse(EditorHelper.FieldStringValue, out lValue))
                {
                    lIsDirty = true;
                    MinValue = lValue;
                }
            }

            lText = (MaxValue == float.MaxValue ? "" : MaxValue.ToString("f3"));
            if (EditorHelper.TextField("", "", lText, rTarget, 20f))
            {
                float lValue = 0f;
                if (EditorHelper.FieldStringValue.Length == 0)
                {
                    lIsDirty = true;
                    MaxValue = float.MaxValue;
                }
                if (float.TryParse(EditorHelper.FieldStringValue, out lValue))
                {
                    lIsDirty = true;
                    MaxValue = lValue;
                }
            }

            EditorGUILayout.EndHorizontal();

            return lIsDirty;
        }

#endif

    }
}
