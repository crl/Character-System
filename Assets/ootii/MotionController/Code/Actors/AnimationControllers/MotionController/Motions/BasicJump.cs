using UnityEngine;
using com.ootii.Actors.Navigation;
using com.ootii.Geometry;
using com.ootii.Helpers;
using com.ootii.Messages;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Actors.AnimationControllers
{
    /// <summary>
    /// Idle motion for when the character is just standing and waiting
    /// for input or some interaction.
    /// </summary>
    [MotionName("Basic Jump")]
    [MotionDescription("Simple jump motion that can be expanded.")]
    public class BasicJump : MotionControllerMotion
    {
        /// <summary>
        /// Trigger values for the motion
        /// </summary>
        public int PHASE_UNKNOWN = 0;
        public int PHASE_START = 3400;

        /// <summary>
        /// Determines if we're using the IsInMotion() function to verify that
        /// the transition in the animator has occurred for this motion.
        /// </summary>
        public override bool VerifyTransition
        {
            get { return false; }
        }

        /// <summary>
        /// Use the launch velocity throughout the jump
        /// </summary>
        public bool _IsMomentumEnabled = true;
        public bool IsMomentumEnabled
        {
            get { return _IsMomentumEnabled; }
            set { _IsMomentumEnabled = value; }
        }

        /// <summary>
        /// Determines if the player can control the avatar movement
        /// and rotation while in the air.
        /// </summary>
        public bool _IsControlEnabled = true;
        public bool IsControlEnabled
        {
            get { return _IsControlEnabled; }
            set { _IsControlEnabled = value; }
        }

        /// <summary>
        /// When in air, the player can still move the avatar. This
        /// value is the max speed the player can move the avatar by.
        /// </summary>
        public float _ControlSpeed = 2f;
        public float ControlSpeed
        {
            get { return _ControlSpeed; }
            set { _ControlSpeed = value; }
        }

        /// <summary>
        /// Determines if we allow the character to slide before and after the jump
        /// </summary>
        public bool _AllowSliding = true;
        public bool AllowSliding
        {
            get { return _AllowSliding; }
            set { _AllowSliding = value; }
        }
        
        /// <summary>
        /// If the value is great than 0, we'll do a check to see if there
        /// is enough room to even attempt a jump. While in a jump, we'll cancel it
        /// if there isn't enough room
        /// </summary>
        public float _RequiredOverheadDistance = 0.5f;
        public float RequiredOverheadDistance
        {
            get { return _RequiredOverheadDistance; }
            set { _RequiredOverheadDistance = value; }
        }

        // Used to force a change if neede
        protected int mActiveForm = 0;

        // Forward the player was facing when they launched. It helps
        // us control the total rotation that can happen in the air.
        protected Vector3 mLaunchForward = Vector3.zero;

        // Velocity at the time the character launches. This helps us with momentum
        protected Vector3 mLaunchVelocity = Vector3.zero;

        // Helps manage where we are in the jump state
        protected bool mHasLeftGround = false;

        // Store values so we can reset them
        protected bool mStoreIsGravityEnabled = true;
        protected bool mStoreForcedGrounding = true;

        /// <summary>
        /// Default constructor
        /// </summary>
        public BasicJump()
            : base()
        {
            _Category = EnumMotionCategories.JUMP;

            _Priority = 15;
            _ActionAlias = "Jump";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "BasicJump-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public BasicJump(MotionController rController)
            : base(rController)
        {
            _Category = EnumMotionCategories.JUMP;

            _Priority = 15;
            _ActionAlias = "Jump";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "BasicJump-SM"; }
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
            // If we're not startable, this is easy
            if (!mIsStartable)
            {
                return false;
            }

            // If we're working as an NPC, this changes things a bit. We're being controlled
            // by our velocity. So, use that to determine if we're heading up.
            if (mActorController.UseTransformPosition)
            {
                if (!mActorController.IsGrounded)
                {
                    // If we're moving up, we're probably jumping
                    Vector3 lVerticalVelocity = Vector3.Project(mActorController.State.Velocity, mActorController._Transform.up);
                    if (Vector3.Dot(lVerticalVelocity, mActorController._Transform.up) > 0f)
                    {
                        if (mMotionLayer.ActiveMotion == null ||
                            mMotionLayer.ActiveMotion.Category != EnumMotionCategories.CLIMB)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }

            // If we're not grounded, this is easy
            if (!mActorController.IsGrounded)
            {
                return false;
            }

            // Ensure we have input to test
            if (_ActionAlias.Length == 0 || mMotionController._InputSource == null)
            {
                return false;
            }

            // Test the action alias
            if (!mMotionController._InputSource.IsJustPressed(_ActionAlias))
            {
                return false;
            }

            // Perform an upward raycast to determine if something is overhead. If it is, we need
            // to prevent or stop a jump
            if (_RequiredOverheadDistance > 0f)
            {
                if (RaycastExt.SafeRaycast(mActorController._Transform.position, mActorController._Transform.up, _RequiredOverheadDistance, mActorController._CollisionLayers, mActorController._Transform))
                {
                    return false;
                }
            }

            // Allow the jump
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

            // Get out if we're in the end state
            if (mMotionController.State.AnimatorStates[mMotionLayer._AnimatorLayerIndex].StateInfo.IsTag("Exit"))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Determines if this motion can be interrupted
        /// </summary>
        /// <param name="rMotion"></param>
        /// <returns></returns>
        public override bool TestInterruption(MotionControllerMotion rMotion)
        {
            // Don't allow the fall to kick in if we're close to the ground
            if (rMotion is Fall)
            {
                if (mActorController.State.GroundSurfaceDistance < 1f)
                {
                    return false;
                }
            }

            return base.TestInterruption(rMotion);
        }

        /// <summary>
        /// Called to start the specific motion. If the motion
        /// were something like 'jump', this would start the jumping process
        /// </summary>
        /// <param name="rPrevMotion">Motion that this motion is taking over from</param>
        public override bool Activate(MotionControllerMotion rPrevMotion)
        {
            mStoreIsGravityEnabled = mActorController.IsGravityEnabled;
            mActorController.IsGravityEnabled = false;

            mStoreForcedGrounding = mActorController.ForceGrounding;
            mActorController.ForceGrounding = false;

            // Grab the current velocities
            mHasLeftGround = false;
            mLaunchForward = mActorController._Transform.forward;
            mLaunchVelocity = mActorController.State.Velocity;

            // Trigger the transition
            mActiveForm = (_Form > 0 ? _Form : mMotionController.CurrentForm);
            mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, mActiveForm, mParameter, true);

            // Finalize the activation
            return base.Activate(rPrevMotion);
        }

        /// <summary>
        /// Raised when we shut the motion down
        /// </summary>
        public override void Deactivate()
        {
            mActorController.IsGravityEnabled = mStoreIsGravityEnabled;
            mActorController.ForceGrounding = mStoreForcedGrounding;

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
        }

        /// <summary>
        /// Updates the motion over time. This is called by the controller
        /// every update cycle so animations and stages can be updated.
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        public override void Update(float rDeltaTime, int rUpdateIndex)
        {
            if (!mHasLeftGround && mActorController.State.GroundSurfaceDistance > 0.1f) { mHasLeftGround = true; }

            // Determine the resulting velocity of this update
            mVelocity = DetermineVelocity(_AllowSliding);

            // Determine if it's time to re-enable grounding
            if (mHasLeftGround && mActorController.State.GroundSurfaceDistance < 0.1f)
            {
                mActorController.IsGravityEnabled = mStoreIsGravityEnabled;
                mActorController.ForceGrounding = mStoreForcedGrounding;
            }
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
                        if (mActorController.State.Velocity.magnitude < 5f)
                        {
                            rMessage.Recipient = this;
                            rMessage.IsHandled = true;

                            mMotionController.ActivateMotion(this);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Returns the current velocity of the motion
        /// </summary>
        protected Vector3 DetermineVelocity(bool rAllowSlide)
        {
            Vector3 lVelocity = Vector3.zero;
            //int lStateID = mMotionLayer._AnimatorStateID;

            // TRT 11/20/15: If we're colliding with an object, we won't allow
            // any velocity. This helps prevent sliding while jumping
            // against an object.
            if (mActorController.State.IsColliding)
            {
                return lVelocity;
            }

            // Determines if we allow sliding or not
            if (!rAllowSlide && mActorController.State.IsGrounded)
            {
                return lVelocity;
            }

            // If were in the midst of jumping, we want to add velocity based on 
            // the magnitude of the controller. 
            //if ((lStateID != STATE_JumpRecoverIdle || rAllowSlide) &&
            //    (lStateID != STATE_JumpRecoverRun || rAllowSlide) &&
            //    IsInMotionState)
            {
                MotionState lState = mMotionController.State;

                // Speed that comes from momenum
                Vector3 lMomentum = mLaunchVelocity;
                float lMomentumSpeed = (_IsMomentumEnabled ? lMomentum.magnitude : 0f);

                // Speed that comes from the user
                float lControlSpeed = (_IsControlEnabled ? _ControlSpeed * lState.InputMagnitudeTrend.Value : 0f);

                // Speed we'll use as the character is jumping
                float lAirSpeed = Mathf.Max(lMomentumSpeed, lControlSpeed);

                // If we allow control, let the player determine the direction
                if (_IsControlEnabled)
                {
                    Vector3 lBaseForward = mActorController._Transform.forward;
                    if (mMotionController._InputSource != null && mMotionController._InputSource.IsEnabled)
                    {
                        if (mMotionController._CameraTransform != null)
                        {
                            lBaseForward = mMotionController._CameraTransform.forward;
                        }
                    }

                    // Create a quaternion that gets us from our world-forward to our actor/camera direction.
                    // FromToRotation creates a quaternion using the shortest method which can sometimes
                    // flip the angle. LookRotation will attempt to keep the "up" direction "up".
                    Quaternion lToBaseForward = Quaternion.LookRotation(lBaseForward, mActorController._Transform.up);

                    // Determine the avatar displacement direction. This isn't just
                    // normal movement forward, but includes movement to the side
                    Vector3 lMoveDirection = lToBaseForward * lState.InputForward;

                    // Apply the direction and speed
                    lVelocity = lVelocity + (lMoveDirection * lAirSpeed);
                }

                // If momementum is enabled, add it to keep the player moving in the direction of the jump
                if (_IsMomentumEnabled)
                {
                    lVelocity = lVelocity + lMomentum;
                }

                // Don't exceed our air speed
                if (lVelocity.magnitude > lAirSpeed)
                {
                    lVelocity = lVelocity.normalized * lAirSpeed;
                }
            }

            return lVelocity;
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

            string lNewActionAlias = EditorGUILayout.TextField(new GUIContent("Action Alias", "Action alias that triggers a climb."), ActionAlias, GUILayout.MinWidth(30));
            if (lNewActionAlias != ActionAlias)
            {
                lIsDirty = true;
                ActionAlias = lNewActionAlias;
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Is Momentum Enabled", "Determines if the avatar's speed and direction before the jump are used to propel the avatar while in the air.", IsMomentumEnabled, mMotionController))
            {
                lIsDirty = true;
                IsMomentumEnabled = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("Is Control Enabled", "Determines if the player can control the avatar while in the air.", IsControlEnabled, mMotionController))
            {
                lIsDirty = true;
                IsControlEnabled = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.FloatField("Control Speed", "Speed of the avatar when in the air. This should roughly match the ground speed of the avatar.", ControlSpeed, mMotionController))
            {
                lIsDirty = true;
                ControlSpeed = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.BoolField("Allow Sliding", "Determines if the character can move while on the grounde. This creates sliding, but keeps the movement flowing.", AllowSliding, mMotionController))
            {
                lIsDirty = true;
                AllowSliding = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.FloatField("Overhead Distance", "Distance above the character that must be clear before we can jump.", RequiredOverheadDistance, mMotionController))
            {
                lIsDirty = true;
                RequiredOverheadDistance = EditorHelper.FieldFloatValue;
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
        public int STATE_Start = -1;
        public int STATE_UnarmedJump = -1;
        public int STATE_IdlePose = -1;
        public int TRANS_AnyState_UnarmedJump = -1;
        public int TRANS_EntryState_UnarmedJump = -1;
        public int TRANS_UnarmedJump_IdlePose = -1;

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
                    if (lStateID == STATE_UnarmedJump) { return true; }
                    if (lStateID == STATE_IdlePose) { return true; }
                }

                if (lTransitionID == TRANS_AnyState_UnarmedJump) { return true; }
                if (lTransitionID == TRANS_EntryState_UnarmedJump) { return true; }
                if (lTransitionID == TRANS_UnarmedJump_IdlePose) { return true; }
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
            if (rStateID == STATE_UnarmedJump) { return true; }
            if (rStateID == STATE_IdlePose) { return true; }
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
                if (rStateID == STATE_UnarmedJump) { return true; }
                if (rStateID == STATE_IdlePose) { return true; }
            }

            if (rTransitionID == TRANS_AnyState_UnarmedJump) { return true; }
            if (rTransitionID == TRANS_EntryState_UnarmedJump) { return true; }
            if (rTransitionID == TRANS_UnarmedJump_IdlePose) { return true; }
            return false;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            string lLayer = mMotionController.Animator.GetLayerName(mMotionLayer._AnimatorLayerIndex);
            TRANS_AnyState_UnarmedJump = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".BasicJump-SM.Unarmed Jump");
            TRANS_EntryState_UnarmedJump = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".BasicJump-SM.Unarmed Jump");
            STATE_Start = mMotionController.AddAnimatorName("" + lLayer + ".Start");
            STATE_UnarmedJump = mMotionController.AddAnimatorName("" + lLayer + ".BasicJump-SM.Unarmed Jump");
            TRANS_UnarmedJump_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".BasicJump-SM.Unarmed Jump -> " + lLayer + ".BasicJump-SM.IdlePose");
            STATE_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".BasicJump-SM.IdlePose");
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

            UnityEditor.Animations.AnimatorStateMachine lSSM_31068 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "BasicJump-SM");
            if (lSSM_31068 == null) { lSSM_31068 = lLayerStateMachine.AddStateMachine("BasicJump-SM", new Vector3(192, -864, 0)); }

            UnityEditor.Animations.AnimatorState lState_31422 = MotionControllerMotion.EditorFindState(lSSM_31068, "Unarmed Jump");
            if (lState_31422 == null) { lState_31422 = lSSM_31068.AddState("Unarmed Jump", new Vector3(360, -60, 0)); }
            lState_31422.speed = 1.1f;
            lState_31422.mirror = false;
            lState_31422.tag = "";
            lState_31422.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Jumping/ootii_StandingJump.fbx", "StandingJump");

            UnityEditor.Animations.AnimatorState lState_32404 = MotionControllerMotion.EditorFindState(lSSM_31068, "IdlePose");
            if (lState_32404 == null) { lState_32404 = lSSM_31068.AddState("IdlePose", new Vector3(600, -60, 0)); }
            lState_32404.speed = 1f;
            lState_32404.mirror = false;
            lState_32404.tag = "Exit";
            lState_32404.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx", "IdlePose");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_31250 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_31422, 0);
            if (lAnyTransition_31250 == null) { lAnyTransition_31250 = lLayerStateMachine.AddAnyStateTransition(lState_31422); }
            lAnyTransition_31250.isExit = false;
            lAnyTransition_31250.hasExitTime = false;
            lAnyTransition_31250.hasFixedDuration = true;
            lAnyTransition_31250.exitTime = 0.75f;
            lAnyTransition_31250.duration = 0.25f;
            lAnyTransition_31250.offset = 0f;
            lAnyTransition_31250.mute = false;
            lAnyTransition_31250.solo = false;
            lAnyTransition_31250.canTransitionToSelf = true;
            lAnyTransition_31250.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_31250.conditions.Length - 1; i >= 0; i--) { lAnyTransition_31250.RemoveCondition(lAnyTransition_31250.conditions[i]); }
            lAnyTransition_31250.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3400f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_31250.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");
            lAnyTransition_31250.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lTransition_32406 = MotionControllerMotion.EditorFindTransition(lState_31422, lState_32404, 0);
            if (lTransition_32406 == null) { lTransition_32406 = lState_31422.AddTransition(lState_32404); }
            lTransition_32406.isExit = false;
            lTransition_32406.hasExitTime = true;
            lTransition_32406.hasFixedDuration = true;
            lTransition_32406.exitTime = 0.7643284f;
            lTransition_32406.duration = 0.25f;
            lTransition_32406.offset = 0f;
            lTransition_32406.mute = false;
            lTransition_32406.solo = false;
            lTransition_32406.canTransitionToSelf = true;
            lTransition_32406.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_32406.conditions.Length - 1; i >= 0; i--) { lTransition_32406.RemoveCondition(lTransition_32406.conditions[i]); }

#if USE_ARCHERY_MP || OOTII_AYMP
            ArcheryPackDefinition.ExtendBasicJump(rMotionController, rLayerIndex);
#endif

#if USE_SWORD_SHIELD_MP || OOTII_SSMP
            SwordShieldPackDefinition.ExtendBasicJump(rMotionController, rLayerIndex);
#endif

#if USE_SPELL_CASTING_MP || OOTII_SCMP
            SpellCastingPackDefinition.ExtendBasicJump(rMotionController, rLayerIndex);
#endif

            // Run any post processing after creating the state machine
            OnStateMachineCreated();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}
