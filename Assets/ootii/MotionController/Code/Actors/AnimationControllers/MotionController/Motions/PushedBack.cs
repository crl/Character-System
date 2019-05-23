using UnityEngine;
using com.ootii.Helpers;
using com.ootii.Messages;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Actors.AnimationControllers
{
    /// <summary>
    /// This motion is a simple pose used to push the character back or forwards.
    /// </summary>
    [MotionName("Pushed Back")]
    [MotionDescription("This motion is a simple pose used to push the character back or forwards.")]
    public class PushedBack : MotionControllerMotion
    {
        // Enum values for the motion
        public const int PHASE_UNKNOWN = 0;
        public const int PHASE_START = 1830;
        public const int PHASE_END = 1835;

        /// <summary>
        /// Velocity that defines the speed and direction of the push
        /// </summary>
        public Vector3 _PushVelocity = new Vector3(0f, 0f, 0f);
        public Vector3 PushVelocity
        {
            get { return _PushVelocity; }
            set { _PushVelocity = value; }
        }

        /// <summary>
        /// Amount we'll slow the push velocity by each fixed updated
        /// </summary>
        public float _DragFactor = 0.05f;
        public float DragFactor
        {
            get { return _DragFactor; }
            set { _DragFactor = value; }
        }

        /// <summary>
        /// Maximum amount of time to be pushed back
        /// </summary>
        public float _MaxAge = 3f;
        public float MaxAge
        {
            get { return _MaxAge; }
            set { _MaxAge = value; }
        }

        /// <summary>
        /// Determines if we'll disable the AC's UseTransform property temporarily
        /// </summary>
        public bool _DisableUseTransform = true;
        public bool DisableUseTransform
        {
            get { return _DisableUseTransform; }
            set { _DisableUseTransform = value; }
        }

        // Determines if the motion has expired
        protected bool mHasExpired = false;

        // Determines if we actually have a push velocity
        protected bool mHasPushVelocity = false;

        // Velocity used to push the actor
        protected Vector3 mPushVelocity = Vector3.zero;

        // Determines if the actor was using transform movement
        protected bool mStoredUseTransform = false;

        /// <summary>
        /// Default constructor
        /// </summary>
        public PushedBack()
            : base()
        {
            _Priority = 20;
            _ActionAlias = "";
            _Category = EnumMotionCategories.IMPACT;

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "Utilities-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public PushedBack(MotionController rController)
            : base(rController)
        {
            _Priority = 20;
            _ActionAlias = "";
            _Category = EnumMotionCategories.IMPACT;

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "Utilities-SM"; }
#endif
        }

        /// <summary>
        /// Tests if this motion should be started. However, the motion isn't actually started.
        /// </summary>
        /// <returns></returns>
        public override bool TestActivate()
        {
            if (!mIsStartable) { return false; }

            // Test if we're supposed to activate the motion
            if (_ActionAlias.Length > 0 && mMotionController._InputSource != null)
            {
                if (mMotionController._InputSource.IsJustPressed(_ActionAlias))
                {
                    mPushVelocity = _PushVelocity;
                    return true;
                }
            }

            // Get out
            return false;
        }

        /// <summary>
        /// Tests if the motion should continue. If it shouldn't, the motion is typically disabled
        /// </summary>
        /// <returns>Boolean that determines if the motion continues</returns>
        public override bool TestUpdate()
        {
            if (mIsActivatedFrame) { return true; }

            // End when we're almost done recovering
            if (mMotionLayer._AnimatorStateID == STATE_IdlePose)
            {
                return false;
            }
            else if (mMotionLayer._AnimatorStateID == STATE_PushedBack_Recover && 
                mMotionLayer._AnimatorTransitionID == 0 && 
                mMotionLayer._AnimatorStateNormalizedTime > 0.9f)
            {
                return false;
            }

            // Default to continue
            return true;
        }

        /// <summary>
        /// Called to start the specific motion. If the motion were something like 'jump', this would start the jumping process
        /// </summary>
        /// <param name="rPrevMotion">Motion that this motion is taking over from</param>
        public override bool Activate(MotionControllerMotion rPrevMotion)
        {
            mHasExpired = false;

            if (mPushVelocity.sqrMagnitude < 0.0001f) { mPushVelocity = PushVelocity; }
            mHasPushVelocity = (mPushVelocity.sqrMagnitude > 0.0001f);

            if (DisableUseTransform)
            {
                mStoredUseTransform = mActorController.UseTransformPosition;
                mActorController.UseTransformPosition = false;
            }

            mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, PHASE_START, true);
            return base.Activate(rPrevMotion);
        }

        /// <summary>
        /// Called to stop the motion. If the motion is stopable. Some motions like jump cannot be stopped early
        /// </summary>
        public override void Deactivate()
        {
            mPushVelocity = Vector3.zero;

            if (DisableUseTransform)
            {
                mActorController.UseTransformPosition = mStoredUseTransform;
            }

            base.Deactivate();
        }

        /// <summary>
        /// Internal fixed update called by the motion layer. 
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        public override void FixedUpdate(float rDeltaTime)
        {
            mPushVelocity = mPushVelocity * (1f - _DragFactor);
        }

        /// <summary>
        /// Allows the motion to modify the root-motion velocities before they are applied. 
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        /// <param name="rVelocityDelta">Root-motion linear velocity relative to the actor's forward</param>
        /// <param name="rRotationDelta">Root-motion rotational velocity</param>
        /// <returns></returns>
        public override void UpdateRootMotion(float rDeltaTime, int rUpdateIndex, ref Vector3 rVelocityDelta, ref Quaternion rRotationDelta)
        {
            rVelocityDelta = Vector3.zero;
            rRotationDelta = Quaternion.identity;
        }

        /// <summary>
        /// Updates the motion over time. This is called by the controller every update cycle so animations and stages can be updated.
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        public override void Update(float rDeltaTime, int rUpdateIndex)
        {
            mMovement = Vector3.zero;

            // Determine if we've expired or there is no movement left
            if (!mHasExpired)
            {
                if ((_MaxAge > 0f && mAge > _MaxAge) || 
                    (mHasPushVelocity && mPushVelocity.sqrMagnitude < 0.5f) ||
                    (mActorController.State.Velocity.magnitude <= 0.5f))
                {
                    if (mMotionLayer._AnimatorStateID == STATE_PushedBack_Loop)
                    {
                        mHasExpired = true;
                        mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, PHASE_END, false);
                    }
                }
            }

            // Move the character
            mMovement = mPushVelocity * rDeltaTime;
        }

        /// <summary>
        /// Raised by the controller when a message is received
        /// </summary>
        public override void OnMessageReceived(IMessage rMessage)
        {
            if (rMessage == null || rMessage.IsHandled) { return; }
            if (mActorController.State.Stance == EnumControllerStance.UNCONCIOUS) { return; }

            // Activate if we're being pushed back
            if (rMessage is Navigation.NavigationMessage)
            {
                Navigation.NavigationMessage lMessage = rMessage as Navigation.NavigationMessage;
                if (lMessage != null && lMessage.ID == Navigation.NavigationMessage.MSG_NAVIGATE_PUSHED_BACK)
                { 
                    if (!mIsActive)
                    {
                        if (rMessage.Data is Vector3)
                        {
                            mPushVelocity = (Vector3)rMessage.Data;
                        }

                        mMotionController.ActivateMotion(this);

                        rMessage.IsHandled = true;
                        rMessage.Recipient = this;
                    }
                }
            }
            // Cancel if we get that message
            else if (rMessage is MotionMessage)
            {
                if (mIsActive)
                {
                if (rMessage.ID == MotionMessage.MSG_MOTION_CONTINUE || rMessage.ID == MotionMessage.MSG_MOTION_DEACTIVATE)
                {
                    mHasExpired = true;
                    mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, PHASE_END, true);

                    rMessage.IsHandled = true;
                    rMessage.Recipient = this;
                    }
                }
            }
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

            if (EditorHelper.TextField("Action Alias", "Action alias that starts the action and then exits the action (mostly for debugging).", ActionAlias, mMotionController))
            {
                lIsDirty = true;
                ActionAlias = EditorHelper.FieldStringValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.FloatField("Duration", "Time (in seconds) that the push back should occur for.", MaxAge, mMotionController))
            {
                lIsDirty = true;
                MaxAge = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.BoolField("Disable Use Transform", "Determines if we temporarily disable the AC's 'Use Transform' property so the character will be pushed back.", DisableUseTransform, mMotionController))
            {
                lIsDirty = true;
                DisableUseTransform = EditorHelper.FieldBoolValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.Vector3Field("Velocity", "Speed and direction of the movement caused by the push.", PushVelocity, mMotionController))
            {
                lIsDirty = true;
                PushVelocity = EditorHelper.FieldVector3Value;
            }

            if (EditorHelper.FloatField("Drag Factor", "Velocity multiplier we'll apply each frame to reduce the velocity.", DragFactor, mMotionController))
            {
                lIsDirty = true;
                DragFactor = Mathf.Clamp01(EditorHelper.FieldFloatValue);
            }

            return lIsDirty;
        }

