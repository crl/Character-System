using System;
using UnityEngine;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Cameras;
using com.ootii.Helpers;
using com.ootii.Geometry;
using com.ootii.Timing;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.MotionControllerPacks
{
    /// <summary>
    /// Very basic idle when swimming.
    /// </summary>
    [MotionName("Swim - Idle")]
    [MotionDescription("Standard idle motion while swimming.")]
    public class Swim_Idle : MotionControllerMotion
    {
        // Enum values for the motion
        public const int PHASE_UNKNOWN = 0;
        public const int PHASE_START = 31300;
        public const int PHASE_STOP_IDLE = 31301;

        /// <summary>
        /// Contains information about the swimmer
        /// </summary>
        [NonSerialized]
        public SwimmerInfo SwimmerInfo = null;

        /// <summary>
        /// Body transform to tilt
        /// </summary>
        public Transform _BodyTransform = null;
        public Transform BodyTransform
        {
            get { return _BodyTransform; }

            set
            {
                _BodyTransform = value;
                if (SwimmerInfo != null) { SwimmerInfo.BodyTransform = value; }
            }
        }

        /// <summary>
        /// Max distance from the surface that we'll test for. Using the surface
        /// found, we'll test the actor's depth
        /// </summary>
        public float _WaterMaxSurfaceTest = 10f;
        public float WaterMaxSurfaceTest
        {
            get { return _WaterMaxSurfaceTest; }

            set
            {
                _WaterMaxSurfaceTest = value;
                if (SwimmerInfo != null) { SwimmerInfo.MaxSurfaceTest = value; }
            }
        }

        /// <summary>
        /// Water layers to trigger entering swimming stance
        /// </summary>
        public int _WaterLayers = 16;
        public int WaterLayers
        {
            get { return _WaterLayers; }

            set
            {
                _WaterLayers = value;
                if (SwimmerInfo != null) { SwimmerInfo.WaterLayers = value; }
            }
        }

        /// <summary>
        /// Distance of the ray to test for entering water
        /// </summary>
        public float _WaterEnterDepth = 1.2f;
        public float WaterEnterDepth
        {
            get { return _WaterEnterDepth; }

            set
            {
                _WaterEnterDepth = value;
                if (SwimmerInfo != null) { SwimmerInfo.EnterDepth = value; }
            }
        }

        /// <summary>
        /// Distance of the ray to test for exiting water
        /// </summary>
        public float _WaterExitDepth = 1.1f;
        public float WaterExitDepth
        {
            get { return _WaterExitDepth; }

            set
            {
                _WaterExitDepth = value;
                if (SwimmerInfo != null) { SwimmerInfo.ExitDepth = value; }
            }
        }

        /// <summary>
        /// Speed at which the actor rises (posative) or sinks (negative)
        /// </summary>
        public float _Buoyancy = 0f;
        public float Buoyancy
        {
            get { return _Buoyancy; }

            set
            {
                _Buoyancy = value;
                if (SwimmerInfo != null) { SwimmerInfo.Buoyancy = value; }
            }
        }

        /// <summary>
        /// Index into the stored GameObjects
        /// </summary>
        public int _RipplesPrefabIndex = -1;
        public int RipplesPrefabIndex
        {
            get { return _RipplesPrefabIndex; }

            set
            {
                _RipplesPrefabIndex = value;
                mRipplesPrefab = mMotionController.GetStoredGameObject(ref _RipplesPrefabIndex);
            }
        }

        /// <summary>
        /// Prefab that we can use to create particles from
        /// </summary>
        [NonSerialized]
        protected GameObject mRipplesPrefab = null;
        public GameObject RipplesPrefab
        {
            get { return mRipplesPrefab; }

            set
            {
                mRipplesPrefab = value;

#if UNITY_EDITOR

                if (!Application.isPlaying && mMotionController != null)
                {
                    _RipplesPrefabIndex = mMotionController.StoreGameObject(_RipplesPrefabIndex, mRipplesPrefab);
                }

#endif

                if (SwimmerInfo != null) { SwimmerInfo.RipplesPrefab = mRipplesPrefab; }

            }
        }

        /// <summary>
        /// Index into the stored GameObjects
        /// </summary>
        public int _SplashPrefabIndex = -1;
        public int SplashPrefabIndex
        {
            get { return _SplashPrefabIndex; }

            set
            {
                _SplashPrefabIndex = value;
                mSplashPrefab = mMotionController.GetStoredGameObject(ref _SplashPrefabIndex);
            }
        }

        /// <summary>
        /// Prefab that we can use to create particles from
        /// </summary>
        [NonSerialized]
        protected GameObject mSplashPrefab = null;
        public GameObject SplashPrefab
        {
            get { return mSplashPrefab; }

            set
            {
                mSplashPrefab = value;

#if UNITY_EDITOR

                if (!Application.isPlaying && mMotionController != null)
                {
                    _SplashPrefabIndex = mMotionController.StoreGameObject(_SplashPrefabIndex, mSplashPrefab);
                }

#endif

                if (SwimmerInfo != null) { SwimmerInfo.SplashPrefab = mSplashPrefab; }

            }
        }

        /// <summary>
        /// Index into the stored GameObjects
        /// </summary>
        public int _UnderwaterPrefabIndex = -1;
        public int UnderwaterPrefabIndex
        {
            get { return _UnderwaterPrefabIndex; }

            set
            {
                _UnderwaterPrefabIndex = value;
                mUnderwaterPrefab = mMotionController.GetStoredGameObject(ref _UnderwaterPrefabIndex);
            }
        }

        /// <summary>
        /// Prefab that we can use to create particles from
        /// </summary>
        [NonSerialized]
        protected GameObject mUnderwaterPrefab = null;
        public GameObject UnderwaterPrefab
        {
            get { return mUnderwaterPrefab; }

            set
            {
                mUnderwaterPrefab = value;

#if UNITY_EDITOR

                if (!Application.isPlaying && mMotionController != null)
                {
                    _UnderwaterPrefabIndex = mMotionController.StoreGameObject(_UnderwaterPrefabIndex, mUnderwaterPrefab);
                }

#endif

                if (SwimmerInfo != null) { SwimmerInfo.UnderwaterPrefab = mUnderwaterPrefab; }

            }
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
        /// Desired degrees of rotation per second
        /// </summary>
        public float _RotationSpeed = 270f;
        public float RotationSpeed
        {
            get { return _RotationSpeed; }

            set
            {
                _RotationSpeed = value;
                mDegreesPer60FPSTick = _RotationSpeed / 60f;
            }
        }

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
        public Swim_Idle()
            : base()
        {
            _Pack = Swim_Idle.GroupName();
            _Category = EnumMotionCategories.IDLE;

            _Priority = 20;

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "Swim_Idle-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public Swim_Idle(MotionController rController)
            : base(rController)
        {
            _Pack = Swim_Idle.GroupName();
            _Category = EnumMotionCategories.IDLE;

            _Priority = 20;

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "Swim_Idle-SM"; }
#endif
        }

        /// <summary>
        /// Allows for any processing after the motion has been deserialized
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            // Ensure we grab the stored GameObject
            RipplesPrefabIndex = _RipplesPrefabIndex;
            SplashPrefabIndex = _SplashPrefabIndex;
            UnderwaterPrefabIndex = _UnderwaterPrefabIndex;

            // Register the swimmer
            if (SwimmerInfo == null)
            {
#if UNITY_EDITOR
                // Temporary hack ******************************************

                if (RipplesPrefab == null && SplashPrefab == null && UnderwaterPrefab == null)
                {
                    Reset();

                    for (int i = 0; i < mMotionLayer.Motions.Count; i++)
                    {
                        if (mMotionLayer.Motions[i] == this)
                        {
                            mMotionLayer.MotionDefinitions[i] = this.SerializeMotion();
                        }
                    }
                }

                // Temporary hack ******************************************
#endif

                SwimmerInfo = new SwimmerInfo();
                SwimmerInfo.ActorController = mActorController;
                SwimmerInfo.MotionController = mMotionController;
                SwimmerInfo.Transform = mMotionController._Transform;
                SwimmerInfo.BodyTransform = _BodyTransform;
                SwimmerInfo.MaxSurfaceTest = _WaterMaxSurfaceTest;
                SwimmerInfo.WaterLayers = _WaterLayers;
                SwimmerInfo.EnterDepth = _WaterEnterDepth;
                SwimmerInfo.ExitDepth = _WaterExitDepth;
                SwimmerInfo.Buoyancy = _Buoyancy;
                SwimmerInfo.RipplesPrefab = mRipplesPrefab;
                SwimmerInfo.SplashPrefab = mSplashPrefab;
                SwimmerInfo.UnderwaterPrefab = mUnderwaterPrefab;
            }

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
            if (!mIsStartable)
            {
                return false;
            }

            // This is only valid if we're in the right stance
            if (mActorController.State.Stance != EnumControllerStance.SWIMMING)
            {
                // If we're moving, we won't use this motion to enter the water;
                if (mMotionController.State.InputMagnitudeTrend.Value < 0.1f)
                {
                    if (SwimmerInfo.TestEnterWater())
                    {
                        SwimmerInfo.WaterSurfaceLastPosition = SwimmerInfo.WaterSurface.position;
                        return true;
                    }
                }
            }
            else
            {
                if (mMotionLayer.ActiveMotion == null)
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
            if (mIsActivatedFrame) { return true; }
            if (mActorController.State.Stance != EnumControllerStance.SWIMMING) { return false; }

            // Ensure we're in the animation
            if (mIsAnimatorActive && !IsInMotionState)
            {
                return false;
            }

            // Ensure we're in water
            if (SwimmerInfo == null || SwimmerInfo.WaterSurface == null)
            {
                return false;
            }            
            
            // If we're surfacing, exit to the idle pose
            if (mMotionLayer._AnimatorStateID == STATE_IdlePose)
            {
                if (SwimmerInfo != null)
                {
                    SwimmerInfo.ExitWater();
                }

                return false;
            }

            // Just incse, ensure we're in water
            float lWaterMovement = SwimmerInfo.WaterSurface.position.y - SwimmerInfo.WaterSurfaceLastPosition.y;
            float lDepth = SwimmerInfo.GetDepth();
            if (lDepth - lWaterMovement <= 0f)
            {
                SwimmerInfo.ExitWater();
                return false;
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
            return true;
        }

        /// <summary>
        /// Called to start the specific motion. If the motion
        /// were something like 'jump', this would start the jumping process
        /// </summary>
        /// <param name="rPrevMotion">Motion that this motion is taking over from</param>
        public override bool Activate(MotionControllerMotion rPrevMotion)
        {
            bool lWasSwimming = (mActorController.State.Stance == EnumControllerStance.SWIMMING);

            // Force the stance
            SwimmerInfo.EnterWater();

            if (rPrevMotion is Jump || rPrevMotion is Fall)
            {
                SwimmerInfo.CreateSplash();
            }

            // Tell the animator to start your animations
            mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, PHASE_START, (lWasSwimming ? 1 : 0), true);

            // Register this motion with the camera
            if (_RotateWithCamera && mMotionController.CameraRig is BaseCameraRig)
            {
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate += OnCameraUpdated;
            }

            // Return
            return base.Activate(rPrevMotion);
        }

        /// <summary>
        /// Called to stop the motion. If the motion is stopable. Some motions
        /// like jump cannot be stopped early
        /// </summary>
        public override void Deactivate()
        {
            // Register this motion with the camera
            if (mMotionController.CameraRig is BaseCameraRig)
            {
                ((BaseCameraRig)mMotionController.CameraRig).OnPostLateUpdate -= OnCameraUpdated;
            }
            
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

            // Do a surface check to see if we're exiting the water
            float lWaterMovement = SwimmerInfo.WaterSurface.position.y - SwimmerInfo.WaterSurfaceLastPosition.y;
            if (mMotionLayer._AnimatorStateID == STATE_treading_water && SwimmerInfo.TestExitWater(lWaterMovement))
            {
                mMotionController.SetAnimatorMotionPhase(mMotionLayer._AnimatorLayerIndex, PHASE_STOP_IDLE, 0, true);
                return;
            }

            // If we're at the surface, we may need to move with the surface
            if (SwimmerInfo.IsAtWaterSurface(0.5f))
            {
                mMovement.y = mMovement.y + lWaterMovement;
                SwimmerInfo.CreateRipples(mMotionController._Transform.position);
            }
            else
            {
                SwimmerInfo.CreateUnderwaterEffect(mMotionController.Animator, HumanBodyBones.Head);
            }

            SwimmerInfo.WaterSurfaceLastPosition = SwimmerInfo.WaterSurface.position;

            // Move based on the buoyancy
            mMovement = mMovement + SwimmerInfo.GetBuoyancy(rDeltaTime);

            // Move vertically
            if (mMotionController._InputSource != null)
            {
                float lUpSpeed = mMotionController._InputSource.GetValue(_UpAlias) * _VerticalSpeed;
                if (SwimmerInfo.IsAtWaterSurface(0f)) { lUpSpeed = 0f; }

                float lDownSpeed = mMotionController._InputSource.GetValue(_DownAlias) * -_VerticalSpeed;
                if (mActorController.IsGrounded) { lDownSpeed = 0f; }

                mMovement.y = mMovement.y + ((lUpSpeed + lDownSpeed) * rDeltaTime); 
            }

            // If we're not dealing with an ootii camera rig, we need to rotate to the camera here
            if (_RotateWithCamera && !(mMotionController.CameraRig is BaseCameraRig))
            {
                OnCameraUpdated(rDeltaTime, rUpdateIndex, null);
            }

            // Rotate as needed
            if (!_RotateWithCamera && _RotateWithInput)
            {
                RotateUsingInput(rDeltaTime, ref mRotation);
            }
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

        #region Editor Methods

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
            RipplesPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ootii/MotionControllerPacks/Swimming/Content/Resources/Prefabs/SurfaceRipples.prefab");
            SplashPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ootii/MotionControllerPacks/Swimming/Content/Resources/Prefabs/SurfaceSplash.prefab");
            UnderwaterPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/ootii/MotionControllerPacks/Swimming/Content/Resources/Prefabs/Underwater.prefab");
        }

        /// <summary>
        /// Clears all the values to the un-initialized state
        /// </summary>
        public override void Clear()
        {
            RipplesPrefab = null;
            SplashPrefab = null;
            UnderwaterPrefab = null;
        }

        /// <summary>
        /// Allow the constraint to render it's own GUI
        /// </summary>
        /// <returns>Reports if the object's value was changed</returns>
        public override bool OnInspectorGUI()
        {
            bool lIsDirty = false;

            float lLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 100f;

            // Temporary hack ******************************************

            if (RipplesPrefab == null && SplashPrefab == null && UnderwaterPrefab == null)
            {
                Reset();
                lIsDirty = true;
            }

            // Temporary hack ******************************************

            EditorHelper.DrawInspectorDescription("Swimmer properties for all motions", MessageType.None);

            // Water layers
            int lNewWaterLayers = EditorHelper.LayerMaskField(new GUIContent("Water Layers", "Layers that we'll use for submersion tests"), WaterLayers);
            if (lNewWaterLayers != WaterLayers)
            {
                lIsDirty = true;
                WaterLayers = lNewWaterLayers;
            }

            if (EditorHelper.FloatField("Surface Test", "Distance from actor to test for water surface", WaterMaxSurfaceTest, mMotionController, 40f))
            {
                lIsDirty = true;
                WaterMaxSurfaceTest = EditorHelper.FieldFloatValue;
            }

            GUILayout.BeginHorizontal();

            EditorGUILayout.LabelField(new GUIContent("Surface Depth", "Distance from actor root that determines if we enter the water."), GUILayout.Width(EditorGUIUtility.labelWidth));
            EditorGUILayout.LabelField("Enter", GUILayout.Width(35f));
            if (EditorHelper.FloatField(WaterEnterDepth, "Enter Depth", mMotionController, 40f))
            {
                lIsDirty = true;
                WaterEnterDepth = EditorHelper.FieldFloatValue;
            }

            GUILayout.Space(5f);

            EditorGUILayout.LabelField("Exit", GUILayout.Width(27f));
            if (EditorHelper.FloatField(WaterExitDepth, "Exit Depth", mMotionController, 40f))
            {
                lIsDirty = true;
                WaterExitDepth = EditorHelper.FieldFloatValue;
            }

            GUILayout.FlexibleSpace();

            GUILayout.EndHorizontal();

            GUILayout.Space(5f);

            if (EditorHelper.ObjectField<Transform>("Body Transform", "Body Transform if you want the actor to tilt when going up and down", BodyTransform, mMotionController))
            {
                lIsDirty = true;
                BodyTransform = EditorHelper.FieldObjectValue as Transform;
            }

            if (EditorHelper.FloatField("Buoyancy", "Determines the units per second the actor rises (posative) or sinks (negative).", Buoyancy, mMotionController))
            {
                lIsDirty = true;
                Buoyancy = EditorHelper.FieldFloatValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.ObjectField<GameObject>("Ripples Prefab", "Prefab that we'll instantiate ripple particles from.", RipplesPrefab, mMotionController))
            {
                lIsDirty = true;
                RipplesPrefab = EditorHelper.FieldObjectValue as GameObject;
            }

            if (EditorHelper.ObjectField<GameObject>("Splash Prefab", "Prefab that we'll instantiate splash particles from.", SplashPrefab, mMotionController))
            {
                lIsDirty = true;
                SplashPrefab = EditorHelper.FieldObjectValue as GameObject;
            }

            if (EditorHelper.ObjectField<GameObject>("Underwater Prefab", "Prefab that we'll instantiate bubble particles from.", UnderwaterPrefab, mMotionController))
            {
                lIsDirty = true;
                UnderwaterPrefab = EditorHelper.FieldObjectValue as GameObject;
            }

            EditorHelper.DrawLine();

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

            if (EditorHelper.FloatField("Vertical Speed", "Speed (units per second) to move up or down.", VerticalSpeed, mMotionController))
            {
                lIsDirty = true;
                VerticalSpeed = EditorHelper.FieldFloatValue;
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

            if (EditorHelper.FloatField("Rotation Speed", "Degrees per second to rotate the actor.", RotationSpeed, mMotionController))
            {
                lIsDirty = true;
                RotationSpeed = EditorHelper.FieldFloatValue;
            }

            EditorGUIUtility.labelWidth = lLabelWidth;

            return lIsDirty;
        }

#endif

        #endregion

        #region Pack Methods

        /// <summary>
        /// Name of the group these motions belong to
        /// </summary>
        public static string GroupName()
        {
            return "Swimming";
        }

#if UNITY_EDITOR

        public static bool sCreateSubStateMachines = true;

        public static bool sCreateInputAliases = true;

        public static bool sCreateMotions = true;

        /// <summary>
        /// Determines if this class represents a starting point for a pack
        /// </summary>
        /// <returns></returns>
        public static string RegisterPack()
        {
            return GroupName();
        }

        /// <summary>
        /// Draws the inspector for the pack
        /// </summary>
        /// <returns></returns>
        public static bool OnPackInspector(MotionController rMotionController)
        {
            EditorHelper.DrawSmallTitle("Swimming");

            GUILayout.Space(5f);

            EditorGUILayout.LabelField("See included documentation:", EditorHelper.SmallBoldLabel);
            EditorGUILayout.LabelField("1. Download and import animations.", EditorHelper.SmallLabel);
            EditorGUILayout.LabelField("2. Unzip and replace animation meta files.", EditorHelper.SmallLabel);
            EditorGUILayout.LabelField("3. Select options and create motions.", EditorHelper.SmallLabel);

            EditorHelper.DrawLine();

            EditorHelper.BoolField("Create Mecanim", "Determines if we create/override the existing sub-state machine", sCreateSubStateMachines);
            sCreateSubStateMachines = EditorHelper.FieldBoolValue;

            GUILayout.Space(5f);

            EditorHelper.BoolField("Create Input Aliases", "Determines if we create input aliases", sCreateInputAliases);
            sCreateInputAliases = EditorHelper.FieldBoolValue;

            EditorHelper.BoolField("Create Motions", "Determines if we create the archery motions", sCreateMotions);
            sCreateMotions = EditorHelper.FieldBoolValue;

            GUILayout.Space(5f);

            if (GUILayout.Button(new GUIContent("Setup Pack", "Create and setup the motion pack."), EditorStyles.miniButton))
            {
                if (sCreateInputAliases)
                {
                    // Run
                    if (!InputManagerHelper.IsDefined("Run"))
                    {
                        InputManagerEntry lEntry = new InputManagerEntry();
                        lEntry.Name = "Run";
                        lEntry.PositiveButton = "left shift";
                        lEntry.Gravity = 1000;
                        lEntry.Dead = 0.001f;
                        lEntry.Sensitivity = 1000;
                        lEntry.Type = InputManagerEntryType.KEY_MOUSE_BUTTON;
                        lEntry.Axis = 0;
                        lEntry.JoyNum = 0;

                        InputManagerHelper.AddEntry(lEntry, true);

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX

            lEntry = new InputManagerEntry();
            lEntry.Name = "Run";
            lEntry.PositiveButton = "";
            lEntry.Gravity = 1;
            lEntry.Dead = 0.3f;
            lEntry.Sensitivity = 1;
            lEntry.Type = InputManagerEntryType.JOYSTICK_AXIS; // Left trigger
            lEntry.Axis = 5;
            lEntry.JoyNum = 0;

            InputManagerHelper.AddEntry(lEntry, true);

#else

                        lEntry = new InputManagerEntry();
                        lEntry.Name = "Run";
                        lEntry.PositiveButton = "";
                        lEntry.Gravity = 1;
                        lEntry.Dead = 0.3f;
                        lEntry.Sensitivity = 1;
                        lEntry.Type = InputManagerEntryType.JOYSTICK_AXIS; // Left trigger
                        lEntry.Axis = 9;
                        lEntry.JoyNum = 0;

                        InputManagerHelper.AddEntry(lEntry, true);

#endif
                    }

                    // Move Up
                    if (!InputManagerHelper.IsDefined("Move Up"))
                    {
                        InputManagerEntry lEntry = new InputManagerEntry();
                        lEntry.Name = "Move Up";
                        lEntry.PositiveButton = "e";
                        lEntry.Gravity = 1000;
                        lEntry.Dead = 0.001f;
                        lEntry.Sensitivity = 1000;
                        lEntry.Type = InputManagerEntryType.KEY_MOUSE_BUTTON;
                        lEntry.Axis = 0;
                        lEntry.JoyNum = 0;

                        InputManagerHelper.AddEntry(lEntry, true);
                    }

                    // Move down
                    if (!InputManagerHelper.IsDefined("Move Down"))
                    {
                        InputManagerEntry lEntry = new InputManagerEntry();
                        lEntry.Name = "Move Down";
                        lEntry.PositiveButton = "q";
                        lEntry.Gravity = 1000;
                        lEntry.Dead = 0.001f;
                        lEntry.Sensitivity = 1000;
                        lEntry.Type = InputManagerEntryType.KEY_MOUSE_BUTTON;
                        lEntry.Axis = 0;
                        lEntry.JoyNum = 0;

                        InputManagerHelper.AddEntry(lEntry, true);
                    }
                }

                if (sCreateMotions || sCreateSubStateMachines)
                {
                    IBaseCameraRig lCameraRig = rMotionController.CameraRig;
                    if (lCameraRig == null) { lCameraRig = rMotionController.ExtractCameraRig(rMotionController.CameraTransform); }

                    Swim_Idle lIdle = rMotionController.CreateMotion<Swim_Idle>(0);

                    if (sCreateMotions)
                    {
                        if (lCameraRig is FollowRig) { lCameraRig = null; }

                        lIdle.RotateWithInput = (lCameraRig == null);
                        lIdle.RotateWithCamera = (lCameraRig != null);
                        rMotionController.SerializeMotion(lIdle);
                    }

                    if (sCreateSubStateMachines)
                    {
                        Animator lAnimator = rMotionController.Animator;
                        if (lAnimator == null) { lAnimator = rMotionController.gameObject.GetComponent<Animator>(); }

                        if (lAnimator != null)
                        {
                            UnityEditor.Animations.AnimatorController lAnimatorController = lAnimator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;

                            lIdle.CreateStateMachine(lAnimatorController);
                        }
                    }

                    Swim_Dive lDive = rMotionController.CreateMotion<Swim_Dive>(0);

                    if (sCreateMotions)
                    {
                        rMotionController.SerializeMotion(lDive);
                    }

                    if (sCreateSubStateMachines)
                    {
                        Animator lAnimator = rMotionController.Animator;
                        if (lAnimator == null) { lAnimator = rMotionController.gameObject.GetComponent<Animator>(); }

                        if (lAnimator != null)
                        {
                            UnityEditor.Animations.AnimatorController lAnimatorController = lAnimator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;

                            lDive.CreateStateMachine(lAnimatorController);
                        }
                    }

                    Swim_Strafe lStrafe = rMotionController.CreateMotion<Swim_Strafe>(0);

                    if (sCreateMotions)
                    {
                        if (lCameraRig is FollowRig) { lCameraRig = null; }

                        lStrafe.RotateWithInput = (lCameraRig == null);
                        lStrafe.RotateWithCamera = (lCameraRig != null);

                        rMotionController.SerializeMotion(lStrafe);
                    }

                    if (sCreateSubStateMachines)
                    {
                        Animator lAnimator = rMotionController.Animator;
                        if (lAnimator == null) { lAnimator = rMotionController.gameObject.GetComponent<Animator>(); }

                        if (lAnimator != null)
                        {
                            UnityEditor.Animations.AnimatorController lAnimatorController = lAnimator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;

                            lStrafe.CreateStateMachine(lAnimatorController);
                        }
                    }

                    Swim_Exit lExit = rMotionController.CreateMotion<Swim_Exit>(0);

                    if (sCreateMotions)
                    {
                         rMotionController.SerializeMotion(lExit);
                    }

                    if (sCreateSubStateMachines)
                    {
                        Animator lAnimator = rMotionController.Animator;
                        if (lAnimator == null) { lAnimator = rMotionController.gameObject.GetComponent<Animator>(); }

                        if (lAnimator != null)
                        {
                            UnityEditor.Animations.AnimatorController lAnimatorController = lAnimator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController;

                            lExit.CreateStateMachine(lAnimatorController);
                        }
                    }
                }

                EditorUtility.DisplayDialog("Swimming Motion Pack", "Motion pack setup.", "ok");

                return true;
            }

            return false;
        }

#endif

        #endregion

        #region Auto-Generated
        // ************************************ START AUTO GENERATED ************************************

        /// <summary>
        /// These declarations go inside the class so you can test for which state
        /// and transitions are active. Testing hash values is much faster than strings.
        /// </summary>
        public static int STATE_treading_water = -1;
        public static int STATE_IdlePose = -1;
        public static int TRANS_AnyState_treading_water = -1;
        public static int TRANS_EntryState_treading_water = -1;
        public static int TRANS_treading_water_IdlePose = -1;

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

                if (lStateID == STATE_treading_water) { return true; }
                if (lStateID == STATE_IdlePose) { return true; }
                if (lTransitionID == TRANS_AnyState_treading_water) { return true; }
                if (lTransitionID == TRANS_EntryState_treading_water) { return true; }
                if (lTransitionID == TRANS_AnyState_treading_water) { return true; }
                if (lTransitionID == TRANS_EntryState_treading_water) { return true; }
                if (lTransitionID == TRANS_treading_water_IdlePose) { return true; }
                return false;
            }
        }

        /// <summary>
        /// Used to determine if the actor is in one of the states for this motion
        /// </summary>
        /// <returns></returns>
        public override bool IsMotionState(int rStateID)
        {
            if (rStateID == STATE_treading_water) { return true; }
            if (rStateID == STATE_IdlePose) { return true; }
            return false;
        }

        /// <summary>
        /// Used to determine if the actor is in one of the states for this motion
        /// </summary>
        /// <returns></returns>
        public override bool IsMotionState(int rStateID, int rTransitionID)
        {
            if (rStateID == STATE_treading_water) { return true; }
            if (rStateID == STATE_IdlePose) { return true; }
            if (rTransitionID == TRANS_AnyState_treading_water) { return true; }
            if (rTransitionID == TRANS_EntryState_treading_water) { return true; }
            if (rTransitionID == TRANS_AnyState_treading_water) { return true; }
            if (rTransitionID == TRANS_EntryState_treading_water) { return true; }
            if (rTransitionID == TRANS_treading_water_IdlePose) { return true; }
            return false;
        }

        /// <summary>
        /// Preprocess any animator data so the motion can use it later
        /// </summary>
        public override void LoadAnimatorData()
        {
            TRANS_AnyState_treading_water = mMotionController.AddAnimatorName("AnyState -> Base Layer.Swim_Idle-SM.treading_water");
            TRANS_EntryState_treading_water = mMotionController.AddAnimatorName("Entry -> Base Layer.Swim_Idle-SM.treading_water");
            TRANS_AnyState_treading_water = mMotionController.AddAnimatorName("AnyState -> Base Layer.Swim_Idle-SM.treading_water");
            TRANS_EntryState_treading_water = mMotionController.AddAnimatorName("Entry -> Base Layer.Swim_Idle-SM.treading_water");
            STATE_treading_water = mMotionController.AddAnimatorName("Base Layer.Swim_Idle-SM.treading_water");
            TRANS_treading_water_IdlePose = mMotionController.AddAnimatorName("Base Layer.Swim_Idle-SM.treading_water -> Base Layer.Swim_Idle-SM.IdlePose");
            STATE_IdlePose = mMotionController.AddAnimatorName("Base Layer.Swim_Idle-SM.IdlePose");
        }

#if UNITY_EDITOR

        private AnimationClip m15642 = null;
        private AnimationClip m14222 = null;

        /// <summary>
        /// Creates the animator substate machine for this motion.
        /// </summary>
        protected override void CreateStateMachine()
        {
            // Grab the root sm for the layer
            UnityEditor.Animations.AnimatorStateMachine lRootStateMachine = _EditorAnimatorController.layers[mMotionLayer.AnimatorLayerIndex].stateMachine;
            UnityEditor.Animations.AnimatorStateMachine lSM_23510 = _EditorAnimatorController.layers[mMotionLayer.AnimatorLayerIndex].stateMachine;
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

            UnityEditor.Animations.AnimatorStateMachine lSM_23568 = lRootSubStateMachine;
            if (lSM_23568 != null)
            {
                for (int i = lSM_23568.entryTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_23568.RemoveEntryTransition(lSM_23568.entryTransitions[i]);
                }

                for (int i = lSM_23568.anyStateTransitions.Length - 1; i >= 0; i--)
                {
                    lSM_23568.RemoveAnyStateTransition(lSM_23568.anyStateTransitions[i]);
                }

                for (int i = lSM_23568.states.Length - 1; i >= 0; i--)
                {
                    lSM_23568.RemoveState(lSM_23568.states[i].state);
                }

                for (int i = lSM_23568.stateMachines.Length - 1; i >= 0; i--)
                {
                    lSM_23568.RemoveStateMachine(lSM_23568.stateMachines[i].stateMachine);
                }
            }
            else
            {
                lSM_23568 = lSM_23510.AddStateMachine(_EditorAnimatorSMName, new Vector3(192, -84, 0));
            }

            UnityEditor.Animations.AnimatorState lS_23790 = lSM_23568.AddState("treading_water", new Vector3(312, 12, 0));
            lS_23790.speed = 1f;
            lS_23790.motion = m15642;

            UnityEditor.Animations.AnimatorState lS_24626 = lSM_23568.AddState("IdlePose", new Vector3(576, 12, 0));
            lS_24626.speed = 1f;
            lS_24626.motion = m14222;

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_23674 = lRootStateMachine.AddAnyStateTransition(lS_23790);
            lT_23674.hasExitTime = false;
            lT_23674.hasFixedDuration = true;
            lT_23674.exitTime = 0.9f;
            lT_23674.duration = 0.2f;
            lT_23674.offset = 0f;
            lT_23674.mute = false;
            lT_23674.solo = false;
            lT_23674.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 31300f, "L0MotionPhase");
            lT_23674.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 0f, "L0MotionParameter");

            // Create the transition from the any state. Note that 'AnyState' transitions have to be added to the root
            UnityEditor.Animations.AnimatorStateTransition lT_23676 = lRootStateMachine.AddAnyStateTransition(lS_23790);
            lT_23676.hasExitTime = false;
            lT_23676.hasFixedDuration = true;
            lT_23676.exitTime = 0.9f;
            lT_23676.duration = 0.05f;
            lT_23676.offset = 0f;
            lT_23676.mute = false;
            lT_23676.solo = false;
            lT_23676.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 31300f, "L0MotionPhase");
            lT_23676.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 1f, "L0MotionParameter");

            UnityEditor.Animations.AnimatorStateTransition lT_24628 = lS_23790.AddTransition(lS_24626);
            lT_24628.hasExitTime = false;
            lT_24628.hasFixedDuration = true;
            lT_24628.exitTime = 0.9166667f;
            lT_24628.duration = 0.25f;
            lT_24628.offset = 0f;
            lT_24628.mute = false;
            lT_24628.solo = false;
            lT_24628.canTransitionToSelf = true;
            lT_24628.AddCondition(UnityEditor.Animations.AnimatorConditionMode.Equals, 31301f, "L0MotionPhase");

        }

        /// <summary>
        /// Gathers the animations so we can use them when creating the sub-state machine.
        /// </summary>
        public override void FindAnimations()
        {
            m15642 = FindAnimationClip("Assets/ootii/MotionControllerPacks/Swimming/Content/Animations/Mixamo/treading_water.fbx/treading_water.anim", "treading_water");
            m14222 = FindAnimationClip("Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdlePose.anim", "IdlePose");

            // Add the remaining functionality
            base.FindAnimations();
        }

        /// <summary>
        /// Used to show the settings that allow us to generate the animator setup.
        /// </summary>
        public override void OnSettingsGUI()
        {
            UnityEditor.EditorGUILayout.IntField(new GUIContent("Phase ID", "Phase ID used to transition to the state."), PHASE_START);
            m15642 = CreateAnimationField("treading_water", "Assets/ootii/MotionControllerPacks/Swimming/Content/Animations/Mixamo/treading_water.fbx/treading_water.anim", "treading_water", m15642);
            m14222 = CreateAnimationField("IdlePose", "Assets/ootii/MotionController/Content/Animations/Humanoid/Idling/unity_Idle_IdleToIdlesR.fbx/IdlePose.anim", "IdlePose", m14222);

            // Add the remaining functionality
            base.OnSettingsGUI();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}