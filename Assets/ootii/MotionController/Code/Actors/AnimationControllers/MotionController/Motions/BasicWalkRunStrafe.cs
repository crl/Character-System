using System.Collections.Generic;
using UnityEngine;
using com.ootii.Actors.Combat;
using com.ootii.Cameras;
using com.ootii.Geometry;
using com.ootii.Helpers;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Actors.AnimationControllers
{
    /// <summary>
    /// Forward facing strafing walk/run animations.
    /// </summary>
    [MotionName("Basic Walk Run Strafe")]
    [MotionDescription("Shooter game style movement that can be expanded. Uses no transitions.")]
    public class BasicWalkRunStrafe : MotionControllerMotion, IWalkRunMotion
    {
        /// <summary>
        /// Trigger values for th emotion
        /// </summary>
        public int PHASE_UNKNOWN = 0;
        public int PHASE_START = 3100;
        public int PHASE_STOP = 3105;

        /// <summary>
        /// Determines if we're using the IsInMotion() function to verify that
        /// the transition in the animator has occurred for this motion.
        /// </summary>
        public override bool VerifyTransition
        {
            get { return false; }
        }

        /// <summary>
        /// Determines if we force the strafe to use targeting mode when the combatant has a target
        /// </summary>
        public bool _RequireTarget = true;
        public bool RequireTarget
        {
            get { return _RequireTarget; }
            set { _RequireTarget = value; }
        }

        /// <summary>
        /// Used to trigger the motion to activate when a button is
        /// held. This is useful for things like targeting or aiming.
        /// </summary>
        public string _ActivationAlias = "";
        public string ActivationAlias
        {
            get { return _ActivationAlias; }
            set { _ActivationAlias = value; }
        }

        /// <summary>
        /// Comma delimited string of stance IDs that the motion will work for. An empty string means all.
        /// </summary>
        public string _ActorStances = "";
        public string ActorStances
        {
            get { return _ActorStances; }

            set
            {
                _ActorStances = value;

                if (_ActorStances.Length == 0)
                {
                    if (mActorStances != null)
                    {
                        mActorStances.Clear();
                    }
                }
                else
                {
                    if (mActorStances == null) { mActorStances = new List<int>(); }
                    mActorStances.Clear();

                    int lStanceID = 0;
                    string[] lStanceIDs = _ActorStances.Split(',');
                    for (int i = 0; i < lStanceIDs.Length; i++)
                    {
                        if (int.TryParse(lStanceIDs[i], out lStanceID))
                        {
                            if (!mActorStances.Contains(lStanceID))
                            {
                                mActorStances.Add(lStanceID);
                            }
                        }
                    }
                }
            }
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
            set { mStartInWalk = value; }
        }

        /// <summary>
        /// Determines if we shortcut the motion and start in a run
        /// </summary>
        private bool mStartInRun = false;
        public bool StartInRun
        {
            get { return mStartInRun; }
            set { mStartInRun = value; }
        }

        /// <summary>
        /// Determines if we rotate by ourselves
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
        /// Determines if we rotate to match the camera
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
        public float _RotationSpeed = 360f;
        public float RotationSpeed
        {
            get { return _RotationSpeed; }
            set { _RotationSpeed = value; }
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
        /// Target that the strafe will focus on
        /// </summary>
        protected ICombatant mCombatant = null;
        public ICombatant Combatant
        {
            get { return mCombatant; }
            set { mCombatant = value; }
        }

        /// <summary>
        /// Actor stances we'll check to see if we are in
        /// </summary>
        [SerializeField]
        protected List<int> mActorStances = new List<int>();

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
        public BasicWalkRunStrafe()
            : base()
        {
            _Category = EnumMotionCategories.WALK;

            _Priority = 7;
            _ActionAlias = "Run";
            _Form = -1;

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "BasicWalkRunStrafe-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public BasicWalkRunStrafe(MotionController rController)
            : base(rController)
        {
            _Category = EnumMotionCategories.WALK;

            _Priority = 7;
            _ActionAlias = "Run";
            _Form = -1;

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "BasicWalkRunStrafe-SM"; }
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

            // Extract the combatant if we can
            mCombatant = mMotionController.gameObject.GetComponent<ICombatant>();
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

            // We need some minimum input before we can move
            if (mMotionController.State.InputMagnitudeTrend.Value < 0.49f)
            {
                return false;
            }

            bool lIsFree = (!_RequireTarget && _ActivationAlias.Length == 0);
            bool lIsTargetValid = (_RequireTarget && mCombatant != null && mCombatant.IsTargetLocked);
            bool lIsAliasValid = (_ActivationAlias.Length > 0 && mMotionController._InputSource != null && mMotionController._InputSource.IsPressed(_ActivationAlias));

            if (!lIsFree && !lIsTargetValid && !lIsAliasValid)
            {
                return false;
            }

            // Ensure we're in a valid stance
            if (mActorStances != null && mActorStances.Count > 0)
            {
                if (!mActorStances.Contains(mMotionController.Stance))
                {
                    return false;
                }
            }

            //// If we're using a targeting mode, it takes priority
            //if (_RequireTarget)
            //{
            //    if (mCombatant != null && mCombatant.IsTargetLocked)
            //    {
            //        return true;
            //    }
            //    else
            //    {
            //        return false;
            //    }
            //}

            //// Determine if we only use this strafe when activated by input
            //if (_ActivationAlias.Length > 0)
            //{
            //    if (mMotionController._InputSource == null || !mMotionController._InputSource.IsPressed(_ActivationAlias))
            //    {
            //        return false;
            //    }
            //}

            // We're good to move
            return true;
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

            bool lIsFree = (!_RequireTarget && _ActivationAlias.Length == 0);
            bool lIsTargetValid = (_RequireTarget && mCombatant != null && mCombatant.IsTargetLocked);
            bool lIsAliasValid = (_ActivationAlias.Length > 0 && mMotionController._InputSource != null && mMotionController._InputSource.IsPressed(_ActivationAlias));

            if (!lIsFree && !lIsTargetValid && !lIsAliasValid)
            {
                // If we're moving, but transitioning to another WR motion, force the input
                if (mMotionController.State.InputMagnitudeTrend.Value > 0.2f && _ActivationAlias.Length > 0)
                {
                    mMotionController.ForcedInput.x = mInputX.Average;
                    mMotionController.ForcedInput.y = mInputY.Average;
                }

                return false;
            }

            //// If we have no target, exit
            //if (_RequireTarget)
            //{
            //    if (mCombatant == null || !mCombatant.IsTargetLocked)
            //    {
            //        return false;
            //    }
            //}

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

            // Ensure we're in a valid stance
            if (mActorStances != null && mActorStances.Count > 0)
            {
                if (!mActorStances.Contains(mMotionController.Stance))
                {
                    return false;
                }
            }

            //// If we're not activated, we can exit
            //if (_ActivationAlias.Length > 0)
            //{
            //    if (mMotionController._InputSource == null || !mMotionController._InputSource.IsPressed(_ActivationAlias))
            //    {
            //        return false;
            //    }
            //}

            // Stay in
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
            mLinkRotation = false;

            mInputX.Clear();
            mInputY.Clear();
            mInputMagnitude.Clear();

            // Update the max speed based on our animation
            mMotionController.MaxSpeed = 5.668f;

            // Determine how we'll start our animation
            mActiveForm = (_Form >= 0 ? _Form : mMotionController.CurrentForm);
            mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, mActiveForm, 0, true);

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
            // Clear out the start
            mStartInRun = false;
            mStartInWalk = false;

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
            rRotation = Quaternion.identity;

            // Override root motion if we're meant to
            float lMovementSpeed = (IsRunActive ? _RunSpeed : _WalkSpeed);
            if (lMovementSpeed > 0f)
            {
                rMovement.x = mMotionController.State.InputX;
                rMovement.y = 0f;
                rMovement.z = mMotionController.State.InputY;
                rMovement = rMovement.normalized * (lMovementSpeed * rDeltaTime);
            }
            // Handle movement manually
            else
            { 
                // Get rid of root-motion that is not aligned with our input
                if (mMotionController._InputSource != null)
                {
                    lMovementSpeed = rMovement.magnitude;

                    rMovement.x = mMotionController.State.InputX;
                    rMovement.y = 0f;
                    rMovement.z = mMotionController.State.InputY;
                    rMovement = rMovement.normalized * lMovementSpeed;
                }
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

            // Rotate using the target direction
            if (_RequireTarget && mCombatant != null && mCombatant.IsTargetLocked)
            {
                if (!mCombatant.ForceActorRotation)
                {
                    Vector3 lDirection = (mCombatant.Target.position - mMotionController._Transform.position).normalized;
                    RotateToDirection(lDirection, _RotationSpeed, rDeltaTime, ref mRotation);
                }
            }
            // If set, rotate most of the way to the camera direction
            else if (_RotateWithCamera && mMotionController._CameraTransform != null)
            {
                RotateToDirection(mMotionController._CameraTransform.forward, _RotationSpeed, rDeltaTime, ref mRotation);
            }
            // Otherwise, rotate using input
            else if (!_RotateWithCamera)
            {
                if (_RotateWithInput) { RotateUsingInput(_RotationSpeed, rDeltaTime, ref mRotation); }
            }

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
        /// <param name="rDeltaTime"></param>
        /// <param name="rAngularVelocity"></param>
        private void RotateUsingInput(float rSpeed, float rDeltaTime, ref Quaternion rRotation)
        {
            // If we don't have an input source, stop
            if (mMotionController._InputSource == null) { return; }

            // Determine this frame's rotation
            float lYawDelta = 0f;
            float lYawSmoothing = 0.1f;

            if (mMotionController._InputSource.IsViewingActivated)
            {
                lYawDelta = mMotionController._InputSource.ViewX * rSpeed * rDeltaTime;
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
        /// Create a rotation velocity that rotates the character based on input
        /// </summary>
        /// <param name="rInputFromAvatarAngle"></param>
        /// <param name="rDeltaTime"></param>
        protected void RotateToDirection(Vector3 rForward, float rSpeed, float rDeltaTime, ref Quaternion rRotation)
        {
            // We do the inverse tilt so we calculate the rotation in "natural up" space vs. "actor up" space. 
            Quaternion lInvTilt = QuaternionExt.FromToRotation(mMotionController._Transform.up, Vector3.up);

            // Forward direction of the actor in "natural up"
            Vector3 lActorForward = lInvTilt * mMotionController._Transform.forward;

            // Camera forward in "natural up"
            Vector3 lTargetForward = lInvTilt * rForward;

            // Ensure we don't exceed our rotation speed
            float lActorToCameraAngle = NumberHelper.GetHorizontalAngle(lActorForward, lTargetForward);
            if (rSpeed > 0f && Mathf.Abs(lActorToCameraAngle) > rSpeed * rDeltaTime)
            {
                lActorToCameraAngle = Mathf.Sign(lActorToCameraAngle) * rSpeed * rDeltaTime;
            }

            // We only want to do this is we're very very close to the desired angle. This will remove any stuttering
            rRotation = Quaternion.AngleAxis(lActorToCameraAngle, Vector3.up);
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
        protected virtual void OnCameraUpdated(float rDeltaTime, int rUpdateIndex, BaseCameraRig rCamera)
        {
            if (!_RotateWithCamera) { return; }
            if (_RequireTarget && mCombatant != null && mCombatant.IsTargetLocked) { return; }
            if (mMotionController._CameraTransform == null) { return; }

            // Get out early if we we aren't modifying the view.
            if (mMotionController._InputSource != null && mMotionController._InputSource.ViewX == 0f) { return; }

            // We do the inverse tilt so we calculate the rotation in "natural up" space vs. "actor up" space. 
            Quaternion lInvTilt = QuaternionExt.FromToRotation(mMotionController._Transform.up, Vector3.up);

            // Forward direction of the actor in "natural up"
            Vector3 lActorForward = lInvTilt * mMotionController._Transform.forward;

            // Camera forward in "natural up"
            Vector3 lCameraForward = lInvTilt * mMotionController._CameraTransform.forward;

            // Get the rotation angle to the camera
            float lActorToCameraAngle = NumberHelper.GetHorizontalAngle(lActorForward, lCameraForward);

            // Clear the link if we're out of rotation range
            if (Mathf.Abs(lActorToCameraAngle) > _RotationSpeed * rDeltaTime * 5f) { mIsRotationLocked = false; }

            // We only want to do this is we're very very close to the desired angle. This will remove any stuttering
            if (_RotationSpeed == 0f || mIsRotationLocked || Mathf.Abs(lActorToCameraAngle) < _RotationSpeed * rDeltaTime * 1f)
            {
                mIsRotationLocked = true;

                // Since we're after the camera update, we have to force the rotation outside the normal flow
                Quaternion lRotation = Quaternion.AngleAxis(lActorToCameraAngle, Vector3.up);
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

            if (EditorHelper.BoolField("Activate With Target", "Determines if the motion will activate when the Combatant component has a target. If checked we will automatically rotate to the target as long as there is one.", RequireTarget, mMotionController))
            {
                lIsDirty = true;
                RequireTarget = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.TextField("Activation Alias", "If set, the action alias that will activate this motion when pressed.", ActivationAlias, mMotionController))
            {
                lIsDirty = true;
                ActivationAlias = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.TextField("Valid Actor Stances", "Comma delimited list of stance IDs that the transition will work in. Leave empty to ignore this condition.", ActorStances, mMotionController))
            {
                lIsDirty = true;
                ActorStances = EditorHelper.FieldStringValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Default to Run", "Determines if the default is to run or walk.", DefaultToRun, mMotionController))
            {
                lIsDirty = true;
                DefaultToRun = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.TextField("Run Action Alias", "Action alias that triggers a run or walk (which ever is opposite the default).", ActionAlias, mMotionController))
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

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Rotate With Input", "Determines if we rotate based on user input.", RotateWithInput, mMotionController))
            {
                lIsDirty = true;
                RotateWithInput = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("Rotate With Camera", "Determines if we rotate to match the camera.", RotateWithCamera, mMotionController))
            {
                lIsDirty = true;
                RotateWithCamera = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.FloatField("Rotation Speed", "Degrees per second to rotate the actor.", RotationSpeed, mMotionController))
            {
                lIsDirty = true;
                RotationSpeed = EditorHelper.FieldFloatValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.IntField("Smoothing Samples", "Smoothing factor for input. The more samples the smoother, but the less responsive (0 disables).", SmoothingSamples, mMotionController))
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
            TRANS_AnyState_UnarmedBlendTree = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".BasicWalkRunStrafe-SM.Unarmed BlendTree");
            TRANS_EntryState_UnarmedBlendTree = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".BasicWalkRunStrafe-SM.Unarmed BlendTree");
            STATE_Empty = mMotionController.AddAnimatorName("" + lLayer + ".Empty");
            STATE_UnarmedBlendTree = mMotionController.AddAnimatorName("" + lLayer + ".BasicWalkRunStrafe-SM.Unarmed BlendTree");
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

            UnityEditor.Animations.AnimatorStateMachine lSSM_37926 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "BasicWalkRunStrafe-SM");
            if (lSSM_37926 == null) { lSSM_37926 = lLayerStateMachine.AddStateMachine("BasicWalkRunStrafe-SM", new Vector3(408, -1008, 0)); }

            UnityEditor.Animations.AnimatorState lState_38402 = MotionControllerMotion.EditorFindState(lSSM_37926, "Unarmed BlendTree");
            if (lState_38402 == null) { lState_38402 = lSSM_37926.AddState("Unarmed BlendTree", new Vector3(336, 24, 0)); }
            lState_38402.speed = 1f;
            lState_38402.mirror = false;
            lState_38402.tag = "";

            UnityEditor.Animations.BlendTree lM_25600 = MotionControllerMotion.EditorCreateBlendTree("Move Blend Tree", lController, rLayerIndex);
            lM_25600.blendType = UnityEditor.Animations.BlendTreeType.Simple1D;
            lM_25600.blendParameter = "InputMagnitude";
            lM_25600.blendParameterY = "InputX";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
            lM_25600.useAutomaticThresholds = false;
#endif
            lM_25600.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx", "IdlePose"), 0f);

            UnityEditor.Animations.BlendTree lM_25694 = MotionControllerMotion.EditorCreateBlendTree("WalkTree", lController, rLayerIndex);
            lM_25694.blendType = UnityEditor.Animations.BlendTreeType.SimpleDirectional2D;
            lM_25694.blendParameter = "InputX";
            lM_25694.blendParameterY = "InputY";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
            lM_25694.useAutomaticThresholds = true;
#endif
            lM_25694.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_WalkFWD_v2.fbx", "WalkForward"), new Vector2(0f, 0.35f));
            UnityEditor.Animations.ChildMotion[] lM_25694_0_Children = lM_25694.children;
            lM_25694_0_Children[lM_25694_0_Children.Length - 1].mirror = false;
            lM_25694_0_Children[lM_25694_0_Children.Length - 1].timeScale = 1.1f;
            lM_25694.children = lM_25694_0_Children;

            lM_25694.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_SWalk_v2.fbx", "SWalkForwardRight"), new Vector2(0.35f, 0.35f));
            UnityEditor.Animations.ChildMotion[] lM_25694_1_Children = lM_25694.children;
            lM_25694_1_Children[lM_25694_1_Children.Length - 1].mirror = false;
            lM_25694_1_Children[lM_25694_1_Children.Length - 1].timeScale = 1.2f;
            lM_25694.children = lM_25694_1_Children;

            lM_25694.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_SWalk_v2.fbx", "SWalkForwardLeft"), new Vector2(-0.35f, 0.35f));
            UnityEditor.Animations.ChildMotion[] lM_25694_2_Children = lM_25694.children;
            lM_25694_2_Children[lM_25694_2_Children.Length - 1].mirror = false;
            lM_25694_2_Children[lM_25694_2_Children.Length - 1].timeScale = 1.2f;
            lM_25694.children = lM_25694_2_Children;

            lM_25694.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_SWalk_v2.fbx", "SWalkLeft"), new Vector2(-0.35f, 0f));
            UnityEditor.Animations.ChildMotion[] lM_25694_3_Children = lM_25694.children;
            lM_25694_3_Children[lM_25694_3_Children.Length - 1].mirror = false;
            lM_25694_3_Children[lM_25694_3_Children.Length - 1].timeScale = 1.2f;
            lM_25694.children = lM_25694_3_Children;

            lM_25694.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_SWalk_v2.fbx", "SWalkRight"), new Vector2(0.35f, 0f));
            UnityEditor.Animations.ChildMotion[] lM_25694_4_Children = lM_25694.children;
            lM_25694_4_Children[lM_25694_4_Children.Length - 1].mirror = false;
            lM_25694_4_Children[lM_25694_4_Children.Length - 1].timeScale = 1.2f;
            lM_25694.children = lM_25694_4_Children;

            lM_25694.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2Strafe_AllAngles.fbx", "WalkStrafeBackwardsLeft"), new Vector2(-0.35f, -0.35f));
            UnityEditor.Animations.ChildMotion[] lM_25694_5_Children = lM_25694.children;
            lM_25694_5_Children[lM_25694_5_Children.Length - 1].mirror = false;
            lM_25694_5_Children[lM_25694_5_Children.Length - 1].timeScale = 1.1f;
            lM_25694.children = lM_25694_5_Children;

            lM_25694.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2Strafe_AllAngles.fbx", "WalkStrafeBackwardsRight"), new Vector2(0.35f, -0.35f));
            UnityEditor.Animations.ChildMotion[] lM_25694_6_Children = lM_25694.children;
            lM_25694_6_Children[lM_25694_6_Children.Length - 1].mirror = false;
            lM_25694_6_Children[lM_25694_6_Children.Length - 1].timeScale = 1.1f;
            lM_25694.children = lM_25694_6_Children;

            lM_25694.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_BWalk.fbx", "WalkBackwards"), new Vector2(0f, -0.35f));
            UnityEditor.Animations.ChildMotion[] lM_25694_7_Children = lM_25694.children;
            lM_25694_7_Children[lM_25694_7_Children.Length - 1].mirror = false;
            lM_25694_7_Children[lM_25694_7_Children.Length - 1].timeScale = 1f;
            lM_25694.children = lM_25694_7_Children;

            lM_25600.AddChild(lM_25694, 0.5f);

            UnityEditor.Animations.BlendTree lM_25630 = MotionControllerMotion.EditorCreateBlendTree("RunTree", lController, rLayerIndex);
            lM_25630.blendType = UnityEditor.Animations.BlendTreeType.SimpleDirectional2D;
            lM_25630.blendParameter = "InputX";
            lM_25630.blendParameterY = "InputY";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
            lM_25630.useAutomaticThresholds = true;
#endif
            lM_25630.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunForward_v2.fbx", "RunForward"), new Vector2(0f, 0.7f));
            UnityEditor.Animations.ChildMotion[] lM_25630_0_Children = lM_25630.children;
            lM_25630_0_Children[lM_25630_0_Children.Length - 1].mirror = false;
            lM_25630_0_Children[lM_25630_0_Children.Length - 1].timeScale = 1f;
            lM_25630.children = lM_25630_0_Children;

            lM_25630.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunStrafe.fbx", "RunStrafeForwardRight"), new Vector2(0.7f, 0.7f));
            UnityEditor.Animations.ChildMotion[] lM_25630_1_Children = lM_25630.children;
            lM_25630_1_Children[lM_25630_1_Children.Length - 1].mirror = false;
            lM_25630_1_Children[lM_25630_1_Children.Length - 1].timeScale = 1.1f;
            lM_25630.children = lM_25630_1_Children;

            lM_25630.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunStrafe.fbx", "RunStrafeForwardLeft"), new Vector2(-0.7f, 0.7f));
            UnityEditor.Animations.ChildMotion[] lM_25630_2_Children = lM_25630.children;
            lM_25630_2_Children[lM_25630_2_Children.Length - 1].mirror = false;
            lM_25630_2_Children[lM_25630_2_Children.Length - 1].timeScale = 1.1f;
            lM_25630.children = lM_25630_2_Children;

            lM_25630.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunStrafe.fbx", "RunStrafeLeft"), new Vector2(-0.7f, 0f));
            UnityEditor.Animations.ChildMotion[] lM_25630_3_Children = lM_25630.children;
            lM_25630_3_Children[lM_25630_3_Children.Length - 1].mirror = false;
            lM_25630_3_Children[lM_25630_3_Children.Length - 1].timeScale = 1f;
            lM_25630.children = lM_25630_3_Children;

            lM_25630.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunStrafe.fbx", "RunStrafeRight"), new Vector2(0.7f, 0f));
            UnityEditor.Animations.ChildMotion[] lM_25630_4_Children = lM_25630.children;
            lM_25630_4_Children[lM_25630_4_Children.Length - 1].mirror = false;
            lM_25630_4_Children[lM_25630_4_Children.Length - 1].timeScale = 1f;
            lM_25630.children = lM_25630_4_Children;

            lM_25630.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunStrafe.fbx", "RunStrafeBackwardLeft"), new Vector2(-0.7f, -0.7f));
            UnityEditor.Animations.ChildMotion[] lM_25630_5_Children = lM_25630.children;
            lM_25630_5_Children[lM_25630_5_Children.Length - 1].mirror = false;
            lM_25630_5_Children[lM_25630_5_Children.Length - 1].timeScale = 1.1f;
            lM_25630.children = lM_25630_5_Children;

            lM_25630.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunStrafe.fbx", "RunStrafeBackwardRight"), new Vector2(0.7f, -0.7f));
            UnityEditor.Animations.ChildMotion[] lM_25630_6_Children = lM_25630.children;
            lM_25630_6_Children[lM_25630_6_Children.Length - 1].mirror = false;
            lM_25630_6_Children[lM_25630_6_Children.Length - 1].timeScale = 1.1f;
            lM_25630.children = lM_25630_6_Children;

            lM_25630.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunBackward.fbx", "RunBackwards"), new Vector2(0f, -0.7f));
            UnityEditor.Animations.ChildMotion[] lM_25630_7_Children = lM_25630.children;
            lM_25630_7_Children[lM_25630_7_Children.Length - 1].mirror = false;
            lM_25630_7_Children[lM_25630_7_Children.Length - 1].timeScale = 1f;
            lM_25630.children = lM_25630_7_Children;

            lM_25600.AddChild(lM_25630, 1f);
            lState_38402.motion = lM_25600;

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_38112 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_38402, 0);
            if (lAnyTransition_38112 == null) { lAnyTransition_38112 = lLayerStateMachine.AddAnyStateTransition(lState_38402); }
            lAnyTransition_38112.isExit = false;
            lAnyTransition_38112.hasExitTime = false;
            lAnyTransition_38112.hasFixedDuration = true;
            lAnyTransition_38112.exitTime = 0.9f;
            lAnyTransition_38112.duration = 0.2f;
            lAnyTransition_38112.offset = 0f;
            lAnyTransition_38112.mute = false;
            lAnyTransition_38112.solo = false;
            lAnyTransition_38112.canTransitionToSelf = true;
            lAnyTransition_38112.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_38112.conditions.Length - 1; i >= 0; i--) { lAnyTransition_38112.RemoveCondition(lAnyTransition_38112.conditions[i]); }
            lAnyTransition_38112.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3100f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_38112.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");

#if USE_ARCHERY_MP || OOTII_AYMP
            ArcheryPackDefinition.ExtendBasicWalkRunStrafe(rMotionController, rLayerIndex);
#endif

#if USE_SWORD_SHIELD_MP || OOTII_SSMP
            SwordShieldPackDefinition.ExtendBasicWalkRunStrafe(rMotionController, rLayerIndex);
#endif

#if USE_SPELL_CASTING_MP || OOTII_SCMP
            SpellCastingPackDefinition.ExtendBasicWalkRunStrafe(rMotionController, rLayerIndex);
#endif

#if USE_SHOOTER_MP || OOTII_SHMP
            ShooterPackDefinition.ExtendBasicWalkRunStrafe(rMotionController, rLayerIndex);
#endif

            // Run any post processing after creating the state machine
            OnStateMachineCreated();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}

