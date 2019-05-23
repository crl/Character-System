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
    /// </summary>
    [MotionName("Basic Walk Run Pivot")]
    [MotionDescription("Adventure game style movement that can be expanded. Uses no transitions.")]
    public class BasicWalkRunPivot : MotionControllerMotion, IWalkRunMotion
    {
        /// <summary>
        /// Trigger values for th emotion
        /// </summary>
        public int PHASE_UNKNOWN = 0;
        public int PHASE_START = 3050;
        public int PHASE_STOP = 3099;

        /// <summary>
        /// Determines if we're using the IsInMotion() function to verify that
        /// the transition in the animator has occurred for this motion.
        /// </summary>
        public override bool VerifyTransition
        {
            get { return false; }
        }

        /// <summary>
        /// Determines if we run by default or walk
        /// </summary>
        public bool _DefaultToRun = false;
        public bool DefaultToRun
        {
            get { return _DefaultToRun; }
            set { _DefaultToRun = value; }
        }

        /// <summary>
        /// Speed (units per second) when walking
        /// </summary>
        public float _WalkSpeed = 0f;
        public virtual float WalkSpeed
        {
            get { return _WalkSpeed; }
            set { _WalkSpeed = value; }
        }

        /// <summary>
        /// Speed (units per second) when running
        /// </summary>
        public float _RunSpeed = 0f;
        public virtual float RunSpeed
        {
            get { return _RunSpeed; }
            set { _RunSpeed = value; }
        }

        /// <summary>
        /// Degrees per second to rotate the actor in order to face the input direction
        /// </summary>
        public float _RotationSpeed = 360f;
        public float RotationSpeed
        {
            get { return _RotationSpeed; }
            set { _RotationSpeed = value; }
        }

        /// <summary>
        /// Determines if we shortcut the motion and start in the loop
        /// </summary>
        private bool mStartInMove = false;
        public bool StartInMove
        {
            get { return mStartInMove; }
            set { mStartInMove = value; }
        }

        /// <summary>
        /// Determines if we shortcut the motion and start in a run
        /// </summary>
        private bool mStartInWalk = false;
        public bool StartInWalk
        {
            get { return mStartInWalk; }

            set
            {
                mStartInWalk = value;
                if (value) { mStartInMove = value; }
            }
        }

        /// <summary>
        /// Determines if we shortcut the motion and start in a run
        /// </summary>
        private bool mStartInRun = false;
        public bool StartInRun
        {
            get { return mStartInRun; }

            set
            {
                mStartInRun = value;
                if (value) { mStartInMove = value; }
            }
        }

        /// <summary>
        /// Number of samples to use for smoothing
        /// </summary>
        public int _SmoothingSamples = 10;
        public int SmoothingSamples
        {
            get { return _SmoothingSamples; }

            set
            {
                _SmoothingSamples = value;

                mInputX.SampleCount = _SmoothingSamples;
                mInputY.SampleCount = _SmoothingSamples;
                mInputMagnitude.SampleCount = _SmoothingSamples;
            }
        }
        
        /// <summary>
        /// Determines if the actor should be running based on input
        /// </summary>
        public virtual bool IsRunActive
        {
            get
            {
                if (mMotionController.TargetNormalizedSpeed > 0f && mMotionController.TargetNormalizedSpeed <= 0.5f) { return false; }
                if (mMotionController._InputSource == null) { return _DefaultToRun; }
                return ((_DefaultToRun && !mMotionController._InputSource.IsPressed(_ActionAlias)) || (!_DefaultToRun && mMotionController._InputSource.IsPressed(_ActionAlias)));
            }
        }

        /// <summary>
        /// We use these classes to help smooth the input values so that
        /// movement doesn't drop from 1 to 0 immediately.
        /// </summary>
        protected FloatValue mInputX = new FloatValue(0f, 10);
        protected FloatValue mInputY = new FloatValue(0f, 10);
        protected FloatValue mInputMagnitude = new FloatValue(0f, 15);

        // Track how long we've been in the idle state
        protected float mIdleTime = 0f;

        // Determine if the rotation is locked to the camera
        protected bool mIsRotationLocked = false;

        // Used to force a change if neede
        protected int mActiveForm = 0;

        /// <summary>
        /// Default constructor
        /// </summary>
        public BasicWalkRunPivot()
            : base()
        {
            _Category = EnumMotionCategories.WALK;

            _Priority = 5;
            _ActionAlias = "Run";
            _Form = -1;

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "BasicWalkRunPivot-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public BasicWalkRunPivot(MotionController rController)
            : base(rController)
        {
            _Category = EnumMotionCategories.WALK;

            _Priority = 5;
            _ActionAlias = "Run";
            _Form = -1;

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "BasicWalkRunPivot-SM"; }
#endif
        }

        /// <summary>
        /// Awake is called after all objects are initialized so you can safely speak to other objects. This is where
        /// reference can be associated.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            // Initialize the smoothing variables
            SmoothingSamples = _SmoothingSamples;
        }

        /// <summary>
        /// Tests if this motion should be started. However, the motion
        /// isn't actually started.
        /// </summary>
        /// <returns></returns>
        public override bool TestActivate()
        {
            if (!mIsStartable) { return false; }
            if (!mMotionController.IsGrounded) { return false; }
            
            // If there's enough movement, start the motion
            if (mMotionController.State.InputMagnitudeTrend.Value > 0.49f)
            {
                return true;
            }

            // Don't activate
            return false;
        }

        /// <summary>
        /// Tests if the motion should continue. If it shouldn't, the motion
        /// is typically disabled
        /// </summary>
        /// <returns></returns>
        public override bool TestUpdate()
        {
            if (mIsActivatedFrame) { return true; }
            if (!mMotionController.IsGrounded) { return false; }

            // If we're down to no movement, we can exit
            if (mInputMagnitude.Average == 0f)
            {
                // However, add a small delay to ensure we're not coming back
                mIdleTime = mIdleTime + Time.deltaTime;
                if (mIdleTime > 0.2f)
                {
                    return false;
                }
            }
            else
            {
                mIdleTime = 0f;
            }

            // Stay
            return true;
        }

        /// <summary>
        /// Raised when a motion is being interrupted by another motion
        /// </summary>
        /// <param name="rMotion">Motion doing the interruption</param>
        /// <returns>Boolean determining if it can be interrupted</returns>
        public override bool TestInterruption(MotionControllerMotion rMotion)
        {
            // Since we're dealing with a blend tree, keep the value until the transition completes            
            mMotionController.ForcedInput.x = mInputX.Average;
            mMotionController.ForcedInput.y = mInputY.Average;

            return true;
        }

        /// <summary>
        /// Called to start the specific motion. If the motion
        /// were something like 'jump', this would start the jumping process
        /// </summary>
        /// <param name="rPrevMotion">Motion that this motion is taking over from</param>
        public override bool Activate(MotionControllerMotion rPrevMotion)
        {
            mIdleTime = 0f;
            mIsRotationLocked = false;

            mInputX.Clear();
            mInputY.Clear();
            mInputMagnitude.Clear();

            // Update the max speed based on our animation
            mMotionController.MaxSpeed = 5.668f;

            // Start the motion
            mActiveForm = (_Form >= 0 ? _Form : mMotionController.CurrentForm);
            mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, mActiveForm, 0, true);

            // Register this motion with the camera
            if (mMotionController.CameraRig is BaseCameraRig)
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
            // Clear out the start
            mStartInRun = false;
            mStartInWalk = false;

            // Unregister this motion with the camera
            if (mMotionController.CameraRig is BaseCameraRig)
            {
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
            }

            // Finalize the deactivation
            base.Deactivate();
        }

        /// <summary>
        /// Allows the motion to modify the velocity before it is applied. 
        /// 
        /// NOTE:
        /// Be careful when removing rotations
        /// as some transitions will want rotations even if the state they are transitioning from don't.
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        /// <param name="rMovement">Amount of movement caused by root motion this frame</param>
        /// <param name="rRotation">Amount of rotation caused by root motion this frame</param>
        /// <returns></returns>
        public override void UpdateRootMotion(float rDeltaTime, int rUpdateIndex, ref Vector3 rMovement, ref Quaternion rRotation)
        {
            rRotation = Quaternion.identity;

            // Override root motion if we're meant to
            float lMovementSpeed = (IsRunActive ? _RunSpeed : _WalkSpeed);
            if (lMovementSpeed > 0f)
            {
                if (rMovement.sqrMagnitude > 0f)
                {
                    rMovement = rMovement.normalized * (lMovementSpeed * rDeltaTime);
                }
                else
                {
                    Vector3 lDirection = new Vector3(0f, 0f, 1f);
                    rMovement = lDirection.normalized * (lMovementSpeed * rDeltaTime);
                }

                rMovement.x = 0f;
                rMovement.y = 0f;
                if (rMovement.z < 0f) { rMovement.z = 0f; }
            }
            // Ensure we only move forward with this style
            else
            {
                rMovement.x = 0f;
                rMovement.y = 0f;
                if (rMovement.z < 0f) { rMovement.z = 0f; }
                if (mMotionController.State.InputMagnitudeTrend.Value < 0.05f) { rMovement.z = 0f; }
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
            mRotation = Quaternion.identity;

            // Smooth the input so we don't start and stop immediately in the blend tree.
            SmoothInput();

            // Use the AC to rotate the character towards the input
            RotateToInput(mMotionController.State.InputFromAvatarAngle, rDeltaTime, ref mRotation);

            // Force a style change if needed
            if (_Form <= 0 && mActiveForm != mMotionController.CurrentForm)
            {
                mActiveForm = mMotionController.CurrentForm;
                mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, mActiveForm, 0, true);
            }
        }

        /// <summary>
        /// We smooth the input so that we don't start and stop immediately in the blend tree. That can create pops.
        /// </summary>
        protected void SmoothInput()
        {
            MotionState lState = mMotionController.State;

            // Convert the input to radial so we deal with keyboard and gamepad input the same.
            float lInputMax = (IsRunActive ? 1f : 0.5f);

            float lInputX = Mathf.Clamp(lState.InputX, -lInputMax, lInputMax);
            float lInputY = Mathf.Clamp(lState.InputY, -lInputMax, lInputMax);
            float lInputMagnitude = Mathf.Clamp(lState.InputMagnitudeTrend.Value, 0f, lInputMax);
            InputManagerHelper.ConvertToRadialInput(ref lInputX, ref lInputY, ref lInputMagnitude);

            // Smooth the input
            mInputX.Add(lInputX);
            mInputY.Add(lInputY);
            mInputMagnitude.Add(lInputMagnitude);

            // Modify the input values to add some lag
            mMotionController.State.InputX = mInputX.Average;
            mMotionController.State.InputY = mInputY.Average;
            mMotionController.State.InputMagnitudeTrend.Replace(mInputMagnitude.Average);
        }

        /// <summary>
        /// Create a rotation velocity that rotates the character based on input
        /// </summary>
        /// <param name="rInputFromAvatarAngle"></param>
        /// <param name="rDeltaTime"></param>
        protected void RotateToInput(float rInputFromAvatarAngle, float rDeltaTime, ref Quaternion rRotation)
        {
            if (rInputFromAvatarAngle != 0f)
            {
                if (_RotationSpeed > 0f && Mathf.Abs(rInputFromAvatarAngle) > _RotationSpeed * rDeltaTime)
                {
                    rInputFromAvatarAngle = Mathf.Sign(rInputFromAvatarAngle) * _RotationSpeed * rDeltaTime;
                }

                rRotation = Quaternion.Euler(0f, rInputFromAvatarAngle, 0f);
            }
        }

        /// <summary>
        /// When we want to rotate based on the camera direction (which input does), we need to tweak the actor
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
            if (mMotionController._CameraTransform == null) { return; }

            // Get out early if we we aren't modifying the view.
            if (mMotionController._InputSource != null && mMotionController._InputSource.ViewX == 0f) { return; }

            // We do the inverse tilt so we calculate the rotation in "natural up" space vs. "actor up" space. 
            Quaternion lInvTilt = QuaternionExt.FromToRotation(mMotionController._Transform.up, Vector3.up);

            // Forward direction of the actor in "natural up"
            Vector3 lControllerForward = lInvTilt * mMotionController._Transform.forward;

            // Camera forward in "natural up"
            Vector3 lCameraForward = lInvTilt * mMotionController._CameraTransform.forward;

            // Create a quaternion that gets us from our world-forward to our camera direction.
            Quaternion lToCamera = Quaternion.LookRotation(lCameraForward, Vector3.up);

            // Transform joystick from world space to camera space. Now the input is relative
            // to how the camera is facing.
            Vector3 lMoveDirection = lToCamera * mMotionController.State.InputForward;
            float lInputFromAvatarAngle = NumberHelper.GetHorizontalAngle(lControllerForward, lMoveDirection);

            // Clear the link if we're out of rotation range
            if (Mathf.Abs(lInputFromAvatarAngle) > _RotationSpeed * rDeltaTime * 5f) { mIsRotationLocked = false; }

            // We only want to do this is we're very very close to the desired angle. This will remove any stuttering
            if (_RotationSpeed == 0f || mIsRotationLocked || Mathf.Abs(lInputFromAvatarAngle) < _RotationSpeed * rDeltaTime * 1f)
            {
                mIsRotationLocked = true;

                // Since we're after the camera update, we have to force the rotation outside the normal flow
                Quaternion lRotation = Quaternion.AngleAxis(lInputFromAvatarAngle, Vector3.up);
                mActorController.Yaw = mActorController.Yaw * lRotation;
                mActorController._Transform.rotation = mActorController.Tilt * mActorController.Yaw;
            }
        }

        #region Editor Functions

        // **************************************************************************************************
        // Following properties and function only valid while editing
        // **************************************************************************************************

