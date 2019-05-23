using UnityEngine;
using com.ootii.Actors;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.LifeCores;
using com.ootii.Collections;
using com.ootii.Geometry;

namespace com.ootii.MotionControllerPacks
{
    /// <summary>
    /// Delegate for swimmer functions
    /// </summary>
    /// <param name="rSwimmer"></param>
    public delegate void SwimmerDelegate(SwimmerInfo rSwimmer);

    /// <summary>
    /// Provides information about the swimmer. This helps us manage
    /// the swimmer state frame to frame.
    /// </summary>
    public class SwimmerInfo
    {

        /// <summary>
        /// Tracks our water state
        /// </summary>
        protected bool mIsInWater = false;
        public bool IsInWater
        {
            get { return mIsInWater; }
            set { mIsInWater = value; }
        }

        /// <summary>
        /// Actor Controller that is the swimmer
        /// </summary>
        protected ActorController mActorController = null;
        public ActorController ActorController
        {
            get { return mActorController; }
            set { mActorController = value; }
        }

        /// <summary>
        /// Motion Controller that is the swimmer
        /// </summary>
        protected MotionController mMotionController = null;
        public MotionController MotionController
        {
            get { return mMotionController; }
            set { mMotionController = value; }
        }

        /// <summary>
        /// Transform that is the swimmer
        /// </summary>
        protected Transform mTransform = null;
        public Transform Transform
        {
            get { return mTransform; }
            set { mTransform = value; }
        }

        /// <summary>
        /// Transform that represents the body (for tilting)
        /// </summary>
        protected Transform mBodyTransform = null;
        public Transform BodyTransform
        {
            get { return mBodyTransform; }
            set { mBodyTransform = value; }
        }

        /// <summary>
        /// Water layers
        /// </summary>
        protected int mWaterLayers = 16;
        public int WaterLayers
        {
            get { return mWaterLayers; }
            set { mWaterLayers = value; }
        }

        /// <summary>
        /// Max depth we'll shoot the ray to see if we're under 
        /// </summary>
        protected float mMaxSurfaceTest = 10f;
        public float MaxSurfaceTest
        {
            get { return mMaxSurfaceTest; }
            set { mMaxSurfaceTest = value; }
        }

        /// <summary>
        /// Height from the swimmers root that they enter swimming
        /// </summary>
        protected float mEnterDepth = 1.3f;
        public float EnterDepth
        {
            get { return mEnterDepth; }
            set { mEnterDepth = value; }
        }

        /// <summary>
        /// Height from the swimmers root that they leave swimming
        /// </summary>
        protected float mExitDepth = 1.2f;
        public float ExitDepth
        {
            get { return mExitDepth; }
            set { mExitDepth = value; }
        }

        /// <summary>
        /// Vertical movement while in the water. This simulates buoyancy
        /// </summary>
        protected float mBuoyancy = 0f;
        public float Buoyancy
        {
            get { return mBuoyancy; }
            set { mBuoyancy = value; }
        }

        /// <summary>
        /// Water surface we are currently under
        /// </summary>
        protected Transform mWaterSurface = null;
        public Transform WaterSurface
        {
            get { return mWaterSurface; }
            set { mWaterSurface = value; }
        }

        /// <summary>
        /// Last position of the water surface (in chase it raises or lowers)
        /// </summary>
        protected Vector3 mWaterSurfaceLastPosition = Vector3.zero;
        public Vector3 WaterSurfaceLastPosition
        {
            get { return mWaterSurfaceLastPosition; }
            set { mWaterSurfaceLastPosition = value; }
        }

        /// <summary>
        /// Prefab containing the ripples effects
        /// </summary>
        public GameObject RipplesPrefab = null;

        /// <summary>
        /// Prefab containing the splash effects
        /// </summary>
        public GameObject SplashPrefab = null;

        /// <summary>
        /// Prefab containing the bubbles effects
        /// </summary>
        public GameObject UnderwaterPrefab = null;

        /// <summary>
        /// Ripples we'll hold on to and create as needed
        /// </summary>
        private ParticleCore mRipplesCore = null;

        /// <summary>
        /// Bubbles we'll hold on to and create as needed
        /// </summary>
        private ParticleCore mUnderwaterCore = null;

        /// <summary>
        /// Delegate for callbacks to the swimmer
        /// </summary>
        public SwimmerDelegate OnSwimEnter = null;

