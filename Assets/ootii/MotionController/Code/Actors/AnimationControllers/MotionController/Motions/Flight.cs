// ********************************************************************************
// Use this file a a starting point for creating your own Motion Controller motion.
// These motions will be compatable with the Motion Controller v2 found here:
// https://www.assetstore.unity3d.com/en/#!/content/15672
//
// This file will also be used by the Motion Builder's Guide found here:
// http://www.ootii.com/unity/motioncontroller/MCMotionBuilder.pdf
//
// As always, feel free to ask questions on the forum here:
// http://forum.unity3d.com/threads/motion-controller.229900
//
// 
// This template isn't the end-all be-all of Motion Building, but it should be a
// good place to start. Simply look for these code sections
// ********************************************************************************
using UnityEngine;
using com.ootii.Actors.AnimationControllers;

#if UNITY_EDITOR
using UnityEditor;
#endif

// ********************************************************************************
// 0. Copy this file and rename it something associated with your motion.
//
//    You want to modify that new file and keep this file intact for future
//    motions you want to create.
// ********************************************************************************

// ********************************************************************************
// 1. Namespaces are like "last names" for code. It helps distinguish between two
//    different classes that may have the same name.
//
//    So, replace the ootii namespace with your own. It can be anything you want.
//    For example, let's say your name is Joe Smith. You could use something like:
//    'namespace smith.motions'
// ********************************************************************************
namespace com.ootii.Actors.AnimationControllers
{
    // ********************************************************************************
    // 2. Replace 'MotionTemplate' class name with the name of your motion. In this example, 
    //    let's assume you are creating a duck motion. So, we'll put 'Duck'.
    //
    // 3. Remove the 'abstract' keyword from the class. This way it shows up in the list
    //    of motions.
    //
    // 4. Add a friendly name for the 'MotionName' attribute. In our example, you could put:
    //    '[MotionName("Smith Duck")]
    //
    // 5. Add a friendly description for the 'MotionDescription' attribute.
    // ********************************************************************************
    /// <summary>
    /// Generic motion that can support any basic mecanim animation
    /// </summary>
    [MotionName("Flight")]
    [MotionDescription("Flight using the InvertRotationOrder.")]
    public class Flight : MotionControllerMotion
    {
        // Enum values for the motion
        public const int PHASE_UNKNOWN = 0;

        // ********************************************************************************
        // 6. Replace the '0' with a valid motion phase ID. When following the Motion
        //    Builder's guide, you created a unique number to be used by the Mecanim
        //    Animator transition condition that starts your animation. That's the number
        //    you want to use here. 
        // ********************************************************************************
        public const int PHASE_START = 30800;

        /// <summary>
        /// Fly speed when moving horizontally with the ground
        /// </summary>
        public float _HorizontalFlySpeed = 10f;
        public float HorizontalFlySpeed
        {
            get { return _HorizontalFlySpeed; }
            set { _HorizontalFlySpeed = value; }
        }

        /// <summary>
        /// Fly speed when moving horizontally with the ground
        /// </summary>
        public float _HorizontalHoverSpeed = 3f;
        public float HorizontalHoverSpeed
        {
            get { return _HorizontalHoverSpeed; }
            set { _HorizontalHoverSpeed = value; }
        }

        /// <summary>
        /// Fly speed when moving perpendicular to the ground
        /// </summary>
        public float _VerticalSpeed = 5f;
        public float VerticalSpeed
        {
            get { return _VerticalSpeed; }
            set { _VerticalSpeed = value; }
        }

        /// <summary>
        /// Desired degrees of rotation per second
        /// </summary>
        public float _RotationSpeed = 120f;
        public float RotationSpeed
        {
            get { return _RotationSpeed; }

            set
            {
                _RotationSpeed = value;
            }
        }

        /// <summary>
        /// Used to apply some smoothing to the mouse movement
        /// </summary>
        public float _RotationSmoothing = 0.1f;
        public virtual float RotationSmoothing
        {
            get { return _RotationSmoothing; }
            set { _RotationSmoothing = value; }
        }

