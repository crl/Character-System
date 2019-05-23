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
    /// The sneak is a slow move that keeps the character facing forward.
    /// 
    /// This motion will force the camera into the third-person-fixed mode.
    /// </summary>
    [MotionName("Sneak")]
    [MotionDescription("A forward facing motion that looks like the actor is sneaking. The motion is slower than a walk and has the actor strafe instead of turn.")]
    public class Sneak_v2 : MotionControllerMotion
    {
        // Values to average
        public const int SMOOTHING_BASE = 20;
        
        // Enum values for the motion
        public const int PHASE_UNKNOWN = 0;
        public const int PHASE_START = 620;
        public const int PHASE_START_OLD = 600;
        public const int PHASE_END = 610;

        /// <summary>
        /// Units per second to move. Set to '0' to use root-motion
        /// </summary>
        public float _MovementSpeed = 0f;
        public float MovementSpeed
        {
            get { return _MovementSpeed; }
            set { _MovementSpeed = value; }
        }

        /// <summary>
        /// Determines if the actor rotates based on the input
        /// </summary>
        public bool _RotateWithInput = false;
        public bool RotateWithInput
        {
            get { return _RotateWithInput; }

            set
            {
                _RotateWithInput = value;
                if (_RotateWithInput) { _RotateWithCamera = false; }
            }
        }

        /// <summary>
        /// Determines if the actor rotates to face the direction the
        /// camera is facing
        /// </summary>
        public bool _RotateWithCamera = true;
        public bool RotateWithCamera
        {
            get { return _RotateWithCamera; }

            set
            {
                _RotateWithCamera = value;
                if (_RotateWithCamera) { _RotateWithInput = false; }
            }
        }

        /// <summary>
        /// Desired degrees of rotation per second
        /// </summary>
        public float _RotationSpeed = 180f;
        public float RotationSpeed
        {
            get { return _RotationSpeed; }
            set { _RotationSpeed = value; }
        }

        /// <summary>
        /// Number of samples to use for smoothing
        /// </summary>
        public int _SmoothingSamples = 20;
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
        /// Determines if the actor rotation should be linked to the camera
        /// </summary>
        protected bool mLinkRotation = false;

        /// <summary>
        /// Fields to help smooth out the mouse rotation
        /// </summary>
        protected float mYaw = 0f;
        protected float mYawTarget = 0f;
        protected float mYawVelocity = 0f;

        /// <summary>
        /// We use these classes to help smooth the input values so that
        /// movement doesn't drop from 1 to 0 immediately.
        /// </summary>
        protected FloatValue mInputX = new FloatValue(0f, SMOOTHING_BASE);
        protected FloatValue mInputY = new FloatValue(0f, SMOOTHING_BASE);
        protected FloatValue mInputMagnitude = new FloatValue(0f, SMOOTHING_BASE);

        /// <summary>
        /// Store the stance we're coming from
        /// </summary>
        protected int mStoredStance = EnumControllerStance.TRAVERSAL;

        /// <summary>
        /// Default constructor
        /// </summary>
        public Sneak_v2()
            : base()
        {
            _Priority = 6;
            _ActionAlias = "ChangeStance";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "Sneak v2-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public Sneak_v2(MotionController rController)
            : base(rController)
        {
            _Priority = 6;
            _ActionAlias = "ChangeStance";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "Sneak v2-SM"; }
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

            if (mMotionLayer.ActiveMotion == null)
            {
                if (mActorController.State.Stance == EnumControllerStance.STEALTH)
                {
                    return true;
                }
            }

            // Test for an input change
            if (mMotionController._InputSource != null && mMotionController._InputSource.IsEnabled)
            {
                if (mMotionController._InputSource.IsJustPressed(_ActionAlias))
                {
                    if (mActorController.State.Stance != EnumControllerStance.STEALTH)
                    {
                        mStoredStance = mActorController.State.Stance;
                        mActorController.State.Stance = EnumControllerStance.STEALTH;
                        return true;
                    }
                    else
                    {
                        mActorController.State.Stance = mStoredStance;
                    }
                }
            }
            
            // If we get here, we should not be in the stance
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
            if (mIsAnimatorActive && !IsInMotionState) { return false; }
            if (mActorController.State.Stance != EnumControllerStance.STEALTH) { return false; }

            // Test if we stop sneak due to a change in state
            if (mMotionLayer._AnimatorStateID == STATE_IdlePose)
            {
                mActorController.State.Stance = mStoredStance;
                return false;
            }

            // Otherwise, stay in
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
            mLinkRotation = false;

            // Force the character's stance to change
            mActorController.State.Stance = EnumControllerStance.STEALTH;

            // Trigger the change in the animator
            mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, true);

            // Register this motion with the camera
            if (_RotateWithCamera && mMotionController.CameraRig is BaseCameraRig)
            {
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate += OnCameraUpdated;
            }

            // Allow the base to finish
            return base.Activate(rPrevMotion);
        }

        /// <summary>
        /// Called to stop the motion. If the motion is stopable. Some motions
        /// like jump cannot be stopped early
        /// </summary>
        public override void Deactivate()
        {
            // Register this motion with the camera
            if (mMotionController.CameraRig is BaseCameraRig)
            {
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
            }

            // Deactivate
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
            rRotation = Quaternion.identity;

            // Override root motion if we're meant to
            if (_MovementSpeed > 0f)
            {
                rMovement.x = mMotionController.State.InputX;
                rMovement.y = 0f;
                rMovement.z = mMotionController.State.InputY;
                rMovement = rMovement.normalized * (_MovementSpeed * rDeltaTime);
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

            // Test if we stop sneak due to a change in stance. We do this here to transition
            if (mMotionController.State.AnimatorStates[mMotionLayer._AnimatorLayerIndex].MotionPhase == 0 && mMotionController._InputSource != null && mMotionController._InputSource.IsEnabled)
            {
                if (mMotionController._InputSource.IsJustPressed(_ActionAlias))
                {
                    // We have to use the forced value our we'll change the current blend value as we transition
                    mMotionController.ForcedInput.x = mInputX.Average;
                    mMotionController.ForcedInput.y = mInputY.Average;
                    mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, PHASE_END, 0, true);
                }
            }

            // Grab the state info
            MotionState lState = mMotionController.State;

            // Convert the input to radial so we deal with keyboard and gamepad input the same.
            float lInputX = lState.InputX;
            float lInputY = lState.InputY;
            float lInputMagnitude = lState.InputMagnitudeTrend.Value;
            InputManagerHelper.ConvertToRadialInput(ref lInputX, ref lInputY, ref lInputMagnitude, 0.5f);

            // Smooth the input
            mInputX.Add(lInputX);
            mInputY.Add(lInputY);
            mInputMagnitude.Add(lInputMagnitude);

            // Use the smoothed values for input
            mMotionController.State.InputX = mInputX.Average;
            mMotionController.State.InputY = mInputY.Average;
            mMotionController.State.InputMagnitudeTrend.Replace(mInputMagnitude.Average);

            // If we're not dealing with an ootii camera rig, we need to rotate to the camera here
            if (_RotateWithCamera && !(mMotionController.CameraRig is BaseCameraRig))
            {
                OnCameraUpdated(rDeltaTime, rUpdateIndex, null);
            }

            // Rotate as needed
            if (!_RotateWithCamera && _RotateWithInput)
            {
                RotateUsingInput(rDeltaTime, ref mRotation);
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
            float lYawSmoothing = 0.1f;

            if (mMotionController._InputSource.IsViewingActivated)
            {
                lYawDelta = mMotionController._InputSource.ViewX * _RotationSpeed * rDeltaTime;
            }

            mYawTarget = mYawTarget + lYawDelta;

            // Smooth the rotation
            lYawDelta = (lYawSmoothing <= 0f ? mYawTarget : Mathf.SmoothDampAngle(mYaw, mYawTarget, ref mYawVelocity, lYawSmoothing)) - mYaw;
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
            if (mMotionController._CameraTransform == null) { return; }

            float lToCameraAngle = Vector3Ext.HorizontalAngleTo(mMotionController._Transform.forward, mMotionController._CameraTransform.forward, mMotionController._Transform.up);
            if (!mLinkRotation && Mathf.Abs(lToCameraAngle) <= _RotationSpeed * rDeltaTime) { mLinkRotation = true; }

            if (!mLinkRotation)
            {
                float lRotationAngle = Mathf.Abs(lToCameraAngle);
                float lRotationSign = Mathf.Sign(lToCameraAngle);
                lToCameraAngle = lRotationSign * Mathf.Min(_RotationSpeed * rDeltaTime, lRotationAngle);
            }

            Quaternion lRotation = Quaternion.AngleAxis(lToCameraAngle, Vector3.up);
            mActorController.Yaw = mActorController.Yaw * lRotation;
            mActorController._Transform.rotation = mActorController.Tilt * mActorController.Yaw;
        }

        #region Editor Functions

        // **************************************************************************************************
        // Following properties and function only valid while editing
        // **************************************************************************************************

#if UNITY_EDITOR

        /// <summary>
        /// Creates input settings in the Unity Input Manager
        /// </summary>
        public override void CreateInputManagerSettings()
        {
            if (!InputManagerHelper.IsDefined(_ActionAlias))
            {
                InputManagerEntry lEntry = new InputManagerEntry();
                lEntry.Name = _ActionAlias;
                lEntry.PositiveButton = "t";
                lEntry.Gravity = 1000;
                lEntry.Dead = 0.001f;
                lEntry.Sensitivity = 1000;
                lEntry.Type = InputManagerEntryType.KEY_MOUSE_BUTTON;
                lEntry.Axis = 0;
                lEntry.JoyNum = 0;

                InputManagerHelper.AddEntry(lEntry, true);

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX

            lEntry = new InputManagerEntry();
            lEntry.Name = _ActionAlias;
            lEntry.PositiveButton = "joystick button 11"; // Left stick click
            lEntry.Gravity = 1;
            lEntry.Dead = 0.3f;
            lEntry.Sensitivity = 1;
            lEntry.Type = InputManagerEntryType.KEY_MOUSE_BUTTON;
            lEntry.Axis = 0;
            lEntry.JoyNum = 0;

            InputManagerHelper.AddEntry(lEntry, true);

#else

                lEntry = new InputManagerEntry();
                lEntry.Name = _ActionAlias;
                lEntry.PositiveButton = "joystick button 8"; // Left stick click
                lEntry.Gravity = 1;
                lEntry.Dead = 0.3f;
                lEntry.Sensitivity = 1;
                lEntry.Type = InputManagerEntryType.KEY_MOUSE_BUTTON;
                lEntry.Axis = 0;
                lEntry.JoyNum = 0;

                InputManagerHelper.AddEntry(lEntry, true);

#endif
            }
        }

        /// <summary>
        /// Allow the constraint to render it's own GUI
        /// </summary>
        /// <returns>Reports if the object's value was changed</returns>
        public override bool OnInspectorGUI()
        {
            bool lIsDirty = false;

            if (EditorHelper.TextField("Action Alias", "Action alias that triggers the motion.", ActionAlias, mMotionController))
            {
                lIsDirty = true;
                ActionAlias = EditorHelper.FieldStringValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.FloatField("Movement Speed", "Units per second to move. '0' means use root-motion if it exists. Another option is to set the speed values on the animator state nodes.", MovementSpeed, mMotionController))
            {
                lIsDirty = true;
                MovementSpeed = EditorHelper.FieldFloatValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Rotate with Camera", "Determines if the actor rotates to face the direction of the camera.", RotateWithCamera, mMotionController))
            {
                lIsDirty = true;
                RotateWithCamera = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("Rotate with Input", "Determines if the actor rotates based on the input.", RotateWithInput, mMotionController))
            {
                lIsDirty = true;
                RotateWithInput = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.FloatField("Rotation Speed", "Degrees per second to rotate.", RotationSpeed, mMotionController))
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
        public static int STATE_IdlePose = -1;
        public static int STATE_MoveTree = -1;
        public static int TRANS_AnyState_MoveTree = -1;
        public static int TRANS_EntryState_MoveTree = -1;
        public static int TRANS_MoveTree_IdlePose = -1;

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

                if (lStateID == STATE_IdlePose && lTransitionID == 0) { return true; }
                if (lStateID == STATE_MoveTree && lTransitionID == 0) { return true; }
                if (lTransitionID == TRANS_AnyState_MoveTree) { return true; }
                if (lTransitionID == TRANS_EntryState_MoveTree) { return true; }
                if (lTransitionID == TRANS_MoveTree_IdlePose) { return true; }
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
            if (rStateID == STATE_MoveTree) { return true; }
            return false;
        }

        /// <summary>
        /// Used to determine if the actor is in one of the states for this motion
        /// </summary>
        /// <returns></returns>
        public override bool IsMotionState(int rStateID, int rTransitionID)
        {
            if (rStateID == STATE_IdlePose && rTransitionID == 0) { return true; }
            if (rStateID == STATE_MoveTree && rTransitionID == 0) { return true; }
            if (rTransitionID == TRANS_AnyState_MoveTree) { return true; }
            if (rTransitionID == TRANS_EntryState_MoveTree) { return true; }
            if (rTransitionID == TRANS_MoveTree_IdlePose) { return true; }
            return false;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            TRANS_AnyState_MoveTree = mMotionController.AddAnimatorName("AnyState -> Base Layer.Sneak v2-SM.Move Tree");
            TRANS_EntryState_MoveTree = mMotionController.AddAnimatorName("Entry -> Base Layer.Sneak v2-SM.Move Tree");
            STATE_IdlePose = mMotionController.AddAnimatorName("Base Layer.Sneak v2-SM.IdlePose");
            STATE_MoveTree = mMotionController.AddAnimatorName("Base Layer.Sneak v2-SM.Move Tree");
            TRANS_MoveTree_IdlePose = mMotionController.AddAnimatorName("Base Layer.Sneak v2-SM.Move Tree -> Base Layer.Sneak v2-SM.IdlePose");
        }

#if UNITY_EDITOR

        private AnimationClip m14306 = null;
        private AnimationClip m14708 = null;
        private AnimationClip m14710 = null;
        private AnimationClip m14712 = null;
        private AnimationClip m14716 = null;
        private AnimationClip m14714 = null;
        private AnimationClip m14718 = null;
        private AnimationClip m14704 = null;
        private AnimationClip m14706 = null;
        private AnimationClip m14702 = null;

        /// <summary>
        /// Creates the animator substate machine for this motion.
        /// </summary>
        protected override void CreateStateMachine()
        {
            // Grab the root sm for the layer
            UnityEditor.Animations.AnimatorStateMachine lRootStateMachine = _EditorAnimatorController.layers[mMotionLayer.AnimatorLayerIndex].stateMachine;
            UnityEditor.Animations.AnimatorStateMachine lSM_21500 = _EditorAnimatorController.layers[mMotionLayer.AnimatorLayerIndex].stateMachine;
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

            UnityEditor.Animations.AnimatorStateMachine lSM_21546 = lRootSubStateMachine;
            if (lSM_21546 != null)
            {
                for (int i = lSM_21546.entryTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_21546.RemoveEntryTransition(lSM_21546.entryTransitions[i]);
                }

                for (int i = lSM_21546.anyStateTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_21546.RemoveAnyStateTransition(lSM_21546.anyStateTransitions[i]);
                }

                for (int i = lSM_21546.states.Length - 1; i >= 0; i--)
                {
                    lSM_21546.RemoveState(lSM_21546.states[i].state);
                }

                for (int i = lSM_21546.stateMachines.Length - 1; i >= 0; i--)
                {
                    lSM_21546.RemoveStateMachine(lSM_21546.stateMachines[i].stateMachine);
                }
            }
            else
            {
                lSM_21546 = lSM_21500.AddStateMachine(_EditorAnimatorSMName, new Vector3(192, -696, 0));
            }

            UnityEditor.Animations.AnimatorState lS_22652 = lSM_21546.AddState("IdlePose", new Vector3(504, 72, 0));
            lS_22652.speed = 1f;
            lS_22652.motion = m14306;

            UnityEditor.Animations.AnimatorState lS_21796 = lSM_21546.AddState("Move Tree", new Vector3(264, 72, 0));
            lS_21796.speed = 1f;

            UnityEditor.Animations.BlendTree lM_15642 = CreateBlendTree("Blend Tree", _EditorAnimatorController, mMotionLayer.AnimatorLayerIndex);
            lM_15642.blendType = UnityEditor.Animations.BlendTreeType.FreeformCartesian2D;
            lM_15642.blendParameter = "InputX";
            lM_15642.blendParameterY = "InputY";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
            lM_15642.useAutomaticThresholds = true;
#endif
            lM_15642.AddChild(m14708, new Vector2(0f, 0.7f));
            UnityEditor.Animations.ChildMotion[] lM_15642_0_Children = lM_15642.children;
            lM_15642_0_Children[lM_15642_0_Children.Length - 1].mirror = false;
            lM_15642_0_Children[lM_15642_0_Children.Length - 1].timeScale = 1.2f;
            lM_15642.children = lM_15642_0_Children;

            lM_15642.AddChild(m14710, new Vector2(-0.7f, 0.7f));
            UnityEditor.Animations.ChildMotion[] lM_15642_1_Children = lM_15642.children;
            lM_15642_1_Children[lM_15642_1_Children.Length - 1].mirror = false;
            lM_15642_1_Children[lM_15642_1_Children.Length - 1].timeScale = 1.1f;
            lM_15642.children = lM_15642_1_Children;

            lM_15642.AddChild(m14712, new Vector2(0.7f, 0.7f));
            UnityEditor.Animations.ChildMotion[] lM_15642_2_Children = lM_15642.children;
            lM_15642_2_Children[lM_15642_2_Children.Length - 1].mirror = false;
            lM_15642_2_Children[lM_15642_2_Children.Length - 1].timeScale = 1.1f;
            lM_15642.children = lM_15642_2_Children;

            lM_15642.AddChild(m14716, new Vector2(-0.7f, 0f));
            UnityEditor.Animations.ChildMotion[] lM_15642_3_Children = lM_15642.children;
            lM_15642_3_Children[lM_15642_3_Children.Length - 1].mirror = false;
            lM_15642_3_Children[lM_15642_3_Children.Length - 1].timeScale = 1.1f;
            lM_15642.children = lM_15642_3_Children;

            lM_15642.AddChild(m14714, new Vector2(0f, 0f));
            UnityEditor.Animations.ChildMotion[] lM_15642_4_Children = lM_15642.children;
            lM_15642_4_Children[lM_15642_4_Children.Length - 1].mirror = false;
            lM_15642_4_Children[lM_15642_4_Children.Length - 1].timeScale = 1.3f;
            lM_15642.children = lM_15642_4_Children;

            lM_15642.AddChild(m14718, new Vector2(0.7f, 0f));
            UnityEditor.Animations.ChildMotion[] lM_15642_5_Children = lM_15642.children;
            lM_15642_5_Children[lM_15642_5_Children.Length - 1].mirror = false;
            lM_15642_5_Children[lM_15642_5_Children.Length - 1].timeScale = 1.5f;
            lM_15642.children = lM_15642_5_Children;

            lM_15642.AddChild(m14704, new Vector2(-0.7f, -0.7f));
            UnityEditor.Animations.ChildMotion[] lM_15642_6_Children = lM_15642.children;
            lM_15642_6_Children[lM_15642_6_Children.Length - 1].mirror = false;
            lM_15642_6_Children[lM_15642_6_Children.Length - 1].timeScale = 1.5f;
            lM_15642.children = lM_15642_6_Children;

            lM_15642.AddChild(m14706, new Vector2(0.7f, -0.7f));
            UnityEditor.Animations.ChildMotion[] lM_15642_7_Children = lM_15642.children;
            lM_15642_7_Children[lM_15642_7_Children.Length - 1].mirror = false;
            lM_15642_7_Children[lM_15642_7_Children.Length - 1].timeScale = 1.5f;
            lM_15642.children = lM_15642_7_Children;

            lM_15642.AddChild(m14702, new Vector2(0f, -0.7f));
            UnityEditor.Animations.ChildMotion[] lM_15642_8_Children = lM_15642.children;
            lM_15642_8_Children[lM_15642_8_Children.Length - 1].mirror = false;
            lM_15642_8_Children[lM_15642_8_Children.Length - 1].timeScale = 1.5f;
            lM_15642.children = lM_15642_8_Children;

            lS_21796.motion = lM_15642;

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_21654 = lRootStateMachine.AddAnyStateTransition(lS_21796);
            lT_21654.hasExitTime = false;
            lT_21654.hasFixedDuration = true;
            lT_21654.exitTime = 0.9f;
            lT_21654.duration = 0.2f;
            lT_21654.offset = 0f;
            lT_21654.mute = false;
            lT_21654.solo = false;
            lT_21654.canTransitionToSelf = true;
            lT_21654.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_21654.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 620f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_22654 = lS_21796.AddTransition(lS_22652);
            lT_22654.hasExitTime = false;
            lT_22654.hasFixedDuration = true;
            lT_22654.exitTime = 0.9f;
            lT_22654.duration = 0.2f;
            lT_22654.offset = 0f;
            lT_22654.mute = false;
            lT_22654.solo = false;
            lT_22654.canTransitionToSelf = true;
            lT_22654.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_22654.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 610f, "L0MotionPhase");

        }

        /// <summary>
        /// Gathers the animations so we can use them when creating the sub-state machine.
        /// </summary>
        public override void FindAnimations()
        {
            m14306 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdlePose.anim", "IdlePose");
            m14708 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Sneaking/Unity_Sneak.fbx/SneakForward.anim", "SneakForward");
            m14710 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Sneaking/Unity_Sneak.fbx/SneakForwardLeft.anim", "SneakForwardLeft");
            m14712 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Sneaking/Unity_Sneak.fbx/SneakForwardRight.anim", "SneakForwardRight");
            m14716 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Sneaking/Unity_Sneak.fbx/SneakLeft.anim", "SneakLeft");
            m14714 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Sneaking/Unity_Sneak.fbx/SneakIdle.anim", "SneakIdle");
            m14718 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Sneaking/Unity_Sneak.fbx/SneakRight.anim", "SneakRight");
            m14704 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Sneaking/Unity_Sneak.fbx/SneakBackwardLeft.anim", "SneakBackwardLeft");
            m14706 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Sneaking/Unity_Sneak.fbx/SneakBackwardRight.anim", "SneakBackwardRight");
            m14702 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Sneaking/Unity_Sneak.fbx/SneakBackward.anim", "SneakBackward");

            // Add the remaining functionality
            base.FindAnimations();
        }

        /// <summary>
        /// Used to show the settings that allow us to generate the animator setup.
        /// </summary>
        public override void OnSettingsGUI()
        {
            UnityEditor.EditorGUILayout.IntField(new GUIContent("Phase ID", "Phase ID used to transition to the state."), PHASE_START);
            m14306 = CreateAnimationField("IdlePose", "Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdlePose.anim", "IdlePose", m14306);
            m14708 = CreateAnimationField("Move Tree.SneakForward", "Assets/ootii/MotionController/Content/Animations/Humanoid/Sneaking/Unity_Sneak.fbx/SneakForward.anim", "SneakForward", m14708);
            m14710 = CreateAnimationField("Move Tree.SneakForwardLeft", "Assets/ootii/MotionController/Content/Animations/Humanoid/Sneaking/Unity_Sneak.fbx/SneakForwardLeft.anim", "SneakForwardLeft", m14710);
            m14712 = CreateAnimationField("Move Tree.SneakForwardRight", "Assets/ootii/MotionController/Content/Animations/Humanoid/Sneaking/Unity_Sneak.fbx/SneakForwardRight.anim", "SneakForwardRight", m14712);
            m14716 = CreateAnimationField("Move Tree.SneakLeft", "Assets/ootii/MotionController/Content/Animations/Humanoid/Sneaking/Unity_Sneak.fbx/SneakLeft.anim", "SneakLeft", m14716);
            m14714 = CreateAnimationField("Move Tree.SneakIdle", "Assets/ootii/MotionController/Content/Animations/Humanoid/Sneaking/Unity_Sneak.fbx/SneakIdle.anim", "SneakIdle", m14714);
            m14718 = CreateAnimationField("Move Tree.SneakRight", "Assets/ootii/MotionController/Content/Animations/Humanoid/Sneaking/Unity_Sneak.fbx/SneakRight.anim", "SneakRight", m14718);
            m14704 = CreateAnimationField("Move Tree.SneakBackwardLeft", "Assets/ootii/MotionController/Content/Animations/Humanoid/Sneaking/Unity_Sneak.fbx/SneakBackwardLeft.anim", "SneakBackwardLeft", m14704);
            m14706 = CreateAnimationField("Move Tree.SneakBackwardRight", "Assets/ootii/MotionController/Content/Animations/Humanoid/Sneaking/Unity_Sneak.fbx/SneakBackwardRight.anim", "SneakBackwardRight", m14706);
            m14702 = CreateAnimationField("Move Tree.SneakBackward", "Assets/ootii/MotionController/Content/Animations/Humanoid/Sneaking/Unity_Sneak.fbx/SneakBackward.anim", "SneakBackward", m14702);

            // Add the remaining functionality
            base.OnSettingsGUI();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}
