using UnityEngine;
using com.ootii.Helpers;

namespace com.ootii.Actors.AnimationControllers
{
    /// <summary>
    /// Arms Only motion for managing arms while under cover
    /// </summary>
    [MotionName("Basic Idle Arms")]
    [MotionDescription("Manages the posing of the arms. Meant to be on the Arms Only Layer.")]
    public class BasicIdleArms : MotionControllerMotion
    {
        /// <summary>
        /// Trigger values for th emotion
        /// </summary>
        public int PHASE_UNKNOWN = 0;
        public int PHASE_START = 3650;
        public int PHASE_STOP = 3610;

        /// <summary>
        /// Optional "Form" or "Style" required for this motion to activate
        /// </summary>
        public string _RequiredForms = "500,550";
        public string RequiredForms
        {
            get { return _RequiredForms; }

            set
            {
                _RequiredForms = value;

                if (Application.isPlaying)
                {
                    mRequiredForms = null;

                    if (_RequiredForms.Length > 0)
                    {
                        string[] lRequiredForms = _RequiredForms.Split(',');
                        mRequiredForms = new int[lRequiredForms.Length];

                        for (int i = 0; i < lRequiredForms.Length; i++)
                        {
                            mRequiredForms[i] = -1;
                            int.TryParse(lRequiredForms[i], out mRequiredForms[i]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Determines if we're using the IsInMotion() function to verify that
        /// the transition in the animator has occurred for this motion.
        /// </summary>
        public override bool VerifyTransition
        {
            get { return false; }
        }

        // Used to force a change if neede
        protected int mActiveForm = 0;

        // Strings representing the required forms
        protected int[] mRequiredForms = null;

        /// <summary>
        /// Default constructor
        /// </summary>
        public BasicIdleArms()
            : base()
        {
#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "BasicIdleArms-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public BasicIdleArms(MotionController rController)
            : base(rController)
        {
#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "BasicIdleArms-SM"; }
#endif
        }

        /// <summary>
        /// Awake is called after all objects are initialized so you can safely speak to other objects. This is where
        /// reference can be associated.
        /// </summary>
        public override void Awake()
        {
            base.Awake();
            RequiredForms = _RequiredForms;
        }

        /// <summary>
        /// Tests if this motion should be started. This is called externally.
        /// </summary>
        /// <returns>Determines if the motion should be started</returns>
        public bool TestActivate(int rForm)
        {
            bool lIsFound = (mRequiredForms == null);

            // Check if we're in the required form
            if (!lIsFound)
            {
                for (int i = 0; i < mRequiredForms.Length; i++)
                {
                    if (mRequiredForms[i] == rForm)
                    {
                        lIsFound = true;
                        break;
                    }
                }
            }

            // Return the results
            return lIsFound;
        }

        /// <summary>
        /// Tests if the motion should continue. If it shouldn't, the motion
        /// is typically disabled
        /// </summary>
        /// <returns></returns>
        public override bool TestUpdate()
        {
            // Check if we're in the required form
            if (mRequiredForms != null)
            {
                int lCurrentForm = mMotionController.CurrentForm;
                for (int i = 0; i < mRequiredForms.Length; i++)
                {
                    if (mRequiredForms[i] == lCurrentForm)
                    {
                        return true;
                    }
                }

                return false;
            }

            // Stay in
            return true;
        }

        /// <summary>
        /// Called to start the specific motion. If the motion
        /// were something like 'jump', this would start the jumping process
        /// </summary>
        /// <param name="rPrevMotion">Motion that this motion is taking over from</param>
        public override bool Activate(MotionControllerMotion rPrevMotion)
        {
            mActiveForm = (_Form >= 0 ? _Form : mMotionController.CurrentForm);
            mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, mActiveForm, 0, true);

            // Finalize the activation
            return base.Activate(rPrevMotion);
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

            if (EditorHelper.TextField("Required Forms", "Comma delimited list of forms that this motion will activate for.", RequiredForms, mMotionController))
            {
                lIsDirty = true;
                RequiredForms = EditorHelper.FieldStringValue;
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
        public int STATE_Empty = -1;
        public int STATE_UnarmedIdlePose = -1;
        public int TRANS_AnyState_UnarmedIdlePose = -1;
        public int TRANS_EntryState_UnarmedIdlePose = -1;

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
                    if (lStateID == STATE_UnarmedIdlePose) { return true; }
                }

                if (lTransitionID == TRANS_AnyState_UnarmedIdlePose) { return true; }
                if (lTransitionID == TRANS_EntryState_UnarmedIdlePose) { return true; }
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
            if (rStateID == STATE_UnarmedIdlePose) { return true; }
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
                if (rStateID == STATE_UnarmedIdlePose) { return true; }
            }

            if (rTransitionID == TRANS_AnyState_UnarmedIdlePose) { return true; }
            if (rTransitionID == TRANS_EntryState_UnarmedIdlePose) { return true; }
            return false;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            string lLayer = mMotionController.Animator.GetLayerName(mMotionLayer._AnimatorLayerIndex);
            TRANS_AnyState_UnarmedIdlePose = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".BasicIdleArms-SM.Unarmed Idle Pose");
            TRANS_EntryState_UnarmedIdlePose = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".BasicIdleArms-SM.Unarmed Idle Pose");
            STATE_Empty = mMotionController.AddAnimatorName("" + lLayer + ".Empty");
            STATE_UnarmedIdlePose = mMotionController.AddAnimatorName("" + lLayer + ".BasicIdleArms-SM.Unarmed Idle Pose");
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

            UnityEditor.Animations.AnimatorStateMachine lSSM_N2456644 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "BasicIdleArms-SM");
            if (lSSM_N2456644 == null) { lSSM_N2456644 = lLayerStateMachine.AddStateMachine("BasicIdleArms-SM", new Vector3(192, -816, 0)); }

            // Run any post processing after creating the state machine
            OnStateMachineCreated();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}