#endif

        /// <summary>
        /// Determines if we're using auto-generated code
        /// </summary>
        public override bool HasAutoGeneratedCode
        {
            // Since we have several different types of motions here. We don't
            // want to rely on the IsInMotion functions
            get { return false; }
        }

        #region Auto-Generated
        // ************************************ START AUTO GENERATED ************************************

        /// <summary>
        /// These declarations go inside the class so you can test for which state
        /// and transitions are active. Testing hash values is much faster than strings.
        /// </summary>
        public static int STATE_Start = -1;
        public static int STATE_Idle_PushButton = -1;
        public static int STATE_IdlePose = -1;
        public static int STATE_Idle_PickUp = -1;
        public static int STATE_Sleeping = -1;
        public static int STATE_GettingUp = -1;
        public static int STATE_LayingDown = -1;
        public static int STATE_Death_180 = -1;
        public static int STATE_Death_0 = -1;
        public static int STATE_Damaged_0 = -1;
        public static int STATE_Stunned = -1;
        public static int STATE_Cower = -1;
        public static int STATE_CowerOut = -1;
        public static int STATE_KnockedDown = -1;
        public static int STATE_GettingUpBackward = -1;
        public static int STATE_DeathPose = -1;
        public static int STATE_Frozen = -1;
        public static int STATE_PushedBack_Pose = -1;
        public static int STATE_PushedBack_Recover = -1;
        public static int STATE_PushedBack_Loop = -1;
        public static int TRANS_AnyState_Idle_PushButton = -1;
        public static int TRANS_EntryState_Idle_PushButton = -1;
        public static int TRANS_AnyState_Idle_PickUp = -1;
        public static int TRANS_EntryState_Idle_PickUp = -1;
        public static int TRANS_AnyState_LayingDown = -1;
        public static int TRANS_EntryState_LayingDown = -1;
        public static int TRANS_AnyState_Damaged_0 = -1;
        public static int TRANS_EntryState_Damaged_0 = -1;
        public static int TRANS_AnyState_Death_180 = -1;
        public static int TRANS_EntryState_Death_180 = -1;
        public static int TRANS_AnyState_Death_0 = -1;
        public static int TRANS_EntryState_Death_0 = -1;
        public static int TRANS_AnyState_Stunned = -1;
        public static int TRANS_EntryState_Stunned = -1;
        public static int TRANS_AnyState_Cower = -1;
        public static int TRANS_EntryState_Cower = -1;
        public static int TRANS_AnyState_KnockedDown = -1;
        public static int TRANS_EntryState_KnockedDown = -1;
        public static int TRANS_AnyState_DeathPose = -1;
        public static int TRANS_EntryState_DeathPose = -1;
        public static int TRANS_AnyState_Frozen = -1;
        public static int TRANS_EntryState_Frozen = -1;
        public static int TRANS_AnyState_PushedBack_Pose = -1;
        public static int TRANS_EntryState_PushedBack_Pose = -1;
        public static int TRANS_Idle_PushButton_IdlePose = -1;
        public static int TRANS_Idle_PickUp_IdlePose = -1;
        public static int TRANS_Sleeping_GettingUp = -1;
        public static int TRANS_GettingUp_IdlePose = -1;
        public static int TRANS_LayingDown_Sleeping = -1;
        public static int TRANS_Damaged_0_IdlePose = -1;
        public static int TRANS_Stunned_IdlePose = -1;
        public static int TRANS_Cower_CowerOut = -1;
        public static int TRANS_CowerOut_IdlePose = -1;
        public static int TRANS_KnockedDown_GettingUpBackward = -1;
        public static int TRANS_GettingUpBackward_IdlePose = -1;
        public static int TRANS_Frozen_IdlePose = -1;
        public static int TRANS_PushedBack_Pose_PushedBack_Loop = -1;
        public static int TRANS_PushedBack_Recover_IdlePose = -1;
        public static int TRANS_PushedBack_Loop_PushedBack_Recover = -1;

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
                    if (lStateID == STATE_Idle_PushButton) { return true; }
                    if (lStateID == STATE_IdlePose) { return true; }
                    if (lStateID == STATE_Idle_PickUp) { return true; }
                    if (lStateID == STATE_Sleeping) { return true; }
                    if (lStateID == STATE_GettingUp) { return true; }
                    if (lStateID == STATE_LayingDown) { return true; }
                    if (lStateID == STATE_Death_180) { return true; }
                    if (lStateID == STATE_Death_0) { return true; }
                    if (lStateID == STATE_Damaged_0) { return true; }
                    if (lStateID == STATE_Stunned) { return true; }
                    if (lStateID == STATE_Cower) { return true; }
                    if (lStateID == STATE_CowerOut) { return true; }
                    if (lStateID == STATE_KnockedDown) { return true; }
                    if (lStateID == STATE_GettingUpBackward) { return true; }
                    if (lStateID == STATE_DeathPose) { return true; }
                    if (lStateID == STATE_Frozen) { return true; }
                    if (lStateID == STATE_PushedBack_Pose) { return true; }
                    if (lStateID == STATE_PushedBack_Recover) { return true; }
                    if (lStateID == STATE_PushedBack_Loop) { return true; }
                }

                if (lTransitionID == TRANS_AnyState_Idle_PushButton) { return true; }
                if (lTransitionID == TRANS_EntryState_Idle_PushButton) { return true; }
                if (lTransitionID == TRANS_AnyState_Idle_PickUp) { return true; }
                if (lTransitionID == TRANS_EntryState_Idle_PickUp) { return true; }
                if (lTransitionID == TRANS_AnyState_LayingDown) { return true; }
                if (lTransitionID == TRANS_EntryState_LayingDown) { return true; }
                if (lTransitionID == TRANS_AnyState_Damaged_0) { return true; }
                if (lTransitionID == TRANS_EntryState_Damaged_0) { return true; }
                if (lTransitionID == TRANS_AnyState_Death_180) { return true; }
                if (lTransitionID == TRANS_EntryState_Death_180) { return true; }
                if (lTransitionID == TRANS_AnyState_Death_0) { return true; }
                if (lTransitionID == TRANS_EntryState_Death_0) { return true; }
                if (lTransitionID == TRANS_AnyState_Stunned) { return true; }
                if (lTransitionID == TRANS_EntryState_Stunned) { return true; }
                if (lTransitionID == TRANS_AnyState_Cower) { return true; }
                if (lTransitionID == TRANS_EntryState_Cower) { return true; }
                if (lTransitionID == TRANS_AnyState_KnockedDown) { return true; }
                if (lTransitionID == TRANS_EntryState_KnockedDown) { return true; }
                if (lTransitionID == TRANS_AnyState_DeathPose) { return true; }
                if (lTransitionID == TRANS_EntryState_DeathPose) { return true; }
                if (lTransitionID == TRANS_AnyState_Death_180) { return true; }
                if (lTransitionID == TRANS_EntryState_Death_180) { return true; }
                if (lTransitionID == TRANS_AnyState_Frozen) { return true; }
                if (lTransitionID == TRANS_EntryState_Frozen) { return true; }
                if (lTransitionID == TRANS_AnyState_PushedBack_Pose) { return true; }
                if (lTransitionID == TRANS_EntryState_PushedBack_Pose) { return true; }
                if (lTransitionID == TRANS_Idle_PushButton_IdlePose) { return true; }
                if (lTransitionID == TRANS_Idle_PickUp_IdlePose) { return true; }
                if (lTransitionID == TRANS_Sleeping_GettingUp) { return true; }
                if (lTransitionID == TRANS_GettingUp_IdlePose) { return true; }
                if (lTransitionID == TRANS_LayingDown_Sleeping) { return true; }
                if (lTransitionID == TRANS_Damaged_0_IdlePose) { return true; }
                if (lTransitionID == TRANS_Stunned_IdlePose) { return true; }
                if (lTransitionID == TRANS_Cower_CowerOut) { return true; }
                if (lTransitionID == TRANS_CowerOut_IdlePose) { return true; }
                if (lTransitionID == TRANS_KnockedDown_GettingUpBackward) { return true; }
                if (lTransitionID == TRANS_GettingUpBackward_IdlePose) { return true; }
                if (lTransitionID == TRANS_Frozen_IdlePose) { return true; }
                if (lTransitionID == TRANS_PushedBack_Pose_PushedBack_Loop) { return true; }
                if (lTransitionID == TRANS_PushedBack_Recover_IdlePose) { return true; }
                if (lTransitionID == TRANS_PushedBack_Loop_PushedBack_Recover) { return true; }
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
            if (rStateID == STATE_Idle_PushButton) { return true; }
            if (rStateID == STATE_IdlePose) { return true; }
            if (rStateID == STATE_Idle_PickUp) { return true; }
            if (rStateID == STATE_Sleeping) { return true; }
            if (rStateID == STATE_GettingUp) { return true; }
            if (rStateID == STATE_LayingDown) { return true; }
            if (rStateID == STATE_Death_180) { return true; }
            if (rStateID == STATE_Death_0) { return true; }
            if (rStateID == STATE_Damaged_0) { return true; }
            if (rStateID == STATE_Stunned) { return true; }
            if (rStateID == STATE_Cower) { return true; }
            if (rStateID == STATE_CowerOut) { return true; }
            if (rStateID == STATE_KnockedDown) { return true; }
            if (rStateID == STATE_GettingUpBackward) { return true; }
            if (rStateID == STATE_DeathPose) { return true; }
            if (rStateID == STATE_Frozen) { return true; }
            if (rStateID == STATE_PushedBack_Pose) { return true; }
            if (rStateID == STATE_PushedBack_Recover) { return true; }
            if (rStateID == STATE_PushedBack_Loop) { return true; }
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
                if (rStateID == STATE_Idle_PushButton) { return true; }
                if (rStateID == STATE_IdlePose) { return true; }
                if (rStateID == STATE_Idle_PickUp) { return true; }
                if (rStateID == STATE_Sleeping) { return true; }
                if (rStateID == STATE_GettingUp) { return true; }
                if (rStateID == STATE_LayingDown) { return true; }
                if (rStateID == STATE_Death_180) { return true; }
                if (rStateID == STATE_Death_0) { return true; }
                if (rStateID == STATE_Damaged_0) { return true; }
                if (rStateID == STATE_Stunned) { return true; }
                if (rStateID == STATE_Cower) { return true; }
                if (rStateID == STATE_CowerOut) { return true; }
                if (rStateID == STATE_KnockedDown) { return true; }
                if (rStateID == STATE_GettingUpBackward) { return true; }
                if (rStateID == STATE_DeathPose) { return true; }
                if (rStateID == STATE_Frozen) { return true; }
                if (rStateID == STATE_PushedBack_Pose) { return true; }
                if (rStateID == STATE_PushedBack_Recover) { return true; }
                if (rStateID == STATE_PushedBack_Loop) { return true; }
            }

            if (rTransitionID == TRANS_AnyState_Idle_PushButton) { return true; }
            if (rTransitionID == TRANS_EntryState_Idle_PushButton) { return true; }
            if (rTransitionID == TRANS_AnyState_Idle_PickUp) { return true; }
            if (rTransitionID == TRANS_EntryState_Idle_PickUp) { return true; }
            if (rTransitionID == TRANS_AnyState_LayingDown) { return true; }
            if (rTransitionID == TRANS_EntryState_LayingDown) { return true; }
            if (rTransitionID == TRANS_AnyState_Damaged_0) { return true; }
            if (rTransitionID == TRANS_EntryState_Damaged_0) { return true; }
            if (rTransitionID == TRANS_AnyState_Death_180) { return true; }
            if (rTransitionID == TRANS_EntryState_Death_180) { return true; }
            if (rTransitionID == TRANS_AnyState_Death_0) { return true; }
            if (rTransitionID == TRANS_EntryState_Death_0) { return true; }
            if (rTransitionID == TRANS_AnyState_Stunned) { return true; }
            if (rTransitionID == TRANS_EntryState_Stunned) { return true; }
            if (rTransitionID == TRANS_AnyState_Cower) { return true; }
            if (rTransitionID == TRANS_EntryState_Cower) { return true; }
            if (rTransitionID == TRANS_AnyState_KnockedDown) { return true; }
            if (rTransitionID == TRANS_EntryState_KnockedDown) { return true; }
            if (rTransitionID == TRANS_AnyState_DeathPose) { return true; }
            if (rTransitionID == TRANS_EntryState_DeathPose) { return true; }
            if (rTransitionID == TRANS_AnyState_Death_180) { return true; }
            if (rTransitionID == TRANS_EntryState_Death_180) { return true; }
            if (rTransitionID == TRANS_AnyState_Frozen) { return true; }
            if (rTransitionID == TRANS_EntryState_Frozen) { return true; }
            if (rTransitionID == TRANS_AnyState_PushedBack_Pose) { return true; }
            if (rTransitionID == TRANS_EntryState_PushedBack_Pose) { return true; }
            if (rTransitionID == TRANS_Idle_PushButton_IdlePose) { return true; }
            if (rTransitionID == TRANS_Idle_PickUp_IdlePose) { return true; }
            if (rTransitionID == TRANS_Sleeping_GettingUp) { return true; }
            if (rTransitionID == TRANS_GettingUp_IdlePose) { return true; }
            if (rTransitionID == TRANS_LayingDown_Sleeping) { return true; }
            if (rTransitionID == TRANS_Damaged_0_IdlePose) { return true; }
            if (rTransitionID == TRANS_Stunned_IdlePose) { return true; }
            if (rTransitionID == TRANS_Cower_CowerOut) { return true; }
            if (rTransitionID == TRANS_CowerOut_IdlePose) { return true; }
            if (rTransitionID == TRANS_KnockedDown_GettingUpBackward) { return true; }
            if (rTransitionID == TRANS_GettingUpBackward_IdlePose) { return true; }
            if (rTransitionID == TRANS_Frozen_IdlePose) { return true; }
            if (rTransitionID == TRANS_PushedBack_Pose_PushedBack_Loop) { return true; }
            if (rTransitionID == TRANS_PushedBack_Recover_IdlePose) { return true; }
            if (rTransitionID == TRANS_PushedBack_Loop_PushedBack_Recover) { return true; }
            return false;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            TRANS_AnyState_Idle_PushButton = mMotionController.AddAnimatorName("AnyState -> Base Layer.Utilities-SM.Idle_PushButton");
            TRANS_EntryState_Idle_PushButton = mMotionController.AddAnimatorName("Entry -> Base Layer.Utilities-SM.Idle_PushButton");
            TRANS_AnyState_Idle_PickUp = mMotionController.AddAnimatorName("AnyState -> Base Layer.Utilities-SM.Idle_PickUp");
            TRANS_EntryState_Idle_PickUp = mMotionController.AddAnimatorName("Entry -> Base Layer.Utilities-SM.Idle_PickUp");
            TRANS_AnyState_LayingDown = mMotionController.AddAnimatorName("AnyState -> Base Layer.Utilities-SM.LayingDown");
            TRANS_EntryState_LayingDown = mMotionController.AddAnimatorName("Entry -> Base Layer.Utilities-SM.LayingDown");
            TRANS_AnyState_Damaged_0 = mMotionController.AddAnimatorName("AnyState -> Base Layer.Utilities-SM.Damaged_0");
            TRANS_EntryState_Damaged_0 = mMotionController.AddAnimatorName("Entry -> Base Layer.Utilities-SM.Damaged_0");
            TRANS_AnyState_Death_180 = mMotionController.AddAnimatorName("AnyState -> Base Layer.Utilities-SM.Death_180");
            TRANS_EntryState_Death_180 = mMotionController.AddAnimatorName("Entry -> Base Layer.Utilities-SM.Death_180");
            TRANS_AnyState_Death_0 = mMotionController.AddAnimatorName("AnyState -> Base Layer.Utilities-SM.Death_0");
            TRANS_EntryState_Death_0 = mMotionController.AddAnimatorName("Entry -> Base Layer.Utilities-SM.Death_0");
            TRANS_AnyState_Stunned = mMotionController.AddAnimatorName("AnyState -> Base Layer.Utilities-SM.Stunned");
            TRANS_EntryState_Stunned = mMotionController.AddAnimatorName("Entry -> Base Layer.Utilities-SM.Stunned");
            TRANS_AnyState_Cower = mMotionController.AddAnimatorName("AnyState -> Base Layer.Utilities-SM.Cower");
            TRANS_EntryState_Cower = mMotionController.AddAnimatorName("Entry -> Base Layer.Utilities-SM.Cower");
            TRANS_AnyState_KnockedDown = mMotionController.AddAnimatorName("AnyState -> Base Layer.Utilities-SM.KnockedDown");
            TRANS_EntryState_KnockedDown = mMotionController.AddAnimatorName("Entry -> Base Layer.Utilities-SM.KnockedDown");
            TRANS_AnyState_DeathPose = mMotionController.AddAnimatorName("AnyState -> Base Layer.Utilities-SM.DeathPose");
            TRANS_EntryState_DeathPose = mMotionController.AddAnimatorName("Entry -> Base Layer.Utilities-SM.DeathPose");
            TRANS_AnyState_Death_180 = mMotionController.AddAnimatorName("AnyState -> Base Layer.Utilities-SM.Death_180");
            TRANS_EntryState_Death_180 = mMotionController.AddAnimatorName("Entry -> Base Layer.Utilities-SM.Death_180");
            TRANS_AnyState_Frozen = mMotionController.AddAnimatorName("AnyState -> Base Layer.Utilities-SM.Frozen");
            TRANS_EntryState_Frozen = mMotionController.AddAnimatorName("Entry -> Base Layer.Utilities-SM.Frozen");
            TRANS_AnyState_PushedBack_Pose = mMotionController.AddAnimatorName("AnyState -> Base Layer.Utilities-SM.PushedBack_Pose");
            TRANS_EntryState_PushedBack_Pose = mMotionController.AddAnimatorName("Entry -> Base Layer.Utilities-SM.PushedBack_Pose");
            STATE_Start = mMotionController.AddAnimatorName("Base Layer.Start");
            STATE_Idle_PushButton = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.Idle_PushButton");
            TRANS_Idle_PushButton_IdlePose = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.Idle_PushButton -> Base Layer.Utilities-SM.IdlePose");
            STATE_IdlePose = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.IdlePose");
            STATE_Idle_PickUp = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.Idle_PickUp");
            TRANS_Idle_PickUp_IdlePose = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.Idle_PickUp -> Base Layer.Utilities-SM.IdlePose");
            STATE_Sleeping = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.Sleeping");
            TRANS_Sleeping_GettingUp = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.Sleeping -> Base Layer.Utilities-SM.GettingUp");
            STATE_GettingUp = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.GettingUp");
            TRANS_GettingUp_IdlePose = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.GettingUp -> Base Layer.Utilities-SM.IdlePose");
            STATE_LayingDown = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.LayingDown");
            TRANS_LayingDown_Sleeping = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.LayingDown -> Base Layer.Utilities-SM.Sleeping");
            STATE_Death_180 = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.Death_180");
            STATE_Death_0 = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.Death_0");
            STATE_Damaged_0 = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.Damaged_0");
            TRANS_Damaged_0_IdlePose = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.Damaged_0 -> Base Layer.Utilities-SM.IdlePose");
            STATE_Stunned = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.Stunned");
            TRANS_Stunned_IdlePose = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.Stunned -> Base Layer.Utilities-SM.IdlePose");
            STATE_Cower = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.Cower");
            TRANS_Cower_CowerOut = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.Cower -> Base Layer.Utilities-SM.Cower Out");
            STATE_CowerOut = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.Cower Out");
            TRANS_CowerOut_IdlePose = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.Cower Out -> Base Layer.Utilities-SM.IdlePose");
            STATE_KnockedDown = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.KnockedDown");
            TRANS_KnockedDown_GettingUpBackward = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.KnockedDown -> Base Layer.Utilities-SM.GettingUpBackward");
            STATE_GettingUpBackward = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.GettingUpBackward");
            TRANS_GettingUpBackward_IdlePose = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.GettingUpBackward -> Base Layer.Utilities-SM.IdlePose");
            STATE_DeathPose = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.DeathPose");
            STATE_Frozen = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.Frozen");
            TRANS_Frozen_IdlePose = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.Frozen -> Base Layer.Utilities-SM.IdlePose");
            STATE_PushedBack_Pose = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.PushedBack_Pose");
            TRANS_PushedBack_Pose_PushedBack_Loop = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.PushedBack_Pose -> Base Layer.Utilities-SM.PushedBack_Loop");
            STATE_PushedBack_Recover = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.PushedBack_Recover");
            TRANS_PushedBack_Recover_IdlePose = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.PushedBack_Recover -> Base Layer.Utilities-SM.IdlePose");
            STATE_PushedBack_Loop = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.PushedBack_Loop");
            TRANS_PushedBack_Loop_PushedBack_Recover = mMotionController.AddAnimatorName("Base Layer.Utilities-SM.PushedBack_Loop -> Base Layer.Utilities-SM.PushedBack_Recover");
        }

