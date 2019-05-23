using System;
using UnityEngine;
using com.ootii.Actors.Navigation;
using com.ootii.Messages;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Actors.AnimationControllers
{
    /// <summary>
    /// Idle that jump
    /// Adventure Camera orbits character
    /// </summary>
    [MotionName("Running Jump")]
    [MotionDescription("Jump when the actor is running forward.")]
    public class RunningJump : MotionControllerMotion
    {
        /// <summary>
        /// Trigger values for the motion
        /// </summary>
        public const int PHASE_UNKNOWN = 0;
        public const int PHASE_START = 27500;

        public const int PHASE_TOP = 27520;
        public const int PHASE_FALL = 27530;
        public const int PHASE_LAND_IDLE = 27540;
        public const int PHASE_LAND_RUN = 27545;

        /// <summary>
        /// Impulse to apply to the jump
        /// </summary>
        public float _Impulse = 7f;
        public float Impulse
        {
            get { return _Impulse; }
            set { _Impulse = value; }
        }

        /// <summary>
        /// The physics jump creates a parabola that is typically based on the
        /// feet. However, if the animation has the feet move... that could be an issue.
        /// </summary>
        public bool _ConvertToHipBase = true;
        public bool ConvertToHipBase
        {
            get { return _ConvertToHipBase; }
            set { _ConvertToHipBase = value; }
        }

        /// <summary>
        /// Allows us to assign a hip bone for adjusting the jump height off of
        /// the foot position
        /// </summary>
        public string _HipBoneName = "";
        public string HipBoneName
        {
            get { return _HipBoneName; }

            set
            {
                _HipBoneName = value;
                if (mMotionController != null)
                {
                    mHipBone = mMotionController.gameObject.transform.Find(_HipBoneName);
                }
            }
        }

        /// <summary>
        /// Minimum distance before a jump turns into a fall
        /// </summary>
        public float _MinFallHeight = 2f;
        public float MinFallHeight
        {
            get { return _MinFallHeight; }
            set { _MinFallHeight = value; }
        }

        /// <summary>
        /// Determines if the impulse has been applied or not
        /// </summary>
        protected bool mIsImpulseApplied = false;

        /// <summary>
        /// Velocity at the time the character launches. This helps us with momenumt
        /// </summary>
        protected Vector3 mLaunchVelocity = Vector3.zero;

        /// <summary>
        /// Transform for the hip to help adjust the height
        /// </summary>
        protected Transform mHipBone = null;

        /// <summary>
        /// Distance between the base and the hips
        /// </summary>
        protected float mLastHipDistance = 0f;

        /// <summary>
        /// Connect to the move motion if we can
        /// </summary>
        protected IWalkRunMotion mWalkRunMotion = null;
        //protected WalkRunPivot mWalkRunPivot = null;
        //protected WalkRunPivot_v2 mWalkRunPivot_v2 = null;
        //protected WalkRunStrafe mWalkRunStrafe = null;
        //protected WalkRunStrafe_v2 mWalkRunStrafe_v2 = null;
        //protected WalkRunRotate mWalkRunRotate = null;
        //protected WalkRunRotate_v2 mWalkRunRotate_v2 = null;

        /// <summary>
        /// Grab a fall motion incase we need to transition to it
        /// </summary>
        protected MotionControllerMotion mFall = null;

        /// <summary>
        /// Determines if the exit is triggered
        /// </summary>
        protected bool mIsExitTriggered = false;

        /// <summary>
        /// Default constructor
        /// </summary>
        public RunningJump()
            : base()
        {
            _Priority = 16;
            _ActionAlias = "Jump";
            mIsStartable = true;
            //mIsGroundedExpected = false;

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "RunningJump-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public RunningJump(MotionController rController)
            : base(rController)
        {
            _Priority = 16;
            _ActionAlias = "Jump";
            mIsStartable = true;
            //mIsGroundedExpected = false;

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "RunningJump-SM"; }
#endif
        }

        /// <summary>
        /// Initialize is called after all the motions have been initialized. This allow us time to
        /// create references before the motions start working
        /// </summary>
        public override void Initialize()
        {
            if (mMotionController != null)
            {
                if (mWalkRunMotion == null) { mWalkRunMotion = mMotionController.GetMotionInterface<IWalkRunMotion>(); }
                //if (mWalkRunPivot == null) { mWalkRunPivot = mMotionController.GetMotion<WalkRunPivot>(); }
                //if (mWalkRunPivot_v2 == null) { mWalkRunPivot_v2 = mMotionController.GetMotion<WalkRunPivot_v2>(); }
                //if (mWalkRunStrafe == null) { mWalkRunStrafe = mMotionController.GetMotion<WalkRunStrafe>(); }
                //if (mWalkRunStrafe_v2 == null) { mWalkRunStrafe_v2 = mMotionController.GetMotion<WalkRunStrafe_v2>(); }
                //if (mWalkRunRotate == null) { mWalkRunRotate = mMotionController.GetMotion<WalkRunRotate>(); }
                //if (mWalkRunRotate_v2 == null) { mWalkRunRotate_v2 = mMotionController.GetMotion<WalkRunRotate_v2>(); }

                mFall = mMotionController.GetMotion("Fall");
                if (mFall == null) { mFall = mMotionController.GetMotion<Fall>(); }
            }
        }

        /// <summary>
        /// Tests if this motion should be started. However, the motion
        /// isn't actually started.
        /// </summary>
        /// <returns></returns>
        public override bool TestActivate()
        {
            // If we're not startable, this is easy
            if (!mIsStartable)
            {
                return false;
            }

            // If we're not grounded, this is easy
            if (!mActorController.IsGrounded)
            {
                return false;
            }

            // Ensure we have input to test
            if (mMotionController._InputSource == null)
            {
                return false;
            }

            // If we're not wanting to jump, this is easy
            if (!mMotionController._InputSource.IsJustPressed(_ActionAlias))
            {
                return false;
            }

            // Ensure we're in a valid starting motion
            if (mMotionLayer.ActiveMotion != null)
            {
                IWalkRunMotion lWalkRunMotion = mMotionLayer.ActiveMotion as IWalkRunMotion;
                if (!(mMotionLayer.ActiveMotion is BalanceWalk) && (lWalkRunMotion == null || !lWalkRunMotion.IsRunActive))
                {
                    return false;
                }

                // Test if we're actually running
                mWalkRunMotion = lWalkRunMotion;
            }

            // We need to be running "forward" for this jump
            if (mMotionController.State.InputForward.magnitude < 0.5f || Mathf.Abs(mMotionController.State.InputFromAvatarAngle) > 10f)
            {
                return false;
            }

            // The motion may not be active, but the animator may not have moved
            // out of the IdlePose yet. Wait for it to transition out before we allow 
            // another jump.
            int lStateID = mMotionController.State.AnimatorStates[mMotionLayer.AnimatorLayerIndex].StateInfo.fullPathHash;
            if (lStateID == STATE_IdlePose)
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
            // If we just entered this frame, stay
            if (mIsActivatedFrame) { return true; }

            // If we're in the idle state with no movement, stop
            MotionState lState = mMotionController.State;
            int lStateID = lState.AnimatorStates[mMotionLayer.AnimatorLayerIndex].StateInfo.fullPathHash;

            // If we're in the idle pose, we're done
            if (lStateID == STATE_IdlePose)
            {
                return false;
            }
            // If we've launched, make sure we're in one of our states
            else if (mIsImpulseApplied && !IsMotionState(lStateID))
            {
                return false;
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
            if (rMotion is Fall)
            {
                if (mActorController.State.GroundSurfaceDistance < 2f)
                {
                    return false;
                }
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
            // Attempt to find the hip bone if we have a name
            if (_ConvertToHipBase)
            {
                if (mHipBone == null)
                {
                    if (_HipBoneName.Length > 0)
                    {
                        mHipBone = mMotionController._Transform.Find(_HipBoneName);
                    }

                    if (mHipBone == null)
                    {
                        mHipBone = mMotionController.Animator.GetBoneTransform(HumanBodyBones.Hips);
                        if (mHipBone != null) { _HipBoneName = mHipBone.name; }
                    }
                }
            }

            // Reset the distance flag for this jump
            mLastHipDistance = 0f;

            // Reset the impulse flag
            mIsImpulseApplied = false;
            mIsExitTriggered = false;

            // Grab the current velocities
            mLaunchVelocity = mActorController.State.Velocity;

            // Control whether we're walking or running
            mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, PHASE_START, true);

            // Flag this motion as active
            return base.Activate(rPrevMotion);
        }

        /// <summary>
        /// Raised when we shut the motion down
        /// </summary>
        public override void Deactivate()
        {
            // Continue with the deactivation
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
        /// <returns></returns>
        public override void UpdateRootMotion(float rDeltaTime, int rUpdateIndex, ref Vector3 rVelocityDelta, ref Quaternion rRotationDelta)
        {
            // Remove all velocity and rotation since we'll be using our physics to jump
            //rVelocityDelta = Vector3.zero;
            //rRotationDelta = Quaternion.identity;

            if (mMotionLayer._AnimatorTransitionID == TRANS_RunningJump_RunJump_RunForward ||
                mMotionLayer._AnimatorStateID == STATE_RunJump_RunForward)
            {
                rVelocityDelta = rVelocityDelta.normalized * (mLaunchVelocity.magnitude * Time.deltaTime); // (Time.smoothDeltaTime > 0f ? Time.smoothDeltaTime : Time.deltaTime));
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
            mVelocity = Vector3.zero;
            mMovement = Vector3.zero;
            float lHipDistanceDelta = 0f;

            // Since we're not doing any lerping or physics based stuff here,
            // we'll only process once per cyle even if we're running slow.
            if (rUpdateIndex != 1) { return; }

            // If we have a hip bone, we'll adjust the jump based on the distance
            // that changes between the foot and the hips. This way, the jump is
            // "hip based" and not "foot based".
            if (_ConvertToHipBase && mHipBone != null)
            {
                float lHipDistance = mHipBone.position.y - mMotionController._Transform.position.y;

                // As the distance gets smaller, we increase the shift
                lHipDistanceDelta = -(lHipDistance - mLastHipDistance);
                mLastHipDistance = lHipDistance;
            }

            // Determine how gravity is being applied
            //Vector3 lWorldGravity = (mActorController._Gravity.sqrMagnitude == 0f ? UnityEngine.Physics.gravity : mActorController._Gravity);
            //if (mActorController._IsGravityRelative) { lWorldGravity = mActorController._Transform.rotation * lWorldGravity; }

            //Vector3 lGravityNormal = lWorldGravity.normalized;

            // Determine our velocities
            //mVelocity = mLaunchVelocity;

            //Vector3 lVelocity = mActorController.State.MovementForceAdjust / rDeltaTime;
            //Vector3 lVerticalVelocity = Vector3.Project(lVelocity, mActorController._Transform.up);

            // Grab the state info
            MotionState lState = mMotionController.State;
            //int lStateMotionPhase = lState.AnimatorStates[mMotionLayer._AnimatorLayerIndex].MotionPhase;
            //int lStateMotionParameter = lState.AnimatorStates[mMotionLayer._AnimatorLayerIndex].MotionParameter;

            //AnimatorStateInfo lStateInfo = lState.AnimatorStates[mMotionLayer._AnimatorLayerIndex].StateInfo;
            int lStateID = mMotionLayer._AnimatorStateID;
            float lStateTime = mMotionLayer._AnimatorStateNormalizedTime;

            //AnimatorTransitionInfo lTransitionInfo = lState.AnimatorStates[mMotionLayer._AnimatorLayerIndex].TransitionInfo;
            //int lTransitionID = lTransitionInfo.fullPathHash;

            // On launch, add the impulse
            if (lStateID == STATE_RunningJump)
            {
                // If we haven't applied the impulse, do it now. 
                if (!mIsImpulseApplied)
                {
                    mIsImpulseApplied = true;
                    mActorController.AddImpulse(mActorController._Transform.up * _Impulse);
                }

                // If we're pushing into the ground, end the running jump
                if (!mIsExitTriggered && mIsImpulseApplied && lStateTime > 0.2f && lStateTime < 0.5f)
                {
                    if (mActorController.State.IsGrounded)
                    {
                        mIsExitTriggered = true;
                        mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_LAND_IDLE, true);
                    }
                }

                // As we come to the end, we have a couple of options
                if (!mIsExitTriggered && lStateTime > 0.83f)
                {
                    // If we're a long way from the ground, transition to a fall
                    if (mFall != null && mFall.IsEnabled && mActorController.State.GroundSurfaceDistance > _MinFallHeight)
                    {
                        mIsExitTriggered = true;
                        mMotionController.ActivateMotion(mFall);
                    }
                    // If we're still getting input, keep running
                    else if (lState.InputMagnitudeTrend.Value >= 0.1f) // && Mathf.Abs(lState.InputFromAvatarAngle) < 10f)
                    {
                        mIsExitTriggered = true;
                        mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_LAND_RUN, true);
                    }
                    // Come to a quick stop
                    else
                    {
                        mIsExitTriggered = true;
                        mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_LAND_IDLE, true);
                    }
                }
                // While in the jump, adjust the displacement
                else
                {
                    mMovement = mActorController._Transform.up * lHipDistanceDelta;
                }
            }
            // Once we get into the run forward, we can transition to the true run
            else if (lStateID == STATE_RunJump_RunForward)
            {
                if (lStateTime > 0.0f)
                {
                    // It may be time to move into the walk/run
                    if (mWalkRunMotion != null && mWalkRunMotion.IsEnabled)
                    {
                        mWalkRunMotion.StartInRun = mWalkRunMotion.IsRunActive;
                        mWalkRunMotion.StartInWalk = !mWalkRunMotion.StartInRun;
                        mMotionController.ActivateMotion(mWalkRunMotion as MotionControllerMotion);
                    }
                    //if (mWalkRunPivot != null && mWalkRunPivot.IsEnabled)
                    //{
                    //    mWalkRunPivot.StartInRun = mWalkRunPivot.IsRunActive;
                    //    mWalkRunPivot.StartInWalk = !mWalkRunPivot.StartInRun;
                    //    mMotionController.ActivateMotion(mWalkRunPivot);
                    //}
                    //else if (mWalkRunPivot_v2 != null && mWalkRunPivot_v2.IsEnabled)
                    //{
                    //    mWalkRunPivot_v2.StartInRun = mWalkRunPivot_v2.IsRunActive;
                    //    mWalkRunPivot_v2.StartInWalk = !mWalkRunPivot_v2.StartInRun;
                    //    mMotionController.ActivateMotion(mWalkRunPivot_v2);
                    //}
                    //else if (mWalkRunStrafe != null && mWalkRunStrafe.IsEnabled)
                    //{
                    //    mWalkRunStrafe.StartInRun = mWalkRunStrafe.IsRunActive;
                    //    mWalkRunStrafe.StartInWalk = !mWalkRunStrafe.StartInRun;
                    //    mMotionController.ActivateMotion(mWalkRunStrafe);
                    //}
                    //else if (mWalkRunStrafe_v2 != null && mWalkRunStrafe_v2.IsEnabled)
                    //{
                    //    mWalkRunStrafe_v2.StartInRun = mWalkRunStrafe_v2.IsRunActive;
                    //    mWalkRunStrafe_v2.StartInWalk = !mWalkRunStrafe_v2.StartInRun;
                    //    mMotionController.ActivateMotion(mWalkRunStrafe_v2);
                    //}
                    //else if (mWalkRunRotate != null && mWalkRunRotate.IsEnabled)
                    //{
                    //    mWalkRunRotate.StartInRun = mWalkRunRotate.IsRunActive;
                    //    mWalkRunRotate.StartInWalk = !mWalkRunRotate.StartInRun;
                    //    mMotionController.ActivateMotion(mWalkRunRotate);
                    //}
                    //else if (mWalkRunRotate_v2 != null && mWalkRunRotate_v2.IsEnabled)
                    //{
                    //    mWalkRunRotate_v2.StartInRun = mWalkRunRotate_v2.IsRunActive;
                    //    mWalkRunRotate_v2.StartInWalk = !mWalkRunRotate_v2.StartInRun;
                    //    mMotionController.ActivateMotion(mWalkRunRotate_v2);
                    //}
                    else
                    {
                        Deactivate();
                    }
                }
            }
            // Once we get to the idle pose, we can deactivate
            else if (lStateID == STATE_IdlePose)
            {
                Deactivate();
            }

            //Log.FileWrite("Adv_J.Update(" + rDeltaTime.ToString("f4") + ", " + rUpdateIndex + ") isAct:" + mIsActive + " " + mMotionController.AnimatorStateNames[lStateID] + " %:" + lStateInfo.normalizedTime.ToString("0.000") + " mm:" + mMovement.y.ToString("f4") + " mv:" + mVelocity.y.ToString("f4") + " pv:" + lProjectedVelocity.y.ToString("f4"));
        }

        /// <summary>
        /// Raised by the controller when a message is received
        /// </summary>
        public override void OnMessageReceived(IMessage rMessage)
        {
            if (rMessage == null) { return; }

            NavigationMessage lMessage = rMessage as NavigationMessage;
            if (lMessage != null)
            {
                // Call for a climb test
                if (rMessage.ID == NavigationMessage.MSG_NAVIGATE_JUMP)
                {
                    if (!mIsActive && mMotionController.IsGrounded)
                    {
                        if (mActorController.State.Velocity.magnitude >= 5f)
                        {
                            rMessage.Recipient = this;
                            rMessage.IsHandled = true;

                            mMotionController.ActivateMotion(this);
                        }
                    }
                }
            }
        }

#if UNITY_EDITOR

        /// <summary>
        /// Allow the motion to render it's own GUI
        /// </summary>
        public override bool OnInspectorGUI()
        {
            bool lIsDirty = false;

            string lNewActionAlias = EditorGUILayout.TextField(new GUIContent("Action Alias", "Action alias that triggers a climb."), ActionAlias, GUILayout.MinWidth(30));
            if (lNewActionAlias != ActionAlias)
            {
                lIsDirty = true;
                ActionAlias = lNewActionAlias;
            }

            bool lNewConvertToHipBase = EditorGUILayout.Toggle(new GUIContent("Convert To Hip Base", "Determines if we apply the physics to the hip bone vs. feet."), ConvertToHipBase);
            if (lNewConvertToHipBase != ConvertToHipBase)
            {
                lIsDirty = true;
                ConvertToHipBase = lNewConvertToHipBase;
            }

            string lNewHipBoneName = EditorGUILayout.TextField(new GUIContent("Hip Bone", "Name of the hip bone for adjusting the jump root."), HipBoneName);
            if (lNewHipBoneName != HipBoneName)
            {
                lIsDirty = true;
                HipBoneName = lNewHipBoneName;
            }

            float lNewImpulse = EditorGUILayout.FloatField(new GUIContent("Impulse", "Strength of the jump as an instant force."), Impulse);
            if (lNewImpulse != Impulse)
            {
                lIsDirty = true;
                Impulse = lNewImpulse;
            }

            float lNewFallDistance = EditorGUILayout.FloatField(new GUIContent("Min Fall Height", "Minimum distance before the jump turns into a fall."), MinFallHeight);
            if (lNewFallDistance != MinFallHeight)
            {
                lIsDirty = true;
                MinFallHeight = lNewFallDistance;
            }

            return lIsDirty;
        }

#endif

        #region Auto-Generated
        // ************************************ START AUTO GENERATED ************************************

        /// <summary>
        /// These declarations go inside the class so you can test for which state
        /// and transitions are active. Testing hash values is much faster than strings.
        /// </summary>
        public static int STATE_Start = -1;
        public static int STATE_IdlePose = -1;
        public static int STATE_RunJump_RunForward = -1;
        public static int STATE_RunningJump = -1;
        public static int STATE_LandToIdle = -1;
        public static int TRANS_AnyState_RunningJump = -1;
        public static int TRANS_EntryState_RunningJump = -1;
        public static int TRANS_RunningJump_RunJump_RunForward = -1;
        public static int TRANS_RunningJump_LandToIdle = -1;
        public static int TRANS_LandToIdle_IdlePose = -1;

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
                    if (lStateID == STATE_IdlePose) { return true; }
                    if (lStateID == STATE_RunJump_RunForward) { return true; }
                    if (lStateID == STATE_RunningJump) { return true; }
                    if (lStateID == STATE_LandToIdle) { return true; }
                }

                if (lTransitionID == TRANS_AnyState_RunningJump) { return true; }
                if (lTransitionID == TRANS_EntryState_RunningJump) { return true; }
                if (lTransitionID == TRANS_RunningJump_RunJump_RunForward) { return true; }
                if (lTransitionID == TRANS_RunningJump_LandToIdle) { return true; }
                if (lTransitionID == TRANS_LandToIdle_IdlePose) { return true; }
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
            if (rStateID == STATE_IdlePose) { return true; }
            if (rStateID == STATE_RunJump_RunForward) { return true; }
            if (rStateID == STATE_RunningJump) { return true; }
            if (rStateID == STATE_LandToIdle) { return true; }
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
                if (rStateID == STATE_IdlePose) { return true; }
                if (rStateID == STATE_RunJump_RunForward) { return true; }
                if (rStateID == STATE_RunningJump) { return true; }
                if (rStateID == STATE_LandToIdle) { return true; }
            }

            if (rTransitionID == TRANS_AnyState_RunningJump) { return true; }
            if (rTransitionID == TRANS_EntryState_RunningJump) { return true; }
            if (rTransitionID == TRANS_RunningJump_RunJump_RunForward) { return true; }
            if (rTransitionID == TRANS_RunningJump_LandToIdle) { return true; }
            if (rTransitionID == TRANS_LandToIdle_IdlePose) { return true; }
            return false;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            TRANS_AnyState_RunningJump = mMotionController.AddAnimatorName("AnyState -> Base Layer.RunningJump-SM.RunningJump");
            TRANS_EntryState_RunningJump = mMotionController.AddAnimatorName("Entry -> Base Layer.RunningJump-SM.RunningJump");
            STATE_Start = mMotionController.AddAnimatorName("Base Layer.Start");
            STATE_IdlePose = mMotionController.AddAnimatorName("Base Layer.RunningJump-SM.IdlePose");
            STATE_RunJump_RunForward = mMotionController.AddAnimatorName("Base Layer.RunningJump-SM.RunJump_RunForward");
            STATE_RunningJump = mMotionController.AddAnimatorName("Base Layer.RunningJump-SM.RunningJump");
            TRANS_RunningJump_RunJump_RunForward = mMotionController.AddAnimatorName("Base Layer.RunningJump-SM.RunningJump -> Base Layer.RunningJump-SM.RunJump_RunForward");
            TRANS_RunningJump_LandToIdle = mMotionController.AddAnimatorName("Base Layer.RunningJump-SM.RunningJump -> Base Layer.RunningJump-SM.LandToIdle");
            STATE_LandToIdle = mMotionController.AddAnimatorName("Base Layer.RunningJump-SM.LandToIdle");
            TRANS_LandToIdle_IdlePose = mMotionController.AddAnimatorName("Base Layer.RunningJump-SM.LandToIdle -> Base Layer.RunningJump-SM.IdlePose");
        }

