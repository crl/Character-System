using System;
using UnityEngine;
using com.ootii.Geometry;
using com.ootii.Helpers;

namespace com.ootii.Actors.AnimationControllers
{
    /// <summary>
    /// Forward facing strafing walk/run animations.
    /// </summary>
    [MotionName("Basic Walk Run Focus")]
    [MotionDescription("Top down game style movement where the character focus on a mouse, input direction, target position, or target.")]
    public class BasicWalkRunFocus : BasicWalkRunStrafe
    {
        /// <summary>
        /// Index into the stored GameObjects
        /// </summary>
        public int _TargetIndex = -1;
        public int TargetIndex
        {
            get { return _TargetIndex; }

            set
            {
                _TargetIndex = value;
                mTarget = mMotionController.GetStoredGameObject(ref _TargetIndex);
            }
        }

        /// <summary>
        /// Prefab that we can use to create particles from
        /// </summary>
        [NonSerialized]
        protected GameObject mTarget = null;
        public GameObject Target
        {
            get { return mTarget; }

            set
            {
                mTarget = value;

#if UNITY_EDITOR
                if (mMotionController != null)
                {
                    _TargetIndex = mMotionController.StoreGameObject(_TargetIndex, mTarget);
                }
#endif


                if (mTarget != null)
                {
                    _TargetPosition = Vector3.zero;
                    _RotateToInput = false;
                    _RotateToViewInput = false;
                    _RotateToMouse = false;
                }
            }
        }

        /// <summary>
        /// Overrides the facing direction so that the character faces the target
        /// </summary>
        public Vector3 _TargetPosition = Vector3.zero;
        public Vector3 TargetPosition
        {
            get { return _TargetPosition; }

            set
            {
                _TargetPosition = value;

                if (_TargetPosition.sqrMagnitude > 0f)
                {
                    Target = null;
                    _RotateToInput = false;
                    _RotateToViewInput = false;
                    _RotateToMouse = false;
                }
            }
        }

        /// <summary>
        /// Determines if we rotate the character to face the direction of the mouse.
        /// </summary>
        public bool _RotateToMouse = true;
        public bool RotateToMouse
        {
            get { return _RotateToMouse; }

            set
            {
                _RotateToMouse = value;

                if (_RotateToMouse)
                {
                    Target = null;
                    _TargetPosition = Vector3.zero;
                    _RotateToInput = false;
                    _RotateToViewInput = false;
                }
            }
        }

        /// <summary>
        /// Determines if we rotate the character to face the direction of movement input
        /// </summary>
        public bool _RotateToInput = false;
        public bool RotateToInput
        {
            get { return _RotateToInput; }

            set
            {
                _RotateToInput = value;

                if (_RotateToInput)
                {
                    Target = null;
                    _TargetPosition = Vector3.zero;
                    _RotateToMouse = false;
                    _RotateToViewInput = false;
                }
            }
        }

        /// <summary>
        /// Determines if we rotate the character to face the direction of view input
        /// </summary>
        public bool _RotateToViewInput = false;
        public bool RotateToViewInput
        {
            get { return _RotateToViewInput; }

            set
            {
                _RotateToViewInput = value;

                if (_RotateToViewInput)
                {
                    Target = null;
                    _TargetPosition = Vector3.zero;
                    _RotateToMouse = false;
                    _RotateToInput = false;
                }
            }
        }

        /// <summary>
        /// Determines if our movement direction is relative to the camera
        /// </summary>
        public bool _MoveRelativeToCamera = true;
        public bool MoveRelativeToCamera
        {
            get { return _MoveRelativeToCamera; }

            set
            {
                _MoveRelativeToCamera = value;
                
                if (_MoveRelativeToCamera)
                {
                    _MoveRelativeToActor = false;
                }
            }
        }

        /// <summary>
        /// Determines if our movement direction is relative to the character
        /// </summary>
        public bool _MoveRelativeToActor = false;
        public bool MoveRelativeToActor
        {
            get { return _MoveRelativeToActor; }

            set
            {
                _MoveRelativeToActor = value;

                if (_MoveRelativeToActor)
                {
                    _MoveRelativeToCamera = false;
                }
            }
        }

        // Store so we don't lose the previous input
        protected Vector3 mStoredInputForward = Vector3.zero;

        /// <summary>
        /// Default constructor
        /// </summary>
        public BasicWalkRunFocus()
            : base()
        {
            _RequireTarget = false;
            _RotateWithCamera = false;

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "BasicWalkRunStrafe-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public BasicWalkRunFocus(MotionController rController)
            : base(rController)
        {
            _RequireTarget = false;
            _RotateWithCamera = false;

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "BasicWalkRunStrafe-SM"; }
#endif
        }