#if UNITY_EDITOR
        
        /// <summary>
        /// Allow the motion to render it's own GUI
        /// </summary>
        public override bool OnInspectorGUI()
        {
            bool lIsDirty = false;

            if (EditorHelper.IntField("Form", "Sets the LXMotionForm animator property to determine the animation for the motion. If value is < 0, we use the Actor Core's 'Default Form' state.", Form, mMotionController))
            {
                lIsDirty = true;
                Form = EditorHelper.FieldIntValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Default to Run", "Determines if the default is to run or walk.", DefaultToRun, mMotionController))
            {
                lIsDirty = true;
                DefaultToRun = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.TextField("Action Alias", "Action alias that triggers a run or walk (which ever is opposite the default).", ActionAlias, mMotionController))
            {
                lIsDirty = true;
                ActionAlias = EditorHelper.FieldStringValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.FloatField("Walk Speed", "Speed (units per second) to move when walking. Set to 0 to use root-motion.", WalkSpeed, mMotionController))
            {
                lIsDirty = true;
                WalkSpeed = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField("Run Speed", "Speed (units per second) to move when running. Set to 0 to use root-motion.", RunSpeed, mMotionController))
            {
                lIsDirty = true;
                RunSpeed = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField("Rotation Speed", "Degrees per second to rotate the actor ('0' means instant rotation).", RotationSpeed, mMotionController))
            {
                lIsDirty = true;
                RotationSpeed = EditorHelper.FieldFloatValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.IntField("Smoothing Samples", "The more samples the smoother movement is, but the less responsive.", SmoothingSamples, mMotionController))
            {
                lIsDirty = true;
                SmoothingSamples = EditorHelper.FieldIntValue;
            }

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
        public int STATE_Empty = -1;
        public int STATE_UnarmedBlendTree = -1;
        public int TRANS_AnyState_UnarmedBlendTree = -1;
        public int TRANS_EntryState_UnarmedBlendTree = -1;

        /// <summary>
        /// Determines if we're using auto-generated code
        /// </summary>
        public override bool HasAutoGeneratedCode
        {
            get { return true; }
        }

        /// <summary>
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
                    if (lStateID == STATE_UnarmedBlendTree) { return true; }
                }

                if (lTransitionID == TRANS_AnyState_UnarmedBlendTree) { return true; }
                if (lTransitionID == TRANS_EntryState_UnarmedBlendTree) { return true; }
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
            if (rStateID == STATE_UnarmedBlendTree) { return true; }
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
                if (rStateID == STATE_UnarmedBlendTree) { return true; }
            }

            if (rTransitionID == TRANS_AnyState_UnarmedBlendTree) { return true; }
            if (rTransitionID == TRANS_EntryState_UnarmedBlendTree) { return true; }
            return false;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            string lLayer = mMotionController.Animator.GetLayerName(mMotionLayer._AnimatorLayerIndex);
            TRANS_AnyState_UnarmedBlendTree = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".BasicWalkRunPivot-SM.Unarmed BlendTree");
            TRANS_EntryState_UnarmedBlendTree = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".BasicWalkRunPivot-SM.Unarmed BlendTree");
            STATE_Empty = mMotionController.AddAnimatorName("" + lLayer + ".Empty");
            STATE_UnarmedBlendTree = mMotionController.AddAnimatorName("" + lLayer + ".BasicWalkRunPivot-SM.Unarmed BlendTree");
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

            UnityEditor.Animations.AnimatorStateMachine lSSM_37924 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "BasicWalkRunPivot-SM");
            if (lSSM_37924 == null) { lSSM_37924 = lLayerStateMachine.AddStateMachine("BasicWalkRunPivot-SM", new Vector3(408, -1056, 0)); }

            UnityEditor.Animations.AnimatorState lState_38400 = MotionControllerMotion.EditorFindState(lSSM_37924, "Unarmed BlendTree");
            if (lState_38400 == null) { lState_38400 = lSSM_37924.AddState("Unarmed BlendTree", new Vector3(312, 72, 0)); }
            lState_38400.speed = 1f;
            lState_38400.mirror = false;
            lState_38400.tag = "";

            UnityEditor.Animations.BlendTree lM_25576 = MotionControllerMotion.EditorCreateBlendTree("Blend Tree", lController, rLayerIndex);
            lM_25576.blendType = UnityEditor.Animations.BlendTreeType.Simple1D;
            lM_25576.blendParameter = "InputMagnitude";
            lM_25576.blendParameterY = "InputX";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
            lM_25576.useAutomaticThresholds = false;
