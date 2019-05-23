using System;
using UnityEngine;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Cameras;
using com.ootii.Helpers;
using com.ootii.Geometry;
using com.ootii.Timing;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.MotionControllerPacks
{
    /// <summary>
    /// Very basic idle when swimming.
    /// </summary>
    [MotionName("Swim - Exit")]
    [MotionDescription("Swimming motion that has the actor climb out of the edge.")]
    public class Swim_Exit : MotionControllerMotion
    {
        // Enum values for the motion
        public const int PHASE_UNKNOWN = 0;
        public const int PHASE_START = 31600;
        public const int PHASE_STOP = 31601;

        /// <summary>
        /// Scale used to adjust for different sized characters. Relative to a
        /// 1.8m tall human.
        /// </summary>
        public float _BodyScale = 1f;
        public float BodyScale
        {
            get { return _BodyScale; }
            set { _BodyScale = value; }
        }

        /// <summary>
        /// Radius to use for the character
        /// </summary>
        public float _BodyRadius = 0.25f;
        public float BodyRadius
        {
            get { return _BodyRadius; }
            set { _BodyRadius = value; }
        }

        /// <summary>
        /// Min horizontal distance the actor can be from the ladder in order to climb
        /// </summary>
        public float _MinDistance = 0.2f;
        public float MinDistance
        {
            get { return _MinDistance; }
            set { _MinDistance = value; }
        }

        /// <summary>
        /// Max horizontal distance the actor can be from the ladder in order to climb
        /// </summary>
        public float _MaxDistance = 0.6f;
        public float MaxDistance
        {
            get { return _MaxDistance; }
            set { _MaxDistance = value; }
        }

        /// <summary>
        /// Min height of the object that can be climbed.
        /// </summary>
        public float _MinHeight = 1f;
        public float MinHeight
        {
            get { return _MinHeight; }
            set { _MinHeight = value; }
        }

        /// <summary>
        /// Max height of the object that can be climbed.
        /// </summary>
        public float _MaxHeight = 1.6f;
        public float MaxHeight
        {
            get { return _MaxHeight; }
            set { _MaxHeight = value; }
        }

        /// <summary>
        /// Max depth you can be to grab a ledge
        /// </summary>
        public float _MaxDepth = 1.5f;
        public float MaxDepth
        {
            get { return _MaxDepth; }
            set { _MaxDepth = value; }
        }

        /// <summary>
        /// User layer id set for objects that are climbable.
        /// </summary>
        public int _ClimbableLayers = 1;
        public int ClimbableLayers
        {
            get { return _ClimbableLayers; }
            set { _ClimbableLayers = value; }
        }

#if UNITY_EDITOR

        /// <summary>
        /// Used to render debug info to the screen
        /// </summary>
        public bool _IsDebugEnabled = false;
        public bool IsDebugEnabled
        {
            get { return _IsDebugEnabled; }
            set { _IsDebugEnabled = value; }
        }

#endif

        /// <summary>
        /// Contains information about the swimmer
        /// </summary>
        protected SwimmerInfo mSwimmerInfo = null;

        /// <summary>
        /// Keeps us from having to reallocate over and over
        /// </summary>
        protected RaycastHit mRaycastHitInfo = RaycastExt.EmptyHitInfo;

        /// <summary>
        /// Swim motion we'll get running state from
        /// </summary>
        protected Swim_Strafe mSwimStrafe = null;

        /// <summary>
        /// Determines if the swimmer is moving fast
        /// </summary>
        protected bool mIsRunning = false;

        /// <summary>
        /// Determines if we're testing the follow-up frame
        /// </summary>
        protected bool mIsSecondPass = false;

        /// <summary>
        /// Track the last platform position so we can determine how it's moving
        /// </summary>
        protected Vector3 mLastPlatformPosition = Vector3.zero;

        /// <summary>
        /// Default constructor
        /// </summary>
        public Swim_Exit()
            : base()
        {
            _Pack = Swim_Idle.GroupName();
            _Category = EnumMotionCategories.WALK;

            _Priority = 25;
            _ActionAlias = "Jump";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "Swim_Exit-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public Swim_Exit(MotionController rController)
            : base(rController)
        {
            _Pack = Swim_Idle.GroupName();
            _Category = EnumMotionCategories.WALK;

            _Priority = 25;
            _ActionAlias = "Jump";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "Swim_Exit-SM"; }
#endif
        }

        /// <summary>
        /// Allows for any processing after the motion has been deserialized
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            mSwimStrafe = mMotionController.GetMotion<Swim_Strafe>(mMotionLayer._AnimatorLayerIndex);
        }

        /// <summary>
        /// Tests if this motion should be started. However, the motion
        /// isn't actually started.
        /// </summary>
        /// <returns></returns>
        public override bool TestActivate()
        {
            if (!mIsStartable) { return false; }

            // Grab the swimmer info if it doesn't exist
            if (mSwimmerInfo == null) { mSwimmerInfo = SwimmerInfo.GetSwimmerInfo(mMotionController._Transform); }

            // This is only valid if we're in the right stance
            if (mActorController.State.Stance != EnumControllerStance.SWIMMING) { return false; }

            // Check that we're actually moving fast
            mIsRunning = (mSwimStrafe != null && mSwimStrafe.IsActive && mSwimStrafe.IsRunning);

            // If we passed last frame, ensure the platform isn't moving down
            if (mIsSecondPass)
            {
                mIsSecondPass = false;

                // Ensure there is STILL an edge without the bounds
                bool lIsFound = RaycastExt.GetForwardEdge2(mMotionController._Transform, _MinHeight * _BodyScale, _MaxHeight * _BodyScale, 0.1f * _BodyScale, (_BodyRadius + _MaxDistance) * (mIsRunning ? 2f : 1f) * _BodyScale, _ClimbableLayers, out mRaycastHitInfo);
                if (lIsFound && mRaycastHitInfo.collider.gameObject.transform.position.y >= mLastPlatformPosition.y)
                {
                    return true;
                }
            }
            else
            {
                // Check if the user initiated exiting
                if (mMotionController._InputSource != null && mMotionController._InputSource.IsJustPressed(ActionAlias))
                {
                    // We can't exit if we're too deep
                    if (_MaxDepth > 0f && mSwimmerInfo.GetDepth() < _MaxDepth * _BodyScale)
                    {
                        // Ensure there is an edge without the bounds
                        bool lIsFound = RaycastExt.GetForwardEdge2(mMotionController._Transform, _MinHeight * _BodyScale, _MaxHeight * _BodyScale, 0.1f * _BodyScale, (_BodyRadius + _MaxDistance) * (mIsRunning ? 2f : 1f) * _BodyScale, _ClimbableLayers, out mRaycastHitInfo);
                        if (lIsFound)
                        {
                            mIsSecondPass = true;
                            mLastPlatformPosition = mRaycastHitInfo.collider.gameObject.transform.position;
                        }
                    }
                }
            }

#if UNITY_EDITOR
            if (_IsDebugEnabled)
            {
                Vector3 lStart = mMotionController._Transform.position + (mMotionController._Transform.up * _MinHeight * _BodyScale) + (mMotionController._Transform.forward * _MinDistance * _BodyScale);
                Vector3 lEnd = mMotionController._Transform.position + (mMotionController._Transform.up * _MinHeight * _BodyScale) + (mMotionController._Transform.forward * _MaxDistance * (mIsRunning ? 2f : 1f) * _BodyScale);
                com.ootii.Graphics.GraphicsManager.DrawLine(lStart, lEnd, Color.blue);

                lStart = mMotionController._Transform.position + (mMotionController._Transform.up * _MaxHeight * _BodyScale) + (mMotionController._Transform.forward * _MinDistance * _BodyScale);
                lEnd = mMotionController._Transform.position + (mMotionController._Transform.up * _MaxHeight * _BodyScale) + (mMotionController._Transform.forward * _MaxDistance * (mIsRunning ? 2f : 1f) * _BodyScale);
                com.ootii.Graphics.GraphicsManager.DrawLine(lStart, lEnd, Color.blue);
            }
#endif

            // Return the final result
            return false;
        }

        /// <summary>
        /// Tests if the motion should continue. If it shouldn't, the motion
        /// is typically disabled
        /// </summary>
        /// <returns>Boolean that determines if the motion continues</returns>
        public override bool TestUpdate()
        {
            // Ensure we're in the animation
            if (mIsAnimatorActive)
            {
                // Ensure we're in a valid animation
                if (!IsInMotionState)
                {
                    return false;
                }
            }

            // If we're surfacing, exit to the idle pose
            if (mMotionLayer._AnimatorStateID == STATE_IdlePose || mMotionLayer._AnimatorStateID == STATE_TreadIdleExitPose)
            {
                mSwimmerInfo.ExitWater();
                return false;
            }

            return true;
        }

        /// <summary>
        /// Raised when a motion is being interrupted by another motion
        /// </summary>
        /// <param name="rMotion">Motion doing the interruption</param>
        /// <returns>Boolean determining if it can be interrupted</returns>
        public override bool TestInterruption(MotionControllerMotion rMotion)
        {
            return true;
        }

        /// <summary>
        /// Called to start the specific motion. If the motion
        /// were something like 'jump', this would start the jumping process
        /// </summary>
        /// <param name="rPrevMotion">Motion that this motion is taking over from</param>
        public override bool Activate(MotionControllerMotion rPrevMotion)
        {
            // Force the stance
            mSwimmerInfo.EnterWater();

            // Disable actor controller processing for a short time
            //mActorController.IsGravityEnabled = false;
            //mActorController.FixGroundPenetration = false;
            mActorController.SetGround(mRaycastHitInfo.collider.gameObject.transform);

            // Ensure we don't collide with the object we're climbing
            mActorController.IgnoreCollision(mRaycastHitInfo.collider, true);

            // Tell the animator to start your animations
            mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, PHASE_START, (mIsRunning ? 1 : 0), true);

            // Return
            return base.Activate(rPrevMotion);
        }

        /// <summary>
        /// Called to stop the motion. If the motion is stopable. Some motions
        /// like jump cannot be stopped early
        /// </summary>
        public override void Deactivate()
        {
            base.Deactivate();
        }

        /// <summary>
        /// Allows the motion to modify the root-motion velocities before they are applied. 
        /// 
        /// NOTE:
        /// Be careful when removing rotations as some transitions will want rotations even 
        /// if the state they are transitioning from don't.
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        /// <param name="rVelocityDelta">Root-motion linear velocity relative to the actor's forward</param>
        /// <param name="rRotationDelta">Root-motion rotational velocity</param>
        /// <returns></returns>
        public override void UpdateRootMotion(float rDeltaTime, int rUpdateIndex, ref Vector3 rMovement, ref Quaternion rRotation)
        {
        }

        /// <summary>
        /// Updates the motion over time. This is called by the controller
        /// every update cycle so animations and stages can be updated.
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        public override void Update(float rDeltaTime, int rUpdateIndex)
        {
            // Compensate for a moving platform
            mRaycastHitInfo.point = mRaycastHitInfo.point + mActorController.State.MovementPlatformAdjust;

            // Now we can determine our positions
            Vector3 lRootPosition = Vector3.zero;
            Vector3 lTargetPosition = mRaycastHitInfo.point;

            // Adjust movement based on the state
            if (mMotionLayer._AnimatorStateID == STATE_SwimEdgeExit)
            {
                lRootPosition = mMotionController.Animator.GetBoneTransform(HumanBodyBones.LeftHand).position;
                lRootPosition = lRootPosition + (mMotionController._Transform.forward * (0.15f * _BodyScale));
                mMovement = GetReachMovement(lTargetPosition, lRootPosition, 0f, 0.5f, true, true, false, true, true);
            }
            else if (mMotionLayer._AnimatorStateID == STATE_TreadIdlePose)
            {
                lRootPosition = Vector3.Lerp(mMotionController.Animator.GetBoneTransform(HumanBodyBones.LeftHand).position, mMotionController.Animator.GetBoneTransform(HumanBodyBones.RightHand).position, 0.5f);
                lRootPosition = lRootPosition + (mMotionController._Transform.forward * (0.15f * _BodyScale));
                mMovement = GetReachMovement(lTargetPosition, lRootPosition, mMotionLayer._AnimatorTransitionNormalizedTime, true, false, true, true);
            }
            else if (mMotionLayer._AnimatorStateID == STATE_braced_hang_to_crouch)
            {
                lRootPosition = Vector3.Lerp(mMotionController.Animator.GetBoneTransform(HumanBodyBones.LeftHand).position, mMotionController.Animator.GetBoneTransform(HumanBodyBones.RightHand).position, 0.5f);
                lRootPosition = lRootPosition + (mMotionController._Transform.forward * (0.15f * _BodyScale));
                mMovement = GetReachMovement(lTargetPosition, lRootPosition, 0f, 0.43f, false, true, false, true, true);

                lRootPosition = mMotionController._Transform.position;
                mMovement = mMovement + GetReachMovement(lTargetPosition, lRootPosition, 0.7f, 1f, false, true, false, true, false);
            }
            else if (mMotionLayer._AnimatorStateID == STATE_crouched_to_standing)
            {
                lRootPosition = mMotionController._Transform.position;
                mMovement = mMovement + GetReachMovement(lTargetPosition, lRootPosition, 0.0f, 0.1f, true, true, false, true, false);
            }

            // Exit out of the swim and reset the properties
            if (mMotionLayer._AnimatorStateID == STATE_crouched_to_standing)
            {
                if (mSwimmerInfo != null && mSwimmerInfo.IsInWater)
                {
                    // Re-enable actor controller processing
                    //mActorController.IsGravityEnabled = true;
                    //mActorController.IsCollsionEnabled = true;
                    //mActorController.FixGroundPenetration = true;
                    mActorController.SetGround(null);

                    // Exit the water
                    mSwimmerInfo.ExitWater();
                    mActorController.IgnoreCollision(mRaycastHitInfo.collider, false);
                }
            }

#if UNITY_EDITOR
            if (_IsDebugEnabled)
            {
                com.ootii.Graphics.GraphicsManager.DrawPoint(lTargetPosition, Color.red);
                com.ootii.Graphics.GraphicsManager.DrawPoint(lRootPosition, Color.black);
            }
#endif
        }

        #region Editor Methods

        // **************************************************************************************************
        // Following properties and function only valid while editing
        // **************************************************************************************************