        /// <summary>
        /// Delegate for callbacks to the swimmer
        /// </summary>
        public SwimmerDelegate OnSwimExit = null;

        /// <summary>
        /// Store state values so we can reset them
        /// </summary>
        //private bool mSavedIsGravityEnabled = true;
        //private bool mSavedFixGroundPenetration = true;
        private bool mSavedInvertRotationOrder = false;
        private bool mSavedAllowPushback = false;
        private int mSavedStance = EnumControllerStance.TRAVERSAL;

        /// <summary>
        /// Returns the actor's depth under water. This is based on the water surface's
        /// position-y and the actor's root
        /// </summary>
        /// <returns>Depth the actor us under water or -1 if we don't think they are</returns>
        public float GetDepth()
        {
            float lDepth = -1f;

            RaycastHit lHitInfo;
            Vector3 lStart = mTransform.position + (Vector3.up * mMaxSurfaceTest);

            // Check surface height based on the raycast
            if (RaycastExt.SafeRaycast(lStart, Vector3.down, out lHitInfo, mMaxSurfaceTest, mWaterLayers, mTransform, null, false))
            {
                mWaterSurface = lHitInfo.collider.gameObject.transform;
            }
            // TRT 01/25/2017 - When exiting water, the ray may still hit the water plane and store the surface.
            // This can lead to us thinking low ground with no water has water. So, if we don't hit water
            // stop storing it.
            else if (mWaterSurface != null)
            {
                Vector3 lWaterPoint = mTransform.InverseTransformPoint(mWaterSurface.position);
                if (lWaterPoint.y < 0f)
                {
                    mWaterSurface = null;
                }
            }
            
            // As a sanity check, let's see if we're really far under water
            if (mWaterSurface != null)
            {
                lDepth = mWaterSurface.position.y - mTransform.position.y;
            }

            //Utilities.Debug.Log.ScreenWrite("Depth:" + lDepth.ToString("f3"), 12);

            return lDepth;
        }

        /// <summary>
        /// Determines if we're entering the water
        /// </summary>
        /// <returns></returns>
        public bool TestEnterWater()
        {
            float lDepth = GetDepth();
            return (lDepth >= mEnterDepth);
        }

        /// <summary>
        /// Determine if we're exiting the water
        /// </summary>
        /// <returns></returns>
        public bool TestExitWater(float rBuffer = 0f)
        {
            float lDepth = GetDepth();
            return (lDepth - rBuffer <= mExitDepth);
        }

        /// <summary>
        /// Initializes the swimmer when they enter water
        /// </summary>
        public void EnterWater()
        {
            if (mIsInWater) { return; }
            mIsInWater = true;

            //mSavedIsGravityEnabled = mActorController.IsGravityEnabled;
            mActorController.IsGravityEnabled = false;
            mActorController.ForceGrounding = false;

            //mSavedFixGroundPenetration = mActorController.FixGroundPenetration;
            //mActorController.FixGroundPenetration = false;

            mSavedInvertRotationOrder = mActorController.InvertRotationOrder;
            mActorController.InvertRotationOrder = false;

            mSavedAllowPushback = mActorController.AllowPushback;
            mActorController.AllowPushback = true;

            mSavedStance = mActorController.State.Stance;
            mActorController.State.Stance = EnumControllerStance.SWIMMING;

            if (mWaterSurface != null)
            {
                mWaterSurfaceLastPosition = mWaterSurface.position;
            }

            if (OnSwimEnter != null) { OnSwimEnter(this); }

#if USE_MESSAGE_DISPATCHER
            com.ootii.Messages.MessageDispatcher.SendMessage(mActorController.gameObject, "SWIM_ENTER", this, 0f);
#endif

#if USE_BONE_CONTROLLER
            Actors.BoneControllers.BoneController lBoneController = mActorController.gameObject.GetComponent<Actors.BoneControllers.BoneController>();
            if (lBoneController != null)
            {
                lBoneController.EnableMotors<Actors.BoneControllers.FootGround2BoneMotor>(false);
            }
#endif
        }