        /// <summary>
        /// Allows for any processing after the motion has been deserialized
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            // Ensure we grab the stored GameObject
            TargetIndex = _TargetIndex;
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
            rRotation = Quaternion.identity;

            // Override root motion if we're meant to
            float lMovementSpeed = (IsRunActive ? _RunSpeed : _WalkSpeed);
            if (lMovementSpeed > 0f)
            {
                if (_RotateToMouse || _RotateToViewInput || mTarget != null || _TargetPosition.sqrMagnitude > 0f)
                {
                    rMovement = rMovement.normalized;
                }
                else if (_RotateToInput)
                {
                    rMovement = Vector3.forward;
                }

                rMovement = rMovement.normalized * (lMovementSpeed * rDeltaTime);
            }
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

            // Smooth the input so we don't start and stop immediately in the blend tree.
            SmoothInput();

            // Grab the new direction we want
            Quaternion lForwardRotation = mMotionController._Transform.rotation;
            Vector3 lForward = mMotionController._Transform.forward;

            // Rotate the character to the direction of the target
            if (mTarget != null)
            {
                lForward = (mTarget.transform.position - mMotionController._Transform.position).normalized;
                lForwardRotation = Quaternion.LookRotation(lForward, mMotionController._Transform.up);
            }
            // Rotate the character to the direction of the position
            else if (_TargetPosition.sqrMagnitude > 0f)
            {
                lForward = (_TargetPosition - mMotionController._Transform.position).normalized;
                lForwardRotation = Quaternion.LookRotation(lForward, mMotionController._Transform.up);
            }
            // Rotate the character to the direction of the mouse cursor
            else if (_RotateToMouse)
            {
                Vector2 lScreenPosition = Camera.main.WorldToScreenPoint(mMotionController._Transform.position);
                Vector2 lMousePosition = UnityEngine.Input.mousePosition;
                Vector2 lDirection = (lMousePosition - lScreenPosition).normalized;

                lForward = new Vector3(lDirection.x, 0f, lDirection.y);
                if (Camera.main != null) { lForward = Quaternion.AngleAxis(Camera.main.transform.rotation.eulerAngles.y, Vector3.up) * lForward; }

                lForwardRotation = Quaternion.LookRotation(lForward, mMotionController._Transform.up);
            }
            // Rotate the character to the direction of the view input
            else if (_RotateToViewInput)
            {
                Vector3 lStateForward = new Vector3(mMotionController._InputSource.ViewX, 0f, mMotionController._InputSource.ViewY);
                if (lStateForward.sqrMagnitude == 0f) { lStateForward = mStoredInputForward; } else { mStoredInputForward = lStateForward.normalized; }

                if (_MoveRelativeToActor) { lStateForward = mMotionController._Transform.rotation * lStateForward; }
                if (_MoveRelativeToCamera) { lStateForward = Quaternion.AngleAxis(Camera.main.transform.rotation.eulerAngles.y, Vector3.up) * lStateForward; }

                Vector3 lTargetPosition = mMotionController._Transform.position;
                lTargetPosition.x = lTargetPosition.x + lStateForward.x;
                lTargetPosition.z = lTargetPosition.z + lStateForward.z;

                lForward = (lTargetPosition - mMotionController._Transform.position).normalized;

                if (lForward.sqrMagnitude > 0)
                {
                    lForwardRotation = Quaternion.LookRotation(lForward, mMotionController._Transform.up);
                }
            }
            // Rotate the character to the direction of the movement input
            else if (_RotateToInput)
            {
                Vector3 lPosition = mMotionController._Transform.position;

                Vector3 lStateForward = mMotionController.State.InputForward;
                if (lStateForward.sqrMagnitude == 0f) { lStateForward = mStoredInputForward; } else { mStoredInputForward = lStateForward.normalized; }

                if (_MoveRelativeToCamera) { lStateForward = Quaternion.AngleAxis(Camera.main.transform.rotation.eulerAngles.y, Vector3.up) * lStateForward; }

                Vector3 lTargetPosition = lPosition;
                lTargetPosition.x = lTargetPosition.x + lStateForward.x;
                lTargetPosition.z = lTargetPosition.z + lStateForward.z;

                lForward = (lTargetPosition - mMotionController._Transform.position).normalized;

                if (lForward.sqrMagnitude > 0)
                {
                    lForwardRotation = Quaternion.LookRotation(lForward, mMotionController._Transform.up);
                }
            }

            // Rotate to the new direction
            RotateToDirection(lForward, _RotationSpeed, rDeltaTime, ref mRotation);

            if (ShowDebug)
            {
                Graphics.GraphicsManager.DrawLine(mMotionController._Transform.position, mMotionController._Transform.position + lForward, Color.blue);
            }

