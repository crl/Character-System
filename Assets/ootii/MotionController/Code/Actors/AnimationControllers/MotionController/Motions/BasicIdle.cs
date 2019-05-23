using UnityEngine;
using com.ootii.Cameras;
using com.ootii.Geometry;
using com.ootii.Helpers;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Actors.AnimationControllers
{
    /// <summary>
    /// Idle motion for when the character is just standing and waiting
    /// for input or some interaction.
    /// </summary>
    [MotionName("Basic Idle")]
    [MotionDescription("Simple idle motion to be used as a default motion. It can also rotate the actor with the camera view.")]
    public class BasicIdle : MotionControllerMotion
    {
        /// <summary>
        /// Trigger values for the motion
        /// </summary>
        public int PHASE_UNKNOWN = 0;
        public int PHASE_START = 3000;

        /// <summary>
        /// Determines if we're using the IsInMotion() function to verify that
        /// the transition in the animator has occurred for this motion.
        /// </summary>
        public override bool VerifyTransition
        {
            get { return false; }
        }

        /// <summary>
        /// Determines if we rotate to match the camera
        /// </summary>
        public bool _RotateWithCamera = false;
        public bool RotateWithCamera
        {
            get { return _RotateWithCamera; }

            set
            {
                _RotateWithCamera = value;

                // Register this motion with the camera
                if (mMotionController != null && mMotionController.CameraRig is BaseCameraRig)
                {
                    ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
                    if (_RotateWithCamera) { ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate += OnCameraUpdated; }
                }
            }
        }

        /// <summary>
        /// Desired degrees of rotation per second
        /// </summary>
        public float _RotationToCameraSpeed = 360f;
        public float RotationToCameraSpeed
        {
            get { return _RotationToCameraSpeed; }
            set { _RotationToCameraSpeed = value; }
        }

        /// <summary>
        /// Determines if we rotate by ourselves
        /// </summary>
        public bool _RotateWithInput = false;
        public bool RotateWithInput
        {
            get { return _RotateWithInput; }
            set { _RotateWithInput = value; }
        }

        /// <summary>
        /// Desired degrees of rotation per second
        /// </summary>
        public float _RotationSpeed = 120f;
        public float RotationSpeed
        {
            get { return _RotationSpeed; }
            set { _RotationSpeed = value; }
        }

        /// <summary>
        /// Used to apply some smoothing to the mouse movement
        /// </summary>
        public float _RotationSmoothing = 0.1f;
        public virtual float RotationSmoothing
        {
            get { return _RotationSmoothing; }
            set { _RotationSmoothing = value; }
        }

        /// <summary>
        /// Determines if the actor rotation should be linked to the camera
        /// </summary>
        protected bool mLinkRotation = false;

        /// <summary>
        /// Fields to help smooth out the mouse rotation
        /// </summary>
        protected float mYaw = 0f;
        protected float mYawTarget = 0f;
        protected float mYawVelocity = 0f;

        // Used to force a change if neede
        protected int mActiveForm = 0;

        /// <summary>
        /// Default constructor
        /// </summary>
        public BasicIdle()
            : base()
        {
            _Category = EnumMotionCategories.IDLE;

            _Priority = 0;

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "BasicIdle-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public BasicIdle(MotionController rController)
            : base(rController)
        {
            _Category = EnumMotionCategories.IDLE;

            _Priority = 0;

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "BasicIdle-SM"; }
#endif
        }

        /// <summary>
        /// Awake is called after all objects are initialized so you can safely speak to other objects. This is where
        /// reference can be associated.
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
            // This is a catch all. If there are no motions found to match
            // the controller's state, we default to this motion.
            if (mMotionLayer.ActiveMotion == null)
            {
                // We used different timing based on the grounded flag
                if (mMotionController.IsGrounded)
                {
                    return true;
                }
            }

            // Handle the disqualifiers
            if (!mIsStartable) { return false; }
            if (!mMotionController.IsGrounded) { return false; }
            if (mMotionController.State.InputMagnitudeTrend.Average != 0f) { return false; }

            return true;
        }

        /// <summary>
        /// Tests if the motion should continue. If it shouldn't, the motion
        /// is typically disabled
        /// </summary>
        /// <returns></returns>
        public override bool TestUpdate()
        {
            //if (mIsAnimatorActive && !IsInMotionState)
            //{
            //    return false;
            //}

            //if (mMotionController.Stance != EnumControllerStance.TRAVERSAL)
            //{
            //    return false;
            //}

            // Exit if we hit an exit node
            if (mMotionLayer.AnimatorTransitionID == 0 &&
                mMotionController.State.AnimatorStates[mMotionLayer._AnimatorLayerIndex].StateInfo.IsTag("Exit"))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Called to start the specific motion. If the motion
        /// were something like 'jump', this would start the jumping process
        /// </summary>
        /// <param name="rPrevMotion">Motion that this motion is taking over from</param>
        public override bool Activate(MotionControllerMotion rPrevMotion)
        {
            // Reset the yaw info for smoothing
            mYaw = 0f;
            mYawTarget = 0f;
            mYawVelocity = 0f;
            mLinkRotation = false;

            // Trigger the transition
            mActiveForm = (_Form > 0 ? _Form : mMotionController.CurrentForm);
            mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, mActiveForm, mParameter, true);

            // Register this motion with the camera
            if (_RotateWithCamera && mMotionController.CameraRig is BaseCameraRig)
            {
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate += OnCameraUpdated;
            }

            // Finalize the activation
            return base.Activate(rPrevMotion);
        }

        /// <summary>
        /// Raised when we shut the motion down
        /// </summary>
        public override void Deactivate()
        {
            mParameter = 0;

            // Register this motion with the camera
            if (mMotionController.CameraRig is BaseCameraRig)
            {
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
            }

            // Continue with the deactivation
            base.Deactivate();
        }

        /// <summary>
        /// Allows the motion to modify the velocity before it is applied.
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        /// <param name="rMovement">Amount of movement caused by root motion this frame</param>
        /// <param name="rRotation">Amount of rotation caused by root motion this frame</param>
        /// <returns></returns>
        public override void UpdateRootMotion(float rDeltaTime, int rUpdateIndex, ref Vector3 rMovement, ref Quaternion rRotation)
        {
            rMovement = Vector3.zero;
            rRotation = Quaternion.identity;
        }

        /// <summary>
        /// Updates the motion over time. This is called by the controller
        /// every update cycle so animations and stages can be updated.
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        public override void Update(float rDeltaTime, int rUpdateIndex)
        {
            mVelocity = Vector3.zero;
            mMovement = Vector3.zero;
            mAngularVelocity = Vector3.zero;
            mRotation = Quaternion.identity;

            // Check if we're rotating with the camera
            bool lRotateWithCamera = false;
            if (_RotateWithCamera && mMotionController._CameraTransform != null)
            {
                if (mMotionController._InputSource.IsPressed(_ActionAlias))
                {
                    lRotateWithCamera = true;

                    // If we're meant to rotate with the camera (and OnCameraUpdate isn't already attached), do it here
                    if (!(mMotionController.CameraRig is BaseCameraRig))
                    {
                        OnCameraUpdated(rDeltaTime, rUpdateIndex, null);
                    }
                }
            }

            // If we're not rotating with the camera, rotate with the input
            if (!lRotateWithCamera && _RotateWithInput)
            {
                mLinkRotation = false;
                RotateUsingInput(rDeltaTime, ref mRotation);
            }
            
            // Force a style change if needed
            if (_Form <= 0 && mActiveForm != mMotionController.CurrentForm)
            {
                mActiveForm = mMotionController.CurrentForm;
                mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, mActiveForm, 0, true);
            }
        }

        /// <summary>
        /// Create a rotation velocity that rotates the character based on input
        /// </summary>
        /// <param name="rDeltaTime"></param>
        /// <param name="rAngularVelocity"></param>
        private void RotateUsingInput(float rDeltaTime, ref Quaternion rRotation)
        {
            // If we don't have an input source, stop
            if (mMotionController._InputSource == null) { return; }

            // Determine this frame's rotation
            float lYawDelta = 0f;
            if (_RotateWithInput && mMotionController._InputSource.IsViewingActivated)
            {
                lYawDelta = mMotionController._InputSource.ViewX * _RotationSpeed * rDeltaTime;
            }

            mYawTarget = mYawTarget + lYawDelta;

            // Smooth the rotation
            lYawDelta = (_RotationSmoothing <= 0f ? mYawTarget : Mathf.SmoothDampAngle(mYaw, mYawTarget, ref mYawVelocity, _RotationSmoothing)) - mYaw;
            mYaw = mYaw + lYawDelta;

            // Use this frame's smoothed rotation
            if (lYawDelta != 0f)
            {
                rRotation = Quaternion.Euler(0f, lYawDelta, 0f);
            }
        }

        /// <summary>
        /// When we want to rotate based on the camera direction, we need to tweak the actor
        /// rotation AFTER we process the camera. Otherwise, we can get small stutters during camera rotation. 
        /// 
        /// This is the only way to keep them totally in sync. It also means we can't run any of our AC processing
        /// as the AC already ran. So, we do minimal work here
        /// </summary>
        /// <param name="rDeltaTime"></param>
        /// <param name="rUpdateCount"></param>
        /// <param name="rCamera"></param>
        private void OnCameraUpdated(float rDeltaTime, int rUpdateIndex, BaseCameraRig rCamera)
        {
            if (!_RotateWithCamera) { return; }
            if (mMotionController._CameraTransform == null) { return; }

            float lToCameraAngle = Vector3Ext.HorizontalAngleTo(mMotionController._Transform.forward, mMotionController._CameraTransform.forward, mMotionController._Transform.up);
            if (!mLinkRotation && Mathf.Abs(lToCameraAngle) <= _RotationToCameraSpeed * rDeltaTime) { mLinkRotation = true; }

            if (!mLinkRotation)
            {
                float lRotationAngle = Mathf.Abs(lToCameraAngle);
                float lRotationSign = Mathf.Sign(lToCameraAngle);
                lToCameraAngle = lRotationSign * Mathf.Min(_RotationToCameraSpeed * rDeltaTime, lRotationAngle);
            }

            Quaternion lRotation = Quaternion.AngleAxis(lToCameraAngle, Vector3.up);
            mActorController.Yaw = mActorController.Yaw * lRotation;
            mActorController._Transform.rotation = mActorController.Tilt * mActorController.Yaw;
        }

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

            if (EditorHelper.IntField("Form", "Sets the LXMotionForm animator property to determine the animation for the motion. If value is < 0, we use the Actor Core's 'Default Form' state.", Form, mMotionController))
            {
                lIsDirty = true;
                Form = EditorHelper.FieldIntValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Rotate With Camera", "Determines if we rotate to match the camera.", RotateWithCamera, mMotionController))
            {
                lIsDirty = true;
                RotateWithCamera = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.TextField("Rotate Action Alias", "Action alias determines if rotation is activated. This typically matches the input source's View Activator.", ActionAlias, mMotionController))
            {
                lIsDirty = true;
                ActionAlias = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.FloatField("Rotation Speed", "Degrees per second to rotate to the camera's direction.", RotationToCameraSpeed, mMotionController))
            {
                lIsDirty = true;
                RotationToCameraSpeed = EditorHelper.FieldFloatValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Rotate With View Input", "Determines if we rotate based on user input (view x).", RotateWithInput, mMotionController))
            {
                lIsDirty = true;
                RotateWithInput = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.FloatField("Rotation Speed", "Degrees per second to rotate the actor.", RotationSpeed, mMotionController))
            {
                lIsDirty = true;
                RotationSpeed = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField("Rotation Smoothing", "Smoothing factor applied to rotation (0 disables).", RotationSmoothing, mMotionController))
            {
                lIsDirty = true;
                RotationSmoothing = EditorHelper.FieldFloatValue;
            }

            return lIsDirty;
        }

#endif

        #region Pack Methods

        /// <summary>
        /// Name of the group these motions belong to
        /// </summary>
        public static string GroupName()
        {
            return "Basic";
        }

        #endregion

        #region Auto-Generated
        // ************************************ START AUTO GENERATED ************************************

        /// <summary>
        /// These declarations go inside the class so you can test for which state
        /// and transitions are active. Testing hash values is much faster than strings.
        /// </summary>
        public int STATE_Empty = -1;
        public int STATE_UnarmedIdlePose = -1;
        public int TRANS_AnyState_UnarmedIdlePose = -1;
        public int TRANS_EntryState_UnarmedIdlePose = -1;

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

                if (lTransitionID == 0)
                {
                    if (lStateID == STATE_Empty) { return true; }
                    if (lStateID == STATE_UnarmedIdlePose) { return true; }
                }

                if (lTransitionID == TRANS_AnyState_UnarmedIdlePose) { return true; }
                if (lTransitionID == TRANS_EntryState_UnarmedIdlePose) { return true; }
                if (lTransitionID == TRANS_AnyState_UnarmedIdlePose) { return true; }
                if (lTransitionID == TRANS_EntryState_UnarmedIdlePose) { return true; }
                return false;
            }
        }

        /// <summary>
        /// Used to determine if the actor is in one of the states for this motion
        /// </summary>
        /// <returns></returns>
        public override bool IsMotionState(int rStateID)
        {
            if (rStateID == STATE_Empty) { return true; }
            if (rStateID == STATE_UnarmedIdlePose) { return true; }
            return false;
        }

        /// <summary>
        /// Used to determine if the actor is in one of the states for this motion
        /// </summary>
        /// <returns></returns>
        public override bool IsMotionState(int rStateID, int rTransitionID)
        {
            if (rTransitionID == 0)
            {
                if (rStateID == STATE_Empty) { return true; }
                if (rStateID == STATE_UnarmedIdlePose) { return true; }
            }

            if (rTransitionID == TRANS_AnyState_UnarmedIdlePose) { return true; }
            if (rTransitionID == TRANS_EntryState_UnarmedIdlePose) { return true; }
            if (rTransitionID == TRANS_AnyState_UnarmedIdlePose) { return true; }
            if (rTransitionID == TRANS_EntryState_UnarmedIdlePose) { return true; }
            return false;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            string lLayer = mMotionController.Animator.GetLayerName(mMotionLayer._AnimatorLayerIndex);
            TRANS_AnyState_UnarmedIdlePose = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".BasicIdle-SM.Unarmed Idle Pose");
            TRANS_EntryState_UnarmedIdlePose = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".BasicIdle-SM.Unarmed Idle Pose");
            TRANS_AnyState_UnarmedIdlePose = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".BasicIdle-SM.Unarmed Idle Pose");
            TRANS_EntryState_UnarmedIdlePose = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".BasicIdle-SM.Unarmed Idle Pose");
            STATE_Empty = mMotionController.AddAnimatorName("" + lLayer + ".Empty");
            STATE_UnarmedIdlePose = mMotionController.AddAnimatorName("" + lLayer + ".BasicIdle-SM.Unarmed Idle Pose");
        }

