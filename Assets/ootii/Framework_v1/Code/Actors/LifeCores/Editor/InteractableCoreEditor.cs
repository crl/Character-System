using UnityEngine;
using UnityEditor;
using com.ootii.Actors.LifeCores;
using com.ootii.Helpers;

[CanEditMultipleObjects]
[CustomEditor(typeof(InteractableCore))]
public class InteractableCoreEditor : Editor
{
    // Helps us keep track of when the list needs to be saved. This
    // is important since some changes happen in scene.
    private bool mIsDirty;

    // The actual class we're storing
    private InteractableCore mTarget;
    private SerializedObject mTargetSO;

    /// <summary>
    /// Called when the object is selected in the editor
    /// </summary>
    private void OnEnable()
    {
        // Grab the serialized objects
        mTarget = (InteractableCore)target;
        mTargetSO = new SerializedObject(target);
    }

    /// <summary>
    /// This function is called when the scriptable object goes out of scope.
    /// </summary>
    private void OnDisable()
    {
    }

    /// <summary>
    /// Called when the inspector needs to draw
    /// </summary>
    public override void OnInspectorGUI()
    {
        // Pulls variables from runtime so we have the latest values.
        mTargetSO.Update();

        GUILayout.Space(5);

        EditorHelper.DrawInspectorTitle("ootii Interactable Core");

        EditorHelper.DrawInspectorDescription("Very basic foundation for object that can be interacted with.", MessageType.None);

        GUILayout.Space(5);

        if (EditorHelper.BoolField("Is Enabled", "Determines if the object can be interacted with.", mTarget.IsEnabled, mTarget))
        {
            mIsDirty = true;
            mTarget.IsEnabled = EditorHelper.FieldBoolValue;
        }

        //if (EditorHelper.TextField("Interaction Motion", "Motion that we'll use to interact with.", mTarget.Motion, mTarget))
        //{
        //    mIsDirty = true;
        //    mTarget.Motion = EditorHelper.FieldStringValue;
        //}

        if (EditorHelper.IntField("Form", "Motion form that will be used as the animation.", mTarget.Form, mTarget))
        {
            mIsDirty = true;
            mTarget.Form = EditorHelper.FieldIntValue;
        }

        GUILayout.Space(5);

        EditorGUILayout.LabelField("Focus Properties", EditorStyles.boldLabel, GUILayout.Height(16f));

        EditorGUILayout.BeginVertical(EditorHelper.GroupBox);

        EditorHelper.DrawInspectorDescription("Defines how we determine if the object has focus.", MessageType.None);

        EditorGUILayout.BeginVertical(EditorHelper.Box);

        if (EditorHelper.BoolField("Use Raycast", "Determines if the interaction motion will use raycasting to find the target.", mTarget.UseRaycast, mTarget))
        {
            mIsDirty = true;
            mTarget.UseRaycast = EditorHelper.FieldBoolValue;
        }

        if (mTarget.UseRaycast)
        {
            if (EditorHelper.ObjectField<Collider>("Raycast Collider", "Collider that the ray should collide with to focus.", mTarget.RaycastCollider, mTarget))
            {
                mIsDirty = true;
                mTarget.RaycastCollider = EditorHelper.FieldObjectValue as Collider;
            }
        }

        if (EditorHelper.ObjectField<Collider>("Trigger Collider", "Collider that the character must be in to focus.", mTarget.TriggerCollider, mTarget))
        {
            mIsDirty = true;
            mTarget.TriggerCollider = EditorHelper.FieldObjectValue as Collider;
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        EditorGUILayout.LabelField("Focus Highlight", EditorStyles.boldLabel, GUILayout.Height(16f));

        EditorGUILayout.BeginVertical(EditorHelper.GroupBox);

        EditorHelper.DrawInspectorDescription("Determines if and how we highlight the object when it has focus.", MessageType.None);

        EditorGUILayout.BeginVertical(EditorHelper.Box);

        if (EditorHelper.ObjectField<Renderer>("Renderer", "Renderer to highlight when focused.", mTarget.HighlightRenderer, mTarget))
        {
            mIsDirty = true;
            mTarget.HighlightRenderer = EditorHelper.FieldObjectValue as Renderer;
        }

        if (EditorHelper.ObjectField<Material>("Material", "Material to render the highlight with.", mTarget.HighlightMaterial, mTarget))
        {
            mIsDirty = true;
            mTarget.HighlightMaterial = EditorHelper.FieldObjectValue as Material;
        }

        Color lNewColor = EditorGUILayout.ColorField(new GUIContent("Color", "Color to render the highlight as."), mTarget.HighlightColor);
        if (lNewColor != mTarget.HighlightColor)
        {
            mIsDirty = true;
            mTarget.HighlightColor = lNewColor;
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        EditorGUILayout.LabelField("Actor Positioning", EditorStyles.boldLabel, GUILayout.Height(16f));

        EditorGUILayout.BeginVertical(EditorHelper.GroupBox);

        EditorHelper.DrawInspectorDescription("Determines if we move and rotate the actor before they can activate the object.", MessageType.None);

        EditorGUILayout.BeginVertical(EditorHelper.Box);

        if (EditorHelper.BoolField("Force Postion", "Determines if we force the actor's postion before we interact.", mTarget.ForcePosition, mTarget))
        {
            mIsDirty = true;
            mTarget.ForcePosition = EditorHelper.FieldBoolValue;
        }

        if (EditorHelper.BoolField("Force Rotation", "Determines if we force the actor's rotation before we interact.", mTarget.ForceRotation, mTarget))
        {
            mIsDirty = true;
            mTarget.ForceRotation = EditorHelper.FieldBoolValue;
        }

        if (EditorHelper.ObjectField<Transform>("Location", "Transform to force the position and rotation to before we interact.", mTarget.TargetLocation, mTarget))
        {
            mIsDirty = true;
            mTarget.TargetLocation = EditorHelper.FieldObjectValue as Transform;
        }

        if (EditorHelper.FloatField("Distance", "Distance to force the position to before we interact.", mTarget.TargetDistance, mTarget))
        {
            mIsDirty = true;
            mTarget.TargetDistance = EditorHelper.FieldFloatValue;
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        // Show the events
        GUILayout.BeginHorizontal();

        if (GUILayout.Button(new GUIContent("Events"), EditorStyles.boldLabel))
        {
            mTarget.EditorShowEvents = !mTarget.EditorShowEvents;
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button(new GUIContent(mTarget.EditorShowEvents ? "-" : "+"), EditorStyles.boldLabel))
        {
            mTarget.EditorShowEvents = !mTarget.EditorShowEvents;
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginVertical(EditorHelper.GroupBox);
        EditorHelper.DrawInspectorDescription("Assign functions to be called when specific events take place.", MessageType.None);

        if (mTarget.EditorShowEvents)
        {
            GUILayout.BeginVertical(EditorHelper.Box);

            SerializedProperty lActivatedEvent = mTargetSO.FindProperty("ActivatedEvent");
            if (lActivatedEvent != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(lActivatedEvent);
                if (EditorGUI.EndChangeCheck())
                {
                    mIsDirty = true;
                }
            }

            GUILayout.EndVertical();
        }

        GUILayout.EndVertical();

        GUILayout.Space(5);

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
