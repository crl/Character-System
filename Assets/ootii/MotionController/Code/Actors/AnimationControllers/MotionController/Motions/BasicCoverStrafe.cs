using System.Collections;
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
    [MotionName("Basic Cover Strafe")]
    [MotionDescription("Shooter game style movement when the character is behind cover. Uses no transitions.")]
    public class BasicCoverStrafe : MotionControllerMotion, IWalkRunMotion, ICoverMotion
    {
        // Constant for error values
        protected const float EPSILON = 0.001f;

        /// <summary>
        /// Trigger values for th emotion
        /// </summary>
        public int PHASE_UNKNOWN = 0;
        public int PHASE_START_WALK = 3600;
        public int PHASE_START_SNEAK = 3605;
        public int PHASE_STOP = 3610;

        /// <summary>
        /// Determines if we're using the IsInMotion() function to verify that
        /// the transition in the animator has occurred for this motion.
        /// </summary>
        public override bool VerifyTransition
        {
            get { return false; }
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
        /// User layer id set for objects that can be used for cover.
        /// </summary>
        public int _CoverLayers = 1;
        public int CoverLayers
        {
            get { return _CoverLayers; }
            set { _CoverLayers = value; }
        }

        /// <summary>
        /// Distance from the wall that the character can be before cover is valid
        /// </summary>
        public float _CoverDistance = 0.91f;
        public virtual float CoverDistance
        {
            get { return _CoverDistance; }
            set { _CoverDistance = value; }
        }

        /// <summary>
        /// Offset for the starting point of the cover ray when shooting for high cover
        /// </summary>
        public float _CoverRayLowHeight = 0.95f;
        public float CoverRayLowHeight
        {
            get { return _CoverRayLowHeight; }
            set { _CoverRayLowHeight = value; }
        }

        /// <summary>
        /// Offset for the starting point of the cover ray when shooting for low cover
        /// </summary>
        public float _CoverRayHighHeight = 1.7f;
        public float CoverRayHighHeight
        {
            get { return _CoverRayHighHeight; }
            set { _CoverRayHighHeight = value; }
        }

        /// <summary>
        /// Determines how far we'll test for a corner in order to shift the camera view
        /// </summary>
        public float _CornerViewDistance = 0.35f;
        public virtual float CornerViewDistance
        {
            get { return _CornerViewDistance; }
            set { _CornerViewDistance = value; }
        }

        /// <summary>
        /// Offset to use with the camera when not at a corner
        /// </summary>
        public float _CameraOffsetX = 0.3f;
        public float CameraOffsetX
        {
            get { return _CameraOffsetX; }
            set { _CameraOffsetX = value; }
        }

        /// <summary>
        /// Offset to use with the camera when we are at a corner
        /// </summary>
        public float _RightCornerCameraOffsetX = 0.35f;
        public float RightCornerCameraOffsetX
        {
            get { return _RightCornerCameraOffsetX; }
            set { _RightCornerCameraOffsetX = value; }
        }

        /// <summary>
        /// Offset to use with the camera when we are at a corner
        /// </summary>
        public float _LeftCornerCameraOffsetX = 0.75f;
        public float LeftCornerCameraOffsetX
        {
            get { return _LeftCornerCameraOffsetX; }
            set { _LeftCornerCameraOffsetX = value; }
        }

        /// <summary>
        /// Speed (in seconds) to reach the target position and rotation
        /// </summary>
        public float _ExitSpeed = 0.4f;
        public float ExitSpeed
        {
            get { return _ExitSpeed; }
            set { _ExitSpeed = value; }
        }

        ///// <summary>
        ///// Camera rig mode to shift to when centered on conver
        ///// </summary>
        //public int _CameraCenterMode = -1;
        //public int CameraCenterMode
        //{
        //    get { return _CameraCenterMode; }
        //    set { _CameraCenterMode = value; }
        //}

        ///// <summary>
        ///// Camera rig mode to shift to when at a corner to the left of the actor
        ///// </summary>
        //public int _CameraLeftMode = -1;
        //public int CameraLeftMode
        //{
        //    get { return _CameraLeftMode; }
        //    set { _CameraLeftMode = value; }
        //}

        ///// <summary>
        ///// Camera rig mode to shift to when at a corner to the right of the actor
        ///// </summary>
        //public int _CameraRightMode = -1;
        //public int CameraRightMode
        //{
        //    get { return _CameraRightMode; }
        //    set { _CameraRightMode = value; }
        //}

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
        /// Camera anchor that we can use for positioning the camera
        /// </summary>
        protected IBaseCameraAnchor mCameraAnchor = null;
        public IBaseCameraAnchor CameraAnchor
        {
            get { return mCameraAnchor; }
        }

        /// <summary>
        /// Determines if we're actively forcing movement
        /// </summary>
        protected bool mIsExiting = false;
        public bool IsExiting
        {
            get { return mIsExiting; }
        }

        /// <summary>
        /// Properties for IWalkRunMotion that have no implmentation
        /// </summary>
        public bool IsRunActive { get { return false; } }
        public bool StartInMove { get { return false; } set { } }
        public bool StartInWalk { get { return false; } set { } }
        public bool StartInRun { get { return false; } set { } }

        /// <summary>
        /// We use these classes to help smooth the input values so that
        /// movement doesn't drop from 1 to 0 immediately.
        /// </summary>
        protected FloatValue mInputX = new FloatValue(0f, 10);
        protected FloatValue mInputY = new FloatValue(0f, 10);
        protected FloatValue mInputMagnitude = new FloatValue(0f, 15);

        // Used to force a change if neede
        protected int mActiveForm = 0;

        // Determines if we pause processing for aiming or shooting
        //protected bool mIsPaused = false;

        // Determines if we're actually in a cover state
        protected bool mIsInCoverState = false;

        // Tracks which way we're facing
        protected bool mIsLeftFacing = true;

        // Keep track of the direction we were facing
        protected bool mLastIsLeftFacing = true;

        // Determine if we found low cover or high cover
        protected bool mIsHighCover = false;

        // Keep track of the kind of cover we were under
        protected bool mLastIsHighCover = false;

        // Distance from the character to the corner
        protected float mCornerDistance = 0f;

        // Tracks the corner view we are in (0 = none, 1 = view right, 2 = view left)
        //protected int mCornerDirection = 0;

        // Tracks the corner view we were in (0 = none, 1 = view right, 2 = view left)
        //protected int mLastCornerDirection = 0;

        // Position of the anchor at the corner (Vector3.zero means its not set)
        protected Vector3 mCornerAnchorPosition = Vector3Ext.Null;

        // Direction we're starting from to rotate
        protected Quaternion mStartRotation = Quaternion.identity;

        // Direction we want to pivot the character to
        protected Quaternion mEndRotation = Quaternion.identity;

        // Normal used to understand how we should be rotated
        protected RaycastHit mLastHitInfo;

        // Average the cover angle to determine how we'll rotate to match
        protected FloatValue mCoverAngle = new FloatValue(0f, 5);

        // Track the current form looking for changes
        protected int mLastCurrentForm = 0;

        // Grab the arms motion if needed
        protected BasicIdleArms mArmsMotion = null;

        /// <summary>
        /// Default constructor
        /// </summary>
        public BasicCoverStrafe()
            : base()
        {
            _Category = EnumMotionCategories.COVER;

            _Priority = 15;
            _ActionAlias = "Cover Toggle";
            _Form = -1;

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "BasicCoverStrafe-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public BasicCoverStrafe(MotionController rController)
            : base(rController)
        {
            _Category = EnumMotionCategories.COVER;

            _Priority = 15;
            _ActionAlias = "Cover Toggle";
            _Form = -1;

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "BasicCoverStrafe-SM"; }
#endif
        }

        /// <summary>
        /// Awake is called after all objects are initialized so you can safely speak to other objects. This is where
        /// reference can be associated.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            // Determine if we have a camera anchor
            if (mMotionController.CameraRig != null)
            {
                Transform lCameraAnchor = mMotionController.CameraRig.Anchor;
                mCameraAnchor = lCameraAnchor.GetComponent<IBaseCameraAnchor>();
            }

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

            // Determine if the activation was pressed
            bool lIsAliasPressed = (_ActionAlias.Length > 0 && mMotionController._InputSource != null && mMotionController._InputSource.IsJustPressed(_ActionAlias));

            if (mParameter == 1)
            {
                mParameter = 0;
                lIsAliasPressed = true;
            }

            if (!lIsAliasPressed)
            {
                return false;
            }

            // Determine if there is cover ahead of us
            if (!RaycastCover(Vector3.zero, mMotionController._Transform.forward, true))
            {
                return false;
            }

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

            // If we're in the exit state (which happens after transitions), exit
            if (mMotionController.State.AnimatorStates[mMotionLayer._AnimatorLayerIndex].StateInfo.IsTag("Exit"))
            {
                return false;
            }

            // Check if the user pressed to exit cover
            bool lIsAliasPressed = (_ActionAlias.Length > 0 && mMotionController._InputSource != null && mMotionController._InputSource.IsJustPressed(_ActionAlias));
            if (lIsAliasPressed)
            {
                ExitCover();
            }

            // Check if our input has us moving away from the wall
            if (mLastHitInfo.collider != null && mMotionController.State.InputMagnitudeTrend.Value > 0.1f)
            {
                Vector3 lInputForward = mMotionController.State.InputForward;
                if (mMotionController._CameraTransform != null) { lInputForward = mMotionController._CameraTransform.rotation * lInputForward; }

                float lAngle = Vector3Ext.HorizontalAngleTo(mLastHitInfo.normal, lInputForward, mMotionController._Transform.up);
                if (Mathf.Abs(lAngle) <= 45f)
                {
                    return false;
                }
            }

            // Check if there is no longer a wall
            if (mLastHitInfo.collider == null)
            {
                // Keep our input forced for the transition out
                mMotionController.ForcedInput.x = mInputX.Average;
                mMotionController.ForcedInput.y = mInputY.Average;

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
            //if (rMotion.Category == EnumMotionCategories.COMBAT_RANGED || 
            //    rMotion.Category == EnumMotionCategories.COMBAT_SHOOTING)
            //{
            //    mIsPaused = true;
            //}

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
            //mIsPaused = false;
            mIsExiting = false;
            mIsLeftFacing = true;
            mLastIsLeftFacing = !mIsLeftFacing;
            mIsInCoverState = false;
            mLastIsHighCover = mIsHighCover;
            mCornerAnchorPosition = Vector3Ext.Null;

            //mCornerDirection = 0;
            //mLastCornerDirection = mCornerDirection;

            mInputX.Clear();
            mInputY.Clear();
            mInputMagnitude.Clear();

            mCoverAngle.Clear();

            // Update the max speed based on our animation
            mMotionController.MaxSpeed = 5.668f;

            // Determine how we'll start our animation
            int lPhase = (mIsHighCover ? PHASE_START_WALK : PHASE_START_SNEAK);

            mStartRotation = mMotionController._Transform.rotation;
            mEndRotation = Quaternion.LookRotation(mLastHitInfo.normal, mMotionController._Transform.up);
            mMotionController.StartCoroutine(mMotionController.MoveAndRotateTo(Vector3.zero, mEndRotation, 0.7f, true, false, true));

            float lAngle = Vector3Ext.HorizontalAngleTo(mMotionController._Transform.forward, mLastHitInfo.normal, mMotionController._Transform.up);
            lPhase = (Mathf.Abs(lAngle) < 30f ? lPhase + 1 : lPhase);

            if (mInputX.Value < -EPSILON) { mIsLeftFacing = false; }
            else if (mInputX.Value > EPSILON) { mIsLeftFacing = true; }

            mActiveForm = (_Form >= 0 ? _Form : mMotionController.CurrentForm);
            mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, lPhase, mActiveForm, (mIsLeftFacing ? 0 : 1), true);

            // Check if we'll override the arms based onthe form
            mLastCurrentForm = mMotionController.CurrentForm;

            mArmsMotion = mMotionController.GetMotion<BasicIdleArms>(2);
            if (mArmsMotion != null)
            {
                if (mArmsMotion.TestActivate(mMotionController.CurrentForm))
                {
                    mMotionController.ActivateMotion(mArmsMotion);
                }
            }

            // Finalize the activation
            return base.Activate(rPrevMotion);
        }

        /// <summary>
        /// Raised when we shut the motion down
        /// </summary>
        public override void Deactivate()
        {
            if (mCameraAnchor != null)
            {
                mCameraAnchor.ClearTarget(3f, 0.8f);
            }

            // Deactivate the arm override if needed
            if (mArmsMotion != null && mArmsMotion.IsActive) { mArmsMotion.Deactivate(); }

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

            mIsInCoverState = mMotionController.State.AnimatorStates[mMotionLayer._AnimatorLayerIndex].StateInfo.IsTag("Cover");

            if (mMotionController.State.AnimatorStates[mMotionLayer._AnimatorLayerIndex].StateInfo.IsTag("PivotToWall"))
            {
                rMovement.x = 0f;
                rMovement.y = 0f;
            }
            else if (mMotionController.State.AnimatorStates[mMotionLayer._AnimatorLayerIndex].StateInfo.IsTag("Pivot180"))
            {
                rMovement.x = 0f;
                rMovement.y = 0f;
                rMovement.z = 0f;
            }
            else
            {
                rMovement.y = 0f;
                rMovement.z = 0f;

                // Override root motion if we're meant to
                if (_WalkSpeed > 0f && rMovement.x != 0f)
                {
                    rMovement.x = Mathf.Sign(rMovement.x) * _WalkSpeed * rDeltaTime;
                }

                // We don't want the root motion to move us backwards
                if (mInputMagnitude.Average == 0f && mInputMagnitude.Value == 0f) { rMovement.x = 0f; }
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
            //if (mIsPaused) { return; }
            if (mIsExiting) { return; }

            mMovement = Vector3.zero;
            mRotation = Quaternion.identity;

            // Smooth the input so we don't start and stop immediately in the blend tree. 
            SmoothInput();

            // Determine how we're facing
            if (mInputX.Value < -EPSILON) { mIsLeftFacing = true; }
            else if (mInputX.Value > EPSILON) { mIsLeftFacing = false; }

            mMotionController.SetAnimatorMotionParameter(mMotionLayer._AnimatorLayerIndex, (mIsLeftFacing ? 0 : 1));

            // Grab the current wall normal
            if (mIsInCoverState)
            {
                RaycastCover(Vector3.zero, -mMotionController._Transform.forward);

                if (mLastHitInfo.collider != null)
                {
                    // Check if we're transitioning from a different cover type
                    if (mIsHighCover != mLastIsHighCover)
                    {
                        int lPhase = (mIsHighCover ? PHASE_START_WALK + 1 : PHASE_START_SNEAK + 1);

                        mActiveForm = (_Form >= 0 ? _Form : mMotionController.CurrentForm);
                        mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, lPhase, mActiveForm, (mIsLeftFacing ? 0 : 1), true);
                    }

                    // We'll rotate to the cover if there's input and a large enough angle
                    if (mInputX.Value != 0f)
                    {
                        float lAngle = Vector3Ext.HorizontalAngleTo(mMotionController._Transform.forward, mLastHitInfo.normal, mMotionController._Transform.up);

                        mCoverAngle.Value = Mathf.Sign(lAngle) * Mathf.Min(90f * Time.deltaTime, Mathf.Abs(lAngle));
                        if (Mathf.Abs(mCoverAngle.Average) > 1f)
                        {
                            mRotation = Quaternion.AngleAxis(mCoverAngle.Average, Vector3.up);
                        }
                    }
                }

                float lCameraOffsetX = CameraOffsetX;

                // Check if we're at a corner
                RaycastCorner(mIsLeftFacing, CornerViewDistance);

                // If we're at a corner, we may need to shift the view
                if (mCornerDistance != 0f)
                {
                    // If we already set the anchor position, don't do it again yet
                    if (mCornerAnchorPosition == Vector3Ext.Null && mActorController.State.Velocity.sqrMagnitude < EPSILON)
                    {
                        float lOffsetX = 0f;

#if OOTII_CC
                        CameraController lCameraController = mMotionController.CameraRig as CameraController;
                        if (lCameraController != null)
                        {
                            YawPitchMotor lMotor = lCameraController.ActiveMotor as YawPitchMotor;
                            if (lMotor != null) { lOffsetX = lMotor.Offset.x; }
                        }
#endif

                        lCameraOffsetX = mCornerDistance + ((mIsLeftFacing ? -LeftCornerCameraOffsetX : RightCornerCameraOffsetX));
                        mCornerAnchorPosition = mMotionController._Transform.position + (mMotionController._Transform.rotation * new Vector3(lCameraOffsetX + lOffsetX, 0f, 0f));

                        if (mCameraAnchor != null)
                        {
                            mCameraAnchor.SetTargetPosition(null, mCornerAnchorPosition, 5f, 0.8f, false);
                        }
                    }
                }
                // Since we're not at a cornder, check the facing direction
                else
                {
                    // Set the facing direction and any offset we may need
                    if (mIsLeftFacing != mLastIsLeftFacing || mCornerAnchorPosition != Vector3Ext.Null)
                    {
                        float lOffsetX = 0f;

#if OOTII_CC
                        CameraController lCameraController = mMotionController.CameraRig as CameraController;
                        if (lCameraController != null)
                        {
                            YawPitchMotor lMotor = lCameraController.ActiveMotor as YawPitchMotor;
                            if (lMotor != null) { lOffsetX = lMotor.Offset.x; }
                        }
#endif

                        if (mCameraAnchor != null)
                        {
                            lOffsetX = (mIsLeftFacing ? -_CameraOffsetX : _CameraOffsetX) + lOffsetX;
                            mCameraAnchor.SetTargetPosition(mMotionController._Transform, new Vector3(lOffsetX, 0f, 0f), 5f, 0.8f, false);
                        }

                        mLastIsLeftFacing = mIsLeftFacing;
                        mCornerAnchorPosition = Vector3Ext.Null;
                    }
                }

                // Check if we'll override the arms based onthe form
                if (mArmsMotion != null && !mArmsMotion.IsActive && mLastCurrentForm != mMotionController.CurrentForm)
                { 
                    mLastCurrentForm = mMotionController.CurrentForm;
                    if (mArmsMotion.TestActivate(mMotionController.CurrentForm))
                    {
                        mMotionController.ActivateMotion(mArmsMotion);
                    }
                }
            }
        }

        /// <summary>
        /// Forces us to exit cover and then deactivate
        /// </summary>
        /// <param name="rExtrapolatePosition">Determines if we extrapolate the postion from our camera rig.</param>
        /// <param name="rUseCameraRotation">Determines if we keep the camera rotation vs. the anchor rotation.</param>
        public virtual void ExitCover(bool rExtrapolatePosition = false, bool rUseCameraRotation = false)
        {
            if (rExtrapolatePosition && mMotionController.CameraRig != null)
            {
                // Fix the camera anchor to the current spot
                if (mCameraAnchor != null)
                {
                    Vector3 lPosition = mCameraAnchor.Transform.position;
                    mCameraAnchor.SetTargetPosition(null, lPosition, 3f, 0.8f, false);
                }

                // Determine the expected anchor position
                Vector3 lAnchorPosition;
                Quaternion lAnchorRotation;
                mMotionController.CameraRig.ExtrapolateAnchorPosition(out lAnchorPosition, out lAnchorRotation);

                // However, we don't want the rotation of the "anchor". Instead, we want the rotation of the camera.
                if (rUseCameraRotation)
                {
                    float lAngle = Vector3Ext.HorizontalAngleTo(mMotionController._Transform.forward, mMotionController._CameraTransform.forward, mMotionController._Transform.up);
                    lAnchorRotation = lAnchorRotation * Quaternion.AngleAxis(lAngle, Vector3.up);
                }

                // Adjust so that we rotate along the shortest direction
                lAnchorRotation = Quaternion.AngleAxis((mIsLeftFacing ? 0.5f : -0.5f), Vector3.up) * lAnchorRotation;

                // Exit to the 
                ExitCover(lAnchorPosition, lAnchorRotation);
            }
            else
            {
                // Deactivate the arm override if needed
                if (mArmsMotion != null && mArmsMotion.IsActive) { mArmsMotion.Deactivate(); }

                // Move the anchor back to its root
                if (mCameraAnchor != null)
                {
                    mCameraAnchor.ClearTarget(3f, 0.8f);
                }

                mStartRotation = mMotionController._Transform.rotation;
                mEndRotation = mStartRotation * Quaternion.AngleAxis(180f + (mIsLeftFacing ? 1f : -1f), Vector3.up);
                mMotionController.StartCoroutine(ExitCoverInternal(Vector3.zero, mEndRotation, ExitSpeed, true, false, true));

                mActiveForm = (_Form >= 0 ? _Form : mMotionController.CurrentForm);
                mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, PHASE_STOP, mActiveForm, (mIsLeftFacing ? 0 : 1), true);
            }
        }

        /// <summary>
        /// Forces us to exit cover and then deactivate
        /// </summary>
        /// <param name="rPosition">Position to exit to</param>
        /// <param name="rRotation">Rotation to exit to</param>
        public virtual void ExitCover(Vector3 rPosition, Quaternion rRotation)
        {
            // Deactivate the arm override if needed
            if (mArmsMotion != null && mArmsMotion.IsActive) {  mArmsMotion.Deactivate(); }

            // Start the process of rotating the character
            mMotionController.StartCoroutine(ExitCoverInternal(rPosition, rRotation, ExitSpeed, true, true, true));

            // Play the stop cover animation
            mActiveForm = (_Form >= 0 ? _Form : mMotionController.CurrentForm);
            mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, PHASE_STOP, mActiveForm, 0, true);
        }

        /// <summary>
        /// Internal co-routine so we can set the forcing flag as needed
        /// </summary>
        /// <param name="rPosition">Position to exit to.</param>
        /// <param name="rRotation">Rotation to exit to.</param>
        /// <param name="rTime">Time (in seconds) to get to the position and rotation.</param>
        /// <param name="rSmooth">Determines if we add easing.</param>
        /// <param name="rMove">Determines if we move.</param>
        /// <param name="rRotate">Determines if we rotate.</param>
        /// <returns></returns>
        protected IEnumerator ExitCoverInternal(Vector3 rPosition, Quaternion rRotation, float rTime, bool rSmooth, bool rMove, bool rRotate)
        {
            mIsExiting = true;

            if (rTime == 0f)
            {
                if (rMove)
                {
                    mMotionController._ActorController.Move(rPosition - mMotionController._Transform.position);
                    //mMotionController._ActorController.SetPosition(rPosition);
                }

                if (rRotate) { mMotionController._ActorController.SetRotation(rRotation); }
            }
            else
            {
                float lPercent = 0f;
                float lStartTime = Time.time;

                Vector3 lOldPosition = mMotionController._Transform.position;
                Vector3 lNewPosition = rPosition;

                Quaternion lOldRotation = mMotionController._Transform.rotation;

                // Based on the facing direction, we want to force the direction of our rotation. This does that
                float lRotationAngle = Vector3Ext.HorizontalAngleTo(mMotionController._Transform.forward, rRotation.Forward(), mMotionController._Transform.up);

                if (mIsLeftFacing)
                {
                    if (lRotationAngle > 0f) { lRotationAngle = -360f + lRotationAngle; }
                }
                else
                {
                    if (lRotationAngle < 0f) { lRotationAngle = 360f + lRotationAngle; }
                }

                // Begin moving and rotating
                while (lPercent < 1f && (rMove || rRotate))
                {
                    lOldPosition = mMotionController._Transform.position;

                    lPercent = Mathf.Clamp01((Time.time - lStartTime) / rTime);
                    if (rSmooth) { lPercent = NumberHelper.EaseInOutCubic(lPercent); }

                    if (rMove)
                    {

                        Vector3 lStepPosition = Vector3.Lerp(lOldPosition, lNewPosition, lPercent);

                        //Utilities.Debug.Log.FileWrite("pct:" + lPercent.ToString("f3") + " move " + StringHelper.ToSimpleString(lStepPosition - lOldPosition));

                        mMotionController._ActorController.Move(lStepPosition - lOldPosition);
                        //mMotionController._ActorController.SetPosition(lStepPosition);
                    }

                    if (rRotate)
                    {
                        float lStepAngle = Mathf.Lerp(0f, lRotationAngle, lPercent);
                        Quaternion lStepRotation = lOldRotation * Quaternion.AngleAxis(lStepAngle, Vector3.up);
                        mMotionController._ActorController.SetRotation(lStepRotation);
                    }

                    yield return null;
                }
            }

            if (mCameraAnchor != null)
            {
                mCameraAnchor.Transform.rotation = mMotionController._Transform.rotation;
                mCameraAnchor.ClearTarget(true);
            }

            mIsExiting = false;
        }

        /// <summary>
        /// We smooth the input so that we don't start and stop immediately in the blend tree. That can create pops.
        /// </summary>
        protected void SmoothInput()
        {
            MotionState lState = mMotionController.State;

            // Convert the input to radial so we deal with keyboard and gamepad input the same.
            float lInputMax = (IsRunActive ? 1f : 0.5f);

            // Force the input relative to the actor
            Vector3 lInputForward = lState.InputForward;
            if (mMotionController._CameraTransform != null) { lInputForward = mMotionController._CameraTransform.rotation * lInputForward; }
            lInputForward = Quaternion.Inverse(mMotionController._Transform.rotation) * lInputForward;

            float lInputX = Mathf.Clamp(lInputForward.x, -lInputMax, lInputMax);
            float lInputY = Mathf.Clamp(lInputForward.z, -lInputMax, lInputMax);
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
        /// Cast the cover ray to determine if cover is valid
        /// </summary>
        protected bool RaycastCover(Vector3 rOffset, Vector3 rDirection, bool rUseCircularCast = false)
        {
            bool lIsHit = false;
            RaycastHit lHitInfo;

            mLastIsHighCover = mIsHighCover;

            // Test for high cover
            Vector3 lRayStart = mMotionController._Transform.position + (mMotionController._Transform.up * CoverRayHighHeight);

            //Graphics.GraphicsManager.DrawLine(lRayStart, lRayStart + (rDirection * CoverDistance), Color.green, null, 1f);

            if (rUseCircularCast)
            {
                lIsHit = RaycastExt.SafeCircularCast(lRayStart, rDirection, mMotionController._Transform.up, out mLastHitInfo, CoverDistance, 30f, CoverLayers, null, mMotionController._Transform);
            }
            else
            {
                lIsHit = RaycastExt.SafeRaycast(lRayStart, rDirection, out mLastHitInfo, CoverDistance, CoverLayers, mMotionController._Transform);
            }

            mIsHighCover = true;

            // Test for low cover
            lRayStart = mMotionController._Transform.position + (mMotionController._Transform.up * CoverRayLowHeight);

            //Graphics.GraphicsManager.DrawLine(lRayStart, lRayStart + (rDirection * CoverDistance), Color.green, null, 1f);

            if (rUseCircularCast)
            {
                lIsHit = RaycastExt.SafeCircularCast(lRayStart, rDirection, mMotionController._Transform.up, out lHitInfo, CoverDistance, 30f, CoverLayers, null, mMotionController._Transform);
            }
            else
            {
                lIsHit = RaycastExt.SafeRaycast(lRayStart, rDirection, out lHitInfo, CoverDistance, CoverLayers, mMotionController._Transform);
            }

            // Check if the lower hit overrides the higher hit
            if (lIsHit)
            {
                if (mLastHitInfo.distance == 0f || lHitInfo.distance < mLastHitInfo.distance)
                {
                    mIsHighCover = false;
                    mLastHitInfo = lHitInfo;
                }
            }

            // Get out successfully if there's a hit
            if (mLastHitInfo.distance > 0f)
            {
                return true;
            }

            // When no cover is found, clear the info
            mLastHitInfo = RaycastExt.EmptyHitInfo;

            return false;
        }

        /// <summary>
        /// Cast an offset ray to determine if we're at a corner
        /// </summary>
        /// <returns>The distance to the corner if found or 0 if no corner found.</returns>
        protected float RaycastCorner(bool mIsFacingLeft, float rViewDistance)
        {
            mCornerDistance = 0f;
            //mCornerDirection = 0;

            // Check if we can shoot a ray from our character to the direction we're moving
            Vector3 lRayStart = mMotionController._Transform.position + (mMotionController._Transform.up * CoverRayLowHeight);
            Vector3 lRayDirection = (mIsFacingLeft ? -mMotionController._Transform.right : mMotionController._Transform.right);

            //Graphics.GraphicsManager.DrawLine(lRayStart, lRayStart + (lRayDirection * CoverDistance * 1.0f), Color.blue, null, 1f);

            bool lIsHit = RaycastExt.SafeRaycast(lRayStart, lRayDirection, CoverDistance * 1.0f, CoverLayers, mMotionController._Transform);
            if (lIsHit)
            {
                return 0f;
            }

            // Check if we can shoot a ray from our offset behind the character
            lRayStart = mMotionController._Transform.position + (mMotionController._Transform.up * CoverRayLowHeight);
            lRayStart = lRayStart + (mMotionController._Transform.right * (mIsFacingLeft ? -rViewDistance : rViewDistance));
            lRayDirection = -mMotionController._Transform.forward;

            //Graphics.GraphicsManager.DrawLine(lRayStart, lRayStart + (lRayDirection * CoverDistance * 1.0f), Color.cyan, null, 1f);

            lIsHit = RaycastExt.SafeRaycast(lRayStart, lRayDirection, CoverDistance * 1.0f, CoverLayers, mMotionController._Transform);
            if (lIsHit)
            {
                return 0f;
            }

            // Find the corner by stepping the ray until we don't hit
            float lStepDistance = 0.05f;
            for (float lDistance = 0f; lDistance <= CoverDistance * 1.0f; lDistance += lStepDistance)
            {
                lRayStart = mMotionController._Transform.position + (mMotionController._Transform.up * CoverRayLowHeight);
                lRayStart = lRayStart + (mMotionController._Transform.right * (mIsFacingLeft ? -lDistance : lDistance));

                //Graphics.GraphicsManager.DrawLine(lRayStart, lRayStart + (lRayDirection * CoverDistance * 1.0f), Color.yellow, null, 1f);

                lIsHit = RaycastExt.SafeRaycast(lRayStart, lRayDirection, CoverDistance * 1.0f, CoverLayers, mMotionController._Transform);
                if (!lIsHit)
                {
                    mCornerDistance = (mIsFacingLeft ? -1f : 1f) * Mathf.Max(lDistance - (lStepDistance * 0.5f), 0f);

                    lRayStart = mMotionController._Transform.position + (mMotionController._Transform.up * CoverRayLowHeight);
                    lRayStart = lRayStart + (mMotionController._Transform.right * mCornerDistance);
                    //Graphics.GraphicsManager.DrawLine(lRayStart, lRayStart + (lRayDirection * CoverDistance * 1.0f), Color.red, null, 1f);

                    //mCornerDirection = (mIsFacingLeft ? 1 : 2);
                    return Mathf.Max(lDistance - lStepDistance, 0f);
                }
            }

            return 0f;
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

            if (EditorHelper.TextField("Activation Alias", "If set, the action alias that will activate this motion when pressed.", ActionAlias, mMotionController))
            {
                lIsDirty = true;
                ActionAlias = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.FloatField("Cover Distance", "Max distance we can be from cover to activate it.", CoverDistance, mMotionController))
            {
                lIsDirty = true;
                CoverDistance = EditorHelper.FieldFloatValue;
            }

            // Cast heights
            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(new GUIContent("Cover Cast Heights", "Heights (low and and high) to cast the ray to determine different levels of cover."), GUILayout.Width(EditorGUIUtility.labelWidth - 4f));

            if (EditorHelper.FloatField(CoverRayLowHeight, "Cover Low", mMotionController, 0f, 20f))
            {
                lIsDirty = true;
                CoverRayLowHeight = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField(CoverRayHighHeight, "Cover High", mMotionController, 0f, 20f))
            {
                lIsDirty = true;
                CoverRayHighHeight = EditorHelper.FieldFloatValue;
            }

            GUILayout.EndHorizontal();

            int lNewCoverLayers = EditorHelper.LayerMaskField(new GUIContent("Cover Layers", "Layers that identies objects that can be used as cover."), CoverLayers);
            if (lNewCoverLayers != CoverLayers)
            {
                lIsDirty = true;
                CoverLayers = lNewCoverLayers;
            }

            GUILayout.Space(5f);

            if (EditorHelper.FloatField("Corner Distance", "Determines how far out we'll test for a corner to adjust the camera view.", CornerViewDistance, mMotionController))
            {
                lIsDirty = true;
                CornerViewDistance = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField("Camera Offset", "Camera distance from character when in cover.", CameraOffsetX, mMotionController))
            {
                lIsDirty = true;
                CameraOffsetX = EditorHelper.FieldFloatValue;
            }

            // Camera Offsets
            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(new GUIContent("Corner Offset", "Camera distance from character when in cover and at a corner (to left and to right)."), GUILayout.Width(EditorGUIUtility.labelWidth - 4f));

            if (EditorHelper.FloatField(LeftCornerCameraOffsetX, "Left Corner Offset", mMotionController, 0f, 20f))
            {
                lIsDirty = true;
                LeftCornerCameraOffsetX = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField(RightCornerCameraOffsetX, "Right Corner Offset", mMotionController, 0f, 20f))
            {
                lIsDirty = true;
                RightCornerCameraOffsetX = EditorHelper.FieldFloatValue;
            }

            GUILayout.EndHorizontal();

            //if (EditorHelper.IntField("Center View Mode", "Camera mode when the character is at the center of cover (-1 means do not set).", CameraCenterMode, mMotionController))
            //{
            //    lIsDirty = true;
            //    CameraCenterMode = EditorHelper.FieldIntValue;
            //}

            //if (EditorHelper.IntField("Left View Mode", "Camera mode when the view has a corner to its left (-1 means do not set).", CameraLeftMode, mMotionController))
            //{
            //    lIsDirty = true;
            //    CameraLeftMode = EditorHelper.FieldIntValue;
            //}

            //if (EditorHelper.IntField("Right View Mode", "Camera mode when the view has a corner to its right (-1 means do not set).", CameraRightMode, mMotionController))
            //{
            //    lIsDirty = true;
            //    CameraRightMode = EditorHelper.FieldIntValue;
            //}

            GUILayout.Space(5f);

            if (EditorHelper.FloatField("Exit Speed", "Time in seconds to exit to the target position and rotation.", ExitSpeed, mMotionController))
            {
                lIsDirty = true;
                ExitSpeed = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField("Walk Speed", "Speed (units per second) to move when walking. Set to 0 to use root-motion.", WalkSpeed, mMotionController))
            {
                lIsDirty = true;
                WalkSpeed = EditorHelper.FieldFloatValue;
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
        public int STATE_Start = -1;
        public int STATE_IdleToCoverWalkIdle = -1;
        public int STATE_CoverWalkLeftIdle = -1;
        public int STATE_CoverWalkRightIdle = -1;
        public int STATE_IdleToCoverSneakIdle = -1;
        public int STATE_CoverSneakLeftIdle = -1;
        public int STATE_CoverSneakRightIdle = -1;
        public int STATE_CoverWalkLeft = -1;
        public int STATE_CoverWalkRight = -1;
        public int STATE_CoverSneakLeft = -1;
        public int STATE_CoverSneakRight = -1;
        public int STATE_CoverWalkToIdleLeft = -1;
        public int STATE_CoverWalkToIdleRight = -1;
        public int STATE_CoverSneakToIdleLeft = -1;
        public int STATE_CoverSneakToIdleRight = -1;
        public int STATE_IdlePoseExit = -1;
        public int TRANS_AnyState_IdleToCoverWalkIdle = -1;
        public int TRANS_EntryState_IdleToCoverWalkIdle = -1;
        public int TRANS_AnyState_IdleToCoverSneakIdle = -1;
        public int TRANS_EntryState_IdleToCoverSneakIdle = -1;
        public int TRANS_AnyState_CoverWalkLeft = -1;
        public int TRANS_EntryState_CoverWalkLeft = -1;
        public int TRANS_AnyState_CoverWalkRight = -1;
        public int TRANS_EntryState_CoverWalkRight = -1;
        public int TRANS_AnyState_CoverSneakLeft = -1;
        public int TRANS_EntryState_CoverSneakLeft = -1;
        public int TRANS_AnyState_CoverSneakRight = -1;
        public int TRANS_EntryState_CoverSneakRight = -1;
        public int TRANS_IdleToCoverWalkIdle_CoverWalkLeftIdle = -1;
        public int TRANS_IdleToCoverWalkIdle_CoverWalkLeft = -1;
        public int TRANS_IdleToCoverWalkIdle_CoverWalkRight = -1;
        public int TRANS_IdleToCoverWalkIdle_CoverWalkRightIdle = -1;
        public int TRANS_CoverWalkLeftIdle_CoverWalkRightIdle = -1;
        public int TRANS_CoverWalkLeftIdle_CoverWalkLeft = -1;
        public int TRANS_CoverWalkLeftIdle_CoverWalkToIdleLeft = -1;
        public int TRANS_CoverWalkRightIdle_CoverWalkLeftIdle = -1;
        public int TRANS_CoverWalkRightIdle_CoverWalkRight = -1;
        public int TRANS_CoverWalkRightIdle_CoverWalkToIdleRight = -1;
        public int TRANS_IdleToCoverSneakIdle_CoverSneakLeftIdle = -1;
        public int TRANS_IdleToCoverSneakIdle_CoverSneakLeft = -1;
        public int TRANS_IdleToCoverSneakIdle_CoverSneakRight = -1;
        public int TRANS_IdleToCoverSneakIdle_CoverSneakRightIdle = -1;
        public int TRANS_CoverSneakLeftIdle_CoverSneakRightIdle = -1;
        public int TRANS_CoverSneakLeftIdle_CoverSneakLeft = -1;
        public int TRANS_CoverSneakLeftIdle_CoverSneakToIdleLeft = -1;
        public int TRANS_CoverSneakRightIdle_CoverSneakLeftIdle = -1;
        public int TRANS_CoverSneakRightIdle_CoverSneakRight = -1;
        public int TRANS_CoverSneakRightIdle_CoverSneakToIdleRight = -1;
        public int TRANS_CoverWalkLeft_CoverWalkRight = -1;
        public int TRANS_CoverWalkLeft_CoverWalkLeftIdle = -1;
        public int TRANS_CoverWalkRight_CoverWalkLeft = -1;
        public int TRANS_CoverWalkRight_CoverWalkRightIdle = -1;
        public int TRANS_CoverSneakLeft_CoverSneakRight = -1;
        public int TRANS_CoverSneakLeft_CoverSneakLeftIdle = -1;
        public int TRANS_CoverSneakRight_CoverSneakLeft = -1;
        public int TRANS_CoverSneakRight_CoverSneakRightIdle = -1;
        public int TRANS_CoverWalkToIdleLeft_IdlePoseExit = -1;
        public int TRANS_CoverWalkToIdleRight_IdlePoseExit = -1;
        public int TRANS_CoverSneakToIdleLeft_IdlePoseExit = -1;
        public int TRANS_CoverSneakToIdleRight_IdlePoseExit = -1;

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
                    if (lStateID == STATE_Start) { return true; }
                    if (lStateID == STATE_IdleToCoverWalkIdle) { return true; }
                    if (lStateID == STATE_CoverWalkLeftIdle) { return true; }
                    if (lStateID == STATE_CoverWalkRightIdle) { return true; }
                    if (lStateID == STATE_IdleToCoverSneakIdle) { return true; }
                    if (lStateID == STATE_CoverSneakLeftIdle) { return true; }
                    if (lStateID == STATE_CoverSneakRightIdle) { return true; }
                    if (lStateID == STATE_CoverWalkLeft) { return true; }
                    if (lStateID == STATE_CoverWalkRight) { return true; }
                    if (lStateID == STATE_CoverSneakLeft) { return true; }
                    if (lStateID == STATE_CoverSneakRight) { return true; }
                    if (lStateID == STATE_CoverWalkToIdleLeft) { return true; }
                    if (lStateID == STATE_CoverWalkToIdleRight) { return true; }
                    if (lStateID == STATE_CoverSneakToIdleLeft) { return true; }
                    if (lStateID == STATE_CoverSneakToIdleRight) { return true; }
                    if (lStateID == STATE_IdlePoseExit) { return true; }
                }

                if (lTransitionID == TRANS_AnyState_IdleToCoverWalkIdle) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleToCoverWalkIdle) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleToCoverSneakIdle) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleToCoverSneakIdle) { return true; }
                if (lTransitionID == TRANS_AnyState_CoverWalkLeft) { return true; }
                if (lTransitionID == TRANS_EntryState_CoverWalkLeft) { return true; }
                if (lTransitionID == TRANS_AnyState_CoverWalkRight) { return true; }
                if (lTransitionID == TRANS_EntryState_CoverWalkRight) { return true; }
                if (lTransitionID == TRANS_AnyState_CoverSneakLeft) { return true; }
                if (lTransitionID == TRANS_EntryState_CoverSneakLeft) { return true; }
                if (lTransitionID == TRANS_AnyState_CoverSneakRight) { return true; }
                if (lTransitionID == TRANS_EntryState_CoverSneakRight) { return true; }
                if (lTransitionID == TRANS_IdleToCoverWalkIdle_CoverWalkLeftIdle) { return true; }
                if (lTransitionID == TRANS_IdleToCoverWalkIdle_CoverWalkLeft) { return true; }
                if (lTransitionID == TRANS_IdleToCoverWalkIdle_CoverWalkRight) { return true; }
                if (lTransitionID == TRANS_IdleToCoverWalkIdle_CoverWalkRightIdle) { return true; }
                if (lTransitionID == TRANS_CoverWalkLeftIdle_CoverWalkRightIdle) { return true; }
                if (lTransitionID == TRANS_CoverWalkLeftIdle_CoverWalkLeft) { return true; }
                if (lTransitionID == TRANS_CoverWalkLeftIdle_CoverWalkToIdleLeft) { return true; }
                if (lTransitionID == TRANS_CoverWalkRightIdle_CoverWalkLeftIdle) { return true; }
                if (lTransitionID == TRANS_CoverWalkRightIdle_CoverWalkRight) { return true; }
                if (lTransitionID == TRANS_CoverWalkRightIdle_CoverWalkToIdleRight) { return true; }
                if (lTransitionID == TRANS_IdleToCoverSneakIdle_CoverSneakLeftIdle) { return true; }
                if (lTransitionID == TRANS_IdleToCoverSneakIdle_CoverSneakLeft) { return true; }
                if (lTransitionID == TRANS_IdleToCoverSneakIdle_CoverSneakRight) { return true; }
                if (lTransitionID == TRANS_IdleToCoverSneakIdle_CoverSneakRightIdle) { return true; }
                if (lTransitionID == TRANS_CoverSneakLeftIdle_CoverSneakRightIdle) { return true; }
                if (lTransitionID == TRANS_CoverSneakLeftIdle_CoverSneakLeft) { return true; }
                if (lTransitionID == TRANS_CoverSneakLeftIdle_CoverSneakToIdleLeft) { return true; }
                if (lTransitionID == TRANS_CoverSneakRightIdle_CoverSneakLeftIdle) { return true; }
                if (lTransitionID == TRANS_CoverSneakRightIdle_CoverSneakRight) { return true; }
                if (lTransitionID == TRANS_CoverSneakRightIdle_CoverSneakToIdleRight) { return true; }
                if (lTransitionID == TRANS_CoverWalkLeft_CoverWalkRight) { return true; }
                if (lTransitionID == TRANS_CoverWalkLeft_CoverWalkLeftIdle) { return true; }
                if (lTransitionID == TRANS_CoverWalkRight_CoverWalkLeft) { return true; }
                if (lTransitionID == TRANS_CoverWalkRight_CoverWalkRightIdle) { return true; }
                if (lTransitionID == TRANS_CoverSneakLeft_CoverSneakRight) { return true; }
                if (lTransitionID == TRANS_CoverSneakLeft_CoverSneakLeftIdle) { return true; }
                if (lTransitionID == TRANS_CoverSneakRight_CoverSneakLeft) { return true; }
                if (lTransitionID == TRANS_CoverSneakRight_CoverSneakRightIdle) { return true; }
                if (lTransitionID == TRANS_CoverWalkToIdleLeft_IdlePoseExit) { return true; }
                if (lTransitionID == TRANS_CoverWalkToIdleRight_IdlePoseExit) { return true; }
                if (lTransitionID == TRANS_CoverSneakToIdleLeft_IdlePoseExit) { return true; }
                if (lTransitionID == TRANS_CoverSneakToIdleRight_IdlePoseExit) { return true; }
                return false;
            }
        }

        /// <summary>
        /// Used to determine if the actor is in one of the states for this motion
        /// </summary>
        /// <returns></returns>
        public override bool IsMotionState(int rStateID)
        {
            if (rStateID == STATE_Start) { return true; }
            if (rStateID == STATE_IdleToCoverWalkIdle) { return true; }
            if (rStateID == STATE_CoverWalkLeftIdle) { return true; }
            if (rStateID == STATE_CoverWalkRightIdle) { return true; }
            if (rStateID == STATE_IdleToCoverSneakIdle) { return true; }
            if (rStateID == STATE_CoverSneakLeftIdle) { return true; }
            if (rStateID == STATE_CoverSneakRightIdle) { return true; }
            if (rStateID == STATE_CoverWalkLeft) { return true; }
            if (rStateID == STATE_CoverWalkRight) { return true; }
            if (rStateID == STATE_CoverSneakLeft) { return true; }
            if (rStateID == STATE_CoverSneakRight) { return true; }
            if (rStateID == STATE_CoverWalkToIdleLeft) { return true; }
            if (rStateID == STATE_CoverWalkToIdleRight) { return true; }
            if (rStateID == STATE_CoverSneakToIdleLeft) { return true; }
            if (rStateID == STATE_CoverSneakToIdleRight) { return true; }
            if (rStateID == STATE_IdlePoseExit) { return true; }
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
                if (rStateID == STATE_Start) { return true; }
                if (rStateID == STATE_IdleToCoverWalkIdle) { return true; }
                if (rStateID == STATE_CoverWalkLeftIdle) { return true; }
                if (rStateID == STATE_CoverWalkRightIdle) { return true; }
                if (rStateID == STATE_IdleToCoverSneakIdle) { return true; }
                if (rStateID == STATE_CoverSneakLeftIdle) { return true; }
                if (rStateID == STATE_CoverSneakRightIdle) { return true; }
                if (rStateID == STATE_CoverWalkLeft) { return true; }
                if (rStateID == STATE_CoverWalkRight) { return true; }
                if (rStateID == STATE_CoverSneakLeft) { return true; }
                if (rStateID == STATE_CoverSneakRight) { return true; }
                if (rStateID == STATE_CoverWalkToIdleLeft) { return true; }
                if (rStateID == STATE_CoverWalkToIdleRight) { return true; }
                if (rStateID == STATE_CoverSneakToIdleLeft) { return true; }
                if (rStateID == STATE_CoverSneakToIdleRight) { return true; }
                if (rStateID == STATE_IdlePoseExit) { return true; }
            }

            if (rTransitionID == TRANS_AnyState_IdleToCoverWalkIdle) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleToCoverWalkIdle) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleToCoverSneakIdle) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleToCoverSneakIdle) { return true; }
            if (rTransitionID == TRANS_AnyState_CoverWalkLeft) { return true; }
            if (rTransitionID == TRANS_EntryState_CoverWalkLeft) { return true; }
            if (rTransitionID == TRANS_AnyState_CoverWalkRight) { return true; }
            if (rTransitionID == TRANS_EntryState_CoverWalkRight) { return true; }
            if (rTransitionID == TRANS_AnyState_CoverSneakLeft) { return true; }
            if (rTransitionID == TRANS_EntryState_CoverSneakLeft) { return true; }
            if (rTransitionID == TRANS_AnyState_CoverSneakRight) { return true; }
            if (rTransitionID == TRANS_EntryState_CoverSneakRight) { return true; }
            if (rTransitionID == TRANS_IdleToCoverWalkIdle_CoverWalkLeftIdle) { return true; }
            if (rTransitionID == TRANS_IdleToCoverWalkIdle_CoverWalkLeft) { return true; }
            if (rTransitionID == TRANS_IdleToCoverWalkIdle_CoverWalkRight) { return true; }
            if (rTransitionID == TRANS_IdleToCoverWalkIdle_CoverWalkRightIdle) { return true; }
            if (rTransitionID == TRANS_CoverWalkLeftIdle_CoverWalkRightIdle) { return true; }
            if (rTransitionID == TRANS_CoverWalkLeftIdle_CoverWalkLeft) { return true; }
            if (rTransitionID == TRANS_CoverWalkLeftIdle_CoverWalkToIdleLeft) { return true; }
            if (rTransitionID == TRANS_CoverWalkRightIdle_CoverWalkLeftIdle) { return true; }
            if (rTransitionID == TRANS_CoverWalkRightIdle_CoverWalkRight) { return true; }
            if (rTransitionID == TRANS_CoverWalkRightIdle_CoverWalkToIdleRight) { return true; }
            if (rTransitionID == TRANS_IdleToCoverSneakIdle_CoverSneakLeftIdle) { return true; }
            if (rTransitionID == TRANS_IdleToCoverSneakIdle_CoverSneakLeft) { return true; }
            if (rTransitionID == TRANS_IdleToCoverSneakIdle_CoverSneakRight) { return true; }
            if (rTransitionID == TRANS_IdleToCoverSneakIdle_CoverSneakRightIdle) { return true; }
            if (rTransitionID == TRANS_CoverSneakLeftIdle_CoverSneakRightIdle) { return true; }
            if (rTransitionID == TRANS_CoverSneakLeftIdle_CoverSneakLeft) { return true; }
            if (rTransitionID == TRANS_CoverSneakLeftIdle_CoverSneakToIdleLeft) { return true; }
            if (rTransitionID == TRANS_CoverSneakRightIdle_CoverSneakLeftIdle) { return true; }
            if (rTransitionID == TRANS_CoverSneakRightIdle_CoverSneakRight) { return true; }
            if (rTransitionID == TRANS_CoverSneakRightIdle_CoverSneakToIdleRight) { return true; }
            if (rTransitionID == TRANS_CoverWalkLeft_CoverWalkRight) { return true; }
            if (rTransitionID == TRANS_CoverWalkLeft_CoverWalkLeftIdle) { return true; }
            if (rTransitionID == TRANS_CoverWalkRight_CoverWalkLeft) { return true; }
            if (rTransitionID == TRANS_CoverWalkRight_CoverWalkRightIdle) { return true; }
            if (rTransitionID == TRANS_CoverSneakLeft_CoverSneakRight) { return true; }
            if (rTransitionID == TRANS_CoverSneakLeft_CoverSneakLeftIdle) { return true; }
            if (rTransitionID == TRANS_CoverSneakRight_CoverSneakLeft) { return true; }
            if (rTransitionID == TRANS_CoverSneakRight_CoverSneakRightIdle) { return true; }
            if (rTransitionID == TRANS_CoverWalkToIdleLeft_IdlePoseExit) { return true; }
            if (rTransitionID == TRANS_CoverWalkToIdleRight_IdlePoseExit) { return true; }
            if (rTransitionID == TRANS_CoverSneakToIdleLeft_IdlePoseExit) { return true; }
            if (rTransitionID == TRANS_CoverSneakToIdleRight_IdlePoseExit) { return true; }
            return false;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            string lLayer = mMotionController.Animator.GetLayerName(mMotionLayer._AnimatorLayerIndex);
            TRANS_AnyState_IdleToCoverWalkIdle = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".BasicCoverStrafe-SM.IdleToCoverWalkIdle");
            TRANS_EntryState_IdleToCoverWalkIdle = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".BasicCoverStrafe-SM.IdleToCoverWalkIdle");
            TRANS_AnyState_IdleToCoverSneakIdle = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".BasicCoverStrafe-SM.IdleToCoverSneakIdle");
            TRANS_EntryState_IdleToCoverSneakIdle = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".BasicCoverStrafe-SM.IdleToCoverSneakIdle");
            TRANS_AnyState_CoverWalkLeft = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".BasicCoverStrafe-SM.CoverWalkLeft");
            TRANS_EntryState_CoverWalkLeft = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".BasicCoverStrafe-SM.CoverWalkLeft");
            TRANS_AnyState_CoverWalkRight = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".BasicCoverStrafe-SM.CoverWalkRight");
            TRANS_EntryState_CoverWalkRight = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".BasicCoverStrafe-SM.CoverWalkRight");
            TRANS_AnyState_CoverSneakLeft = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".BasicCoverStrafe-SM.CoverSneakLeft");
            TRANS_EntryState_CoverSneakLeft = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".BasicCoverStrafe-SM.CoverSneakLeft");
            TRANS_AnyState_CoverSneakRight = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".BasicCoverStrafe-SM.CoverSneakRight");
            TRANS_EntryState_CoverSneakRight = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".BasicCoverStrafe-SM.CoverSneakRight");
            STATE_Start = mMotionController.AddAnimatorName("" + lLayer + ".Start");
            STATE_IdleToCoverWalkIdle = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.IdleToCoverWalkIdle");
            TRANS_IdleToCoverWalkIdle_CoverWalkLeftIdle = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.IdleToCoverWalkIdle -> " + lLayer + ".BasicCoverStrafe-SM.CoverWalkLeftIdle");
            TRANS_IdleToCoverWalkIdle_CoverWalkLeft = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.IdleToCoverWalkIdle -> " + lLayer + ".BasicCoverStrafe-SM.CoverWalkLeft");
            TRANS_IdleToCoverWalkIdle_CoverWalkRight = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.IdleToCoverWalkIdle -> " + lLayer + ".BasicCoverStrafe-SM.CoverWalkRight");
            TRANS_IdleToCoverWalkIdle_CoverWalkRightIdle = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.IdleToCoverWalkIdle -> " + lLayer + ".BasicCoverStrafe-SM.CoverWalkRightIdle");
            STATE_CoverWalkLeftIdle = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverWalkLeftIdle");
            TRANS_CoverWalkLeftIdle_CoverWalkRightIdle = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverWalkLeftIdle -> " + lLayer + ".BasicCoverStrafe-SM.CoverWalkRightIdle");
            TRANS_CoverWalkLeftIdle_CoverWalkLeft = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverWalkLeftIdle -> " + lLayer + ".BasicCoverStrafe-SM.CoverWalkLeft");
            TRANS_CoverWalkLeftIdle_CoverWalkToIdleLeft = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverWalkLeftIdle -> " + lLayer + ".BasicCoverStrafe-SM.CoverWalkToIdleLeft");
            STATE_CoverWalkRightIdle = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverWalkRightIdle");
            TRANS_CoverWalkRightIdle_CoverWalkLeftIdle = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverWalkRightIdle -> " + lLayer + ".BasicCoverStrafe-SM.CoverWalkLeftIdle");
            TRANS_CoverWalkRightIdle_CoverWalkRight = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverWalkRightIdle -> " + lLayer + ".BasicCoverStrafe-SM.CoverWalkRight");
            TRANS_CoverWalkRightIdle_CoverWalkToIdleRight = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverWalkRightIdle -> " + lLayer + ".BasicCoverStrafe-SM.CoverWalkToIdleRight");
            STATE_IdleToCoverSneakIdle = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.IdleToCoverSneakIdle");
            TRANS_IdleToCoverSneakIdle_CoverSneakLeftIdle = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.IdleToCoverSneakIdle -> " + lLayer + ".BasicCoverStrafe-SM.CoverSneakLeftIdle");
            TRANS_IdleToCoverSneakIdle_CoverSneakLeft = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.IdleToCoverSneakIdle -> " + lLayer + ".BasicCoverStrafe-SM.CoverSneakLeft");
            TRANS_IdleToCoverSneakIdle_CoverSneakRight = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.IdleToCoverSneakIdle -> " + lLayer + ".BasicCoverStrafe-SM.CoverSneakRight");
            TRANS_IdleToCoverSneakIdle_CoverSneakRightIdle = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.IdleToCoverSneakIdle -> " + lLayer + ".BasicCoverStrafe-SM.CoverSneakRightIdle");
            STATE_CoverSneakLeftIdle = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverSneakLeftIdle");
            TRANS_CoverSneakLeftIdle_CoverSneakRightIdle = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverSneakLeftIdle -> " + lLayer + ".BasicCoverStrafe-SM.CoverSneakRightIdle");
            TRANS_CoverSneakLeftIdle_CoverSneakLeft = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverSneakLeftIdle -> " + lLayer + ".BasicCoverStrafe-SM.CoverSneakLeft");
            TRANS_CoverSneakLeftIdle_CoverSneakToIdleLeft = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverSneakLeftIdle -> " + lLayer + ".BasicCoverStrafe-SM.CoverSneakToIdleLeft");
            STATE_CoverSneakRightIdle = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverSneakRightIdle");
            TRANS_CoverSneakRightIdle_CoverSneakLeftIdle = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverSneakRightIdle -> " + lLayer + ".BasicCoverStrafe-SM.CoverSneakLeftIdle");
            TRANS_CoverSneakRightIdle_CoverSneakRight = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverSneakRightIdle -> " + lLayer + ".BasicCoverStrafe-SM.CoverSneakRight");
            TRANS_CoverSneakRightIdle_CoverSneakToIdleRight = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverSneakRightIdle -> " + lLayer + ".BasicCoverStrafe-SM.CoverSneakToIdleRight");
            STATE_CoverWalkLeft = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverWalkLeft");
            TRANS_CoverWalkLeft_CoverWalkRight = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverWalkLeft -> " + lLayer + ".BasicCoverStrafe-SM.CoverWalkRight");
            TRANS_CoverWalkLeft_CoverWalkLeftIdle = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverWalkLeft -> " + lLayer + ".BasicCoverStrafe-SM.CoverWalkLeftIdle");
            STATE_CoverWalkRight = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverWalkRight");
            TRANS_CoverWalkRight_CoverWalkLeft = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverWalkRight -> " + lLayer + ".BasicCoverStrafe-SM.CoverWalkLeft");
            TRANS_CoverWalkRight_CoverWalkRightIdle = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverWalkRight -> " + lLayer + ".BasicCoverStrafe-SM.CoverWalkRightIdle");
            STATE_CoverSneakLeft = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverSneakLeft");
            TRANS_CoverSneakLeft_CoverSneakRight = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverSneakLeft -> " + lLayer + ".BasicCoverStrafe-SM.CoverSneakRight");
            TRANS_CoverSneakLeft_CoverSneakLeftIdle = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverSneakLeft -> " + lLayer + ".BasicCoverStrafe-SM.CoverSneakLeftIdle");
            STATE_CoverSneakRight = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverSneakRight");
            TRANS_CoverSneakRight_CoverSneakLeft = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverSneakRight -> " + lLayer + ".BasicCoverStrafe-SM.CoverSneakLeft");
            TRANS_CoverSneakRight_CoverSneakRightIdle = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverSneakRight -> " + lLayer + ".BasicCoverStrafe-SM.CoverSneakRightIdle");
            STATE_CoverWalkToIdleLeft = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverWalkToIdleLeft");
            TRANS_CoverWalkToIdleLeft_IdlePoseExit = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverWalkToIdleLeft -> " + lLayer + ".BasicCoverStrafe-SM.IdlePoseExit");
            STATE_CoverWalkToIdleRight = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverWalkToIdleRight");
            TRANS_CoverWalkToIdleRight_IdlePoseExit = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverWalkToIdleRight -> " + lLayer + ".BasicCoverStrafe-SM.IdlePoseExit");
            STATE_CoverSneakToIdleLeft = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverSneakToIdleLeft");
            TRANS_CoverSneakToIdleLeft_IdlePoseExit = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverSneakToIdleLeft -> " + lLayer + ".BasicCoverStrafe-SM.IdlePoseExit");
            STATE_CoverSneakToIdleRight = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverSneakToIdleRight");
            TRANS_CoverSneakToIdleRight_IdlePoseExit = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.CoverSneakToIdleRight -> " + lLayer + ".BasicCoverStrafe-SM.IdlePoseExit");
            STATE_IdlePoseExit = mMotionController.AddAnimatorName("" + lLayer + ".BasicCoverStrafe-SM.IdlePoseExit");
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

            UnityEditor.Animations.AnimatorStateMachine lSSM_32288 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "BasicCoverStrafe-SM");
            if (lSSM_32288 == null) { lSSM_32288 = lLayerStateMachine.AddStateMachine("BasicCoverStrafe-SM", new Vector3(408, -960, 0)); }

            UnityEditor.Animations.AnimatorState lState_33676 = MotionControllerMotion.EditorFindState(lSSM_32288, "IdleToCoverWalkIdle");
            if (lState_33676 == null) { lState_33676 = lSSM_32288.AddState("IdleToCoverWalkIdle", new Vector3(348, -84, 0)); }
            lState_33676.speed = 1f;
            lState_33676.mirror = false;
            lState_33676.tag = "PivotToWall";
            lState_33676.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Cover/Cover.fbx", "IdleToCoverWalkIdle");

            UnityEditor.Animations.AnimatorState lState_33678 = MotionControllerMotion.EditorFindState(lSSM_32288, "CoverWalkLeftIdle");
            if (lState_33678 == null) { lState_33678 = lSSM_32288.AddState("CoverWalkLeftIdle", new Vector3(240, 48, 0)); }
            lState_33678.speed = 1f;
            lState_33678.mirror = false;
            lState_33678.tag = "Cover";
            lState_33678.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Cover/Cover.fbx", "CoverWalkLeftIdle");

            UnityEditor.Animations.AnimatorState lState_33680 = MotionControllerMotion.EditorFindState(lSSM_32288, "CoverWalkRightIdle");
            if (lState_33680 == null) { lState_33680 = lSSM_32288.AddState("CoverWalkRightIdle", new Vector3(468, 48, 0)); }
            lState_33680.speed = 1f;
            lState_33680.mirror = false;
            lState_33680.tag = "Cover";
            lState_33680.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Cover/Cover.fbx", "CoverWalkRightIdle");

            UnityEditor.Animations.AnimatorState lState_33682 = MotionControllerMotion.EditorFindState(lSSM_32288, "IdleToCoverSneakIdle");
            if (lState_33682 == null) { lState_33682 = lSSM_32288.AddState("IdleToCoverSneakIdle", new Vector3(360, 312, 0)); }
            lState_33682.speed = 1f;
            lState_33682.mirror = false;
            lState_33682.tag = "PivotToWall";
            lState_33682.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Cover/Cover.fbx", "IdleToCoverSneakIdle");

            UnityEditor.Animations.AnimatorState lState_33684 = MotionControllerMotion.EditorFindState(lSSM_32288, "CoverSneakLeftIdle");
            if (lState_33684 == null) { lState_33684 = lSSM_32288.AddState("CoverSneakLeftIdle", new Vector3(240, 444, 0)); }
            lState_33684.speed = 1f;
            lState_33684.mirror = false;
            lState_33684.tag = "Cover";
            lState_33684.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Cover/Cover.fbx", "CoverSneakLeftIdle");

            UnityEditor.Animations.AnimatorState lState_33686 = MotionControllerMotion.EditorFindState(lSSM_32288, "CoverSneakRightIdle");
            if (lState_33686 == null) { lState_33686 = lSSM_32288.AddState("CoverSneakRightIdle", new Vector3(468, 444, 0)); }
            lState_33686.speed = 1f;
            lState_33686.mirror = false;
            lState_33686.tag = "Cover";
            lState_33686.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Cover/Cover.fbx", "CoverSneakRightIdle");

            UnityEditor.Animations.AnimatorState lState_33688 = MotionControllerMotion.EditorFindState(lSSM_32288, "CoverWalkLeft");
            if (lState_33688 == null) { lState_33688 = lSSM_32288.AddState("CoverWalkLeft", new Vector3(240, -12, 0)); }
            lState_33688.speed = 1f;
            lState_33688.mirror = false;
            lState_33688.tag = "Cover";
            lState_33688.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Cover/Cover.fbx", "CoverWalkLeft");

            UnityEditor.Animations.AnimatorState lState_33690 = MotionControllerMotion.EditorFindState(lSSM_32288, "CoverWalkRight");
            if (lState_33690 == null) { lState_33690 = lSSM_32288.AddState("CoverWalkRight", new Vector3(468, -12, 0)); }
            lState_33690.speed = 1f;
            lState_33690.mirror = false;
            lState_33690.tag = "Cover";
            lState_33690.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Cover/Cover.fbx", "CoverWalkRight");

            UnityEditor.Animations.AnimatorState lState_33692 = MotionControllerMotion.EditorFindState(lSSM_32288, "CoverSneakLeft");
            if (lState_33692 == null) { lState_33692 = lSSM_32288.AddState("CoverSneakLeft", new Vector3(240, 384, 0)); }
            lState_33692.speed = 1f;
            lState_33692.mirror = false;
            lState_33692.tag = "Cover";
            lState_33692.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Cover/Cover.fbx", "CoverSneakLeft");

            UnityEditor.Animations.AnimatorState lState_33694 = MotionControllerMotion.EditorFindState(lSSM_32288, "CoverSneakRight");
            if (lState_33694 == null) { lState_33694 = lSSM_32288.AddState("CoverSneakRight", new Vector3(468, 384, 0)); }
            lState_33694.speed = 1f;
            lState_33694.mirror = false;
            lState_33694.tag = "Cover";
            lState_33694.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Cover/Cover.fbx", "CoverSneakRight");

            UnityEditor.Animations.AnimatorState lState_33696 = MotionControllerMotion.EditorFindState(lSSM_32288, "CoverWalkToIdleLeft");
            if (lState_33696 == null) { lState_33696 = lSSM_32288.AddState("CoverWalkToIdleLeft", new Vector3(240, 108, 0)); }
            lState_33696.speed = 1.3f;
            lState_33696.mirror = false;
            lState_33696.tag = "Pivot180";
            lState_33696.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Cover/Cover.fbx", "CoverWalkToIdleLeft");

            UnityEditor.Animations.AnimatorState lState_33698 = MotionControllerMotion.EditorFindState(lSSM_32288, "CoverWalkToIdleRight");
            if (lState_33698 == null) { lState_33698 = lSSM_32288.AddState("CoverWalkToIdleRight", new Vector3(468, 108, 0)); }
            lState_33698.speed = 1.3f;
            lState_33698.mirror = false;
            lState_33698.tag = "Pivot180";
            lState_33698.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Cover/Cover.fbx", "CoverWalkToIdleRight");

            UnityEditor.Animations.AnimatorState lState_33700 = MotionControllerMotion.EditorFindState(lSSM_32288, "CoverSneakToIdleLeft");
            if (lState_33700 == null) { lState_33700 = lSSM_32288.AddState("CoverSneakToIdleLeft", new Vector3(240, 504, 0)); }
            lState_33700.speed = 1.3f;
            lState_33700.mirror = false;
            lState_33700.tag = "Pivot180";
            lState_33700.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Cover/Cover.fbx", "CoverSneakToIdleLeft");

            UnityEditor.Animations.AnimatorState lState_33702 = MotionControllerMotion.EditorFindState(lSSM_32288, "CoverSneakToIdleRight");
            if (lState_33702 == null) { lState_33702 = lSSM_32288.AddState("CoverSneakToIdleRight", new Vector3(468, 504, 0)); }
            lState_33702.speed = 1.3f;
            lState_33702.mirror = false;
            lState_33702.tag = "Pivot180";
            lState_33702.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Cover/Cover.fbx", "CoverSneakToIdleRight");

            UnityEditor.Animations.AnimatorState lState_33704 = MotionControllerMotion.EditorFindState(lSSM_32288, "IdlePoseExit");
            if (lState_33704 == null) { lState_33704 = lSSM_32288.AddState("IdlePoseExit", new Vector3(780, 180, 0)); }
            lState_33704.speed = 1f;
            lState_33704.mirror = false;
            lState_33704.tag = "Exit";
            lState_33704.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx", "IdlePose");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_33596 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_33676, 0);
            if (lAnyTransition_33596 == null) { lAnyTransition_33596 = lLayerStateMachine.AddAnyStateTransition(lState_33676); }
            lAnyTransition_33596.isExit = false;
            lAnyTransition_33596.hasExitTime = false;
            lAnyTransition_33596.hasFixedDuration = true;
            lAnyTransition_33596.exitTime = 0.75f;
            lAnyTransition_33596.duration = 0.2f;
            lAnyTransition_33596.offset = 0f;
            lAnyTransition_33596.mute = false;
            lAnyTransition_33596.solo = false;
            lAnyTransition_33596.canTransitionToSelf = true;
            lAnyTransition_33596.orderedInterruption = true;
            lAnyTransition_33596.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_33596.conditions.Length - 1; i >= 0; i--) { lAnyTransition_33596.RemoveCondition(lAnyTransition_33596.conditions[i]); }
            lAnyTransition_33596.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3600f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_33598 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_33682, 0);
            if (lAnyTransition_33598 == null) { lAnyTransition_33598 = lLayerStateMachine.AddAnyStateTransition(lState_33682); }
            lAnyTransition_33598.isExit = false;
            lAnyTransition_33598.hasExitTime = false;
            lAnyTransition_33598.hasFixedDuration = true;
            lAnyTransition_33598.exitTime = 0.75f;
            lAnyTransition_33598.duration = 0.2f;
            lAnyTransition_33598.offset = 0f;
            lAnyTransition_33598.mute = false;
            lAnyTransition_33598.solo = false;
            lAnyTransition_33598.canTransitionToSelf = true;
            lAnyTransition_33598.orderedInterruption = true;
            lAnyTransition_33598.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_33598.conditions.Length - 1; i >= 0; i--) { lAnyTransition_33598.RemoveCondition(lAnyTransition_33598.conditions[i]); }
            lAnyTransition_33598.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3605f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_33600 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_33688, 0);
            if (lAnyTransition_33600 == null) { lAnyTransition_33600 = lLayerStateMachine.AddAnyStateTransition(lState_33688); }
            lAnyTransition_33600.isExit = false;
            lAnyTransition_33600.hasExitTime = false;
            lAnyTransition_33600.hasFixedDuration = true;
            lAnyTransition_33600.exitTime = 0.75f;
            lAnyTransition_33600.duration = 0.25f;
            lAnyTransition_33600.offset = 0f;
            lAnyTransition_33600.mute = false;
            lAnyTransition_33600.solo = false;
            lAnyTransition_33600.canTransitionToSelf = true;
            lAnyTransition_33600.orderedInterruption = true;
            lAnyTransition_33600.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_33600.conditions.Length - 1; i >= 0; i--) { lAnyTransition_33600.RemoveCondition(lAnyTransition_33600.conditions[i]); }
            lAnyTransition_33600.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3601f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_33600.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_33602 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_33690, 0);
            if (lAnyTransition_33602 == null) { lAnyTransition_33602 = lLayerStateMachine.AddAnyStateTransition(lState_33690); }
            lAnyTransition_33602.isExit = false;
            lAnyTransition_33602.hasExitTime = false;
            lAnyTransition_33602.hasFixedDuration = true;
            lAnyTransition_33602.exitTime = 0.75f;
            lAnyTransition_33602.duration = 0.25f;
            lAnyTransition_33602.offset = 0f;
            lAnyTransition_33602.mute = false;
            lAnyTransition_33602.solo = false;
            lAnyTransition_33602.canTransitionToSelf = true;
            lAnyTransition_33602.orderedInterruption = true;
            lAnyTransition_33602.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_33602.conditions.Length - 1; i >= 0; i--) { lAnyTransition_33602.RemoveCondition(lAnyTransition_33602.conditions[i]); }
            lAnyTransition_33602.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3601f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_33602.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_33604 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_33692, 0);
            if (lAnyTransition_33604 == null) { lAnyTransition_33604 = lLayerStateMachine.AddAnyStateTransition(lState_33692); }
            lAnyTransition_33604.isExit = false;
            lAnyTransition_33604.hasExitTime = false;
            lAnyTransition_33604.hasFixedDuration = true;
            lAnyTransition_33604.exitTime = 0.75f;
            lAnyTransition_33604.duration = 0.25f;
            lAnyTransition_33604.offset = 0f;
            lAnyTransition_33604.mute = false;
            lAnyTransition_33604.solo = false;
            lAnyTransition_33604.canTransitionToSelf = true;
            lAnyTransition_33604.orderedInterruption = true;
            lAnyTransition_33604.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_33604.conditions.Length - 1; i >= 0; i--) { lAnyTransition_33604.RemoveCondition(lAnyTransition_33604.conditions[i]); }
            lAnyTransition_33604.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3606f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_33604.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_33606 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_33694, 0);
            if (lAnyTransition_33606 == null) { lAnyTransition_33606 = lLayerStateMachine.AddAnyStateTransition(lState_33694); }
            lAnyTransition_33606.isExit = false;
            lAnyTransition_33606.hasExitTime = false;
            lAnyTransition_33606.hasFixedDuration = true;
            lAnyTransition_33606.exitTime = 0.75f;
            lAnyTransition_33606.duration = 0.25f;
            lAnyTransition_33606.offset = 0f;
            lAnyTransition_33606.mute = false;
            lAnyTransition_33606.solo = false;
            lAnyTransition_33606.canTransitionToSelf = true;
            lAnyTransition_33606.orderedInterruption = true;
            lAnyTransition_33606.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_33606.conditions.Length - 1; i >= 0; i--) { lAnyTransition_33606.RemoveCondition(lAnyTransition_33606.conditions[i]); }
            lAnyTransition_33606.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3606f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_33606.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39008 = MotionControllerMotion.EditorFindTransition(lState_33676, lState_33678, 0);
            if (lTransition_39008 == null) { lTransition_39008 = lState_33676.AddTransition(lState_33678); }
            lTransition_39008.isExit = false;
            lTransition_39008.hasExitTime = true;
            lTransition_39008.hasFixedDuration = true;
            lTransition_39008.exitTime = 0.5f;
            lTransition_39008.duration = 0.2f;
            lTransition_39008.offset = 0f;
            lTransition_39008.mute = false;
            lTransition_39008.solo = false;
            lTransition_39008.canTransitionToSelf = true;
            lTransition_39008.orderedInterruption = true;
            lTransition_39008.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39008.conditions.Length - 1; i >= 0; i--) { lTransition_39008.RemoveCondition(lTransition_39008.conditions[i]); }
            lTransition_39008.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.3f, "InputMagnitude");
            lTransition_39008.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39010 = MotionControllerMotion.EditorFindTransition(lState_33676, lState_33688, 0);
            if (lTransition_39010 == null) { lTransition_39010 = lState_33676.AddTransition(lState_33688); }
            lTransition_39010.isExit = false;
            lTransition_39010.hasExitTime = true;
            lTransition_39010.hasFixedDuration = true;
            lTransition_39010.exitTime = 0.6f;
            lTransition_39010.duration = 0.2f;
            lTransition_39010.offset = 0f;
            lTransition_39010.mute = false;
            lTransition_39010.solo = false;
            lTransition_39010.canTransitionToSelf = true;
            lTransition_39010.orderedInterruption = true;
            lTransition_39010.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39010.conditions.Length - 1; i >= 0; i--) { lTransition_39010.RemoveCondition(lTransition_39010.conditions[i]); }
            lTransition_39010.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.4f, "InputMagnitude");
            lTransition_39010.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39012 = MotionControllerMotion.EditorFindTransition(lState_33676, lState_33690, 0);
            if (lTransition_39012 == null) { lTransition_39012 = lState_33676.AddTransition(lState_33690); }
            lTransition_39012.isExit = false;
            lTransition_39012.hasExitTime = true;
            lTransition_39012.hasFixedDuration = true;
            lTransition_39012.exitTime = 0.5470365f;
            lTransition_39012.duration = 0.2f;
            lTransition_39012.offset = 0f;
            lTransition_39012.mute = false;
            lTransition_39012.solo = false;
            lTransition_39012.canTransitionToSelf = true;
            lTransition_39012.orderedInterruption = true;
            lTransition_39012.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39012.conditions.Length - 1; i >= 0; i--) { lTransition_39012.RemoveCondition(lTransition_39012.conditions[i]); }
            lTransition_39012.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.4f, "InputMagnitude");
            lTransition_39012.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39014 = MotionControllerMotion.EditorFindTransition(lState_33676, lState_33680, 0);
            if (lTransition_39014 == null) { lTransition_39014 = lState_33676.AddTransition(lState_33680); }
            lTransition_39014.isExit = false;
            lTransition_39014.hasExitTime = true;
            lTransition_39014.hasFixedDuration = true;
            lTransition_39014.exitTime = 0.5470365f;
            lTransition_39014.duration = 0.2f;
            lTransition_39014.offset = 0f;
            lTransition_39014.mute = false;
            lTransition_39014.solo = false;
            lTransition_39014.canTransitionToSelf = true;
            lTransition_39014.orderedInterruption = true;
            lTransition_39014.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39014.conditions.Length - 1; i >= 0; i--) { lTransition_39014.RemoveCondition(lTransition_39014.conditions[i]); }
            lTransition_39014.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.3f, "InputMagnitude");
            lTransition_39014.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39016 = MotionControllerMotion.EditorFindTransition(lState_33678, lState_33680, 0);
            if (lTransition_39016 == null) { lTransition_39016 = lState_33678.AddTransition(lState_33680); }
            lTransition_39016.isExit = false;
            lTransition_39016.hasExitTime = false;
            lTransition_39016.hasFixedDuration = true;
            lTransition_39016.exitTime = 0f;
            lTransition_39016.duration = 0.25f;
            lTransition_39016.offset = 0f;
            lTransition_39016.mute = false;
            lTransition_39016.solo = false;
            lTransition_39016.canTransitionToSelf = true;
            lTransition_39016.orderedInterruption = true;
            lTransition_39016.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39016.conditions.Length - 1; i >= 0; i--) { lTransition_39016.RemoveCondition(lTransition_39016.conditions[i]); }
            lTransition_39016.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");
            lTransition_39016.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionPhase");
            lTransition_39016.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39018 = MotionControllerMotion.EditorFindTransition(lState_33678, lState_33688, 0);
            if (lTransition_39018 == null) { lTransition_39018 = lState_33678.AddTransition(lState_33688); }
            lTransition_39018.isExit = false;
            lTransition_39018.hasExitTime = false;
            lTransition_39018.hasFixedDuration = true;
            lTransition_39018.exitTime = 0f;
            lTransition_39018.duration = 0.1f;
            lTransition_39018.offset = 0.2728711f;
            lTransition_39018.mute = false;
            lTransition_39018.solo = false;
            lTransition_39018.canTransitionToSelf = true;
            lTransition_39018.orderedInterruption = true;
            lTransition_39018.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39018.conditions.Length - 1; i >= 0; i--) { lTransition_39018.RemoveCondition(lTransition_39018.conditions[i]); }
            lTransition_39018.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.1f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39020 = MotionControllerMotion.EditorFindTransition(lState_33678, lState_33696, 0);
            if (lTransition_39020 == null) { lTransition_39020 = lState_33678.AddTransition(lState_33696); }
            lTransition_39020.isExit = false;
            lTransition_39020.hasExitTime = false;
            lTransition_39020.hasFixedDuration = true;
            lTransition_39020.exitTime = 0f;
            lTransition_39020.duration = 0.1f;
            lTransition_39020.offset = 0f;
            lTransition_39020.mute = false;
            lTransition_39020.solo = false;
            lTransition_39020.canTransitionToSelf = true;
            lTransition_39020.orderedInterruption = true;
            lTransition_39020.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39020.conditions.Length - 1; i >= 0; i--) { lTransition_39020.RemoveCondition(lTransition_39020.conditions[i]); }
            lTransition_39020.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3610f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39022 = MotionControllerMotion.EditorFindTransition(lState_33680, lState_33678, 0);
            if (lTransition_39022 == null) { lTransition_39022 = lState_33680.AddTransition(lState_33678); }
            lTransition_39022.isExit = false;
            lTransition_39022.hasExitTime = false;
            lTransition_39022.hasFixedDuration = true;
            lTransition_39022.exitTime = 0f;
            lTransition_39022.duration = 0.25f;
            lTransition_39022.offset = 0f;
            lTransition_39022.mute = false;
            lTransition_39022.solo = false;
            lTransition_39022.canTransitionToSelf = true;
            lTransition_39022.orderedInterruption = true;
            lTransition_39022.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39022.conditions.Length - 1; i >= 0; i--) { lTransition_39022.RemoveCondition(lTransition_39022.conditions[i]); }
            lTransition_39022.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");
            lTransition_39022.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionPhase");
            lTransition_39022.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39024 = MotionControllerMotion.EditorFindTransition(lState_33680, lState_33690, 0);
            if (lTransition_39024 == null) { lTransition_39024 = lState_33680.AddTransition(lState_33690); }
            lTransition_39024.isExit = false;
            lTransition_39024.hasExitTime = false;
            lTransition_39024.hasFixedDuration = true;
            lTransition_39024.exitTime = 0f;
            lTransition_39024.duration = 0.1f;
            lTransition_39024.offset = 0.7009962f;
            lTransition_39024.mute = false;
            lTransition_39024.solo = false;
            lTransition_39024.canTransitionToSelf = true;
            lTransition_39024.orderedInterruption = true;
            lTransition_39024.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39024.conditions.Length - 1; i >= 0; i--) { lTransition_39024.RemoveCondition(lTransition_39024.conditions[i]); }
            lTransition_39024.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.1f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39026 = MotionControllerMotion.EditorFindTransition(lState_33680, lState_33698, 0);
            if (lTransition_39026 == null) { lTransition_39026 = lState_33680.AddTransition(lState_33698); }
            lTransition_39026.isExit = false;
            lTransition_39026.hasExitTime = false;
            lTransition_39026.hasFixedDuration = true;
            lTransition_39026.exitTime = 0f;
            lTransition_39026.duration = 0.1f;
            lTransition_39026.offset = 0f;
            lTransition_39026.mute = false;
            lTransition_39026.solo = false;
            lTransition_39026.canTransitionToSelf = true;
            lTransition_39026.orderedInterruption = true;
            lTransition_39026.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39026.conditions.Length - 1; i >= 0; i--) { lTransition_39026.RemoveCondition(lTransition_39026.conditions[i]); }
            lTransition_39026.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3610f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39028 = MotionControllerMotion.EditorFindTransition(lState_33682, lState_33684, 0);
            if (lTransition_39028 == null) { lTransition_39028 = lState_33682.AddTransition(lState_33684); }
            lTransition_39028.isExit = false;
            lTransition_39028.hasExitTime = true;
            lTransition_39028.hasFixedDuration = true;
            lTransition_39028.exitTime = 0.5470365f;
            lTransition_39028.duration = 0.2f;
            lTransition_39028.offset = 0f;
            lTransition_39028.mute = false;
            lTransition_39028.solo = false;
            lTransition_39028.canTransitionToSelf = true;
            lTransition_39028.orderedInterruption = true;
            lTransition_39028.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39028.conditions.Length - 1; i >= 0; i--) { lTransition_39028.RemoveCondition(lTransition_39028.conditions[i]); }
            lTransition_39028.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.3f, "InputMagnitude");
            lTransition_39028.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39030 = MotionControllerMotion.EditorFindTransition(lState_33682, lState_33692, 0);
            if (lTransition_39030 == null) { lTransition_39030 = lState_33682.AddTransition(lState_33692); }
            lTransition_39030.isExit = false;
            lTransition_39030.hasExitTime = true;
            lTransition_39030.hasFixedDuration = true;
            lTransition_39030.exitTime = 0.5470365f;
            lTransition_39030.duration = 0.2f;
            lTransition_39030.offset = 0f;
            lTransition_39030.mute = false;
            lTransition_39030.solo = false;
            lTransition_39030.canTransitionToSelf = true;
            lTransition_39030.orderedInterruption = true;
            lTransition_39030.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39030.conditions.Length - 1; i >= 0; i--) { lTransition_39030.RemoveCondition(lTransition_39030.conditions[i]); }
            lTransition_39030.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.4f, "InputMagnitude");
            lTransition_39030.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39032 = MotionControllerMotion.EditorFindTransition(lState_33682, lState_33694, 0);
            if (lTransition_39032 == null) { lTransition_39032 = lState_33682.AddTransition(lState_33694); }
            lTransition_39032.isExit = false;
            lTransition_39032.hasExitTime = true;
            lTransition_39032.hasFixedDuration = true;
            lTransition_39032.exitTime = 0.5470365f;
            lTransition_39032.duration = 0.2f;
            lTransition_39032.offset = 0f;
            lTransition_39032.mute = false;
            lTransition_39032.solo = false;
            lTransition_39032.canTransitionToSelf = true;
            lTransition_39032.orderedInterruption = true;
            lTransition_39032.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39032.conditions.Length - 1; i >= 0; i--) { lTransition_39032.RemoveCondition(lTransition_39032.conditions[i]); }
            lTransition_39032.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.4f, "InputMagnitude");
            lTransition_39032.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39034 = MotionControllerMotion.EditorFindTransition(lState_33682, lState_33686, 0);
            if (lTransition_39034 == null) { lTransition_39034 = lState_33682.AddTransition(lState_33686); }
            lTransition_39034.isExit = false;
            lTransition_39034.hasExitTime = true;
            lTransition_39034.hasFixedDuration = true;
            lTransition_39034.exitTime = 0.5470365f;
            lTransition_39034.duration = 0.2f;
            lTransition_39034.offset = 0f;
            lTransition_39034.mute = false;
            lTransition_39034.solo = false;
            lTransition_39034.canTransitionToSelf = true;
            lTransition_39034.orderedInterruption = true;
            lTransition_39034.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39034.conditions.Length - 1; i >= 0; i--) { lTransition_39034.RemoveCondition(lTransition_39034.conditions[i]); }
            lTransition_39034.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.3f, "InputMagnitude");
            lTransition_39034.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39036 = MotionControllerMotion.EditorFindTransition(lState_33684, lState_33686, 0);
            if (lTransition_39036 == null) { lTransition_39036 = lState_33684.AddTransition(lState_33686); }
            lTransition_39036.isExit = false;
            lTransition_39036.hasExitTime = false;
            lTransition_39036.hasFixedDuration = true;
            lTransition_39036.exitTime = 0f;
            lTransition_39036.duration = 0.25f;
            lTransition_39036.offset = 0f;
            lTransition_39036.mute = false;
            lTransition_39036.solo = false;
            lTransition_39036.canTransitionToSelf = true;
            lTransition_39036.orderedInterruption = true;
            lTransition_39036.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39036.conditions.Length - 1; i >= 0; i--) { lTransition_39036.RemoveCondition(lTransition_39036.conditions[i]); }
            lTransition_39036.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");
            lTransition_39036.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39038 = MotionControllerMotion.EditorFindTransition(lState_33684, lState_33692, 0);
            if (lTransition_39038 == null) { lTransition_39038 = lState_33684.AddTransition(lState_33692); }
            lTransition_39038.isExit = false;
            lTransition_39038.hasExitTime = false;
            lTransition_39038.hasFixedDuration = true;
            lTransition_39038.exitTime = 0f;
            lTransition_39038.duration = 0.1f;
            lTransition_39038.offset = 0.3826591f;
            lTransition_39038.mute = false;
            lTransition_39038.solo = false;
            lTransition_39038.canTransitionToSelf = true;
            lTransition_39038.orderedInterruption = true;
            lTransition_39038.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39038.conditions.Length - 1; i >= 0; i--) { lTransition_39038.RemoveCondition(lTransition_39038.conditions[i]); }
            lTransition_39038.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39040 = MotionControllerMotion.EditorFindTransition(lState_33684, lState_33700, 0);
            if (lTransition_39040 == null) { lTransition_39040 = lState_33684.AddTransition(lState_33700); }
            lTransition_39040.isExit = false;
            lTransition_39040.hasExitTime = false;
            lTransition_39040.hasFixedDuration = true;
            lTransition_39040.exitTime = 0f;
            lTransition_39040.duration = 0.1f;
            lTransition_39040.offset = 0f;
            lTransition_39040.mute = false;
            lTransition_39040.solo = false;
            lTransition_39040.canTransitionToSelf = true;
            lTransition_39040.orderedInterruption = true;
            lTransition_39040.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39040.conditions.Length - 1; i >= 0; i--) { lTransition_39040.RemoveCondition(lTransition_39040.conditions[i]); }
            lTransition_39040.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3610f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39042 = MotionControllerMotion.EditorFindTransition(lState_33686, lState_33684, 0);
            if (lTransition_39042 == null) { lTransition_39042 = lState_33686.AddTransition(lState_33684); }
            lTransition_39042.isExit = false;
            lTransition_39042.hasExitTime = false;
            lTransition_39042.hasFixedDuration = true;
            lTransition_39042.exitTime = 0f;
            lTransition_39042.duration = 0.25f;
            lTransition_39042.offset = 0f;
            lTransition_39042.mute = false;
            lTransition_39042.solo = false;
            lTransition_39042.canTransitionToSelf = true;
            lTransition_39042.orderedInterruption = true;
            lTransition_39042.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39042.conditions.Length - 1; i >= 0; i--) { lTransition_39042.RemoveCondition(lTransition_39042.conditions[i]); }
            lTransition_39042.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");
            lTransition_39042.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39044 = MotionControllerMotion.EditorFindTransition(lState_33686, lState_33694, 0);
            if (lTransition_39044 == null) { lTransition_39044 = lState_33686.AddTransition(lState_33694); }
            lTransition_39044.isExit = false;
            lTransition_39044.hasExitTime = false;
            lTransition_39044.hasFixedDuration = true;
            lTransition_39044.exitTime = 0f;
            lTransition_39044.duration = 0.09999999f;
            lTransition_39044.offset = 0.8044095f;
            lTransition_39044.mute = false;
            lTransition_39044.solo = false;
            lTransition_39044.canTransitionToSelf = true;
            lTransition_39044.orderedInterruption = true;
            lTransition_39044.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39044.conditions.Length - 1; i >= 0; i--) { lTransition_39044.RemoveCondition(lTransition_39044.conditions[i]); }
            lTransition_39044.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39046 = MotionControllerMotion.EditorFindTransition(lState_33686, lState_33702, 0);
            if (lTransition_39046 == null) { lTransition_39046 = lState_33686.AddTransition(lState_33702); }
            lTransition_39046.isExit = false;
            lTransition_39046.hasExitTime = false;
            lTransition_39046.hasFixedDuration = true;
            lTransition_39046.exitTime = 0f;
            lTransition_39046.duration = 0.1f;
            lTransition_39046.offset = 0f;
            lTransition_39046.mute = false;
            lTransition_39046.solo = false;
            lTransition_39046.canTransitionToSelf = true;
            lTransition_39046.orderedInterruption = true;
            lTransition_39046.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39046.conditions.Length - 1; i >= 0; i--) { lTransition_39046.RemoveCondition(lTransition_39046.conditions[i]); }
            lTransition_39046.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3610f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39048 = MotionControllerMotion.EditorFindTransition(lState_33688, lState_33690, 0);
            if (lTransition_39048 == null) { lTransition_39048 = lState_33688.AddTransition(lState_33690); }
            lTransition_39048.isExit = false;
            lTransition_39048.hasExitTime = false;
            lTransition_39048.hasFixedDuration = true;
            lTransition_39048.exitTime = 0.7794118f;
            lTransition_39048.duration = 0.25f;
            lTransition_39048.offset = 0f;
            lTransition_39048.mute = false;
            lTransition_39048.solo = false;
            lTransition_39048.canTransitionToSelf = true;
            lTransition_39048.orderedInterruption = true;
            lTransition_39048.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39048.conditions.Length - 1; i >= 0; i--) { lTransition_39048.RemoveCondition(lTransition_39048.conditions[i]); }
            lTransition_39048.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.4f, "InputMagnitude");
            lTransition_39048.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39050 = MotionControllerMotion.EditorFindTransition(lState_33688, lState_33678, 0);
            if (lTransition_39050 == null) { lTransition_39050 = lState_33688.AddTransition(lState_33678); }
            lTransition_39050.isExit = false;
            lTransition_39050.hasExitTime = false;
            lTransition_39050.hasFixedDuration = true;
            lTransition_39050.exitTime = 0.7794118f;
            lTransition_39050.duration = 0.25f;
            lTransition_39050.offset = 0f;
            lTransition_39050.mute = false;
            lTransition_39050.solo = false;
            lTransition_39050.canTransitionToSelf = true;
            lTransition_39050.orderedInterruption = true;
            lTransition_39050.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39050.conditions.Length - 1; i >= 0; i--) { lTransition_39050.RemoveCondition(lTransition_39050.conditions[i]); }
            lTransition_39050.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.1f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39052 = MotionControllerMotion.EditorFindTransition(lState_33690, lState_33688, 0);
            if (lTransition_39052 == null) { lTransition_39052 = lState_33690.AddTransition(lState_33688); }
            lTransition_39052.isExit = false;
            lTransition_39052.hasExitTime = false;
            lTransition_39052.hasFixedDuration = true;
            lTransition_39052.exitTime = 0.7794118f;
            lTransition_39052.duration = 0.25f;
            lTransition_39052.offset = 0f;
            lTransition_39052.mute = false;
            lTransition_39052.solo = false;
            lTransition_39052.canTransitionToSelf = true;
            lTransition_39052.orderedInterruption = true;
            lTransition_39052.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39052.conditions.Length - 1; i >= 0; i--) { lTransition_39052.RemoveCondition(lTransition_39052.conditions[i]); }
            lTransition_39052.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.4f, "InputMagnitude");
            lTransition_39052.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39054 = MotionControllerMotion.EditorFindTransition(lState_33690, lState_33680, 0);
            if (lTransition_39054 == null) { lTransition_39054 = lState_33690.AddTransition(lState_33680); }
            lTransition_39054.isExit = false;
            lTransition_39054.hasExitTime = false;
            lTransition_39054.hasFixedDuration = true;
            lTransition_39054.exitTime = 0.7794118f;
            lTransition_39054.duration = 0.25f;
            lTransition_39054.offset = 0f;
            lTransition_39054.mute = false;
            lTransition_39054.solo = false;
            lTransition_39054.canTransitionToSelf = true;
            lTransition_39054.orderedInterruption = true;
            lTransition_39054.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39054.conditions.Length - 1; i >= 0; i--) { lTransition_39054.RemoveCondition(lTransition_39054.conditions[i]); }
            lTransition_39054.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.1f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39056 = MotionControllerMotion.EditorFindTransition(lState_33692, lState_33694, 0);
            if (lTransition_39056 == null) { lTransition_39056 = lState_33692.AddTransition(lState_33694); }
            lTransition_39056.isExit = false;
            lTransition_39056.hasExitTime = false;
            lTransition_39056.hasFixedDuration = true;
            lTransition_39056.exitTime = 0f;
            lTransition_39056.duration = 0.25f;
            lTransition_39056.offset = 0f;
            lTransition_39056.mute = false;
            lTransition_39056.solo = false;
            lTransition_39056.canTransitionToSelf = true;
            lTransition_39056.orderedInterruption = true;
            lTransition_39056.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39056.conditions.Length - 1; i >= 0; i--) { lTransition_39056.RemoveCondition(lTransition_39056.conditions[i]); }
            lTransition_39056.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.4f, "InputMagnitude");
            lTransition_39056.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39058 = MotionControllerMotion.EditorFindTransition(lState_33692, lState_33684, 0);
            if (lTransition_39058 == null) { lTransition_39058 = lState_33692.AddTransition(lState_33684); }
            lTransition_39058.isExit = false;
            lTransition_39058.hasExitTime = false;
            lTransition_39058.hasFixedDuration = true;
            lTransition_39058.exitTime = 0f;
            lTransition_39058.duration = 0.25f;
            lTransition_39058.offset = 0f;
            lTransition_39058.mute = false;
            lTransition_39058.solo = false;
            lTransition_39058.canTransitionToSelf = true;
            lTransition_39058.orderedInterruption = true;
            lTransition_39058.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39058.conditions.Length - 1; i >= 0; i--) { lTransition_39058.RemoveCondition(lTransition_39058.conditions[i]); }
            lTransition_39058.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.3f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39060 = MotionControllerMotion.EditorFindTransition(lState_33694, lState_33692, 0);
            if (lTransition_39060 == null) { lTransition_39060 = lState_33694.AddTransition(lState_33692); }
            lTransition_39060.isExit = false;
            lTransition_39060.hasExitTime = false;
            lTransition_39060.hasFixedDuration = true;
            lTransition_39060.exitTime = 0f;
            lTransition_39060.duration = 0.25f;
            lTransition_39060.offset = 0f;
            lTransition_39060.mute = false;
            lTransition_39060.solo = false;
            lTransition_39060.canTransitionToSelf = true;
            lTransition_39060.orderedInterruption = true;
            lTransition_39060.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39060.conditions.Length - 1; i >= 0; i--) { lTransition_39060.RemoveCondition(lTransition_39060.conditions[i]); }
            lTransition_39060.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.4f, "InputMagnitude");
            lTransition_39060.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39062 = MotionControllerMotion.EditorFindTransition(lState_33694, lState_33686, 0);
            if (lTransition_39062 == null) { lTransition_39062 = lState_33694.AddTransition(lState_33686); }
            lTransition_39062.isExit = false;
            lTransition_39062.hasExitTime = false;
            lTransition_39062.hasFixedDuration = true;
            lTransition_39062.exitTime = 0f;
            lTransition_39062.duration = 0.25f;
            lTransition_39062.offset = 0f;
            lTransition_39062.mute = false;
            lTransition_39062.solo = false;
            lTransition_39062.canTransitionToSelf = true;
            lTransition_39062.orderedInterruption = true;
            lTransition_39062.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39062.conditions.Length - 1; i >= 0; i--) { lTransition_39062.RemoveCondition(lTransition_39062.conditions[i]); }
            lTransition_39062.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.3f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lTransition_39068 = MotionControllerMotion.EditorFindTransition(lState_33696, lState_33704, 0);
            if (lTransition_39068 == null) { lTransition_39068 = lState_33696.AddTransition(lState_33704); }
            lTransition_39068.isExit = false;
            lTransition_39068.hasExitTime = true;
            lTransition_39068.hasFixedDuration = true;
            lTransition_39068.exitTime = 0.2675369f;
            lTransition_39068.duration = 0.15f;
            lTransition_39068.offset = 0f;
            lTransition_39068.mute = false;
            lTransition_39068.solo = false;
            lTransition_39068.canTransitionToSelf = true;
            lTransition_39068.orderedInterruption = true;
            lTransition_39068.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39068.conditions.Length - 1; i >= 0; i--) { lTransition_39068.RemoveCondition(lTransition_39068.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_39074 = MotionControllerMotion.EditorFindTransition(lState_33698, lState_33704, 0);
            if (lTransition_39074 == null) { lTransition_39074 = lState_33698.AddTransition(lState_33704); }
            lTransition_39074.isExit = false;
            lTransition_39074.hasExitTime = true;
            lTransition_39074.hasFixedDuration = true;
            lTransition_39074.exitTime = 0.2675369f;
            lTransition_39074.duration = 0.15f;
            lTransition_39074.offset = 0f;
            lTransition_39074.mute = false;
            lTransition_39074.solo = false;
            lTransition_39074.canTransitionToSelf = true;
            lTransition_39074.orderedInterruption = true;
            lTransition_39074.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39074.conditions.Length - 1; i >= 0; i--) { lTransition_39074.RemoveCondition(lTransition_39074.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_39076 = MotionControllerMotion.EditorFindTransition(lState_33700, lState_33704, 0);
            if (lTransition_39076 == null) { lTransition_39076 = lState_33700.AddTransition(lState_33704); }
            lTransition_39076.isExit = false;
            lTransition_39076.hasExitTime = true;
            lTransition_39076.hasFixedDuration = true;
            lTransition_39076.exitTime = 0.5f;
            lTransition_39076.duration = 0.15f;
            lTransition_39076.offset = 0f;
            lTransition_39076.mute = false;
            lTransition_39076.solo = false;
            lTransition_39076.canTransitionToSelf = true;
            lTransition_39076.orderedInterruption = true;
            lTransition_39076.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39076.conditions.Length - 1; i >= 0; i--) { lTransition_39076.RemoveCondition(lTransition_39076.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_39078 = MotionControllerMotion.EditorFindTransition(lState_33702, lState_33704, 0);
            if (lTransition_39078 == null) { lTransition_39078 = lState_33702.AddTransition(lState_33704); }
            lTransition_39078.isExit = false;
            lTransition_39078.hasExitTime = true;
            lTransition_39078.hasFixedDuration = true;
            lTransition_39078.exitTime = 0.5f;
            lTransition_39078.duration = 0.15f;
            lTransition_39078.offset = 0f;
            lTransition_39078.mute = false;
            lTransition_39078.solo = false;
            lTransition_39078.canTransitionToSelf = true;
            lTransition_39078.orderedInterruption = true;
            lTransition_39078.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_39078.conditions.Length - 1; i >= 0; i--) { lTransition_39078.RemoveCondition(lTransition_39078.conditions[i]); }


            // Run any post processing after creating the state machine
            OnStateMachineCreated();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}

