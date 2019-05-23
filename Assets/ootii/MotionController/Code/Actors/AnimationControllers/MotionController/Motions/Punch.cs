using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using com.ootii.Base;
using com.ootii.Cameras;
using com.ootii.Helpers;
using com.ootii.Input;
using com.ootii.Utilities.Debug;

namespace com.ootii.Actors.AnimationControllers
{
    /// <summary>
    /// This is a simple punch used to test using different
    /// motions at the same time with MotionLayers.
    /// </summary>
    [MotionName("Punch")]
    [MotionDescription("A simple motion used to test motions on different layers. When put on a seperate layer, " +
                   "this motion will cause the avatar to punch with his left hand.")]
    public class Punch : MotionControllerMotion
    {
        // Enum values for the motion
        public const int PHASE_UNKNOWN = 0;
        public const int PHASE_START = 500;

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
        public Punch()
            : base()
        {
            _Priority = 10;
            _ActionAlias = "Fire1";
            mIsStartable = true;

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "Punch-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public Punch(MotionController rController)
            : base(rController)
        {
            _Priority = 10;
            _ActionAlias = "Fire1";
            mIsStartable = true;

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "Punch-SM"; }
#endif
        }

        /// <summary>
        /// Tests if this motion should be started. However, the motion
        /// isn't actually started.
        /// </summary>
        /// <returns></returns>
        public override bool TestActivate()
        {
            // Handle the input processing here for now
            if (mMotionController._InputSource != null && _ActionAlias.Length > 0 && mMotionController._InputSource.IsJustPressed(_ActionAlias))
            {
                return true;
            }         

            // Get out
            return false;
        }


        /// <summary>
        /// Tests if the motion should continue. If it shouldn't, the motion
        /// is typically disabled
        /// </summary>
        /// <returns></returns>
        public override bool TestUpdate()
        {
            // If we just entered this frame, stay
            if (mIsActivatedFrame)
            {
                return true;
            }

            // Cancel if we're not in the motion
            if (mIsAnimatorActive && !IsInMotionState)
            {
                return false;
            }

            // Once we've exceeded the motion time, get out
            if (mIsAnimatorActive && mMotionLayer._AnimatorStateID == STATE_Punch && mMotionLayer._AnimatorStateNormalizedTime > 0.8f)
            {
                return false;
            }

            // Stay
            return true;
        }
        
        /// <summary>
        /// Called to start the specific motion. If the motion
        /// were something like 'jump', this would start the jumping process
        /// </summary>
        /// <param name="rPrevMotion">Motion that this motion is taking over from</param>
        public override bool Activate(MotionControllerMotion rPrevMotion)
        {
            mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, Punch.PHASE_START, true);
            return base.Activate(rPrevMotion);
        }

#if UNITY_EDITOR

        /// <summary>
        /// Allow the motion to render it's own GUI
        /// </summary>
        public override bool OnInspectorGUI()
        {
            bool lIsDirty = false;

            if (EditorHelper.TextField("Action Alias", "Action alias that triggers a punch. Clear the value to not use input.", ActionAlias, mMotionController))
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
        public static int STATE_Empty = -1;
        public static int STATE_Punch = -1;
        public static int TRANS_AnyState_Punch = -1;
        public static int TRANS_EntryState_Punch = -1;
        public static int TRANS_Punch_Empty = -1;

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
                    if (lStateID == STATE_Punch) { return true; }
                }

                if (lTransitionID == TRANS_AnyState_Punch) { return true; }
                if (lTransitionID == TRANS_EntryState_Punch) { return true; }
                if (lTransitionID == TRANS_Punch_Empty) { return true; }
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
            if (rStateID == STATE_Punch) { return true; }
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
                if (rStateID == STATE_Punch) { return true; }
            }

            if (rTransitionID == TRANS_AnyState_Punch) { return true; }
            if (rTransitionID == TRANS_EntryState_Punch) { return true; }
            if (rTransitionID == TRANS_Punch_Empty) { return true; }
            return false;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            string lBaseLayer = mMotionController.Animator.GetLayerName(0);
            string lLayer = mMotionController.Animator.GetLayerName(mMotionLayer._AnimatorLayerIndex);
            TRANS_AnyState_Punch = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".Punch-SM.Punch");
            TRANS_EntryState_Punch = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".Punch-SM.Punch");
            STATE_Empty = mMotionController.AddAnimatorName(lBaseLayer + ".Empty");
            STATE_Punch = mMotionController.AddAnimatorName(lLayer + ".Punch-SM.Punch");
            TRANS_Punch_Empty = mMotionController.AddAnimatorName("" + lLayer + ".Punch-SM.Punch -> " + lBaseLayer + ".Empty");
        }