#if UNITY_EDITOR

        // Used to hide/show the offset section
        private bool mShowDimensions = false;

        /// <summary>
        /// Allow the constraint to render it's own GUI
        /// </summary>
        /// <returns>Reports if the object's value was changed</returns>
        public override bool OnInspectorGUI()
        {
            bool lIsDirty = false;

            float lLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100f;

            if (EditorHelper.TextField("Grab Alias", "Action alias that triggers a climb.", ActionAlias, mMotionController, 30))
            {
                lIsDirty = true;
                ActionAlias = EditorHelper.FieldStringValue;
            }

            // Balance layer
            int lNewClimbableLayers = EditorHelper.LayerMaskField(new GUIContent("Climbing Layers", "Layers that identies objects that can be climbed."), ClimbableLayers);
            if (lNewClimbableLayers != ClimbableLayers)
            {
                lIsDirty = true;
                ClimbableLayers = lNewClimbableLayers;
            }

            if (EditorHelper.FloatField("Body Scale", "Scale applied to climbing distances and body dimensions to account for a body larger or smaller than 1.8 meters.", BodyScale, mMotionController))
            {
                lIsDirty = true;
                BodyScale = EditorHelper.FieldFloatValue;
            }

            EditorGUI.indentLevel++;
            mShowDimensions = EditorGUILayout.Foldout(mShowDimensions, new GUIContent("Body dimensions"));
            EditorGUI.indentLevel--;

            if (mShowDimensions)
            {
                EditorGUILayout.HelpBox("The values below are for an average human that is 1.8m tall. These values will be scaled by the BodyScale property.", MessageType.Info);

                if (EditorHelper.FloatField("Body Radius", "Typical radius of the body for raycast tests.", BodyRadius, mMotionController))
                {
                    lIsDirty = true;
                    BodyRadius = EditorHelper.FieldFloatValue;
                }


                GUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(new GUIContent("Distance", "Distances that the climb is valid for"), GUILayout.Width(EditorGUIUtility.labelWidth));
                EditorGUILayout.LabelField("Min", GUILayout.Width(35f));
                if (EditorHelper.FloatField(MinDistance, "Min Distance", mMotionController, 40f))
                {
                    lIsDirty = true;
                    MinDistance = EditorHelper.FieldFloatValue;
                }

                GUILayout.Space(5f);

                EditorGUILayout.LabelField("Max", GUILayout.Width(27f));
                if (EditorHelper.FloatField(MaxDistance, "Max Distance", mMotionController, 40f))
                {
                    lIsDirty = true;
                    MaxDistance = EditorHelper.FieldFloatValue;
                }

                GUILayout.FlexibleSpace();

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(new GUIContent("Height", "Heights that the climb is valid for"), GUILayout.Width(EditorGUIUtility.labelWidth));
                EditorGUILayout.LabelField("Min", GUILayout.Width(35f));
                if (EditorHelper.FloatField(MinHeight, "Min Height", mMotionController, 40f))
                {
                    lIsDirty = true;
                    MinHeight = EditorHelper.FieldFloatValue;
                }

                GUILayout.Space(5f);

                EditorGUILayout.LabelField("Max", GUILayout.Width(27f));
                if (EditorHelper.FloatField(MaxHeight, "Max Height", mMotionController, 40f))
                {
                    lIsDirty = true;
                    MaxHeight = EditorHelper.FieldFloatValue;
                }

                GUILayout.FlexibleSpace();

                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();

                EditorGUILayout.LabelField(new GUIContent("Depth", "Maximum depth to grab an edge and exit."), GUILayout.Width(EditorGUIUtility.labelWidth));
                EditorGUILayout.LabelField("Max", GUILayout.Width(35f));
                if (EditorHelper.FloatField(MaxDepth, "Max Depth", mMotionController, 40f))
                {
                    lIsDirty = true;
                    MaxDepth = EditorHelper.FieldFloatValue;
                }

                GUILayout.FlexibleSpace();

                GUILayout.EndHorizontal();
            }

            EditorGUIUtility.labelWidth = lLabelWidth;

            return lIsDirty;
        }

