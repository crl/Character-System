using UnityEngine;
using com.ootii.Actors.Combat;
using com.ootii.Cameras;
using com.ootii.Helpers;
using com.ootii.Geometry;
using com.ootii.Messages;
using com.ootii.Timing;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

namespace com.ootii.Actors.AnimationControllers
{
    /// <summary>
    /// Basic motion for a sword attack
    /// </summary>
    [MotionName("Levitate")]
    [MotionDescription("Allows the character to hover up and down with minimal lateral movement.")]
    public class Levitate : MotionControllerMotion
    {
        /// <summary>
        /// Trigger values for the motion
        /// </summary>
        public const int PHASE_UNKNOWN = 0;
        public const int PHASE_START = 1800;
        public const int PHASE_END = 1805;

        /// <summary>
        /// When levitation is activated, the velocity we'll apply (in units per second)
        /// </summary>
        public Vector3 _ConstantVelocity = new Vector3(0f, 0f, 0f);
        public Vector3 ConstantVelocity
        {
            get { return _ConstantVelocity; }
            set { _ConstantVelocity = value; }
        }

        /// <summary>
        /// Action alias for moving up
        /// </summary>
        public string _UpAlias = "Move Up";
        public string UpAlias
        {
            get { return _UpAlias; }
            set { _UpAlias = value; }
        }

        /// <summary>
        /// Action alias for moving down
        /// </summary>
        public string _DownAlias = "Move Down";
        public string DownAlias
        {
            get { return _DownAlias; }
            set { _DownAlias = value; }
        }

        /// <summary>
        /// Speed when moving up and down
        /// </summary>
        public float _VerticalSpeed = 1f;
        public float VerticalSpeed
        {
            get { return _VerticalSpeed; }
            set { _VerticalSpeed = value; }
        }

        /// <summary>
        /// Speed when moving laterally
        /// </summary>
        public float _HorizontalSpeed = 0.5f;
        public float HorizontalSpeed
        {
            get { return _HorizontalSpeed; }
            set { _HorizontalSpeed = value; }
        }

        /// <summary>
        /// Determines if we rotate by ourselves
        /// </summary>
        public bool _RotateWithInput = false;
        public bool RotateWithInput
        {
            get { return _RotateWithInput; }
            set { _RotateWithInput = value; }
        }

        /// <summary>
        /// Determines if we rotate to match the camera
        /// </summary>
        public bool _RotateWithCamera = true;
        public bool RotateWithCamera
        {
            get { return _RotateWithCamera; }
            set { _RotateWithCamera = value; }
        }

        /// <summary>
        /// User layer id set for objects that are climbable.
        /// </summary>
        public string _RotateActionAlias = "";
        public string RotateActionAlias
        {
            get { return _RotateActionAlias; }
            set { _RotateActionAlias = value; }
        }

        /// <summary>
        /// Desired degrees of rotation per second
        /// </summary>
        public float _RotationSpeed = 360f;
        public float RotationSpeed
        {
            get { return _RotationSpeed; }

            set
            {
                _RotationSpeed = value;
                mDegreesPer60FPSTick = _RotationSpeed / 60f;
            }
        }

        // Determines if we've left the ground
        protected bool mHasLaunched = false;

        // Determines if we've landed
        protected bool mHasLanded = false;

        // Stored properties we'll reset at the end of the motion
        protected bool mStoredIsGravityEnabled = true;
        protected bool mStoredIsOrientationEnabled = true;

        /// <summary>
        /// Rotation target we're heading to
        /// </summary>
        protected Vector3 mTargetForward = Vector3.zero;

        /// <summary>
        /// Speed we'll actually apply to the rotation. This is essencially the
        /// number of degrees per tick assuming we're running at 60 FPS
        /// </summary>
        protected float mDegreesPer60FPSTick = 1f;

        /// <summary>
        /// Fields to help smooth out the mouse rotation
        /// </summary>
        protected float mYaw = 0f;
        protected float mYawTarget = 0f;
        protected float mYawVelocity = 0f;

