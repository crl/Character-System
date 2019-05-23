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
    [MotionName("Walk Run Pivot")]
    [MotionDescription("Standard movement (walk/run) for an adventure game.")]
    public class WalkRunPivot_v2 : MotionControllerMotion, IWalkRunMotion
    {
        /// <summary>
        /// Trigger values for th emotion
        /// </summary>
        public const int PHASE_UNKNOWN = 0;
        public const int PHASE_START = 27130;
        public const int PHASE_END_RUN = 27131;
        public const int PHASE_END_WALK = 27132;
        public const int PHASE_RESUME = 27133;

        public const int PHASE_START_IDLE_PIVOT = 27135;

        /// <summary>
        /// Optional "Form" or "Style" value to test to see if this motion should activate.
        /// </summary>
        public int _FormCondition = 0;
        public int FormCondition
        {
            get { return _FormCondition; }
            set { _FormCondition = value; }
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
        /// Determines if we rotate to match the camera
        /// </summary>
        public bool _RotateWithCamera = true;
        public bool RotateWithCamera
        {
            get { return _RotateWithCamera; }
            set { _RotateWithCamera = value; }
        }

        /// <summary>
        /// User layer id set for objects that are climbable.
        /// </summary>
        public string _RotateActionAlias = "ActivateRotation";
        public string RotateActionAlias
        {
            get { return _RotateActionAlias; }
            set { _RotateActionAlias = value; }
        }

        /// <summary>
        /// Degrees per second to rotate the actor in order to face the input direction
        /// </summary>
        public float _RotationSpeed = 180f;
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
        /// Determines if we'll use the start transitions when starting from idle
        /// </summary>
        public bool _UseStartTransitions = true;
        public bool UseStartTransitions
        {
            get { return _UseStartTransitions; }
            set { _UseStartTransitions = value; }
        }

        /// <summary>
        /// Determines if we'll use the start transitions when stopping movement
        /// </summary>
        public bool _UseStopTransitions = true;
        public bool UseStopTransitions
        {
            get { return _UseStopTransitions; }
            set { _UseStopTransitions = value; }
        }

        /// <summary>
        /// Determines if the character can pivot while idle
        /// </summary>
        public bool _UseTapToPivot = false;
        public bool UseTapToPivot
        {
            get { return _UseTapToPivot; }
            set { _UseTapToPivot = value; }
        }

        /// <summary>
        /// Determines how long we wait before testing for an idle pivot
        /// </summary>
        public float _TapToPivotDelay = 0.2f;
        public float TapToPivotDelay
        {
            get { return _TapToPivotDelay; }
            set { _TapToPivotDelay = value; }
        }

        /// <summary>
        /// Minimum angle before we use the pivot speed
        /// </summary>
        public float _MinPivotAngle = 20f;
        public float MinPivotAngle
        {
            get { return _MinPivotAngle; }
            set { _MinPivotAngle = value; }
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
        /// Determine if we're pivoting from an idle
        /// </summary>
        protected bool mStartInPivot = false;

        /// <summary>
        /// Angle of the input from when the motion was activated
        /// </summary>
        protected Vector3 mSavedInputForward = Vector3.zero;

        /// <summary>
        /// Time that has elapsed since there was no input
        /// </summary>
        protected float mNoInputElapsed = 0f;

        /// <summary>
        /// Phase ID we're using to transition out
        /// </summary>
        protected int mExitPhaseID = 0;

        /// <summary>
        /// Frame level rotation test
        /// </summary>
        protected bool mRotateWithCamera = false;

        /// <summary>
        /// Determines if the actor rotation should be linked to the camera
        /// </summary>
        protected bool mLinkRotation = false;

        /// <summary>
        /// We use these classes to help smooth the input values so that
        /// movement doesn't drop from 1 to 0 immediately.
        /// </summary>
        protected FloatValue mInputX = new FloatValue(0f, 10);
        protected FloatValue mInputY = new FloatValue(0f, 10);
        protected FloatValue mInputMagnitude = new FloatValue(0f, 15);

        /// <summary>
        /// Last time we had input activity
        /// </summary>
        protected float mLastTapStartTime = 0f;
        protected float mLastTapInputFromAvatarAngle = 0f;
        protected Vector3 mLastTapInputForward = Vector3.zero;

        /// <summary>
        /// Default constructor
        /// </summary>
        public WalkRunPivot_v2()
            : base()
        {
            _Category = EnumMotionCategories.WALK;

            _Priority = 5;
            _ActionAlias = "Run";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "WalkRunPivot v2-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public WalkRunPivot_v2(MotionController rController)
            : base(rController)
        {
            _Category = EnumMotionCategories.WALK;

            _Priority = 5;
            _ActionAlias = "Run";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "WalkRunPivot v2-SM"; }
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
            if (!mIsStartable ||
                !mMotionController.IsGrounded ||
                mMotionController.Stance != EnumControllerStance.TRAVERSAL)
            {
                mStartInPivot = false;
                mLastTapStartTime = 0f;
                return false;
            }

            // Test to see if the form condition matches our current default form
            if (_FormCondition >= 0 && mMotionController.CurrentForm != _FormCondition)
            {
                return false;
            }

            bool lIsPivotable = (_UseTapToPivot && (mLastTapStartTime > 0f || Mathf.Abs(mMotionController.State.InputFromAvatarAngle) > _MinPivotAngle));

            bool lIsIdling = (_UseTapToPivot && mMotionLayer.ActiveMotion != null && mMotionLayer.ActiveMotion.Category == EnumMotionCategories.IDLE);

            // Determine if tapping is enabled
            if (_UseTapToPivot && lIsPivotable && lIsIdling)
            {
                // If there's input, it could be the start of a tap or true movement
                if (mMotionController.State.InputMagnitudeTrend.Value > 0.1f)
                {
                    // Start the timer
                    if (mLastTapStartTime == 0f)
                    {
                        mLastTapStartTime = Time.time;
                        mLastTapInputForward = mMotionController.State.InputForward;
                        mLastTapInputFromAvatarAngle = mMotionController.State.InputFromAvatarAngle;
                        return true;
                    }
                    // Timer has expired. So, we must really be moving
                    else if (mLastTapStartTime + _TapToPivotDelay <= Time.time)
                    {
                        mStartInPivot = false;
                        mLastTapStartTime = 0f;
                        return true;
                    }

                    // Keep waiting
                    return false;
                }
                // No input. So, at the end of a tap or there really is nothing
                else
                {
                    if (mLastTapStartTime > 0f)
                    {
                        mStartInPivot = true;
                        mLastTapStartTime = 0f;
                        return true;
                    }
                }
            }
            // If not, we do normal processing
            else
            {
                mStartInPivot = false;
                mLastTapStartTime = 0f;

                // If there's enough movement, start the motion
                if (mMotionController.State.InputMagnitudeTrend.Value > 0.49f)
                {
                    return true;
                }
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
            if (mLastTapStartTime > 0f) { return true; }
            if (!mMotionController.IsGrounded) { return false; }

            // Test to see if the form condition matches our current default form
            if (_FormCondition >= 0 && mMotionController.CurrentForm != _FormCondition)
            {
                return false;
            }

            // Our idle pose is a good exit
            if (mMotionLayer._AnimatorStateID == STATE_IdlePose)
            {
                return false;
            }

            // Our exit pose for the idle pivots
            if (mMotionLayer._AnimatorStateID == STATE_IdleTurnEndPose)
            {
                if (mMotionController.State.InputMagnitudeTrend.Value < 0.1f)
                {
                    return false;
                }
            }

            // One last check to make sure we're in this state
            if (mIsAnimatorActive && !IsInMotionState)
            {
                return false;
            }

            // If we no longer have input and we're in normal movement, we can stop
            if (mMotionController.State.InputMagnitudeTrend.Average < 0.1f)
            {
                if (mMotionLayer._AnimatorStateID == STATE_MoveTree && mMotionLayer._AnimatorTransitionID == 0)
                {
                    return false;
                }
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
            if (mLastTapStartTime == 0f) { DelayedActivate(); }

            return base.Activate(rPrevMotion);
        }

        /// <summary>
        /// Called to start the specific motion. If the motion
        /// were something like 'jump', this would start the jumping process
        /// </summary>
        /// <param name="rPrevMotion">Motion that this motion is taking over from</param>
        public void DelayedActivate()
        {
            mExitPhaseID = 0;
            mSavedInputForward = mMotionController.State.InputForward;

            // Update the max speed based on our animation
            mMotionController.MaxSpeed = 5.668f;

            // Determine how we start
            if (mStartInPivot)
            {
                mMotionController.State.InputFromAvatarAngle = mLastTapInputFromAvatarAngle;
                mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START_IDLE_PIVOT, 0, true);
            }
            else if (mStartInMove)
            {
                mStartInMove = false;
                mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, 1, true);
            }
            else if (mMotionController._InputSource == null)
            {
                mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, (_UseStartTransitions ? 0 : 1), true);
            }
            else
            {
                // Grab the state info
                MotionState lState = mMotionController.State;

                // Convert the input to radial so we deal with keyboard and gamepad input the same.
                float lInputX = lState.InputX;
                float lInputY = lState.InputY;
                float lInputMagnitude = lState.InputMagnitudeTrend.Value;
                InputManagerHelper.ConvertToRadialInput(ref lInputX, ref lInputY, ref lInputMagnitude, (IsRunActive ? 1f : 0.5f));

                // Smooth the input
                if (lInputX != 0f || lInputY < 0f)
                {
                    mInputX.Clear(lInputX);
                    mInputY.Clear(lInputY);
                    mInputMagnitude.Clear(lInputMagnitude);
                }

                // Start the motion
                mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, (_UseStartTransitions ? 0 : 1), true);
            }

            // Register this motion with the camera
            if (_RotateWithCamera && mMotionController.CameraRig is BaseCameraRig)
            {
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate += OnCameraUpdated;
            }
        }

        /// <summary>
        /// Raised when we shut the motion down
        /// </summary>
        public override void Deactivate()
        {
            mLastTapStartTime = 0f;
            mLastTapInputFromAvatarAngle = 0f;

            // Clear out the start
            mStartInPivot = false;
            mStartInRun = false;
            mStartInWalk = false;

            // Register this motion with the camera
            if (mMotionController.CameraRig is BaseCameraRig)
            {
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
            }

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
            if (mLastTapStartTime > 0f) { return; }

            if (mMotionLayer._AnimatorTransitionID == TRANS_EntryState_MoveTree)
            {
                rRotation = Quaternion.identity;

                rMovement = rMovement.normalized * (mActorController.PrevState.Velocity.magnitude * Time.deltaTime); // (Time.smoothDeltaTime > 0f ? Time.smoothDeltaTime : Time.deltaTime));

                rMovement.x = 0f;
                rMovement.y = 0f;
                if (rMovement.z < 0f) { rMovement.z = 0f; }
            }
            else if (mMotionLayer._AnimatorStateID == STATE_MoveTree && mMotionLayer._AnimatorTransitionID == 0)
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
                }

                rMovement.x = 0f;
                rMovement.y = 0f;
                if (rMovement.z < 0f) { rMovement.z = 0f; }
            }
            else
            {
                if (_UseTapToPivot && IsIdlePivoting())
                {
                    rMovement = Vector3.zero;
                }
                // If we're stopping, add some lag
                else if (IsStopping())
                {
                    rMovement = rMovement * 0.5f;
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

            if (mLastTapStartTime > 0f)
            {
                UpdateDelayedActivation(rDeltaTime, rUpdateIndex);
            }
            else if (_UseTapToPivot && IsIdlePivoting())
            {
                UpdateIdlePivot(rDeltaTime, rUpdateIndex);
            }
            else
            {
                UpdateMovement(rDeltaTime, rUpdateIndex);
            }
        }

        /// <summary>
        /// Update used when we're delaying activation for possible pivot
        /// </summary>
        private void UpdateDelayedActivation(float rDeltaTime, int rUpdateIndex)
        {
            if (mMotionController.State.InputMagnitudeTrend.Value < 0.1f)
            {
                mStartInPivot = true;
                mLastTapStartTime = 0f;

                DelayedActivate();
            }
            else if (mLastTapStartTime + _TapToPivotDelay < Time.time)
            {
                mStartInPivot = false;
                mLastTapStartTime = 0f;

                DelayedActivate();
            }

            // Update smoothing
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
        /// Update processing for the idle pivot
        /// </summary>
        /// <param name="rDeltaTime"></param>
        /// <param name="rUpdateIndex"></param>
        private void UpdateIdlePivot(float rDeltaTime, int rUpdateIndex)
        {
            int lStateID = mMotionLayer._AnimatorStateID;
            if (lStateID == STATE_IdleTurn180L ||
                lStateID == STATE_IdleTurn90L ||
                lStateID == STATE_IdleTurn20L ||
                lStateID == STATE_IdleTurn20R ||
                lStateID == STATE_IdleTurn90R ||
                lStateID == STATE_IdleTurn180R)
            {
                if (mMotionLayer._AnimatorTransitionID != 0 && mLastTapInputForward.sqrMagnitude > 0f)
                {
                    if (mMotionController._CameraTransform != null)
                    {
                        Vector3 lInputForward = mMotionController._CameraTransform.rotation * mLastTapInputForward;

                        float lAngle = Vector3Ext.HorizontalAngleTo(mMotionController._Transform.forward, lInputForward, mMotionController._Transform.up);
                        mRotation = Quaternion.Euler(0f, lAngle * mMotionLayer._AnimatorTransitionNormalizedTime, 0f);
                    }
                }
            }
        }

        /// <summary>
        /// Update processing for moving
        /// </summary>
        /// <param name="rDeltaTime"></param>
        /// <param name="rUpdateIndex"></param>
        private void UpdateMovement(float rDeltaTime, int rUpdateIndex)
        {
            bool lUpdateSamples = true;

            // Store the last valid input we had
            if (mMotionController.State.InputMagnitudeTrend.Value > 0.4f)
            {
                mExitPhaseID = 0;
                mNoInputElapsed = 0f;
                mSavedInputForward = mMotionController.State.InputForward;

                // If we were stopping, allow us to resume without leaving the motion
                if (IsStopping())
                {
                    mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, PHASE_RESUME, 0, true);
                }
            }
            else
            {
                mNoInputElapsed = mNoInputElapsed + rDeltaTime;

                if (_UseStopTransitions)
                {
                    lUpdateSamples = false;

                    // If we've passed the delay, we really are stopping
                    if (mNoInputElapsed > 0.2f)
                    {
                        // Determine how we'll stop
                        if (mExitPhaseID == 0)
                        {
                            mExitPhaseID = (mInputMagnitude.Average < 0.6f ? PHASE_END_WALK : PHASE_END_RUN);
                        }

                        // Ensure we actually stop that way
                        if (mExitPhaseID != 0 && mMotionLayer._AnimatorStateID == STATE_MoveTree && mMotionLayer._AnimatorTransitionID == 0)
                        {
                            mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, mExitPhaseID, 0, true);
                        }
                    }
                }
            }

            // If we need to update the samples... 
            if (lUpdateSamples)
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
            }

            // Modify the input values to add some lag
            mMotionController.State.InputX = mInputX.Average;
            mMotionController.State.InputY = mInputY.Average;
            mMotionController.State.InputMagnitudeTrend.Replace(mInputMagnitude.Average);

            // We may want to rotate with the camera if we're facing forward
            mRotateWithCamera = false;
            if (_RotateWithCamera && mMotionController._CameraTransform != null)
            {
                float lToCameraAngle = Vector3Ext.HorizontalAngleTo(mMotionController._Transform.forward, mMotionController._CameraTransform.forward, mMotionController._Transform.up);
                mRotateWithCamera = (Mathf.Abs(lToCameraAngle) < _RotationSpeed * rDeltaTime);

                if (mRotateWithCamera && mMotionLayer._AnimatorStateID != STATE_MoveTree) { mRotateWithCamera = false; }
                if (mRotateWithCamera && mMotionLayer._AnimatorTransitionID != 0) { mRotateWithCamera = false; }
                if (mRotateWithCamera && (Mathf.Abs(mMotionController.State.InputX) > 0.05f || mMotionController.State.InputY <= 0f)) { mRotateWithCamera = false; }
                if (mRotateWithCamera && (_RotateActionAlias.Length > 0 && !mMotionController._InputSource.IsPressed(_RotateActionAlias))) { mRotateWithCamera = false; }
            }

            // If we're meant to rotate with the camera (and OnCameraUpdate isn't already attached), do it here
            if (_RotateWithCamera && !(mMotionController.CameraRig is BaseCameraRig))
            {
                OnCameraUpdated(rDeltaTime, rUpdateIndex, null);
            }

            // We only allow input rotation under certain circumstances
            if (mMotionLayer._AnimatorTransitionID == TRANS_EntryState_MoveTree ||
                (mMotionLayer._AnimatorStateID == STATE_MoveTree && mMotionLayer._AnimatorTransitionID == 0) ||

                (mMotionLayer._AnimatorStateID == STATE_IdleToWalk180L && mMotionLayer._AnimatorStateNormalizedTime > 0.7f) ||
                (mMotionLayer._AnimatorStateID == STATE_IdleToWalk90L && mMotionLayer._AnimatorStateNormalizedTime > 0.6f) ||
                (mMotionLayer._AnimatorStateID == STATE_IdleToWalk90R && mMotionLayer._AnimatorStateNormalizedTime > 0.6f) ||
                (mMotionLayer._AnimatorStateID == STATE_IdleToWalk180R && mMotionLayer._AnimatorStateNormalizedTime > 0.7f) ||

                (mMotionLayer._AnimatorStateID == STATE_IdleToRun180L && mMotionLayer._AnimatorStateNormalizedTime > 0.6f) ||
                (mMotionLayer._AnimatorStateID == STATE_IdleToRun90L && mMotionLayer._AnimatorStateNormalizedTime > 0.6f) ||
                (mMotionLayer._AnimatorStateID == STATE_IdleToRun) ||
                (mMotionLayer._AnimatorStateID == STATE_IdleToRun90R && mMotionLayer._AnimatorStateNormalizedTime > 0.6f) ||
                (mMotionLayer._AnimatorStateID == STATE_IdleToRun180R && mMotionLayer._AnimatorStateNormalizedTime > 0.6f)
                )
            {
                // Since we're not rotating with the camera, rotate with input
                if (!mRotateWithCamera)
                {
                    if (mMotionController._CameraTransform != null && mMotionController.State.InputForward.sqrMagnitude == 0f)
                    {
                        RotateToInput(mMotionController._CameraTransform.rotation * mSavedInputForward, rDeltaTime, ref mRotation);
                    }
                    else
                    {
                        RotateToInput(mMotionController.State.InputFromAvatarAngle, rDeltaTime, ref mRotation);
                    }
                }
            }
        }

        /// <summary>
        /// Create a rotation velocity that rotates the character based on input
        /// </summary>
        /// <param name="rInputForward"></param>
        /// <param name="rDeltaTime"></param>
        private void RotateToInput(Vector3 rInputForward, float rDeltaTime, ref Quaternion rRotation)
        {
            float lAngle = Vector3Ext.HorizontalAngleTo(mMotionController._Transform.forward, rInputForward, mMotionController._Transform.up);
            if (lAngle != 0f)
            {
                if (_RotationSpeed > 0f && Mathf.Abs(lAngle) > _RotationSpeed * rDeltaTime)
                {
                    lAngle = Mathf.Sign(lAngle) * _RotationSpeed * rDeltaTime;
                }

                rRotation = Quaternion.Euler(0f, lAngle, 0f);
            }
        }

        /// <summary>
        /// Create a rotation velocity that rotates the character based on input
        /// </summary>
        /// <param name="rInputFromAvatarAngle"></param>
        /// <param name="rDeltaTime"></param>
        private void RotateToInput(float rInputFromAvatarAngle, float rDeltaTime, ref Quaternion rRotation)
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

            if (!mRotateWithCamera)
            {
                mLinkRotation = false;
                return;
            }

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

        /// <summary>
        /// Tests if we're in one of the stopping states
        /// </summary>
        /// <returns></returns>
        private bool IsStopping()
        {
            if (!_UseStopTransitions) { return false; }

            int lStateID = mMotionLayer._AnimatorStateID;
            if (lStateID == STATE_RunToIdle_LDown) { return true; }
            if (lStateID == STATE_RunToIdle_RDown) { return true; }
            if (lStateID == STATE_WalkToIdle_LDown) { return true; }
            if (lStateID == STATE_WalkToIdle_RDown) { return true; }

            int lTransitionID = mMotionLayer._AnimatorTransitionID;
            if (lTransitionID == TRANS_MoveTree_RunToIdle_LDown) { return true; }
            if (lTransitionID == TRANS_MoveTree_RunToIdle_RDown) { return true; }
            if (lTransitionID == TRANS_MoveTree_WalkToIdle_LDown) { return true; }
            if (lTransitionID == TRANS_MoveTree_WalkToIdle_RDown) { return true; }

            return false;
        }

        /// <summary>
        /// Tests if we're in one of the pivoting states
        /// </summary>
        /// <returns></returns>
        private bool IsIdlePivoting()
        {
            if (!_UseTapToPivot) { return false; }

            int lStateID = mMotionLayer._AnimatorStateID;
            if (lStateID == STATE_IdleTurn180L) { return true; }
            if (lStateID == STATE_IdleTurn90L) { return true; }
            if (lStateID == STATE_IdleTurn20L) { return true; }
            if (lStateID == STATE_IdleTurn20R) { return true; }
            if (lStateID == STATE_IdleTurn90R) { return true; }
            if (lStateID == STATE_IdleTurn180R) { return true; }

            int lTransitionID = mMotionLayer._AnimatorTransitionID;
            if (lTransitionID == TRANS_EntryState_IdleTurn180L) { return true; }
            if (lTransitionID == TRANS_EntryState_IdleTurn90L) { return true; }
            if (lTransitionID == TRANS_EntryState_IdleTurn20L) { return true; }
            if (lTransitionID == TRANS_EntryState_IdleTurn20R) { return true; }
            if (lTransitionID == TRANS_EntryState_IdleTurn90R) { return true; }
            if (lTransitionID == TRANS_EntryState_IdleTurn180R) { return true; }

            return false;
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
                lEntry.PositiveButton = "left shift";
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
                lEntry.PositiveButton = "";
                lEntry.Gravity = 1;
                lEntry.Dead = 0.3f;
                lEntry.Sensitivity = 1;
                lEntry.Type = InputManagerEntryType.JOYSTICK_AXIS;
                lEntry.Axis = 5;
                lEntry.JoyNum = 0;

                InputManagerHelper.AddEntry(lEntry, true);

#else

                lEntry = new InputManagerEntry();
                lEntry.Name = _ActionAlias;
                lEntry.PositiveButton = "";
                lEntry.Gravity = 1;
                lEntry.Dead = 0.3f;
                lEntry.Sensitivity = 1;
                lEntry.Type = InputManagerEntryType.JOYSTICK_AXIS;
                lEntry.Axis = 9;
                lEntry.JoyNum = 0;

                InputManagerHelper.AddEntry(lEntry, true);

#endif
            }
        }
        
        /// <summary>
        /// Allow the motion to render it's own GUI
        /// </summary>
        public override bool OnInspectorGUI()
        {
            bool lIsDirty = false;

            if (EditorHelper.IntField("Form Condition", "Optional condition used to only activate this motion if the value matches the current Default Form of the MC. Set to -1 to disable.", FormCondition, mMotionController))
            {
                lIsDirty = true;
                FormCondition = EditorHelper.FieldIntValue;
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

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Rotate With Camera", "Determines if we rotate to match the camera.", RotateWithCamera, mMotionController))
            {
                lIsDirty = true;
                RotateWithCamera = EditorHelper.FieldBoolValue;
            }

            if (RotateWithCamera)
            {
                if (EditorHelper.TextField("Rotate Action Alias", "Action alias determines if rotation is activated. This typically matches the input source's View Activator.", RotateActionAlias, mMotionController))
                {
                    lIsDirty = true;
                    RotateActionAlias = EditorHelper.FieldStringValue;
                }
            }

            if (EditorHelper.FloatField("Rotation Speed", "Degrees per second to rotate the actor ('0' means instant rotation).", RotationSpeed, mMotionController))
            {
                lIsDirty = true;
                RotationSpeed = EditorHelper.FieldFloatValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Use Start Transitions", "Determines if we'll use the start transitions when coming from idle", UseStartTransitions, mMotionController))
            {
                lIsDirty = true;
                UseStartTransitions = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("Use Stop Transitions", "Determines if we'll use the stop transitions when stopping movement", UseStopTransitions, mMotionController))
            {
                lIsDirty = true;
                UseStopTransitions = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("Use Tap to Pivot", "Determines if taping a direction while idle will pivot the character without moving them.", UseTapToPivot, mMotionController))
            {
                lIsDirty = true;
                UseTapToPivot = EditorHelper.FieldBoolValue;
            }

            if (UseTapToPivot)
            {
                EditorGUILayout.BeginHorizontal();

                if (EditorHelper.FloatField("Min Angle", "Sets the minimum angle between the input direction and character direction where we'll do a pivot.", MinPivotAngle, mMotionController))
                {
                    lIsDirty = true;
                    MinPivotAngle = EditorHelper.FieldFloatValue;
                }

                GUILayout.Space(10f);

                EditorGUILayout.LabelField(new GUIContent("Delay", "Delay in seconds before we test if we're NOT pivoting, but moving. In my tests, the average tap took 0.12 to 0.15 seconds."), GUILayout.Width(40f));
                if (EditorHelper.FloatField(TapToPivotDelay, "Delay", mMotionController, 40f))
                {
                    lIsDirty = true;
                    TapToPivotDelay = EditorHelper.FieldFloatValue;
                }

                GUILayout.FlexibleSpace();

                EditorGUILayout.EndHorizontal();
            }

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
        public int STATE_Start = -1;
        public int STATE_MoveTree = -1;
        public int STATE_IdleToWalk90L = -1;
        public int STATE_IdleToWalk90R = -1;
        public int STATE_IdleToWalk180R = -1;
        public int STATE_IdleToWalk180L = -1;
        public int STATE_IdlePose = -1;
        public int STATE_IdleToRun90L = -1;
        public int STATE_IdleToRun180L = -1;
        public int STATE_IdleToRun90R = -1;
        public int STATE_IdleToRun180R = -1;
        public int STATE_IdleToRun = -1;
        public int STATE_RunPivot180R_LDown = -1;
        public int STATE_WalkPivot180L = -1;
        public int STATE_RunToIdle_LDown = -1;
        public int STATE_WalkToIdle_LDown = -1;
        public int STATE_WalkToIdle_RDown = -1;
        public int STATE_RunToIdle_RDown = -1;
        public int STATE_IdleTurn20R = -1;
        public int STATE_IdleTurn90R = -1;
        public int STATE_IdleTurn180R = -1;
        public int STATE_IdleTurn20L = -1;
        public int STATE_IdleTurn90L = -1;
        public int STATE_IdleTurn180L = -1;
        public int STATE_IdleTurnEndPose = -1;
        public int TRANS_AnyState_IdleToWalk90L = -1;
        public int TRANS_EntryState_IdleToWalk90L = -1;
        public int TRANS_AnyState_IdleToWalk90R = -1;
        public int TRANS_EntryState_IdleToWalk90R = -1;
        public int TRANS_AnyState_IdleToWalk180R = -1;
        public int TRANS_EntryState_IdleToWalk180R = -1;
        public int TRANS_AnyState_MoveTree = -1;
        public int TRANS_EntryState_MoveTree = -1;
        public int TRANS_AnyState_IdleToWalk180L = -1;
        public int TRANS_EntryState_IdleToWalk180L = -1;
        public int TRANS_AnyState_IdleToRun180L = -1;
        public int TRANS_EntryState_IdleToRun180L = -1;
        public int TRANS_AnyState_IdleToRun90L = -1;
        public int TRANS_EntryState_IdleToRun90L = -1;
        public int TRANS_AnyState_IdleToRun90R = -1;
        public int TRANS_EntryState_IdleToRun90R = -1;
        public int TRANS_AnyState_IdleToRun180R = -1;
        public int TRANS_EntryState_IdleToRun180R = -1;
        public int TRANS_AnyState_IdleToRun = -1;
        public int TRANS_EntryState_IdleToRun = -1;
        public int TRANS_AnyState_IdleTurn180L = -1;
        public int TRANS_EntryState_IdleTurn180L = -1;
        public int TRANS_AnyState_IdleTurn90L = -1;
        public int TRANS_EntryState_IdleTurn90L = -1;
        public int TRANS_AnyState_IdleTurn20L = -1;
        public int TRANS_EntryState_IdleTurn20L = -1;
        public int TRANS_AnyState_IdleTurn20R = -1;
        public int TRANS_EntryState_IdleTurn20R = -1;
        public int TRANS_AnyState_IdleTurn90R = -1;
        public int TRANS_EntryState_IdleTurn90R = -1;
        public int TRANS_AnyState_IdleTurn180R = -1;
        public int TRANS_EntryState_IdleTurn180R = -1;
        public int TRANS_MoveTree_RunPivot180R_LDown = -1;
        public int TRANS_MoveTree_WalkPivot180L = -1;
        public int TRANS_MoveTree_RunToIdle_LDown = -1;
        public int TRANS_MoveTree_WalkToIdle_LDown = -1;
        public int TRANS_MoveTree_RunToIdle_RDown = -1;
        public int TRANS_MoveTree_WalkToIdle_RDown = -1;
        public int TRANS_IdleToWalk90L_MoveTree = -1;
        public int TRANS_IdleToWalk90L_IdlePose = -1;
        public int TRANS_IdleToWalk90R_MoveTree = -1;
        public int TRANS_IdleToWalk90R_IdlePose = -1;
        public int TRANS_IdleToWalk180R_MoveTree = -1;
        public int TRANS_IdleToWalk180R_IdlePose = -1;
        public int TRANS_IdleToWalk180L_MoveTree = -1;
        public int TRANS_IdleToWalk180L_IdlePose = -1;
        public int TRANS_IdleToRun90L_MoveTree = -1;
        public int TRANS_IdleToRun90L_IdlePose = -1;
        public int TRANS_IdleToRun180L_MoveTree = -1;
        public int TRANS_IdleToRun180L_IdlePose = -1;
        public int TRANS_IdleToRun90R_MoveTree = -1;
        public int TRANS_IdleToRun90R_IdlePose = -1;
        public int TRANS_IdleToRun180R_MoveTree = -1;
        public int TRANS_IdleToRun180R_IdlePose = -1;
        public int TRANS_IdleToRun_MoveTree = -1;
        public int TRANS_IdleToRun_IdlePose = -1;
        public int TRANS_RunPivot180R_LDown_MoveTree = -1;
        public int TRANS_WalkPivot180L_MoveTree = -1;
        public int TRANS_RunToIdle_LDown_IdlePose = -1;
        public int TRANS_RunToIdle_LDown_MoveTree = -1;
        public int TRANS_RunToIdle_LDown_RunPivot180R_LDown = -1;
        public int TRANS_WalkToIdle_LDown_MoveTree = -1;
        public int TRANS_WalkToIdle_LDown_IdlePose = -1;
        public int TRANS_WalkToIdle_RDown_MoveTree = -1;
        public int TRANS_WalkToIdle_RDown_IdlePose = -1;
        public int TRANS_RunToIdle_RDown_MoveTree = -1;
        public int TRANS_RunToIdle_RDown_IdlePose = -1;
        public int TRANS_RunToIdle_RDown_RunPivot180R_LDown = -1;
        public int TRANS_IdleTurn20R_IdleTurnEndPose = -1;
        public int TRANS_IdleTurn90R_IdleTurnEndPose = -1;
        public int TRANS_IdleTurn180R_IdleTurnEndPose = -1;
        public int TRANS_IdleTurn20L_IdleTurnEndPose = -1;
        public int TRANS_IdleTurn90L_IdleTurnEndPose = -1;
        public int TRANS_IdleTurn180L_IdleTurnEndPose = -1;
        public int TRANS_IdleTurnEndPose_MoveTree = -1;

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
                    if (lStateID == STATE_MoveTree) { return true; }
                    if (lStateID == STATE_IdleToWalk90L) { return true; }
                    if (lStateID == STATE_IdleToWalk90R) { return true; }
                    if (lStateID == STATE_IdleToWalk180R) { return true; }
                    if (lStateID == STATE_IdleToWalk180L) { return true; }
                    if (lStateID == STATE_IdlePose) { return true; }
                    if (lStateID == STATE_IdleToRun90L) { return true; }
                    if (lStateID == STATE_IdleToRun180L) { return true; }
                    if (lStateID == STATE_IdleToRun90R) { return true; }
                    if (lStateID == STATE_IdleToRun180R) { return true; }
                    if (lStateID == STATE_IdleToRun) { return true; }
                    if (lStateID == STATE_RunPivot180R_LDown) { return true; }
                    if (lStateID == STATE_WalkPivot180L) { return true; }
                    if (lStateID == STATE_RunToIdle_LDown) { return true; }
                    if (lStateID == STATE_WalkToIdle_LDown) { return true; }
                    if (lStateID == STATE_WalkToIdle_RDown) { return true; }
                    if (lStateID == STATE_RunToIdle_RDown) { return true; }
                    if (lStateID == STATE_IdleTurn20R) { return true; }
                    if (lStateID == STATE_IdleTurn90R) { return true; }
                    if (lStateID == STATE_IdleTurn180R) { return true; }
                    if (lStateID == STATE_IdleTurn20L) { return true; }
                    if (lStateID == STATE_IdleTurn90L) { return true; }
                    if (lStateID == STATE_IdleTurn180L) { return true; }
                    if (lStateID == STATE_IdleTurnEndPose) { return true; }
                }

                if (lTransitionID == TRANS_AnyState_IdleToWalk90L) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleToWalk90L) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleToWalk90R) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleToWalk90R) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleToWalk180R) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleToWalk180R) { return true; }
                if (lTransitionID == TRANS_AnyState_MoveTree) { return true; }
                if (lTransitionID == TRANS_EntryState_MoveTree) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleToWalk180L) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleToWalk180L) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleToRun180L) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleToRun180L) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleToRun90L) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleToRun90L) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleToRun90R) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleToRun90R) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleToRun180R) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleToRun180R) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleToRun) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleToRun) { return true; }
                if (lTransitionID == TRANS_AnyState_MoveTree) { return true; }
                if (lTransitionID == TRANS_EntryState_MoveTree) { return true; }
                if (lTransitionID == TRANS_AnyState_MoveTree) { return true; }
                if (lTransitionID == TRANS_EntryState_MoveTree) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleTurn180L) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleTurn180L) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleTurn90L) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleTurn90L) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleTurn20L) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleTurn20L) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleTurn20R) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleTurn20R) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleTurn90R) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleTurn90R) { return true; }
                if (lTransitionID == TRANS_AnyState_IdleTurn180R) { return true; }
                if (lTransitionID == TRANS_EntryState_IdleTurn180R) { return true; }
                if (lTransitionID == TRANS_MoveTree_RunPivot180R_LDown) { return true; }
                if (lTransitionID == TRANS_MoveTree_RunPivot180R_LDown) { return true; }
                if (lTransitionID == TRANS_MoveTree_WalkPivot180L) { return true; }
                if (lTransitionID == TRANS_MoveTree_WalkPivot180L) { return true; }
                if (lTransitionID == TRANS_MoveTree_RunToIdle_LDown) { return true; }
                if (lTransitionID == TRANS_MoveTree_WalkToIdle_LDown) { return true; }
                if (lTransitionID == TRANS_MoveTree_RunToIdle_RDown) { return true; }
                if (lTransitionID == TRANS_MoveTree_WalkToIdle_RDown) { return true; }
                if (lTransitionID == TRANS_MoveTree_RunToIdle_RDown) { return true; }
                if (lTransitionID == TRANS_MoveTree_RunToIdle_LDown) { return true; }
                if (lTransitionID == TRANS_MoveTree_WalkToIdle_RDown) { return true; }
                if (lTransitionID == TRANS_MoveTree_WalkToIdle_LDown) { return true; }
                if (lTransitionID == TRANS_IdleToWalk90L_MoveTree) { return true; }
                if (lTransitionID == TRANS_IdleToWalk90L_IdlePose) { return true; }
                if (lTransitionID == TRANS_IdleToWalk90R_MoveTree) { return true; }
                if (lTransitionID == TRANS_IdleToWalk90R_IdlePose) { return true; }
                if (lTransitionID == TRANS_IdleToWalk180R_MoveTree) { return true; }
                if (lTransitionID == TRANS_IdleToWalk180R_IdlePose) { return true; }
                if (lTransitionID == TRANS_IdleToWalk180L_MoveTree) { return true; }
                if (lTransitionID == TRANS_IdleToWalk180L_IdlePose) { return true; }
                if (lTransitionID == TRANS_IdleToRun90L_MoveTree) { return true; }
                if (lTransitionID == TRANS_IdleToRun90L_IdlePose) { return true; }
                if (lTransitionID == TRANS_IdleToRun180L_MoveTree) { return true; }
                if (lTransitionID == TRANS_IdleToRun180L_IdlePose) { return true; }
                if (lTransitionID == TRANS_IdleToRun90R_MoveTree) { return true; }
                if (lTransitionID == TRANS_IdleToRun90R_IdlePose) { return true; }
                if (lTransitionID == TRANS_IdleToRun180R_MoveTree) { return true; }
                if (lTransitionID == TRANS_IdleToRun180R_IdlePose) { return true; }
                if (lTransitionID == TRANS_IdleToRun_MoveTree) { return true; }
                if (lTransitionID == TRANS_IdleToRun_IdlePose) { return true; }
                if (lTransitionID == TRANS_RunPivot180R_LDown_MoveTree) { return true; }
                if (lTransitionID == TRANS_WalkPivot180L_MoveTree) { return true; }
                if (lTransitionID == TRANS_RunToIdle_LDown_IdlePose) { return true; }
                if (lTransitionID == TRANS_RunToIdle_LDown_MoveTree) { return true; }
                if (lTransitionID == TRANS_RunToIdle_LDown_RunPivot180R_LDown) { return true; }
                if (lTransitionID == TRANS_RunToIdle_LDown_RunPivot180R_LDown) { return true; }
                if (lTransitionID == TRANS_WalkToIdle_LDown_MoveTree) { return true; }
                if (lTransitionID == TRANS_WalkToIdle_LDown_IdlePose) { return true; }
                if (lTransitionID == TRANS_WalkToIdle_RDown_MoveTree) { return true; }
                if (lTransitionID == TRANS_WalkToIdle_RDown_IdlePose) { return true; }
                if (lTransitionID == TRANS_RunToIdle_RDown_MoveTree) { return true; }
                if (lTransitionID == TRANS_RunToIdle_RDown_IdlePose) { return true; }
                if (lTransitionID == TRANS_RunToIdle_RDown_RunPivot180R_LDown) { return true; }
                if (lTransitionID == TRANS_RunToIdle_RDown_RunPivot180R_LDown) { return true; }
                if (lTransitionID == TRANS_IdleTurn20R_IdleTurnEndPose) { return true; }
                if (lTransitionID == TRANS_IdleTurn90R_IdleTurnEndPose) { return true; }
                if (lTransitionID == TRANS_IdleTurn180R_IdleTurnEndPose) { return true; }
                if (lTransitionID == TRANS_IdleTurn20L_IdleTurnEndPose) { return true; }
                if (lTransitionID == TRANS_IdleTurn90L_IdleTurnEndPose) { return true; }
                if (lTransitionID == TRANS_IdleTurn180L_IdleTurnEndPose) { return true; }
                if (lTransitionID == TRANS_IdleTurnEndPose_MoveTree) { return true; }
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
            if (rStateID == STATE_MoveTree) { return true; }
            if (rStateID == STATE_IdleToWalk90L) { return true; }
            if (rStateID == STATE_IdleToWalk90R) { return true; }
            if (rStateID == STATE_IdleToWalk180R) { return true; }
            if (rStateID == STATE_IdleToWalk180L) { return true; }
            if (rStateID == STATE_IdlePose) { return true; }
            if (rStateID == STATE_IdleToRun90L) { return true; }
            if (rStateID == STATE_IdleToRun180L) { return true; }
            if (rStateID == STATE_IdleToRun90R) { return true; }
            if (rStateID == STATE_IdleToRun180R) { return true; }
            if (rStateID == STATE_IdleToRun) { return true; }
            if (rStateID == STATE_RunPivot180R_LDown) { return true; }
            if (rStateID == STATE_WalkPivot180L) { return true; }
            if (rStateID == STATE_RunToIdle_LDown) { return true; }
            if (rStateID == STATE_WalkToIdle_LDown) { return true; }
            if (rStateID == STATE_WalkToIdle_RDown) { return true; }
            if (rStateID == STATE_RunToIdle_RDown) { return true; }
            if (rStateID == STATE_IdleTurn20R) { return true; }
            if (rStateID == STATE_IdleTurn90R) { return true; }
            if (rStateID == STATE_IdleTurn180R) { return true; }
            if (rStateID == STATE_IdleTurn20L) { return true; }
            if (rStateID == STATE_IdleTurn90L) { return true; }
            if (rStateID == STATE_IdleTurn180L) { return true; }
            if (rStateID == STATE_IdleTurnEndPose) { return true; }
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
                if (rStateID == STATE_MoveTree) { return true; }
                if (rStateID == STATE_IdleToWalk90L) { return true; }
                if (rStateID == STATE_IdleToWalk90R) { return true; }
                if (rStateID == STATE_IdleToWalk180R) { return true; }
                if (rStateID == STATE_IdleToWalk180L) { return true; }
                if (rStateID == STATE_IdlePose) { return true; }
                if (rStateID == STATE_IdleToRun90L) { return true; }
                if (rStateID == STATE_IdleToRun180L) { return true; }
                if (rStateID == STATE_IdleToRun90R) { return true; }
                if (rStateID == STATE_IdleToRun180R) { return true; }
                if (rStateID == STATE_IdleToRun) { return true; }
                if (rStateID == STATE_RunPivot180R_LDown) { return true; }
                if (rStateID == STATE_WalkPivot180L) { return true; }
                if (rStateID == STATE_RunToIdle_LDown) { return true; }
                if (rStateID == STATE_WalkToIdle_LDown) { return true; }
                if (rStateID == STATE_WalkToIdle_RDown) { return true; }
                if (rStateID == STATE_RunToIdle_RDown) { return true; }
                if (rStateID == STATE_IdleTurn20R) { return true; }
                if (rStateID == STATE_IdleTurn90R) { return true; }
                if (rStateID == STATE_IdleTurn180R) { return true; }
                if (rStateID == STATE_IdleTurn20L) { return true; }
                if (rStateID == STATE_IdleTurn90L) { return true; }
                if (rStateID == STATE_IdleTurn180L) { return true; }
                if (rStateID == STATE_IdleTurnEndPose) { return true; }
            }

            if (rTransitionID == TRANS_AnyState_IdleToWalk90L) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleToWalk90L) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleToWalk90R) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleToWalk90R) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleToWalk180R) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleToWalk180R) { return true; }
            if (rTransitionID == TRANS_AnyState_MoveTree) { return true; }
            if (rTransitionID == TRANS_EntryState_MoveTree) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleToWalk180L) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleToWalk180L) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleToRun180L) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleToRun180L) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleToRun90L) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleToRun90L) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleToRun90R) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleToRun90R) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleToRun180R) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleToRun180R) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleToRun) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleToRun) { return true; }
            if (rTransitionID == TRANS_AnyState_MoveTree) { return true; }
            if (rTransitionID == TRANS_EntryState_MoveTree) { return true; }
            if (rTransitionID == TRANS_AnyState_MoveTree) { return true; }
            if (rTransitionID == TRANS_EntryState_MoveTree) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleTurn180L) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleTurn180L) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleTurn90L) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleTurn90L) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleTurn20L) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleTurn20L) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleTurn20R) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleTurn20R) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleTurn90R) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleTurn90R) { return true; }
            if (rTransitionID == TRANS_AnyState_IdleTurn180R) { return true; }
            if (rTransitionID == TRANS_EntryState_IdleTurn180R) { return true; }
            if (rTransitionID == TRANS_MoveTree_RunPivot180R_LDown) { return true; }
            if (rTransitionID == TRANS_MoveTree_RunPivot180R_LDown) { return true; }
            if (rTransitionID == TRANS_MoveTree_WalkPivot180L) { return true; }
            if (rTransitionID == TRANS_MoveTree_WalkPivot180L) { return true; }
            if (rTransitionID == TRANS_MoveTree_RunToIdle_LDown) { return true; }
            if (rTransitionID == TRANS_MoveTree_WalkToIdle_LDown) { return true; }
            if (rTransitionID == TRANS_MoveTree_RunToIdle_RDown) { return true; }
            if (rTransitionID == TRANS_MoveTree_WalkToIdle_RDown) { return true; }
            if (rTransitionID == TRANS_MoveTree_RunToIdle_RDown) { return true; }
            if (rTransitionID == TRANS_MoveTree_RunToIdle_LDown) { return true; }
            if (rTransitionID == TRANS_MoveTree_WalkToIdle_RDown) { return true; }
            if (rTransitionID == TRANS_MoveTree_WalkToIdle_LDown) { return true; }
            if (rTransitionID == TRANS_IdleToWalk90L_MoveTree) { return true; }
            if (rTransitionID == TRANS_IdleToWalk90L_IdlePose) { return true; }
            if (rTransitionID == TRANS_IdleToWalk90R_MoveTree) { return true; }
            if (rTransitionID == TRANS_IdleToWalk90R_IdlePose) { return true; }
            if (rTransitionID == TRANS_IdleToWalk180R_MoveTree) { return true; }
            if (rTransitionID == TRANS_IdleToWalk180R_IdlePose) { return true; }
            if (rTransitionID == TRANS_IdleToWalk180L_MoveTree) { return true; }
            if (rTransitionID == TRANS_IdleToWalk180L_IdlePose) { return true; }
            if (rTransitionID == TRANS_IdleToRun90L_MoveTree) { return true; }
            if (rTransitionID == TRANS_IdleToRun90L_IdlePose) { return true; }
            if (rTransitionID == TRANS_IdleToRun180L_MoveTree) { return true; }
            if (rTransitionID == TRANS_IdleToRun180L_IdlePose) { return true; }
            if (rTransitionID == TRANS_IdleToRun90R_MoveTree) { return true; }
            if (rTransitionID == TRANS_IdleToRun90R_IdlePose) { return true; }
            if (rTransitionID == TRANS_IdleToRun180R_MoveTree) { return true; }
            if (rTransitionID == TRANS_IdleToRun180R_IdlePose) { return true; }
            if (rTransitionID == TRANS_IdleToRun_MoveTree) { return true; }
            if (rTransitionID == TRANS_IdleToRun_IdlePose) { return true; }
            if (rTransitionID == TRANS_RunPivot180R_LDown_MoveTree) { return true; }
            if (rTransitionID == TRANS_WalkPivot180L_MoveTree) { return true; }
            if (rTransitionID == TRANS_RunToIdle_LDown_IdlePose) { return true; }
            if (rTransitionID == TRANS_RunToIdle_LDown_MoveTree) { return true; }
            if (rTransitionID == TRANS_RunToIdle_LDown_RunPivot180R_LDown) { return true; }
            if (rTransitionID == TRANS_RunToIdle_LDown_RunPivot180R_LDown) { return true; }
            if (rTransitionID == TRANS_WalkToIdle_LDown_MoveTree) { return true; }
            if (rTransitionID == TRANS_WalkToIdle_LDown_IdlePose) { return true; }
            if (rTransitionID == TRANS_WalkToIdle_RDown_MoveTree) { return true; }
            if (rTransitionID == TRANS_WalkToIdle_RDown_IdlePose) { return true; }
            if (rTransitionID == TRANS_RunToIdle_RDown_MoveTree) { return true; }
            if (rTransitionID == TRANS_RunToIdle_RDown_IdlePose) { return true; }
            if (rTransitionID == TRANS_RunToIdle_RDown_RunPivot180R_LDown) { return true; }
            if (rTransitionID == TRANS_RunToIdle_RDown_RunPivot180R_LDown) { return true; }
            if (rTransitionID == TRANS_IdleTurn20R_IdleTurnEndPose) { return true; }
            if (rTransitionID == TRANS_IdleTurn90R_IdleTurnEndPose) { return true; }
            if (rTransitionID == TRANS_IdleTurn180R_IdleTurnEndPose) { return true; }
            if (rTransitionID == TRANS_IdleTurn20L_IdleTurnEndPose) { return true; }
            if (rTransitionID == TRANS_IdleTurn90L_IdleTurnEndPose) { return true; }
            if (rTransitionID == TRANS_IdleTurn180L_IdleTurnEndPose) { return true; }
            if (rTransitionID == TRANS_IdleTurnEndPose_MoveTree) { return true; }
            return false;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            string lLayer = mMotionController.Animator.GetLayerName(mMotionLayer._AnimatorLayerIndex);
            TRANS_AnyState_IdleToWalk90L = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleToWalk90L");
            TRANS_EntryState_IdleToWalk90L = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleToWalk90L");
            TRANS_AnyState_IdleToWalk90R = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleToWalk90R");
            TRANS_EntryState_IdleToWalk90R = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleToWalk90R");
            TRANS_AnyState_IdleToWalk180R = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleToWalk180R");
            TRANS_EntryState_IdleToWalk180R = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleToWalk180R");
            TRANS_AnyState_MoveTree = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_EntryState_MoveTree = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_AnyState_IdleToWalk180L = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleToWalk180L");
            TRANS_EntryState_IdleToWalk180L = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleToWalk180L");
            TRANS_AnyState_IdleToRun180L = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleToRun180L");
            TRANS_EntryState_IdleToRun180L = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleToRun180L");
            TRANS_AnyState_IdleToRun90L = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleToRun90L");
            TRANS_EntryState_IdleToRun90L = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleToRun90L");
            TRANS_AnyState_IdleToRun90R = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleToRun90R");
            TRANS_EntryState_IdleToRun90R = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleToRun90R");
            TRANS_AnyState_IdleToRun180R = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleToRun180R");
            TRANS_EntryState_IdleToRun180R = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleToRun180R");
            TRANS_AnyState_IdleToRun = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleToRun");
            TRANS_EntryState_IdleToRun = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleToRun");
            TRANS_AnyState_MoveTree = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_EntryState_MoveTree = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_AnyState_MoveTree = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_EntryState_MoveTree = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_AnyState_IdleTurn180L = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurn180L");
            TRANS_EntryState_IdleTurn180L = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurn180L");
            TRANS_AnyState_IdleTurn90L = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurn90L");
            TRANS_EntryState_IdleTurn90L = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurn90L");
            TRANS_AnyState_IdleTurn20L = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurn20L");
            TRANS_EntryState_IdleTurn20L = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurn20L");
            TRANS_AnyState_IdleTurn20R = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurn20R");
            TRANS_EntryState_IdleTurn20R = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurn20R");
            TRANS_AnyState_IdleTurn90R = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurn90R");
            TRANS_EntryState_IdleTurn90R = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurn90R");
            TRANS_AnyState_IdleTurn180R = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurn180R");
            TRANS_EntryState_IdleTurn180R = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurn180R");
            STATE_Start = mMotionController.AddAnimatorName("" + lLayer + ".Start");
            STATE_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_MoveTree_RunPivot180R_LDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.Move Tree -> " + lLayer + ".WalkRunPivot v2-SM.RunPivot180R_LDown");
            TRANS_MoveTree_RunPivot180R_LDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.Move Tree -> " + lLayer + ".WalkRunPivot v2-SM.RunPivot180R_LDown");
            TRANS_MoveTree_WalkPivot180L = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.Move Tree -> " + lLayer + ".WalkRunPivot v2-SM.WalkPivot180L");
            TRANS_MoveTree_WalkPivot180L = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.Move Tree -> " + lLayer + ".WalkRunPivot v2-SM.WalkPivot180L");
            TRANS_MoveTree_RunToIdle_LDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.Move Tree -> " + lLayer + ".WalkRunPivot v2-SM.RunToIdle_LDown");
            TRANS_MoveTree_WalkToIdle_LDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.Move Tree -> " + lLayer + ".WalkRunPivot v2-SM.WalkToIdle_LDown");
            TRANS_MoveTree_RunToIdle_RDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.Move Tree -> " + lLayer + ".WalkRunPivot v2-SM.RunToIdle_RDown");
            TRANS_MoveTree_WalkToIdle_RDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.Move Tree -> " + lLayer + ".WalkRunPivot v2-SM.WalkToIdle_RDown");
            TRANS_MoveTree_RunToIdle_RDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.Move Tree -> " + lLayer + ".WalkRunPivot v2-SM.RunToIdle_RDown");
            TRANS_MoveTree_RunToIdle_LDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.Move Tree -> " + lLayer + ".WalkRunPivot v2-SM.RunToIdle_LDown");
            TRANS_MoveTree_WalkToIdle_RDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.Move Tree -> " + lLayer + ".WalkRunPivot v2-SM.WalkToIdle_RDown");
            TRANS_MoveTree_WalkToIdle_LDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.Move Tree -> " + lLayer + ".WalkRunPivot v2-SM.WalkToIdle_LDown");
            STATE_IdleToWalk90L = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToWalk90L");
            TRANS_IdleToWalk90L_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToWalk90L -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_IdleToWalk90L_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToWalk90L -> " + lLayer + ".WalkRunPivot v2-SM.IdlePose");
            STATE_IdleToWalk90R = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToWalk90R");
            TRANS_IdleToWalk90R_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToWalk90R -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_IdleToWalk90R_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToWalk90R -> " + lLayer + ".WalkRunPivot v2-SM.IdlePose");
            STATE_IdleToWalk180R = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToWalk180R");
            TRANS_IdleToWalk180R_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToWalk180R -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_IdleToWalk180R_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToWalk180R -> " + lLayer + ".WalkRunPivot v2-SM.IdlePose");
            STATE_IdleToWalk180L = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToWalk180L");
            TRANS_IdleToWalk180L_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToWalk180L -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_IdleToWalk180L_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToWalk180L -> " + lLayer + ".WalkRunPivot v2-SM.IdlePose");
            STATE_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdlePose");
            STATE_IdleToRun90L = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun90L");
            TRANS_IdleToRun90L_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun90L -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_IdleToRun90L_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun90L -> " + lLayer + ".WalkRunPivot v2-SM.IdlePose");
            STATE_IdleToRun180L = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun180L");
            TRANS_IdleToRun180L_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun180L -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_IdleToRun180L_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun180L -> " + lLayer + ".WalkRunPivot v2-SM.IdlePose");
            STATE_IdleToRun90R = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun90R");
            TRANS_IdleToRun90R_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun90R -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_IdleToRun90R_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun90R -> " + lLayer + ".WalkRunPivot v2-SM.IdlePose");
            STATE_IdleToRun180R = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun180R");
            TRANS_IdleToRun180R_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun180R -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_IdleToRun180R_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun180R -> " + lLayer + ".WalkRunPivot v2-SM.IdlePose");
            STATE_IdleToRun = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun");
            TRANS_IdleToRun_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_IdleToRun_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleToRun -> " + lLayer + ".WalkRunPivot v2-SM.IdlePose");
            STATE_RunPivot180R_LDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.RunPivot180R_LDown");
            TRANS_RunPivot180R_LDown_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.RunPivot180R_LDown -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            STATE_WalkPivot180L = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.WalkPivot180L");
            TRANS_WalkPivot180L_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.WalkPivot180L -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            STATE_RunToIdle_LDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.RunToIdle_LDown");
            TRANS_RunToIdle_LDown_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.RunToIdle_LDown -> " + lLayer + ".WalkRunPivot v2-SM.IdlePose");
            TRANS_RunToIdle_LDown_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.RunToIdle_LDown -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_RunToIdle_LDown_RunPivot180R_LDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.RunToIdle_LDown -> " + lLayer + ".WalkRunPivot v2-SM.RunPivot180R_LDown");
            TRANS_RunToIdle_LDown_RunPivot180R_LDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.RunToIdle_LDown -> " + lLayer + ".WalkRunPivot v2-SM.RunPivot180R_LDown");
            STATE_WalkToIdle_LDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.WalkToIdle_LDown");
            TRANS_WalkToIdle_LDown_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.WalkToIdle_LDown -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_WalkToIdle_LDown_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.WalkToIdle_LDown -> " + lLayer + ".WalkRunPivot v2-SM.IdlePose");
            STATE_WalkToIdle_RDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.WalkToIdle_RDown");
            TRANS_WalkToIdle_RDown_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.WalkToIdle_RDown -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_WalkToIdle_RDown_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.WalkToIdle_RDown -> " + lLayer + ".WalkRunPivot v2-SM.IdlePose");
            STATE_RunToIdle_RDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.RunToIdle_RDown");
            TRANS_RunToIdle_RDown_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.RunToIdle_RDown -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
            TRANS_RunToIdle_RDown_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.RunToIdle_RDown -> " + lLayer + ".WalkRunPivot v2-SM.IdlePose");
            TRANS_RunToIdle_RDown_RunPivot180R_LDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.RunToIdle_RDown -> " + lLayer + ".WalkRunPivot v2-SM.RunPivot180R_LDown");
            TRANS_RunToIdle_RDown_RunPivot180R_LDown = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.RunToIdle_RDown -> " + lLayer + ".WalkRunPivot v2-SM.RunPivot180R_LDown");
            STATE_IdleTurn20R = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleTurn20R");
            TRANS_IdleTurn20R_IdleTurnEndPose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleTurn20R -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurnEndPose");
            STATE_IdleTurn90R = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleTurn90R");
            TRANS_IdleTurn90R_IdleTurnEndPose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleTurn90R -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurnEndPose");
            STATE_IdleTurn180R = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleTurn180R");
            TRANS_IdleTurn180R_IdleTurnEndPose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleTurn180R -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurnEndPose");
            STATE_IdleTurn20L = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleTurn20L");
            TRANS_IdleTurn20L_IdleTurnEndPose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleTurn20L -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurnEndPose");
            STATE_IdleTurn90L = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleTurn90L");
            TRANS_IdleTurn90L_IdleTurnEndPose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleTurn90L -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurnEndPose");
            STATE_IdleTurn180L = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleTurn180L");
            TRANS_IdleTurn180L_IdleTurnEndPose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleTurn180L -> " + lLayer + ".WalkRunPivot v2-SM.IdleTurnEndPose");
            STATE_IdleTurnEndPose = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleTurnEndPose");
            TRANS_IdleTurnEndPose_MoveTree = mMotionController.AddAnimatorName("" + lLayer + ".WalkRunPivot v2-SM.IdleTurnEndPose -> " + lLayer + ".WalkRunPivot v2-SM.Move Tree");
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

            UnityEditor.Animations.AnimatorStateMachine lSSM_72014 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "WalkRunPivot v2-SM");
            if (lSSM_72014 == null) { lSSM_72014 = lLayerStateMachine.AddStateMachine("WalkRunPivot v2-SM", new Vector3(624, -756, 0)); }

            UnityEditor.Animations.AnimatorState lState_64316 = MotionControllerMotion.EditorFindState(lSSM_72014, "Move Tree");
            if (lState_64316 == null) { lState_64316 = lSSM_72014.AddState("Move Tree", new Vector3(240, 372, 0)); }
            lState_64316.speed = 1f;
            lState_64316.mirror = false;
            lState_64316.tag = "";

            UnityEditor.Animations.BlendTree lM_47872 = MotionControllerMotion.EditorCreateBlendTree("Move Blend Tree", lController, rLayerIndex);
            lM_47872.blendType = UnityEditor.Animations.BlendTreeType.Simple1D;
            lM_47872.blendParameter = "InputMagnitude";
            lM_47872.blendParameterY = "InputX";