        /// <summary>
        /// Initializes the swimmer when they exit water
        /// </summary>
        public void ExitWater()
        {
            if (!mIsInWater) { return; }

            if (OnSwimExit != null) { OnSwimExit(this); }

#if USE_MESSAGE_DISPATCHER
            com.ootii.Messages.MessageDispatcher.SendMessage(mActorController.gameObject, "SWIM_EXIT", this, 0f);
#endif

#if USE_BONE_CONTROLLER
            Actors.BoneControllers.BoneController lBoneController = mActorController.gameObject.GetComponent<Actors.BoneControllers.BoneController>();
            if (lBoneController != null)
            {
                lBoneController.EnableMotors<Actors.BoneControllers.FootGround2BoneMotor>(true);
            }
#endif

            mIsInWater = false;

            // Jumping into water can cause us to save a bad value since the jump turns off gravity
            //mActorController.IsGravityEnabled = mSavedIsGravityEnabled;
            mActorController.IsGravityEnabled = true;
            mActorController.ForceGrounding = true;

            //mActorController.FixGroundPenetration = mSavedFixGroundPenetration;
            mActorController.InvertRotationOrder = mSavedInvertRotationOrder;
            mActorController.AllowPushback = mSavedAllowPushback;
            mActorController.State.Stance = mSavedStance;

            mWaterSurface = null;
            mWaterSurfaceLastPosition = Vector3.zero;
        }

        /// <summary>
        /// Determines if the swimmer is at the water surface
        /// </summary>
        /// <param name="rBuffer">Additional range to be within</param>
        /// <param name="rTestLastPosition">Determines if we'll include the last surface position in our test</param>
        /// <returns></returns>
        public bool IsAtWaterSurface(float rBuffer = 0f, bool rTestLastPosition = true)
        {
            if (mTransform == null) { return false; }

            float lDepth = GetDepth();
            if (lDepth - rBuffer <= mEnterDepth) { return true; }

            if (rTestLastPosition && mWaterSurfaceLastPosition.sqrMagnitude != 0f)
            {
                lDepth = mWaterSurfaceLastPosition.y - mTransform.position.y;
                if (lDepth - rBuffer <= mEnterDepth) { return true; }
            }

            return false;
        }