        /// <summary>
        /// Determines if the actor rotation should be linked to the camera
        /// </summary>
        protected bool mLinkRotation = false;

        /// <summary>
        /// Default constructor
        /// </summary>
        public Levitate()
            : base()
        {
            _Pack = Idle.GroupName();
            _Category = EnumMotionCategories.SPELL_CASTING;

            _Priority = 25;
            _ActionAlias = "";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "Levitate-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public Levitate(MotionController rController)
            : base(rController)
        {
            _Pack = Idle.GroupName();
            _Category = EnumMotionCategories.SPELL_CASTING;

            _Priority = 25;
            _ActionAlias = "";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "Levitate-SM"; }
#endif
        }

        /// <summary>
        /// Awake is called after all objects are initialized so you can safely speak to other objects. This is where
        /// reference can be associated.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            // Default the speed we'll use to rotate
            mDegreesPer60FPSTick = _RotationSpeed / 60f;
        }

        /// <summary>
        /// Tests if this motion should be started. However, the motion
        /// isn't actually started.
        /// </summary>
        /// <returns></returns>
        public override bool TestActivate()
        {
            if (!mIsStartable) { return false; }

            if (!mActorController.IsGrounded) { return false; }

            // Check if we've been activated
            if (_ActionAlias.Length > 0 && mMotionController._InputSource != null)
            {
                if (mMotionController._InputSource.IsJustPressed(_ActionAlias))
                {
                    mParameter = 0;
                    return true;
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
            if (mIsActivatedFrame) { return true; }

            // Ensure we're actually in our animation state
            if (mIsAnimatorActive)
            {
                if (!IsInMotionState)
                {
                    return false;
                }
            }

            // End when we hit the idle pose
            if (mMotionLayer._AnimatorStateID == STATE_IdlePose)
            {
                return false;
            }

            // Check if we've been activated
            if (_ActionAlias.Length > 0 && mMotionController._InputSource != null)
            {
                if (mMotionController._InputSource.IsJustPressed(_ActionAlias))
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
            mHasLanded = false;
            mHasLaunched = false;

            // Ensure the AC allows us to levitate
            mStoredIsGravityEnabled = mActorController.IsGravityEnabled;
            mActorController.IsGravityEnabled = false;

            mStoredIsOrientationEnabled = mActorController.OrientToGround;
            mActorController.OrientToGround = false;

            mActorController.FixGroundPenetration = false;

            // Run the approapriate cast
            mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, 0, true);

            // Register this motion with the camera
            if (_RotateWithCamera && mMotionController.CameraRig is BaseCameraRig)
            {
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate += OnCameraUpdated;
            }

            // Now, activate the motion
            return base.Activate(rPrevMotion);
        }

        /// <summary>
        /// Called to interrupt the motion if it is currently active. This
        /// gives the motion a chance to stop itself how it sees fit. The motion
        /// may simply ignore the call.
        /// </summary>
        /// <param name="rParameter">Any value you wish to pass</param>
        /// <returns>Boolean determining if the motion accepts the interruption. It doesn't mean it will deactivate.</returns>
        public override bool Interrupt(object rParameter)
        {
            return false;
        }

        /// <summary>
        /// Raised when we shut the motion down
        /// </summary>
        public override void Deactivate()
        {
            // Reset the AC properties
            mActorController.IsGravityEnabled = mStoredIsGravityEnabled;
            mActorController.OrientToGround = mStoredIsOrientationEnabled;
            mActorController.FixGroundPenetration = true;

            // Register this motion with the camera
            if (mMotionController.CameraRig is BaseCameraRig)
            {
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
            }

            // Continue
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
            mMovement = Vector3.zero;
            mRotation = Quaternion.identity;

            // Determine if we've launched or not
            if (!mHasLaunched && !mMotionController.IsGrounded) { mHasLaunched = true; }

            // If we've touched the ground again, we're done
            if (mHasLaunched && mMotionController.IsGrounded) { mHasLanded = true; }

            // Determine the movement
            if (!mHasLanded)
            {
                if (!mHasLaunched)
                {
                    mMovement = mMotionController._Transform.up * (0.25f * rDeltaTime);
                }
                else
                {
                    mMovement = ConstantVelocity * rDeltaTime;
                }

                if (mMotionController._InputSource != null)
                {
                    float lUpSpeed = mMotionController._InputSource.GetValue(_UpAlias) * _VerticalSpeed;
                    float lDownSpeed = mMotionController._InputSource.GetValue(_DownAlias) * -_VerticalSpeed;
                    mMovement = mMovement + mMotionController._Transform.up * ((lUpSpeed + lDownSpeed) * rDeltaTime);

                    Vector3 lMovement = mMotionController.State.InputForward;
                    mMovement = mMovement + (mMotionController._Transform.rotation * (lMovement * (_HorizontalSpeed * rDeltaTime)));                    
                }
            }
            else
            {
                mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_END, 0, true);
            }

            // Determine if we're rotating based on input
            if (!_RotateWithCamera && _RotateWithInput)
            {
                RotateUsingInput(rDeltaTime, ref mRotation);
            }

            // If we're meant to rotate with the camera (and OnCameraUpdate isn't already attached), do it here
            if (_RotateWithCamera && !(mMotionController.CameraRig is BaseCameraRig))
            {
                OnCameraUpdated(rDeltaTime, rUpdateIndex, null);
            }

            //// Ensure we face the target if we're meant to
            //if (mCombatant != null && mCombatant.IsTargetLocked)
            //{
            //    RotateCameraToTarget(mCombatant.Target, _ToTargetRotationSpeed);
            //    RotateToTarget(mCombatant.Target, _ToTargetRotationSpeed, rDeltaTime, ref mRotation);
            //}
            //// Otherwise, rotate towards the camera
            //else if (mTargetForward.sqrMagnitude > 0f)
            //{
            //    if (RotateToCameraForward)
            //    {
            //        RotateToTargetForward(mTargetForward, _ToTargetRotationSpeed, ref mRotation);
            //    }
            //}

            // Allow the base class to render debug info
            base.Update(rDeltaTime, rUpdateIndex);
        }


        /// <summary>
        /// Create a rotation velocity that rotates the character based on input
        /// </summary>
        /// <param name="rDeltaTime"></param>
        /// <param name="rAngularVelocity"></param>
        private void RotateUsingInput(float rDeltaTime, ref Quaternion rRotation)
        {
            // If we don't have an input source, stop
            if (mMotionController._InputSource == null) { return; }

            // Determine this frame's rotation
            float lYawDelta = 0f;
            float lYawSmoothing = 0.1f;

            if (mMotionController._InputSource.IsViewingActivated)
            {
                lYawDelta = mMotionController._InputSource.ViewX * mDegreesPer60FPSTick;
            }

            mYawTarget = mYawTarget + lYawDelta;

            // Smooth the rotation
            lYawDelta = (lYawSmoothing <= 0f ? mYawTarget : Mathf.SmoothDampAngle(mYaw, mYawTarget, ref mYawVelocity, lYawSmoothing)) - mYaw;
            mYaw = mYaw + lYawDelta;

            // Use this frame's smoothed rotation
            if (lYawDelta != 0f)
            {
                rRotation = Quaternion.Euler(0f, lYawDelta, 0f);
            }
        }

        /// <summary>
        /// When we want to rotate based on the camera direction, we need to tweak the actor
        /// rotation AFTER we process the camera. Otherwise, we can get small stutters during camera rotation. 
        /// 
        /// This is the only way to keep them totally in sync. It also means we can't run any of our AC processing
        /// as the AC already ran. So, we do minimal work here
        /// </summary>
        /// <param name="rDeltaTime"></param>
        /// <param name="rUpdateCount"></param>
        /// <param name="rCamera"></param>
        private void OnCameraUpdated(float rDeltaTime, int rUpdateCount, BaseCameraRig rCamera)
        {
            if (mMotionController._CameraTransform == null) { return; }

            float lToCameraAngle = Vector3Ext.HorizontalAngleTo(mMotionController._Transform.forward, mMotionController._CameraTransform.forward, mMotionController._Transform.up);
            float lRotationAngle = Mathf.Abs(lToCameraAngle);
            float lRotationSign = Mathf.Sign(lToCameraAngle);

            if (!mLinkRotation && lRotationAngle <= (_RotationSpeed / 60f) * TimeManager.Relative60FPSDeltaTime) { mLinkRotation = true; }

            // Record the velocity for our idle pivoting
            if (lRotationAngle < 1f)
            {
                float lVelocitySign = Mathf.Sign(mYawVelocity);
                mYawVelocity = mYawVelocity - (lVelocitySign * rDeltaTime * 10f);

                if (Mathf.Sign(mYawVelocity) != lVelocitySign) { mYawVelocity = 0f; }
            }
            else
            {
                mYawVelocity = lRotationSign * 12f;
            }

            // If we're not linked, rotate smoothly
            if (!mLinkRotation)
            {
                lToCameraAngle = lRotationSign * Mathf.Min((_RotationSpeed / 60f) * TimeManager.Relative60FPSDeltaTime, lRotationAngle);
            }

            Quaternion lRotation = Quaternion.AngleAxis(lToCameraAngle, Vector3.up);
            mActorController.Yaw = mActorController.Yaw * lRotation;
            mActorController._Transform.rotation = mActorController.Tilt * mActorController.Yaw;
        }

        /// <summary>
        /// Raised by the controller when a message is received
        /// </summary>
        public override void OnMessageReceived(IMessage rMessage)
        {
            if (rMessage == null) { return; }
            //if (mActorController.State.Stance != EnumControllerStance.SPELL_CASTING) { return; }

            CombatMessage lCombatMessage = rMessage as CombatMessage;
            if (lCombatMessage != null)
            {
                // Attack messages
                if (lCombatMessage.Attacker == mMotionController.gameObject)
                {
                    // Call for an attack
                    if (rMessage.ID == CombatMessage.MSG_COMBATANT_ATTACK)
                    {
                    }
                    // Attack has been activated (pre attack)
                    else if (rMessage.ID == CombatMessage.MSG_DEFENDER_ATTACKED)
                    {
                    }
                    // Gives us a chance to respond to the defender's reaction (post attack)
                    else if (rMessage.ID == CombatMessage.MSG_DEFENDER_ATTACKED_BLOCKED)
                    {
                    }
                    // Final hit (post attack)
                    else if (rMessage.ID == CombatMessage.MSG_DEFENDER_ATTACKED_PARRIED ||
                             rMessage.ID == CombatMessage.MSG_DEFENDER_DAMAGED ||
                             rMessage.ID == CombatMessage.MSG_DEFENDER_KILLED)
                    {
                    }
                }
                // Defender messages
                else if (lCombatMessage.Defender == mMotionController.gameObject)
                {
                }
            }
        }

        #region Editor Functions

        // **************************************************************************************************
        // Following properties and function only valid while editing
        // **************************************************************************************************

#if UNITY_EDITOR

        /// <summary>
        /// Reset to default values. Reset is called when the user hits the Reset button in the Inspector's 
        /// context menu or when adding the component the first time. This function is only called in editor mode.
        /// </summary>
        public override void Reset()
        {
        }

        /// <summary>
        /// Allow the constraint to render it's own GUI
        /// </summary>
        /// <returns>Reports if the object's value was changed</returns>
        public override bool OnInspectorGUI()
        {
            bool lIsDirty = false;

            if (EditorHelper.TextField("Action Alias", "Action alias that activates and deactivates this movement.", ActionAlias, mMotionController))
            {
                lIsDirty = true;
                ActionAlias = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.Vector3Field("Constant Velocity", "Velocity that is applied every frame.", ConstantVelocity, mMotionController))
            {
                lIsDirty = true;
                ConstantVelocity = EditorHelper.FieldVector3Value;
            }

            GUILayout.Space(5f);

            if (EditorHelper.TextField("Up Action Alias", "Action alias that has us move up.", UpAlias, mMotionController))
            {
                lIsDirty = true;
                UpAlias = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.TextField("Down Action Alias", "Action alias that has us move down.", DownAlias, mMotionController))
            {
                lIsDirty = true;
                DownAlias = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.FloatField("Vertical Speed", "Speed at which we'll move up/down.", VerticalSpeed, mMotionController))
            {
                lIsDirty = true;
                VerticalSpeed = EditorHelper.FieldFloatValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.FloatField("Horizontal Speed", "Speed at which we'll move laterally.", HorizontalSpeed, mMotionController))
            {
                lIsDirty = true;
                HorizontalSpeed = EditorHelper.FieldFloatValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Rotate With Input", "Determines if we rotate based on user input.", RotateWithInput, mMotionController))
            {
                lIsDirty = true;
                RotateWithInput = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("Rotate With Camera", "Determines if we rotate to match the camera.", RotateWithCamera, mMotionController))
            {
                lIsDirty = true;
                RotateWithCamera = EditorHelper.FieldBoolValue;
            }

            if (RotateWithCamera)
            {
                if (EditorHelper.TextField("Rotate Action Alias", "Action alias determines if rotation is activated. This typically matches the input source's View Activator.", RotateActionAlias, mMotionController))
                {
                    lIsDirty = true;
                    RotateActionAlias = EditorHelper.FieldStringValue;
                }
            }

            if (EditorHelper.FloatField("Rotation Speed", "Degrees per second to rotate the actor ('0' means instant rotation).", RotationSpeed, mMotionController))
            {
                lIsDirty = true;
                RotationSpeed = EditorHelper.FieldFloatValue;
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
        public static int STATE_LandToIdle = -1;
        public static int STATE_FallToLand = -1;
        public static int STATE_FallPose = -1;
        public static int STATE_IdlePose = -1;
        public static int TRANS_AnyState_FallPose = -1;
        public static int TRANS_EntryState_FallPose = -1;
        public static int TRANS_LandToIdle_IdlePose = -1;
        public static int TRANS_FallToLand_LandToIdle = -1;
        public static int TRANS_FallPose_FallToLand = -1;

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
                    if (lStateID == STATE_LandToIdle) { return true; }
                    if (lStateID == STATE_FallToLand) { return true; }
                    if (lStateID == STATE_FallPose) { return true; }
                    if (lStateID == STATE_IdlePose) { return true; }
                }

                if (lTransitionID == TRANS_AnyState_FallPose) { return true; }
                if (lTransitionID == TRANS_EntryState_FallPose) { return true; }
                if (lTransitionID == TRANS_LandToIdle_IdlePose) { return true; }
                if (lTransitionID == TRANS_FallToLand_LandToIdle) { return true; }
                if (lTransitionID == TRANS_FallPose_FallToLand) { return true; }
                return false;
            }
        }

        /// <summary>
        /// Used to determine if the actor is in one of the states for this motion
        /// </summary>
        /// <returns></returns>
        public override bool IsMotionState(int rStateID)
        {
            if (rStateID == STATE_LandToIdle) { return true; }
            if (rStateID == STATE_FallToLand) { return true; }
            if (rStateID == STATE_FallPose) { return true; }
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
                if (rStateID == STATE_LandToIdle) { return true; }
                if (rStateID == STATE_FallToLand) { return true; }
                if (rStateID == STATE_FallPose) { return true; }
                if (rStateID == STATE_IdlePose) { return true; }
            }

            if (rTransitionID == TRANS_AnyState_FallPose) { return true; }
            if (rTransitionID == TRANS_EntryState_FallPose) { return true; }
            if (rTransitionID == TRANS_LandToIdle_IdlePose) { return true; }
            if (rTransitionID == TRANS_FallToLand_LandToIdle) { return true; }
            if (rTransitionID == TRANS_FallPose_FallToLand) { return true; }
            return false;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            TRANS_AnyState_FallPose = mMotionController.AddAnimatorName("AnyState -> Base Layer.Levitate-SM.FallPose");
            TRANS_EntryState_FallPose = mMotionController.AddAnimatorName("Entry -> Base Layer.Levitate-SM.FallPose");
            STATE_LandToIdle = mMotionController.AddAnimatorName("Base Layer.Levitate-SM.LandToIdle");
            TRANS_LandToIdle_IdlePose = mMotionController.AddAnimatorName("Base Layer.Levitate-SM.LandToIdle -> Base Layer.Levitate-SM.IdlePose");
            STATE_FallToLand = mMotionController.AddAnimatorName("Base Layer.Levitate-SM.FallToLand");
            TRANS_FallToLand_LandToIdle = mMotionController.AddAnimatorName("Base Layer.Levitate-SM.FallToLand -> Base Layer.Levitate-SM.LandToIdle");
            STATE_FallPose = mMotionController.AddAnimatorName("Base Layer.Levitate-SM.FallPose");
            TRANS_FallPose_FallToLand = mMotionController.AddAnimatorName("Base Layer.Levitate-SM.FallPose -> Base Layer.Levitate-SM.FallToLand");
            STATE_IdlePose = mMotionController.AddAnimatorName("Base Layer.Levitate-SM.IdlePose");
        }

#if UNITY_EDITOR