#if UNITY_EDITOR

        private AnimationClip m17118 = null;
        private AnimationClip m17050 = null;
        private AnimationClip m22024 = null;
        private AnimationClip m20978 = null;

        /// <summary>
        /// Creates the animator substate machine for this motion.
        /// </summary>
        protected override void CreateStateMachine()
        {
            // Grab the root sm for the layer
            UnityEditor.Animations.AnimatorStateMachine lRootStateMachine = _EditorAnimatorController.layers[mMotionLayer.AnimatorLayerIndex].stateMachine;
            UnityEditor.Animations.AnimatorStateMachine lSM_32610 = _EditorAnimatorController.layers[mMotionLayer.AnimatorLayerIndex].stateMachine;
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

            UnityEditor.Animations.AnimatorStateMachine lSM_33036 = lRootSubStateMachine;
            if (lSM_33036 != null)
            {
                for (int i = lSM_33036.entryTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_33036.RemoveEntryTransition(lSM_33036.entryTransitions[i]);
                }

                for (int i = lSM_33036.anyStateTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_33036.RemoveAnyStateTransition(lSM_33036.anyStateTransitions[i]);
                }

                for (int i = lSM_33036.states.Length - 1; i >= 0; i--)
                {
                    lSM_33036.RemoveState(lSM_33036.states[i].state);
                }

                for (int i = lSM_33036.stateMachines.Length - 1; i >= 0; i--)
                {
                    lSM_33036.RemoveStateMachine(lSM_33036.stateMachines[i].stateMachine);
                }
            }
            else
            {
                lSM_33036 = lSM_32610.AddStateMachine(_EditorAnimatorSMName, new Vector3(408, -420, 0));
            }

            UnityEditor.Animations.AnimatorState lS_34708 = lSM_33036.AddState("IdlePose", new Vector3(840, 204, 0));
            lS_34708.speed = 1f;
            lS_34708.motion = m17118;

            UnityEditor.Animations.AnimatorState lS_34710 = lSM_33036.AddState("RunJump_RunForward", new Vector3(588, 288, 0));
            lS_34710.speed = 1f;
            lS_34710.motion = m17050;

            UnityEditor.Animations.AnimatorState lS_34426 = lSM_33036.AddState("RunningJump", new Vector3(324, 204, 0));
            lS_34426.speed = 1f;
            lS_34426.motion = m22024;

            UnityEditor.Animations.AnimatorState lS_34712 = lSM_33036.AddState("LandToIdle", new Vector3(588, 204, 0));
            lS_34712.speed = 1f;
            lS_34712.motion = m20978;

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_33112 = lRootStateMachine.AddAnyStateTransition(lS_34426);
            lT_33112.hasExitTime = false;
            lT_33112.hasFixedDuration = true;
            lT_33112.exitTime = 0.9f;
            lT_33112.duration = 0.05f;
            lT_33112.offset = 0f;
            lT_33112.mute = false;
            lT_33112.solo = false;
            lT_33112.canTransitionToSelf = true;
            lT_33112.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_33112.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27500f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_34716 = lS_34426.AddTransition(lS_34710);
            lT_34716.hasExitTime = false;
            lT_34716.hasFixedDuration = true;
            lT_34716.exitTime = 0.8318414f;
            lT_34716.duration = 0.1f;
            lT_34716.offset = 0.8475341f;
            lT_34716.mute = false;
            lT_34716.solo = false;
            lT_34716.canTransitionToSelf = true;
            lT_34716.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_34716.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27545f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_34718 = lS_34426.AddTransition(lS_34712);
            lT_34718.hasExitTime = false;
            lT_34718.hasFixedDuration = true;
            lT_34718.exitTime = 0.8032071f;
            lT_34718.duration = 0.1951104f;
            lT_34718.offset = 0f;
            lT_34718.mute = false;
            lT_34718.solo = false;
            lT_34718.canTransitionToSelf = true;
            lT_34718.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_34718.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 27540f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_34720 = lS_34712.AddTransition(lS_34708);
            lT_34720.hasExitTime = true;
            lT_34720.hasFixedDuration = true;
            lT_34720.exitTime = 0.6590909f;
            lT_34720.duration = 0.25f;
            lT_34720.offset = 0f;
            lT_34720.mute = false;
            lT_34720.solo = false;
            lT_34720.canTransitionToSelf = true;
            lT_34720.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

        }

        /// <summary>
        /// Gathers the animations so we can use them when creating the sub-state machine.
        /// </summary>
        public override void FindAnimations()
        {
            m17118 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdlePose.anim", "IdlePose");
            m17050 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunForward_v2.fbx/RunForward.anim", "RunForward");
            m22024 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Jumping/ootii_RunningJump.fbx/RunningJump.anim", "RunningJump");
            m20978 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Jumping/ootii_Jump.fbx/LandToIdle.anim", "LandToIdle");

            // Add the remaining functionality
            base.FindAnimations();
        }

        /// <summary>
        /// Used to show the settings that allow us to generate the animator setup.
        /// </summary>
        public override void OnSettingsGUI()
        {
            UnityEditor.EditorGUILayout.IntField(new GUIContent("Phase ID", "Phase ID used to transition to the state."), PHASE_START);
            m17118 = CreateAnimationField("Start.IdlePose", "Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdlePose.anim", "IdlePose", m17118);
            m17050 = CreateAnimationField("RunJump_RunForward.RunForward", "Assets/ootii/MotionController/Content/Animations/Humanoid/Running/RunForward_v2.fbx/RunForward.anim", "RunForward", m17050);
            m22024 = CreateAnimationField("RunningJump", "Assets/ootii/MotionController/Content/Animations/Humanoid/Jumping/ootii_RunningJump.fbx/RunningJump.anim", "RunningJump", m22024);
            m20978 = CreateAnimationField("LandToIdle", "Assets/ootii/MotionController/Content/Animations/Humanoid/Jumping/ootii_Jump.fbx/LandToIdle.anim", "LandToIdle", m20978);

            // Add the remaining functionality
            base.OnSettingsGUI();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}
