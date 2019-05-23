using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using com.ootii.Actors;
using com.ootii.Cameras;
using com.ootii.Editor;
using com.ootii.Geometry;
using com.ootii.Helpers;

[CanEditMultipleObjects]
[CustomEditor(typeof(ActorController))]
public class ActorControllerEditor : Editor
{
    // Helps us keep track of when the list needs to be saved. This
    // is important since some changes happen in scene.
    private bool mIsDirty;

    // The actual class we're storing
    private ActorController mTarget;
    private SerializedObject mTargetSO;

    // List object for our shapes
    private ReorderableList mList;

    /// <summary>
    /// Creates the drop-down list of body shapes
    /// </summary>
    private int mBodyShapeIndex;
    private List<Type> mBodyShapeTypes = new List<Type>();
    private List<String> mBodyShapeNames = new List<string>();

    /// <summary>
    /// Called when the object is selected in the editor
    /// </summary>
    private void OnEnable()
    {
        // Grab the serialized objects
        mTarget = (ActorController)target;
        mTargetSO = new SerializedObject(target);

        // Reinstanciate any of the body shapes
        if (!UnityEngine.Application.isPlaying)
        {
            mTarget.DeserializeBodyShapes();
        }

        // Create the list of body shapes
        InstanciateList();

        // Generate the list of motions to display
        Assembly lAssembly = Assembly.GetAssembly(typeof(ActorController));
        foreach (Type lType in lAssembly.GetTypes())
        {
            if (lType.IsAbstract) { continue; }
            if (typeof(BodyShape).IsAssignableFrom(lType))
            {
                mBodyShapeTypes.Add(lType);
                mBodyShapeNames.Add(StringHelper.FormatCamelCase(lType.Name));
            }
        }

        // Initialize the component for the first time.
        if (!mTarget.EditorComponentInitialized)
        {
            if (mTarget.BodyShapes.Count == 0)
            {
                CreateDefaultShapes();

                mTarget.EditorComponentInitialized = true;
                mIsDirty = true;
            }
        }

        // Refresh the layers in case they were updated
        EditorHelper.RefreshLayers();
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

        // Start rendering the GUI
        GUILayout.Space(5);

        EditorHelper.DrawInspectorTitle("ootii Actor Controller");

        //if (!TestGroundingRadius())
        //{
        //    EditorGUILayout.BeginVertical(WarningBox);

        //    Color lGUIColor = GUI.color;
        //    GUI.color = EditorHelper.CreateColor(255f, 0f, 0f, 0.8f);
        //    EditorGUILayout.HelpBox("Your Grounding Radius is greater than or equal to a body shape radius. Reduce your Grounding Radius.", MessageType.Warning);
        //    GUI.color = lGUIColor;

        //    EditorGUILayout.EndVertical();
        //}

        GUILayout.Space(10);

        EditorGUILayout.BeginHorizontal();

        GUILayout.FlexibleSpace();

        EditorGUILayout.BeginHorizontal();

        GUILayout.Label("", BasicIcon, GUILayout.Width(16), GUILayout.Height(16));

        if (GUILayout.Button("Basic", EditorStyles.miniButton, GUILayout.Width(70)))
        {
            mTarget.EditorShowAdvanced = false;
            mIsDirty = true;
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(20);

        EditorGUILayout.BeginHorizontal();

        GUILayout.Label("", AdvancedIcon, GUILayout.Width(16), GUILayout.Height(16));

        if (GUILayout.Button("Advanced", EditorStyles.miniButton, GUILayout.Width(70)))
        {
            mTarget.EditorShowAdvanced = true;
            mIsDirty = true;
        }

        EditorGUILayout.EndHorizontal();

        GUILayout.FlexibleSpace();

        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);

        if (mTarget.EditorShowAdvanced)
        {
            mIsDirty = OnAdvancedInspector();
        }
        else
        {
            mIsDirty = OnBasicInspector();
        }

        GUILayout.Space(5);

        EditorGUILayout.LabelField("Body Shapes", EditorStyles.boldLabel, GUILayout.Height(16f));

        GUILayout.BeginVertical(EditorHelper.GroupBox);
        EditorHelper.DrawInspectorDescription("Body shapes define the collision areas of the actor.", MessageType.None);

        mList.DoLayoutList();

        if (mList.index >= 0)
        {
            GUILayout.Space(5f);
            GUILayout.BeginVertical(EditorHelper.Box);

            bool lItemIsDirty = DrawDetailItem(mTarget.BodyShapes[mList.index]);
            if (lItemIsDirty) { mIsDirty = true; }

            GUILayout.EndVertical();
        }

        GUILayout.EndVertical();

        GUILayout.Space(5f);

        // Show the debug
        EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel, GUILayout.Height(16f));

        GUILayout.BeginVertical(EditorHelper.GroupBox);
        EditorHelper.DrawInspectorDescription("Determines if we'll render debug information. We can do this motion-by-motion or for all.", MessageType.None);

        GUILayout.BeginVertical(EditorHelper.Box);

        if (EditorHelper.BoolField("Show Debug Info", "Determines if the AC will render debug information at all.", mTarget.ShowDebug, mTarget))
        {
            mIsDirty = true;
            mTarget.ShowDebug = EditorHelper.FieldBoolValue;
        }

        GUILayout.EndVertical();

        GUILayout.EndVertical();

        GUILayout.Space(10);

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