#endif
            lM_25576.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx", "IdlePose"), 0f);
            lM_25576.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_WalkFWD_v2.fbx", "WalkForward"), 0.5f);
            lM_25576.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunForward_v2.fbx", "RunForward"), 1f);
            lState_38400.motion = lM_25576;

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38110 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38400, 0);
            if (lAnyTransition_38110 == null) { lAnyTransition_38110 = lLayerStateMachine.AddAnyStateTransition(lState_38400); }
            lAnyTransition_38110.isExit = false;
            lAnyTransition_38110.hasExitTime = false;
            lAnyTransition_38110.hasFixedDuration = true;
            lAnyTransition_38110.exitTime = 0.75f;
            lAnyTransition_38110.duration = 0.25f;
            lAnyTransition_38110.offset = 0f;
            lAnyTransition_38110.mute = false;
            lAnyTransition_38110.solo = false;
            lAnyTransition_38110.canTransitionToSelf = true;
            lAnyTransition_38110.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38110.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38110.RemoveCondition(lAnyTransition_38110.conditions[i]); }
            lAnyTransition_38110.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3050f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38110.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");

#if USE_ARCHERY_MP || OOTII_AYMP
            ArcheryPackDefinition.ExtendBasicWalkRunPivot(rMotionController, rLayerIndex);
#endif

#if USE_SWORD_SHIELD_MP || OOTII_SSMP
            SwordShieldPackDefinition.ExtendBasicWalkRunPivot(rMotionController, rLayerIndex);
#endif

#if USE_SPELL_CASTING_MP || OOTII_SCMP
            SpellCastingPackDefinition.ExtendBasicWalkRunPivot(rMotionController, rLayerIndex);
#endif

#if USE_SHOOTER_MP || OOTII_SHMP
            ShooterPackDefinition.ExtendBasicWalkRunPivot(rMotionController, rLayerIndex);
#endif

            // Run any post processing after creating the state machine
            OnStateMachineCreated();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}
