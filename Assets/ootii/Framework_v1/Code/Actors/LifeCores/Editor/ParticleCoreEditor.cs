using UnityEngine;
using UnityEditor;
using com.ootii.Actors.LifeCores;
using com.ootii.Helpers;

[CanEditMultipleObjects]
[CustomEditor(typeof(ParticleCore), true)]
public class ParticleCoreEditor : Editor
{
    // Helps us keep track of when the list needs to be saved. This
    // is important since some changes happen in scene.
    private bool mIsDirty;

    // The actual class we're storing
    private ParticleCore mTarget;
    private SerializedObject mTargetSO;

    /// <summary>
    /// Called when the object is selected in the editor
    /// </summary>
    private void OnEnable()
    {
        // Grab the serialized objects
        mTarget = (ParticleCore)target;
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

        EditorHelper.DrawInspectorTitle("ootii Particle Core");

        EditorHelper.DrawInspectorDescription("Very basic foundation for particles. This allows us to set some simple properties and auto-destroy.", MessageType.None);

        GUILayout.Space(5);

        if (EditorHelper.FloatField("Max Age", "Seconds before the object is destroyed.", mTarget.MaxAge, mTarget))
        {
            mIsDirty = true;
            mTarget.MaxAge = EditorHelper.FieldFloatValue;
        }

        if (EditorHelper.ObjectField<Transform>("Attractor", "Target that we'll pull the particles towards.", mTarget.Attractor, mTarget))
        {
            mIsDirty = true;
            mTarget.Attractor = EditorHelper.FieldObjectValue as Transform;
        }

        if (mTarget.Attractor != null)
        {
            if (EditorHelper.Vector3Field("Attractor Offset", "Offset from the attractor that the particles will pull towards.", mTarget.AttractorOffset, mTarget))
            {
                mIsDirty = true;
                mTarget.AttractorOffset = EditorHelper.FieldVector3Value;
            }
        }

        GUILayout.Space(5f);

        EditorGUILayout.LabelField("Particle & Sound Containers", EditorStyles.boldLabel, GUILayout.Height(16f));

        GUILayout.BeginVertical(EditorHelper.Box);

        if (EditorHelper.ObjectField<GameObject>("Particle Root", "Child objects that holds effects and sounds during the life of the area.", mTarget.LifeRoot, mTarget))
        {
            mIsDirty = true;
            mTarget.LifeRoot = EditorHelper.FieldObjectValue as GameObject;
        }

        GUILayout.EndVertical();

        GUILayout.Space(5f);

        EditorGUILayout.LabelField("Skim Properties", EditorStyles.boldLabel, GUILayout.Height(16f));

        GUILayout.BeginVertical(EditorHelper.Box);

        if (EditorHelper.BoolField("Skim Surface", "Determines if we'll move the particles to skim the surface/ground", mTarget.SkimSurface, mTarget))
        {
            mIsDirty = true;
            mTarget.SkimSurface = EditorHelper.FieldBoolValue;
        }

        if (mTarget.SkimSurface)
        {
            if (EditorHelper.FloatField("Distance", "Distance offset from the surface/ground", mTarget.SkimSurfaceDistance, mTarget))
            {
                mIsDirty = true;
                mTarget.SkimSurfaceDistance = EditorHelper.FieldFloatValue;
            }

            // Collisions layer
            int lNewCollisionLayers = EditorHelper.LayerMaskField(new GUIContent("Collision Layers", "Layers that we'll test collisions against"), mTarget.SkimSurfaceLayers);
            if (lNewCollisionLayers != mTarget.SkimSurfaceLayers)
            {
                mIsDirty = true;
                mTarget.SkimSurfaceLayers = lNewCollisionLayers;
            }
        }

        GUILayout.EndVertical();

        GUILayout.Space(5f);

        EditorGUILayout.LabelField("Fade Properties", EditorStyles.boldLabel, GUILayout.Height(16f));

        GUILayout.BeginVertical(EditorHelper.Box);

        // Audio fade
        UnityEngine.GUILayout.BeginHorizontal();

        UnityEditor.EditorGUILayout.LabelField(new UnityEngine.GUIContent("Audio Fade", "Fade in and fade out speed."), GUILayout.Width(EditorGUIUtility.labelWidth));

        if (EditorHelper.FloatField(mTarget.AudioFadeInSpeed, "Fade In", mTarget, 0f, 20f))
        {
            mIsDirty = true;
            mTarget.AudioFadeInSpeed = EditorHelper.FieldFloatValue;
        }

        if (EditorHelper.FloatField(mTarget.AudioFadeOutSpeed, "Fade Out", mTarget, 0f, 20f))
        {
            mIsDirty = true;
            mTarget.AudioFadeOutSpeed = EditorHelper.FieldFloatValue;
        }

        UnityEngine.GUILayout.EndHorizontal();

        // Light fade
        UnityEngine.GUILayout.BeginHorizontal();

        UnityEditor.EditorGUILayout.LabelField(new UnityEngine.GUIContent("Light Fade", "Fade in and fade out speed."), GUILayout.Width(EditorGUIUtility.labelWidth));

        if (EditorHelper.FloatField(mTarget.LightFadeInSpeed, "Fade In", mTarget, 0f, 20f))
        {
            mIsDirty = true;
            mTarget.LightFadeInSpeed = EditorHelper.FieldFloatValue;
        }

        if (EditorHelper.FloatField(mTarget.LightFadeOutSpeed, "Fade Out", mTarget, 0f, 20f))
        {
            mIsDirty = true;
            mTarget.LightFadeOutSpeed = EditorHelper.FieldFloatValue;
        }

        UnityEngine.GUILayout.EndHorizontal();

        // Projector fade
        UnityEngine.GUILayout.BeginHorizontal();

        UnityEditor.EditorGUILayout.LabelField(new UnityEngine.GUIContent("Projector Fade", "Fade in and fade out speed."), GUILayout.Width(EditorGUIUtility.labelWidth));

        if (EditorHelper.FloatField(mTarget.ProjectorFadeInSpeed, "Fade In", mTarget, 0f, 20f))
        {
            mIsDirty = true;
            mTarget.ProjectorFadeInSpeed = EditorHelper.FieldFloatValue;
        }

        if (EditorHelper.FloatField(mTarget.ProjectorFadeOutSpeed, "Fade Out", mTarget, 0f, 20f))
        {
            mIsDirty = true;
            mTarget.ProjectorFadeOutSpeed = EditorHelper.FieldFloatValue;
        }

        UnityEngine.GUILayout.EndHorizontal();

        GUILayout.EndVertical();

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