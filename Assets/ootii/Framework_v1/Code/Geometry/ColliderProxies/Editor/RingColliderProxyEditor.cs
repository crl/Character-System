using UnityEditor;
using UnityEngine;
using com.ootii.Geometry;
using com.ootii.Helpers;

[CanEditMultipleObjects]
[CustomEditor(typeof(RingColliderProxy))]
public class RingColliderProxyEditor : Editor
{
    // Helps us keep track of when the target needs to be saved. This
    // is important since some chang es happen in scene.
    private bool mIsDirty;

    // The actual class we're storing
    private RingColliderProxy mTarget;
    private SerializedObject mTargetSO;

    /// <summary>
    /// Called when the object is selected in the editor
    /// </summary>
    private void OnEnable()
    {
        // Grab the serialized objects
        mTarget = (RingColliderProxy)target;
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

        EditorHelper.DrawInspectorTitle("ootii Ring Collider Proxy");

        EditorHelper.DrawInspectorDescription("Passes all trigger events to the target object. This version will create a 'wall' or 'ring' around the center.", MessageType.None);

        GUILayout.Space(5);

        if (EditorHelper.ObjectField<GameObject>("Target", "Target who will get the trigger events", mTarget.Target, mTarget))
        {
            mIsDirty = true;
            mTarget.Target = EditorHelper.FieldObjectValue as GameObject;
        }

        GUILayout.Space(5);

        if (EditorHelper.IntField("Segments", "Number of segments for the ring.", mTarget.Segments, mTarget))
        {
            mIsDirty = true;
            mTarget.Segments = EditorHelper.FieldIntValue;
        }

        if (EditorHelper.FloatField("Thickness", "Thickness of the ring's wall.", mTarget.Thickness, mTarget))
        {
            mIsDirty = true;
            mTarget.Thickness = EditorHelper.FieldFloatValue;
        }

        if (EditorHelper.FloatField("Speed", "Speed (degrees per second) used to create the wall over time. 0 means instant, posative means clockwise, and negative is counter-clockwise.", mTarget.Speed, mTarget))
        {
            mIsDirty = true;
            mTarget.Speed = EditorHelper.FieldFloatValue;
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