            // Modify the input values to be relative to our character
            Vector3 lInputForward = new Vector3(mInputX.Average, 0f, mInputY.Average);

            if (_RotateToMouse || mTarget != null || _TargetPosition.sqrMagnitude > 0f)
            {
                if (_MoveRelativeToCamera) { lInputForward = Camera.main.transform.rotation * lInputForward; }
                if (_MoveRelativeToActor) { lInputForward = lForwardRotation * lInputForward; }
                lInputForward = Quaternion.Inverse(lForwardRotation) * lInputForward;
            }
            else if (_RotateToInput)
            {
                lInputForward = Vector3.forward;
            }
            else
            {
                if (_MoveRelativeToCamera) { lInputForward = Camera.main.transform.rotation * lInputForward; }
                if (_MoveRelativeToActor) { lInputForward = lForwardRotation * lInputForward; }
                lInputForward = Quaternion.Inverse(lForwardRotation) * lInputForward;
            }

            mMotionController.State.InputX = lInputForward.x;
            mMotionController.State.InputY = lInputForward.z;
            mMotionController.State.InputForward = lInputForward;

            // Force a style change if needed
            if (_Form <= 0 && mActiveForm != mMotionController.CurrentForm)
            {
                mActiveForm = mMotionController.CurrentForm;
                mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, mActiveForm, 0, true);
            }
        }

        /// <summary>
        /// Used to determine the angle of two screen vectors.
        /// </summary>
        private float ScreenAngle(Vector2 rFrom, Vector2 rTo)
        {
            float lAngle = Mathf.Atan2(rFrom.y - rTo.y, rFrom.x - rTo.x) * Mathf.Rad2Deg;
            return (lAngle + 90f);
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

            if (EditorHelper.IntField("Form", "Sets the LXMotionForm animator property to determine the animation for the motion. If value is < 0, we use the Actor Core's 'Default Form' state.", Form, mMotionController))
            {
                lIsDirty = true;
                Form = EditorHelper.FieldIntValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Default to Run", "Determines if the default is to run or walk.", DefaultToRun, mMotionController))
            {
                lIsDirty = true;
                DefaultToRun = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.TextField("Run Action Alias", "Action alias that triggers a run or walk (which ever is opposite the default).", ActionAlias, mMotionController))
            {
                lIsDirty = true;
                ActionAlias = EditorHelper.FieldStringValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.FloatField("Walk Speed", "Speed (units per second) to move when walking. Set to 0 to use root-motion.", WalkSpeed, mMotionController))
            {
                lIsDirty = true;
                WalkSpeed = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField("Run Speed", "Speed (units per second) to move when running. Set to 0 to use root-motion.", RunSpeed, mMotionController))
            {
                lIsDirty = true;
                RunSpeed = EditorHelper.FieldFloatValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Rotate To Mouse", "Determines if we rotate to face the direction of the mouse pointer.", RotateToMouse, mMotionController))
            {
                lIsDirty = true;
                RotateToMouse = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("Rotate To Input", "Determines if we rotate to face the direction of movement input.", RotateToInput, mMotionController))
            {
                lIsDirty = true;
                RotateToInput = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("Rotate To View Input", "Determines if we rotate to face the direction of view input (Xbox controller only).", RotateToViewInput, mMotionController))
            {
                lIsDirty = true;
                RotateToViewInput = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.ObjectField<GameObject>("Rotate To Object", "Target we'll rotate towards.", Target, mMotionController))
            {
                lIsDirty = true;
                Target = EditorHelper.FieldObjectValue as GameObject;
            }

            if (EditorHelper.Vector3Field("Rotate To Postion", "Target position we'll rotate towards.", TargetPosition))
            {
                lIsDirty = true;
                TargetPosition = EditorHelper.FieldVector3Value;
            }

            if (EditorHelper.FloatField("Rotation Speed", "Degrees per second to rotate the actor.", RotationSpeed, mMotionController))
            {
                lIsDirty = true;
                RotationSpeed = EditorHelper.FieldFloatValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Camera Relative Input", "Determines if we move relative to the camera direction.", MoveRelativeToCamera, mMotionController))
            {
                lIsDirty = true;
                MoveRelativeToCamera = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("Actor Relative Input", "Determines if we move relative to the character direction.", MoveRelativeToActor, mMotionController))
            {
                lIsDirty = true;
                MoveRelativeToActor = EditorHelper.FieldBoolValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.IntField("Smoothing Samples", "Smoothing factor for input. The more samples the smoother, but the less responsive (0 disables).", SmoothingSamples, mMotionController))
            {
                lIsDirty = true;
                SmoothingSamples = EditorHelper.FieldIntValue;
            }

            return lIsDirty;
        }

#endif

        #endregion

    }
}