#endif

        #endregion

        #region Auto-Generated
        // ************************************ START AUTO GENERATED ************************************

        /// <summary>
        /// These declarations go inside the class so you can test for which state
        /// and transitions are active. Testing hash values is much faster than strings.
        /// </summary>
        public static int STATE_IdlePose = -1;
        public static int STATE_SwimEdgeExit = -1;
        public static int STATE_crouched_to_standing = -1;
        public static int STATE_braced_hang_to_crouch = -1;
        public static int STATE_TreadIdlePose = -1;
        public static int STATE_TreadIdleExitPose = -1;
        public static int TRANS_AnyState_SwimEdgeExit = -1;
        public static int TRANS_EntryState_SwimEdgeExit = -1;
        public static int TRANS_AnyState_TreadIdlePose = -1;
        public static int TRANS_EntryState_TreadIdlePose = -1;
        public static int TRANS_AnyState_TreadIdleExitPose = -1;
        public static int TRANS_EntryState_TreadIdleExitPose = -1;
        public static int TRANS_SwimEdgeExit_braced_hang_to_crouch = -1;
        public static int TRANS_crouched_to_standing_IdlePose = -1;
        public static int TRANS_braced_hang_to_crouch_crouched_to_standing = -1;
        public static int TRANS_TreadIdlePose_braced_hang_to_crouch = -1;

        /// <summary>
        /// Determines if we're using auto-generated code
        /// </summary>
        public override bool HasAutoGeneratedCode
        {
            get { return true; }
        }

        /// <summary>
        /// Used to determine if the actor is in one of the states for this motion
        /// </summary>
        /// <returns></returns>
        public override bool IsInMotionState
        {
            get
            {
                int lStateID = mMotionLayer._AnimatorStateID;
                int lTransitionID = mMotionLayer._AnimatorTransitionID;

                if (lStateID == STATE_IdlePose) { return true; }
                if (lStateID == STATE_SwimEdgeExit) { return true; }
                if (lStateID == STATE_crouched_to_standing) { return true; }
                if (lStateID == STATE_braced_hang_to_crouch) { return true; }
                if (lStateID == STATE_TreadIdlePose) { return true; }
                if (lStateID == STATE_TreadIdleExitPose) { return true; }
                if (lTransitionID == TRANS_AnyState_SwimEdgeExit) { return true; }
                if (lTransitionID == TRANS_EntryState_SwimEdgeExit) { return true; }
                if (lTransitionID == TRANS_AnyState_TreadIdlePose) { return true; }
                if (lTransitionID == TRANS_EntryState_TreadIdlePose) { return true; }
                if (lTransitionID == TRANS_AnyState_TreadIdleExitPose) { return true; }
                if (lTransitionID == TRANS_EntryState_TreadIdleExitPose) { return true; }
                if (lTransitionID == TRANS_SwimEdgeExit_braced_hang_to_crouch) { return true; }
                if (lTransitionID == TRANS_crouched_to_standing_IdlePose) { return true; }
                if (lTransitionID == TRANS_braced_hang_to_crouch_crouched_to_standing) { return true; }
                if (lTransitionID == TRANS_TreadIdlePose_braced_hang_to_crouch) { return true; }
                return false;
            }
        }

        /// <summary>
        /// Used to determine if the actor is in one of the states for this motion
        /// </summary>
        /// <returns></returns>
        public override bool IsMotionState(int rStateID)
        {
            if (rStateID == STATE_IdlePose) { return true; }
            if (rStateID == STATE_SwimEdgeExit) { return true; }
            if (rStateID == STATE_crouched_to_standing) { return true; }
            if (rStateID == STATE_braced_hang_to_crouch) { return true; }
            if (rStateID == STATE_TreadIdlePose) { return true; }
            if (rStateID == STATE_TreadIdleExitPose) { return true; }
            return false;
        }

        /// <summary>
        /// Used to determine if the actor is in one of the states for this motion
        /// </summary>
        /// <returns></returns>
        public override bool IsMotionState(int rStateID, int rTransitionID)
        {
            if (rStateID == STATE_IdlePose) { return true; }
            if (rStateID == STATE_SwimEdgeExit) { return true; }
            if (rStateID == STATE_crouched_to_standing) { return true; }
            if (rStateID == STATE_braced_hang_to_crouch) { return true; }
            if (rStateID == STATE_TreadIdlePose) { return true; }
            if (rStateID == STATE_TreadIdleExitPose) { return true; }
            if (rTransitionID == TRANS_AnyState_SwimEdgeExit) { return true; }
            if (rTransitionID == TRANS_EntryState_SwimEdgeExit) { return true; }
            if (rTransitionID == TRANS_AnyState_TreadIdlePose) { return true; }
            if (rTransitionID == TRANS_EntryState_TreadIdlePose) { return true; }
            if (rTransitionID == TRANS_AnyState_TreadIdleExitPose) { return true; }
            if (rTransitionID == TRANS_EntryState_TreadIdleExitPose) { return true; }
            if (rTransitionID == TRANS_SwimEdgeExit_braced_hang_to_crouch) { return true; }
            if (rTransitionID == TRANS_crouched_to_standing_IdlePose) { return true; }
            if (rTransitionID == TRANS_braced_hang_to_crouch_crouched_to_standing) { return true; }
            if (rTransitionID == TRANS_TreadIdlePose_braced_hang_to_crouch) { return true; }
            return false;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            TRANS_AnyState_SwimEdgeExit = mMotionController.AddAnimatorName("AnyState -> Base Layer.Swim_Exit-SM.SwimEdgeExit");
            TRANS_EntryState_SwimEdgeExit = mMotionController.AddAnimatorName("Entry -> Base Layer.Swim_Exit-SM.SwimEdgeExit");
            TRANS_AnyState_TreadIdlePose = mMotionController.AddAnimatorName("AnyState -> Base Layer.Swim_Exit-SM.TreadIdlePose");
            TRANS_EntryState_TreadIdlePose = mMotionController.AddAnimatorName("Entry -> Base Layer.Swim_Exit-SM.TreadIdlePose");
            TRANS_AnyState_TreadIdleExitPose = mMotionController.AddAnimatorName("AnyState -> Base Layer.Swim_Exit-SM.TreadIdleExitPose");
            TRANS_EntryState_TreadIdleExitPose = mMotionController.AddAnimatorName("Entry -> Base Layer.Swim_Exit-SM.TreadIdleExitPose");
            STATE_IdlePose = mMotionController.AddAnimatorName("Base Layer.Swim_Exit-SM.IdlePose");
            STATE_SwimEdgeExit = mMotionController.AddAnimatorName("Base Layer.Swim_Exit-SM.SwimEdgeExit");
            TRANS_SwimEdgeExit_braced_hang_to_crouch = mMotionController.AddAnimatorName("Base Layer.Swim_Exit-SM.SwimEdgeExit -> Base Layer.Swim_Exit-SM.braced_hang_to_crouch");
            STATE_crouched_to_standing = mMotionController.AddAnimatorName("Base Layer.Swim_Exit-SM.crouched_to_standing");
            TRANS_crouched_to_standing_IdlePose = mMotionController.AddAnimatorName("Base Layer.Swim_Exit-SM.crouched_to_standing -> Base Layer.Swim_Exit-SM.IdlePose");
            STATE_braced_hang_to_crouch = mMotionController.AddAnimatorName("Base Layer.Swim_Exit-SM.braced_hang_to_crouch");
            TRANS_braced_hang_to_crouch_crouched_to_standing = mMotionController.AddAnimatorName("Base Layer.Swim_Exit-SM.braced_hang_to_crouch -> Base Layer.Swim_Exit-SM.crouched_to_standing");
            STATE_TreadIdlePose = mMotionController.AddAnimatorName("Base Layer.Swim_Exit-SM.TreadIdlePose");
            TRANS_TreadIdlePose_braced_hang_to_crouch = mMotionController.AddAnimatorName("Base Layer.Swim_Exit-SM.TreadIdlePose -> Base Layer.Swim_Exit-SM.braced_hang_to_crouch");
            STATE_TreadIdleExitPose = mMotionController.AddAnimatorName("Base Layer.Swim_Exit-SM.TreadIdleExitPose");
        }