#if !(UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3)
            lM_47872.useAutomaticThresholds = true;
#endif
            lM_47872.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx", "IdlePose"), 0f);
            lM_47872.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_WalkFWD_v2.fbx", "WalkForward"), 0.5f);
            lM_47872.AddChild(MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunForward_v2.fbx", "RunForward"), 1f);
            lState_64316.motion = lM_47872;

            UnityEditor.Animations.AnimatorState lState_67868 = MotionControllerMotion.EditorFindState(lSSM_72014, "IdleToWalk90L");
            if (lState_67868 == null) { lState_67868 = lSSM_72014.AddState("IdleToWalk90L", new Vector3(-180, 204, 0)); }
            lState_67868.speed = 1.3f;
            lState_67868.mirror = false;
            lState_67868.tag = "";
            lState_67868.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx", "IdleToWalk90L");

            UnityEditor.Animations.AnimatorState lState_66242 = MotionControllerMotion.EditorFindState(lSSM_72014, "IdleToWalk90R");
            if (lState_66242 == null) { lState_66242 = lSSM_72014.AddState("IdleToWalk90R", new Vector3(-180, 264, 0)); }
            lState_66242.speed = 1.3f;
            lState_66242.mirror = false;
            lState_66242.tag = "";
            lState_66242.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx", "IdleToWalk90R");

            UnityEditor.Animations.AnimatorState lState_64410 = MotionControllerMotion.EditorFindState(lSSM_72014, "IdleToWalk180R");
            if (lState_64410 == null) { lState_64410 = lSSM_72014.AddState("IdleToWalk180R", new Vector3(-180, 324, 0)); }
            lState_64410.speed = 1.3f;
            lState_64410.mirror = false;
            lState_64410.tag = "";
            lState_64410.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx", "IdleToWalk180R");

            UnityEditor.Animations.AnimatorState lState_67518 = MotionControllerMotion.EditorFindState(lSSM_72014, "IdleToWalk180L");
            if (lState_67518 == null) { lState_67518 = lSSM_72014.AddState("IdleToWalk180L", new Vector3(-180, 144, 0)); }
            lState_67518.speed = 1.3f;
            lState_67518.mirror = false;
            lState_67518.tag = "";
            lState_67518.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx", "IdleToWalk180L");

            UnityEditor.Animations.AnimatorState lState_70712 = MotionControllerMotion.EditorFindState(lSSM_72014, "IdlePose");
            if (lState_70712 == null) { lState_70712 = lSSM_72014.AddState("IdlePose", new Vector3(132, 216, 0)); }
            lState_70712.speed = 1f;
            lState_70712.mirror = false;
            lState_70712.tag = "";
            lState_70712.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx", "IdlePose");

            UnityEditor.Animations.AnimatorState lState_64800 = MotionControllerMotion.EditorFindState(lSSM_72014, "IdleToRun90L");
            if (lState_64800 == null) { lState_64800 = lSSM_72014.AddState("IdleToRun90L", new Vector3(-168, 492, 0)); }
            lState_64800.speed = 1.5f;
            lState_64800.mirror = false;
            lState_64800.tag = "";
            lState_64800.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_Idle2Run_v2.fbx", "IdleToRun90L");

            UnityEditor.Animations.AnimatorState lState_65040 = MotionControllerMotion.EditorFindState(lSSM_72014, "IdleToRun180L");
            if (lState_65040 == null) { lState_65040 = lSSM_72014.AddState("IdleToRun180L", new Vector3(-168, 432, 0)); }
            lState_65040.speed = 1.3f;
            lState_65040.mirror = false;
            lState_65040.tag = "";
            lState_65040.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_Idle2Run_v2.fbx", "IdleToRun180L");

            UnityEditor.Animations.AnimatorState lState_65850 = MotionControllerMotion.EditorFindState(lSSM_72014, "IdleToRun90R");
            if (lState_65850 == null) { lState_65850 = lSSM_72014.AddState("IdleToRun90R", new Vector3(-168, 612, 0)); }
            lState_65850.speed = 1.5f;
            lState_65850.mirror = false;
            lState_65850.tag = "";
            lState_65850.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_Idle2Run_v2.fbx", "IdleToRun90R");

            UnityEditor.Animations.AnimatorState lState_68600 = MotionControllerMotion.EditorFindState(lSSM_72014, "IdleToRun180R");
            if (lState_68600 == null) { lState_68600 = lSSM_72014.AddState("IdleToRun180R", new Vector3(-168, 672, 0)); }
            lState_68600.speed = 1.3f;
            lState_68600.mirror = false;
            lState_68600.tag = "";
            lState_68600.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_Idle2Run_v2.fbx", "IdleToRun180R");

            UnityEditor.Animations.AnimatorState lState_65808 = MotionControllerMotion.EditorFindState(lSSM_72014, "IdleToRun");
            if (lState_65808 == null) { lState_65808 = lSSM_72014.AddState("IdleToRun", new Vector3(-168, 552, 0)); }
            lState_65808.speed = 2f;
            lState_65808.mirror = false;
            lState_65808.tag = "";
            lState_65808.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunForward_v2.fbx", "IdleToRun");

            UnityEditor.Animations.AnimatorState lState_67386 = MotionControllerMotion.EditorFindState(lSSM_72014, "RunPivot180R_LDown");
            if (lState_67386 == null) { lState_67386 = lSSM_72014.AddState("RunPivot180R_LDown", new Vector3(144, 564, 0)); }
            lState_67386.speed = 1.2f;
            lState_67386.mirror = false;
            lState_67386.tag = "";
            lState_67386.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_PlantNTurn180_Run_R_1.fbx", "RunPivot180R_LDown");

            UnityEditor.Animations.AnimatorState lState_68434 = MotionControllerMotion.EditorFindState(lSSM_72014, "WalkPivot180L");
            if (lState_68434 == null) { lState_68434 = lSSM_72014.AddState("WalkPivot180L", new Vector3(360, 564, 0)); }
            lState_68434.speed = 1.5f;
            lState_68434.mirror = false;
            lState_68434.tag = "";
            lState_68434.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx", "WalkPivot180L");

            UnityEditor.Animations.AnimatorState lState_68386 = MotionControllerMotion.EditorFindState(lSSM_72014, "RunToIdle_LDown");
            if (lState_68386 == null) { lState_68386 = lSSM_72014.AddState("RunToIdle_LDown", new Vector3(576, 336, 0)); }
            lState_68386.speed = 1f;
            lState_68386.mirror = false;
            lState_68386.tag = "";
            lState_68386.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_PlantNTurn180_Run_R_2.fbx", "RunToIdle_LDown");

            UnityEditor.Animations.AnimatorState lState_68956 = MotionControllerMotion.EditorFindState(lSSM_72014, "WalkToIdle_LDown");
            if (lState_68956 == null) { lState_68956 = lSSM_72014.AddState("WalkToIdle_LDown", new Vector3(576, 492, 0)); }
            lState_68956.speed = 1f;
            lState_68956.mirror = false;
            lState_68956.tag = "";
            lState_68956.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx", "WalkToIdle_LDown");

            UnityEditor.Animations.AnimatorState lState_67882 = MotionControllerMotion.EditorFindState(lSSM_72014, "WalkToIdle_RDown");
            if (lState_67882 == null) { lState_67882 = lSSM_72014.AddState("WalkToIdle_RDown", new Vector3(576, 420, 0)); }
            lState_67882.speed = 1f;
            lState_67882.mirror = false;
            lState_67882.tag = "";
            lState_67882.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Walking/unity_Idle2walk_v2.fbx", "WalkToIdle_RDown");

            UnityEditor.Animations.AnimatorState lState_64612 = MotionControllerMotion.EditorFindState(lSSM_72014, "RunToIdle_RDown");
            if (lState_64612 == null) { lState_64612 = lSSM_72014.AddState("RunToIdle_RDown", new Vector3(576, 264, 0)); }
            lState_64612.speed = 1f;
            lState_64612.mirror = false;
            lState_64612.tag = "";
            lState_64612.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/unity_HalfSteps2Idle_PasingLongStepTOIdle.fbx", "RunToIdle_RDown");

            UnityEditor.Animations.AnimatorState lState_71084 = MotionControllerMotion.EditorFindState(lSSM_72014, "IdleTurn20R");
            if (lState_71084 == null) { lState_71084 = lSSM_72014.AddState("IdleTurn20R", new Vector3(-720, 408, 0)); }
            lState_71084.speed = 1f;
            lState_71084.mirror = false;
            lState_71084.tag = "";
            lState_71084.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx", "IdleTurn90R");

            UnityEditor.Animations.AnimatorState lState_66050 = MotionControllerMotion.EditorFindState(lSSM_72014, "IdleTurn90R");
            if (lState_66050 == null) { lState_66050 = lSSM_72014.AddState("IdleTurn90R", new Vector3(-720, 468, 0)); }
            lState_66050.speed = 1.6f;
            lState_66050.mirror = false;
            lState_66050.tag = "";
            lState_66050.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx", "IdleTurn90R");

            UnityEditor.Animations.AnimatorState lState_70048 = MotionControllerMotion.EditorFindState(lSSM_72014, "IdleTurn180R");
            if (lState_70048 == null) { lState_70048 = lSSM_72014.AddState("IdleTurn180R", new Vector3(-720, 528, 0)); }
            lState_70048.speed = 1.4f;
            lState_70048.mirror = false;
            lState_70048.tag = "";
            lState_70048.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx", "IdleTurn180R");

            UnityEditor.Animations.AnimatorState lState_65954 = MotionControllerMotion.EditorFindState(lSSM_72014, "IdleTurn20L");
            if (lState_65954 == null) { lState_65954 = lSSM_72014.AddState("IdleTurn20L", new Vector3(-720, 348, 0)); }
            lState_65954.speed = 1f;
            lState_65954.mirror = false;
            lState_65954.tag = "";
            lState_65954.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx", "IdleTurn90L");

            UnityEditor.Animations.AnimatorState lState_68904 = MotionControllerMotion.EditorFindState(lSSM_72014, "IdleTurn90L");
            if (lState_68904 == null) { lState_68904 = lSSM_72014.AddState("IdleTurn90L", new Vector3(-720, 288, 0)); }
            lState_68904.speed = 1.6f;
            lState_68904.mirror = false;
            lState_68904.tag = "";
            lState_68904.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx", "IdleTurn90L");

            UnityEditor.Animations.AnimatorState lState_67204 = MotionControllerMotion.EditorFindState(lSSM_72014, "IdleTurn180L");
            if (lState_67204 == null) { lState_67204 = lSSM_72014.AddState("IdleTurn180L", new Vector3(-720, 228, 0)); }
            lState_67204.speed = 1.4f;
            lState_67204.mirror = false;
            lState_67204.tag = "";
            lState_67204.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx", "IdleTurn180L");

            UnityEditor.Animations.AnimatorState lState_65326 = MotionControllerMotion.EditorFindState(lSSM_72014, "IdleTurnEndPose");
            if (lState_65326 == null) { lState_65326 = lSSM_72014.AddState("IdleTurnEndPose", new Vector3(-984, 372, 0)); }
            lState_65326.speed = 1f;
            lState_65326.mirror = false;
            lState_65326.tag = "";
            lState_65326.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx", "IdlePose");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_59048 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_67868, 0);
            if (lAnyTransition_59048 == null) { lAnyTransition_59048 = lLayerStateMachine.AddAnyStateTransition(lState_67868); }
            lAnyTransition_59048.isExit = false;
            lAnyTransition_59048.hasExitTime = false;
            lAnyTransition_59048.hasFixedDuration = true;
            lAnyTransition_59048.exitTime = 0.9f;
            lAnyTransition_59048.duration = 0.1f;
            lAnyTransition_59048.offset = 0f;
            lAnyTransition_59048.mute = false;
            lAnyTransition_59048.solo = false;
            lAnyTransition_59048.canTransitionToSelf = true;
            lAnyTransition_59048.orderedInterruption = true;
            lAnyTransition_59048.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_59048.conditions.Length - 1; i >= 0; i--) { lAnyTransition_59048.RemoveCondition(lAnyTransition_59048.conditions[i]); }
            lAnyTransition_59048.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_59048.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");
            lAnyTransition_59048.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -20f, "InputAngleFromAvatar");
            lAnyTransition_59048.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, -160f, "InputAngleFromAvatar");
            lAnyTransition_59048.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.6f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_57930 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_66242, 0);
            if (lAnyTransition_57930 == null) { lAnyTransition_57930 = lLayerStateMachine.AddAnyStateTransition(lState_66242); }
            lAnyTransition_57930.isExit = false;
            lAnyTransition_57930.hasExitTime = false;
            lAnyTransition_57930.hasFixedDuration = true;
            lAnyTransition_57930.exitTime = 0.9f;
            lAnyTransition_57930.duration = 0.1f;
            lAnyTransition_57930.offset = 0f;
            lAnyTransition_57930.mute = false;
            lAnyTransition_57930.solo = false;
            lAnyTransition_57930.canTransitionToSelf = true;
            lAnyTransition_57930.orderedInterruption = true;
            lAnyTransition_57930.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_57930.conditions.Length - 1; i >= 0; i--) { lAnyTransition_57930.RemoveCondition(lAnyTransition_57930.conditions[i]); }
            lAnyTransition_57930.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_57930.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");
            lAnyTransition_57930.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 20f, "InputAngleFromAvatar");
            lAnyTransition_57930.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 160f, "InputAngleFromAvatar");
            lAnyTransition_57930.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.6f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_50388 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_64410, 0);
            if (lAnyTransition_50388 == null) { lAnyTransition_50388 = lLayerStateMachine.AddAnyStateTransition(lState_64410); }
            lAnyTransition_50388.isExit = false;
            lAnyTransition_50388.hasExitTime = false;
            lAnyTransition_50388.hasFixedDuration = true;
            lAnyTransition_50388.exitTime = 0.9f;
            lAnyTransition_50388.duration = 0.1f;
            lAnyTransition_50388.offset = 0f;
            lAnyTransition_50388.mute = false;
            lAnyTransition_50388.solo = false;
            lAnyTransition_50388.canTransitionToSelf = true;
            lAnyTransition_50388.orderedInterruption = true;
            lAnyTransition_50388.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_50388.conditions.Length - 1; i >= 0; i--) { lAnyTransition_50388.RemoveCondition(lAnyTransition_50388.conditions[i]); }
            lAnyTransition_50388.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_50388.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");
            lAnyTransition_50388.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 160f, "InputAngleFromAvatar");
            lAnyTransition_50388.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.6f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_49256 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_64316, 0);
            if (lAnyTransition_49256 == null) { lAnyTransition_49256 = lLayerStateMachine.AddAnyStateTransition(lState_64316); }
            lAnyTransition_49256.isExit = false;
            lAnyTransition_49256.hasExitTime = false;
            lAnyTransition_49256.hasFixedDuration = true;
            lAnyTransition_49256.exitTime = 0.9f;
            lAnyTransition_49256.duration = 0.1f;
            lAnyTransition_49256.offset = 0f;
            lAnyTransition_49256.mute = false;
            lAnyTransition_49256.solo = false;
            lAnyTransition_49256.canTransitionToSelf = true;
            lAnyTransition_49256.orderedInterruption = true;
            lAnyTransition_49256.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_49256.conditions.Length - 1; i >= 0; i--) { lAnyTransition_49256.RemoveCondition(lAnyTransition_49256.conditions[i]); }
            lAnyTransition_49256.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_49256.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, -20f, "InputAngleFromAvatar");
            lAnyTransition_49256.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 20f, "InputAngleFromAvatar");
            lAnyTransition_49256.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.6f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_63036 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_67518, 0);
            if (lAnyTransition_63036 == null) { lAnyTransition_63036 = lLayerStateMachine.AddAnyStateTransition(lState_67518); }
            lAnyTransition_63036.isExit = false;
            lAnyTransition_63036.hasExitTime = false;
            lAnyTransition_63036.hasFixedDuration = true;
            lAnyTransition_63036.exitTime = 0.9f;
            lAnyTransition_63036.duration = 0.1f;
            lAnyTransition_63036.offset = 0f;
            lAnyTransition_63036.mute = false;
            lAnyTransition_63036.solo = false;
            lAnyTransition_63036.canTransitionToSelf = true;
            lAnyTransition_63036.orderedInterruption = true;
            lAnyTransition_63036.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_63036.conditions.Length - 1; i >= 0; i--) { lAnyTransition_63036.RemoveCondition(lAnyTransition_63036.conditions[i]); }
            lAnyTransition_63036.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_63036.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");
            lAnyTransition_63036.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -160f, "InputAngleFromAvatar");
            lAnyTransition_63036.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.6f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_63334 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_65040, 0);
            if (lAnyTransition_63334 == null) { lAnyTransition_63334 = lLayerStateMachine.AddAnyStateTransition(lState_65040); }
            lAnyTransition_63334.isExit = false;
            lAnyTransition_63334.hasExitTime = false;
            lAnyTransition_63334.hasFixedDuration = true;
            lAnyTransition_63334.exitTime = 0.9f;
            lAnyTransition_63334.duration = 0.1f;
            lAnyTransition_63334.offset = 0f;
            lAnyTransition_63334.mute = false;
            lAnyTransition_63334.solo = false;
            lAnyTransition_63334.canTransitionToSelf = true;
            lAnyTransition_63334.orderedInterruption = true;
            lAnyTransition_63334.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_63334.conditions.Length - 1; i >= 0; i--) { lAnyTransition_63334.RemoveCondition(lAnyTransition_63334.conditions[i]); }
            lAnyTransition_63334.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_63334.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");
            lAnyTransition_63334.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -160f, "InputAngleFromAvatar");
            lAnyTransition_63334.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.6f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_63694 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_64800, 0);
            if (lAnyTransition_63694 == null) { lAnyTransition_63694 = lLayerStateMachine.AddAnyStateTransition(lState_64800); }
            lAnyTransition_63694.isExit = false;
            lAnyTransition_63694.hasExitTime = false;
            lAnyTransition_63694.hasFixedDuration = true;
            lAnyTransition_63694.exitTime = 0.9f;
            lAnyTransition_63694.duration = 0.1f;
            lAnyTransition_63694.offset = 0f;
            lAnyTransition_63694.mute = false;
            lAnyTransition_63694.solo = false;
            lAnyTransition_63694.canTransitionToSelf = true;
            lAnyTransition_63694.orderedInterruption = true;
            lAnyTransition_63694.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_63694.conditions.Length - 1; i >= 0; i--) { lAnyTransition_63694.RemoveCondition(lAnyTransition_63694.conditions[i]); }
            lAnyTransition_63694.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_63694.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");
            lAnyTransition_63694.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -20f, "InputAngleFromAvatar");
            lAnyTransition_63694.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, -160f, "InputAngleFromAvatar");
            lAnyTransition_63694.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.6f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_50970 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_65850, 0);
            if (lAnyTransition_50970 == null) { lAnyTransition_50970 = lLayerStateMachine.AddAnyStateTransition(lState_65850); }
            lAnyTransition_50970.isExit = false;
            lAnyTransition_50970.hasExitTime = false;
            lAnyTransition_50970.hasFixedDuration = true;
            lAnyTransition_50970.exitTime = 0.9f;
            lAnyTransition_50970.duration = 0.1f;
            lAnyTransition_50970.offset = 0f;
            lAnyTransition_50970.mute = false;
            lAnyTransition_50970.solo = false;
            lAnyTransition_50970.canTransitionToSelf = true;
            lAnyTransition_50970.orderedInterruption = true;
            lAnyTransition_50970.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_50970.conditions.Length - 1; i >= 0; i--) { lAnyTransition_50970.RemoveCondition(lAnyTransition_50970.conditions[i]); }
            lAnyTransition_50970.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_50970.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");
            lAnyTransition_50970.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 20f, "InputAngleFromAvatar");
            lAnyTransition_50970.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 160f, "InputAngleFromAvatar");
            lAnyTransition_50970.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.6f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_59966 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_68600, 0);
            if (lAnyTransition_59966 == null) { lAnyTransition_59966 = lLayerStateMachine.AddAnyStateTransition(lState_68600); }
            lAnyTransition_59966.isExit = false;
            lAnyTransition_59966.hasExitTime = false;
            lAnyTransition_59966.hasFixedDuration = true;
            lAnyTransition_59966.exitTime = 0.9f;
            lAnyTransition_59966.duration = 0.1f;
            lAnyTransition_59966.offset = 0f;
            lAnyTransition_59966.mute = false;
            lAnyTransition_59966.solo = false;
            lAnyTransition_59966.canTransitionToSelf = true;
            lAnyTransition_59966.orderedInterruption = true;
            lAnyTransition_59966.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_59966.conditions.Length - 1; i >= 0; i--) { lAnyTransition_59966.RemoveCondition(lAnyTransition_59966.conditions[i]); }
            lAnyTransition_59966.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_59966.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");
            lAnyTransition_59966.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 160f, "InputAngleFromAvatar");
            lAnyTransition_59966.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.6f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_58438 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_65808, 0);
            if (lAnyTransition_58438 == null) { lAnyTransition_58438 = lLayerStateMachine.AddAnyStateTransition(lState_65808); }
            lAnyTransition_58438.isExit = false;
            lAnyTransition_58438.hasExitTime = false;
            lAnyTransition_58438.hasFixedDuration = true;
            lAnyTransition_58438.exitTime = 0.9f;
            lAnyTransition_58438.duration = 0.1f;
            lAnyTransition_58438.offset = 0f;
            lAnyTransition_58438.mute = false;
            lAnyTransition_58438.solo = false;
            lAnyTransition_58438.canTransitionToSelf = true;
            lAnyTransition_58438.orderedInterruption = true;
            lAnyTransition_58438.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_58438.conditions.Length - 1; i >= 0; i--) { lAnyTransition_58438.RemoveCondition(lAnyTransition_58438.conditions[i]); }
            lAnyTransition_58438.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_58438.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");
            lAnyTransition_58438.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, -20f, "InputAngleFromAvatar");
            lAnyTransition_58438.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 20f, "InputAngleFromAvatar");
            lAnyTransition_58438.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.6f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_56842 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_64316, 1);
            if (lAnyTransition_56842 == null) { lAnyTransition_56842 = lLayerStateMachine.AddAnyStateTransition(lState_64316); }
            lAnyTransition_56842.isExit = false;
            lAnyTransition_56842.hasExitTime = false;
            lAnyTransition_56842.hasFixedDuration = true;
            lAnyTransition_56842.exitTime = 0.9f;
            lAnyTransition_56842.duration = 0.1f;
            lAnyTransition_56842.offset = 0.5f;
            lAnyTransition_56842.mute = false;
            lAnyTransition_56842.solo = false;
            lAnyTransition_56842.canTransitionToSelf = true;
            lAnyTransition_56842.orderedInterruption = true;
            lAnyTransition_56842.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_56842.conditions.Length - 1; i >= 0; i--) { lAnyTransition_56842.RemoveCondition(lAnyTransition_56842.conditions[i]); }
            lAnyTransition_56842.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_56842.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 2f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_63790 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_64316, 2);
            if (lAnyTransition_63790 == null) { lAnyTransition_63790 = lLayerStateMachine.AddAnyStateTransition(lState_64316); }
            lAnyTransition_63790.isExit = false;
            lAnyTransition_63790.hasExitTime = false;
            lAnyTransition_63790.hasFixedDuration = true;
            lAnyTransition_63790.exitTime = 0.9f;
            lAnyTransition_63790.duration = 0.2f;
            lAnyTransition_63790.offset = 0f;
            lAnyTransition_63790.mute = false;
            lAnyTransition_63790.solo = false;
            lAnyTransition_63790.canTransitionToSelf = true;
            lAnyTransition_63790.orderedInterruption = true;
            lAnyTransition_63790.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_63790.conditions.Length - 1; i >= 0; i--) { lAnyTransition_63790.RemoveCondition(lAnyTransition_63790.conditions[i]); }
            lAnyTransition_63790.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27130f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_63790.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_53208 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_67204, 0);
            if (lAnyTransition_53208 == null) { lAnyTransition_53208 = lLayerStateMachine.AddAnyStateTransition(lState_67204); }
            lAnyTransition_53208.isExit = false;
            lAnyTransition_53208.hasExitTime = false;
            lAnyTransition_53208.hasFixedDuration = true;
            lAnyTransition_53208.exitTime = 0.9f;
            lAnyTransition_53208.duration = 0.05f;
            lAnyTransition_53208.offset = 0.2228713f;
            lAnyTransition_53208.mute = false;
            lAnyTransition_53208.solo = false;
            lAnyTransition_53208.canTransitionToSelf = true;
            lAnyTransition_53208.orderedInterruption = true;
            lAnyTransition_53208.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_53208.conditions.Length - 1; i >= 0; i--) { lAnyTransition_53208.RemoveCondition(lAnyTransition_53208.conditions[i]); }
            lAnyTransition_53208.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27135f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_53208.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -135f, "InputAngleFromAvatar");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_61690 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_68904, 0);
            if (lAnyTransition_61690 == null) { lAnyTransition_61690 = lLayerStateMachine.AddAnyStateTransition(lState_68904); }
            lAnyTransition_61690.isExit = false;
            lAnyTransition_61690.hasExitTime = false;
            lAnyTransition_61690.hasFixedDuration = true;
            lAnyTransition_61690.exitTime = 0.9f;
            lAnyTransition_61690.duration = 0.05f;
            lAnyTransition_61690.offset = 0.1442637f;
            lAnyTransition_61690.mute = false;
            lAnyTransition_61690.solo = false;
            lAnyTransition_61690.canTransitionToSelf = true;
            lAnyTransition_61690.orderedInterruption = true;
            lAnyTransition_61690.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_61690.conditions.Length - 1; i >= 0; i--) { lAnyTransition_61690.RemoveCondition(lAnyTransition_61690.conditions[i]); }
            lAnyTransition_61690.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27135f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_61690.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -45f, "InputAngleFromAvatar");
            lAnyTransition_61690.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, -135f, "InputAngleFromAvatar");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_50904 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_65954, 0);
            if (lAnyTransition_50904 == null) { lAnyTransition_50904 = lLayerStateMachine.AddAnyStateTransition(lState_65954); }
            lAnyTransition_50904.isExit = false;
            lAnyTransition_50904.hasExitTime = false;
            lAnyTransition_50904.hasFixedDuration = true;
            lAnyTransition_50904.exitTime = 0.9f;
            lAnyTransition_50904.duration = 0.05f;
            lAnyTransition_50904.offset = 0.1442637f;
            lAnyTransition_50904.mute = false;
            lAnyTransition_50904.solo = false;
            lAnyTransition_50904.canTransitionToSelf = true;
            lAnyTransition_50904.orderedInterruption = true;
            lAnyTransition_50904.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_50904.conditions.Length - 1; i >= 0; i--) { lAnyTransition_50904.RemoveCondition(lAnyTransition_50904.conditions[i]); }
            lAnyTransition_50904.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27135f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_50904.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0f, "InputAngleFromAvatar");
            lAnyTransition_50904.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, -45f, "InputAngleFromAvatar");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_52874 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_71084, 0);
            if (lAnyTransition_52874 == null) { lAnyTransition_52874 = lLayerStateMachine.AddAnyStateTransition(lState_71084); }
            lAnyTransition_52874.isExit = false;
            lAnyTransition_52874.hasExitTime = false;
            lAnyTransition_52874.hasFixedDuration = true;
            lAnyTransition_52874.exitTime = 0.9f;
            lAnyTransition_52874.duration = 0.05f;
            lAnyTransition_52874.offset = 0.2277291f;
            lAnyTransition_52874.mute = false;
            lAnyTransition_52874.solo = false;
            lAnyTransition_52874.canTransitionToSelf = true;
            lAnyTransition_52874.orderedInterruption = true;
            lAnyTransition_52874.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_52874.conditions.Length - 1; i >= 0; i--) { lAnyTransition_52874.RemoveCondition(lAnyTransition_52874.conditions[i]); }
            lAnyTransition_52874.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27135f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_52874.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0f, "InputAngleFromAvatar");
            lAnyTransition_52874.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 45f, "InputAngleFromAvatar");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_62600 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_66050, 0);
            if (lAnyTransition_62600 == null) { lAnyTransition_62600 = lLayerStateMachine.AddAnyStateTransition(lState_66050); }
            lAnyTransition_62600.isExit = false;
            lAnyTransition_62600.hasExitTime = false;
            lAnyTransition_62600.hasFixedDuration = true;
            lAnyTransition_62600.exitTime = 0.8999999f;
            lAnyTransition_62600.duration = 0.05000001f;
            lAnyTransition_62600.offset = 0.2277291f;
            lAnyTransition_62600.mute = false;
            lAnyTransition_62600.solo = false;
            lAnyTransition_62600.canTransitionToSelf = true;
            lAnyTransition_62600.orderedInterruption = true;
            lAnyTransition_62600.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_62600.conditions.Length - 1; i >= 0; i--) { lAnyTransition_62600.RemoveCondition(lAnyTransition_62600.conditions[i]); }
            lAnyTransition_62600.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27135f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_62600.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 45f, "InputAngleFromAvatar");
            lAnyTransition_62600.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 135f, "InputAngleFromAvatar");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_58836 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_70048, 0);
            if (lAnyTransition_58836 == null) { lAnyTransition_58836 = lLayerStateMachine.AddAnyStateTransition(lState_70048); }
            lAnyTransition_58836.isExit = false;
            lAnyTransition_58836.hasExitTime = false;
            lAnyTransition_58836.hasFixedDuration = true;
            lAnyTransition_58836.exitTime = 0.9f;
            lAnyTransition_58836.duration = 0.05f;
            lAnyTransition_58836.offset = 0.2689505f;
            lAnyTransition_58836.mute = false;
            lAnyTransition_58836.solo = false;
            lAnyTransition_58836.canTransitionToSelf = true;
            lAnyTransition_58836.orderedInterruption = true;
            lAnyTransition_58836.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_58836.conditions.Length - 1; i >= 0; i--) { lAnyTransition_58836.RemoveCondition(lAnyTransition_58836.conditions[i]); }
            lAnyTransition_58836.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27135f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_58836.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 135f, "InputAngleFromAvatar");

            UnityEditor.Animations.AnimatorStateTransition lTransition_50746 = MotionControllerMotion.EditorFindTransition(lState_64316, lState_67386, 0);
            if (lTransition_50746 == null) { lTransition_50746 = lState_64316.AddTransition(lState_67386); }
            lTransition_50746.isExit = false;
            lTransition_50746.hasExitTime = false;
            lTransition_50746.hasFixedDuration = true;
            lTransition_50746.exitTime = 0.5481927f;
            lTransition_50746.duration = 0.1f;
            lTransition_50746.offset = 0f;
            lTransition_50746.mute = false;
            lTransition_50746.solo = false;
            lTransition_50746.canTransitionToSelf = true;
            lTransition_50746.orderedInterruption = true;
            lTransition_50746.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_50746.conditions.Length - 1; i >= 0; i--) { lTransition_50746.RemoveCondition(lTransition_50746.conditions[i]); }
            lTransition_50746.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 160f, "InputAngleFromAvatar");
            lTransition_50746.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.6f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lTransition_59244 = MotionControllerMotion.EditorFindTransition(lState_64316, lState_67386, 1);
            if (lTransition_59244 == null) { lTransition_59244 = lState_64316.AddTransition(lState_67386); }
            lTransition_59244.isExit = false;
            lTransition_59244.hasExitTime = false;
            lTransition_59244.hasFixedDuration = true;
            lTransition_59244.exitTime = 0.5481927f;
            lTransition_59244.duration = 0.1f;
            lTransition_59244.offset = 0f;
            lTransition_59244.mute = false;
            lTransition_59244.solo = false;
            lTransition_59244.canTransitionToSelf = true;
            lTransition_59244.orderedInterruption = true;
            lTransition_59244.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_59244.conditions.Length - 1; i >= 0; i--) { lTransition_59244.RemoveCondition(lTransition_59244.conditions[i]); }
            lTransition_59244.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -160f, "InputAngleFromAvatar");
            lTransition_59244.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.6f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lTransition_58210 = MotionControllerMotion.EditorFindTransition(lState_64316, lState_68434, 0);
            if (lTransition_58210 == null) { lTransition_58210 = lState_64316.AddTransition(lState_68434); }
            lTransition_58210.isExit = false;
            lTransition_58210.hasExitTime = false;
            lTransition_58210.hasFixedDuration = true;
            lTransition_58210.exitTime = 0.5481927f;
            lTransition_58210.duration = 0.1f;
            lTransition_58210.offset = 0f;
            lTransition_58210.mute = false;
            lTransition_58210.solo = false;
            lTransition_58210.canTransitionToSelf = true;
            lTransition_58210.orderedInterruption = true;
            lTransition_58210.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_58210.conditions.Length - 1; i >= 0; i--) { lTransition_58210.RemoveCondition(lTransition_58210.conditions[i]); }
            lTransition_58210.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 160f, "InputAngleFromAvatar");
            lTransition_58210.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.2f, "InputMagnitude");
            lTransition_58210.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.6f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lTransition_55720 = MotionControllerMotion.EditorFindTransition(lState_64316, lState_68434, 1);
            if (lTransition_55720 == null) { lTransition_55720 = lState_64316.AddTransition(lState_68434); }
            lTransition_55720.isExit = false;
            lTransition_55720.hasExitTime = false;
            lTransition_55720.hasFixedDuration = true;
            lTransition_55720.exitTime = 0.5481927f;
            lTransition_55720.duration = 0.1f;
            lTransition_55720.offset = 0f;
            lTransition_55720.mute = false;
            lTransition_55720.solo = false;
            lTransition_55720.canTransitionToSelf = true;
            lTransition_55720.orderedInterruption = true;
            lTransition_55720.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_55720.conditions.Length - 1; i >= 0; i--) { lTransition_55720.RemoveCondition(lTransition_55720.conditions[i]); }
            lTransition_55720.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -160f, "InputAngleFromAvatar");
            lTransition_55720.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.2f, "InputMagnitude");
            lTransition_55720.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.6f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lTransition_62664 = MotionControllerMotion.EditorFindTransition(lState_64316, lState_68386, 0);
            if (lTransition_62664 == null) { lTransition_62664 = lState_64316.AddTransition(lState_68386); }
            lTransition_62664.isExit = false;
            lTransition_62664.hasExitTime = true;
            lTransition_62664.hasFixedDuration = true;
            lTransition_62664.exitTime = 0.5f;
            lTransition_62664.duration = 0.2f;
            lTransition_62664.offset = 0.3595567f;
            lTransition_62664.mute = false;
            lTransition_62664.solo = false;
            lTransition_62664.canTransitionToSelf = true;
            lTransition_62664.orderedInterruption = true;
            lTransition_62664.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_62664.conditions.Length - 1; i >= 0; i--) { lTransition_62664.RemoveCondition(lTransition_62664.conditions[i]); }
            lTransition_62664.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27131f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lTransition_60786 = MotionControllerMotion.EditorFindTransition(lState_64316, lState_68956, 0);
            if (lTransition_60786 == null) { lTransition_60786 = lState_64316.AddTransition(lState_68956); }
            lTransition_60786.isExit = false;
            lTransition_60786.hasExitTime = true;
            lTransition_60786.hasFixedDuration = true;
            lTransition_60786.exitTime = 0.5f;
            lTransition_60786.duration = 0.2f;
            lTransition_60786.offset = 0.5352634f;
            lTransition_60786.mute = false;
            lTransition_60786.solo = false;
            lTransition_60786.canTransitionToSelf = true;
            lTransition_60786.orderedInterruption = true;
            lTransition_60786.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_60786.conditions.Length - 1; i >= 0; i--) { lTransition_60786.RemoveCondition(lTransition_60786.conditions[i]); }
            lTransition_60786.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27132f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lTransition_53986 = MotionControllerMotion.EditorFindTransition(lState_64316, lState_64612, 0);
            if (lTransition_53986 == null) { lTransition_53986 = lState_64316.AddTransition(lState_64612); }
            lTransition_53986.isExit = false;
            lTransition_53986.hasExitTime = true;
            lTransition_53986.hasFixedDuration = true;
            lTransition_53986.exitTime = 1f;
            lTransition_53986.duration = 0.2f;
            lTransition_53986.offset = 0f;
            lTransition_53986.mute = false;
            lTransition_53986.solo = false;
            lTransition_53986.canTransitionToSelf = true;
            lTransition_53986.orderedInterruption = true;
            lTransition_53986.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_53986.conditions.Length - 1; i >= 0; i--) { lTransition_53986.RemoveCondition(lTransition_53986.conditions[i]); }
            lTransition_53986.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27131f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lTransition_62224 = MotionControllerMotion.EditorFindTransition(lState_64316, lState_67882, 0);
            if (lTransition_62224 == null) { lTransition_62224 = lState_64316.AddTransition(lState_67882); }
            lTransition_62224.isExit = false;
            lTransition_62224.hasExitTime = true;
            lTransition_62224.hasFixedDuration = true;
            lTransition_62224.exitTime = 1f;
            lTransition_62224.duration = 0.2f;
            lTransition_62224.offset = 0.4974566f;
            lTransition_62224.mute = false;
            lTransition_62224.solo = false;
            lTransition_62224.canTransitionToSelf = true;
            lTransition_62224.orderedInterruption = true;
            lTransition_62224.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_62224.conditions.Length - 1; i >= 0; i--) { lTransition_62224.RemoveCondition(lTransition_62224.conditions[i]); }
            lTransition_62224.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27132f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lTransition_59524 = MotionControllerMotion.EditorFindTransition(lState_64316, lState_64612, 1);
            if (lTransition_59524 == null) { lTransition_59524 = lState_64316.AddTransition(lState_64612); }
            lTransition_59524.isExit = false;
            lTransition_59524.hasExitTime = true;
            lTransition_59524.hasFixedDuration = true;
            lTransition_59524.exitTime = 0.25f;
            lTransition_59524.duration = 0.2f;
            lTransition_59524.offset = 0.1060333f;
            lTransition_59524.mute = false;
            lTransition_59524.solo = false;
            lTransition_59524.canTransitionToSelf = true;
            lTransition_59524.orderedInterruption = true;
            lTransition_59524.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_59524.conditions.Length - 1; i >= 0; i--) { lTransition_59524.RemoveCondition(lTransition_59524.conditions[i]); }
            lTransition_59524.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27131f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lTransition_52494 = MotionControllerMotion.EditorFindTransition(lState_64316, lState_68386, 1);
            if (lTransition_52494 == null) { lTransition_52494 = lState_64316.AddTransition(lState_68386); }
            lTransition_52494.isExit = false;
            lTransition_52494.hasExitTime = true;
            lTransition_52494.hasFixedDuration = true;
            lTransition_52494.exitTime = 0.75f;
            lTransition_52494.duration = 0.2f;
            lTransition_52494.offset = 0.4174516f;
            lTransition_52494.mute = false;
            lTransition_52494.solo = false;
            lTransition_52494.canTransitionToSelf = true;
            lTransition_52494.orderedInterruption = true;
            lTransition_52494.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_52494.conditions.Length - 1; i >= 0; i--) { lTransition_52494.RemoveCondition(lTransition_52494.conditions[i]); }
            lTransition_52494.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27131f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lTransition_59294 = MotionControllerMotion.EditorFindTransition(lState_64316, lState_67882, 1);
            if (lTransition_59294 == null) { lTransition_59294 = lState_64316.AddTransition(lState_67882); }
            lTransition_59294.isExit = false;
            lTransition_59294.hasExitTime = true;
            lTransition_59294.hasFixedDuration = true;
            lTransition_59294.exitTime = 0.75f;
            lTransition_59294.duration = 0.2f;
            lTransition_59294.offset = 0.256667f;
            lTransition_59294.mute = false;
            lTransition_59294.solo = false;
            lTransition_59294.canTransitionToSelf = true;
            lTransition_59294.orderedInterruption = true;
            lTransition_59294.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_59294.conditions.Length - 1; i >= 0; i--) { lTransition_59294.RemoveCondition(lTransition_59294.conditions[i]); }
            lTransition_59294.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27132f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lTransition_63144 = MotionControllerMotion.EditorFindTransition(lState_64316, lState_68956, 1);
            if (lTransition_63144 == null) { lTransition_63144 = lState_64316.AddTransition(lState_68956); }
            lTransition_63144.isExit = false;
            lTransition_63144.hasExitTime = true;
            lTransition_63144.hasFixedDuration = true;
            lTransition_63144.exitTime = 0.25f;
            lTransition_63144.duration = 0.2f;
            lTransition_63144.offset = 0.2689477f;
            lTransition_63144.mute = false;
            lTransition_63144.solo = false;
            lTransition_63144.canTransitionToSelf = true;
            lTransition_63144.orderedInterruption = true;
            lTransition_63144.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_63144.conditions.Length - 1; i >= 0; i--) { lTransition_63144.RemoveCondition(lTransition_63144.conditions[i]); }
            lTransition_63144.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27132f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lTransition_54852 = MotionControllerMotion.EditorFindTransition(lState_67868, lState_64316, 0);
            if (lTransition_54852 == null) { lTransition_54852 = lState_67868.AddTransition(lState_64316); }
            lTransition_54852.isExit = false;
            lTransition_54852.hasExitTime = true;
            lTransition_54852.hasFixedDuration = true;
            lTransition_54852.exitTime = 0.75f;
            lTransition_54852.duration = 0.15f;
            lTransition_54852.offset = 0.0963606f;
            lTransition_54852.mute = false;
            lTransition_54852.solo = false;
            lTransition_54852.canTransitionToSelf = true;
            lTransition_54852.orderedInterruption = true;
            lTransition_54852.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_54852.conditions.Length - 1; i >= 0; i--) { lTransition_54852.RemoveCondition(lTransition_54852.conditions[i]); }
            lTransition_54852.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lTransition_61634 = MotionControllerMotion.EditorFindTransition(lState_67868, lState_70712, 0);
            if (lTransition_61634 == null) { lTransition_61634 = lState_67868.AddTransition(lState_70712); }
            lTransition_61634.isExit = false;
            lTransition_61634.hasExitTime = true;
            lTransition_61634.hasFixedDuration = true;
            lTransition_61634.exitTime = 0.8404255f;
            lTransition_61634.duration = 0.25f;
            lTransition_61634.offset = 0f;
            lTransition_61634.mute = false;
            lTransition_61634.solo = false;
            lTransition_61634.canTransitionToSelf = true;
            lTransition_61634.orderedInterruption = true;
            lTransition_61634.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_61634.conditions.Length - 1; i >= 0; i--) { lTransition_61634.RemoveCondition(lTransition_61634.conditions[i]); }
            lTransition_61634.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lTransition_56378 = MotionControllerMotion.EditorFindTransition(lState_66242, lState_64316, 0);
            if (lTransition_56378 == null) { lTransition_56378 = lState_66242.AddTransition(lState_64316); }
            lTransition_56378.isExit = false;
            lTransition_56378.hasExitTime = true;
            lTransition_56378.hasFixedDuration = true;
            lTransition_56378.exitTime = 0.75f;
            lTransition_56378.duration = 0.15f;
            lTransition_56378.offset = 0.6026077f;
            lTransition_56378.mute = false;
            lTransition_56378.solo = false;
            lTransition_56378.canTransitionToSelf = true;
            lTransition_56378.orderedInterruption = true;
            lTransition_56378.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_56378.conditions.Length - 1; i >= 0; i--) { lTransition_56378.RemoveCondition(lTransition_56378.conditions[i]); }
            lTransition_56378.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lTransition_49428 = MotionControllerMotion.EditorFindTransition(lState_66242, lState_70712, 0);
            if (lTransition_49428 == null) { lTransition_49428 = lState_66242.AddTransition(lState_70712); }
            lTransition_49428.isExit = false;
            lTransition_49428.hasExitTime = true;
            lTransition_49428.hasFixedDuration = true;
            lTransition_49428.exitTime = 0.7916668f;
            lTransition_49428.duration = 0.25f;
            lTransition_49428.offset = 0f;
            lTransition_49428.mute = false;
            lTransition_49428.solo = false;
            lTransition_49428.canTransitionToSelf = true;
            lTransition_49428.orderedInterruption = true;
            lTransition_49428.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_49428.conditions.Length - 1; i >= 0; i--) { lTransition_49428.RemoveCondition(lTransition_49428.conditions[i]); }
            lTransition_49428.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lTransition_53528 = MotionControllerMotion.EditorFindTransition(lState_64410, lState_64316, 0);
            if (lTransition_53528 == null) { lTransition_53528 = lState_64410.AddTransition(lState_64316); }
            lTransition_53528.isExit = false;
            lTransition_53528.hasExitTime = true;
            lTransition_53528.hasFixedDuration = true;
            lTransition_53528.exitTime = 0.8846154f;
            lTransition_53528.duration = 0.25f;
            lTransition_53528.offset = 0.8864383f;
            lTransition_53528.mute = false;
            lTransition_53528.solo = false;
            lTransition_53528.canTransitionToSelf = true;
            lTransition_53528.orderedInterruption = true;
            lTransition_53528.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_53528.conditions.Length - 1; i >= 0; i--) { lTransition_53528.RemoveCondition(lTransition_53528.conditions[i]); }
            lTransition_53528.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lTransition_59224 = MotionControllerMotion.EditorFindTransition(lState_64410, lState_70712, 0);
            if (lTransition_59224 == null) { lTransition_59224 = lState_64410.AddTransition(lState_70712); }
            lTransition_59224.isExit = false;
            lTransition_59224.hasExitTime = true;
            lTransition_59224.hasFixedDuration = true;
            lTransition_59224.exitTime = 0.8584907f;
            lTransition_59224.duration = 0.25f;
            lTransition_59224.offset = 0f;
            lTransition_59224.mute = false;
            lTransition_59224.solo = false;
            lTransition_59224.canTransitionToSelf = true;
            lTransition_59224.orderedInterruption = true;
            lTransition_59224.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_59224.conditions.Length - 1; i >= 0; i--) { lTransition_59224.RemoveCondition(lTransition_59224.conditions[i]); }
            lTransition_59224.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lTransition_48842 = MotionControllerMotion.EditorFindTransition(lState_67518, lState_64316, 0);
            if (lTransition_48842 == null) { lTransition_48842 = lState_67518.AddTransition(lState_64316); }
            lTransition_48842.isExit = false;
            lTransition_48842.hasExitTime = true;
            lTransition_48842.hasFixedDuration = true;
            lTransition_48842.exitTime = 0.9074074f;
            lTransition_48842.duration = 0.25f;
            lTransition_48842.offset = 0.3468954f;
            lTransition_48842.mute = false;
            lTransition_48842.solo = false;
            lTransition_48842.canTransitionToSelf = true;
            lTransition_48842.orderedInterruption = true;
            lTransition_48842.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_48842.conditions.Length - 1; i >= 0; i--) { lTransition_48842.RemoveCondition(lTransition_48842.conditions[i]); }
            lTransition_48842.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lTransition_55394 = MotionControllerMotion.EditorFindTransition(lState_67518, lState_70712, 0);
            if (lTransition_55394 == null) { lTransition_55394 = lState_67518.AddTransition(lState_70712); }
            lTransition_55394.isExit = false;
            lTransition_55394.hasExitTime = true;
            lTransition_55394.hasFixedDuration = true;
            lTransition_55394.exitTime = 0.8584907f;
            lTransition_55394.duration = 0.25f;
            lTransition_55394.offset = 0f;
            lTransition_55394.mute = false;
            lTransition_55394.solo = false;
            lTransition_55394.canTransitionToSelf = true;
            lTransition_55394.orderedInterruption = true;
            lTransition_55394.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_55394.conditions.Length - 1; i >= 0; i--) { lTransition_55394.RemoveCondition(lTransition_55394.conditions[i]); }
            lTransition_55394.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lTransition_49910 = MotionControllerMotion.EditorFindTransition(lState_64800, lState_64316, 0);
            if (lTransition_49910 == null) { lTransition_49910 = lState_64800.AddTransition(lState_64316); }
            lTransition_49910.isExit = false;
            lTransition_49910.hasExitTime = true;
            lTransition_49910.hasFixedDuration = true;
            lTransition_49910.exitTime = 0.7222224f;
            lTransition_49910.duration = 0.25f;
            lTransition_49910.offset = 0f;
            lTransition_49910.mute = false;
            lTransition_49910.solo = false;
            lTransition_49910.canTransitionToSelf = true;
            lTransition_49910.orderedInterruption = true;
            lTransition_49910.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_49910.conditions.Length - 1; i >= 0; i--) { lTransition_49910.RemoveCondition(lTransition_49910.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_62014 = MotionControllerMotion.EditorFindTransition(lState_64800, lState_70712, 0);
            if (lTransition_62014 == null) { lTransition_62014 = lState_64800.AddTransition(lState_70712); }
            lTransition_62014.isExit = false;
            lTransition_62014.hasExitTime = true;
            lTransition_62014.hasFixedDuration = true;
            lTransition_62014.exitTime = 0.7794119f;
            lTransition_62014.duration = 0.25f;
            lTransition_62014.offset = 0f;
            lTransition_62014.mute = false;
            lTransition_62014.solo = false;
            lTransition_62014.canTransitionToSelf = true;
            lTransition_62014.orderedInterruption = true;
            lTransition_62014.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_62014.conditions.Length - 1; i >= 0; i--) { lTransition_62014.RemoveCondition(lTransition_62014.conditions[i]); }
            lTransition_62014.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lTransition_53080 = MotionControllerMotion.EditorFindTransition(lState_65040, lState_64316, 0);
            if (lTransition_53080 == null) { lTransition_53080 = lState_65040.AddTransition(lState_64316); }
            lTransition_53080.isExit = false;
            lTransition_53080.hasExitTime = true;
            lTransition_53080.hasFixedDuration = true;
            lTransition_53080.exitTime = 0.7580653f;
            lTransition_53080.duration = 0.25f;
            lTransition_53080.offset = 0f;
            lTransition_53080.mute = false;
            lTransition_53080.solo = false;
            lTransition_53080.canTransitionToSelf = true;
            lTransition_53080.orderedInterruption = true;
            lTransition_53080.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_53080.conditions.Length - 1; i >= 0; i--) { lTransition_53080.RemoveCondition(lTransition_53080.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_61184 = MotionControllerMotion.EditorFindTransition(lState_65040, lState_70712, 0);
            if (lTransition_61184 == null) { lTransition_61184 = lState_65040.AddTransition(lState_70712); }
            lTransition_61184.isExit = false;
            lTransition_61184.hasExitTime = true;
            lTransition_61184.hasFixedDuration = true;
            lTransition_61184.exitTime = 0.8125004f;
            lTransition_61184.duration = 0.25f;
            lTransition_61184.offset = 0f;
            lTransition_61184.mute = false;
            lTransition_61184.solo = false;
            lTransition_61184.canTransitionToSelf = true;
            lTransition_61184.orderedInterruption = true;
            lTransition_61184.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_61184.conditions.Length - 1; i >= 0; i--) { lTransition_61184.RemoveCondition(lTransition_61184.conditions[i]); }
            lTransition_61184.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lTransition_56376 = MotionControllerMotion.EditorFindTransition(lState_65850, lState_64316, 0);
            if (lTransition_56376 == null) { lTransition_56376 = lState_65850.AddTransition(lState_64316); }
            lTransition_56376.isExit = false;
            lTransition_56376.hasExitTime = true;
            lTransition_56376.hasFixedDuration = true;
            lTransition_56376.exitTime = 0.7580646f;
            lTransition_56376.duration = 0.25f;
            lTransition_56376.offset = 0.5379788f;
            lTransition_56376.mute = false;
            lTransition_56376.solo = false;
            lTransition_56376.canTransitionToSelf = true;
            lTransition_56376.orderedInterruption = true;
            lTransition_56376.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_56376.conditions.Length - 1; i >= 0; i--) { lTransition_56376.RemoveCondition(lTransition_56376.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_62926 = MotionControllerMotion.EditorFindTransition(lState_65850, lState_70712, 0);
            if (lTransition_62926 == null) { lTransition_62926 = lState_65850.AddTransition(lState_70712); }
            lTransition_62926.isExit = false;
            lTransition_62926.hasExitTime = true;
            lTransition_62926.hasFixedDuration = true;
            lTransition_62926.exitTime = 0.7794119f;
            lTransition_62926.duration = 0.25f;
            lTransition_62926.offset = 0f;
            lTransition_62926.mute = false;
            lTransition_62926.solo = false;
            lTransition_62926.canTransitionToSelf = true;
            lTransition_62926.orderedInterruption = true;
            lTransition_62926.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_62926.conditions.Length - 1; i >= 0; i--) { lTransition_62926.RemoveCondition(lTransition_62926.conditions[i]); }
            lTransition_62926.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lTransition_60648 = MotionControllerMotion.EditorFindTransition(lState_68600, lState_64316, 0);
            if (lTransition_60648 == null) { lTransition_60648 = lState_68600.AddTransition(lState_64316); }
            lTransition_60648.isExit = false;
            lTransition_60648.hasExitTime = true;
            lTransition_60648.hasFixedDuration = true;
            lTransition_60648.exitTime = 0.8255816f;
            lTransition_60648.duration = 0.25f;
            lTransition_60648.offset = 0.5181294f;
            lTransition_60648.mute = false;
            lTransition_60648.solo = false;
            lTransition_60648.canTransitionToSelf = true;
            lTransition_60648.orderedInterruption = true;
            lTransition_60648.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_60648.conditions.Length - 1; i >= 0; i--) { lTransition_60648.RemoveCondition(lTransition_60648.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_60584 = MotionControllerMotion.EditorFindTransition(lState_68600, lState_70712, 0);
            if (lTransition_60584 == null) { lTransition_60584 = lState_68600.AddTransition(lState_70712); }
            lTransition_60584.isExit = false;
            lTransition_60584.hasExitTime = true;
            lTransition_60584.hasFixedDuration = true;
            lTransition_60584.exitTime = 0.8125004f;
            lTransition_60584.duration = 0.25f;
            lTransition_60584.offset = 0f;
            lTransition_60584.mute = false;
            lTransition_60584.solo = false;
            lTransition_60584.canTransitionToSelf = true;
            lTransition_60584.orderedInterruption = true;
            lTransition_60584.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_60584.conditions.Length - 1; i >= 0; i--) { lTransition_60584.RemoveCondition(lTransition_60584.conditions[i]); }
            lTransition_60584.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lTransition_56186 = MotionControllerMotion.EditorFindTransition(lState_65808, lState_64316, 0);
            if (lTransition_56186 == null) { lTransition_56186 = lState_65808.AddTransition(lState_64316); }
            lTransition_56186.isExit = false;
            lTransition_56186.hasExitTime = true;
            lTransition_56186.hasFixedDuration = true;
            lTransition_56186.exitTime = 0.6182807f;
            lTransition_56186.duration = 0.25f;
            lTransition_56186.offset = 0.02634108f;
            lTransition_56186.mute = false;
            lTransition_56186.solo = false;
            lTransition_56186.canTransitionToSelf = true;
            lTransition_56186.orderedInterruption = true;
            lTransition_56186.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_56186.conditions.Length - 1; i >= 0; i--) { lTransition_56186.RemoveCondition(lTransition_56186.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_55874 = MotionControllerMotion.EditorFindTransition(lState_65808, lState_70712, 0);
            if (lTransition_55874 == null) { lTransition_55874 = lState_65808.AddTransition(lState_70712); }
            lTransition_55874.isExit = false;
            lTransition_55874.hasExitTime = true;
            lTransition_55874.hasFixedDuration = true;
            lTransition_55874.exitTime = 0.6250002f;
            lTransition_55874.duration = 0.25f;
            lTransition_55874.offset = 0f;
            lTransition_55874.mute = false;
            lTransition_55874.solo = false;
            lTransition_55874.canTransitionToSelf = true;
            lTransition_55874.orderedInterruption = true;
            lTransition_55874.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_55874.conditions.Length - 1; i >= 0; i--) { lTransition_55874.RemoveCondition(lTransition_55874.conditions[i]); }
            lTransition_55874.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.4f, "InputMagnitude");

            UnityEditor.Animations.AnimatorStateTransition lTransition_53876 = MotionControllerMotion.EditorFindTransition(lState_67386, lState_64316, 0);
            if (lTransition_53876 == null) { lTransition_53876 = lState_67386.AddTransition(lState_64316); }
            lTransition_53876.isExit = false;
            lTransition_53876.hasExitTime = true;
            lTransition_53876.hasFixedDuration = true;
            lTransition_53876.exitTime = 0.8469388f;
            lTransition_53876.duration = 0.25f;
            lTransition_53876.offset = 0f;
            lTransition_53876.mute = false;
            lTransition_53876.solo = false;
            lTransition_53876.canTransitionToSelf = true;
            lTransition_53876.orderedInterruption = true;
            lTransition_53876.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_53876.conditions.Length - 1; i >= 0; i--) { lTransition_53876.RemoveCondition(lTransition_53876.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_53544 = MotionControllerMotion.EditorFindTransition(lState_68434, lState_64316, 0);
            if (lTransition_53544 == null) { lTransition_53544 = lState_68434.AddTransition(lState_64316); }
            lTransition_53544.isExit = false;
            lTransition_53544.hasExitTime = true;
            lTransition_53544.hasFixedDuration = true;
            lTransition_53544.exitTime = 0.8636364f;
            lTransition_53544.duration = 0.25f;
            lTransition_53544.offset = 0.8593867f;
            lTransition_53544.mute = false;
            lTransition_53544.solo = false;
            lTransition_53544.canTransitionToSelf = true;
            lTransition_53544.orderedInterruption = true;
            lTransition_53544.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_53544.conditions.Length - 1; i >= 0; i--) { lTransition_53544.RemoveCondition(lTransition_53544.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_58816 = MotionControllerMotion.EditorFindTransition(lState_68386, lState_70712, 0);
            if (lTransition_58816 == null) { lTransition_58816 = lState_68386.AddTransition(lState_70712); }
            lTransition_58816.isExit = false;
            lTransition_58816.hasExitTime = true;
            lTransition_58816.hasFixedDuration = true;
            lTransition_58816.exitTime = 0.7f;
            lTransition_58816.duration = 0.2f;
            lTransition_58816.offset = 0f;
            lTransition_58816.mute = false;
            lTransition_58816.solo = false;
            lTransition_58816.canTransitionToSelf = true;
            lTransition_58816.orderedInterruption = true;
            lTransition_58816.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_58816.conditions.Length - 1; i >= 0; i--) { lTransition_58816.RemoveCondition(lTransition_58816.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_54208 = MotionControllerMotion.EditorFindTransition(lState_68386, lState_64316, 0);
            if (lTransition_54208 == null) { lTransition_54208 = lState_68386.AddTransition(lState_64316); }
            lTransition_54208.isExit = false;
            lTransition_54208.hasExitTime = false;
            lTransition_54208.hasFixedDuration = true;
            lTransition_54208.exitTime = 0.8684211f;
            lTransition_54208.duration = 0.25f;
            lTransition_54208.offset = 0f;
            lTransition_54208.mute = false;
            lTransition_54208.solo = false;
            lTransition_54208.canTransitionToSelf = true;
            lTransition_54208.orderedInterruption = true;
            lTransition_54208.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_54208.conditions.Length - 1; i >= 0; i--) { lTransition_54208.RemoveCondition(lTransition_54208.conditions[i]); }
            lTransition_54208.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27133f, "L" + rLayerIndex + "MotionPhase");
            lTransition_54208.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, -160f, "InputAngleFromAvatar");
            lTransition_54208.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 160f, "InputAngleFromAvatar");

            UnityEditor.Animations.AnimatorStateTransition lTransition_N190382 = MotionControllerMotion.EditorFindTransition(lState_68386, lState_67386, 0);
            if (lTransition_N190382 == null) { lTransition_N190382 = lState_68386.AddTransition(lState_67386); }
            lTransition_N190382.isExit = false;
            lTransition_N190382.hasExitTime = false;
            lTransition_N190382.hasFixedDuration = true;
            lTransition_N190382.exitTime = 0.8684211f;
            lTransition_N190382.duration = 0.25f;
            lTransition_N190382.offset = 0f;
            lTransition_N190382.mute = false;
            lTransition_N190382.solo = false;
            lTransition_N190382.canTransitionToSelf = true;
            lTransition_N190382.orderedInterruption = true;
            lTransition_N190382.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_N190382.conditions.Length - 1; i >= 0; i--) { lTransition_N190382.RemoveCondition(lTransition_N190382.conditions[i]); }
            lTransition_N190382.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27133f, "L" + rLayerIndex + "MotionPhase");
            lTransition_N190382.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 160f, "InputAngleFromAvatar");

            UnityEditor.Animations.AnimatorStateTransition lTransition_N190818 = MotionControllerMotion.EditorFindTransition(lState_68386, lState_67386, 1);
            if (lTransition_N190818 == null) { lTransition_N190818 = lState_68386.AddTransition(lState_67386); }
            lTransition_N190818.isExit = false;
            lTransition_N190818.hasExitTime = false;
            lTransition_N190818.hasFixedDuration = true;
            lTransition_N190818.exitTime = 0.8684211f;
            lTransition_N190818.duration = 0.25f;
            lTransition_N190818.offset = 0f;
            lTransition_N190818.mute = false;
            lTransition_N190818.solo = false;
            lTransition_N190818.canTransitionToSelf = true;
            lTransition_N190818.orderedInterruption = true;
            lTransition_N190818.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_N190818.conditions.Length - 1; i >= 0; i--) { lTransition_N190818.RemoveCondition(lTransition_N190818.conditions[i]); }
            lTransition_N190818.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27133f, "L" + rLayerIndex + "MotionPhase");
            lTransition_N190818.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -160f, "InputAngleFromAvatar");

            UnityEditor.Animations.AnimatorStateTransition lTransition_52796 = MotionControllerMotion.EditorFindTransition(lState_68956, lState_64316, 0);
            if (lTransition_52796 == null) { lTransition_52796 = lState_68956.AddTransition(lState_64316); }
            lTransition_52796.isExit = false;
            lTransition_52796.hasExitTime = false;
            lTransition_52796.hasFixedDuration = true;
            lTransition_52796.exitTime = 0.75f;
            lTransition_52796.duration = 0.25f;
            lTransition_52796.offset = 0f;
            lTransition_52796.mute = false;
            lTransition_52796.solo = false;
            lTransition_52796.canTransitionToSelf = true;
            lTransition_52796.orderedInterruption = true;
            lTransition_52796.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_52796.conditions.Length - 1; i >= 0; i--) { lTransition_52796.RemoveCondition(lTransition_52796.conditions[i]); }
            lTransition_52796.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27133f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lTransition_60738 = MotionControllerMotion.EditorFindTransition(lState_68956, lState_70712, 0);
            if (lTransition_60738 == null) { lTransition_60738 = lState_68956.AddTransition(lState_70712); }
            lTransition_60738.isExit = false;
            lTransition_60738.hasExitTime = true;
            lTransition_60738.hasFixedDuration = true;
            lTransition_60738.exitTime = 0.8f;
            lTransition_60738.duration = 0.2f;
            lTransition_60738.offset = 0f;
            lTransition_60738.mute = false;
            lTransition_60738.solo = false;
            lTransition_60738.canTransitionToSelf = true;
            lTransition_60738.orderedInterruption = true;
            lTransition_60738.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_60738.conditions.Length - 1; i >= 0; i--) { lTransition_60738.RemoveCondition(lTransition_60738.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_56448 = MotionControllerMotion.EditorFindTransition(lState_67882, lState_64316, 0);
            if (lTransition_56448 == null) { lTransition_56448 = lState_67882.AddTransition(lState_64316); }
            lTransition_56448.isExit = false;
            lTransition_56448.hasExitTime = false;
            lTransition_56448.hasFixedDuration = true;
            lTransition_56448.exitTime = 0.75f;
            lTransition_56448.duration = 0.25f;
            lTransition_56448.offset = 0f;
            lTransition_56448.mute = false;
            lTransition_56448.solo = false;
            lTransition_56448.canTransitionToSelf = true;
            lTransition_56448.orderedInterruption = true;
            lTransition_56448.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_56448.conditions.Length - 1; i >= 0; i--) { lTransition_56448.RemoveCondition(lTransition_56448.conditions[i]); }
            lTransition_56448.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27133f, "L" + rLayerIndex + "MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lTransition_48486 = MotionControllerMotion.EditorFindTransition(lState_67882, lState_70712, 0);
            if (lTransition_48486 == null) { lTransition_48486 = lState_67882.AddTransition(lState_70712); }
            lTransition_48486.isExit = false;
            lTransition_48486.hasExitTime = true;
            lTransition_48486.hasFixedDuration = true;
            lTransition_48486.exitTime = 0.8f;
            lTransition_48486.duration = 0.2f;
            lTransition_48486.offset = 0f;
            lTransition_48486.mute = false;
            lTransition_48486.solo = false;
            lTransition_48486.canTransitionToSelf = true;
            lTransition_48486.orderedInterruption = true;
            lTransition_48486.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_48486.conditions.Length - 1; i >= 0; i--) { lTransition_48486.RemoveCondition(lTransition_48486.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_61854 = MotionControllerMotion.EditorFindTransition(lState_64612, lState_64316, 0);
            if (lTransition_61854 == null) { lTransition_61854 = lState_64612.AddTransition(lState_64316); }
            lTransition_61854.isExit = false;
            lTransition_61854.hasExitTime = false;
            lTransition_61854.hasFixedDuration = true;
            lTransition_61854.exitTime = 0.8170732f;
            lTransition_61854.duration = 0.25f;
            lTransition_61854.offset = 0f;
            lTransition_61854.mute = false;
            lTransition_61854.solo = false;
            lTransition_61854.canTransitionToSelf = true;
            lTransition_61854.orderedInterruption = true;
            lTransition_61854.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_61854.conditions.Length - 1; i >= 0; i--) { lTransition_61854.RemoveCondition(lTransition_61854.conditions[i]); }
            lTransition_61854.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27133f, "L" + rLayerIndex + "MotionPhase");
            lTransition_61854.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, -160f, "InputAngleFromAvatar");
            lTransition_61854.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 160f, "InputAngleFromAvatar");

            UnityEditor.Animations.AnimatorStateTransition lTransition_50738 = MotionControllerMotion.EditorFindTransition(lState_64612, lState_70712, 0);
            if (lTransition_50738 == null) { lTransition_50738 = lState_64612.AddTransition(lState_70712); }
            lTransition_50738.isExit = false;
            lTransition_50738.hasExitTime = true;
            lTransition_50738.hasFixedDuration = true;
            lTransition_50738.exitTime = 0.5021765f;
            lTransition_50738.duration = 0.1999999f;
            lTransition_50738.offset = 0.04457206f;
            lTransition_50738.mute = false;
            lTransition_50738.solo = false;
            lTransition_50738.canTransitionToSelf = true;
            lTransition_50738.orderedInterruption = true;
            lTransition_50738.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_50738.conditions.Length - 1; i >= 0; i--) { lTransition_50738.RemoveCondition(lTransition_50738.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_N112062 = MotionControllerMotion.EditorFindTransition(lState_64612, lState_67386, 0);
            if (lTransition_N112062 == null) { lTransition_N112062 = lState_64612.AddTransition(lState_67386); }
            lTransition_N112062.isExit = false;
            lTransition_N112062.hasExitTime = false;
            lTransition_N112062.hasFixedDuration = true;
            lTransition_N112062.exitTime = 0.8170732f;
            lTransition_N112062.duration = 0.25f;
            lTransition_N112062.offset = 0f;
            lTransition_N112062.mute = false;
            lTransition_N112062.solo = false;
            lTransition_N112062.canTransitionToSelf = true;
            lTransition_N112062.orderedInterruption = true;
            lTransition_N112062.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_N112062.conditions.Length - 1; i >= 0; i--) { lTransition_N112062.RemoveCondition(lTransition_N112062.conditions[i]); }
            lTransition_N112062.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27133f, "L" + rLayerIndex + "MotionPhase");
            lTransition_N112062.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 160f, "InputAngleFromAvatar");

            UnityEditor.Animations.AnimatorStateTransition lTransition_N112416 = MotionControllerMotion.EditorFindTransition(lState_64612, lState_67386, 1);
            if (lTransition_N112416 == null) { lTransition_N112416 = lState_64612.AddTransition(lState_67386); }
            lTransition_N112416.isExit = false;
            lTransition_N112416.hasExitTime = false;
            lTransition_N112416.hasFixedDuration = true;
            lTransition_N112416.exitTime = 0.8170732f;
            lTransition_N112416.duration = 0.25f;
            lTransition_N112416.offset = 0f;
            lTransition_N112416.mute = false;
            lTransition_N112416.solo = false;
            lTransition_N112416.canTransitionToSelf = true;
            lTransition_N112416.orderedInterruption = true;
            lTransition_N112416.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_N112416.conditions.Length - 1; i >= 0; i--) { lTransition_N112416.RemoveCondition(lTransition_N112416.conditions[i]); }
            lTransition_N112416.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27133f, "L" + rLayerIndex + "MotionPhase");
            lTransition_N112416.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -160f, "InputAngleFromAvatar");

            UnityEditor.Animations.AnimatorStateTransition lTransition_59176 = MotionControllerMotion.EditorFindTransition(lState_71084, lState_65326, 0);
            if (lTransition_59176 == null) { lTransition_59176 = lState_71084.AddTransition(lState_65326); }
            lTransition_59176.isExit = false;
            lTransition_59176.hasExitTime = true;
            lTransition_59176.hasFixedDuration = true;
            lTransition_59176.exitTime = 0.3138752f;
            lTransition_59176.duration = 0.15f;
            lTransition_59176.offset = 0f;
            lTransition_59176.mute = false;
            lTransition_59176.solo = false;
            lTransition_59176.canTransitionToSelf = true;
            lTransition_59176.orderedInterruption = true;
            lTransition_59176.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_59176.conditions.Length - 1; i >= 0; i--) { lTransition_59176.RemoveCondition(lTransition_59176.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_54980 = MotionControllerMotion.EditorFindTransition(lState_66050, lState_65326, 0);
            if (lTransition_54980 == null) { lTransition_54980 = lState_66050.AddTransition(lState_65326); }
            lTransition_54980.isExit = false;
            lTransition_54980.hasExitTime = true;
            lTransition_54980.hasFixedDuration = true;
            lTransition_54980.exitTime = 0.5643811f;
            lTransition_54980.duration = 0.15f;
            lTransition_54980.offset = 0f;
            lTransition_54980.mute = false;
            lTransition_54980.solo = false;
            lTransition_54980.canTransitionToSelf = true;
            lTransition_54980.orderedInterruption = true;
            lTransition_54980.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_54980.conditions.Length - 1; i >= 0; i--) { lTransition_54980.RemoveCondition(lTransition_54980.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_56976 = MotionControllerMotion.EditorFindTransition(lState_70048, lState_65326, 0);
            if (lTransition_56976 == null) { lTransition_56976 = lState_70048.AddTransition(lState_65326); }
            lTransition_56976.isExit = false;
            lTransition_56976.hasExitTime = true;
            lTransition_56976.hasFixedDuration = true;
            lTransition_56976.exitTime = 0.7016318f;
            lTransition_56976.duration = 0.15f;
            lTransition_56976.offset = 0f;
            lTransition_56976.mute = false;
            lTransition_56976.solo = false;
            lTransition_56976.canTransitionToSelf = true;
            lTransition_56976.orderedInterruption = true;
            lTransition_56976.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_56976.conditions.Length - 1; i >= 0; i--) { lTransition_56976.RemoveCondition(lTransition_56976.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_64230 = MotionControllerMotion.EditorFindTransition(lState_65954, lState_65326, 0);
            if (lTransition_64230 == null) { lTransition_64230 = lState_65954.AddTransition(lState_65326); }
            lTransition_64230.isExit = false;
            lTransition_64230.hasExitTime = true;
            lTransition_64230.hasFixedDuration = true;
            lTransition_64230.exitTime = 0.2468245f;
            lTransition_64230.duration = 0.15f;
            lTransition_64230.offset = 0f;
            lTransition_64230.mute = false;
            lTransition_64230.solo = false;
            lTransition_64230.canTransitionToSelf = true;
            lTransition_64230.orderedInterruption = true;
            lTransition_64230.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_64230.conditions.Length - 1; i >= 0; i--) { lTransition_64230.RemoveCondition(lTransition_64230.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_57232 = MotionControllerMotion.EditorFindTransition(lState_68904, lState_65326, 0);
            if (lTransition_57232 == null) { lTransition_57232 = lState_68904.AddTransition(lState_65326); }
            lTransition_57232.isExit = false;
            lTransition_57232.hasExitTime = true;
            lTransition_57232.hasFixedDuration = true;
            lTransition_57232.exitTime = 0.5180793f;
            lTransition_57232.duration = 0.15f;
            lTransition_57232.offset = 0f;
            lTransition_57232.mute = false;
            lTransition_57232.solo = false;
            lTransition_57232.canTransitionToSelf = true;
            lTransition_57232.orderedInterruption = true;
            lTransition_57232.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_57232.conditions.Length - 1; i >= 0; i--) { lTransition_57232.RemoveCondition(lTransition_57232.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_52202 = MotionControllerMotion.EditorFindTransition(lState_67204, lState_65326, 0);
            if (lTransition_52202 == null) { lTransition_52202 = lState_67204.AddTransition(lState_65326); }
            lTransition_52202.isExit = false;
            lTransition_52202.hasExitTime = true;
            lTransition_52202.hasFixedDuration = true;
            lTransition_52202.exitTime = 0.6774405f;
            lTransition_52202.duration = 0.15f;
            lTransition_52202.offset = 0f;
            lTransition_52202.mute = false;
            lTransition_52202.solo = false;
            lTransition_52202.canTransitionToSelf = true;
            lTransition_52202.orderedInterruption = true;
            lTransition_52202.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_52202.conditions.Length - 1; i >= 0; i--) { lTransition_52202.RemoveCondition(lTransition_52202.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_55860 = MotionControllerMotion.EditorFindTransition(lState_65326, lState_64316, 0);
            if (lTransition_55860 == null) { lTransition_55860 = lState_65326.AddTransition(lState_64316); }
            lTransition_55860.isExit = false;
            lTransition_55860.hasExitTime = false;
            lTransition_55860.hasFixedDuration = true;
            lTransition_55860.exitTime = 0f;
            lTransition_55860.duration = 0.1f;
            lTransition_55860.offset = 0f;
            lTransition_55860.mute = false;
            lTransition_55860.solo = false;
            lTransition_55860.canTransitionToSelf = true;
            lTransition_55860.orderedInterruption = true;
            lTransition_55860.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_55860.conditions.Length - 1; i >= 0; i--) { lTransition_55860.RemoveCondition(lTransition_55860.conditions[i]); }
            lTransition_55860.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.4f, "InputMagnitude");


            // Run any post processing after creating the state machine
            OnStateMachineCreated();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}
