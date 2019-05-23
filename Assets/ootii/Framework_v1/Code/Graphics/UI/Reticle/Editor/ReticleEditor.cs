using UnityEngine;
using UnityEditor;
using com.ootii.Helpers;

namespace com.ootii.Graphics.UI
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Reticle))]
    public class ReticleEditor : UnityEditor.Editor
    {
        // Helps us keep track of when the list needs to be saved. This
        // is important since some changes happen in scene.
        private bool mIsDirty;

        // The actual class we're stroing
        private Reticle mTarget;
        private SerializedObject mTargetSO;

        /// <summary>
        /// Called when the script object is loaded
        /// </summary>
        void OnEnable()
        {
            // Grab the serialized objects
            mTarget = (Reticle)target;
            mTargetSO = new SerializedObject(target);
        }

        /// <summary>
        /// Called when the inspector needs to draw
        /// </summary>
        public override void OnInspectorGUI()
        {
            // Pulls variables from runtime so we have the latest values.
            mTargetSO.Update();

            GUILayout.Space(5);

            EditorHelper.DrawInspectorTitle("ootii Reticle");

            EditorHelper.DrawInspectorDescription("Targeting reticle used to help center the view and select objects.", MessageType.None);

            GUILayout.Space(5);

            if (EditorHelper.BoolField("Is Visible", "Determines if we can see the reticle.", mTarget.IsVisible, mTarget))
            {
                mIsDirty = true;
                mTarget.IsVisible = EditorHelper.FieldBoolValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.Vector2Field("Size", "Width and height of the reticle.", mTarget.Size, mTarget))
            {
                mIsDirty = true;
                mTarget.Size = EditorHelper.FieldVector2Value;
            }

            if (EditorHelper.Vector2Field("Offset", "Offset from the screens center (in pixels).", mTarget.Offset, mTarget))
            {
                mIsDirty = true;
                mTarget.Offset = EditorHelper.FieldVector2Value;
            }

            GUILayout.Space(5f);

            if (EditorHelper.ObjectField<Transform>("Raycast Root", "Transform to use as the start of any raycasting. Typically this is the camera or camera rig.", mTarget.RaycastRoot, mTarget))
            {
                mIsDirty = true;
                mTarget.RaycastRoot = EditorHelper.FieldObjectValue as Transform;
            }

            GUILayout.Space(5f);

            if (EditorHelper.ObjectField<Texture2D>("Background", "Background texture for the reticle.", mTarget.BGTexture, mTarget))
            {
                mIsDirty = true;
                mTarget.BGTexture = EditorHelper.FieldObjectValue as Texture2D;
            }

            if (EditorHelper.ObjectField<Texture2D>("Foreground", "Foreground texture used as fill.", mTarget.FillTexture, mTarget))
            {
                mIsDirty = true;
                mTarget.FillTexture = EditorHelper.FieldObjectValue as Texture2D;
            }

            GUILayout.Space(5f);

            // If there is a change... update.
            if (mIsDirty)
            {
                // Flag the object as needing to be saved
                EditorUtility.SetDirty(mTarget);

#if UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
            EditorApplication.MarkSceneDirty();
#else
                if (!EditorApplication.isPlaying)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
                }
#endif

                // Pushes the values back to the runtime so it has the changes
                mTargetSO.ApplyModifiedProperties();

                // Clear out the dirty flag
                mIsDirty = false;
            }
        }
    }
}