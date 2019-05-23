using System;
using UnityEngine;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Helpers;
using com.ootii.Geometry;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.MotionControllerPacks
{
    /// <summary>
    /// Very basic idle when the bow is out. There is no rotations.
    /// </summary>
    [MotionName("Swim - Dive")]
    [MotionDescription("Standard dive motion to start swimming.")]
    public class Swim_Dive : MotionControllerMotion
    {
        // Enum values for the motion
        public const int PHASE_UNKNOWN = 0;
        public const int PHASE_START = 31500;
        public const int PHASE_ENTER_WATER = 31501;

        /// <summary>
        /// Distance to check if water exists
        /// </summary>
        public float _TestDistance = 2.2f;
        public float TestDistance
        {
            get { return _TestDistance; }
            set { _TestDistance = value; }
        }

        /// <summary>
        /// Distance to check if water exists (when moving)
        /// </summary>
        public float _MovingTestDistance = 3.2f;
        public float MovingTestDistance
        {
            get { return _MovingTestDistance; }
            set { _MovingTestDistance = value; }
        }

        /// <summary>
        /// Depth to check if the water is deep enough to dive into
        /// </summary>
        public float _MinDepth = 1.5f;
        public float MinDepth
        {
            get { return _MinDepth; }
            set { _MinDepth = value; }
        }

        /// <summary>
        /// Depth at which we come out of the dive
        /// </summary>
        public float _RecoverDepth = 1.4f;
        public float RecoverDepth
        {
            get { return _RecoverDepth; }
            set { _RecoverDepth = value; }
        }

        /// <summary>
        /// When we hit the water, the factor we adjust current momentum by
        /// </summary>
        public float _InWaterMomentumFactor = 0.7f;
        public float InWaterMomentumFactor
        {
            get { return _InWaterMomentumFactor; }
            set { _InWaterMomentumFactor = value; }
        }

        /// <summary>
        /// Determines if we've hit water yet
        /// </summary>
        protected bool mHasHitWater = false;

        /// <summary>
        /// Determines if we've splashed yet
        /// </summary>
        protected bool mHasSplashed = false;

        /// <summary>
        /// Determines if we're dealing with a running dive or idle dive
        /// </summary>
        protected bool mIsRunningDive = false;

        /// <summary>
        /// Momentum from the start of the dive
        /// </summary>
        protected Vector3 mMomentum = Vector3.zero;

        /// <summary>
        /// Swimmer info associated with Swim_Idle
        /// </summary>
        protected SwimmerInfo mSwimmerInfo = null;

        /// <summary>
        /// Movement motion we'll use for tilting
        /// </summary>
        protected Swim_Strafe mSwimStrafe = null;

        /// <summary>
        /// Default constructor
        /// </summary>
        public Swim_Dive()
            : base()
        {
            _Pack = Swim_Idle.GroupName();
            _Category = EnumMotionCategories.JUMP;

            _Priority = 24;
            _ActionAlias = "Jump";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "Swim_Dive-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public Swim_Dive(MotionController rController)
            : base(rController)
        {
            _Pack = Swim_Idle.GroupName();
            _Category = EnumMotionCategories.JUMP;

            _Priority = 24;
            _ActionAlias = "Jump";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "Swim_Dive-SM"; }
#endif
        }

        /// <summary>
        /// Allows for any processing after the motion has been deserialized
        /// </summary>
        public override void Awake()
        {
            base.Awake();
        }

        /// <summary>
        /// Tests if this motion should be started. However, the motion
        /// isn't actually started.
        /// </summary>
        /// <returns></returns>
        public override bool TestActivate()
        {
            if (!mIsStartable) { return false; }

            if (mMotionController._InputSource != null && mMotionController._InputSource.IsJustPressed(ActionAlias))
            {
                // Grab the swimmer info if it doesn't exist
                if (mSwimmerInfo == null) { mSwimmerInfo = SwimmerInfo.GetSwimmerInfo(mMotionController._Transform); }
                if (mSwimStrafe == null) { mSwimStrafe = mMotionController.GetMotion<Swim_Strafe>(); }

                // We need to test if there is water to dive in to
                // This is only valid if we're in the right stance
                if (mActorController.State.Stance != EnumControllerStance.SWIMMING)
                {
                    float lWaterDistance = 0f;
                    float lObstacleDistance = float.MaxValue;


                    // Do a test to see if there is actually water to jump into
                    RaycastHit lHitInfo;

                    float lMinDepth = Mathf.Max(MinDepth, mSwimmerInfo.EnterDepth * 1.5f);
                    float lDepthTest = Mathf.Max(lMinDepth * 1.6f, mSwimmerInfo.MaxSurfaceTest);

                    float lTestDistance = TestDistance;
                    if (mMotionController.State.InputMagnitudeTrend.Value > 0.5f) { lTestDistance = MovingTestDistance; }

                    // Check if we can dive forward without hitting something
                    Vector3 lStart = mMotionController._Transform.position + (Vector3.up * mSwimmerInfo.EnterDepth);
                    if (!RaycastExt.SafeRaycast(lStart, mMotionController._Transform.forward, lTestDistance, mActorController.CollisionLayers, mMotionController._Transform, null, false))
                    {
                        // Check surface height based on the raycast
                        lStart = mMotionController._Transform.position + (mMotionController._Transform.forward * lTestDistance) + (Vector3.up * mSwimmerInfo.EnterDepth * 1.5f);
                        if (RaycastExt.SafeRaycast(lStart, Vector3.down, out lHitInfo, lDepthTest, mSwimmerInfo.WaterLayers, mMotionController._Transform, null, false))
                        {
                            lWaterDistance = lHitInfo.distance;

                            // Ensure nothing is blocking the water
                            if (RaycastExt.SafeRaycast(lStart, Vector3.down, out lHitInfo, lDepthTest, mActorController.GroundingLayers, mMotionController._Transform, null, false))
                            {
                                lObstacleDistance = Mathf.Min(lHitInfo.distance, lObstacleDistance);
                            }

                            if (RaycastExt.SafeRaycast(lStart, Vector3.down, out lHitInfo, lDepthTest, mActorController.CollisionLayers, mMotionController._Transform, null, false))
                            {
                                lObstacleDistance = Mathf.Min(lHitInfo.distance, lObstacleDistance);
                            }

                            // Ensure there's enough depth to swim in
                            float lMaxDepthFound = lObstacleDistance - lWaterDistance;
                            if (lMaxDepthFound > lMinDepth)
                            {
                                return true;
                            }
                        }
                    }
                }
            }

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
                // This is only valid if we're in the right stance
                if (mActorController.State.Stance != EnumControllerStance.SWIMMING)
                {
                    //return false;
                }

                // Ensure we're in a valid animation
                if (!IsInMotionState)
                {
                    return false;
                }
            }

            // If we're surfacing, exit to the idle pose
            if (mMotionLayer._AnimatorStateID == STATE_SwimIdlePose ||
                mMotionLayer._AnimatorStateID == STATE_TreadIdlePose)
            {
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
            mHasHitWater = false;
            mHasSplashed = false;

            mMomentum = mActorController.State.Velocity;
            if (mMomentum.sqrMagnitude == 0f) { mMomentum = mActorController._Transform.forward * 5f; }

            // Let the animation handle vertical movement for now
            mActorController.IsGravityEnabled = false;
            mActorController.ForceGrounding = false;

            // Tell the animator to start your animations
            mIsRunningDive = mMotionController.State.InputMagnitudeTrend.Value > 0.6f;
            mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, PHASE_START, (mIsRunningDive ? 1 : 0), true);

            // Return
            return base.Activate(rPrevMotion);
        }

        /// <summary>
        /// Called to stop the motion. If the motion is stopable. Some motions
        /// like jump cannot be stopped early
        /// </summary>
        public override void Deactivate()
        {
            // Finish the deactivation process
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
            if (!mHasHitWater &&
                mMotionLayer._AnimatorStateID == STATE_run_to_dive &&
                mMotionLayer._AnimatorStateNormalizedTime > 0.81f)
            {
                mActorController.IsGravityEnabled = true;
                mActorController.ForceGrounding = true;
            }
        }

        /// <summary>
        /// Updates the motion over time. This is called by the controller
        /// every update cycle so animations and stages can be updated.
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        public override void Update(float rDeltaTime, int rUpdateIndex)
        {
            mMovement = Vector3.zero;

            // Add movement based on the dive
            if ((mMotionLayer._AnimatorStateID == STATE_run_to_dive && mMotionLayer._AnimatorStateNormalizedTime > 0.5f) ||
                mMotionLayer._AnimatorTransitionID == TRANS_run_to_dive_SwimIdlePose ||
                mMotionLayer._AnimatorTransitionID == TRANS_run_to_dive_TreadIdlePose)
            {
                mMovement = mMomentum * _InWaterMomentumFactor * rDeltaTime;
            }

            // Test if it's time to play the splash
            if (!mHasHitWater)
            {
                if ((mMotionLayer._AnimatorStateID == STATE_run_to_dive && mMotionLayer._AnimatorStateNormalizedTime > 0.65f) ||
                    (mMotionLayer._AnimatorStateID == STATE_StandingDive && mMotionLayer._AnimatorStateNormalizedTime > 0.65f))
                {
                    float lDepth = mSwimmerInfo.GetDepth();

                    if (!mHasSplashed && lDepth > 0.1f)
                    {
                        mHasSplashed = true;
                        mSwimmerInfo.CreateSplash(mMotionController._Transform.position + (mMotionController._Transform.forward * 1f));
                    }

                    if (lDepth > _RecoverDepth)
                    {
                        mHasHitWater = true;
                        mSwimmerInfo.EnterWater();

                        mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, PHASE_ENTER_WATER, true);
                    }
                }
            }
            else
            {
                // We still want gravity to take effect to a little while... just to represent momentum
                if (mMomentum.y == 0f && mMotionLayer._AnimatorTransitionID != 0 && mMotionLayer._AnimatorTransitionNormalizedTime < 0.5f)
                {
                    mMovement.y = mMovement.y + (UnityEngine.Physics.gravity.y * _InWaterMomentumFactor * 0.25f * rDeltaTime);
                }
            }

            // This is a safety check to ensure we don't run aground
            if ((mMotionLayer._AnimatorStateID == STATE_run_to_dive && mMotionLayer._AnimatorStateNormalizedTime > 0.65f))
            {
                if (RaycastExt.SafeRaycast(mMotionController._Transform.position + (Vector3.up * mSwimmerInfo.EnterDepth * 0.1f), Vector3.down, 0.12f, mActorController.GroundingLayers, mMotionController._Transform, null, false))
                {
                    Deactivate();
                }
            }
        }

        #region Editor Methods

        // **************************************************************************************************
        // Following properties and function only valid while editing
        // **************************************************************************************************