#if UNITY_EDITOR

        private AnimationClip m16152 = null;
        private AnimationClip m14248 = null;
        private AnimationClip m23022 = null;
        private AnimationClip m19974 = null;
        private AnimationClip m19970 = null;
        private AnimationClip m19972 = null;
        private AnimationClip m22300 = null;
        private AnimationClip m22298 = null;
        private AnimationClip m22296 = null;
        private AnimationClip m22318 = null;
        private AnimationClip m22294 = null;
        private AnimationClip m22312 = null;
        private AnimationClip m22304 = null;
        private AnimationClip m22302 = null;
        private AnimationClip m98422 = null;
        private AnimationClip m17896 = null;
        private AnimationClip m17898 = null;
        private AnimationClip m117692 = null;

        /// <summary>
        /// Creates the animator substate machine for this motion.
        /// </summary>
        protected override void CreateStateMachine()
        {
            // Grab the root sm for the layer
            UnityEditor.Animations.AnimatorStateMachine lRootStateMachine = _EditorAnimatorController.layers[mMotionLayer.AnimatorLayerIndex].stateMachine;
            UnityEditor.Animations.AnimatorStateMachine lSM_24982 = _EditorAnimatorController.layers[mMotionLayer.AnimatorLayerIndex].stateMachine;
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

            UnityEditor.Animations.AnimatorStateMachine lSM_24984 = lRootSubStateMachine;
            if (lSM_24984 != null)
            {
                for (int i = lSM_24984.entryTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_24984.RemoveEntryTransition(lSM_24984.entryTransitions[i]);
                }

                for (int i = lSM_24984.anyStateTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_24984.RemoveAnyStateTransition(lSM_24984.anyStateTransitions[i]);
                }

                for (int i = lSM_24984.states.Length - 1; i >= 0; i--)
                {
                    lSM_24984.RemoveState(lSM_24984.states[i].state);
                }

                for (int i = lSM_24984.stateMachines.Length - 1; i >= 0; i--)
                {
                    lSM_24984.RemoveStateMachine(lSM_24984.stateMachines[i].stateMachine);
                }
            }
            else
            {
                lSM_24984 = lSM_24982.AddStateMachine(_EditorAnimatorSMName, new Vector3(192, -180, 0));
            }

            UnityEditor.Animations.AnimatorState lS_24986 = lSM_24984.AddState("Idle_PushButton", new Vector3(300, 48, 0));
            lS_24986.speed = 1f;
            lS_24986.motion = m14248;

            UnityEditor.Animations.AnimatorState lS_24988 = lSM_24984.AddState("IdlePose", new Vector3(792, 156, 0));
            lS_24988.speed = 1f;
            lS_24988.motion = m16152;

            UnityEditor.Animations.AnimatorState lS_24990 = lSM_24984.AddState("Idle_PickUp", new Vector3(300, 120, 0));
            lS_24990.speed = 1f;
            lS_24990.motion = m23022;

            UnityEditor.Animations.AnimatorState lS_24992 = lSM_24984.AddState("Sleeping", new Vector3(540, 552, 0));
            lS_24992.speed = 1f;
            lS_24992.motion = m19974;

            UnityEditor.Animations.AnimatorState lS_24994 = lSM_24984.AddState("GettingUp", new Vector3(780, 552, 0));
            lS_24994.speed = 1.7f;
            lS_24994.motion = m19970;

            UnityEditor.Animations.AnimatorState lS_24996 = lSM_24984.AddState("LayingDown", new Vector3(300, 552, 0));
            lS_24996.speed = -1.7f;
            lS_24996.motion = m19972;

            UnityEditor.Animations.AnimatorState lS_24998 = lSM_24984.AddState("Death_180", new Vector3(300, 768, 0));
            lS_24998.speed = 1.8f;
            lS_24998.motion = m22300;

            UnityEditor.Animations.AnimatorState lS_25000 = lSM_24984.AddState("Death_0", new Vector3(300, 696, 0));
            lS_25000.speed = 1.5f;
            lS_25000.motion = m22298;

            UnityEditor.Animations.AnimatorState lS_25002 = lSM_24984.AddState("Damaged_0", new Vector3(300, 264, 0));
            lS_25002.speed = 3f;
            lS_25002.motion = m22296;

            UnityEditor.Animations.AnimatorState lS_25004 = lSM_24984.AddState("Stunned", new Vector3(300, 336, 0));
            lS_25004.speed = 1f;
            lS_25004.motion = m22318;

            UnityEditor.Animations.AnimatorState lS_25006 = lSM_24984.AddState("Cower", new Vector3(300, 408, 0));
            lS_25006.speed = 1f;
            lS_25006.motion = m22294;

            UnityEditor.Animations.AnimatorState lS_25008 = lSM_24984.AddState("Cower Out", new Vector3(540, 408, 0));
            lS_25008.speed = -1f;
            lS_25008.motion = m22294;

            UnityEditor.Animations.AnimatorState lS_25010 = lSM_24984.AddState("KnockedDown", new Vector3(300, 480, 0));
            lS_25010.speed = 1f;
            lS_25010.motion = m22312;

            UnityEditor.Animations.AnimatorState lS_25012 = lSM_24984.AddState("GettingUpBackward", new Vector3(540, 480, 0));
            lS_25012.speed = 1f;
            lS_25012.motion = m22304;

            UnityEditor.Animations.AnimatorState lS_25014 = lSM_24984.AddState("DeathPose", new Vector3(300, 840, 0));
            lS_25014.speed = 1f;
            lS_25014.motion = m22302;

            UnityEditor.Animations.AnimatorState lS_N1006850 = lSM_24984.AddState("Frozen", new Vector3(300, 192, 0));
            lS_N1006850.speed = 1f;
            lS_N1006850.motion = m98422;

            UnityEditor.Animations.AnimatorState lS_N1276828 = lSM_24984.AddState("PushedBack_Pose", new Vector3(300, 624, 0));
            lS_N1276828.speed = 1f;
            lS_N1276828.motion = m17896;

            UnityEditor.Animations.AnimatorState lS_N1276830 = lSM_24984.AddState("PushedBack_Recover", new Vector3(840, 624, 0));
            lS_N1276830.speed = 2f;
            lS_N1276830.motion = m17898;

            UnityEditor.Animations.AnimatorState lS_N1380204 = lSM_24984.AddState("PushedBack_Loop", new Vector3(540, 624, 0));
            lS_N1380204.speed = 0.4f;
            lS_N1380204.motion = m117692;

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_25128 = lRootStateMachine.AddAnyStateTransition(lS_24986);
            lT_25128.hasExitTime = false;
            lT_25128.hasFixedDuration = true;
            lT_25128.exitTime = 0.8999999f;
            lT_25128.duration = 0.09999999f;
            lT_25128.offset = 0.1753971f;
            lT_25128.mute = false;
            lT_25128.solo = false;
            lT_25128.canTransitionToSelf = true;
            lT_25128.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_25128.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 2000f, "L0MotionPhase");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_25148 = lRootStateMachine.AddAnyStateTransition(lS_24990);
            lT_25148.hasExitTime = false;
            lT_25148.hasFixedDuration = true;
            lT_25148.exitTime = 0.9f;
            lT_25148.duration = 0.1f;
            lT_25148.offset = 0f;
            lT_25148.mute = false;
            lT_25148.solo = false;
            lT_25148.canTransitionToSelf = true;
            lT_25148.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_25148.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 2001f, "L0MotionPhase");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_25202 = lRootStateMachine.AddAnyStateTransition(lS_24996);
            lT_25202.hasExitTime = false;
            lT_25202.hasFixedDuration = true;
            lT_25202.exitTime = 0.8999993f;
            lT_25202.duration = 0.3f;
            lT_25202.offset = 0.3938867f;
            lT_25202.mute = false;
            lT_25202.solo = false;
            lT_25202.canTransitionToSelf = true;
            lT_25202.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_25202.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1820f, "L0MotionPhase");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_25204 = lRootStateMachine.AddAnyStateTransition(lS_25002);
            lT_25204.hasExitTime = false;
            lT_25204.hasFixedDuration = true;
            lT_25204.exitTime = 0.9000001f;
            lT_25204.duration = 0.1f;
            lT_25204.offset = 0.1943718f;
            lT_25204.mute = false;
            lT_25204.solo = false;
            lT_25204.canTransitionToSelf = true;
            lT_25204.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_25204.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1850f, "L0MotionPhase");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_25206 = lRootStateMachine.AddAnyStateTransition(lS_24998);
            lT_25206.hasExitTime = false;
            lT_25206.hasFixedDuration = true;
            lT_25206.exitTime = 0.8999999f;
            lT_25206.duration = 0.1f;
            lT_25206.offset = 0.06562664f;
            lT_25206.mute = false;
            lT_25206.solo = false;
            lT_25206.canTransitionToSelf = true;
            lT_25206.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_25206.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1840f, "L0MotionPhase");
            lT_25206.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 100f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_25208 = lRootStateMachine.AddAnyStateTransition(lS_25000);
            lT_25208.hasExitTime = false;
            lT_25208.hasFixedDuration = true;
            lT_25208.exitTime = 0.8999998f;
            lT_25208.duration = 0.1f;
            lT_25208.offset = 0.1486627f;
            lT_25208.mute = false;
            lT_25208.solo = false;
            lT_25208.canTransitionToSelf = true;
            lT_25208.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_25208.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1840f, "L0MotionPhase");
            lT_25208.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, -100f, "L0MotionParameter");
            lT_25208.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 100f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_25210 = lRootStateMachine.AddAnyStateTransition(lS_25004);
            lT_25210.hasExitTime = false;
            lT_25210.hasFixedDuration = true;
            lT_25210.exitTime = 0.9f;
            lT_25210.duration = 0.2f;
            lT_25210.offset = 0f;
            lT_25210.mute = false;
            lT_25210.solo = false;
            lT_25210.canTransitionToSelf = true;
            lT_25210.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_25210.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1870f, "L0MotionPhase");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_25212 = lRootStateMachine.AddAnyStateTransition(lS_25006);
            lT_25212.hasExitTime = false;
            lT_25212.hasFixedDuration = true;
            lT_25212.exitTime = 0.9f;
            lT_25212.duration = 0.2f;
            lT_25212.offset = 0f;
            lT_25212.mute = false;
            lT_25212.solo = false;
            lT_25212.canTransitionToSelf = true;
            lT_25212.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_25212.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1860f, "L0MotionPhase");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_25214 = lRootStateMachine.AddAnyStateTransition(lS_25010);
            lT_25214.hasExitTime = false;
            lT_25214.hasFixedDuration = true;
            lT_25214.exitTime = 0.9f;
            lT_25214.duration = 0.2f;
            lT_25214.offset = 0.08291358f;
            lT_25214.mute = false;
            lT_25214.solo = false;
            lT_25214.canTransitionToSelf = true;
            lT_25214.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_25214.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1880f, "L0MotionPhase");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_25216 = lRootStateMachine.AddAnyStateTransition(lS_25014);
            lT_25216.hasExitTime = false;
            lT_25216.hasFixedDuration = true;
            lT_25216.exitTime = 0.9f;
            lT_25216.duration = 0.1f;
            lT_25216.offset = 0f;
            lT_25216.mute = false;
            lT_25216.solo = false;
            lT_25216.canTransitionToSelf = true;
            lT_25216.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_25216.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, -99f, "L0MotionPhase");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N34206 = lRootStateMachine.AddAnyStateTransition(lS_24998);
            lT_N34206.hasExitTime = false;
            lT_N34206.hasFixedDuration = true;
            lT_N34206.exitTime = 0.9f;
            lT_N34206.duration = 0.1f;
            lT_N34206.offset = 0.06562664f;
            lT_N34206.mute = false;
            lT_N34206.solo = false;
            lT_N34206.canTransitionToSelf = true;
            lT_N34206.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N34206.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1840f, "L0MotionPhase");
            lT_N34206.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -100f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N1007520 = lRootStateMachine.AddAnyStateTransition(lS_N1006850);
            lT_N1007520.hasExitTime = false;
            lT_N1007520.hasFixedDuration = true;
            lT_N1007520.exitTime = 0.9f;
            lT_N1007520.duration = 0.2f;
            lT_N1007520.offset = 0f;
            lT_N1007520.mute = false;
            lT_N1007520.solo = false;
            lT_N1007520.canTransitionToSelf = true;
            lT_N1007520.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1007520.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1890f, "L0MotionPhase");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N1277960 = lRootStateMachine.AddAnyStateTransition(lS_N1276828);
            lT_N1277960.hasExitTime = false;
            lT_N1277960.hasFixedDuration = true;
            lT_N1277960.exitTime = 0.9f;
            lT_N1277960.duration = 0.1f;
            lT_N1277960.offset = 0f;
            lT_N1277960.mute = false;
            lT_N1277960.solo = false;
            lT_N1277960.canTransitionToSelf = true;
            lT_N1277960.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1277960.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1830f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_26284 = lS_24986.AddTransition(lS_24988);
            lT_26284.hasExitTime = true;
            lT_26284.hasFixedDuration = true;
            lT_26284.exitTime = 0.758442f;
            lT_26284.duration = 0.2499998f;
            lT_26284.offset = 0f;
            lT_26284.mute = false;
            lT_26284.solo = false;
            lT_26284.canTransitionToSelf = true;
            lT_26284.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_26286 = lS_24990.AddTransition(lS_24988);
            lT_26286.hasExitTime = true;
            lT_26286.hasFixedDuration = true;
            lT_26286.exitTime = 0.90625f;
            lT_26286.duration = 0.25f;
            lT_26286.offset = 0f;
            lT_26286.mute = false;
            lT_26286.solo = false;
            lT_26286.canTransitionToSelf = true;
            lT_26286.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_26288 = lS_24992.AddTransition(lS_24994);
            lT_26288.hasExitTime = false;
            lT_26288.hasFixedDuration = true;
            lT_26288.exitTime = 0.9635922f;
            lT_26288.duration = 0.5f;
            lT_26288.offset = 0f;
            lT_26288.mute = false;
            lT_26288.solo = false;
            lT_26288.canTransitionToSelf = true;
            lT_26288.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_26288.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1825f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_26290 = lS_24994.AddTransition(lS_24988);
            lT_26290.hasExitTime = true;
            lT_26290.hasFixedDuration = true;
            lT_26290.exitTime = 0.5882814f;
            lT_26290.duration = 0.25f;
            lT_26290.offset = 0f;
            lT_26290.mute = false;
            lT_26290.solo = false;
            lT_26290.canTransitionToSelf = true;
            lT_26290.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_26292 = lS_24996.AddTransition(lS_24992);
            lT_26292.hasExitTime = true;
            lT_26292.hasFixedDuration = true;
            lT_26292.exitTime = 0.9f;
            lT_26292.duration = 0.5f;
            lT_26292.offset = 0f;
            lT_26292.mute = false;
            lT_26292.solo = false;
            lT_26292.canTransitionToSelf = true;
            lT_26292.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_26294 = lS_25002.AddTransition(lS_24988);
            lT_26294.hasExitTime = true;
            lT_26294.hasFixedDuration = true;
            lT_26294.exitTime = 0.8578009f;
            lT_26294.duration = 0.2500002f;
            lT_26294.offset = 27.37617f;
            lT_26294.mute = false;
            lT_26294.solo = false;
            lT_26294.canTransitionToSelf = true;
            lT_26294.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_26296 = lS_25004.AddTransition(lS_24988);
            lT_26296.hasExitTime = false;
            lT_26296.hasFixedDuration = true;
            lT_26296.exitTime = 0.97f;
            lT_26296.duration = 0.25f;
            lT_26296.offset = 0f;
            lT_26296.mute = false;
            lT_26296.solo = false;
            lT_26296.canTransitionToSelf = true;
            lT_26296.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_26296.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1875f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_26298 = lS_25006.AddTransition(lS_25008);
            lT_26298.hasExitTime = false;
            lT_26298.hasFixedDuration = true;
            lT_26298.exitTime = 1f;
            lT_26298.duration = 0f;
            lT_26298.offset = 0f;
            lT_26298.mute = false;
            lT_26298.solo = false;
            lT_26298.canTransitionToSelf = true;
            lT_26298.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_26298.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1865f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_26300 = lS_25008.AddTransition(lS_24988);
            lT_26300.hasExitTime = true;
            lT_26300.hasFixedDuration = true;
            lT_26300.exitTime = 0.8333334f;
            lT_26300.duration = 0.25f;
            lT_26300.offset = 0f;
            lT_26300.mute = false;
            lT_26300.solo = false;
            lT_26300.canTransitionToSelf = true;
            lT_26300.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_26302 = lS_25010.AddTransition(lS_25012);
            lT_26302.hasExitTime = false;
            lT_26302.hasFixedDuration = true;
            lT_26302.exitTime = 0.8863636f;
            lT_26302.duration = 0.25f;
            lT_26302.offset = 0f;
            lT_26302.mute = false;
            lT_26302.solo = false;
            lT_26302.canTransitionToSelf = true;
            lT_26302.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_26302.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1885f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_26304 = lS_25012.AddTransition(lS_24988);
            lT_26304.hasExitTime = true;
            lT_26304.hasFixedDuration = true;
            lT_26304.exitTime = 0.6343459f;
            lT_26304.duration = 0.2500002f;
            lT_26304.offset = 0f;
            lT_26304.mute = false;
            lT_26304.solo = false;
            lT_26304.canTransitionToSelf = true;
            lT_26304.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_N1007618 = lS_N1006850.AddTransition(lS_24988);
            lT_N1007618.hasExitTime = false;
            lT_N1007618.hasFixedDuration = true;
            lT_N1007618.exitTime = 0f;
            lT_N1007618.duration = 0.25f;
            lT_N1007618.offset = 0f;
            lT_N1007618.mute = false;
            lT_N1007618.solo = false;
            lT_N1007618.canTransitionToSelf = true;
            lT_N1007618.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1007618.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1895f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_N1380416 = lS_N1276828.AddTransition(lS_N1380204);
            lT_N1380416.hasExitTime = true;
            lT_N1380416.hasFixedDuration = true;
            lT_N1380416.exitTime = 0.95f;
            lT_N1380416.duration = 0.1f;
            lT_N1380416.offset = 0f;
            lT_N1380416.mute = false;
            lT_N1380416.solo = false;
            lT_N1380416.canTransitionToSelf = true;
            lT_N1380416.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_N1278084 = lS_N1276830.AddTransition(lS_24988);
            lT_N1278084.hasExitTime = true;
            lT_N1278084.hasFixedDuration = true;
            lT_N1278084.exitTime = 0.6834157f;
            lT_N1278084.duration = 0.25f;
            lT_N1278084.offset = 0f;
            lT_N1278084.mute = false;
            lT_N1278084.solo = false;
            lT_N1278084.canTransitionToSelf = true;
            lT_N1278084.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_N1380520 = lS_N1380204.AddTransition(lS_N1276830);
            lT_N1380520.hasExitTime = true;
            lT_N1380520.hasFixedDuration = true;
            lT_N1380520.exitTime = 0.06250077f;
            lT_N1380520.duration = 0.25f;
            lT_N1380520.offset = 0f;
            lT_N1380520.mute = false;
            lT_N1380520.solo = false;
            lT_N1380520.canTransitionToSelf = true;
            lT_N1380520.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1380520.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1835f, "L0MotionPhase");

        }

        /// <summary>
        /// Gathers the animations so we can use them when creating the sub-state machine.
        /// </summary>
        public override void FindAnimations()
        {
            m16152 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdlePose.anim", "IdlePose");
            m14248 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Interacting/unity_IdleGrab_Neutral.fbx/Idle_PushButton.anim", "Idle_PushButton");
            m23022 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Interacting/unity_IdleGrab_LowFront.fbx/Idle_PickUp.anim", "Idle_PickUp");
            m19974 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Sleeping.fbx/Sleeping.anim", "Sleeping");
            m19970 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Sleeping.fbx/GettingUp.anim", "GettingUp");
            m19972 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Sleeping.fbx/LayingDown.anim", "LayingDown");
            m22300 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Utilities_01.fbx/DeathForward.anim", "DeathForward");
            m22298 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Utilities_01.fbx/DeathBackward.anim", "DeathBackward");
            m22296 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Utilities_01.fbx/Damaged.anim", "Damaged");
            m22318 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Utilities_01.fbx/Stunned.anim", "Stunned");
            m22294 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Utilities_01.fbx/Cower.anim", "Cower");
            m22312 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Utilities_01.fbx/KnockedDown.anim", "KnockedDown");
            m22304 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Utilities_01.fbx/GettingUpBackward.anim", "GettingUpBackward");
            m22302 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Utilities_01.fbx/DeathPose.anim", "DeathPose");
            m98422 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Utilities_02.fbx/Frozen.anim", "Frozen");
            m17896 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_PushedBack.fbx/PushedBack_Pose.anim", "PushedBack_Pose");
            m17898 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_PushedBack.fbx/PushedBack_Recover.anim", "PushedBack_Recover");
            m117692 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_PushedBack.fbx/PushedBack_Loop.anim", "PushedBack_Loop");

            // Add the remaining functionality
            base.FindAnimations();
        }

        /// <summary>
        /// Used to show the settings that allow us to generate the animator setup.
        /// </summary>
        public override void OnSettingsGUI()
        {
            UnityEditor.EditorGUILayout.IntField(new GUIContent("Phase ID", "Phase ID used to transition to the state."), PHASE_START);
            m16152 = CreateAnimationField("Start.IdlePose", "Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdlePose.anim", "IdlePose", m16152);
            m14248 = CreateAnimationField("Idle_PushButton", "Assets/ootii/MotionController/Content/Animations/Humanoid/Interacting/unity_IdleGrab_Neutral.fbx/Idle_PushButton.anim", "Idle_PushButton", m14248);
            m23022 = CreateAnimationField("Idle_PickUp", "Assets/ootii/MotionController/Content/Animations/Humanoid/Interacting/unity_IdleGrab_LowFront.fbx/Idle_PickUp.anim", "Idle_PickUp", m23022);
            m19974 = CreateAnimationField("Sleeping", "Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Sleeping.fbx/Sleeping.anim", "Sleeping", m19974);
            m19970 = CreateAnimationField("GettingUp", "Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Sleeping.fbx/GettingUp.anim", "GettingUp", m19970);
            m19972 = CreateAnimationField("LayingDown", "Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Sleeping.fbx/LayingDown.anim", "LayingDown", m19972);
            m22300 = CreateAnimationField("Death_180.DeathForward", "Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Utilities_01.fbx/DeathForward.anim", "DeathForward", m22300);
            m22298 = CreateAnimationField("Death_0.DeathBackward", "Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Utilities_01.fbx/DeathBackward.anim", "DeathBackward", m22298);
            m22296 = CreateAnimationField("Damaged_0.Damaged", "Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Utilities_01.fbx/Damaged.anim", "Damaged", m22296);
            m22318 = CreateAnimationField("Stunned", "Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Utilities_01.fbx/Stunned.anim", "Stunned", m22318);
            m22294 = CreateAnimationField("Cower", "Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Utilities_01.fbx/Cower.anim", "Cower", m22294);
            m22312 = CreateAnimationField("KnockedDown", "Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Utilities_01.fbx/KnockedDown.anim", "KnockedDown", m22312);
            m22304 = CreateAnimationField("GettingUpBackward", "Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Utilities_01.fbx/GettingUpBackward.anim", "GettingUpBackward", m22304);
            m22302 = CreateAnimationField("DeathPose", "Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Utilities_01.fbx/DeathPose.anim", "DeathPose", m22302);
            m98422 = CreateAnimationField("Frozen", "Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Utilities_02.fbx/Frozen.anim", "Frozen", m98422);
            m17896 = CreateAnimationField("PushedBack_Pose", "Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_PushedBack.fbx/PushedBack_Pose.anim", "PushedBack_Pose", m17896);
            m17898 = CreateAnimationField("PushedBack_Recover", "Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_PushedBack.fbx/PushedBack_Recover.anim", "PushedBack_Recover", m17898);
            m117692 = CreateAnimationField("PushedBack_Loop", "Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_PushedBack.fbx/PushedBack_Loop.anim", "PushedBack_Loop", m117692);

            // Add the remaining functionality
            base.OnSettingsGUI();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}
