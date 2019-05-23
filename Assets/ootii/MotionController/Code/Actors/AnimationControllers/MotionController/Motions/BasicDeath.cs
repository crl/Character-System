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
    /// Generic death motion
    /// </summary>
    [MotionName("Basic Death")]
    [MotionDescription("Support generic animations for dying.")]
    public class BasicDeath : MotionControllerMotion
    {
        // Enum values for the motion
        public int PHASE_UNKNOWN = 0;
        public int PHASE_START = 3375;

        /// <summary>
        /// Determines if we're using the IsInMotion() function to verify that
        /// the transition in the animator has occurred for this motion.
        /// </summary>
        public override bool VerifyTransition
        {
            get { return false; }
        }

        /// <summary>
        /// Determines if we remove the body shapes on death
        /// </summary>
        public bool _RemoveBodyShapes = false;
        public bool RemoveBodyShapes
        {
            get { return _RemoveBodyShapes; }
            set { _RemoveBodyShapes = value; }
        }

        /// <summary>
        /// Determines if we've removed all the colliders as needed
        /// </summary>
        private bool mIsClean = false;

        /// <summary>
        /// Helps us to determine if we are in this motion or not
        /// </summary>
        private int mActivateTransitionID = -1;

        /// <summary>
        /// Default constructor
        /// </summary>
        public BasicDeath()
            : base()
        {
            _Category = EnumMotionCategories.DEATH;

            _Priority = 100;
            _ActionAlias = "";
            _OverrideLayers = true;

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "BasicDeath-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public BasicDeath(MotionController rController)
            : base(rController)
        {
            _Category = EnumMotionCategories.DEATH;

            _Priority = 100;
            _ActionAlias = "";
            _OverrideLayers = true;

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "BasicDeath-SM"; }
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
            return true;
        }

        /// <summary>
        /// Raised when a motion is being interrupted by another motion
        /// </summary>
        /// <param name="rMotion">Motion doing the interruption</param>
        /// <returns>Boolean determining if it can be interrupted</returns>
        public override bool TestInterruption(MotionControllerMotion rMotion)
        {
            return false;
        }

        /// <summary>
        /// Called to start the specific motion. If the motion
        /// were something like 'jump', this would start the jumping process
        /// </summary>
        /// <param name="rPrevMotion">Motion that this motion is taking over from</param>
        public override bool Activate(MotionControllerMotion rPrevMotion)
        {
            mIsClean = false;
            mActivateTransitionID = mMotionLayer._AnimatorTransitionID;

            // Allow the character to be pushed by other colliders
            mActorController.AllowPushback = true;

            // Activate the motion
            int lForm = (_Form > 0 ? _Form : mMotionController.CurrentForm);

#if OOTII_SHMP
            if (lForm == 500 || lForm == 550) { lForm = 0; }
#endif

            mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, PHASE_START, lForm, Parameter, false);

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
            // In some cases, we may force the death while in this utility motion. So, we'll clear the phase ourselves
            if (mMotionLayer._AnimatorTransitionID != mActivateTransitionID)
            {
                mActivateTransitionID = -1;
                mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, 0, 0, true);
            }

            // Determine how we'll recover
            if (mActivateTransitionID == -1 && mMotionLayer._AnimatorTransitionID == 0)
            {
                if (!mIsClean && mMotionLayer._AnimatorStateNormalizedTime > 0.6f)
                {
                    mIsClean = true;

                    mActorController.AllowPushback = false;

                    if (_RemoveBodyShapes)
                    {
                        mActorController.RemoveBodyShapes();
                    }
                }
            }
        }

        /// <summary>
        /// Look at the incoming message to determine if it means we should react
        /// </summary>
        /// <param name="rMessage"></param>
        public override void OnMessageReceived(IMessage rMessage)
        {
            if (mIsActive) { return; }
            if (rMessage == null) { return; }
            if (rMessage.IsHandled) { return; }
            //if (mActorController.State.Stance != EnumControllerStance.TRAVERSAL) { return; }

            if (rMessage is CombatMessage)
            {
                CombatMessage lCombatMessage = rMessage as CombatMessage;

                // Defender messages
                if (lCombatMessage.Defender == mMotionController.gameObject)
                {
                    if (rMessage.ID == CombatMessage.MSG_DEFENDER_KILLED)
                    {
                        if (!mIsActive)
                        {
                            Vector3 lLocalPosition = mMotionController._Transform.InverseTransformPoint(lCombatMessage.HitPoint);
                            Vector3 lLocalDirection = (lLocalPosition - Vector3.zero).normalized;
                            float lAttackAngle = Vector3Ext.HorizontalAngleTo(Vector3.forward, lLocalDirection, Vector3.up);

                            mMotionController.ActivateMotion(this, (int)lAttackAngle);

                            rMessage.IsHandled = true;
                        }
                    }
                }
                // Attack messages
                else if (lCombatMessage.Attacker == mMotionController.gameObject)
                {
                }
            }
            else if (rMessage is DamageMessage)
            {
                if (rMessage.ID == CombatMessage.MSG_DEFENDER_KILLED)
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

            if (EditorHelper.TextField("Action Alias", "Action alias that starts the action and then exits the action (mostly for debugging).", ActionAlias, mMotionController))
            {
                lIsDirty = true;
                ActionAlias = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.IntField("Form", "Sets the LXMotionForm animator property to determine the animation for the motion. If value is < 0, we use the Actor Core's 'Default Form' state.", Form, mMotionController))
            {
                lIsDirty = true;
                Form = EditorHelper.FieldIntValue;
            }

            if (EditorHelper.BoolField("Remove Body Shapes", "Determines if we remove the body shapes on death.", RemoveBodyShapes, mMotionController))
            {
                lIsDirty = true;
                RemoveBodyShapes = EditorHelper.FieldBoolValue;
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
        public int STATE_UnarmedDeath0 = -1;
        public int STATE_UnarmedDeath180 = -1;
        public int TRANS_AnyState_UnarmedDeath0 = -1;
        public int TRANS_EntryState_UnarmedDeath0 = -1;
        public int TRANS_AnyState_UnarmedDeath180 = -1;
        public int TRANS_EntryState_UnarmedDeath180 = -1;

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
                    if (lStateID == STATE_UnarmedDeath0) { return true; }
                    if (lStateID == STATE_UnarmedDeath180) { return true; }
                }

                if (lTransitionID == TRANS_AnyState_UnarmedDeath0) { return true; }
                if (lTransitionID == TRANS_EntryState_UnarmedDeath0) { return true; }
                if (lTransitionID == TRANS_AnyState_UnarmedDeath180) { return true; }
                if (lTransitionID == TRANS_EntryState_UnarmedDeath180) { return true; }
                if (lTransitionID == TRANS_AnyState_UnarmedDeath180) { return true; }
                if (lTransitionID == TRANS_EntryState_UnarmedDeath180) { return true; }
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
            if (rStateID == STATE_UnarmedDeath0) { return true; }
            if (rStateID == STATE_UnarmedDeath180) { return true; }
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
                if (rStateID == STATE_UnarmedDeath0) { return true; }
                if (rStateID == STATE_UnarmedDeath180) { return true; }
            }

            if (rTransitionID == TRANS_AnyState_UnarmedDeath0) { return true; }
            if (rTransitionID == TRANS_EntryState_UnarmedDeath0) { return true; }
            if (rTransitionID == TRANS_AnyState_UnarmedDeath180) { return true; }
            if (rTransitionID == TRANS_EntryState_UnarmedDeath180) { return true; }
            if (rTransitionID == TRANS_AnyState_UnarmedDeath180) { return true; }
            if (rTransitionID == TRANS_EntryState_UnarmedDeath180) { return true; }
            return false;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            string lLayer = mMotionController.Animator.GetLayerName(mMotionLayer._AnimatorLayerIndex);
            TRANS_AnyState_UnarmedDeath0 = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".BasicDeath-SM.Unarmed Death 0");
            TRANS_EntryState_UnarmedDeath0 = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".BasicDeath-SM.Unarmed Death 0");
            TRANS_AnyState_UnarmedDeath180 = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".BasicDeath-SM.Unarmed Death 180");
            TRANS_EntryState_UnarmedDeath180 = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".BasicDeath-SM.Unarmed Death 180");
            TRANS_AnyState_UnarmedDeath180 = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".BasicDeath-SM.Unarmed Death 180");
            TRANS_EntryState_UnarmedDeath180 = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".BasicDeath-SM.Unarmed Death 180");
            STATE_Empty = mMotionController.AddAnimatorName("" + lLayer + ".Empty");
            STATE_UnarmedDeath0 = mMotionController.AddAnimatorName("" + lLayer + ".BasicDeath-SM.Unarmed Death 0");
            STATE_UnarmedDeath180 = mMotionController.AddAnimatorName("" + lLayer + ".BasicDeath-SM.Unarmed Death 180");
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

            UnityEditor.Animations.AnimatorStateMachine lSSM_N237494 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "BasicDeath-SM");
            if (lSSM_N237494 == null) { lSSM_N237494 = lLayerStateMachine.AddStateMachine("BasicDeath-SM", new Vector3(192, -912, 0)); }

            UnityEditor.Animations.AnimatorState lState_N247470 = MotionControllerMotion.EditorFindState(lSSM_N237494, "Unarmed Death 0");
            if (lState_N247470 == null) { lState_N247470 = lSSM_N237494.AddState("Unarmed Death 0", new Vector3(324, -72, 0)); }
            lState_N247470.speed = 1.5f;
            lState_N247470.mirror = false;
            lState_N247470.tag = "";
            lState_N247470.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Utilities_01.fbx", "DeathBackward");

            UnityEditor.Animations.AnimatorState lState_N247472 = MotionControllerMotion.EditorFindState(lSSM_N237494, "Unarmed Death 180");
            if (lState_N247472 == null) { lState_N247472 = lSSM_N237494.AddState("Unarmed Death 180", new Vector3(324, -24, 0)); }
            lState_N247472.speed = 1.8f;
            lState_N247472.mirror = false;
            lState_N247472.tag = "";
            lState_N247472.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Utilities/Utilities_01.fbx", "DeathForward");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_N299372 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_N247470, 0);
            if (lAnyTransition_N299372 == null) { lAnyTransition_N299372 = lLayerStateMachine.AddAnyStateTransition(lState_N247470); }
            lAnyTransition_N299372.isExit = false;
            lAnyTransition_N299372.hasExitTime = false;
            lAnyTransition_N299372.hasFixedDuration = true;
            lAnyTransition_N299372.exitTime = 0.75f;
            lAnyTransition_N299372.duration = 0.1f;
            lAnyTransition_N299372.offset = 0.115787f;
            lAnyTransition_N299372.mute = false;
            lAnyTransition_N299372.solo = false;
            lAnyTransition_N299372.canTransitionToSelf = true;
            lAnyTransition_N299372.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_N299372.conditions.Length - 1; i >= 0; i--) { lAnyTransition_N299372.RemoveCondition(lAnyTransition_N299372.conditions[i]); }
            lAnyTransition_N299372.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3375f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_N299372.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");
            lAnyTransition_N299372.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, -100f, "L" + rLayerIndex + "MotionParameter");
            lAnyTransition_N299372.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 100f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_N299806 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_N247472, 0);
            if (lAnyTransition_N299806 == null) { lAnyTransition_N299806 = lLayerStateMachine.AddAnyStateTransition(lState_N247472); }
            lAnyTransition_N299806.isExit = false;
            lAnyTransition_N299806.hasExitTime = false;
            lAnyTransition_N299806.hasFixedDuration = true;
            lAnyTransition_N299806.exitTime = 0.75f;
            lAnyTransition_N299806.duration = 0.1f;
            lAnyTransition_N299806.offset = 0.115787f;
            lAnyTransition_N299806.mute = false;
            lAnyTransition_N299806.solo = false;
            lAnyTransition_N299806.canTransitionToSelf = true;
            lAnyTransition_N299806.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_N299806.conditions.Length - 1; i >= 0; i--) { lAnyTransition_N299806.RemoveCondition(lAnyTransition_N299806.conditions[i]); }
            lAnyTransition_N299806.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3375f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_N299806.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");
            lAnyTransition_N299806.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 100f, "L" + rLayerIndex + "MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_N300182 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_N247472, 1);
            if (lAnyTransition_N300182 == null) { lAnyTransition_N300182 = lLayerStateMachine.AddAnyStateTransition(lState_N247472); }
            lAnyTransition_N300182.isExit = false;
            lAnyTransition_N300182.hasExitTime = false;
            lAnyTransition_N300182.hasFixedDuration = true;
            lAnyTransition_N300182.exitTime = 0.75f;
            lAnyTransition_N300182.duration = 0.1f;
            lAnyTransition_N300182.offset = 0.115787f;
            lAnyTransition_N300182.mute = false;
            lAnyTransition_N300182.solo = false;
            lAnyTransition_N300182.canTransitionToSelf = true;
            lAnyTransition_N300182.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_N300182.conditions.Length - 1; i >= 0; i--) { lAnyTransition_N300182.RemoveCondition(lAnyTransition_N300182.conditions[i]); }
            lAnyTransition_N300182.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3375f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_N300182.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");
            lAnyTransition_N300182.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, -100f, "L" + rLayerIndex + "MotionParameter");

#if USE_ARCHERY_MP || OOTII_AYMP
            ArcheryPackDefinition.ExtendBasicDeath(rMotionController, rLayerIndex);
#endif

#if USE_SWORD_SHIELD_MP || OOTII_SSMP
            SwordShieldPackDefinition.ExtendBasicDeath(rMotionController, rLayerIndex);
#endif

#if USE_SPELL_CASTING_MP || OOTII_SCMP
            //SpellCastingPackDefinition.ExtendBasicDeath(rMotionController, rLayerIndex);
#endif

            // Run any post processing after creating the state machine
            OnStateMachineCreated();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}