#if UNITY_EDITOR

        /// <summary>
        /// New way to create sub-state machines without destroying what exists first.
        /// </summary>
        protected override void CreateStateMachine()
        {
            int rLayerIndex = mMotionLayer._AnimatorLayerIndex;
            MotionController rMotionController = mMotionController;

            UnityEditor.Animations.AnimatorController lController = null;

            Animator lAnimator = rMotionController.Animator;
            if (lAnimator == null) { lAnimator = rMotionController.gameObject.GetComponent<Animator>(); }
            if (lAnimator != null) { lController = lAnimator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController; }
            if (lController == null) { return; }

            while (lController.layers.Length <= rLayerIndex)
            {
                UnityEditor.Animations.AnimatorControllerLayer lNewLayer = new UnityEditor.Animations.AnimatorControllerLayer();
                lNewLayer.name = "Layer " + (lController.layers.Length + 1);
                lNewLayer.stateMachine = new UnityEditor.Animations.AnimatorStateMachine();
                lController.AddLayer(lNewLayer);
            }

            UnityEditor.Animations.AnimatorControllerLayer lLayer = lController.layers[rLayerIndex];

            UnityEditor.Animations.AnimatorStateMachine lLayerStateMachine = lLayer.stateMachine;

            UnityEditor.Animations.AnimatorStateMachine lSSM_37922 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "BasicIdle-SM");
            if (lSSM_37922 == null) { lSSM_37922 = lLayerStateMachine.AddStateMachine("BasicIdle-SM", new Vector3(192, -1056, 0)); }

            UnityEditor.Animations.AnimatorState lState_38398 = MotionControllerMotion.EditorFindState(lSSM_37922, "Unarmed Idle Pose");
            if (lState_38398 == null) { lState_38398 = lSSM_37922.AddState("Unarmed Idle Pose", new Vector3(312, 84, 0)); }
            lState_38398.speed = 1f;
            lState_38398.mirror = false;
            lState_38398.tag = "";
            lState_38398.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx", "IdlePose");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38106 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38398, 0);
            if (lAnyTransition_38106 == null) { lAnyTransition_38106 = lLayerStateMachine.AddAnyStateTransition(lState_38398); }
            lAnyTransition_38106.isExit = false;
            lAnyTransition_38106.hasExitTime = false;
            lAnyTransition_38106.hasFixedDuration = true;
            lAnyTransition_38106.exitTime = 0.75f;
            lAnyTransition_38106.duration = 0.1f;
            lAnyTransition_38106.offset = 0f;
            lAnyTransition_38106.mute = false;
            lAnyTransition_38106.solo = false;
            lAnyTransition_38106.canTransitionToSelf = true;
            lAnyTransition_38106.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38106.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38106.RemoveCondition(lAnyTransition_38106.conditions[i]); }
            lAnyTransition_38106.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3000f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38106.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");
            lAnyTransition_38106.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38108 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38398, 1);
            if (lAnyTransition_38108 == null) { lAnyTransition_38108 = lLayerStateMachine.AddAnyStateTransition(lState_38398); }
            lAnyTransition_38108.isExit = false;
            lAnyTransition_38108.hasExitTime = false;
            lAnyTransition_38108.hasFixedDuration = true;
            lAnyTransition_38108.exitTime = 0.75f;
            lAnyTransition_38108.duration = 0f;
            lAnyTransition_38108.offset = 0f;
            lAnyTransition_38108.mute = false;
            lAnyTransition_38108.solo = false;
            lAnyTransition_38108.canTransitionToSelf = true;
            lAnyTransition_38108.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38108.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38108.RemoveCondition(lAnyTransition_38108.conditions[i]); }
            lAnyTransition_38108.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3000f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38108.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");
            lAnyTransition_38108.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

#if USE_ARCHERY_MP || OOTII_AYMP
            ArcheryPackDefinition.ExtendBasicIdle(rMotionController, rLayerIndex);
#endif

#if USE_SWORD_SHIELD_MP || OOTII_SSMP
            SwordShieldPackDefinition.ExtendBasicIdle(rMotionController, rLayerIndex);
#endif

#if USE_SPELL_CASTING_MP || OOTII_SCMP
            SpellCastingPackDefinition.ExtendBasicIdle(rMotionController, rLayerIndex);
#endif

#if USE_SHOOTER_MP || OOTII_SHMP
            ShooterPackDefinition.ExtendBasicIdle(rMotionController, rLayerIndex);
#endif

            // Run any post processing after creating the state machine
            OnStateMachineCreated();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}