    /// <summary>
    /// Draws the basic version of the GUI
    /// </summary>
    /// <returns></returns>
    private bool OnBasicInspector()
    {
        bool lIsDirty = false;

        GUILayout.Space(5f);

        EditorGUILayout.BeginVertical(Box, GUILayout.Height(40f));

        EditorGUILayout.BeginHorizontal();

        GUILayout.Space(10);

        EditorGUILayout.BeginVertical(GUILayout.Width(16f));
        GUILayout.Space(12);
        bool lNewEditorCollideWithObjects = EditorGUILayout.Toggle(mTarget.EditorCollideWithObjects, OptionToggle);
        if (lNewEditorCollideWithObjects != mTarget.EditorCollideWithObjects)
        {
            lIsDirty = true;
            mTarget.EditorCollideWithObjects = lNewEditorCollideWithObjects;

            EnableCollisions(mTarget.EditorCollideWithObjects);
        }
        EditorGUILayout.EndVertical();

        if (GUILayout.Button(new GUIContent("", "Enable collisions."), CapsuleIcon, GUILayout.Width(32f), GUILayout.Height(32f)))
        {
            lIsDirty = true;
            mTarget.EditorCollideWithObjects = !mTarget.EditorCollideWithObjects;

            EnableCollisions(mTarget.EditorCollideWithObjects);
        }

        EditorGUILayout.LabelField("Collide with objects", OptionText, GUILayout.MinWidth(50), GUILayout.ExpandWidth(true));

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();


        GUILayout.Space(5);


        EditorGUILayout.BeginVertical(Box, GUILayout.Height(40f));

        EditorGUILayout.BeginHorizontal();

        GUILayout.Space(10);

        EditorGUILayout.BeginVertical(GUILayout.Width(16f));
        GUILayout.Space(12);
        bool lNewEditorWalkOnWalls = EditorGUILayout.Toggle(mTarget.EditorWalkOnWalls, OptionToggle);
        if (lNewEditorWalkOnWalls != mTarget.EditorWalkOnWalls)
        {
            lIsDirty = true;
            mTarget.EditorWalkOnWalls = lNewEditorWalkOnWalls;

            EnableWallWalking(mTarget.EditorWalkOnWalls);
        }
        EditorGUILayout.EndVertical();

        if (GUILayout.Button(new GUIContent("", "Enable wall walking."), SpiderIcon, GUILayout.Width(32f), GUILayout.Height(32f)))
        {
            lIsDirty = true;
            mTarget.EditorWalkOnWalls = !mTarget.EditorWalkOnWalls;

            EnableWallWalking(mTarget.EditorWalkOnWalls);
        }

        EditorGUILayout.LabelField("Walk on walls", OptionText, GUILayout.MinWidth(50), GUILayout.ExpandWidth(true));

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();


        GUILayout.Space(5);


        EditorGUILayout.BeginVertical(Box, GUILayout.Height(40f));

        EditorGUILayout.BeginHorizontal();

        GUILayout.Space(10);

        EditorGUILayout.BeginVertical(GUILayout.Width(16f));
        GUILayout.Space(12);
        bool lNewEditorSlideOnSlopes = EditorGUILayout.Toggle(mTarget.EditorSlideOnSlopes, OptionToggle);
        if (lNewEditorSlideOnSlopes != mTarget.EditorSlideOnSlopes)
        {
            lIsDirty = true;
            mTarget.EditorSlideOnSlopes = lNewEditorSlideOnSlopes;

            EnableSlopeSliding(mTarget.EditorSlideOnSlopes);
        }
        EditorGUILayout.EndVertical();

        if (GUILayout.Button(new GUIContent("", "Enable slope sliding."), SlopeIcon, GUILayout.Width(32f), GUILayout.Height(32f)))
        {
            lIsDirty = true;
            mTarget.EditorSlideOnSlopes = !mTarget.EditorSlideOnSlopes;

            EnableSlopeSliding(mTarget.EditorSlideOnSlopes);
        }

        EditorGUILayout.LabelField("Slide on slopes", OptionText, GUILayout.MinWidth(50), GUILayout.ExpandWidth(true));

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();


        GUILayout.Space(5);


        EditorGUILayout.BeginVertical(Box, GUILayout.Height(40f));

        EditorGUILayout.BeginHorizontal();

        GUILayout.Space(10);

        EditorGUILayout.BeginVertical(GUILayout.Width(16f));
        GUILayout.Space(12);
        bool lNewEditorRespondToColliders = EditorGUILayout.Toggle(mTarget.EditorRespondToColliders, OptionToggle);
        if (lNewEditorRespondToColliders != mTarget.EditorRespondToColliders)
        {
            lIsDirty = true;
            mTarget.EditorRespondToColliders = lNewEditorRespondToColliders;

            EnableCollisionResponse(mTarget.EditorRespondToColliders);
        }
        EditorGUILayout.EndVertical();

        if (GUILayout.Button(new GUIContent("", "Enable collision response."), ResponseIcon, GUILayout.Width(32f), GUILayout.Height(32f)))
        {
            lIsDirty = true;
            mTarget.EditorRespondToColliders = !mTarget.EditorRespondToColliders;

            EnableCollisionResponse(mTarget.EditorRespondToColliders);
        }

        EditorGUILayout.LabelField("Move due to colliders", OptionText, GUILayout.MinWidth(50), GUILayout.ExpandWidth(true));

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();

        return lIsDirty;
    }