#if UNITY_EDITOR

        /// <summary>
        /// Allow the constraint to render it's own GUI
        /// </summary>
        /// <returns>Reports if the object's value was changed</returns>
        public override bool OnInspectorGUI()
        {
            bool lIsDirty = false;

            float lLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100f;

            if (EditorHelper.TextField("Action Alias", "Action alias that triggers diving.", ActionAlias, mMotionController))
            {
                lIsDirty = true;
                ActionAlias = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.FloatField("Test Distance", "Forward distance to check for water.", TestDistance, mMotionController))
            {
                lIsDirty = true;
                TestDistance = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField("Move Test Distance", "Forward distance to check for water when moving.", MovingTestDistance, mMotionController))
            {
                lIsDirty = true;
                MovingTestDistance = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField("Min Depth", "Minimum depth that we can dive into.", MinDepth, mMotionController))
            {
                lIsDirty = true;
                MinDepth = EditorHelper.FieldFloatValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.FloatField("Recover Depth", "Depth at which we come out of the dive and into a normal swim.", RecoverDepth, mMotionController))
            {
                lIsDirty = true;
                RecoverDepth = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField("Momentum Factor", "When we hit the water, the amount we multiply momentum by to reduce it.", InWaterMomentumFactor, mMotionController))
            {
                lIsDirty = true;
                InWaterMomentumFactor = EditorHelper.FieldFloatValue;
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
        public static int STATE_run_to_dive = -1;
        public static int STATE_SwimIdlePose = -1;
        public static int STATE_StandingDive = -1;
        public static int STATE_TreadIdlePose = -1;
        public static int TRANS_AnyState_StandingDive = -1;
        public static int TRANS_EntryState_StandingDive = -1;
        public static int TRANS_AnyState_run_to_dive = -1;
        public static int TRANS_EntryState_run_to_dive = -1;
        public static int TRANS_run_to_dive_SwimIdlePose = -1;
        public static int TRANS_run_to_dive_TreadIdlePose = -1;
        public static int TRANS_StandingDive_run_to_dive = -1;

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

                if (lStateID == STATE_run_to_dive) { return true; }
                if (lStateID == STATE_SwimIdlePose) { return true; }
                if (lStateID == STATE_StandingDive) { return true; }
                if (lStateID == STATE_TreadIdlePose) { return true; }
                if (lTransitionID == TRANS_AnyState_StandingDive) { return true; }
                if (lTransitionID == TRANS_EntryState_StandingDive) { return true; }
                if (lTransitionID == TRANS_AnyState_run_to_dive) { return true; }
                if (lTransitionID == TRANS_EntryState_run_to_dive) { return true; }
                if (lTransitionID == TRANS_run_to_dive_SwimIdlePose) { return true; }
                if (lTransitionID == TRANS_run_to_dive_TreadIdlePose) { return true; }
                if (lTransitionID == TRANS_StandingDive_run_to_dive) { return true; }
                return false;
            }
        }

        /// <summary>
        /// Used to determine if the actor is in one of the states for this motion
        /// </summary>
        /// <returns></returns>
        public override bool IsMotionState(int rStateID)
        {
            if (rStateID == STATE_run_to_dive) { return true; }
            if (rStateID == STATE_SwimIdlePose) { return true; }
            if (rStateID == STATE_StandingDive) { return true; }
            if (rStateID == STATE_TreadIdlePose) { return true; }
            return false;
        }

        /// <summary>
        /// Used to determine if the actor is in one of the states for this motion
        /// </summary>
        /// <returns></returns>
        public override bool IsMotionState(int rStateID, int rTransitionID)
        {
            if (rStateID == STATE_run_to_dive) { return true; }
            if (rStateID == STATE_SwimIdlePose) { return true; }
            if (rStateID == STATE_StandingDive) { return true; }
            if (rStateID == STATE_TreadIdlePose) { return true; }
            if (rTransitionID == TRANS_AnyState_StandingDive) { return true; }
            if (rTransitionID == TRANS_EntryState_StandingDive) { return true; }
            if (rTransitionID == TRANS_AnyState_run_to_dive) { return true; }
            if (rTransitionID == TRANS_EntryState_run_to_dive) { return true; }
            if (rTransitionID == TRANS_run_to_dive_SwimIdlePose) { return true; }
            if (rTransitionID == TRANS_run_to_dive_TreadIdlePose) { return true; }
            if (rTransitionID == TRANS_StandingDive_run_to_dive) { return true; }
            return false;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            TRANS_AnyState_StandingDive = mMotionController.AddAnimatorName("AnyState -> Base Layer.Swim_Dive-SM.StandingDive");
            TRANS_EntryState_StandingDive = mMotionController.AddAnimatorName("Entry -> Base Layer.Swim_Dive-SM.StandingDive");
            TRANS_AnyState_run_to_dive = mMotionController.AddAnimatorName("AnyState -> Base Layer.Swim_Dive-SM.run_to_dive");
            TRANS_EntryState_run_to_dive = mMotionController.AddAnimatorName("Entry -> Base Layer.Swim_Dive-SM.run_to_dive");
            STATE_run_to_dive = mMotionController.AddAnimatorName("Base Layer.Swim_Dive-SM.run_to_dive");
            TRANS_run_to_dive_SwimIdlePose = mMotionController.AddAnimatorName("Base Layer.Swim_Dive-SM.run_to_dive -> Base Layer.Swim_Dive-SM.SwimIdlePose");
            TRANS_run_to_dive_TreadIdlePose = mMotionController.AddAnimatorName("Base Layer.Swim_Dive-SM.run_to_dive -> Base Layer.Swim_Dive-SM.TreadIdlePose");
            STATE_SwimIdlePose = mMotionController.AddAnimatorName("Base Layer.Swim_Dive-SM.SwimIdlePose");
            STATE_StandingDive = mMotionController.AddAnimatorName("Base Layer.Swim_Dive-SM.StandingDive");
            TRANS_StandingDive_run_to_dive = mMotionController.AddAnimatorName("Base Layer.Swim_Dive-SM.StandingDive -> Base Layer.Swim_Dive-SM.run_to_dive");
            STATE_TreadIdlePose = mMotionController.AddAnimatorName("Base Layer.Swim_Dive-SM.TreadIdlePose");
        }

