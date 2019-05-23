using System;
using System.Collections;
using UnityEngine;
using com.ootii.Actors.LifeCores;
using com.ootii.Geometry;
using com.ootii.Helpers;
using com.ootii.Messages;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Actors.AnimationControllers
{
    /// <summary>
    /// Motion used to open doors, pick up items, etc.
    /// </summary>
    [MotionName("Basic Interaction")]
    [MotionDescription("Simple motion to handle object interactions like opening doors, opening chests, picking up objects, etc.")]
    public class BasicInteraction : MotionControllerMotion
    {
        /// <summary>
        /// Trigger values for the motion
        /// </summary>
        public int PHASE_UNKNOWN = 0;
        public int PHASE_START = 3450;
        public int PHASE_CONTINUE = 3455;

        /// <summary>
        /// Determines if we're using the IsInMotion() function to verify that
        /// the transition in the animator has occurred for this motion.
        /// </summary>
        public override bool VerifyTransition
        {
            get { return false; }
        }

        /// <summary>
        /// Determines if we're constantly shooting a ray to look for interactables.
        /// </summary>
        public bool _IsInteractableRaycastEnabled = true;
        public bool IsInteractableRaycastEnabled
        {
            get { return _IsInteractableRaycastEnabled; }
            set { _IsInteractableRaycastEnabled = value; }
        }

        /// <summary>
        /// Name of the bone to use as the root for raycasting
        /// </summary>
        public string _InteractableRaycastRoot = "Head";
        public string InteractableRaycastRoot
        {
            get { return _InteractableRaycastRoot; }

            set
            {
                _InteractableRaycastRoot = value;

                if (Application.isPlaying)
                {
                    mRaycastRoot = FindTransform(_InteractableRaycastRoot);
                }
            }
        }

        /// <summary>
        /// Determines the layers our ray will collide with
        /// </summary>
        public int _InteractableLayers = -1;
        public int InteractableLayers
        {
            get { return _InteractableLayers; }
            set { _InteractableLayers = value; }
        }

        /// <summary>
        /// Determines the distance our ray will be shot from
        /// </summary>
        public float _InteractableDistance = 2f;
        public float InteractableDistance
        {
            get { return _InteractableDistance; }
            set { _InteractableDistance = value; }
        }

        /// <summary>
        /// Determines if an interactable core is required to be selected
        /// </summary>
        public bool _IsInteractableCoreRequired = true;
        public bool IsInteractableCoreRequired
        {
            get { return _IsInteractableCoreRequired; }
            set { _IsInteractableCoreRequired = value; }
        }

        /// <summary>
        /// Motion to activate to control movement
        /// </summary>
        public string _WalkRunMotion = "Controlled Walk";
        public string WalkRunMotion
        {
            get { return _WalkRunMotion; }
            set { _WalkRunMotion = value; }
        }

        /// <summary>
        /// Speed to walk to the target location
        /// </summary>
        public float _WalkSpeed = 1f;
        public float WalkSpeed
        {
            get { return _WalkSpeed; }
            set { _WalkSpeed = value; }
        }

        /// <summary>
        /// Speed to rotate to the target location
        /// </summary>
        public float _RotationSpeed = 180f;
        public float RotationSpeed
        {
            get { return _RotationSpeed; }
            set { _RotationSpeed = value; }
        }

        /// <summary>
        /// Event for when the interaction is triggered (based on the animation event)
        /// </summary>
        [NonSerialized]
        public MotionDelegate OnTriggeredEvent = null;

        /// <summary>
        /// Current object we're interacting with
        /// </summary>
        protected GameObject mInteractable = null;
        public GameObject Interactable
        {
            get { return mInteractable; }

            set
            {
                mInteractable = value;
                mInteractableCore = (mInteractable != null ? mInteractable.GetComponent<InteractableCore>() : null);
            }
        }

        /// <summary>
        /// Current interactable core we're interacting with
        /// </summary>
        protected IInteractableCore mInteractableCore = null;
        public IInteractableCore InteractableCore
        {
            get { return mInteractableCore; }

            set
            {
                mInteractableCore = value;
                mInteractable = (mInteractableCore != null ? mInteractableCore.gameObject : null);
            }
        }

        /// <summary>
        /// Form that is used for this activation
        /// </summary>
        protected int mActiveForm = -1;
        public int ActiveForm
        {
            get { return mActiveForm; }
            set { mActiveForm = value; }
        }

        // Root transform to use for the raycast
        protected Transform mRaycastRoot = null;

        /// <summary>
        /// Default constructor
        /// </summary>
        public BasicInteraction()
            : base()
        {
            _Category = EnumMotionCategories.INTERACT;

            _Priority = 20;
            _ActionAlias = "Interact";
            _Form = 0;

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "BasicInteraction-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public BasicInteraction(MotionController rController)
            : base(rController)
        {
            _Category = EnumMotionCategories.INTERACT;

            _Priority = 20;
            _ActionAlias = "Interact";
            _Form = 0;

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "BasicInteraction-SM"; }
#endif
        }

        /// <summary>
        /// Awake is called after all objects are initialized so you can safely speak to other objects. This is where
        /// reference can be associated.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            InteractableRaycastRoot = _InteractableRaycastRoot;
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

            // Raycast if needed
            if (_IsInteractableRaycastEnabled)
            {
                bool lIsFound = false;

                RaycastHit lHitInfo;

                // Even though we have a raycast root, we really want to shoot from the camera. We'll check distance with the root later.
                Vector3 lStart = (mMotionController.CameraTransform != null ? mMotionController.CameraTransform.position : mMotionController._Transform.position);
                Vector3 lForward = (mMotionController.CameraTransform != null ? mMotionController.CameraTransform.forward : mMotionController._Transform.forward);

                if (RaycastExt.SafeRaycast(lStart, lForward, out lHitInfo, _InteractableDistance * 5f, _InteractableLayers, mMotionController._Transform))
                {
                    bool lIsValid = true;

                    if (mRaycastRoot != null)
                    {
                        float lDistance = Vector3.Distance(lHitInfo.point, mRaycastRoot.position);
                        if (lDistance > _InteractableDistance) { lIsValid = false; }
                    }

                    if (lIsValid)
                    {
                        IInteractableCore lCore = lHitInfo.collider.gameObject.GetComponent<IInteractableCore>();
                        if (lCore == null) { lCore = lHitInfo.collider.gameObject.GetComponentInParent<IInteractableCore>(); }

                        if (!_IsInteractableCoreRequired && lCore == null)
                        {
                            lIsFound = true;
                            Interactable = lHitInfo.collider.gameObject;
                        }
                        else
                        { 
                            if (lCore == null) { lIsValid = false; }
                            if (lIsValid && !lCore.IsEnabled) { lIsValid = false; }
                            if (lIsValid && !lCore.TestActivator(mMotionController._Transform)) { lIsValid = false; }
                            if (lIsValid && lCore.RaycastCollider != null && lHitInfo.collider != lCore.RaycastCollider) { lIsValid = false; }

                            if (lIsValid)
                            {
                                lIsFound = true;
                                InteractableCore = lCore;
                                InteractableCore.StartFocus();

                                mActiveForm = lCore.Form;
                            }
                        }
                    }
                }

                // Deselect any interactable if none is found
                if (!lIsFound && _IsInteractableRaycastEnabled)
                {
                    Interactable = null;
                }
            }

            // Test if we're supposed to activate
            if (_ActionAlias.Length > 0 && mMotionController._InputSource != null)
            {
                if (mMotionController._InputSource.IsJustPressed(_ActionAlias))
                {
                    // Check if the interactable is going to prepare thier activator. If so, we
                    // aren't going to activate here. The activator will trigger the activation.
                    if (InteractableCore != null)
                    {
                        if (InteractableCore.ForcePosition || InteractableCore.ForceRotation)
                        {
                            mMotionController.StartCoroutine(MoveToTargetInternal(InteractableCore));
                        }
                        else
                        {
                            return true;
                        }
                    }
                    // Since we're just dealing with a simple game object, activate
                    else if (Interactable != null)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Tests if the motion should continue. If it shouldn't, the motion
        /// is typically disabled
        /// </summary>
        /// <returns></returns>
        public override bool TestUpdate()
        {
            if (mMotionController.State.AnimatorStates[mMotionLayer._AnimatorLayerIndex].StateInfo.IsTag("Exit"))
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
            // Trigger the transition
            mActiveForm = (mActiveForm >= 0 ? mActiveForm : _Form);
            mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, mActiveForm, mParameter, true);


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

        /// <summary>
        /// Raised by the animation when an event occurs
        /// </summary>
        public override void OnAnimationEvent(AnimationEvent rEvent)
        {
            if (rEvent == null) { return; }
            if (!mIsActive) { return; }

            // Report the interaction with the core
            if (mInteractableCore != null)
            {
                mInteractableCore.OnActivated(this);
            }

            // Call the callback
            if (OnTriggeredEvent != null)
            {
                OnTriggeredEvent(mMotionLayer._AnimatorLayerIndex, this);
            }

            // Trigger any desired event
            if (mMotionController.ActionTriggeredEvent != null)
            {
                Message lMessage = Message.Allocate();
                lMessage.ID = EnumMessageID.MSG_INTERACTION_ACTIVATE;
                lMessage.Data = mInteractable;

                mMotionController.ActionTriggeredEvent.Invoke(lMessage);

                Message.Release(lMessage);
            }
        }

        /// <summary>
        /// Raised by the controller when a message is received
        /// </summary>
        public override void OnMessageReceived(IMessage rMessage)
        {
            if (rMessage == null) { return; }

            MotionMessage lMotionMessage = rMessage as MotionMessage;
            if (lMotionMessage != null)
            {
                // Activate the interaction
                if (lMotionMessage.ID == MotionMessage.MSG_MOTION_ACTIVATE)
                {
                    if (!mIsActive)
                    {
                        mActiveForm = (lMotionMessage.Form >= 0 ? lMotionMessage.Form : _Form);
                        Activate(mMotionLayer.ActiveMotion);

                        lMotionMessage.IsHandled = true;
                        lMotionMessage.Recipient = this;
                    }
                }
                // Continue with the casting
                if (lMotionMessage.ID == MotionMessage.MSG_MOTION_CONTINUE)
                {
                    if (mIsActive)
                    {
                        mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_CONTINUE, 0, 0, true);

                        lMotionMessage.IsHandled = true;
                        lMotionMessage.Recipient = this;
                    }
                }
            }
        }

        /// <summary>
        /// Coroutine for moving the actor to the right position
        /// </summary>
        /// <param name="rMotion"></param>
        /// <returns></returns>
        public virtual IEnumerator MoveToTargetInternal(IInteractableCore rInteractableCore)
        {
            bool lStoredWalkRunMotionEnabled = false;
            bool lStoredUseTransformPosition = false;
            bool lStoredUseTransformRotation = false;

            MotionController lMotionController = mMotionController;
            ActorController lActorController = lMotionController._ActorController;

            // Enable AC positioning
            lStoredUseTransformPosition = lActorController.UseTransformPosition;
            lActorController.UseTransformPosition = true;

            lStoredUseTransformRotation = lActorController.UseTransformRotation;
            lActorController.UseTransformRotation = true;

            // Enable our strafing motion
            MotionControllerMotion lWalkRunMotion = lMotionController.GetMotion(_WalkRunMotion, true);
            if (lWalkRunMotion != null)
            {
                lStoredWalkRunMotionEnabled = lWalkRunMotion.IsEnabled;
                lWalkRunMotion.IsEnabled = true;
            }

            Vector3 lTargetPosition = lActorController._Transform.position;
            if (rInteractableCore.ForcePosition)
            {
                if (rInteractableCore.TargetLocation != null)
                {
                    lTargetPosition = rInteractableCore.TargetLocation.position;
                }
                else if (rInteractableCore.TargetDistance > 0f)
                {
                    Vector3 lInteractablePosition = rInteractableCore.gameObject.transform.position;
                    lInteractablePosition.y = lActorController._Transform.position.y;

                    lTargetPosition = lInteractablePosition + ((lActorController._Transform.position - lInteractablePosition).normalized * rInteractableCore.TargetDistance);
                }
            }

            Vector3 lTargetForward = lActorController._Transform.forward;
            if (rInteractableCore.ForceRotation)
            {
                if (rInteractableCore.TargetLocation != null)
                {
                    lTargetForward = rInteractableCore.TargetLocation.forward;
                }
                else
                {
                    Vector3 lInteractablePosition = rInteractableCore.gameObject.transform.position;
                    lInteractablePosition.y = lActorController._Transform.position.y;

                    lTargetForward = (lInteractablePosition - lActorController._Transform.position).normalized;
                }
            }

            // Move to the target position and rotation
            Vector3 lDirection = lTargetPosition - lActorController._Transform.position;
            float lAngle = Vector3Ext.HorizontalAngleTo(lActorController._Transform.forward, lTargetForward);

            while (HorizontalDistance(lActorController._Transform.position, lTargetPosition) > 0.01f || Mathf.Abs(lAngle) > 0.1f)
            {
                float lDistance = Mathf.Min(lDirection.magnitude, _WalkSpeed * Time.deltaTime);
                lActorController._Transform.position = lActorController._Transform.position + (lDirection.normalized * lDistance);

                float lYaw = Mathf.Sign(lAngle) * Mathf.Min(Mathf.Abs(lAngle), _RotationSpeed * Time.deltaTime);
                lActorController._Transform.rotation = (lActorController._Transform.rotation * Quaternion.Euler(0f, lYaw, 0f));

                yield return new WaitForEndOfFrame();
                lDirection = lTargetPosition - lActorController._Transform.position;
                lAngle = Vector3Ext.HorizontalAngleTo(lActorController._Transform.forward, lTargetForward);
            }

            // Activate BasicInteraction
            mActiveForm = rInteractableCore.Form;
            InteractableCore = rInteractableCore;
            lMotionController.ActivateMotion(this);

            // Give some final frames to get to the exact position
            yield return new WaitForSeconds(0.2f);

            // Reset the motion and movement options
            if (lWalkRunMotion != null) { lWalkRunMotion.IsEnabled = lStoredWalkRunMotionEnabled; }
            lActorController.UseTransformPosition = lStoredUseTransformPosition;
            lActorController.UseTransformRotation = lStoredUseTransformRotation;

            lMotionController.TargetNormalizedSpeed = 1f;
        }

        /// <summary>
        /// Attempts to find a matching transform
        /// </summary>
        /// <param name="rName">Name or identifier of the transform we want</param>
        /// <returns>Transform matching bone name or transform</returns>
        protected Transform FindTransform(string rName)
        {
            if (rName.Length > 0)
            {
                Transform lTransform = mMotionController._Transform.Find(rName);
                if (lTransform != null) { return lTransform; }

                string[] lBones = System.Enum.GetNames(typeof(HumanBodyBones));
                for (int i = 0; i < lBones.Length; i++)
                {
                    if (string.Compare(lBones[i].Replace(" ", String.Empty).Replace("_", String.Empty), _InteractableRaycastRoot) == 0)
                    {
                        return mMotionController.Animator.GetBoneTransform((HumanBodyBones)i);
                    }
                }
            }

            return mMotionController._Transform;
        }

        /// <summary>
        /// Returns the horizontal distance between two vectors (ignoring the y-component)
        /// </summary>
        /// <param name="rVector1">First position</param>
        /// <param name="rVector2">Second position</param>
        /// <returns>Distnce between the two vectors (ignoring the y-component)</returns>
        protected float HorizontalDistance(Vector3 rVector1, Vector3 rVector2)
        {
            rVector2.y = rVector1.y;
            return Vector3.Distance(rVector1, rVector2);
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

            if (EditorHelper.IntField("Form", "Sets the LXMotionForm animator property to determine the animation for the motion.", Form, mMotionController))
            {
                lIsDirty = true;
                Form = EditorHelper.FieldIntValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Use Raycast", "Determines if we'll constantly shoot a ray to test for interactables.", IsInteractableRaycastEnabled, mMotionController))
            {
                lIsDirty = true;
                IsInteractableRaycastEnabled = EditorHelper.FieldBoolValue;
            }

            if (IsInteractableRaycastEnabled)
            {
                int lNewLayers = EditorHelper.LayerMaskField(new GUIContent("Layers", "Layers that identies objects that can be interacted with."), InteractableLayers);
                if (lNewLayers != InteractableLayers)
                {
                    lIsDirty = true;
                    InteractableLayers = lNewLayers;
                }

                if (EditorHelper.TextField("Distance Root", "Root position to check the distance test. While we shoot the ray from the camera, this root allows us to check the distance better. Use the HumanBodyBones names or the name of a specific transform.", InteractableRaycastRoot, mMotionController))
                {
                    lIsDirty = true;
                    InteractableRaycastRoot = EditorHelper.FieldStringValue;
                }

                if (EditorHelper.FloatField("Distance", "Max ray distance used to check for interactables.", InteractableDistance, mMotionController))
                {
                    lIsDirty = true;
                    InteractableDistance = EditorHelper.FieldFloatValue;
                }

                if (EditorHelper.BoolField("Interactable Core Required", "Determines if an InteractableCore component is required to select a target.", IsInteractableCoreRequired, mMotionController))
                {
                    lIsDirty = true;
                    IsInteractableCoreRequired = EditorHelper.FieldBoolValue;
                }

                GUILayout.Space(5f);

                if (EditorHelper.TextField("Walk Motion", "Walk motion to enable for any position shifting that we need to do.", WalkRunMotion, mMotionController))
                {
                    lIsDirty = true;
                    WalkRunMotion = EditorHelper.FieldStringValue;
                }

                if (EditorHelper.FloatField("Walk Speed", "Speed (units per second) to walk to the target location.", WalkSpeed, mMotionController))
                {
                    lIsDirty = true;
                    WalkSpeed = EditorHelper.FieldFloatValue;
                }

                if (EditorHelper.FloatField("Rotation Speed", "Rotation speed (degrees per second) to rotate to the target location forward.", RotationSpeed, mMotionController))
                {
                    lIsDirty = true;
                    RotationSpeed = EditorHelper.FieldFloatValue;
                }
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
        public int STATE_Start = -1;
        public int STATE_Idle_GrabHighFront = -1;
        public int STATE_Idle_PickUp = -1;
        public int STATE_Idle_PushButton = -1;
        public int STATE_IdlePose = -1;
        public int TRANS_AnyState_Idle_PushButton = -1;
        public int TRANS_EntryState_Idle_PushButton = -1;
        public int TRANS_AnyState_Idle_GrabHighFront = -1;
        public int TRANS_EntryState_Idle_GrabHighFront = -1;
        public int TRANS_AnyState_Idle_PickUp = -1;
        public int TRANS_EntryState_Idle_PickUp = -1;
        public int TRANS_Idle_GrabHighFront_IdlePose = -1;
        public int TRANS_Idle_PickUp_IdlePose = -1;
        public int TRANS_Idle_PushButton_IdlePose = -1;

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
                    if (lStateID == STATE_Idle_GrabHighFront) { return true; }
                    if (lStateID == STATE_Idle_PickUp) { return true; }
                    if (lStateID == STATE_Idle_PushButton) { return true; }
                    if (lStateID == STATE_IdlePose) { return true; }
                }

                if (lTransitionID == TRANS_AnyState_Idle_PushButton) { return true; }
                if (lTransitionID == TRANS_EntryState_Idle_PushButton) { return true; }
                if (lTransitionID == TRANS_AnyState_Idle_GrabHighFront) { return true; }
                if (lTransitionID == TRANS_EntryState_Idle_GrabHighFront) { return true; }
                if (lTransitionID == TRANS_AnyState_Idle_PickUp) { return true; }
                if (lTransitionID == TRANS_EntryState_Idle_PickUp) { return true; }
                if (lTransitionID == TRANS_Idle_GrabHighFront_IdlePose) { return true; }
                if (lTransitionID == TRANS_Idle_PickUp_IdlePose) { return true; }
                if (lTransitionID == TRANS_Idle_PushButton_IdlePose) { return true; }
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
            if (rStateID == STATE_Idle_GrabHighFront) { return true; }
            if (rStateID == STATE_Idle_PickUp) { return true; }
            if (rStateID == STATE_Idle_PushButton) { return true; }
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
                if (rStateID == STATE_Idle_GrabHighFront) { return true; }
                if (rStateID == STATE_Idle_PickUp) { return true; }
                if (rStateID == STATE_Idle_PushButton) { return true; }
                if (rStateID == STATE_IdlePose) { return true; }
            }

            if (rTransitionID == TRANS_AnyState_Idle_PushButton) { return true; }
            if (rTransitionID == TRANS_EntryState_Idle_PushButton) { return true; }
            if (rTransitionID == TRANS_AnyState_Idle_GrabHighFront) { return true; }
            if (rTransitionID == TRANS_EntryState_Idle_GrabHighFront) { return true; }
            if (rTransitionID == TRANS_AnyState_Idle_PickUp) { return true; }
            if (rTransitionID == TRANS_EntryState_Idle_PickUp) { return true; }
            if (rTransitionID == TRANS_Idle_GrabHighFront_IdlePose) { return true; }
            if (rTransitionID == TRANS_Idle_PickUp_IdlePose) { return true; }
            if (rTransitionID == TRANS_Idle_PushButton_IdlePose) { return true; }
            return false;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            string lLayer = mMotionController.Animator.GetLayerName(mMotionLayer._AnimatorLayerIndex);
            TRANS_AnyState_Idle_PushButton = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".BasicInteraction-SM.Idle_PushButton");
            TRANS_EntryState_Idle_PushButton = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".BasicInteraction-SM.Idle_PushButton");
            TRANS_AnyState_Idle_GrabHighFront = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".BasicInteraction-SM.Idle_GrabHighFront");
            TRANS_EntryState_Idle_GrabHighFront = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".BasicInteraction-SM.Idle_GrabHighFront");
            TRANS_AnyState_Idle_PickUp = mMotionController.AddAnimatorName("AnyState -> " + lLayer + ".BasicInteraction-SM.Idle_PickUp");
            TRANS_EntryState_Idle_PickUp = mMotionController.AddAnimatorName("Entry -> " + lLayer + ".BasicInteraction-SM.Idle_PickUp");
            STATE_Start = mMotionController.AddAnimatorName("" + lLayer + ".Start");
            STATE_Idle_GrabHighFront = mMotionController.AddAnimatorName("" + lLayer + ".BasicInteraction-SM.Idle_GrabHighFront");
            TRANS_Idle_GrabHighFront_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".BasicInteraction-SM.Idle_GrabHighFront -> " + lLayer + ".BasicInteraction-SM.IdlePose");
            STATE_Idle_PickUp = mMotionController.AddAnimatorName("" + lLayer + ".BasicInteraction-SM.Idle_PickUp");
            TRANS_Idle_PickUp_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".BasicInteraction-SM.Idle_PickUp -> " + lLayer + ".BasicInteraction-SM.IdlePose");
            STATE_Idle_PushButton = mMotionController.AddAnimatorName("" + lLayer + ".BasicInteraction-SM.Idle_PushButton");
            TRANS_Idle_PushButton_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".BasicInteraction-SM.Idle_PushButton -> " + lLayer + ".BasicInteraction-SM.IdlePose");
            STATE_IdlePose = mMotionController.AddAnimatorName("" + lLayer + ".BasicInteraction-SM.IdlePose");
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

            UnityEditor.Animations.AnimatorStateMachine lSSM_N1556694 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "BasicInteraction-SM");
            if (lSSM_N1556694 == null) { lSSM_N1556694 = lLayerStateMachine.AddStateMachine("BasicInteraction-SM", new Vector3(408, -960, 0)); }

            UnityEditor.Animations.AnimatorState lState_N1565974 = MotionControllerMotion.EditorFindState(lSSM_N1556694, "Idle_GrabHighFront");
            if (lState_N1565974 == null) { lState_N1565974 = lSSM_N1556694.AddState("Idle_GrabHighFront", new Vector3(337, 54, 0)); }
            lState_N1565974.speed = 1.5f;
            lState_N1565974.mirror = false;
            lState_N1565974.tag = "";
            lState_N1565974.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Interacting/Unity_IdleGrab_FrontHigh.fbx", "Idle_GrabHighFront");

            UnityEditor.Animations.AnimatorState lState_N1566382 = MotionControllerMotion.EditorFindState(lSSM_N1556694, "Idle_PickUp");
            if (lState_N1566382 == null) { lState_N1566382 = lSSM_N1556694.AddState("Idle_PickUp", new Vector3(336, 168, 0)); }
            lState_N1566382.speed = 1.5f;
            lState_N1566382.mirror = false;
            lState_N1566382.tag = "";
            lState_N1566382.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Interacting/unity_IdleGrab_LowFront.fbx", "Idle_PickUp");

            UnityEditor.Animations.AnimatorState lState_N1567060 = MotionControllerMotion.EditorFindState(lSSM_N1556694, "Idle_PushButton");
            if (lState_N1567060 == null) { lState_N1567060 = lSSM_N1556694.AddState("Idle_PushButton", new Vector3(336, -48, 0)); }
            lState_N1567060.speed = 1.5f;
            lState_N1567060.mirror = false;
            lState_N1567060.tag = "";
            lState_N1567060.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Interacting/unity_IdleGrab_Neutral.fbx", "Idle_PushButton");

            UnityEditor.Animations.AnimatorState lState_N1568354 = MotionControllerMotion.EditorFindState(lSSM_N1556694, "IdlePose");
            if (lState_N1568354 == null) { lState_N1568354 = lSSM_N1556694.AddState("IdlePose", new Vector3(600, 48, 0)); }
            lState_N1568354.speed = 1f;
            lState_N1568354.mirror = false;
            lState_N1568354.tag = "Exit";
            lState_N1568354.motion = MotionControllerMotion.EditorFindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx", "IdlePose");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_N1573638 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_N1567060, 0);
            if (lAnyTransition_N1573638 == null) { lAnyTransition_N1573638 = lLayerStateMachine.AddAnyStateTransition(lState_N1567060); }
            lAnyTransition_N1573638.isExit = false;
            lAnyTransition_N1573638.hasExitTime = false;
            lAnyTransition_N1573638.hasFixedDuration = true;
            lAnyTransition_N1573638.exitTime = 0.75f;
            lAnyTransition_N1573638.duration = 0.25f;
            lAnyTransition_N1573638.offset = 0.1517324f;
            lAnyTransition_N1573638.mute = false;
            lAnyTransition_N1573638.solo = false;
            lAnyTransition_N1573638.canTransitionToSelf = true;
            lAnyTransition_N1573638.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_N1573638.conditions.Length - 1; i >= 0; i--) { lAnyTransition_N1573638.RemoveCondition(lAnyTransition_N1573638.conditions[i]); }
            lAnyTransition_N1573638.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3450f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_N1573638.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_N1574214 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_N1565974, 0);
            if (lAnyTransition_N1574214 == null) { lAnyTransition_N1574214 = lLayerStateMachine.AddAnyStateTransition(lState_N1565974); }
            lAnyTransition_N1574214.isExit = false;
            lAnyTransition_N1574214.hasExitTime = false;
            lAnyTransition_N1574214.hasFixedDuration = true;
            lAnyTransition_N1574214.exitTime = 0.75f;
            lAnyTransition_N1574214.duration = 0.25f;
            lAnyTransition_N1574214.offset = 0.07021895f;
            lAnyTransition_N1574214.mute = false;
            lAnyTransition_N1574214.solo = false;
            lAnyTransition_N1574214.canTransitionToSelf = true;
            lAnyTransition_N1574214.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_N1574214.conditions.Length - 1; i >= 0; i--) { lAnyTransition_N1574214.RemoveCondition(lAnyTransition_N1574214.conditions[i]); }
            lAnyTransition_N1574214.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3450f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_N1574214.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lAnyTransition_N1574786 = MotionControllerMotion.EditorFindAnyStateTransition(lLayerStateMachine, lState_N1566382, 0);
            if (lAnyTransition_N1574786 == null) { lAnyTransition_N1574786 = lLayerStateMachine.AddAnyStateTransition(lState_N1566382); }
            lAnyTransition_N1574786.isExit = false;
            lAnyTransition_N1574786.hasExitTime = false;
            lAnyTransition_N1574786.hasFixedDuration = true;
            lAnyTransition_N1574786.exitTime = 0.75f;
            lAnyTransition_N1574786.duration = 0.25f;
            lAnyTransition_N1574786.offset = 0f;
            lAnyTransition_N1574786.mute = false;
            lAnyTransition_N1574786.solo = false;
            lAnyTransition_N1574786.canTransitionToSelf = true;
            lAnyTransition_N1574786.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lAnyTransition_N1574786.conditions.Length - 1; i >= 0; i--) { lAnyTransition_N1574786.RemoveCondition(lAnyTransition_N1574786.conditions[i]); }
            lAnyTransition_N1574786.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 3450f, "L" + rLayerIndex + "MotionPhase");
            lAnyTransition_N1574786.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 2f, "L" + rLayerIndex + "MotionForm");

            UnityEditor.Animations.AnimatorStateTransition lTransition_N1569370 = MotionControllerMotion.EditorFindTransition(lState_N1565974, lState_N1568354, 0);
            if (lTransition_N1569370 == null) { lTransition_N1569370 = lState_N1565974.AddTransition(lState_N1568354); }
            lTransition_N1569370.isExit = false;
            lTransition_N1569370.hasExitTime = true;
            lTransition_N1569370.hasFixedDuration = true;
            lTransition_N1569370.exitTime = 0.9285715f;
            lTransition_N1569370.duration = 0.25f;
            lTransition_N1569370.offset = 0f;
            lTransition_N1569370.mute = false;
            lTransition_N1569370.solo = false;
            lTransition_N1569370.canTransitionToSelf = true;
            lTransition_N1569370.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_N1569370.conditions.Length - 1; i >= 0; i--) { lTransition_N1569370.RemoveCondition(lTransition_N1569370.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_N1569788 = MotionControllerMotion.EditorFindTransition(lState_N1566382, lState_N1568354, 0);
            if (lTransition_N1569788 == null) { lTransition_N1569788 = lState_N1566382.AddTransition(lState_N1568354); }
            lTransition_N1569788.isExit = false;
            lTransition_N1569788.hasExitTime = true;
            lTransition_N1569788.hasFixedDuration = true;
            lTransition_N1569788.exitTime = 0.90625f;
            lTransition_N1569788.duration = 0.25f;
            lTransition_N1569788.offset = 0f;
            lTransition_N1569788.mute = false;
            lTransition_N1569788.solo = false;
            lTransition_N1569788.canTransitionToSelf = true;
            lTransition_N1569788.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_N1569788.conditions.Length - 1; i >= 0; i--) { lTransition_N1569788.RemoveCondition(lTransition_N1569788.conditions[i]); }

            UnityEditor.Animations.AnimatorStateTransition lTransition_N1569000 = MotionControllerMotion.EditorFindTransition(lState_N1567060, lState_N1568354, 0);
            if (lTransition_N1569000 == null) { lTransition_N1569000 = lState_N1567060.AddTransition(lState_N1568354); }
            lTransition_N1569000.isExit = false;
            lTransition_N1569000.hasExitTime = true;
            lTransition_N1569000.hasFixedDuration = true;
            lTransition_N1569000.exitTime = 0.7673402f;
            lTransition_N1569000.duration = 0.2499998f;
            lTransition_N1569000.offset = 0f;
            lTransition_N1569000.mute = false;
            lTransition_N1569000.solo = false;
            lTransition_N1569000.canTransitionToSelf = true;
            lTransition_N1569000.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            for (int i = lTransition_N1569000.conditions.Length - 1; i >= 0; i--) { lTransition_N1569000.RemoveCondition(lTransition_N1569000.conditions[i]); }


            // Run any post processing after creating the state machine
            OnStateMachineCreated();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}