    /// <summary>
    /// Draws the advanced verions of the GUI
    /// </summary>
    /// <returns></returns>
    private bool OnAdvancedInspector()
    {
        bool lIsDirty = false;

        //// Stablize the update cycle
        //EditorGUILayout.BeginHorizontal();

        //bool lNewIsFixedUpdateEnabled = EditorGUILayout.Toggle(new GUIContent("Use Fixed Update", "Determines if we attempt to keep a stable update schedule. This is useful for simulations."), mTarget.IsFixedUpdateEnabled);
        //if (lNewIsFixedUpdateEnabled != mTarget.IsFixedUpdateEnabled)
        //{
        //    lIsDirty = true;
        //    mTarget.IsFixedUpdateEnabled = lNewIsFixedUpdateEnabled;
        //}

        //GUILayout.Space(45);
        //EditorGUILayout.LabelField(new GUIContent("FPS", "Determines the frame rate we're targeting for fixed updates."), GUILayout.Width(50));
        //int lNewFixedUpdateFPS = EditorGUILayout.IntField((int)mTarget.FixedUpdateFPS, GUILayout.Width(65));
        //if (lNewFixedUpdateFPS != mTarget.FixedUpdateFPS)
        //{
        //    lIsDirty = true;
        //    mTarget.FixedUpdateFPS = lNewFixedUpdateFPS;
        //}

        //GUILayout.FlexibleSpace();

        //EditorGUILayout.EndHorizontal();

        //GUILayout.Space(5);

        // Camera
        //Camera lNewCamera = EditorGUILayout.ObjectField(new GUIContent("Camera", "Optional camera that this actor is responsible for."), mTarget.Camera, typeof(Camera), true) as Camera;
        //if (mTarget.Camera != lNewCamera)
        //{
        //    mIsDirty = true;
        //    mTarget.Camera = lNewCamera;
        //}

        //GUILayout.Space(5);

        // Update section
        EditorGUILayout.BeginVertical(EditorHelper.Box);

        bool lNewIsEnabled = EditorGUILayout.Toggle(new GUIContent("Is Enabled", "Determines if the AC is updating and controlling movement and rotation."), mTarget.IsEnabled);
        if (lNewIsEnabled != mTarget.IsEnabled)
        {
            lIsDirty = true;
            mTarget.IsEnabled = lNewIsEnabled;
        }

        bool lNewProcessInLateUpdate = EditorGUILayout.Toggle(new GUIContent("Use LateUpdate", "Determines if we'll do processing in Unity's Update() or LateUpdate() functions. LateUpdate() is prefered, but not always possible."), mTarget.ProcessInLateUpdate);
        if (lNewProcessInLateUpdate != mTarget.ProcessInLateUpdate)
        {
            lIsDirty = true;
            mTarget.ProcessInLateUpdate = lNewProcessInLateUpdate;
        }

        bool lNewInvertRotationOrder = EditorGUILayout.Toggle(new GUIContent("Invert Rotation Order", "Causes rotations to be yaw * tilt (instead of tilt * yaw). This is useful for flying, swimming, and other 3D movements."), mTarget.InvertRotationOrder);
        if (lNewInvertRotationOrder != mTarget.InvertRotationOrder)
        {
            lIsDirty = true;
            mTarget.InvertRotationOrder = lNewInvertRotationOrder;
        }

        GUILayout.Space(5f);

        //if (EditorHelper.BoolField("Use Transform Position", "Determines if we ignore movement requests and simply use the transform position. This is useful for NPCs that are driven by nav mesh agents and other drivers.", mTarget.UseTransformPosition, mTarget))
        //{
        //    lIsDirty = true;
        //    mTarget.UseTransformPosition = EditorHelper.FieldBoolValue;
        //}

        //if (EditorHelper.BoolField("Use Transform Rotation", "Determines if we ignore rotation requests and simply use the transform rotation. This is useful for NPCs that are driven by nav mesh agents and other drivers.", mTarget.UseTransformRotation, mTarget))
        //{
        //    lIsDirty = true;
        //    mTarget.UseTransformRotation = EditorHelper.FieldBoolValue;
        //}

        //GUILayout.Space(5f);

        EditorHelper.DrawLine();

        if (EditorHelper.BoolField("Use Transform", "Determines if we ignore movement requests and simply use the transform position. This is useful for NPCs that are driven by nav mesh agents and other assets.", mTarget.UseTransformPosition, mTarget))
        {
            lIsDirty = true;
            mTarget.UseTransformPosition = EditorHelper.FieldBoolValue;
        }

        if (mTarget.UseTransformPosition)
        {
            if (EditorHelper.BoolField("  Rotation", "Determines if we ignore rotation requests and simply use the transform rotation. This is useful for NPCs that are driven by nav mesh agents and other drivers.", mTarget.UseTransformRotation, mTarget))
            {
                lIsDirty = true;
                mTarget.UseTransformRotation = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.FloatField("  Skin Width", "Skin width used when using Use Transform. This can be important as the nav mesh is built away from the mesh and stairs can cause the character to think he's not grounded.", mTarget.AltSkinWidth, mTarget))
            {
                lIsDirty = true;
                mTarget.AltSkinWidth = EditorHelper.FieldFloatValue;
            }
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        EditorGUILayout.LabelField("Actor Properties", EditorStyles.boldLabel, GUILayout.Height(16f));

        EditorGUILayout.BeginVertical(EditorHelper.GroupBox);

        EditorHelper.DrawInspectorDescription("Basic properties that define the character.", MessageType.None);

        EditorGUILayout.BeginVertical(EditorHelper.Box);

        if (EditorHelper.FloatField("Height", "Typical height (in units) of the character along the world's y axis.", mTarget.Height, mTarget))
        {
            lIsDirty = true;
            mTarget.Height = EditorHelper.FieldFloatValue;
        }

        if (EditorHelper.FloatField("Width (as radius)", "Typical radius (in units) of the character along the world's x and z axis.", mTarget.Radius, mTarget))
        {
            lIsDirty = true;
            mTarget.Radius = EditorHelper.FieldFloatValue;
        }

        if (EditorHelper.FloatField("Mass", "Typical mass of the character. In order to be consistant with Unity physics, mass follows thier scale (1 cube unit = ~45kg = 1 point). So an averge male would be 2. (2 cube units = ~70kg = 2 points)", mTarget.Mass, mTarget))
        {
            lIsDirty = true;
            mTarget.Mass = EditorHelper.FieldFloatValue;
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndVertical();

        GUILayout.Space(5);

        if (!mTarget.UseTransformPosition)
        {
            // Gravity section

            EditorGUILayout.LabelField("Gravity & Grounding", EditorStyles.boldLabel, GUILayout.Height(16f));

            EditorGUILayout.BeginVertical(EditorHelper.GroupBox);

            EditorHelper.DrawInspectorDescription("Properties that define how gravity works for this actor and how the actor will determine if they are on the ground.", MessageType.None);

            EditorGUILayout.BeginVertical(EditorHelper.Box);

            EditorGUILayout.BeginHorizontal();

            UnityEditor.EditorGUILayout.LabelField(new UnityEngine.GUIContent("Is Gravity Enabled", "Determines if we'll apply gravity to the character."), UnityEngine.GUILayout.Width(UnityEditor.EditorGUIUtility.labelWidth - 4f));

            if (EditorHelper.BoolField(mTarget.IsGravityEnabled, "Is Gravity Enabled", mTarget, 30f))
            {
                lIsDirty = true;
                mTarget.IsGravityEnabled = EditorHelper.FieldBoolValue;
            }

            EditorHelper.LabelField("Relative", "Determines if gravity is relative to the character's tilt.", 50f);
            if (EditorHelper.BoolField(mTarget.IsGravityRelative, "Is Relative", mTarget, 10f))
            {
                lIsDirty = true;
                mTarget.IsGravityRelative = EditorHelper.FieldBoolValue;
                mTarget.EditorWalkOnWalls = mTarget.OrientToGround && EditorHelper.FieldBoolValue;
            }

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();

            UnityEditor.EditorGUILayout.LabelField(new UnityEngine.GUIContent("Use Fixed Update", "Determines if gravity is applied during FixedUpdate or LateUpdate."), UnityEngine.GUILayout.Width(UnityEditor.EditorGUIUtility.labelWidth - 4f));

            if (EditorHelper.BoolField(mTarget.IsFixedUpdateEnabled, "Use Fixed Update", mTarget, 30f))
            {
                lIsDirty = true;
                mTarget.IsFixedUpdateEnabled = EditorHelper.FieldBoolValue;
            }

            EditorHelper.LabelField("Extrapolate", "Determines if we estimate the physics forces in Update() to smooth movement.", 50f);
            if (EditorHelper.BoolField(mTarget.ExtrapolatePhysics, "Extrapolate", mTarget, 10f))
            {
                lIsDirty = true;
                mTarget.ExtrapolatePhysics = EditorHelper.FieldBoolValue;
            }

            EditorGUILayout.EndHorizontal();

            GUILayout.Space(5);

            if (EditorHelper.Vector3Field("Gravity", "Gravity to apply to this actor. Use (0, 0, 0) to use Unity's gravity.", mTarget.Gravity, mTarget))
            {
                lIsDirty = true;
                mTarget.Gravity = EditorHelper.FieldVector3Value;
            }

            if (EditorHelper.FloatField("Dampen Factor", "When grounded, multiplier to apply to lateral movement due to applied forces.", mTarget.GroundDampenFactor, mTarget))
            {
                lIsDirty = true;
                mTarget.GroundDampenFactor = EditorHelper.FieldFloatValue;
            }

            GUILayout.Space(5);

            if (EditorHelper.FloatField("Skin Width", "Range in which we'll force the actor to the ground.", mTarget.SkinWidth, mTarget))
            {
                lIsDirty = true;
                mTarget.SkinWidth = EditorHelper.FieldFloatValue;
            }

            EditorHelper.DrawLine();

            if (EditorHelper.BoolField("Use Grounding Layers", "Determine if we will use the grounding layers to test grounding collisions against.", mTarget.IsGroundingLayersEnabled, mTarget))
            {
                lIsDirty = true;
                mTarget.IsGroundingLayersEnabled = EditorHelper.FieldBoolValue;
            }

            // Grounding layer
            int lNewGroundingLayers = EditorHelper.LayerMaskField(new GUIContent("Grounding Layers", "Layers that we'll test grounding collisions against"), mTarget.GroundingLayers);
            if (lNewGroundingLayers != mTarget.GroundingLayers)
            {
                lIsDirty = true;
                mTarget.GroundingLayers = lNewGroundingLayers;
            }

            if (EditorHelper.FloatField("Grounding Radius", "Radius of the 'feet' used to help determine grounding.", mTarget.BaseRadius, mTarget))
            {
                lIsDirty = true;
                mTarget.BaseRadius = EditorHelper.FieldFloatValue;
            }

            UnityEngine.GUILayout.BeginHorizontal();

            UnityEditor.EditorGUILayout.LabelField(new UnityEngine.GUIContent("Grounding Ray", "Ray start height and distance"), UnityEngine.GUILayout.Width(UnityEditor.EditorGUIUtility.labelWidth - 4f));

            if (EditorHelper.FloatField(mTarget.GroundingStartOffset, "Ray Start", mTarget, 0f, 20f))
            {
                lIsDirty = true;
                mTarget.GroundingStartOffset = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField(mTarget.GroundingDistance, "Ray Distance", mTarget, 0f, 20f))
            {
                lIsDirty = true;
                mTarget.GroundingDistance = EditorHelper.FieldFloatValue;
            }

            UnityEngine.GUILayout.EndHorizontal();

            GUILayout.Space(5f);

            UnityEngine.GUILayout.BeginHorizontal();

            UnityEditor.EditorGUILayout.LabelField(new UnityEngine.GUIContent("Force Grounding", "Determines if we force the actor to the ground and how far away the character can be before we force them."), UnityEngine.GUILayout.Width(UnityEditor.EditorGUIUtility.labelWidth - 4f));

            if (EditorHelper.BoolField(mTarget.ForceGrounding, "Force Grounding", mTarget, 16f))
            {
                lIsDirty = true;
                mTarget.ForceGrounding = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.FloatField(mTarget._ForceGroundingDistance, "Force Distance", mTarget, 0f, 20f))
            {
                lIsDirty = true;
                mTarget.ForceGroundingDistance = EditorHelper.FieldFloatValue;
            }

            UnityEngine.GUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            EditorGUILayout.LabelField("Collisions", EditorStyles.boldLabel, GUILayout.Height(16f));

            EditorGUILayout.BeginVertical(EditorHelper.GroupBox);

            EditorHelper.DrawInspectorDescription("Properties determine what we'll collide with and how to test for collisions.", MessageType.None);

            EditorGUILayout.BeginVertical(EditorHelper.Box);

            // Is collision enabled
            if (EditorHelper.BoolField("Is Collision Enabled", "Determines if we process collisions. This doesn't effect grounding.", mTarget.IsCollsionEnabled, mTarget))
            {
                lIsDirty = true;
                mTarget.IsCollsionEnabled = EditorHelper.FieldBoolValue;
                mTarget.EditorCollideWithObjects = EditorHelper.FieldBoolValue;
            }

            if (mTarget.IsCollsionEnabled)
            {
                if (EditorHelper.BoolField("Stop Rotations", "Determines stop rotating when a collision occurs due to rotation.", mTarget.StopOnRotationCollision, mTarget))
                {
                    lIsDirty = true;
                    mTarget.StopOnRotationCollision = EditorHelper.FieldBoolValue;
                }

                if (EditorHelper.BoolField("Allow Pushback", "Determines if objects can move the actor.", mTarget.AllowPushback, mTarget))
                {
                    lIsDirty = true;
                    mTarget.AllowPushback = EditorHelper.FieldBoolValue;
                    mTarget.EditorRespondToColliders = EditorHelper.FieldBoolValue;
                }

                GUILayout.Space(5);

                if (EditorHelper.Vector3Field("Overlap Center", "Relative center of the object where overlap is tested from. Not required, but used by some custom drivers.", mTarget.OverlapCenter, mTarget))
                {
                    lIsDirty = true;
                    mTarget.OverlapCenter = EditorHelper.FieldVector3Value;
                }

                if (EditorHelper.FloatField("Overlap Radius", "Radius used to determine the overlap of other objects. Not required, but used by some custom drivers.", mTarget.OverlapRadius, mTarget))
                {
                    lIsDirty = true;
                    mTarget.OverlapRadius = EditorHelper.FieldFloatValue;
                }

                GUILayout.Space(5);

                // Collisions layer
                int lNewCollisionLayers = EditorHelper.LayerMaskField(new GUIContent("Collision Layers", "Layers that we'll test collisions against"), mTarget.CollisionLayers);
                if (lNewCollisionLayers != mTarget.CollisionLayers)
                {
                    lIsDirty = true;
                    mTarget.CollisionLayers = lNewCollisionLayers;
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            EditorGUILayout.LabelField("Steps", EditorStyles.boldLabel, GUILayout.Height(16f));

            EditorGUILayout.BeginVertical(EditorHelper.GroupBox);

            EditorHelper.DrawInspectorDescription("Properties that define how we'll handle steps. Smooth stepping allows us to hide popping by smoothly moving up and down step heights.", MessageType.None);

            EditorGUILayout.BeginVertical(EditorHelper.Box);

            // Stepping
            if (EditorHelper.FloatField("Step Height", "Max height that we can simply move onto without stopping. When grounded, all collisions under this height are ignored.", mTarget.MaxStepHeight, mTarget))
            {
                lIsDirty = true;
                mTarget.MaxStepHeight = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField("Step Up Speed", "Factor of lateral speed we'll use to move up steps. Set to 0 to disable smooth stepping up.", mTarget.StepUpSpeed, mTarget))
            {
                lIsDirty = true;
                mTarget.StepUpSpeed = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField("Step Down Speed", "Factor of lateral speed we'll use to move down steps. Set to 0 to disable smooth stepping down.", mTarget.StepDownSpeed, mTarget))
            {
                lIsDirty = true;
                mTarget.StepDownSpeed = EditorHelper.FieldFloatValue;
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();

            GUILayout.Space(5);

            EditorGUILayout.LabelField("Slopes", EditorStyles.boldLabel, GUILayout.Height(16f));

            EditorGUILayout.BeginVertical(EditorHelper.GroupBox);

            EditorHelper.DrawInspectorDescription("Properties that define how we'll handle ramps and slopes. Including what is traversable.", MessageType.None);

            EditorGUILayout.BeginVertical(EditorHelper.Box);

            if (EditorHelper.FloatField("Max Slope", "Max angle which the actor can no longer go up.", mTarget.MaxSlopeAngle, mTarget))
            {
                lIsDirty = true;
                mTarget.MaxSlopeAngle = EditorHelper.FieldFloatValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Is Sliding Enabled", "Determines if actors will slide while on slopes greater than 'Min Slope'.", mTarget.IsSlidingEnabled, mTarget))
            {
                lIsDirty = true;
                mTarget.IsSlidingEnabled = EditorHelper.FieldBoolValue;
                mTarget.EditorSlideOnSlopes = EditorHelper.FieldBoolValue;
            }

            if (mTarget.IsSlidingEnabled)
            {
                if (EditorHelper.FloatField("Min Slope", "Angle at which the actor will start to slide.", mTarget.MinSlopeAngle, mTarget))
                {
                    lIsDirty = true;
                    mTarget.MinSlopeAngle = EditorHelper.FieldFloatValue;
                }

                if (EditorHelper.FloatField("Gravity Factor", "Multiplier to gravity that helps determine the slide speed.", mTarget.MinSlopeGravityCoefficient, mTarget))
                {
                    lIsDirty = true;
                    mTarget.MinSlopeGravityCoefficient = EditorHelper.FieldFloatValue;
                }
            }

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();

            GUILayout.Space(5);
        }

        // Orient to ground
        EditorGUILayout.LabelField("Tilting", EditorStyles.boldLabel, GUILayout.Height(16f));

        EditorGUILayout.BeginVertical(EditorHelper.GroupBox);

        EditorHelper.DrawInspectorDescription("Properties that define if and how the actor orients himself to the ground. ", MessageType.None);

        EditorGUILayout.BeginVertical(EditorHelper.Box);

        if (EditorHelper.BoolField("Orient to Ground", "Determines if we'll rotate the actor to match the ground he's on.", mTarget.OrientToGround, mTarget))
        {
            lIsDirty = true;
            mTarget.OrientToGround = EditorHelper.FieldBoolValue;
            mTarget.EditorWalkOnWalls = EditorHelper.FieldBoolValue && mTarget.IsGravityRelative;
        }

        if (mTarget.OrientToGround)
        {
            if (EditorHelper.BoolField("Keep Orientation", "Determines if keep the last orientation while jumping (ie vertical force).", mTarget.KeepOrientationInAir, mTarget))
            {
                lIsDirty = true;
                mTarget.KeepOrientationInAir = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.FloatField("Min Angle", "Minimum angle before the orientation time kicks in. Less than this and the rotation is instant.", mTarget.MinOrientToGroundAngleForSpeed, mTarget))
            {
                lIsDirty = true;
                mTarget.MinOrientToGroundAngleForSpeed = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField("Max Distance", "Distance from the ground before falling reverts to the natural ground (Vector3.up).", mTarget.OrientToGroundDistance, mTarget))
            {
                lIsDirty = true;
                mTarget.OrientToGroundDistance = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField("Time", "Time (in seconds) to orient to the new ground normal.", mTarget.OrientToGroundSpeed, mTarget))
            {
                lIsDirty = true;
                mTarget.OrientToGroundSpeed = EditorHelper.FieldFloatValue;
            }
        }

        EditorGUILayout.EndVertical();

        EditorGUILayout.EndVertical();

        if (!mTarget.UseTransformPosition)
        {

            GUILayout.Space(5);

            EditorGUILayout.LabelField("Limits", EditorStyles.boldLabel, GUILayout.Height(16f));

            EditorGUILayout.BeginVertical(EditorHelper.GroupBox);

            EditorHelper.DrawInspectorDescription("Locks movement and rotation along specific axes.", MessageType.None);

            EditorGUILayout.BeginVertical(EditorHelper.Box);

            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(new GUIContent("Freeze Position", "Prevents movement on the specified axis."), GUILayout.Width(EditorGUIUtility.labelWidth));

            EditorGUILayout.LabelField(new GUIContent("x"), GUILayout.Width(10));
            bool lNewFreezePositionX = EditorGUILayout.Toggle(mTarget.FreezePositionX, GUILayout.Width(10));
            if (lNewFreezePositionX != mTarget.FreezePositionX)
            {
                lIsDirty = true;
                mTarget.FreezePositionX = lNewFreezePositionX;
            }

            GUILayout.Space(5);
            EditorGUILayout.LabelField(new GUIContent("y"), GUILayout.Width(10));
            bool lNewFreezePositionY = EditorGUILayout.Toggle(mTarget.FreezePositionY, GUILayout.Width(10));
            if (lNewFreezePositionY != mTarget.FreezePositionY)
            {
                lIsDirty = true;
                mTarget.FreezePositionY = lNewFreezePositionY;
            }

            GUILayout.Space(5);
            EditorGUILayout.LabelField(new GUIContent("z"), GUILayout.Width(10));
            bool lNewFreezePositionZ = EditorGUILayout.Toggle(mTarget.FreezePositionZ, GUILayout.Width(10));
            if (lNewFreezePositionZ != mTarget.FreezePositionZ)
            {
                lIsDirty = true;
                mTarget.FreezePositionZ = lNewFreezePositionZ;
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();


            EditorGUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(new GUIContent("Freeze Rotation", "Prevents rotation on the specified axis."), GUILayout.Width(EditorGUIUtility.labelWidth));

            EditorGUILayout.LabelField(new GUIContent("x"), GUILayout.Width(10));
            bool lNewFreezeRotationX = EditorGUILayout.Toggle(mTarget.FreezeRotationX, GUILayout.Width(10));
            if (lNewFreezeRotationX != mTarget.FreezeRotationX)
            {
                lIsDirty = true;
                mTarget.FreezeRotationX = lNewFreezeRotationX;
            }

            GUILayout.Space(5);
            EditorGUILayout.LabelField(new GUIContent("y"), GUILayout.Width(10));
            bool lNewFreezeRotationY = EditorGUILayout.Toggle(mTarget.FreezeRotationY, GUILayout.Width(10));
            if (lNewFreezeRotationY != mTarget.FreezeRotationY)
            {
                lIsDirty = true;
                mTarget.FreezeRotationY = lNewFreezeRotationY;
            }

            GUILayout.Space(5);
            EditorGUILayout.LabelField(new GUIContent("z"), GUILayout.Width(10));
            bool lNewFreezeRotationZ = EditorGUILayout.Toggle(mTarget.FreezeRotationZ, GUILayout.Width(10));
            if (lNewFreezeRotationZ != mTarget.FreezeRotationZ)
            {
                lIsDirty = true;
                mTarget.FreezeRotationZ = lNewFreezeRotationZ;
            }

            GUILayout.FlexibleSpace();

            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();

            EditorGUILayout.EndVertical();
        }

        return lIsDirty;
    }

    /// <summary>
    /// Create the reorderable list
    /// </summary>
    private void InstanciateList()
    {
        mList = new ReorderableList(mTarget.BodyShapes, typeof(BodyShape), true, true, false, false);
        mList.drawHeaderCallback = DrawListHeader;
        mList.drawFooterCallback = DrawListFooter;
        mList.drawElementCallback = DrawListElement;
        mList.onAddCallback = OnListItemAdd;
        mList.onSelectCallback = OnListItemSelect;
        mList.onRemoveCallback = OnListItemRemove;
        mList.footerHeight = 17f;

        if (mTarget.EditorBodyShapeIndex >= mList.count) { mTarget.EditorBodyShapeIndex = mList.count - 1; }
        //if (mTarget.EditorBodyShapeIndex >= 0) { mList.index = mTarget.EditorBodyShapeIndex; }

        if (mTarget.EditorBodyShapeIndex < mList.count)
        {
            mList.index = mTarget.EditorBodyShapeIndex;
            OnListItemSelect(mList);
        }
    }

    /// <summary>
    /// Header for the list
    /// </summary>
    /// <param name="rRect"></param>
    private void DrawListHeader(Rect rRect)
    {
        EditorGUI.LabelField(rRect, "Body Shapes");

        Rect lTitleRect = new Rect(rRect.x, rRect.y + 1, rRect.width - 80, 14);
        if (GUI.Button(lTitleRect, "", EditorStyles.label))
        {
            mList.index = -1;
            OnListItemSelect(mList);
        }

        Rect lAutoRect = new Rect(rRect.width - 54, rRect.y + 1, 80, 14);
        if (GUI.Button(lAutoRect, new GUIContent("Auto Create", "Automatically create body shapes for humanoids"), EditorHelper.TinyButton))
        {
            bool lContinue = true;
            if (mTarget.BodyShapes.Count > 0)
            {
                lContinue = EditorUtility.DisplayDialog("Warning!", "Delete existing shapes and create new ones?", "Yes", "No");
            }

            if (lContinue)
            {
                CreateDefaultShapes();
            }
        }
    }

    /// <summary>
    /// Allows us to draw each item in the list
    /// </summary>
    /// <param name="rRect"></param>
    /// <param name="rIndex"></param>
    /// <param name="rIsActive"></param>
    /// <param name="rIsFocused"></param>
    private void DrawListElement(Rect rRect, int rIndex, bool rIsActive, bool rIsFocused)
    {
        if (rIndex < mTarget.BodyShapes.Count)
        {
            string lName = mTarget.BodyShapes[rIndex] == null ? "null" : mTarget.BodyShapes[rIndex].Name;

            rRect.y += 2;
            EditorGUI.LabelField(new Rect(rRect.x, rRect.y, rRect.width, EditorGUIUtility.singleLineHeight), lName);
        }
    }

    /// <summary>
    /// Footer for the list
    /// </summary>
    /// <param name="rRect"></param>
    private void DrawListFooter(Rect rRect)
    {
        Rect lShapeRect = new Rect(rRect.x, rRect.y + 1, rRect.width - 4 - 28 - 28, 16);
        mBodyShapeIndex = EditorGUI.Popup(lShapeRect, mBodyShapeIndex, mBodyShapeNames.ToArray());

        Rect lAddRect = new Rect(lShapeRect.x + lShapeRect.width + 4, lShapeRect.y, 28, 15);
        if (GUI.Button(lAddRect, new GUIContent("+", "Add Shape."), EditorStyles.miniButtonLeft)) { OnListItemAdd(mList); }

        Rect lDeleteRect = new Rect(lAddRect.x + lAddRect.width, lAddRect.y, 28, 15);
        if (GUI.Button(lDeleteRect, new GUIContent("-", "Delete Shape."), EditorStyles.miniButtonRight)) { OnListItemRemove(mList); };
    }

    /// <summary>
    /// Allows us process when a list is selected
    /// </summary>
    /// <param name="rList"></param>
    private void OnListItemSelect(ReorderableList rList)
    {
        mTarget.EditorBodyShapeIndex = rList.index;
    }

    /// <summary>
    /// Allows us to add to a list
    /// </summary>
    /// <param name="rList"></param>
    private void OnListItemAdd(ReorderableList rList)
    {
        if (mBodyShapeIndex >= mBodyShapeTypes.Count) { return; }

        BodyShape lShape = Activator.CreateInstance(mBodyShapeTypes[mBodyShapeIndex]) as BodyShape;
        lShape.Name = mBodyShapeNames[mBodyShapeIndex];
        lShape._Parent = mTarget.transform;
        if (lShape._UseUnityColliders) { lShape.CreateUnityColliders(); }
        mTarget.BodyShapes.Add(lShape);

        mList.index = mTarget.BodyShapes.Count - 1;
        mTarget.EditorBodyShapeIndex = rList.index;

        mIsDirty = true;
        mTarget.SerializeBodyShapes();
    }

    /// <summary>
    /// Allows us to stop before removing the item
    /// </summary>
    /// <param name="rList"></param>
    private void OnListItemRemove(ReorderableList rList)
    {
        if (EditorUtility.DisplayDialog("Warning!", "Are you sure you want to delete the item?", "Yes", "No"))
        {
            int lIndex = rList.index;

            BodyShape lShape = mTarget.BodyShapes[lIndex];
            lShape.DestroyUnityColliders();

            mTarget.BodyShapes.RemoveAt(lIndex);

            rList.index--;
            if (mTarget.EditorBodyShapeIndex >= rList.count) { mTarget.EditorBodyShapeIndex = rList.count - 1; }

            mIsDirty = true;
            mTarget.SerializeBodyShapes();
        }
    }

    /// <summary>
    /// Renders the currently selected step
    /// </summary>
    /// <param name="rStep"></param>
    private bool DrawDetailItem(BodyShape rShape)
    {
        if (rShape == null) { return false; }

        bool lIsDirty = false;

        bool lIsShapeDirty = rShape.OnInspectorGUI(mTarget);
        if (lIsShapeDirty)
        {
            lIsDirty = true;
            mTarget.SerializeBodyShapes();
        }

        return lIsDirty;
    }

    /// <summary>
    /// Allow the actor controller to render to the editor
    /// </summary>
    private void OnSceneGUI()
    {
        if (mTarget != null)
        {
            mTarget.OnSceneGUI();
        }
    }

    /// <summary>
    /// Initializes the shapes
    /// </summary>
    private void CreateDefaultShapes()
    {
        mTarget.BodyShapes.Clear();

        BodyCapsule lCapsule = new BodyCapsule();
        lCapsule._Parent = mTarget.transform;
        lCapsule.Name = "Body Capsule";
        lCapsule.Radius = 0.4f;
        lCapsule.Offset = new Vector3(0f, 0.8f, 0f);
        lCapsule.IsEnabledOnGround = true;
        lCapsule.IsEnabledOnSlope = true;
        lCapsule.IsEnabledAboveGround = true;

        lCapsule.EndTransform = mTarget.transform.FindTransform(HumanBodyBones.Head);
        if (lCapsule.EndTransform == null) { lCapsule.EndTransform = mTarget.transform.FindTransform("Head"); }
        if (lCapsule.EndTransform == null) { lCapsule.EndOffset = new Vector3(0f, 1.6f, 0f); }

        mTarget.BodyShapes.Add(lCapsule);

        BodySphere lSphere = new BodySphere();
        lSphere._Parent = mTarget.transform;
        lSphere.Name = "Foot Sphere";
        lSphere.Radius = 0.25f;
        lSphere.Offset = new Vector3(0f, 0.25f, 0f);
        lSphere.IsEnabledOnGround = false;
        lSphere.IsEnabledOnSlope = false;
        lSphere.IsEnabledAboveGround = true;

        mTarget.BodyShapes.Add(lSphere);

        // Store the current body shapes
        mTarget.SerializeBodyShapes();
    }

    /// <summary>
    /// Sets properties that enables the functionality
    /// </summary>
    /// <param name="rEnable"></param>
    private void EnableCollisions(bool rEnable)
    {
        mTarget.IsCollsionEnabled = rEnable;
    }

    /// <summary>
    /// Sets properties that enables the functionality
    /// </summary>
    /// <param name="rEnable"></param>
    private void EnableWallWalking(bool rEnable)
    {
        mTarget.IsGravityEnabled = (rEnable ? true : mTarget.IsGravityEnabled);
        mTarget.IsGravityRelative = rEnable;
        mTarget.OrientToGround = rEnable;
    }

    /// <summary>
    /// Sets properties that enables the functionality
    /// </summary>
    /// <param name="rEnable"></param>
    private void EnableSlopeSliding(bool rEnable)
    {
        mTarget.IsSlidingEnabled = rEnable;
    }

    /// <summary>
    /// Sets properties that enables the functionality
    /// </summary>
    /// <param name="rEnable"></param>
    private void EnableCollisionResponse(bool rEnable)
    {
        mTarget.AllowPushback = rEnable;
    }

    /// <summary>
    /// Test if the grounding radius is valid
    /// </summary>
    /// <returns></returns>
    private bool TestGroundingRadius()
    {
        for (int i = 0; i < mTarget.BodyShapes.Count; i++)
        {
            BodyShape lShape = mTarget.BodyShapes[i];
            if (lShape.Radius <= mTarget.BaseRadius)
            {
                if (lShape.IsEnabledOnGround)
                {
                    return false;
                }
            }
        }

        return true;
    }

    /// <summary>
    /// Box used to group standard GUI elements
    /// </summary>
    private static GUIStyle mBasicIcon = null;
    private static GUIStyle BasicIcon
    {
        get
        {
            if (mBasicIcon == null)
            {
                Texture2D lTexture = Resources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "BasicIcon_pro" : "BasicIcon");

                mBasicIcon = new GUIStyle(GUI.skin.button);
                mBasicIcon.normal.background = lTexture;
                mBasicIcon.padding = new RectOffset(0, 0, 0, 0);
                mBasicIcon.margin = new RectOffset(0, 0, 1, 0);
                mBasicIcon.border = new RectOffset(0, 0, 0, 0);
                mBasicIcon.stretchHeight = false;
                mBasicIcon.stretchWidth = false;

            }

            return mBasicIcon;
        }
    }

    /// <summary>
    /// Box used to group standard GUI elements
    /// </summary>
    private static GUIStyle mAdvancedIcon = null;
    private static GUIStyle AdvancedIcon
    {
        get
        {
            if (mAdvancedIcon == null)
            {
                Texture2D lTexture = Resources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "AdvancedIcon_pro" : "AdvancedIcon");

                mAdvancedIcon = new GUIStyle(GUI.skin.button);
                mAdvancedIcon.normal.background = lTexture;
                mAdvancedIcon.padding = new RectOffset(0, 0, 0, 0);
                mAdvancedIcon.margin = new RectOffset(0, 0, 1, 0);
                mAdvancedIcon.border = new RectOffset(0, 0, 0, 0);
                mAdvancedIcon.stretchHeight = false;
                mAdvancedIcon.stretchWidth = false;

            }

            return mAdvancedIcon;
        }
    }

    /// <summary>
    /// Box used to group standard GUI elements
    /// </summary>
    private static GUIStyle mCapsuleIcon = null;
    private static GUIStyle CapsuleIcon
    {
        get
        {
            if (mCapsuleIcon == null)
            {
                Texture2D lTexture = Resources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "CapsuleIcon" : "CapsuleIcon");

                mCapsuleIcon = new GUIStyle(GUI.skin.box);
                mCapsuleIcon.normal.background = lTexture;
                mCapsuleIcon.padding = new RectOffset(0, 0, 0, 0);
                mCapsuleIcon.border = new RectOffset(0, 0, 0, 0);
            }

            return mCapsuleIcon;
        }
    }

    /// <summary>
    /// Box used to group standard GUI elements
    /// </summary>
    private static GUIStyle mSpiderIcon = null;
    private static GUIStyle SpiderIcon
    {
        get
        {
            if (mSpiderIcon == null)
            {
                Texture2D lTexture = Resources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "SpiderIcon" : "SpiderIcon");

                mSpiderIcon = new GUIStyle(GUI.skin.box);
                mSpiderIcon.normal.background = lTexture;
                mSpiderIcon.padding = new RectOffset(0, 0, 0, 0);
                mSpiderIcon.border = new RectOffset(0, 0, 0, 0);
            }

            return mSpiderIcon;
        }
    }

    /// <summary>
    /// Box used to group standard GUI elements
    /// </summary>
    private static GUIStyle mSlopeIcon = null;
    private static GUIStyle SlopeIcon
    {
        get
        {
            if (mSlopeIcon == null)
            {
                Texture2D lTexture = Resources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "SlopeIcon" : "SlopeIcon");

                mSlopeIcon = new GUIStyle(GUI.skin.box);
                mSlopeIcon.normal.background = lTexture;
                mSlopeIcon.padding = new RectOffset(0, 0, 0, 0);
                mSlopeIcon.border = new RectOffset(0, 0, 0, 0);
            }

            return mSlopeIcon;
        }
    }

    /// <summary>
    /// Box used to group standard GUI elements
    /// </summary>
    private static GUIStyle mResponseIcon = null;
    private static GUIStyle ResponseIcon
    {
        get
        {
            if (mResponseIcon == null)
            {
                Texture2D lTexture = Resources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "ResponseIcon" : "ResponseIcon");

                mResponseIcon = new GUIStyle(GUI.skin.box);
                mResponseIcon.normal.background = lTexture;
                mResponseIcon.padding = new RectOffset(0, 0, 0, 0);
                mResponseIcon.border = new RectOffset(0, 0, 0, 0);
            }

            return mResponseIcon;
        }
    }

    /// <summary>
    /// Label
    /// </summary>
    private static GUIStyle mOptionText = null;
    private static GUIStyle OptionText
    {
        get
        {
            if (mOptionText == null)
            {
                mOptionText = new GUIStyle(GUI.skin.label);
                mOptionText.wordWrap = true;
                mOptionText.padding.top = 11;
            }

            return mOptionText;
        }
    }

    /// <summary>
    /// Label
    /// </summary>
    private static GUIStyle mOptionToggle = null;
    private static GUIStyle OptionToggle
    {
        get
        {
            if (mOptionToggle == null)
            {
                mOptionToggle = new GUIStyle(GUI.skin.toggle);
            }

            return mOptionToggle;
        }
    }

    /// <summary>
    /// Box used to group standard GUI elements
    /// </summary>
    private static GUIStyle mBox = null;
    private static GUIStyle Box
    {
        get
        {
            if (mBox == null)
            {
                Texture2D lTexture = Resources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "Editor/GroupBox_pro" : "Editor/OrangeGrayBox");

                mBox = new GUIStyle(GUI.skin.box);
                mBox.normal.background = lTexture;
                mBox.padding = new RectOffset(0, 0, 0, 0);
                mBox.margin = new RectOffset(0, 0, 0, 0);
            }

            return mBox;
        }
    }

    private static GUIStyle mWarningBox = null;
    private static GUIStyle WarningBox
    {
        get
        {
            if (mWarningBox == null)
            {
                Texture2D lTexture = EditorHelper.CreateTexture(16, 16, Color.white, Color.red);

                mWarningBox = new GUIStyle(GUI.skin.box);
                mWarningBox.normal.background = lTexture;
                mWarningBox.padding = new RectOffset(0, 0, 0, 0);
                mWarningBox.margin = new RectOffset(0, 0, 0, 0);
            }

            return mWarningBox;
        }
    }
}

