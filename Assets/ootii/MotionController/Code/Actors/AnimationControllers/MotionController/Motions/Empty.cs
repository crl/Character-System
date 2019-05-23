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
    /// Idle motion for when the character is just standing and waiting
    /// for input or some interaction.
    /// </summary>
    [MotionName("Empty")]
    [MotionDescription("Used on additional layers as the 'empty' motion to clear out any animation so that the base layer can be full-body.")]
    public class Empty : MotionControllerMotion
    {
        /// <summary>
        /// Trigger values for the motion
        /// </summary>
        public int PHASE_UNKNOWN = 0;
        public int PHASE_START = 3010;

        /// <summary>
        /// Default constructor
        /// </summary>
        public Empty()
            : base()
        {
            _Category = EnumMotionCategories.IDLE;

            _Priority = 0;
            _Form = 0;

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "Empty-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public Empty(MotionController rController)
            : base(rController)
        {
            _Category = EnumMotionCategories.IDLE;

            _Priority = 0;
            _Form = 0;

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "Empty-SM"; }
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
            // This is a catch all. If there are no motions found to match
            // the controller's state, we default to this motion.
            if (mMotionLayer.ActiveMotion == null)
            {
                return true;
            }

            // Handle the disqualifiers
            if (!mIsStartable) { return false; }
            if (!mMotionController.IsGrounded) { return false; }
            if (mMotionController.State.InputMagnitudeTrend.Average != 0f) { return false; }

            return true;
        }

        /// <summary>
        /// Tests if the motion should continue. If it shouldn't, the motion
        /// is typically disabled
        /// </summary>
        /// <returns></returns>
        public override bool TestUpdate()
        {
            if (mIsAnimatorActive && !IsInMotionState)
            {
                return false;
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
            mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, _Form, mParameter, true);

            // Finalize the activation
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
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        /// <param name="rMovement">Amount of movement caused by root motion this frame</param>
        /// <param name="rRotation">Amount of rotation caused by root motion this frame</param>
        /// <returns></returns>
        public override void UpdateRootMotion(float rDeltaTime, int rUpdateIndex, ref Vector3 rMovement, ref Quaternion rRotation)
        {
            rMovement = Vector3.zero;
            rRotation = Quaternion.identity;
        }

        /// <summary>
        /// Updates the motion over time. This is called by the controller
        /// every update cycle so animations and stages can be updated.
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        public override void Update(float rDeltaTime, int rUpdateIndex)
        {
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

            if (EditorHelper.IntField("Form", "Within the animator state, defines which animator flow will run. This is used to add new animations.", Form, mMotionController))
            {
                lIsDirty = true;
                Form = EditorHelper.FieldIntValue;
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
        public int STATE_Empty = -1;
        public int STATE_EmptyPose = -1;
        public int TRANS_AnyState_EmptyPose = -1;
        public int TRANS_EntryState_EmptyPose = -1;

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
                    if (lStateID == STATE_EmptyPose) { return true; }
                }

                if (lTransitionID == TRANS_AnyState_EmptyPose) { return true; }
                if (lTransitionID == TRANS_EntryState_EmptyPose) { return true; }
                if (lTransitionID == TRANS_AnyState_EmptyPose) { return true; }
                if (lTransitionID == TRANS_EntryState_EmptyPose) { return true; }
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
            if (rStateID == STATE_EmptyPose) { return true; }
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
                if (rStateID == STATE_EmptyPose) { return true; }
            }

            if (rTransitionID == TRANS_AnyState_EmptyPose) { return true; }
            if (rTransitionID == TRANS_EntryState_EmptyPose) { return true; }
            if (rTransitionID == TRANS_AnyState_EmptyPose) { return true; }
            if (rTransitionID == TRANS_EntryState_EmptyPose) { return true; }
            return false;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            string lLayer = mMotionController.Animator.GetLayerName(mMotionLayer._AnimatorLayerIndex);
            TRANS_AnyState_EmptyPose = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".Empty-SM.EmptyPose");
            TRANS_EntryState_EmptyPose = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".Empty-SM.EmptyPose");
            TRANS_AnyState_EmptyPose = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".Empty-SM.EmptyPose");
            TRANS_EntryState_EmptyPose = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".Empty-SM.EmptyPose");
            STATE_Empty = mMotionController.AddAnimatorName("" + lLayer + ".Empty");
            STATE_EmptyPose = mMotionController.AddAnimatorName("" + lLayer + ".Empty-SM.EmptyPose");
        }