        private AnimationClip m16578 = null;
        private AnimationClip m16574 = null;
        private AnimationClip m16572 = null;
        private AnimationClip m14540 = null;

        /// <summary>
        /// Creates the animator substate machine for this motion.
        /// </summary>
        protected override void CreateStateMachine()
        {
            // Grab the root sm for the layer
            UnityEditor.Animations.AnimatorStateMachine lRootStateMachine = _EditorAnimatorController.layers[mMotionLayer.AnimatorLayerIndex].stateMachine;
            UnityEditor.Animations.AnimatorStateMachine lSM_25382 = _EditorAnimatorController.layers[mMotionLayer.AnimatorLayerIndex].stateMachine;
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

            UnityEditor.Animations.AnimatorStateMachine lSM_N1608508 = lRootSubStateMachine;
            if (lSM_N1608508 != null)
            {
                for (int i = lSM_N1608508.entryTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_N1608508.RemoveEntryTransition(lSM_N1608508.entryTransitions[i]);
                }

                for (int i = lSM_N1608508.anyStateTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_N1608508.RemoveAnyStateTransition(lSM_N1608508.anyStateTransitions[i]);
                }

                for (int i = lSM_N1608508.states.Length - 1; i >= 0; i--)
                {
                    lSM_N1608508.RemoveState(lSM_N1608508.states[i].state);
                }

                for (int i = lSM_N1608508.stateMachines.Length - 1; i >= 0; i--)
                {
                    lSM_N1608508.RemoveStateMachine(lSM_N1608508.stateMachines[i].stateMachine);
                }
            }
            else
            {
                lSM_N1608508 = lSM_25382.AddStateMachine(_EditorAnimatorSMName, new Vector3(192, -564, 0));
            }

            UnityEditor.Animations.AnimatorState lS_N1612918 = lSM_N1608508.AddState("LandToIdle", new Vector3(720, 264, 0));
            lS_N1612918.speed = 1f;
            lS_N1612918.motion = m16578;

            UnityEditor.Animations.AnimatorState lS_N1612942 = lSM_N1608508.AddState("FallToLand", new Vector3(480, 264, 0));
            lS_N1612942.speed = 1f;
            lS_N1612942.motion = m16574;

            UnityEditor.Animations.AnimatorState lS_N1626950 = lSM_N1608508.AddState("FallPose", new Vector3(240, 264, 0));
            lS_N1626950.speed = 1f;
            lS_N1626950.motion = m16572;

            UnityEditor.Animations.AnimatorState lS_N1644298 = lSM_N1608508.AddState("IdlePose", new Vector3(960, 264, 0));
            lS_N1644298.speed = 1f;
            lS_N1644298.motion = m14540;

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_N1951384 = lRootStateMachine.AddAnyStateTransition(lS_N1626950);
            lT_N1951384.hasExitTime = false;
            lT_N1951384.hasFixedDuration = true;
            lT_N1951384.exitTime = 0.9f;
            lT_N1951384.duration = 0.2f;
            lT_N1951384.offset = 0f;
            lT_N1951384.mute = false;
            lT_N1951384.solo = false;
            lT_N1951384.canTransitionToSelf = true;
            lT_N1951384.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1951384.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1800f, "L0MotionPhase");