#if UNITY_EDITOR

        private AnimationClip m17128 = null;

        /// <summary>
        /// Creates the animator substate machine for this motion.
        /// </summary>
        protected override void CreateStateMachine()
        {
            // Grab the root sm for the layer
            UnityEditor.Animations.AnimatorStateMachine lRootStateMachine = _EditorAnimatorController.layers[mMotionLayer.AnimatorLayerIndex].stateMachine;
            UnityEditor.Animations.AnimatorStateMachine lSM_23030 = _EditorAnimatorController.layers[mMotionLayer.AnimatorLayerIndex].stateMachine;
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

            UnityEditor.Animations.AnimatorStateMachine lSM_24308 = lRootSubStateMachine;
            if (lSM_24308 != null)
            {
                for (int i = lSM_24308.entryTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_24308.RemoveEntryTransition(lSM_24308.entryTransitions[i]);
                }

                for (int i = lSM_24308.anyStateTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_24308.RemoveAnyStateTransition(lSM_24308.anyStateTransitions[i]);
                }

                for (int i = lSM_24308.states.Length - 1; i >= 0; i--)
                {
                    lSM_24308.RemoveState(lSM_24308.states[i].state);
                }

                for (int i = lSM_24308.stateMachines.Length - 1; i >= 0; i--)
                {
                    lSM_24308.RemoveStateMachine(lSM_24308.stateMachines[i].stateMachine);
                }
            }
            else
            {
                lSM_24308 = lSM_23030.AddStateMachine(_EditorAnimatorSMName, new Vector3(48, 12, 0));
            }

            UnityEditor.Animations.AnimatorState lS_24312 = lSM_24308.AddState("Punch", new Vector3(276, 12, 0));
            lS_24312.speed = 1f;
            lS_24312.motion = m17128;

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_24310 = lRootStateMachine.AddAnyStateTransition(lS_24312);
            lT_24310.hasExitTime = false;
            lT_24310.hasFixedDuration = false;
            lT_24310.exitTime = 0.9f;
            lT_24310.duration = 0.1f;
            lT_24310.offset = 0f;
            lT_24310.mute = false;
            lT_24310.solo = false;
            lT_24310.canTransitionToSelf = true;
            lT_24310.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_24310.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 500f, "L1MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_24314 = lS_24312.AddTransition(lRootStateMachine);
            lT_24314.hasExitTime = true;
            lT_24314.hasFixedDuration = true;
            lT_24314.exitTime = 0.8f;
            lT_24314.duration = 0.2f;
            lT_24314.offset = 0f;
            lT_24314.mute = false;
            lT_24314.solo = false;
            lT_24314.canTransitionToSelf = true;
            lT_24314.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

        }

        /// <summary>
        /// Gathers the animations so we can use them when creating the sub-state machine.
        /// </summary>
        public override void FindAnimations()
        {
            m17128 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Fighting/ootii_Punch.fbx/Punch.anim", "Punch");

            // Add the remaining functionality
            base.FindAnimations();
        }

        /// <summary>
        /// Used to show the settings that allow us to generate the animator setup.
        /// </summary>
        public override void OnSettingsGUI()
        {
            UnityEditor.EditorGUILayout.IntField(new GUIContent("Phase ID", "Phase ID used to transition to the state."), PHASE_START);
            m17128 = CreateAnimationField("Punch", "Assets/ootii/MotionController/Content/Animations/Humanoid/Fighting/ootii_Punch.fbx/Punch.anim", "Punch", m17128);

            // Add the remaining functionality
            base.OnSettingsGUI();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}