#if UNITY_EDITOR

        private AnimationClip m13726 = null;
        private AnimationClip m17380 = null;
        private AnimationClip m18078 = null;
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

            UnityEditor.Animations.AnimatorStateMachine lSM_N25356 = lRootSubStateMachine;
            if (lSM_N25356 != null)
            {
                for (int i = lSM_N25356.entryTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_N25356.RemoveEntryTransition(lSM_N25356.entryTransitions[i]);
                }

                for (int i = lSM_N25356.anyStateTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_N25356.RemoveAnyStateTransition(lSM_N25356.anyStateTransitions[i]);
                }

                for (int i = lSM_N25356.states.Length - 1; i >= 0; i--)
                {
                    lSM_N25356.RemoveState(lSM_N25356.states[i].state);
                }

                for (int i = lSM_N25356.stateMachines.Length - 1; i >= 0; i--)
                {
                    lSM_N25356.RemoveStateMachine(lSM_N25356.stateMachines[i].stateMachine);
                }
            }
            else
            {
                lSM_N25356 = lSM_23510.AddStateMachine(_EditorAnimatorSMName, new Vector3(636, -84, 0));
            }

            UnityEditor.Animations.AnimatorState lS_N25358 = lSM_N25356.AddState("run_to_dive", new Vector3(312, 132, 0));
            lS_N25358.speed = 1f;
            lS_N25358.motion = m13726;

            UnityEditor.Animations.AnimatorState lS_N25360 = lSM_N25356.AddState("SwimIdlePose", new Vector3(576, 180, 0));
            lS_N25360.speed = 1f;
            lS_N25360.motion = m17380;

            UnityEditor.Animations.AnimatorState lS_N25362 = lSM_N25356.AddState("StandingDive", new Vector3(312, 36, 0));
            lS_N25362.speed = 1f;
            lS_N25362.motion = m18078;

            UnityEditor.Animations.AnimatorState lS_N25364 = lSM_N25356.AddState("TreadIdlePose", new Vector3(576, 96, 0));
            lS_N25364.speed = 1f;
            lS_N25364.motion = m15640;

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N25366 = lRootStateMachine.AddAnyStateTransition(lS_N25362);
            lT_N25366.hasExitTime = false;
            lT_N25366.hasFixedDuration = true;
            lT_N25366.exitTime = 0.9f;
            lT_N25366.duration = 0.1f;
            lT_N25366.offset = 0f;
            lT_N25366.mute = false;
            lT_N25366.solo = false;
            lT_N25366.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 31500f, "L0MotionPhase");
            lT_N25366.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N25368 = lRootStateMachine.AddAnyStateTransition(lS_N25358);
            lT_N25368.hasExitTime = false;
            lT_N25368.hasFixedDuration = true;
            lT_N25368.exitTime = 0.9f;
            lT_N25368.duration = 0.1f;
            lT_N25368.offset = 0f;
            lT_N25368.mute = false;
            lT_N25368.solo = false;
            lT_N25368.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 31500f, "L0MotionPhase");
            lT_N25368.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N25370 = lS_N25358.AddTransition(lS_N25360);
            lT_N25370.hasExitTime = false;
            lT_N25370.hasFixedDuration = true;
            lT_N25370.exitTime = 0.8834254f;
            lT_N25370.duration = 0.25f;
            lT_N25370.offset = 0f;
            lT_N25370.mute = false;
            lT_N25370.solo = false;
            lT_N25370.canTransitionToSelf = true;
            lT_N25370.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 31501f, "L0MotionPhase");
            lT_N25370.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.6f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_N25372 = lS_N25358.AddTransition(lS_N25364);
            lT_N25372.hasExitTime = false;
            lT_N25372.hasFixedDuration = true;
            lT_N25372.exitTime = 0.7887324f;
            lT_N25372.duration = 0.5f;
            lT_N25372.offset = 0f;
            lT_N25372.mute = false;
            lT_N25372.solo = false;
            lT_N25372.canTransitionToSelf = true;
            lT_N25372.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 31501f, "L0MotionPhase");
            lT_N25372.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.6f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_N25374 = lS_N25362.AddTransition(lS_N25358);
            lT_N25374.hasExitTime = true;
            lT_N25374.hasFixedDuration = true;
            lT_N25374.exitTime = 0.6953372f;
            lT_N25374.duration = 0.1499999f;
            lT_N25374.offset = 0.3380885f;
            lT_N25374.mute = false;
            lT_N25374.solo = false;
            lT_N25374.canTransitionToSelf = true;

        }

        /// <summary>
        /// Gathers the animations so we can use them when creating the sub-state machine.
        /// </summary>
        public override void FindAnimations()
        {
            m13726 = FindAnimationClip("Assets/ootii/MotionControllerPacks/Swimming/Content/Animations/Mixamo/run_to_dive.fbx/run_to_dive.anim", "run_to_dive");
            m17380 = FindAnimationClip("Assets/ootii/MotionControllerPacks/Swimming/Content/Animations/Mixamo/swimming.fbx/SwimIdlePose.anim", "SwimIdlePose");
            m18078 = FindAnimationClip("Assets/ootii/MotionControllerPacks/Swimming/Content/Animations/Mixamo/dive_roll.fbx/StandingDive.anim", "StandingDive");
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
            m13726 = CreateAnimationField("run_to_dive", "Assets/ootii/MotionControllerPacks/Swimming/Content/Animations/Mixamo/run_to_dive.fbx/run_to_dive.anim", "run_to_dive", m13726);
            m17380 = CreateAnimationField("SwimIdlePose", "Assets/ootii/MotionControllerPacks/Swimming/Content/Animations/Mixamo/swimming.fbx/SwimIdlePose.anim", "SwimIdlePose", m17380);
            m18078 = CreateAnimationField("StandingDive", "Assets/ootii/MotionControllerPacks/Swimming/Content/Animations/Mixamo/dive_roll.fbx/StandingDive.anim", "StandingDive", m18078);
            m15640 = CreateAnimationField("TreadIdlePose", "Assets/ootii/MotionControllerPacks/Swimming/Content/Animations/Mixamo/treading_water.fbx/TreadIdlePose.anim", "TreadIdlePose", m15640);

            // Add the remaining functionality
            base.OnSettingsGUI();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}