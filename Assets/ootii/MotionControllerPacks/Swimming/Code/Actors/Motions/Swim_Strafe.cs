using UnityEngine;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Cameras;
using com.ootii.Geometry;
using com.ootii.Helpers;
using com.ootii.Timing;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.MotionControllerPacks
{
    /// <summary>
    /// Forward facing strafing walk/run that uses Mixamo's bow animations.
    /// </summary>
    [MotionName("Swim - Strafe")]
    [MotionDescription("Forward facing swim that uses Mixamo's animations.")]
    public class Swim_Strafe : MotionControllerMotion
    {
        /// <summary>
        /// Values to average
        /// </summary>
        public const int SMOOTHING_BASE = 20;

        /// <summary>
        /// Trigger values for th emotion
        /// </summary>
        public const int PHASE_UNKNOWN = 0;
        public const int PHASE_START = 31400;
        public const int PHASE_STOP_SWIM = 31405;
        public const int PHASE_STOP_IDLE = 31406;

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
        public bool IsRunActive
        {
            get
            {
                if (mMotionController._InputSource == null) { return _DefaultToRun; }
                return ((_DefaultToRun && !mMotionController._InputSource.IsPressed(_ActionAlias)) || (!_DefaultToRun && mMotionController._InputSource.IsPressed(_ActionAlias)));
            }
        }

        /// <summary>
        /// Determines if the actor is actually be running based on input
        /// </summary>
        public bool IsRunning
        {
            get
            {
                float lMagnitude = mMotionController.State.InputMagnitudeTrend.Value;

                if (mMotionController._InputSource != null)
                {
                    if ((_DefaultToRun && !mMotionController._InputSource.IsPressed(_ActionAlias)) || (!_DefaultToRun && mMotionController._InputSource.IsPressed(_ActionAlias)))
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }

                return _DefaultToRun && lMagnitude > 0.1f;
            }
        }

        /// <summary>
        /// Speed (units per second) when walking
        /// </summary>
        public float _WalkSpeed = 1.2f;
        public virtual float WalkSpeed
        {
            get { return _WalkSpeed; }
            set { _WalkSpeed = value; }
        }

        /// <summary>
        /// Speed (units per second) when running
        /// </summary>
        public float _RunSpeed = 3.2f;
        public virtual float RunSpeed
        {
            get { return _RunSpeed; }
            set { _RunSpeed = value; }
        }

        /// <summary>
        /// Action alias for moving up
        /// </summary>
        public string _UpAlias = "Move Up";
        public string UpAlias
        {
            get { return _UpAlias; }
            set { _UpAlias = value; }
        }

        /// <summary>
        /// Action alias for moving down
        /// </summary>
        public string _DownAlias = "Move Down";
        public string DownAlias
        {
            get { return _DownAlias; }
            set { _DownAlias = value; }
        }

        /// <summary>
        /// Tilts the character body (if it exists) based on the camer
        /// </summary>
        public bool _DiveWithCamera = true;
        public bool DiveWithCamera
        {
            get { return _DiveWithCamera; }
            set { _DiveWithCamera = value; }
        }

        /// <summary>
        /// Determines if the body tilts as we dive
        /// </summary>
        public bool _TiltWithDive = true;
        public bool TiltWithDive
        {
            get { return _TiltWithDive; }
            set { _TiltWithDive = value; }
        }

        /// <summary>
        /// Maximum pitch that can be achieved
        /// </summary>
        public float _MaxPitch = 40f;
        public float MaxPitch
        {
            get { return _MaxPitch; }
            set { _MaxPitch = value; }
        }

        /// <summary>
        /// Speed when moving up and down
        /// </summary>
        public float _VerticalSpeed = 1f;
        public float VerticalSpeed
        {
            get { return _VerticalSpeed; }
            set { _VerticalSpeed = value; }
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
        /// Determines if we rotate to match the camera
        /// </summary>
        public bool _RotateWithCamera = true;
        public bool RotateWithCamera
        {
            get { return _RotateWithCamera; }
            set { _RotateWithCamera = value; }
        }

        /// <summary>
        /// Desired degrees of rotation per second
        /// </summary>
        public float _RotationSpeed = 270f;
        public float RotationSpeed
        {
            get { return _RotationSpeed; }

            set
            {
                _RotationSpeed = value;
                mDegreesPer60FPSTick = _RotationSpeed / 60f;
            }
        }

        /// <summary>
        /// Fields to help smooth out the mouse rotation
        /// </summary>
        protected float mYaw = 0f;
        protected float mYawTarget = 0f;
        protected float mYawVelocity = 0f;

        /// <summary>
        /// Speed we'll actually apply to the rotation. This is essencially the
        /// number of degrees per tick assuming we're running at 60 FPS
        /// </summary>
        protected float mDegreesPer60FPSTick = 1f;

        /// <summary>
        /// Time that has elapsed since there was no input
        /// </summary>
        protected float mNoInputElapsed = 0f;

        /// <summary>
        /// We use these classes to help smooth the input values so that
        /// movement doesn't drop from 1 to 0 immediately.
        /// </summary>
        protected FloatValue mInputX = new FloatValue(0f, SMOOTHING_BASE);
        protected FloatValue mInputY = new FloatValue(0f, SMOOTHING_BASE);
        protected FloatValue mInputMagnitude = new FloatValue(0f, SMOOTHING_BASE);

        /// <summary>
        /// Determines if the actor rotation should be linked to the camera
        /// </summary>
        protected bool mLinkRotation = false;

        /// <summary>
        /// Swimmer info associated with Swim_Idle
        /// </summary>
        protected SwimmerInfo mSwimmerInfo = null;

        /// <summary>
        /// Default constructor
        /// </summary>
        public Swim_Strafe()
            : base()
        {
            _Pack = Swim_Idle.GroupName();
            _Category = EnumMotionCategories.WALK;

            _Priority = 22;
            _ActionAlias = "Run";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "Swim_Strafe-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public Swim_Strafe(MotionController rController)
            : base(rController)
        {
            _Pack = Swim_Idle.GroupName();
            _Category = EnumMotionCategories.WALK;

            _Priority = 22;
            _ActionAlias = "Run";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "Swim_Strafe-SM"; }
#endif
        }

        /// <summary>
        /// Awake is called after all objects are initialized so you can safely speak to other objects. This is where
        /// reference can be associated.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            // Default the speed we'll use to rotate
            mDegreesPer60FPSTick = _RotationSpeed / 60f;
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

            // If we're not actually moving, we won't use this motion
            if (mMotionController.State.InputMagnitudeTrend.Value < 0.1f)
            {
                return false;
            }

            // If we're swimming, we can just move in
            if (mActorController.State.Stance == EnumControllerStance.SWIMMING)
            {
                return true;
            }
            // If we're not swimming yet, we may be entering the water
            else if (mSwimmerInfo.TestEnterWater())
            {
                mSwimmerInfo.WaterSurfaceLastPosition = mSwimmerInfo.WaterSurface.position;
                return true;
            }

            // We're good to move
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
            if (mActorController.State.Stance != EnumControllerStance.SWIMMING) { return false; }

            // Ensure we're in the animation
            if (mIsAnimatorActive && !IsInMotionState)
            {
                return false;
            }

            // Ensure we're in water
            if (mSwimmerInfo == null || mSwimmerInfo.WaterSurface == null)
            {
                return false;
            }

            // If we're in the idle state with no movement, stop
            if (mMotionLayer._AnimatorStateID == STATE_TreadIdlePose)
            {
                if (mMotionController.State.InputMagnitudeTrend.Value < 0.1f)
                {
                    return false;
                }
            }
            // If we're surfacing, exit to the idle pose
            else if (mMotionLayer._AnimatorStateID == STATE_IdlePose)
            {
                mSwimmerInfo.ExitWater();
                return false;
            }

            // Just incse, ensure we're in water
            float lWaterMovement = mSwimmerInfo.WaterSurface.position.y - mSwimmerInfo.WaterSurfaceLastPosition.y;
            float lDepth = mSwimmerInfo.GetDepth();
            if (lDepth - lWaterMovement <= 0f)
            {
                mSwimmerInfo.ExitWater();
                return false;
            }

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
            mNoInputElapsed = 0f;
            mLinkRotation = false;

            // Force the stance
            mSwimmerInfo.EnterWater();

            if (rPrevMotion is Jump || rPrevMotion is Fall)
            {
                mSwimmerInfo.CreateSplash();
            }

            // Helps with syncronizing from a motion like attack
            float lRunFactor = (IsRunActive ? 1f : 0.5f);
            mInputX.Clear(mMotionController.State.InputX * lRunFactor);
            mInputY.Clear(mMotionController.State.InputY * lRunFactor);
            mInputMagnitude.Clear(Mathf.Sqrt((mInputX.Value * mInputX.Value) + (mInputY.Value * mInputY.Value)));

            // Determine how we'll start our animation
            mMotionController.ForcedInput = Vector3.zero;
            mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, true);

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
            // Ensure we remove any tilt
            if (mSwimmerInfo != null && mSwimmerInfo.BodyTransform != null)
            {
                mSwimmerInfo.BodyTransform.localRotation = Quaternion.identity;
            }

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

            bool lIsRunning = IsRunActive && mMotionController.State.InputMagnitudeTrend.Value > 0.6f;
            if ((lIsRunning && _RunSpeed > 0f) || (!lIsRunning && _WalkSpeed > 0f))
            {
                rMovement = Vector3.zero;

                if (mMotionLayer._AnimatorTransitionID == TRANS_EntryState_SwimTree ||
                    (mMotionLayer._AnimatorStateID == STATE_SwimTree && mMotionLayer._AnimatorTransitionID == 0))
                {
                    rMovement = Vector3.ClampMagnitude(mMotionController.State.InputForward, 1f);
                }

                rMovement = rMovement * ((lIsRunning ? _RunSpeed : _WalkSpeed) * rDeltaTime);
            }
            else
            {
                // Clear out any excess movement from the animations
                if (mMotionLayer._AnimatorTransitionID == TRANS_SwimTree_IdlePose)
                {
                    rMovement = Vector3.zero;
                }
                // This is an odd case to avoid the character from going backwards before going forward.
                // Unfortunately, the animation with 'center of mass' seems to do this.
                else if (mMotionLayer._AnimatorTransitionID == TRANS_EntryState_SwimTree)
                {
                    if (mMotionController.State.InputX == 0f && mMotionController.State.InputY > 0f)
                    {
                        if (rMovement.z < 0) { rMovement = Vector3.zero; }
                    }
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

            mNoInputElapsed = mNoInputElapsed + rDeltaTime;

            // If we're at the surface and grounded, we can't be going fast
            bool lAllowRunning = (!mSwimmerInfo.IsInShallowWater(mActorController.GroundingLayers)) && IsRunning;

            // Grab the state info
            MotionState lState = mMotionController.State;

            // Convert the input to radial so we deal with keyboard and gamepad input the same.
            float lInputX = lState.InputX;
            float lInputY = lState.InputY;
            float lInputMagnitude = lState.InputMagnitudeTrend.Value;
            InputManagerHelper.ConvertToRadialInput(ref lInputX, ref lInputY, ref lInputMagnitude, (lAllowRunning ? 1f : 0.5f));

            // If input stops, we're going to keep our current value
            // and have the motion come to a smooth stop as needed
            if (lInputMagnitude > 0f)
            {
                mInputX.Add(lInputX);
                mInputY.Add(lInputY);
                mInputMagnitude.Add(lInputMagnitude);

                mNoInputElapsed = 0f;
            }

            // Use the averaged values for input
            lState.InputX = mInputX.Average;
            lState.InputY = mInputY.Average;
            lState.InputMagnitudeTrend.Replace(mInputMagnitude.Average);
            mMotionController.State = lState;

            // Determine how we'll stop based on the direction
            if (mMotionLayer._AnimatorStateID == STATE_SwimTree && mNoInputElapsed > 0.15f)
            {
                mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, PHASE_STOP_SWIM, 0, true);
            }

            // We do this so we can re-enter the movement if needed
            if (mMotionLayer._AnimatorTransitionID == TRANS_SwimTree_TreadIdlePose)
            {
                mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, 0, 0, true);
            }

            // Do a surface check to see if we're exiting the water
            float lWaterMovement = mSwimmerInfo.WaterSurface.position.y - mSwimmerInfo.WaterSurfaceLastPosition.y;
            if (mSwimmerInfo.TestExitWater(lWaterMovement))
            {
                mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, PHASE_STOP_IDLE, 0, true);
                return;
            }

            // If we're at the surface, we may need to move with the surface
            if (mSwimmerInfo.IsAtWaterSurface(0.5f))
            {
                mMovement.y = mMovement.y + lWaterMovement;
                mSwimmerInfo.CreateRipples(mMotionController._Transform.position);
            }
            else
            {
                mSwimmerInfo.CreateUnderwaterEffect(mMotionController.Animator, HumanBodyBones.Head);
            }

            mSwimmerInfo.WaterSurfaceLastPosition = mSwimmerInfo.WaterSurface.position;

            // Move based on the buoyancy
            mMovement = mMovement + mSwimmerInfo.GetBuoyancy(rDeltaTime);

            // Move vertically
            float lAngle = 0f;

            if (mMotionController._InputSource != null)
            {
                float lUpSpeed = mMotionController._InputSource.GetValue(_UpAlias) * _VerticalSpeed;
                if (lUpSpeed != 0f) { lAngle = _MaxPitch; }
                if (mSwimmerInfo.IsAtWaterSurface(0f)) { lUpSpeed = 0f; }

                float lDownSpeed = mMotionController._InputSource.GetValue(_DownAlias) * -_VerticalSpeed;
                if (lDownSpeed != 0f) { lAngle = -_MaxPitch; }
                if (mActorController.IsGrounded) { lDownSpeed = 0f; }

                mMovement.y = mMovement.y + ((lUpSpeed + lDownSpeed) * rDeltaTime);
            }

            // If we're not using the keys, we may use the mouse
            if (_DiveWithCamera && lAngle == 0f)
            {
                float lUpSpeed = 0f;
                float lDownSpeed = 0f;
                float lAdjustedMaxPitch = _MaxPitch - 5f;

                float lCameraAngle = NumberHelper.GetHorizontalAngle(mMotionController._CameraTransform.forward, mMotionController._Transform.forward, mMotionController._CameraTransform.right);
                if (lCameraAngle > 5f)
                {
                    lCameraAngle = lCameraAngle - 5f;
                    lUpSpeed = (Mathf.Min(lCameraAngle, lAdjustedMaxPitch) / lAdjustedMaxPitch) * _VerticalSpeed;
                    if (mSwimmerInfo.IsAtWaterSurface(0f)) { lUpSpeed = 0f; lCameraAngle = 0f; }
                }
                else if (lCameraAngle < -5f)
                {
                    lCameraAngle = lCameraAngle + 5f;
                    lDownSpeed = (Mathf.Max(lCameraAngle, -lAdjustedMaxPitch) / -lAdjustedMaxPitch) * -_VerticalSpeed;
                    if (mActorController.IsGrounded) { lDownSpeed = 0f; lCameraAngle = 0f; }
                }

                if (lCameraAngle != 0f)
                {
                    lAngle = Mathf.Clamp(lAngle + lCameraAngle, -_MaxPitch, _MaxPitch);
                    mMovement.y = mMovement.y + ((lUpSpeed + lDownSpeed) * rDeltaTime);
                }
            }

            //Utilities.Debug.Log.ScreenWrite("Angle:" + lAngle.ToString("f3"), 14);

            // Rotate the body if we can
            RotateBody(-lAngle);

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
        /// Rotates the body based on the vertical movement that is taking place
        /// </summary>
        public void RotateBody(float rAngle)
        {
            if (mSwimmerInfo == null || mSwimmerInfo.BodyTransform == null) { return; }

            rAngle = Mathf.Clamp(rAngle, -_MaxPitch, _MaxPitch);

            if (mActorController.IsGrounded || mMotionLayer._AnimatorTransitionID == TRANS_SwimTree_IdlePose || mMotionLayer._AnimatorTransitionID == TRANS_SwimTree_TreadIdlePose)
            {
                mSwimmerInfo.RotateBody(0f);
                //mSwimmerInfo.BodyTransform.localRotation = Quaternion.Lerp(mSwimmerInfo.BodyTransform.localRotation, Quaternion.identity, 0.1f);
            }
            else if (_TiltWithDive && mMovement.y < 0f)
            {
                //float lPercent = (mMovement.y / Time.deltaTime) / -_VerticalSpeed;
                //float lAngle = _MaxPitch * mInputMagnitude.Average * lPercent;
                mSwimmerInfo.RotateBody(rAngle, 0.05f);
                //mSwimmerInfo.BodyTransform.localRotation = Quaternion.Lerp(mSwimmerInfo.BodyTransform.localRotation, Quaternion.AngleAxis(lAngle, Vector3.right), 0.05f);
            }
            else if (_TiltWithDive && mMovement.y > 0f)
            {
                //float lPercent = (mMovement.y / Time.deltaTime) / _VerticalSpeed;
                //float lAngle = -_MaxPitch * mInputMagnitude.Average * lPercent;
                mSwimmerInfo.RotateBody(rAngle, 0.05f);
                //mSwimmerInfo.BodyTransform.localRotation = Quaternion.Lerp(mSwimmerInfo.BodyTransform.localRotation, Quaternion.AngleAxis(lAngle, Vector3.right), 0.05f);
            }
            else
            {
                mSwimmerInfo.RotateBody(0f, 0.05f);
                //mSwimmerInfo.BodyTransform.localRotation = Quaternion.Lerp(mSwimmerInfo.BodyTransform.localRotation, Quaternion.identity, 0.05f);
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
                lYawDelta = mMotionController._InputSource.ViewX * mDegreesPer60FPSTick;
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
            if (!mLinkRotation && Mathf.Abs(lToCameraAngle) <= mDegreesPer60FPSTick * TimeManager.Relative60FPSDeltaTime) { mLinkRotation = true; }

            if (!mLinkRotation)
            {
                float lRotationAngle = Mathf.Abs(lToCameraAngle);
                float lRotationSign = Mathf.Sign(lToCameraAngle);
                lToCameraAngle = lRotationSign * Mathf.Min(mDegreesPer60FPSTick * TimeManager.Relative60FPSDeltaTime, lRotationAngle);
            }

            Quaternion lRotation = Quaternion.AngleAxis(lToCameraAngle, Vector3.up);
            mActorController.Yaw = mActorController.Yaw * lRotation;
            mActorController._Transform.rotation = mActorController.Tilt * mActorController.Yaw;
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

            if (EditorHelper.TextField("Up Action Alias", "Action alias that has us move up.", UpAlias, mMotionController))
            {
                lIsDirty = true;
                UpAlias = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.TextField("Down Action Alias", "Action alias that has us move down.", DownAlias, mMotionController))
            {
                lIsDirty = true;
                DownAlias = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.BoolField("Dive With Camera", "Determines if we dive/ascend based on the camera tilt.", DiveWithCamera, mMotionController))
            {
                lIsDirty = true;
                DiveWithCamera = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.FloatField("Vertical Speed", "Speed (units per second) to move up or down.", VerticalSpeed, mMotionController))
            {
                lIsDirty = true;
                VerticalSpeed = EditorHelper.FieldFloatValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Tilt With Dive", "Determines if we tilt the body as we dive/ascend. Requies a 'Body Transform' on the Swim - Idle properties", TiltWithDive, mMotionController))
            {
                lIsDirty = true;
                TiltWithDive = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.FloatField("Max Body Tilt", "Max amount to pitch the body when going up or down.", MaxPitch, mMotionController))
            {
                lIsDirty = true;
                MaxPitch = EditorHelper.FieldFloatValue;
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
        public static int STATE_TreadIdlePose = -1;
        public static int STATE_SwimTree = -1;
        public static int STATE_IdlePose = -1;
        public static int TRANS_AnyState_SwimTree = -1;
        public static int TRANS_EntryState_SwimTree = -1;
        public static int TRANS_TreadIdlePose_SwimTree = -1;
        public static int TRANS_SwimTree_TreadIdlePose = -1;
        public static int TRANS_SwimTree_IdlePose = -1;

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

                if (lStateID == STATE_TreadIdlePose) { return true; }
                if (lStateID == STATE_SwimTree) { return true; }
                if (lStateID == STATE_IdlePose) { return true; }
                if (lTransitionID == TRANS_AnyState_SwimTree) { return true; }
                if (lTransitionID == TRANS_EntryState_SwimTree) { return true; }
                if (lTransitionID == TRANS_TreadIdlePose_SwimTree) { return true; }
                if (lTransitionID == TRANS_SwimTree_TreadIdlePose) { return true; }
                if (lTransitionID == TRANS_SwimTree_IdlePose) { return true; }
                return false;
            }
        }

        /// <summary>
        /// Used to determine if the actor is in one of the states for this motion
        /// </summary>
        /// <returns></returns>
        public override bool IsMotionState(int rStateID)
        {
            if (rStateID == STATE_TreadIdlePose) { return true; }
            if (rStateID == STATE_SwimTree) { return true; }
            if (rStateID == STATE_IdlePose) { return true; }
            return false;
        }

        /// <summary>
        /// Used to determine if the actor is in one of the states for this motion
        /// </summary>
        /// <returns></returns>
        public override bool IsMotionState(int rStateID, int rTransitionID)
        {
            if (rStateID == STATE_TreadIdlePose) { return true; }
            if (rStateID == STATE_SwimTree) { return true; }
            if (rStateID == STATE_IdlePose) { return true; }
            if (rTransitionID == TRANS_AnyState_SwimTree) { return true; }
            if (rTransitionID == TRANS_EntryState_SwimTree) { return true; }
            if (rTransitionID == TRANS_TreadIdlePose_SwimTree) { return true; }
            if (rTransitionID == TRANS_SwimTree_TreadIdlePose) { return true; }
            if (rTransitionID == TRANS_SwimTree_IdlePose) { return true; }
            return false;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            TRANS_AnyState_SwimTree = mMotionController.AddAnimatorName("AnyState -> Base Layer.Swim_Strafe-SM.Swim Tree");
            TRANS_EntryState_SwimTree = mMotionController.AddAnimatorName("Entry -> Base Layer.Swim_Strafe-SM.Swim Tree");
            STATE_TreadIdlePose = mMotionController.AddAnimatorName("Base Layer.Swim_Strafe-SM.TreadIdlePose");
            TRANS_TreadIdlePose_SwimTree = mMotionController.AddAnimatorName("Base Layer.Swim_Strafe-SM.TreadIdlePose -> Base Layer.Swim_Strafe-SM.Swim Tree");
            STATE_SwimTree = mMotionController.AddAnimatorName("Base Layer.Swim_Strafe-SM.Swim Tree");
            TRANS_SwimTree_TreadIdlePose = mMotionController.AddAnimatorName("Base Layer.Swim_Strafe-SM.Swim Tree -> Base Layer.Swim_Strafe-SM.TreadIdlePose");
            TRANS_SwimTree_IdlePose = mMotionController.AddAnimatorName("Base Layer.Swim_Strafe-SM.Swim Tree -> Base Layer.Swim_Strafe-SM.IdlePose");
            STATE_IdlePose = mMotionController.AddAnimatorName("Base Layer.Swim_Strafe-SM.IdlePose");
        }

#if UNITY_EDITOR

        private AnimationClip m15640 = null;
        private AnimationClip m15642 = null;
        private AnimationClip m17384 = null;
        private AnimationClip m17382 = null;
        private AnimationClip m17386 = null;
        private AnimationClip m14222 = null;

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

            UnityEditor.Animations.AnimatorStateMachine lSM_23512 = lRootSubStateMachine;
            if (lSM_23512 != null)
            {
                for (int i = lSM_23512.entryTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_23512.RemoveEntryTransition(lSM_23512.entryTransitions[i]);
                }

                for (int i = lSM_23512.anyStateTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_23512.RemoveAnyStateTransition(lSM_23512.anyStateTransitions[i]);
                }

                for (int i = lSM_23512.states.Length - 1; i >= 0; i--)
                {
                    lSM_23512.RemoveState(lSM_23512.states[i].state);
                }

                for (int i = lSM_23512.stateMachines.Length - 1; i >= 0; i--)
                {
                    lSM_23512.RemoveStateMachine(lSM_23512.stateMachines[i].stateMachine);
                }
            }
            else
            {
                lSM_23512 = lSM_23510.AddStateMachine(_EditorAnimatorSMName, new Vector3(408, -84, 0));
            }

            UnityEditor.Animations.AnimatorState lS_N18490 = lSM_23512.AddState("TreadIdlePose", new Vector3(600, 72, 0));
            lS_N18490.speed = 1f;
            lS_N18490.motion = m15640;

            UnityEditor.Animations.AnimatorState lS_N18492 = lSM_23512.AddState("Swim Tree", new Vector3(312, 120, 0));
            lS_N18492.speed = 1f;

            UnityEditor.Animations.BlendTree lM_N18494 = CreateBlendTree("Swim Blend Tree", _EditorAnimatorController, mMotionLayer.AnimatorLayerIndex);
            lM_N18494.blendType = UnityEditor.Animations.BlendTreeType.Simple1D;
            lM_N18494.blendParameter = "InputMagnitude";
            lM_N18494.blendParameterY = "InputX";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
            lM_N18494.useAutomaticThresholds = false;
#endif
            lM_N18494.AddChild(m15642, 0f);

            UnityEditor.Animations.BlendTree lM_N18498 = CreateBlendTree("TreadTree", _EditorAnimatorController, mMotionLayer.AnimatorLayerIndex);
            lM_N18498.blendType = UnityEditor.Animations.BlendTreeType.SimpleDirectional2D;
            lM_N18498.blendParameter = "InputX";
            lM_N18498.blendParameterY = "InputY";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
            lM_N18498.useAutomaticThresholds = true;
#endif
            lM_N18498.AddChild(m15642, new Vector2(0f, 0.1f));
            lM_N18498.AddChild(m15642, new Vector2(-0.1f, 0f));
            lM_N18498.AddChild(m15642, new Vector2(0.1f, 0f));
            lM_N18498.AddChild(m15642, new Vector2(0f, -0.1f));
            lM_N18494.AddChild(lM_N18498, 0.5f);

            UnityEditor.Animations.BlendTree lM_N18502 = CreateBlendTree("SwimTree", _EditorAnimatorController, mMotionLayer.AnimatorLayerIndex);
            lM_N18502.blendType = UnityEditor.Animations.BlendTreeType.SimpleDirectional2D;
            lM_N18502.blendParameter = "InputX";
            lM_N18502.blendParameterY = "InputY";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
            lM_N18502.useAutomaticThresholds = true;
#endif
            lM_N18502.AddChild(m17384, new Vector2(0f, 0.1f));
            lM_N18502.AddChild(m17382, new Vector2(-0.1f, 0f));
            lM_N18502.AddChild(m17386, new Vector2(0.1f, 0f));
            lM_N18502.AddChild(m15642, new Vector2(0f, -0.1f));
            lM_N18494.AddChild(lM_N18502, 1f);
            lS_N18492.motion = lM_N18494;

            UnityEditor.Animations.AnimatorState lS_N18506 = lSM_23512.AddState("IdlePose", new Vector3(600, 156, 0));
            lS_N18506.speed = 1f;
            lS_N18506.motion = m14222;

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N18508 = lRootStateMachine.AddAnyStateTransition(lS_N18492);
            lT_N18508.hasExitTime = false;
            lT_N18508.hasFixedDuration = true;
            lT_N18508.exitTime = 0.9f;
            lT_N18508.duration = 0.4f;
            lT_N18508.offset = 0f;
            lT_N18508.mute = false;
            lT_N18508.solo = false;
            lT_N18508.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 31400f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_N18510 = lS_N18490.AddTransition(lS_N18492);
            lT_N18510.hasExitTime = false;
            lT_N18510.hasFixedDuration = true;
            lT_N18510.exitTime = 0f;
            lT_N18510.duration = 0.2f;
            lT_N18510.offset = 0f;
            lT_N18510.mute = false;
            lT_N18510.solo = false;
            lT_N18510.canTransitionToSelf = true;
            lT_N18510.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.1f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lT_N18512 = lS_N18492.AddTransition(lS_N18490);
            lT_N18512.hasExitTime = false;
            lT_N18512.hasFixedDuration = true;
            lT_N18512.exitTime = 1f;
            lT_N18512.duration = 0.3f;
            lT_N18512.offset = 0f;
            lT_N18512.mute = false;
            lT_N18512.solo = false;
            lT_N18512.canTransitionToSelf = true;
            lT_N18512.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 31405f, "L0MotionPhase");
            lT_N18512.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_N18514 = lS_N18492.AddTransition(lS_N18506);
            lT_N18514.hasExitTime = false;
            lT_N18514.hasFixedDuration = true;
            lT_N18514.exitTime = 0.9286847f;
            lT_N18514.duration = 0.25f;
            lT_N18514.offset = 0f;
            lT_N18514.mute = false;
            lT_N18514.solo = false;
            lT_N18514.canTransitionToSelf = true;
            lT_N18514.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 31406f, "L0MotionPhase");

        }

        /// <summary>
        /// Gathers the animations so we can use them when creating the sub-state machine.
        /// </summary>
        public override void FindAnimations()
        {
            m15640 = FindAnimationClip("Assets/ootii/MotionControllerPacks/Swimming/Content/Animations/Mixamo/treading_water.fbx/TreadIdlePose.anim", "TreadIdlePose");
            m15642 = FindAnimationClip("Assets/ootii/MotionControllerPacks/Swimming/Content/Animations/Mixamo/treading_water.fbx/treading_water.anim", "treading_water");
            m17384 = FindAnimationClip("Assets/ootii/MotionControllerPacks/Swimming/Content/Animations/Mixamo/swimming.fbx/swimming.anim", "swimming");
            m17382 = FindAnimationClip("Assets/ootii/MotionControllerPacks/Swimming/Content/Animations/Mixamo/swimming.fbx/SwimLeft.anim", "SwimLeft");
            m17386 = FindAnimationClip("Assets/ootii/MotionControllerPacks/Swimming/Content/Animations/Mixamo/swimming.fbx/SwimRight.anim", "SwimRight");
            m14222 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdlePose.anim", "IdlePose");

            // Add the remaining functionality
            base.FindAnimations();
        }

        /// <summary>
        /// Used to show the settings that allow us to generate the animator setup.
        /// </summary>
        public override void OnSettingsGUI()
        {
            UnityEditor.EditorGUILayout.IntField(new GUIContent("Phase ID", "Phase ID used to transition to the state."), PHASE_START);
            m15640 = CreateAnimationField("TreadIdlePose", "Assets/ootii/MotionControllerPacks/Swimming/Content/Animations/Mixamo/treading_water.fbx/TreadIdlePose.anim", "TreadIdlePose", m15640);
            m15642 = CreateAnimationField("Swim Tree.treading_water", "Assets/ootii/MotionControllerPacks/Swimming/Content/Animations/Mixamo/treading_water.fbx/treading_water.anim", "treading_water", m15642);
            m17384 = CreateAnimationField("Swim Tree.swimming", "Assets/ootii/MotionControllerPacks/Swimming/Content/Animations/Mixamo/swimming.fbx/swimming.anim", "swimming", m17384);
            m17382 = CreateAnimationField("Swim Tree.SwimLeft", "Assets/ootii/MotionControllerPacks/Swimming/Content/Animations/Mixamo/swimming.fbx/SwimLeft.anim", "SwimLeft", m17382);
            m17386 = CreateAnimationField("Swim Tree.SwimRight", "Assets/ootii/MotionControllerPacks/Swimming/Content/Animations/Mixamo/swimming.fbx/SwimRight.anim", "SwimRight", m17386);
            m14222 = CreateAnimationField("IdlePose", "Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdlePose.anim", "IdlePose", m14222);

            // Add the remaining functionality
            base.OnSettingsGUI();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}