using UnityEngine;
using UnityEditor;
using com.ootii.Graphics.UI;
using com.ootii.Helpers;

namespace com.ootii.Game
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(TargetingReticle))]
    public class TargetingReticleEditor : ReticleEditor
    {
        /// <summary>
        /// Called when the inspector needs to draw
        /// </summary>
        public override void OnInspectorGUI()
        {
            GUILayout.Space(10f);

            EditorHelper.DrawInspectorDescription("This object is deprecated. Please use ootii.Graphics.UI.Reticle instead.", MessageType.Error);

            base.OnInspectorGUI();
        }
    }
}