        /// <summary>
        /// Fields to help smooth out the mouse rotation
        /// </summary>
        protected float mYaw = 0f;
        protected float mYawTarget = 0f;
        protected float mYawVelocity = 0f;

        // Additional properties we want to restore on deactivate
        private bool mIsGravityEnabled = true;
        private bool mIsOrientationEnabled = false;
        private bool mIsGroundingLayersEnabled = false;
        private int mGroundLayers = 0;

        // ********************************************************************************
        // 7. Change the class name of this constructor to match your new class. In our
        //    example, it would be 'Duck'
        //
        // 8. Provide a default priority. The higher the priority, the more important the
        //    motion and it will take precidence over other motions that want to activate.
        //
        // 9. Provide the name of your sub-state machine. For example "Riding-SM" or "Idle-SM"
        // ********************************************************************************
        /// <summary>
        /// Default constructor
        /// </summary>
        public Flight()
            : base()
        {
            _Priority = 30;
            _ActionAlias = "Jump";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "Fly-SM"; }
#endif
        }

        // ********************************************************************************
        // 10. Change the class name of this constructor to match your new class. In our
        //    example, it would be 'Duck'
        //
        // 11. Provide a default priority. The higher the priority, the more important the
        //     motion and it will take precidence over other motions that want to activate.
        //
        // 12. Provide the name of your sub-state machine. For example "Riding-SM" or "Idle-SM"
        // ********************************************************************************
        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public Flight(MotionController rController)
            : base(rController)
        {
            _Priority = 30;
            _ActionAlias = "Jump";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "Fly-SM"; }
#endif
        }

        /// <summary>
        /// Allows for any processing after the motion has been deserialized
        /// </summary>
        public override void Initialize()
        {
            // ********************************************************************************
            // 13. Any initialization you need to do for the motion can happen here.
            //
            //     Typically, this function is only called once when the motion is deserialized.
            //     However, it could happen multiple times. So, just be aware of that.
            // ********************************************************************************
        }

        /// <summary>
        /// Tests if this motion should be started. However, the motion
        /// isn't actually started.
        /// </summary>
        /// <returns></returns>
        public override bool TestActivate()
        {
            if (!mIsStartable)
            {
                return false;
            }

            // ********************************************************************************
            // 14. Here, you would add code to determine if it is time for your motion to
            //     activate. If multiple motions can activate, the one with the highest 
            //     priority wins.
            //
            //     When you determine it is time to activate, just return 'true'.
            // ********************************************************************************

            if (mMotionController._InputSource != null)
            {
                if (mMotionController._InputSource.IsJustPressed(_ActionAlias))
                {
                    return true;
                }
            }

            // Return the final result
            return false;
        }

        /// <summary>
        /// Tests if the motion should continue. If it shouldn't, the motion
        /// is typically disabled
        /// </summary>
        /// <returns>Boolean that determines if the motion continues</returns>
        public override bool TestUpdate()
        {
            // Ensure we're in the animation
            if (mIsActivatedFrame) { return true; }

            // ********************************************************************************
            // 15. Here, add code to determine if you should continue to run this motion.
            //
            //     If you've determined it's time to deactivate the motion, just return 'false'.
            // ********************************************************************************
            if (mMotionController._InputSource != null)
            {
                if (mMotionController._InputSource.IsJustPressed(_ActionAlias))
                {
                    return false;
                }
            }

            return true;
        }


        /// <summary>
        /// Raised when a motion is being interrupted by another motion
        /// </summary>
        /// <param name="rMotion">Motion doing the interruption</param>
        /// <returns>Boolean determining if it can be interrupted</returns>
        public override bool TestInterruption(MotionControllerMotion rMotion)
        {
            // ********************************************************************************
            // 16. If another motion with a higher priority wants to activate and deactivate
            //     your motion, you can prevent that from happening here. Simply return 'false'.
            // ********************************************************************************

            return true;
        }

        /// <summary>
        /// Called to start the specific motion. If the motion
        /// were something like 'jump', this would start the jumping process
        /// </summary>
        /// <param name="rPrevMotion">Motion that this motion is taking over from</param>
        public override bool Activate(MotionControllerMotion rPrevMotion)
        {
            // ********************************************************************************
            // 17. If there's code you want to run when the motion first activates, you can
            //     do that here. This function will run once for every time the motion is 
            //     activated.
            // ********************************************************************************

            // Reset the yaw info for smoothing
            mYaw = 0f;
            mYawTarget = 0f;
            mYawVelocity = 0f;

            // Ensure the AC doesn't prevent us from flying
            mIsGravityEnabled = mActorController.IsGravityEnabled;
            mActorController.IsGravityEnabled = false;

            mIsOrientationEnabled = mActorController.OrientToGround;
            mActorController.OrientToGround = false;

            mActorController.FixGroundPenetration = false;

            mIsGroundingLayersEnabled = mActorController.IsGroundingLayersEnabled;
            mActorController.IsGroundingLayersEnabled = true;

            mGroundLayers = mActorController.GroundingLayers;
            mActorController.GroundingLayers = 0;

            // Flip the rotation order
            mActorController.InvertRotationOrder = true;

            // Tell the animator to start your animations
            //mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, PHASE_START, true);

            // Return
            return base.Activate(rPrevMotion);
        }

        /// <summary>
        /// Called to stop the motion. If the motion is stopable. Some motions
        /// like jump cannot be stopped early
        /// </summary>
        public override void Deactivate()
        {
            // ********************************************************************************
            // 18. If there's code you want to run when the motion deactivates, you can
            //     do that here. This function will run once for every time the motion is 
            //     deactivated.
            // ********************************************************************************

            // Re-enable gravity
            mActorController.InvertRotationOrder = false;
            mActorController.IsGravityEnabled = mIsGravityEnabled;
            mActorController.OrientToGround = mIsOrientationEnabled;
            mActorController.FixGroundPenetration = true;
            mActorController.IsGroundingLayersEnabled = mIsGroundingLayersEnabled;
            mActorController.GroundingLayers = mGroundLayers;

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
        public override void UpdateRootMotion(float rDeltaTime, int rUpdateIndex, ref Vector3 rVelocityDelta, ref Quaternion rRotationDelta)
        {
            // ********************************************************************************
            // 19. If you want to add, modify, or remove the animation's root-motion, do 
            //     it here.
            // ********************************************************************************
            rVelocityDelta = Vector3.zero;
            rRotationDelta = Quaternion.identity;
        }

        /// <summary>
        /// Updates the motion over time. This is called by the controller
        /// every update cycle so animations and stages can be updated.
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        public override void Update(float rDeltaTime, int rUpdateIndex)
        {
            // ********************************************************************************
            // 20. If there's code you want to run when your motion is activated and running,
            //     you can do it here.
            //
            //     This function typically runs once per frame.
            // ********************************************************************************

            float lYawDelta = mMotionController._InputSource.ViewX;
            float lPitchDelta = mMotionController._InputSource.ViewY;

            mRotation = Quaternion.Euler(0f, lYawDelta, 0f);
            mTilt = Quaternion.Euler(lPitchDelta, 0f, 0f);

            mMovement = Vector3.zero;
            mMovement.x = mMotionController.State.InputX;
            mMovement.z = mMotionController.State.InputY;
            mMovement = mMovement.normalized * _HorizontalFlySpeed; 

            // Vertical movement
            if (mMotionController._InputSource.IsPressed(KeyCode.Q))
            {
                mMovement.y += -_VerticalSpeed;
            }

            if (mMotionController._InputSource.IsPressed(KeyCode.E))
            {
                mMovement.y += _VerticalSpeed;
            }

            mMovement = (mMotionController._Transform.rotation * (mRotation * mTilt)) * (mMovement * rDeltaTime);
        }

        // ********************************************************************************
        // 21. Auto-generated code...
        //
        //     In a lot of my motions, you'll see code in a section labeled 'auto-generated'.
        //     This isn't manditory, but it helps in a couple of ways:
        //
        //     a. It provides others with a one-click way of setting up the animator 
        //        sub-state machine that goes with this motion. It's useful if you plan
        //        on sharing the motion.
        //
        //     b. It includes some helpful identifiers and functions that makes motion 
        //        building easier.
        //        
        //        IsMotionState() is especially helpful to know what state or transition
        //        the animator is currently in.
        //
        //     To generate the auto-generated code:
        //
        //     a. Finish building out your associated animator sub-state machine.
        //
        //     b. Select the character in your scene and go to the Motion Controller.
        //
        //     c. Add this motion to his list of motions and select it.
        //
        //     d. Press the blue gear icon in the motion details.
        //
        //     e. Open the 'Script Generators' area.
        //
        //     f. Press the 'Generate Script' button. You'll see dialog box pop up.
        //
        //     g. The generated code is now on your clip board. Paste it below.
        // ********************************************************************************

        #region Auto-Generated
        // ************************************ START AUTO GENERATED ************************************

        /// <summary>
        /// These declarations go inside the class so you can test for which state
        /// and transitions are active. Testing hash values is much faster than strings.
        /// </summary>
        public static int TRANS_EntryState_float = -1;
        public static int TRANS_AnyState_float = -1;
        public static int STATE_float = -1;
        public static int TRANS_float_flying = -1;
        public static int STATE_flying = -1;
        public static int TRANS_flying_float = -1;

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

                if (lStateID == STATE_float) { return true; }
                if (lStateID == STATE_flying) { return true; }
                if (lTransitionID == TRANS_EntryState_float) { return true; }
                if (lTransitionID == TRANS_AnyState_float) { return true; }
                if (lTransitionID == TRANS_float_flying) { return true; }
                if (lTransitionID == TRANS_flying_float) { return true; }
                return false;
            }
        }

        /// <summary>
        /// Used to determine if the actor is in one of the states for this motion
        /// </summary>
        /// <returns></returns>
        public override bool IsMotionState(int rStateID)
        {
            if (rStateID == STATE_float) { return true; }
            if (rStateID == STATE_flying) { return true; }
            return false;
        }

        /// <summary>
        /// Used to determine if the actor is in one of the states for this motion
        /// </summary>
        /// <returns></returns>
        public override bool IsMotionState(int rStateID, int rTransitionID)
        {
            if (rStateID == STATE_float) { return true; }
            if (rStateID == STATE_flying) { return true; }
            if (rTransitionID == TRANS_EntryState_float) { return true; }
            if (rTransitionID == TRANS_AnyState_float) { return true; }
            if (rTransitionID == TRANS_float_flying) { return true; }
            if (rTransitionID == TRANS_flying_float) { return true; }
            return false;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            /// <summary>
            /// These assignments go inside the 'LoadAnimatorData' function so that we can
            /// extract and assign the hash values for this run. These are typically used for debugging.
            /// </summary>
            TRANS_EntryState_float = mMotionController.AddAnimatorName("Entry -> Base Layer.Fly-SM.float");
            TRANS_AnyState_float = mMotionController.AddAnimatorName("AnyState -> Base Layer.Fly-SM.float");
            STATE_float = mMotionController.AddAnimatorName("Base Layer.Fly-SM.float");
            TRANS_float_flying = mMotionController.AddAnimatorName("Base Layer.Fly-SM.float -> Base Layer.Fly-SM.flying");
            STATE_flying = mMotionController.AddAnimatorName("Base Layer.Fly-SM.flying");
            TRANS_flying_float = mMotionController.AddAnimatorName("Base Layer.Fly-SM.flying -> Base Layer.Fly-SM.float");
        }