#if UNITY_EDITOR


        /// <summary>
        /// Creates the animator substate machine for this motion.
        /// </summary>
        protected override void CreateStateMachine()
        {
            // Grab the root sm for the layer
            UnityEditor.Animations.AnimatorStateMachine lRootStateMachine = _EditorAnimatorController.layers[mMotionLayer.AnimatorLayerIndex].stateMachine;
            UnityEditor.Animations.AnimatorStateMachine lSM_34270 = _EditorAnimatorController.layers[mMotionLayer.AnimatorLayerIndex].stateMachine;
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

            UnityEditor.Animations.AnimatorStateMachine lSM_35666 = lRootSubStateMachine;
            if (lSM_35666 != null)
            {
                for (int i = lSM_35666.entryTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_35666.RemoveEntryTransition(lSM_35666.entryTransitions[i]);
                }

                for (int i = lSM_35666.anyStateTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_35666.RemoveAnyStateTransition(lSM_35666.anyStateTransitions[i]);
                }

                for (int i = lSM_35666.states.Length - 1; i >= 0; i--)
                {
                    lSM_35666.RemoveState(lSM_35666.states[i].state);
                }

                for (int i = lSM_35666.stateMachines.Length - 1; i >= 0; i--)
                {
                    lSM_35666.RemoveStateMachine(lSM_35666.stateMachines[i].stateMachine);
                }
            }
            else
            {
                lSM_35666 = lSM_34270.AddStateMachine(_EditorAnimatorSMName, new Vector3(192, -480, 0));
            }

            UnityEditor.Animations.AnimatorState lS_35692 = lSM_35666.AddState("EmptyPose", new Vector3(312, 84, 0));
            lS_35692.speed = 1f;

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_35676 = lRootStateMachine.AddAnyStateTransition(lS_35692);
            lT_35676.hasExitTime = false;
            lT_35676.hasFixedDuration = true;
            lT_35676.exitTime = 0.75f;
            lT_35676.duration = 0.15f;
            lT_35676.offset = 0f;
            lT_35676.mute = false;
            lT_35676.solo = false;
            lT_35676.canTransitionToSelf = false;
            lT_35676.orderedInterruption = false;
            lT_35676.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)2;
            lT_35676.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3010f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");
            lT_35676.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionForm");
            lT_35676.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_35678 = lRootStateMachine.AddAnyStateTransition(lS_35692);
            lT_35678.hasExitTime = false;
            lT_35678.hasFixedDuration = true;
            lT_35678.exitTime = 0.75f;
            lT_35678.duration = 0f;
            lT_35678.offset = 0f;
            lT_35678.mute = false;
            lT_35678.solo = false;
            lT_35678.canTransitionToSelf = false;
            lT_35678.orderedInterruption = false;
            lT_35678.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)2;
            lT_35678.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3010f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionPhase");
            lT_35678.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionForm");
            lT_35678.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + mMotionLayer._AnimatorLayerIndex + "MotionParameter");

        }

        /// <summary>
        /// Gathers the animations so we can use them when creating the sub-state machine.
        /// </summary>
        public override void FindAnimations()
        {

            // Add the remaining functionality
            base.FindAnimations();
        }

        /// <summary>
        /// Used to show the settings that allow us to generate the animator setup.
        /// </summary>
        public override void OnSettingsGUI()
        {
            UnityEditor.EditorGUILayout.IntField(new GUIContent("Phase ID", "Phase ID used to transition to the state."), PHASE_START);

            // Add the remaining functionality
            base.OnSettingsGUI();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}
