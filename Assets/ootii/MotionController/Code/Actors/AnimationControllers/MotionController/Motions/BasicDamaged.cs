using UnityEngine;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.Combat;
using com.ootii.Geometry;
using com.ootii.Helpers;
using com.ootii.Messages;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.MotionControllerPacks
{
    /// <summary>
    /// Adventure style walk/run that uses Mixamo's bow animations.
    /// </summary>
    [MotionName("Basic Damaged")]
    [MotionDescription("Support generic animations for being damaged.")]
    public class BasicDamaged : MotionControllerMotion
    {
        // Enum values for the motion
        public int PHASE_UNKNOWN = 0;
        public int PHASE_START = 3350;

        /// <summary>
        /// Determines if we're using the IsInMotion() function to verify that
        /// the transition in the animator has occurred for this motion.
        /// </summary>
        public override bool VerifyTransition
        {
            get { return false; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public BasicDamaged()
            : base()
        {
            _Category = EnumMotionCategories.IMPACT;

            _Priority = 30;
            _ActionAlias = "";
            _OverrideLayers = true;

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "BasicDamaged-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public BasicDamaged(MotionController rController)
            : base(rController)
        {
            _Category = EnumMotionCategories.IMPACT;

            _Priority = 30;
            _ActionAlias = "";
            _OverrideLayers = true;

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "BasicDamaged-SM"; }
#endif
        }

        /// <summary>
        /// Allows for any processing after the motion has been deserialized
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
            if (!mIsStartable) { return false; }
            if (!mMotionController.IsGrounded) { return false; }
            //if (mActorController.State.Stance != EnumControllerStance.TRAVERSAL) { return false; }

            // Test if we're supposed to activate
            if (_ActionAlias.Length > 0 && mMotionController._InputSource != null)
            {
                if (mMotionController._InputSource.IsJustPressed(_ActionAlias))
                {
                    return true;
                }
            }

            // Stop
            return false;
        }

        /// <summary>
        /// Tests if the motion should continue. If it shouldn't, the motion
        /// is typically disabled
        /// </summary>
        /// <returns>Boolean that determines if the motion continues</returns>
        public override bool TestUpdate()
        {
            if (mIsActivatedFrame) { return true; }
            if (!mMotionController.IsGrounded) { return false; }
            //if (mActorController.State.Stance != EnumControllerStance.COMBAT_MELEE_SWORD_SHIELD) { return false; }

            // Get out if we're in the end state
            //if (mMotionLayer._AnimatorStateID == STATE_UnarmedDamaged0 && mMotionLayer._AnimatorStateNormalizedTime > 0.9f)
            if (mMotionController.State.AnimatorStates[mMotionLayer._AnimatorLayerIndex].StateInfo.IsTag("Exit"))
            {
                if (mMotionLayer._AnimatorTransitionID == 0 && mMotionController.State.AnimatorStates[mMotionLayer._AnimatorLayerIndex].MotionPhase != PHASE_START)
                {
                    if (mMotionLayer._AnimatorStateID == GetStateID("Unarmed Damaged 0"))
                    {
                        if (mMotionLayer._AnimatorStateNormalizedTime > 0.9f)
                        {
                            return false;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
            }

            //// Ensure we're in the animation
            //if (mIsAnimatorActive && !IsInMotionState)
            //{
            //    return false;
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
            return true;
        }

        /// <summary>
        /// Called to start the specific motion. If the motion
        /// were something like 'jump', this would start the jumping process
        /// </summary>
        /// <param name="rPrevMotion">Motion that this motion is taking over from</param>
        public override bool Activate(MotionControllerMotion rPrevMotion)
        {
            // Activate the motion
            int lForm = (_Form > 0 ? _Form : mMotionController.CurrentForm);

#if OOTII_SHMP
            if (lForm == 500 || lForm == 550) { lForm = 0; }
#endif

            mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, PHASE_START, lForm, Parameter, true);

            // Return
            return base.Activate(rPrevMotion);
        }

        /// <summary>
        /// Called to stop the motion. If the motion is stopable. Some motions
        /// like jump cannot be stopped early
        /// </summary>
        public override void Deactivate()
        {
            // Finish the deactivation process
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
        }

        /// <summary>
        /// Updates the motion over time. This is called by the controller
        /// every update cycle so animations and stages can be updated.
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        public override void Update(float rDeltaTime, int rUpdateIndex)
        {
            //// In some cases, we may force the death while in this utility motion. So, we'll clear the phase ourselves
            //if (mMotionLayer._AnimatorTransitionID == TRANS_EntryState_UnarmedDamaged0)
            //{
            //    //mMotionController.State.AnimatorStates[mMotionLayer._AnimatorLayerIndex].MotionPhaseX = 0;
            //    //mMotionController.State.AnimatorStates[mMotionLayer._AnimatorLayerIndex].AutoClearMotionPhase = true;
            //    mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, 0, 0, true);
            //}

            // Determine how we'll recover
            //if (mMotionLayer._AnimatorStateID == STATE_UnarmedDamaged0)
            //{
                if (mActorController.State.Stance == EnumControllerStance.TRAVERSAL)
                {
                    mMotionController.SetAnimatorMotionParameter(mMotionLayer._AnimatorLayerIndex, 0);
                }
                else
                {
                    mMotionController.SetAnimatorMotionParameter(mMotionLayer._AnimatorLayerIndex, 1);
                }
            //}
        }

        /// <summary>
        /// Look at the incoming message to determine if it means we should react
        /// </summary>
        /// <param name="rMessage"></param>
        public override void OnMessageReceived(IMessage rMessage)
        {
            if (rMessage == null) { return; }
            if (rMessage.IsHandled) { return; }
            if (mActorController.State.Stance != EnumControllerStance.TRAVERSAL) { return; }

            if (rMessage is CombatMessage)
            {
                CombatMessage lCombatMessage = rMessage as CombatMessage;

                // Attack messages
                if (lCombatMessage.Attacker == mMotionController.gameObject)
                {
                }
                // Defender messages
                else if (lCombatMessage.Defender == mMotionController.gameObject)
                {
                    if (rMessage.ID == CombatMessage.MSG_DEFENDER_DAMAGED)
                    {
                        if (!mIsActive)
                        {
                            Vector3 lLocalPosition = mMotionController._Transform.InverseTransformPoint(lCombatMessage.HitPoint);
                            Vector3 lLocalDirection = (lLocalPosition - Vector3.zero).normalized;
                            float lAttackAngle = Vector3Ext.HorizontalAngleTo(Vector3.forward, lLocalDirection, Vector3.up);

                            mMotionController.ActivateMotion(this, (int)lAttackAngle);
                        }
                        else
                        {
                            mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, PHASE_START, Parameter, true);
                        }

                        rMessage.IsHandled = true;
                    }
                }
            }
            else if (rMessage is DamageMessage)
            {
                if (rMessage.ID == CombatMessage.MSG_DEFENDER_DAMAGED)
                {
                    mMotionController.ActivateMotion(this, 0);
                    rMessage.IsHandled = true;
                }
            }
        }

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

            if (EditorHelper.TextField("Action Alias", "Action alias that starts the action and then exits the action (mostly for debugging).", ActionAlias, mMotionController))
            {
                lIsDirty = true;
                ActionAlias = EditorHelper.FieldStringValue;
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
        public int STATE_Empty = -1;
        public int STATE_UnarmedDamaged0 = -1;
        public int TRANS_AnyState_UnarmedDamaged0 = -1;
        public int TRANS_EntryState_UnarmedDamaged0 = -1;

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
                    if (lStateID == STATE_UnarmedDamaged0) { return true; }
                }

                if (lTransitionID == TRANS_AnyState_UnarmedDamaged0) { return true; }
                if (lTransitionID == TRANS_EntryState_UnarmedDamaged0) { return true; }
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
            if (rStateID == STATE_UnarmedDamaged0) { return true; }
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
                if (rStateID == STATE_UnarmedDamaged0) { return true; }
            }

            if (rTransitionID == TRANS_AnyState_UnarmedDamaged0) { return true; }
            if (rTransitionID == TRANS_EntryState_UnarmedDamaged0) { return true; }
            return false;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            string lLayer = mMotionController.Animator.GetLayerName(mMotionLayer._AnimatorLayerIndex);
            TRANS_AnyState_UnarmedDamaged0 = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".BasicDamaged-SM.Unarmed Damaged 0");
            TRANS_EntryState_UnarmedDamaged0 = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".BasicDamaged-SM.Unarmed Damaged 0");
            STATE_Empty = mMotionController.AddAnimatorName("" + lLayer + ".Empty");
            STATE_UnarmedDamaged0 = mMotionController.AddAnimatorName("" + lLayer + ".BasicDamaged-SM.Unarmed Damaged 0");
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

            UnityEditor.Animations.AnimatorStateMachine lSSM_N181712 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "BasicDamaged-SM");
            if (lSSM_N181712 == null) { lSSM_N181712 = lLayerStateMachine.AddStateMachine("BasicDamaged-SM", new Vector3(192, -960, 0)); }

            UnityEditor.Animations.AnimatorState lState_N256722 = MotionControllerMotion.EditorFindState(lSSM_N181712, "Unarmed Damaged 0");
            if (lState_N256722 == null) { lState_N256722 = lSSM_N181712.AddState("Unarmed Damaged 0", new Vector3(312, -24, 0)); }
            lState_N256722.speed = 3f;
            lState_N256722.mirror = false;
            lState_N256722.tag = "Exit";
            lState_N256722.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Utilities_01.fbx", "Damaged");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_N265182 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_N256722, 0);
            if (lAnyTransition_N265182 == null) { lAnyTransition_N265182 = lLayerStateMachine.AddAnyStateTransition(lState_N256722); }
            lAnyTransition_N265182.isExit = false;
            lAnyTransition_N265182.hasExitTime = false;
            lAnyTransition_N265182.hasFixedDuration = true;
            lAnyTransition_N265182.exitTime = 0.75f;
            lAnyTransition_N265182.duration = 0.1f;
            lAnyTransition_N265182.offset = 0.106185f;
            lAnyTransition_N265182.mute = false;
            lAnyTransition_N265182.solo = false;
            lAnyTransition_N265182.canTransitionToSelf = true;
            lAnyTransition_N265182.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_N265182.conditions.Length - 1; i >= 0; i--) { lAnyTransition_N265182.RemoveCondition(lAnyTransition_N265182.conditions[i]); }
            lAnyTransition_N265182.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3350f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_N265182.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");

#if USE_ARCHERY_MP || OOTII_AYMP
            ArcheryPackDefinition.ExtendBasicDamaged(rMotionController, rLayerIndex);
#endif

#if USE_SWORD_SHIELD_MP || OOTII_SSMP
            SwordShieldPackDefinition.ExtendBasicDamaged(rMotionController, rLayerIndex);
#endif

#if USE_SPELL_CASTING_MP || OOTII_SCMP
            //SpellCastingPackDefinition.ExtendBasicDamaged(rMotionController, rLayerIndex);
#endif

            // Run any post processing after creating the state machine
            OnStateMachineCreated();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}