#if UNITY_EDITOR

        private AnimationClip m14222 = null;
        private AnimationClip m10496 = null;
        private AnimationClip m17174 = null;
        private AnimationClip m19706 = null;
        private AnimationClip m15640 = null;

        /// <summary>
        /// Creates the animator substate machine for this motion.
        /// </summary>
        protected override void CreateStateMachine()
        {
            // Grab the root sm for the layer
            UnityEditor.Animations.AnimatorStateMachine lRootStateMachine = _EditorAnimatorController.layers[mMotionLayer.AnimatorLayerIndex].stateMachine;
            UnityEditor.Animations.AnimatorStateMachine lSM_23510 = _EditorAnimatorController.layers[mMotionLayer.AnimatorLayerIndex].stateMachine;
            UnityEditor.Animations.AnimatorStateMachine lRootSubStateMachine = null;

            // If we find the sm with our name, remove it
            for (int i = 0; i < lRootStateMachine.stateMachines.Length; i++)
            {
                // Look for a sm with the matching name
                if (lRootStateMachine.stateMachines[i].stateMachine.name == _EditorAnimatorSMName)
                {
                    lRootSubStateMachine = lRootStateMachine.stateMachines[i].stateMachine;

                    // Allow the user to stop before we remove the sm
                    if (!UnityEditor.EditorUtility.DisplayDialog("Motion Controller", _EditorAnimatorSMName + " already exists. Delete and recreate it?", "Yes", "No"))
                    {
                        return;
                    }

                    // Remove the sm
                    //lRootStateMachine.RemoveStateMachine(lRootStateMachine.stateMachines[i].stateMachine);
                    break;
                }
            }

            UnityEditor.Animations.AnimatorStateMachine lSM_23572 = lRootSubStateMachine;
            if (lSM_23572 != null)
            {
                for (int i = lSM_23572.entryTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_23572.RemoveEntryTransition(lSM_23572.entryTransitions[i]);
                }

                for (int i = lSM_23572.anyStateTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_23572.RemoveAnyStateTransition(lSM_23572.anyStateTransitions[i]);
                }

                for (int i = lSM_23572.states.Length - 1; i >= 0; i--)
                {
                    lSM_23572.RemoveState(lSM_23572.states[i].state);
                }

                for (int i = lSM_23572.stateMachines.Length - 1; i >= 0; i--)
                {
                    lSM_23572.RemoveStateMachine(lSM_23572.stateMachines[i].stateMachine);
                }
            }
            else
            {
                lSM_23572 = lSM_23510.AddStateMachine(_EditorAnimatorSMName, new Vector3(192, -36, 0));
            }

            UnityEditor.Animations.AnimatorState lS_24640 = lSM_23572.AddState("IdlePose", new Vector3(564, -12, 0));
            lS_24640.speed = 1f;
            lS_24640.motion = m14222;

            UnityEditor.Animations.AnimatorState lS_23796 = lSM_23572.AddState("SwimEdgeExit", new Vector3(216, -12, 0));
            lS_23796.speed = 1f;
            lS_23796.motion = m10496;

            UnityEditor.Animations.AnimatorState lS_24642 = lSM_23572.AddState("crouched_to_standing", new Vector3(564, 60, 0));
            lS_24642.speed = 0.75f;
            lS_24642.motion = m17174;

            UnityEditor.Animations.AnimatorState lS_24644 = lSM_23572.AddState("braced_hang_to_crouch", new Vector3(300, 60, 0));
            lS_24644.speed = 0.75f;
            lS_24644.motion = m19706;

            UnityEditor.Animations.AnimatorState lS_23798 = lSM_23572.AddState("TreadIdlePose", new Vector3(216, 144, 0));
            lS_23798.speed = 1f;
            lS_23798.motion = m15640;

            UnityEditor.Animations.AnimatorState lS_23800 = lSM_23572.AddState("TreadIdleExitPose", new Vector3(216, 204, 0));
            lS_23800.speed = 1f;
            lS_23800.motion = m15640;

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_23682 = lRootStateMachine.AddAnyStateTransition(lS_23796);
            lT_23682.hasExitTime = false;
            lT_23682.hasFixedDuration = true;
            lT_23682.exitTime = 0.9f;
            lT_23682.duration = 0.1f;
            lT_23682.offset = 0f;
            lT_23682.mute = false;
            lT_23682.solo = false;
            lT_23682.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 31600f, "L0MotionPhase");
            lT_23682.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_23684 = lRootStateMachine.AddAnyStateTransition(lS_23798);
            lT_23684.hasExitTime = false;
            lT_23684.hasFixedDuration = true;
            lT_23684.exitTime = 0.9f;
            lT_23684.duration = 0.1f;
            lT_23684.offset = 0f;
            lT_23684.mute = false;
            lT_23684.solo = false;
            lT_23684.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 31600f, "L0MotionPhase");
            lT_23684.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_23686 = lRootStateMachine.AddAnyStateTransition(lS_23800);
            lT_23686.hasExitTime = false;
            lT_23686.hasFixedDuration = true;
            lT_23686.exitTime = 0.9f;
            lT_23686.duration = 0.3f;
            lT_23686.offset = 0f;
            lT_23686.mute = false;
            lT_23686.solo = false;
            lT_23686.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 31601f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_24646 = lS_23796.AddTransition(lS_24644);
            lT_24646.hasExitTime = true;
            lT_24646.hasFixedDuration = true;
            lT_24646.exitTime = 0.3347696f;
            lT_24646.duration = 0.4852898f;
            lT_24646.offset = 0f;
            lT_24646.mute = false;
            lT_24646.solo = false;
            lT_24646.canTransitionToSelf = true;

            UnityEditor.Animations.AnimatorStateTransition lT_24648 = lS_24642.AddTransition(lS_24640);
            lT_24648.hasExitTime = true;
            lT_24648.hasFixedDuration = true;
            lT_24648.exitTime = 0.7064211f;
            lT_24648.duration = 0.25f;
            lT_24648.offset = 0f;
            lT_24648.mute = false;
            lT_24648.solo = false;
            lT_24648.canTransitionToSelf = true;

            UnityEditor.Animations.AnimatorStateTransition lT_24650 = lS_24644.AddTransition(lS_24642);
            lT_24650.hasExitTime = true;
            lT_24650.hasFixedDuration = true;
            lT_24650.exitTime = 0.9f;
            lT_24650.duration = 0.1f;
            lT_24650.offset = 0f;
            lT_24650.mute = false;
            lT_24650.solo = false;
            lT_24650.canTransitionToSelf = true;

            UnityEditor.Animations.AnimatorStateTransition lT_24652 = lS_23798.AddTransition(lS_24644);
            lT_24652.hasExitTime = true;
            lT_24652.hasFixedDuration = true;
            lT_24652.exitTime = 0f;
            lT_24652.duration = 0.25f;
            lT_24652.offset = 0f;
            lT_24652.mute = false;
            lT_24652.solo = false;
            lT_24652.canTransitionToSelf = true;

        }

        /// <summary>
        /// Gathers the animations so we can use them when creating the sub-state machine.
        /// </summary>
        public override void FindAnimations()
        {
            m14222 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdlePose.anim", "IdlePose");
            m10496 = FindAnimationClip("Assets/ootii/MotionControllerPacks/Swimming/Content/Animations/Mixamo/swimming_to_edge.fbx/SwimEdgeExit.anim", "SwimEdgeExit");
            m17174 = FindAnimationClip("Assets/ootii/MotionControllerPacks/Swimming/Content/Animations/Mixamo/crouched_to_standing.fbx/crouched_to_standing.anim", "crouched_to_standing");
            m19706 = FindAnimationClip("Assets/ootii/MotionControllerPacks/Swimming/Content/Animations/Mixamo/braced_hang_to_crouch.fbx/braced_hang_to_crouch.anim", "braced_hang_to_crouch");
            m15640 = FindAnimationClip("Assets/ootii/MotionControllerPacks/Swimming/Content/Animations/Mixamo/treading_water.fbx/TreadIdlePose.anim", "TreadIdlePose");

            // Add the remaining functionality
            base.FindAnimations();
        }

        /// <summary>
        /// Used to show the settings that allow us to generate the animator setup.
        /// </summary>
        public override void OnSettingsGUI()
        {
            UnityEditor.EditorGUILayout.IntField(new GUIContent("Phase ID", "Phase ID used to transition to the state."), PHASE_START);
            m14222 = CreateAnimationField("IdlePose", "Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdlePose.anim", "IdlePose", m14222);
            m10496 = CreateAnimationField("SwimEdgeExit", "Assets/ootii/MotionControllerPacks/Swimming/Content/Animations/Mixamo/swimming_to_edge.fbx/SwimEdgeExit.anim", "SwimEdgeExit", m10496);
            m17174 = CreateAnimationField("crouched_to_standing", "Assets/ootii/MotionControllerPacks/Swimming/Content/Animations/Mixamo/crouched_to_standing.fbx/crouched_to_standing.anim", "crouched_to_standing", m17174);
            m19706 = CreateAnimationField("braced_hang_to_crouch", "Assets/ootii/MotionControllerPacks/Swimming/Content/Animations/Mixamo/braced_hang_to_crouch.fbx/braced_hang_to_crouch.anim", "braced_hang_to_crouch", m19706);
            m15640 = CreateAnimationField("TreadIdlePose", "Assets/ootii/MotionControllerPacks/Swimming/Content/Animations/Mixamo/treading_water.fbx/TreadIdlePose.anim", "TreadIdlePose", m15640);

            // Add the remaining functionality
            base.OnSettingsGUI();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}