#if UNITY_EDITOR

        private AnimationClip mfloat = null;
        private AnimationClip mflying = null;

        /// <summary>
        /// Creates the animator substate machine for this motion.
        /// </summary>
        protected override void CreateStateMachine()
        {
            // Grab the root sm for the layer
            UnityEditor.Animations.AnimatorStateMachine lRootStateMachine = _EditorAnimatorController.layers[mMotionLayer.AnimatorLayerIndex].stateMachine;

            // If we find the sm with our name, remove it
            for (int i = 0; i < lRootStateMachine.stateMachines.Length; i++)
            {
                // Look for a sm with the matching name
                if (lRootStateMachine.stateMachines[i].stateMachine.name == _EditorAnimatorSMName)
                {
                    // Allow the user to stop before we remove the sm
                    if (!UnityEditor.EditorUtility.DisplayDialog("Motion Controller", _EditorAnimatorSMName + " already exists. Delete and recreate it?", "Yes", "No"))
                    {
                        return;
                    }

                    // Remove the sm
                    lRootStateMachine.RemoveStateMachine(lRootStateMachine.stateMachines[i].stateMachine);
                }
            }

            UnityEditor.Animations.AnimatorStateMachine lMotionStateMachine = lRootStateMachine.AddStateMachine(_EditorAnimatorSMName);

            // Attach the behaviour if needed
            if (_EditorAttachBehaviour)
            {
                MotionControllerBehaviour lBehaviour = lMotionStateMachine.AddStateMachineBehaviour(typeof(MotionControllerBehaviour)) as MotionControllerBehaviour;
                lBehaviour._MotionKey = (_Key.Length > 0 ? _Key : this.GetType().FullName);
            }

            UnityEditor.Animations.AnimatorState lfloat = lMotionStateMachine.AddState("float", new Vector3(324, 72, 0));
            lfloat.motion = mfloat;
            lfloat.speed = 1f;

            UnityEditor.Animations.AnimatorState lflying = lMotionStateMachine.AddState("flying", new Vector3(612, 72, 0));
            lflying.motion = mflying;
            lflying.speed = 1f;

            UnityEditor.Animations.AnimatorStateTransition lAnyStateTransition = null;

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            lAnyStateTransition = lRootStateMachine.AddAnyStateTransition(lfloat);
            lAnyStateTransition.hasExitTime = false;
            lAnyStateTransition.hasFixedDuration = true;
            lAnyStateTransition.exitTime = 0.9f;
            lAnyStateTransition.duration = 0.1f;
            lAnyStateTransition.offset = 0f;
            lAnyStateTransition.mute = false;
            lAnyStateTransition.solo = false;
            lAnyStateTransition.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 30800f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lStateTransition = null;

            lStateTransition = lfloat.AddTransition(lflying);
            lStateTransition.hasExitTime = false;
            lStateTransition.hasFixedDuration = true;
            lStateTransition.exitTime = 0f;
            lStateTransition.duration = 0.25f;
            lStateTransition.offset = 0f;
            lStateTransition.mute = false;
            lStateTransition.solo = false;
            lStateTransition.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Greater, 0.9f, "InputY");

            lStateTransition = lflying.AddTransition(lfloat);
            lStateTransition.hasExitTime = false;
            lStateTransition.hasFixedDuration = true;
            lStateTransition.exitTime = 0.9050633f;
            lStateTransition.duration = 0.25f;
            lStateTransition.offset = 0f;
            lStateTransition.mute = false;
            lStateTransition.solo = false;
            lStateTransition.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Less, 0.9f, "InputY");

        }

        /// <summary>
        /// Used to show the settings that allow us to generate the animator setup.
        /// </summary>
        public override void OnSettingsGUI()
        {
            UnityEditor.EditorGUILayout.IntField(new GUIContent("Phase ID", "Phase ID used to transition to the state."), PHASE_START);
            mfloat = CreateAnimationField("float", "Assets/MC_ExtraMotions/ootii/Fly/Mixamo@floating.fbx/float.anim", "float", mfloat);
            mflying = CreateAnimationField("flying", "Assets/MC_ExtraMotions/ootii/Fly/Mixamo@flying.fbx/flying.anim", "flying", mflying);

            // Add the remaining functionality
            base.OnSettingsGUI();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}

// ********************************************************************************
// Wrap Up:
//
// That's the basics of creating your own motion. Now, what you do inside the motion
// totally depends on what your motion is all about. So, you can create any
// motion you can think of... you just need to code it within this framework.
// ********************************************************************************