            UnityEditor.Animations.AnimatorStateTransition lT_N1644504 = lS_N1612918.AddTransition(lS_N1644298);
            lT_N1644504.hasExitTime = true;
            lT_N1644504.hasFixedDuration = true;
            lT_N1644504.exitTime = 0.8141199f;
            lT_N1644504.duration = 0.116312f;
            lT_N1644504.offset = 0f;
            lT_N1644504.mute = false;
            lT_N1644504.solo = false;
            lT_N1644504.canTransitionToSelf = true;
            lT_N1644504.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_N1612980 = lS_N1612942.AddTransition(lS_N1612918);
            lT_N1612980.hasExitTime = true;
            lT_N1612980.hasFixedDuration = true;
            lT_N1612980.exitTime = 0.5f;
            lT_N1612980.duration = 0.2f;
            lT_N1612980.offset = 0.485189f;
            lT_N1612980.mute = false;
            lT_N1612980.solo = false;
            lT_N1612980.canTransitionToSelf = true;
            lT_N1612980.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;

            UnityEditor.Animations.AnimatorStateTransition lT_N1626992 = lS_N1626950.AddTransition(lS_N1612942);
            lT_N1626992.hasExitTime = false;
            lT_N1626992.hasFixedDuration = true;
            lT_N1626992.exitTime = 1f;
            lT_N1626992.duration = 0.1f;
            lT_N1626992.offset = 0f;
            lT_N1626992.mute = false;
            lT_N1626992.solo = false;
            lT_N1626992.canTransitionToSelf = true;
            lT_N1626992.interruptionSource = (UnityEditor.Animations.TransitionInterruptionSource)0;
            lT_N1626992.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1805f, "L0MotionPhase");

        }

        /// <summary>
        /// Gathers the animations so we can use them when creating the sub-state machine.
        /// </summary>
        public override void FindAnimations()
        {
            m16578 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Jumping/ootii_Jump.fbx/LandToIdle.anim", "LandToIdle");
            m16574 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Jumping/ootii_Jump.fbx/FallToLand.anim", "FallToLand");
            m16572 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Jumping/ootii_Jump.fbx/FallPose.anim", "FallPose");
            m14540 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdlePose.anim", "IdlePose");

            // Add the remaining functionality
            base.FindAnimations();
        }

        /// <summary>
        /// Used to show the settings that allow us to generate the animator setup.
        /// </summary>
        public override void OnSettingsGUI()
        {
            UnityEditor.EditorGUILayout.IntField(new GUIContent("Phase ID", "Phase ID used to transition to the state."), PHASE_START);
            m16578 = CreateAnimationField("LandToIdle", "Assets/ootii/MotionController/Content/Animations/Humanoid/Jumping/ootii_Jump.fbx/LandToIdle.anim", "LandToIdle", m16578);
            m16574 = CreateAnimationField("FallToLand", "Assets/ootii/MotionController/Content/Animations/Humanoid/Jumping/ootii_Jump.fbx/FallToLand.anim", "FallToLand", m16574);
            m16572 = CreateAnimationField("FallPose", "Assets/ootii/MotionController/Content/Animations/Humanoid/Jumping/ootii_Jump.fbx/FallPose.anim", "FallPose", m16572);
            m14540 = CreateAnimationField("IdlePose", "Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdlePose.anim", "IdlePose", m14540);

            // Add the remaining functionality
            base.OnSettingsGUI();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}