        /// <summary>
        /// Determines if the swimmer is in shallow water
        /// </summary>
        /// <param name="rGroundLayers">Ground Layer masks to testing the ground</param>
        /// <returns></returns>
        public bool IsInShallowWater(int rGroundLayers = -1)
        {
            if (mTransform == null) { return false; }

            float lDepth = GetDepth();
            if (lDepth > mEnterDepth) { return false; }

            // Test if the ground is close
            Vector3 lStart = mTransform.position + (Vector3.up * mEnterDepth);
            if (RaycastExt.SafeRaycast(lStart, Vector3.down, mEnterDepth + 0.1f, rGroundLayers, mTransform, null, false))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Determines the movement of the character based on thier buoyancy.
        /// </summary>
        /// <returns></returns>
        public Vector3 GetBuoyancy(float rDeltaTime)
        {
            Vector3 lMovement = Vector3.zero;

            if (mBuoyancy != 0f)
            {
                if (mBuoyancy < 0f || !IsAtWaterSurface())
                {
                    lMovement = Vector3.up * mBuoyancy * rDeltaTime;
                }
            }

            return lMovement;
        }

        /// <summary>
        /// Rotates the body based on the vertical movement that is taking place
        /// </summary>
        public void RotateBody(float rAngle, float rLerpTime = 0.1f)
        {
            if (mBodyTransform == null) { return; }

            if (rAngle == 0f)
            {
                mBodyTransform.localRotation = Quaternion.Lerp(mBodyTransform.localRotation, Quaternion.identity, rLerpTime);
            }
            else
            {
                mBodyTransform.localRotation = Quaternion.Lerp(mBodyTransform.localRotation, Quaternion.AngleAxis(rAngle, Vector3.right), rLerpTime);
            }
        }

        /// <summary>
        /// Create the splash that will play on the surface
        /// </summary>
        public void CreateSplash()
        {
            if (mTransform != null)
            {
                CreateSplash(mTransform.position);
            }
            else if (mActorController != null)
            {
                CreateSplash(mActorController._Transform.position);
            }
        }

        /// <summary>
        /// Create the splash that will play on the surface
        /// </summary>
        /// <param name="rPosition"></param>
        public void CreateSplash(Vector3 rPosition)
        {
            if (SplashPrefab != null)
            {
                //GameObject lSplash = GameObject.Instantiate(Resources.Load(mSplashPath)) as GameObject;
                GameObject lSplash = GameObjectPool.Allocate(SplashPrefab);
                if (lSplash != null)
                {
                    ParticleCore lSplashCore = lSplash.GetComponent<ParticleCore>();
                    lSplashCore.Prefab = SplashPrefab;

                    if (mWaterSurface != null) { rPosition.y = mWaterSurface.position.y; }
                    lSplash.transform.position = rPosition;
                }
            }
        }

        /// <summary>
        /// Create the underwater effect. Keep it alive as long as it's active
        /// </summary>
        /// <param name="rAnimator">Mecanim animator this swimmer is tied to</param>
        /// <param name="rBone">Bone the effect is to play from</param>
        public void CreateUnderwaterEffect(Animator rAnimator, HumanBodyBones rBone)
        {
            if (mUnderwaterCore == null)
            {
                if (UnderwaterPrefab != null)
                {
                    //GameObject lUnderwater = GameObject.Instantiate(Resources.Load(mUnderwaterPath)) as GameObject;
                    GameObject lUnderwater = GameObjectPool.Allocate(UnderwaterPrefab);
                    if (lUnderwater != null)
                    {
                        Vector3 lPosition = Vector3.zero;
                        if (rAnimator != null)
                        {
                            Transform lBone = rAnimator.GetBoneTransform(rBone);
                            if (lBone != null) { lPosition = lBone.position; }
                        }

                        mUnderwaterCore = lUnderwater.GetComponent<ParticleCore>();
                        mUnderwaterCore.Prefab = UnderwaterPrefab;
                        mUnderwaterCore.OnReleasedEvent = OnUnderwaterReleased;
                        mUnderwaterCore.Transform.parent = mActorController._Transform;
                        mUnderwaterCore.Transform.position = lPosition;
                    }
                }
            }
            else
            {
                mUnderwaterCore.Age = 0f;

                if (rAnimator != null)
                {
                    Transform lBone = rAnimator.GetBoneTransform(rBone);
                    if (lBone != null)
                    {
                        mUnderwaterCore.Transform.position = lBone.position;
                    }
                }
            }
        }

        /// <summary>
        /// Create the ripples that will play on the surface. I the ripples exist,
        /// we'll simply keep them alive
        /// </summary>
        /// <param name="rPosition"></param>
        public void CreateRipples(Vector3 rPosition)
        {
            if (mRipplesCore == null)
            {
                if (RipplesPrefab != null)
                {
                    //GameObject lRipples = GameObject.Instantiate(RipplesPrefab) as GameObject;
                    GameObject lRipples = GameObjectPool.Allocate(RipplesPrefab);
                    if (lRipples != null)
                    {
                        mRipplesCore = lRipples.GetComponent<ParticleCore>();
                        if (mRipplesCore != null)
                        {
                            mRipplesCore.Prefab = RipplesPrefab;
                            mRipplesCore.OnReleasedEvent = OnRipplesReleased;

                            if (mWaterSurface != null) { rPosition.y = mWaterSurface.position.y; }
                            mRipplesCore.Transform.position = rPosition;
                        }
                    }
                }
            }
            else
            {
                mRipplesCore.Age = 0f;

                if (mWaterSurface != null) { rPosition.y = mWaterSurface.position.y; }
                mRipplesCore.Transform.position = rPosition;
            }
        }

        /// <summary>
        /// Notification that the ripples have been destroyed
        /// </summary>
        /// <param name="rRipples"></param>
        public void OnRipplesReleased(ILifeCore rCore, object rUserData = null)
        {
            mRipplesCore = null;
        }

        /// <summary>
        /// Notification that the bubbles have been destroyed
        /// </summary>
        /// <param name="rRipples"></param>
        public void OnUnderwaterReleased(ILifeCore rCore, object rUserData = null)
        {
            mUnderwaterCore = null;
        }

        /// <summary>
        /// Returns the SwimmerInfo associated with the MC
        /// </summary>
        /// <param name="Transform">Transform that is the swimmer</param>
        /// <returns></returns>
        public static SwimmerInfo GetSwimmerInfo(Transform rTransform)
        {
            if (rTransform == null) { return null; }

            MotionController lMotionController = rTransform.gameObject.GetComponent<MotionController>();
            if (lMotionController == null) { return null; }

            Swim_Idle lMotion = lMotionController.GetMotion<Swim_Idle>();
            if (lMotion == null) { return null; }

            return lMotion.SwimmerInfo;
        }
    }
}
