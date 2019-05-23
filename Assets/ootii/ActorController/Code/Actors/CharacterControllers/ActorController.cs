//#define OOTII_PROFILE

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using com.ootii.Geometry;
using com.ootii.Physics;
using com.ootii.Utilities;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Actors
{
    [Serializable]
    [AddComponentMenu("ootii/Actor Controller")]
    public class ActorController : BaseSystemController, ICharacterController
    {
        /// <summary>
        /// Provides a value for "numerical error "
        /// </summary>
        public const float EPSILON = 0.0001f;
        public const float EPSILON_SQR = 0.00000001f;

        /// <summary>
        /// Fixed math we don't want to recalculate over and over
        /// </summary>
        public const float ONE_OVER_COS45 = 1.41421356237f;

        /// <summary>
        /// Extra spacing between the collision objects
        /// </summary>
        public const float COLLISION_BUFFER = 0.001f;

        /// <summary>
        /// Maximum number of segments we'll process when predicting movement.
        /// </summary>
        public const float MAX_SEGMENTS = 20;

        /// <summary>
        /// Max angle we can be grounded on when using the sphere cast
        /// </summary>
        public const float MAX_GROUNDING_ANGLE = 85f;

        /// <summary>
        /// Enabled/disables the AC, but keep the camera processing
        /// </summary>
        public bool _IsEnabled = true;
        public bool IsEnabled
        {
            get { return _IsEnabled; }

            set
            {
                _IsEnabled = value;

                if (_IsEnabled)
                {
                    // Initialize state info
                    RaycastHit lGroundHitInfo;
                    ProcessGrounding(_Transform.position, Vector3.zero, _Transform.up, _Transform.up, _BaseRadius, out lGroundHitInfo);

                    mState.PrevGround = mState.Ground;
                    mState.Position = _Transform.position;
                    mState.Rotation = _Transform.rotation;

                    mPrevState.Ground = mState.Ground;
                    mPrevState.Position = _Transform.position;
                    mPrevState.Rotation = _Transform.rotation;
                }
            }
        }

        /// <summary>
        /// Ignores movement requests and simply follows the transform
        /// </summary>
        public bool _UseTransformPosition = false;
        public bool UseTransformPosition
        {
            get { return _UseTransformPosition; }

            set
            {
                _UseTransformPosition = value;

                // Ensure our yaw and tilt matches the current rotation
                if (!_UseTransformPosition)
                {
                    _Transform.rotation.DecomposeSwingTwist(_Transform.up, ref mTilt, ref mYaw);
                }
            }
        }

        /// <summary>
        /// Ignores rotation requests and simply follows the transform
        /// </summary>
        public bool _UseTransformRotation = true;
        public bool UseTransformRotation
        {
            get { return _UseTransformRotation; }
            set { _UseTransformRotation = value; }
        }

        /// <summary>
        /// Determines if the base system controller actually runs the "update" cycle
        /// in Unity's Update() or LateUpdate() functions. It's up to the derived class
        /// to respect this flag.
        /// </summary>
        public bool _ProcessInLateUpdate = true;
        public bool ProcessInLateUpdate
        {
            get { return _ProcessInLateUpdate; }
            set { _ProcessInLateUpdate = value; }
        }

        /// <summary>
        /// Defines the number of states that we'll keep active
        /// </summary>
        public int _StateCount = ActorState.STATE_COUNT;
        public int StateCount
        {
            get { return _StateCount; }

            set
            {
                if (value <= 0) { return; }
                if (value == _StateCount) { return; }

                _StateCount = value;
                if (mStateIndex >= _StateCount) { mStateIndex = _StateCount - 1; }

                if (mStates != null)
                {
                    Array.Resize<ActorState>(ref mStates, _StateCount);
                }
            }
        }

        /// <summary>
        /// Determines if we multiply with tilt on the right side or not. Setting the 
        /// value to true is useful for flying
        /// </summary>
        public bool _InvertRotationOrder = false;
        public bool InvertRotationOrder
        {
            get { return _InvertRotationOrder; }
            set { _InvertRotationOrder = value; }
        }

        /// <summary>
        /// Determines if we'll "borrow" physics based force information in the
        /// Update() functions that skip FixedUpdate();
        /// </summary>
        public bool _ExtrapolatePhysics = false;
        public bool ExtrapolatePhysics
        {
            get { return _ExtrapolatePhysics; }
            set { _ExtrapolatePhysics = value; }
        }

        /// <summary>
        /// Determines if we'll use gravity
        /// </summary>
        public bool _IsGravityEnabled = true;
        public bool IsGravityEnabled
        {
            get { return _IsGravityEnabled; }
            set { _IsGravityEnabled = value; }
        }

        /// <summary>
        /// Determines if the gravity is relative to the actor
        /// </summary>
        public bool _IsGravityRelative = false;
        public bool IsGravityRelative
        {
            get { return _IsGravityRelative; }
            set { _IsGravityRelative = value; }
        }

        /// <summary>
        /// If we're using gravity, determines the actual value. A magnitude
        /// of 0 means we use Unity's gravity value.
        /// </summary>
        public Vector3 _Gravity = new Vector3(0f, 0f, 0f);
        public Vector3 Gravity
        {
            get { return _Gravity; }
            set { _Gravity = value; }
        }

        /// <summary>
        /// Determines if we apply gravity in fixed update or late update
        /// </summary>
        public bool _ApplyGravityInFixedUpdate = false;
        public bool ApplyGravityInFixedUpdate
        {
            get { return _ApplyGravityInFixedUpdate; }
            set { _ApplyGravityInFixedUpdate = value; }
        }

        /// <summary>
        /// Default height of the character. This determines how far
        /// up the y axis the main body of the character extends
        /// </summary>
        public float _Height = 1.8f;
        public float Height
        {
            get
            {
                if (_Height <= 0f)
                {
                    for (int i = 0; i < BodyShapes.Count; i++)
                    {
                        float lHeight = 0f;

                        BodyShape lBodyShape = BodyShapes[i];

                        Transform lTransform = (lBodyShape._Transform != null ? lBodyShape._Transform : lBodyShape._Parent);
                        Vector3 lTop = lTransform.position + (lTransform.rotation * lBodyShape._Offset) + (lBodyShape._Parent.up * lBodyShape.Radius);
                        lHeight = Vector3.Distance(lTop, _Transform.position);

                        if (BodyShapes[i] is BodyCapsule)
                        {
                            BodyCapsule lCapsuleShape = BodyShapes[i] as BodyCapsule;

                            Transform lEndTransform = (lCapsuleShape._EndTransform != null ? lCapsuleShape._EndTransform : lCapsuleShape._Parent);
                            Vector3 lEndTop = lEndTransform.position + (lEndTransform.rotation * lCapsuleShape._EndOffset) + (lCapsuleShape._Parent.up * lCapsuleShape.Radius);
                            lHeight = Mathf.Max(Vector3.Distance(lEndTop, _Transform.position), lHeight);
                        }

                        _Height = Mathf.Max(lHeight, _Height);
                    }
                }

                return _Height;
            }

            set
            {
                _Height = value;

                mCenter.y = _Height * 0.5f;
            }
        }

        /// <summary>
        /// Width of the character. This determines how far out on the x and z
        /// axis the main body of the character extends
        /// </summary>
        public float _Radius = 0.35f;
        public float Radius
        {
            get { return _Radius; }
            set { _Radius = value; }
        }

        /// <summary>
        /// Mass of the actor. Since Unity defaults a square unit cube to a mass of
        /// "1" and a typical human is almost 2 square units, we'll default to "2".
        /// </summary>
        public float _Mass = 2f;
        public float Mass
        {
            get { return _Mass; }
            set { _Mass = value; }
        }

        /// <summary>
        /// Represents the center point of the character based on the height
        /// </summary>
        protected Vector3 mCenter = new Vector3(0f, 0.9f, 0f);
        public Vector3 Center
        {
            get { return mCenter; }
        }

        /// <summary>
        /// Skin width used to help force grounding
        /// </summary>
        public float _SkinWidth = 0.01f;
        public float SkinWidth
        {
            get { return _SkinWidth; }
            set { _SkinWidth = value; }
        }

        /// <summary>
        /// Skin width used when we are using the transform for movement. This way,
        /// we won't consider ourselves non-grounded due to the nav mesh or steps.
        /// </summary>
        public float _AltSkinWidth = 0.5f;
        public float AltSkinWidth
        {
            get { return _AltSkinWidth; }
            set { _AltSkinWidth = value; }
        }

        /// <summary>
        /// Distance from the actor's origin that the grounding test will start
        /// </summary>
        public float _GroundingStartOffset = 1f;
        public float GroundingStartOffset
        {
            get { return _GroundingStartOffset; }
            set { _GroundingStartOffset = value; }
        }

        /// <summary>
        /// Distance from the actor's origin that the grounding test will end
        /// </summary>
        public float _GroundingDistance = 3f;
        public float GroundingDistance
        {
            get { return _GroundingDistance; }
            set { _GroundingDistance = value; }
        }

        /// <summary>
        /// Determines if we actually use grounding layers
        /// </summary>
        public bool _IsGroundingLayersEnabled = false;
        public bool IsGroundingLayersEnabled
        {
            get { return _IsGroundingLayersEnabled; }
            set { _IsGroundingLayersEnabled = value; }
        }

        /// <summary>
        /// Layer we'll use to collide (ground) against. The default
        /// value is the 'Default' layer (Layer 1)
        /// </summary>
        public int _GroundingLayers = 1;
        public int GroundingLayers
        {
            get { return _GroundingLayers; }
            set { _GroundingLayers = value; }
        }

        /// <summary>
        /// Dampening factor to multiply lateral movement by when the actor is grounded.
        /// </summary>
        public float _GroundDampenFactor = 0.92f;
        public float GroundDampenFactor
        {
            get { return _GroundDampenFactor; }
            set { _GroundDampenFactor = value; }
        }

        /// <summary>
        /// Radius of the "feet" used to test for ground collisions in the event
        /// that the single ray cast fails.
        /// </summary>
        public float _BaseRadius = 0.1f;
        public float BaseRadius
        {
            get { return _BaseRadius; }
            set { _BaseRadius = value; }
        }

        /// <summary>
        /// Determines if we'll automatically push the actor out of the ground when there is ground penetration
        /// </summary>
        public bool _FixGroundPenetration = true;
        public bool FixGroundPenetration
        {
            get { return _FixGroundPenetration; }
            set { _FixGroundPenetration = value; }
        }

        /// <summary>
        /// Determines if the actor is supposed to be grounded. If so, we'll 
        /// force the actor to the ground (if they are within a minimal range).
        /// </summary>
        public bool _ForceGrounding = true;
        public bool ForceGrounding
        {
            get { return _ForceGrounding; }
            set { _ForceGrounding = value; }
        }

        /// <summary>
        /// When forcing to the ground, the distance that we'll use as the max to clamp.
        /// </summary>
        public float _ForceGroundingDistance = 0.3f;
        public float ForceGroundingDistance
        {
            get { return _ForceGroundingDistance; }
            set { _ForceGroundingDistance = value; }
        }

        /// <summary>
        /// Determines if we'll process collisions
        /// </summary>
        public bool _IsCollisionEnabled = true;
        public bool IsCollsionEnabled
        {
            get { return _IsCollisionEnabled; }
            set { _IsCollisionEnabled = value; }
        }

        /// <summary>
        /// Determines if we'll test the current ground for collisions.
        /// </summary>
        public bool _StopOnRotationCollision = false;
        public bool StopOnRotationCollision
        {
            get { return _StopOnRotationCollision; }
            set { _StopOnRotationCollision = value; }
        }

        /// <summary>
        /// Allows moving objects to push the actor even if he's not moving
        /// </summary>
        public bool _AllowPushback = false;
        public bool AllowPushback
        {
            get { return _AllowPushback; }
            set { _AllowPushback = value; }
        }

        /// <summary>
        /// Layer we'll use to collide against. The default
        /// value is the 'Default' layer (Layer 1)
        /// </summary>
        public int _CollisionLayers = 1;
        public int CollisionLayers
        {
            get { return _CollisionLayers; }
            set { _CollisionLayers = value; }
        }

        /// <summary>
        /// Radius of the actor. This is used when determining the overlap of other objects
        /// </summary>
        public float _OverlapRadius = 0.9f;
        public float OverlapRadius
        {
            get { return _OverlapRadius; }
            set { _OverlapRadius = value; }
        }

        /// <summary>
        /// Center point where the overlap for collisions are tested
        /// </summary>
        public Vector3 _OverlapCenter = new Vector3(0f, 0.9f, 0f);
        public Vector3 OverlapCenter
        {
            get { return _OverlapCenter; }
            set { _OverlapCenter = value; }
        }

        /// <summary>
        /// Determines if we allow characters to slide
        /// </summary>
        public bool _IsSlidingEnabled = false;
        public bool IsSlidingEnabled
        {
            get { return _IsSlidingEnabled; }
            set { _IsSlidingEnabled = value; }
        }

        /// <summary>
        /// Slope at which the character starts sliding down. They
        /// can still go up it, but they are slowed down based on gravity.
        /// </summary>
        public float _MinSlopeAngle = 20f;
        public float MinSlopeAngle
        {
            get { return _MinSlopeAngle; }
            set { _MinSlopeAngle = value; }
        }

        /// <summary>
        /// When we're on a slope larger than the MinSlopeAngle, the percentage
        /// of gravity that is applied to our downward slide.
        /// </summary>
        public float _MinSlopeGravityCoefficient = 1f;
        public float MinSlopeGravityCoefficient
        {
            get { return _MinSlopeGravityCoefficient; }
            set { _MinSlopeGravityCoefficient = value; }
        }

        /// <summary>
        /// Max slope angle the character can go up
        /// </summary>
        public float _MaxSlopeAngle = 45f;
        public float MaxSlopeAngle
        {
            get { return _MaxSlopeAngle; }
            set { _MaxSlopeAngle = value; }
        }

        /// <summary>
        /// Determines if we'll use the step height to help test the max
        /// slope the character can go up.
        /// </summary>
        public bool _UseStepHeightWithMaxSlope = true;
        public bool UseStepHeightWithMaxSlope
        {
            get { return _UseStepHeightWithMaxSlope; }
            set { _UseStepHeightWithMaxSlope = value; }
        }

        /// <summary>
        /// For slope testing, movement is broken up into small
        /// chunks and collisions are tested. This is the max size
        /// of those movement steps.
        /// </summary>
        public float _SlopeMovementStep = 0.01f;
        public float SlopeMovementStep
        {
            get { return _SlopeMovementStep; }
            set { _SlopeMovementStep = value; }
        }

        /// <summary>
        /// Determines if our actor orients to match the angle of the ground.
        /// </summary>
        public bool _OrientToGround = false;
        public bool OrientToGround
        {
            get { return _OrientToGround; }

            set
            {
                _OrientToGround = value;

                if (_OrientToGround)
                {
                    mOrientToGroundNormal = _Transform.up;
                    _Transform.rotation.DecomposeSwingTwist(Vector3.up, ref mTilt, ref mYaw);
                }
                else
                {
                    // If we're not on the natural ground, allow us to fall 
                    // and collide as we should
                    mState.IsTilting = true;

                    // If we're already on the natural ground, make sure we align
                    // ourselves correctly so there is no "bump".
                    if (_Transform.up == Vector3.up)
                    {
                        mYaw = _Transform.rotation;
                        mTilt = Quaternion.identity;
                    }
                }
            }
        }

        /// <summary>
        /// Determines if we keep the last orientation while the avatar is in the air (ie jumping)
        /// </summary>
        public bool _KeepOrientationInAir = false;
        public bool KeepOrientationInAir
        {
            get { return _KeepOrientationInAir; }
            set { _KeepOrientationInAir = value; }
        }

        /// <summary>
        /// Maximum distance to stay oriented as the actor falls. 
        /// </summary>
        public float _OrientToGroundDistance = 2f;
        public float OrientToGroundDistance
        {
            get { return _OrientToGroundDistance; }
            set { _OrientToGroundDistance = value; }
        }

        /// <summary>
        /// Max time it will take the actor to rotate 180 degrees
        /// </summary>
        public float _OrientToGroundSpeed = 1f;
        public float OrientToGroundSpeed
        {
            get { return _OrientToGroundSpeed; }
            set { _OrientToGroundSpeed = value; }
        }

        /// <summary>
        /// Minimum angle needed before we start doing a slow rotation to the orientation.
        /// Otherwise, it's instant.
        /// </summary>
        public float _MinOrientToGroundAngleForSpeed = 5f;
        public float MinOrientToGroundAngleForSpeed
        {
            get { return _MinOrientToGroundAngleForSpeed; }
            set { _MinOrientToGroundAngleForSpeed = value; }
        }

        /// <summary>
        /// Max height the character can just walk up
        /// </summary>
        public float _MaxStepHeight = 0.3f;
        public float MaxStepHeight
        {
            get { return _MaxStepHeight; }
            set { _MaxStepHeight = value; }
        }

        /// <summary>
        /// Minimum step depth to help with determining slopes
        /// </summary>
        public float _MinStepDepth = 0.25f;
        public float MinStepDepth
        {
            get { return _MinStepDepth; }
            set { _MinStepDepth = value; }
        }

        /// <summary>
        /// Speed at which we smoothly move up steps
        /// </summary>
        public float _StepUpSpeed = 0.75f;
        public float StepUpSpeed
        {
            get { return _StepUpSpeed; }
            set { _StepUpSpeed = value; }
        }

        /// <summary>
        /// Maximum angle that we'll allow smooth stepping up.
        /// </summary>
        public float _MaxStepUpAngle = 5f;
        public float MaxStepUpAngle
        {
            get { return _MaxStepUpAngle; }
            set { _MaxStepUpAngle = value; }
        }

        /// <summary>
        /// Speed at which we smoothly move down steps
        /// </summary>
        public float _StepDownSpeed = 0.75f;
        public float StepDownSpeed
        {
            get { return _StepDownSpeed; }
            set { _StepDownSpeed = value; }
        }

        /// <summary>
        /// Determines if we prevent movement on the x axis
        /// </summary>
        public bool _FreezePositionX = false;
        public bool FreezePositionX
        {
            get { return _FreezePositionX; }
            set { _FreezePositionX = value; }
        }

        /// <summary>
        /// Determines if we prevent movement on the y axis
        /// </summary>
        public bool _FreezePositionY = false;
        public bool FreezePositionY
        {
            get { return _FreezePositionY; }
            set { _FreezePositionY = value; }
        }

        /// <summary>
        /// Determines if we prevent movement on the z axis
        /// </summary>
        public bool _FreezePositionZ = false;
        public bool FreezePositionZ
        {
            get { return _FreezePositionZ; }
            set { _FreezePositionZ = value; }
        }

        /// <summary>
        /// Determines if we prevent rotation on the x axis
        /// </summary>
        public bool _FreezeRotationX = false;
        public bool FreezeRotationX
        {
            get { return _FreezeRotationX; }
            set { _FreezeRotationX = value; }
        }

        /// <summary>
        /// Determines if we prevent rotation on the y axis
        /// </summary>
        public bool _FreezeRotationY = false;
        public bool FreezeRotationY
        {
            get { return _FreezeRotationY; }
            set { _FreezeRotationY = value; }
        }

        /// <summary>
        /// Determines if we prevent rotation on the z axis
        /// </summary>
        public bool _FreezeRotationZ = false;
        public bool FreezeRotationZ
        {
            get { return _FreezeRotationZ; }
            set { _FreezeRotationZ = value; }
        }

        /// <summary>
        /// By enabling this flag, most processing is disabled and the
        /// actor controller applies movement and rotations directly. Use this
        /// when an animation or other process will totally control movement.
        /// </summary>
        protected bool mOverrideProcessing = false;
        public bool OverrideProcessing
        {
            get { return mOverrideProcessing; }
            set { mOverrideProcessing = value; }
        }

        /// <summary>
        /// Determines if the AC will show debug info at all
        /// </summary>
        public bool _ShowDebug = false;
        public bool ShowDebug
        {
            get { return _ShowDebug; }
            set { _ShowDebug = value; }
        }

        /// <summary>
        /// List of shapes that define the rough shape of the actor
        /// for collision detection. Since Unity doesn't serialize well,
        /// we don't use inheritance here.
        /// </summary>
        [NonSerialized]
        public List<BodyShape> BodyShapes = new List<BodyShape>();

        /// <summary>
        /// Determines if we're actually on the ground
        /// </summary>
        public bool IsGrounded
        {
            get { return mState.IsGrounded; }
        }

        /// <summary>
        /// Length of time the actor has been grounded
        /// </summary>
        protected float mGroundedDuration = 0f;
        public float GroundedDuration
        {
            get { return mGroundedDuration; }
        }

        /// <summary>
        /// Length of time the actor has been falling
        /// </summary>
        protected float mFallDuration = 0f;
        public float FallDuration
        {
            get { return mFallDuration; }
        }

        /// <summary>
        /// Previous position
        /// </summary>
        public Quaternion Rotation
        {
            get { return _Transform.rotation; }
        }

        /// <summary>
        /// Current yaw rotation of the character without any ground tilt
        /// </summary>
        protected Quaternion mYaw = Quaternion.identity;
        public Quaternion Yaw
        {
            get { return mYaw; }

            set
            {
                mYaw = value;
                _Transform.rotation = (_InvertRotationOrder ? mYaw * mTilt : mTilt * mYaw);
            }
        }

        /// <summary>
        /// Current tilt (pitch/roll) rotation of the character without any yaw
        /// </summary>
        protected Quaternion mTilt = Quaternion.identity;
        public Quaternion Tilt
        {
            get { return mTilt; }

            set
            {
                mTilt = value;
                _Transform.rotation = (_InvertRotationOrder ? mYaw * mTilt : mTilt * mYaw);
            }
        }

        /// <summary>
        /// Previous position
        /// </summary>
        public Vector3 Position
        {
            get { return _Transform.position; }
        }

        /// <summary>
        /// Current velocity
        /// </summary>
        public Vector3 Velocity
        {
            get { return mState.Velocity; }
        }

        /// <summary>
        /// Determines if we're previously grounded
        /// </summary>
        public bool PrevIsGrounded
        {
            get { return mPrevState.IsGrounded; }
        }

        /// <summary>
        /// Previous position
        /// </summary>
        public Vector3 PrevPosition
        {
            get { return mPrevState.Position; }
        }

        /// <summary>
        /// Previous velocity
        /// </summary>
        public Vector3 PrevVelocity
        {
            get { return mPrevState.Velocity; }
        }

        /// <summary>
        /// The current state of the controller including speed, direction, etc.
        /// </summary>
        protected ActorState mState = new ActorState();
        public ActorState State
        {
            get { return mState; }
            set { mState = value; }
        }

        /// <summary>
        /// The previous state of the controller including speed, direction, etc.
        /// </summary>
        protected ActorState mPrevState = new ActorState();
        public ActorState PrevState
        {
            get { return mPrevState; }
            set { mPrevState = value; }
        }

        /// <summary>
        /// Use this to store up gravitational and force velocity over time
        /// </summary>
        private Vector3 mAccumulatedVelocity = Vector3.zero;
        public Vector3 AccumulatedVelocity
        {
            get { return mAccumulatedVelocity; }
            set { mAccumulatedVelocity = value; }
        }

        /// <summary>
        /// Used to store the gravitational and force position change over time
        /// </summary>
        private Vector3 mAccumulatedMovement = Vector3.zero;

        /// <summary>
        /// Used to store the non-gravitational force that is applied. This isn't
        /// used for calculations, but for conditional logic.
        /// </summary>
        private Vector3 mAccumulatedForceVelocity = Vector3.zero;

        /// <summary>
        /// Contains a list of forces currently being applied to
        /// the controller.
        /// </summary>
        protected List<Force> mAppliedForces = new List<Force>();
        public List<Force> AppliedForces
        {
            get { return mAppliedForces; }
            set { mAppliedForces = value; }
        }

        /// <summary>
        /// Determines if we temporarily ignore the "Use Transform" option
        /// </summary>
        protected bool mIgnoreUseTransform = false;
        public bool IgnoreUseTransform
        {
            get { return mIgnoreUseTransform; }
            set { mIgnoreUseTransform = value; }
        }

        /// <summary>
        /// Allows for external processing prior to the actor controller doing it's 
        /// work this frame.
        /// </summary>
        protected ControllerLateUpdateDelegate mOnControllerPreLateUpdate = null;
        public ControllerLateUpdateDelegate OnControllerPreLateUpdate
        {
            get { return mOnControllerPreLateUpdate; }
            set { mOnControllerPreLateUpdate = value; }
        }

        /// <summary>
        /// Allows for external processing after the actor controller doing it's 
        /// work this frame.
        /// </summary>
        protected ControllerLateUpdateDelegate mOnControllerPostLateUpdate = null;
        public ControllerLateUpdateDelegate OnControllerPostLateUpdate
        {
            get { return mOnControllerPostLateUpdate; }
            set { mOnControllerPostLateUpdate = value; }
        }

        /// <summary>
        /// Callback that allows the caller to change the final position/rotation
        /// before it's set on the actual transform.
        /// </summary>
        protected ControllerMoveDelegate mOnPreControllerMove = null;
        public ControllerMoveDelegate OnPreControllerMove
        {
            get { return mOnPreControllerMove; }
            set { mOnPreControllerMove = value; }
        }

        /// <summary>
        /// Current state index that we are using
        /// </summary>
        protected int mStateIndex = 0;
        public int StateIndex
        {
            get { return mStateIndex; }
        }

        /// <summary>
        /// Tracks the states so we can reapply or use as needed
        /// </summary>
        protected ActorState[] mStates = null;
        public ActorState[] States
        {
            get { return mStates; }
        }

        /// <summary>
        /// Number of Update() functions that occured where there was no FixedUpdate()
        /// </summary>
        protected float mFixedUpdates = 0f;
        public float FixedUpdates
        {
            get { return mFixedUpdates; }
        }

        /// <summary>
        /// Keeps us from re-calculating the world-up over and over
        /// </summary>
        protected Vector3 mWorldUp = Vector3.up;

        /// <summary>
        /// Speed to orient to the desired direction
        /// </summary>
        protected float mOrientToSpeed = 0f;

        /// <summary>
        /// Normal we'll use to orient the actor to the ground.
        /// </summary>
        protected Vector3 mOrientToGroundNormal = Vector3.up;

        /// <summary>
        /// Target ground normal we're attempting to reach
        /// </summary>
        protected Vector3 mOrientToGroundNormalTarget = Vector3.up;

        /// <summary>
        /// Ground that we want to attach the actor to
        /// </summary>
        protected Transform mTargetGround = null;

        /// <summary>
        /// Sets the ground normal that we'll try to orient the actor to
        /// </summary>
        protected Vector3 mTargetGroundNormal = Vector3.zero;

        /// <summary>
        /// Forces an absolute rotation during the next frame update
        /// </summary>
        protected Quaternion mTargetRotation = new Quaternion(float.MaxValue, 0f, 0f, 0f);

        /// <summary>
        /// Determines the amount of rotation that should occurin the frame
        /// </summary>
        protected Quaternion mTargetRotate = Quaternion.identity;

        /// <summary>
        /// Determines the amount of rotation that should occurin the frame
        /// </summary>
        protected Quaternion mTargetTilt = Quaternion.identity;

        /// <summary>
        /// Determines the speed at which we'll keep rotating
        /// </summary>
        protected Vector3 mTargetRotationVelocity = Vector3.zero;

        /// <summary>
        /// Forces an absolute position during the next frame update
        /// </summary>
        protected Vector3 mTargetPosition = new Vector3(float.MaxValue, 0f, 0f);

        /// <summary>
        /// Determines the amount of movement that should occur in the frame
        /// </summary>
        protected Vector3 mTargetMove = Vector3.zero;

        /// <summary>
        /// Determines the speed at which we'll keep moving
        /// </summary>
        protected Vector3 mTargetVelocity = Vector3.zero;

        /// <summary>
        /// Movement borrowed from the next fixed update while extrapolationg
        /// </summary>
        protected Vector3 mBorrowedFixedMovement = Vector3.zero;

        /// <summary>
        /// Current step speed
        /// </summary>
        protected float mCurrentStepSpeed = 0f;

        /// <summary>
        /// Colliders that impact the actor
        /// </summary>
        protected List<BodyShapeHit> mBodyShapeHits = new List<BodyShapeHit>();

        /// <summary>
        /// Since Unity can't serialize polymorphic lists correctly (even with ScriptableObjects),
        /// we need to do this work around where w serialize things using the definitions.
        /// </summary>
        [SerializeField]
        protected List<string> mBodyShapeDefinitions = new List<string>();

        /// <summary>
        /// List that determines what colliders will be ignored
        /// </summary>
        [NonSerialized]
        protected List<Collider> mIgnoreCollisions = null;
        protected List<Transform> mIgnoreTransforms = null;

        /// <summary>
        /// Used primarily for debuggin
        /// </summary>
        public int IgnoreCollisionCount
        {
            get
            {
                if (mIgnoreCollisions == null) { return 0; }
                return mIgnoreCollisions.Count;
            }
        }

        // Amount of change we're using for smooth stepping
        protected Vector3 mSmoothStepModifier = Vector3.zero;

#if UNITY_EDITOR
        // Debug information about the step movement
        protected List<Vector3> mStepPositions = new List<Vector3>();
#endif

        /// <summary>
        /// Once the objects are instanciated, awake is called before start. Use it
        /// to setup references to other objects
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
        }

        /// <summary>
        /// Used for initialization before Update() occurs
        /// </summary>
        void Start()
        {
            mCenter.y = _Height * 0.5f;

            // Initialize the states
            mStates = new ActorState[_StateCount];
            for (int i = 0; i < mStates.Length; i++) { mStates[i] = new ActorState(); }

            mPrevState = mStates[mStates.Length - 2];
            mState = mStates[mStates.Length - 1];

            // Cache the transform
            _Transform = transform;

            // Initialize the orientation
            mOrientToGroundNormal = _Transform.up;
            _Transform.rotation.DecomposeSwingTwist(Vector3.up, ref mTilt, ref mYaw);

            // Create the body shapes from the definitions
            DeserializeBodyShapes();

            // Create the unity colliders as needed
            for (int i = 0; i < BodyShapes.Count; i++)
            {
                if (BodyShapes[i]._UseUnityColliders)
                {
                    BodyShapes[i].CreateUnityColliders();
                }
            }

            // Initialize state info
            RaycastHit lGroundHitInfo;
            ProcessGrounding(_Transform.position, Vector3.zero, _Transform.up, _Transform.up, _BaseRadius, out lGroundHitInfo);

            mState.PrevGround = mState.Ground;
            mState.Position = _Transform.position;
            mState.Rotation = _Transform.rotation;

            mPrevState.Ground = mState.Ground;
            mPrevState.Position = _Transform.position;
            mPrevState.Rotation = _Transform.rotation;

            // Temporary to ensure we don't have bugs
            //if (_BaseRadius == 0.1f) { _BaseRadius = 0.2f; }
            //if (_StepUpSpeed == 3f || _StepUpSpeed == 1.5f) { _StepUpSpeed = 0.75f; }
            //if (_StepDownSpeed == 3f || _StepDownSpeed == 1.5f) { _StepDownSpeed = 0.75f; }
            //if (_ForceGroundingDistance == 0.05f) { _ForceGroundingDistance = 0.3f; }
        }

        /// <summary>
        /// Applies an instant force. As an impulse, the force of a full second
        /// is automatically applied. The resulting impulse is Force / delta-time.
        /// The impulse is immediately removed after being applied.
        /// </summary>
        /// <param name="rForce">Force including direction and magnitude</param>
        public void AddImpulse(Vector3 rForce)
        {
            // Only add forces during the first update cycle. This way, we
            // don't double forces due to slower frame rates
            if (mUpdateIndex != 1) { return; }

            // Create the force array
            if (mAppliedForces == null) { mAppliedForces = new List<Force>(); }

            // Add the resulting force
            Force lForce = Force.Allocate();
            lForce.Type = ForceMode.Impulse;
            lForce.Value = rForce;
            lForce.StartTime = Time.time;
            lForce.Duration = 0f;

            mAppliedForces.Add(lForce);

            // Temporarily ignore the UseTransform
            mIgnoreUseTransform = true;
        }

        /// <summary>
        /// Applies a force to the avatar over time. 
        /// 
        /// DO NOT USE AddForce AS YOUR PRIMARY METHOD OF MOVING YOUR CHARACTER.
        /// IT IS NOT A RIGID-BODY. USE Move() or RelativeMove().
        /// </summary>
        /// <param name="rForce">Force including direction and magnitude. This is applied each tick for the duration.</param>
        /// <param name="rDuration">Number of seconds to apply the force for (0f is infinite)</param>
        public void AddForce(Vector3 rForce, float rDuration)
        {
            // Only add forces during the first update cycle. This way, we
            // don't double forces due to slower frame rates
            if (mUpdateIndex != 1) { return; }

            // Create the force array
            if (mAppliedForces == null) { mAppliedForces = new List<Force>(); }

            // Allocate the force
            Force lForce = Force.Allocate();
            lForce.Type = ForceMode.Force;
            lForce.Value = rForce;
            lForce.StartTime = Time.time;
            lForce.Duration = rDuration;

            mAppliedForces.Add(lForce);

            // Temporarily ignore the UseTransform
            mIgnoreUseTransform = true;
        }

        /// <summary>
        /// Provides a reliable update time for us to evaluate physics based properties
        /// </summary>
        protected void FixedUpdate()
        {
            //Log.FileWrite("FixedUpdate()");

            // Report that we're not skipping this time
            mFixedUpdates = 0;

            // Since we're grounded, clear out all accumulated velocity
            if (mState.IsGrounded) { mAccumulatedForceVelocity = Vector3.zero; }

            // Gather any forces that have been applied
            Vector3 lForceVelocity = ProcessForces(Time.fixedDeltaTime);
            mAccumulatedForceVelocity = mAccumulatedForceVelocity + lForceVelocity;

            // Setup the world up. It can change based on settings
            mWorldUp = Vector3.up;
            if (_IsGravityRelative)
            {
                // If we're grounded, our "world up" is an average of the last
                // set of ground normals. This ensures we avoid one-off bumps (ie edges)
                if (mState.IsGrounded)
                {
                    mWorldUp = Vector3.zero;

                    int lStateCount = Mathf.Min(_StateCount, 20);
                    for (int i = 0; i < lStateCount; i++)
                    {
                        int lStateIndex = (mStateIndex + i < _StateCount ? mStateIndex + i : mStateIndex + i - _StateCount);
                        if (mStates[lStateIndex] != null) { mWorldUp = mWorldUp + mStates[lStateIndex].GroundSurfaceDirectNormal; }
                    }

                    mWorldUp = mWorldUp.normalized;
                }
                // If we're not grounded, we may head towards the natural world up
                else
                {
                    mWorldUp = Vector3.up;

                    if (mTargetGroundNormal.sqrMagnitude > 0f)
                    {
                        mWorldUp = mTargetGroundNormal;
                    }
                    else if (_KeepOrientationInAir || (mAccumulatedForceVelocity.sqrMagnitude == 0f && mState.GroundSurfaceDistance < _OrientToGroundDistance))
                    {
                        if (mState.GroundSurfaceDirectNormal.sqrMagnitude == 0f)
                        {
                            mWorldUp = _Transform.up;
                        }
                        else
                        {
                            mWorldUp = mState.GroundSurfaceDirectNormal;
                        }
                    }
                }
            }
            // TRT 01/11/17: Added to ensure world up is represented correctly
            else if (_Gravity.sqrMagnitude > 0f)
            {
                mWorldUp = -_Gravity.normalized;
            }
            // TRT 01/11/17: Added to ensure world up is represented correctly
            else if (UnityEngine.Physics.gravity.sqrMagnitude > 0f)
            {
                mWorldUp = -UnityEngine.Physics.gravity.normalized;
            }

            // As needed, clear out any accumulated velocity
            if (mState.IsGrounded)
            {
                Vector3 lVerticalAccumulatedVelocity = Vector3.Project(mAccumulatedVelocity, mWorldUp);
                if (Vector3.Dot(lVerticalAccumulatedVelocity.normalized, mWorldUp) <= 0f)
                {
                    Vector3 lLateralAccumulatedVelocity = mAccumulatedVelocity - lVerticalAccumulatedVelocity;

                    lVerticalAccumulatedVelocity = Vector3.zero;
                    lLateralAccumulatedVelocity = lLateralAccumulatedVelocity * _GroundDampenFactor;

                    mAccumulatedVelocity = lVerticalAccumulatedVelocity + lLateralAccumulatedVelocity;
                }
            }

            // Determine the gravity to apply
            Vector3 lWorldGravity = Vector3.zero;

            if (_IsGravityEnabled && _ApplyGravityInFixedUpdate)
            {
                // Setup the world up. It can change based on settings
                lWorldGravity = (_Gravity.sqrMagnitude == 0f ? UnityEngine.Physics.gravity : _Gravity);
                if (_IsGravityRelative) { lWorldGravity = -mWorldUp * lWorldGravity.magnitude; }

                // Accumulate the gravity over time
                mAccumulatedVelocity = mAccumulatedVelocity + (lWorldGravity * Time.fixedDeltaTime);
            }

            // Apply the forces to the currect velociy (which includes gravity)
            mAccumulatedVelocity = mAccumulatedVelocity + lForceVelocity;

            if (mAccumulatedVelocity.sqrMagnitude > 0f && _ApplyGravityInFixedUpdate)
            {
                Vector3 lFixedMovement = mAccumulatedVelocity * Time.fixedDeltaTime;

                // If we borrowed movement due to a skipped fixed update, reduce the current value
                if (lFixedMovement.sqrMagnitude < mBorrowedFixedMovement.sqrMagnitude)
                {
                    lFixedMovement = Vector3.zero;
                }
                else
                {
                    lFixedMovement = lFixedMovement - mBorrowedFixedMovement;
                }

                // Accumulate the movement
                mAccumulatedMovement = mAccumulatedMovement + lFixedMovement;
            }

            // Clear out any borrowed movement
            mBorrowedFixedMovement = Vector3.zero;
        }

        /// <summary>
        /// Called every frame to perform processing. We only use
        /// this function if it's not called by another component.
        /// </summary>
        protected override void LateUpdate()
        {
            // The BaseSystemController.LateUpdate will actually drive our
            // ControllerLateUpdate that we'll use to perform processing
            base.LateUpdate();

            // Allow hooks to process after we do our update this frame
            if (mOnControllerPostLateUpdate != null)
            {
                // Do as many updates as we need to in order to simulate
                // the desired frame rates
                if (mUpdateCount > 0)
                {
                    for (int i = 1; i <= mUpdateCount; i++)
                    {
                        mOnControllerPostLateUpdate(this, _DeltaTime, i);
                    }
                }
                // In this case, there shouldn't be an update. This typically
                // happens when the true FPS is much faster than our desired FPS
                else
                {
                    mOnControllerPostLateUpdate(this, _DeltaTime, 0);
                }
            }
        }

        /// <summary>
        /// Update logic for the controller should be done here. This allows us
        /// to support dynamic and fixed update times
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        public override void ControllerUpdate(float rDeltaTime, int rUpdateIndex)
        {
            if (!_ProcessInLateUpdate)
            {
                InternalUpdate(rDeltaTime, rUpdateIndex);
            }
        }

        /// <summary>
        /// LateUpdate logic for the controller should be done here. This allows us
        /// to support dynamic and fixed update times
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        public override void ControllerLateUpdate(float rDeltaTime, int rUpdateIndex)
        {
            if (_ProcessInLateUpdate)
            {
                InternalUpdate(rDeltaTime, rUpdateIndex);
            }
        }

        /// <summary>
        /// Force the internal update to occur. This is useful for multiplayer situations where you want
        /// the AC to update itself to support serverside authoratative movement.
        /// </summary>
        /// <param name="rDeltaTime"></param>
        public void ForceInternalUpdate(float rDeltaTime)
        {
            InternalUpdate(rDeltaTime, 1);
        }

        /// <summary>
        /// Update logic for the controller should be done here. This allows us
        /// to support dynamic and fixed update times
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        private void InternalUpdate(float rDeltaTime, int rUpdateIndex)
        {
            //Log.FileWrite("AC.CLU() dt:" + rDeltaTime.ToString("f6") + " ui:" + rUpdateIndex + " udt:" + Time.deltaTime.ToString("f6"));

            // If it's not time for an update, wait for the next frame
            if (rUpdateIndex == 0) { return; }

#if OOTII_PROFILE
            Utilities.Profiler.Start("CLU0");
#endif

            Vector3 lActorUp = _Transform.up;
            //Vector3 lActorPosition = _Transform.position;

            // ----------------------------------------------------------------------
            // PROCESS CONTROLLERS
            // ----------------------------------------------------------------------

            // Allow hooks to process before we handle the movement this frame
            if (mOnControllerPreLateUpdate != null)
            {
                mOnControllerPreLateUpdate(this, rDeltaTime, rUpdateIndex);
            }

            // If we are not enabled, stop
            if (!_IsEnabled) { return; }

            // ----------------------------------------------------------------------
            // SHIFT STATES
            // ----------------------------------------------------------------------

#if OOTII_PROFILE
            Utilities.Profiler.Start("CLU1");
#endif

            // Move the current state into the previous and clear the current
            mPrevState = mState;

            mStateIndex = ActorState.Shift(ref mStates, mStateIndex);
            mState = mStates[mStateIndex];

#if OOTII_PROFILE
            Utilities.Profiler.Stop("CLU1");
#endif

            // ----------------------------------------------------------------------
            // UPDATE BODY SHAPES
            // ----------------------------------------------------------------------
            for (int i = 0; i < BodyShapes.Count; i++)
            {
                BodyShapes[i].LateUpdate();
            }

            // ----------------------------------------------------------------------
            // ROTATION
            // ----------------------------------------------------------------------
            Quaternion lFrameYaw = Quaternion.identity;
            Quaternion lFrameTilt = Quaternion.identity;

            // Use the transform rotation
            if (!mIgnoreUseTransform && _UseTransformPosition && _UseTransformRotation)
            {
                mTargetRotate = Quaternion.identity;
                mTargetRotation = Quaternion.identity;
                mTargetRotation.x = float.MaxValue;
                mTargetRotationVelocity = Vector3.zero;

                Quaternion lFinalRotation = _Transform.rotation;

                lFinalRotation.DecomposeSwingTwist(_Transform.up, ref mTilt, ref mYaw);

                mState.Rotation = lFinalRotation;
                mState.RotationYaw = mYaw;
                mState.RotationTilt = mTilt;
            }
            // Force the rotation if we need to
            else if (mTargetRotation.x != float.MaxValue)
            {
                // Force the rotation
                mYaw = mTargetRotation;
                mTilt = mTargetTilt;

                mState.Rotation = (_InvertRotationOrder ? mYaw * mTilt : mTilt * mYaw);
                _Transform.rotation = mState.Rotation;

                // Clear out any values for the next frame
                mTargetRotation = Quaternion.identity;
                mTargetRotation.x = float.MaxValue;

                mTargetTilt = Quaternion.identity;
            }
            else
            {
                if (!_FreezeRotationY)
                {
                    // Determine the actual movement that will occur based on velocity
                    Quaternion lTargetRotate = Quaternion.Euler(mTargetRotationVelocity * rDeltaTime);
                    lFrameYaw = lFrameYaw * lTargetRotate;

                    // Add any explicit rotation that may have been set
                    lFrameYaw = lFrameYaw * mTargetRotate;
                }

                if (!_FreezeRotationX)
                {
                    lFrameTilt = lFrameTilt * mTargetTilt;
                }

                mTargetRotate = Quaternion.identity;
                mTargetTilt = Quaternion.identity;
            }

            // ----------------------------------------------------------------------
            // POSITION
            // ----------------------------------------------------------------------

            // Use the transform position
            if (!mIgnoreUseTransform && _UseTransformPosition)
            {
                mTargetMove = Vector3.zero;
                mTargetVelocity = Vector3.zero;
                mTargetPosition = Vector3.zero;
                mTargetPosition.x = float.MaxValue;
                mAccumulatedForceVelocity = Vector3.zero;
                if (mAppliedForces != null) { mAppliedForces.Clear(); }

                mYaw = mYaw * lFrameYaw;
                mTilt = mTilt * lFrameTilt;

                Vector3 lFinalPosition = _Transform.position;

                mState.Position = lFinalPosition;

                // Determine the per second velocity
                mState.Velocity = (mState.Position - mPrevState.Position) / Time.deltaTime; // (Time.smoothDeltaTime > 0f ? Time.smoothDeltaTime : Time.deltaTime);

                // If we're not overriding the grounding, see if we are grounded
                if (mTargetGround == null)
                {
                    RaycastHit lHitInfo;
                    mState.IsGrounded = ProcessGrounding(_Transform.position, Vector3.zero, _Transform.up, mWorldUp, _BaseRadius, out lHitInfo);

                    // Determine if we're tilting
                    if (mState.IsGrounded && _OrientToGround)
                    {
                        if (Mathf.Abs(mState.GroundSurfaceAngle) > _MinOrientToGroundAngleForSpeed)
                        {
                            Vector3 lTiltUp = (mTilt * mYaw).Up();
                            mTilt = QuaternionExt.FromToRotation(lTiltUp, mState.GroundSurfaceDirectNormal) * mTilt;

                            mState.Rotation = mTilt * mYaw;
                            mState.RotationYaw = mYaw;
                            mState.RotationTilt = mTilt;
                        }
                    }
                }
                // Override the ground information if we're meant to
                else
                {
                    mState.IsGrounded = true;
                    mState.Ground = mTargetGround;
                    mState.GroundPosition = mTargetGround.position;
                    mState.GroundRotation = mTargetGround.rotation;
                }

                // Apply the rotation value here
                _Transform.rotation = mState.Rotation;
            }
            // Force the position if we need to
            else if (mTargetPosition.x != float.MaxValue)
            {
                mYaw = mYaw * lFrameYaw;
                mTilt = mTilt * lFrameTilt;

                // Track if the user initiated the movement
                mState.IsMoveRequested = true;

                // Zero out any velocity that exists velocity
                mState.Velocity = Vector3.zero;

                // Force the position
                mState.Position = mTargetPosition;
                _Transform.position = mTargetPosition;

                // Do a simple check to see if we're still grounded
                RaycastHit lGroundHitInfo;
                ProcessGrounding(_Transform.position, mState.MovementPlatformAdjust, lActorUp, mWorldUp, _BaseRadius, out lGroundHitInfo);

                // Clear out any target values for the next frame
                mTargetPosition.x = float.MaxValue;
            }
            // Move to the next position
            else
            {
                //UpdateMovement_old(rDeltaTime, rUpdateIndex, lFrameYaw, lFrameTilt);
                UpdateMovement(rDeltaTime, rUpdateIndex, lFrameYaw, lFrameTilt);
            }
        }

        /// <summary>
        /// Processes movement to come up with a the next position
        /// </summary>
        public void UpdateMovement(float rDeltaTime, int rUpdateIndex, Quaternion rFrameYaw, Quaternion rFrameTilt)
        {
            // Store some variables we use a lot
            Vector3 lActorUp = _Transform.up;

            // Holds the resulting changes
            Vector3 lFinalMovement = Vector3.zero;
            Quaternion lFinalRotate = Quaternion.identity;

            // ----------------------------------------------------------------------
            // Gather requested movement
            // ----------------------------------------------------------------------

            // Determine the actual movement that will occur based on velocity
            mState.Movement = mTargetVelocity * rDeltaTime;

            // Add explicit movement that may have been set
            mState.Movement = mState.Movement + mTargetMove;
            mTargetMove = Vector3.zero;

            // Track if the user initiated the movement
            mState.IsMoveRequested = (mState.Movement.sqrMagnitude > ActorController.EPSILON_SQR);

            if (mState.IsMoveRequested)
            {
                //int x = 0;
            }
            else
            {
                //int x = 0;
            }

            // ----------------------------------------------------------------------
            // Apply any platform movement
            // ----------------------------------------------------------------------

            if (mPrevState.IsGrounded)
            {
                ProcessPlatforming(mPrevState);
            }

            // ----------------------------------------------------------------------
            // Apply frame rotation (after platform rotation)
            // ----------------------------------------------------------------------
            mYaw = mYaw * rFrameYaw;
            mTilt = mTilt * rFrameTilt;

            // ----------------------------------------------------------------------
            // Determine the gravitational force
            // ----------------------------------------------------------------------

            // Since gravity and things like jump are physics based, we want them to run in fixed update. This gives us a
            // nice consistant velocity and make jumps and falling consistant. However, FixedUpdate and Update aren't in 
            // synch. So, if we don't adjust, we can get falling that feels like it stutters. If we find that a fixed update
            // is skipped, we'll "borrow" some velocity from the next frame to try and smooth it out a bit.
            if (_ApplyGravityInFixedUpdate)
            {
                if (_ExtrapolatePhysics && mFixedUpdates > 0f)
                {
                    mBorrowedFixedMovement = mAccumulatedVelocity * (rDeltaTime / (mFixedUpdates + 1f));
                    mAccumulatedMovement = mBorrowedFixedMovement;
                }

                // Determine our final movement based on the forces 
                mState.MovementForceAdjust = mAccumulatedMovement;

                // Clear the movement so that we only apply the value during fixed updates.
                mFixedUpdates++;
                mAccumulatedMovement = Vector3.zero;
            }
            else
            {
                if (_IsGravityEnabled)
                {
                    // Setup the world up. It can change based on settings
                    Vector3 lWorldGravity = (_Gravity.sqrMagnitude == 0f ? UnityEngine.Physics.gravity : _Gravity);
                    if (_IsGravityRelative) { lWorldGravity = -mWorldUp * lWorldGravity.magnitude; }

                    // Accumulate the gravity over time
                    mAccumulatedVelocity = mAccumulatedVelocity + (lWorldGravity * rDeltaTime);

                    // Determine our final movement based on the forces 
                    mState.MovementForceAdjust = mAccumulatedVelocity * rDeltaTime;
                }
            }

            // Determine the vertical component of the force. If it's pushing us into the ground
            // and we're already on the ground, we'll remove the vertical component.
            Vector3 lVerticalMovementForceAdjust = Vector3.Project(mState.MovementForceAdjust, lActorUp);
            float lVerticalMovementForceAdjustDot = Vector3.Dot(lVerticalMovementForceAdjust, lActorUp);

            if (mPrevState.IsGrounded && lVerticalMovementForceAdjustDot < 0f)
            {
                mState.MovementForceAdjust = mState.MovementForceAdjust - lVerticalMovementForceAdjust;

                lVerticalMovementForceAdjust = Vector3.zero;
                lVerticalMovementForceAdjustDot = 0f;
            }

            // ----------------------------------------------------------------------
            // Process in steps in case of ultra fast movement
            // ----------------------------------------------------------------------

#if UNITY_EDITOR
            mStepPositions.Clear();
            mStepPositions.Add(_Transform.position);
#endif

            float lStepDistance = 0.1f;

            float lGroundDistance = 0f;

            Vector3 lLastPosition = _Transform.position;

            Vector3 lDesiredMovement = mState.Movement + mState.MovementPlatformAdjust + mState.MovementForceAdjust;
            float lDesiredDistance = lDesiredMovement.magnitude;
            Vector3 lDesiredDirection = lDesiredMovement.normalized;

            Vector3 lActualMovement = Vector3.zero;
            float lActualDistance = 0f;

            while (true)
            {
                lActualDistance = lActualDistance + lStepDistance;
                if (lActualDistance > lDesiredDistance)
                {
                    lStepDistance = lStepDistance - (lActualDistance - lDesiredDistance);
                    lActualDistance = lDesiredDistance;
                }

                lActualMovement = lActualMovement + (lDesiredDirection * lStepDistance);

                // ----------------------------------------------------------------------
                // Determine if we're grounded
                // ----------------------------------------------------------------------

                Vector3 lPositionForGrounding = _Transform.position + lActualMovement + mState.MovementGroundAdjust + mState.MovementCounterAdjust;

                // Get the distance to the ground (even if we penetrate)
                lGroundDistance = GetGroundDistance(lPositionForGrounding, lActorUp);

                // Penetration has occurred and we need to raise up
                if (lGroundDistance < _SkinWidth)
                {
                    mState.IsGrounded = true;

                    if ((_ForceGrounding && lGroundDistance > 0f) || (_FixGroundPenetration && lGroundDistance < 0f))
                    {
                        mState.MovementGroundAdjust = mState.MovementGroundAdjust + (lActorUp * -lGroundDistance);
                    }
                }
                // A gap exists, force grounding if we're meant to
                // TODO: The _FixGroundPenetration really isn't wanted, but I'm supporting existing motions (for now)
                else if (_ForceGrounding && _FixGroundPenetration)
                {
                    // If a force is pushing us down, we'll clamp to the ground. In the case of a jump where
                    // a force pushes us up, we don't want to clamp
                    if (lVerticalMovementForceAdjustDot < EPSILON)
                    {
                        float lGroundingDistance = (_ForceGroundingDistance > 0f ? _ForceGroundingDistance : _SkinWidth);
                        if (mPrevState.IsGrounded && lGroundDistance < lGroundingDistance)
                        {
                            mState.IsGrounded = true;
                            mState.MovementGroundAdjust = mState.MovementGroundAdjust + (lActorUp * -lGroundDistance);
                        }
                    }
                }

                // ----------------------------------------------------------------------
                // Apply sliding
                // ----------------------------------------------------------------------

                if (_IsSlidingEnabled)
                {
                    // We'll only slide if we arn't trying to move and we have bounds
                    if (mState.IsGrounded &&
                        (_MinSlopeAngle > 0f || _MaxSlopeAngle > 0f) &&
                        (mState.GroundSurfaceAngle >= _MaxSlopeAngle || !mState.IsMoveRequested))
                    {
                        float lPercent = 1f;
                        float lMaxSlopeAngle = (_MaxSlopeAngle > 0f ? _MaxSlopeAngle : MAX_GROUNDING_ANGLE);
                        float lMinSlopeAngle = (_MinSlopeAngle > 0f ? _MinSlopeAngle : lMaxSlopeAngle * 0.5f);

                        // Only slide if we're firmly on an angle (meaning this frame and last)
                        if (mState.GroundSurfaceAngle > lMinSlopeAngle && mPrevState.GroundSurfaceAngle > lMinSlopeAngle)
                        {
                            lPercent = (mState.GroundSurfaceAngle - lMinSlopeAngle) / (lMaxSlopeAngle - lMinSlopeAngle);

                            // Setup the world gravity. It can change based on settings
                            Vector3 lWorldGravity = (_Gravity.sqrMagnitude == 0f ? UnityEngine.Physics.gravity : _Gravity);

                            // Determine the gravitation component on the slope
                            Vector3 lSlopeRight = Vector3.Cross(mState.GroundSurfaceNormal, lWorldGravity.normalized);
                            if (lSlopeRight.sqrMagnitude == 0f) { lSlopeRight = Vector3.Cross(mState.GroundSurfaceNormal, -mWorldUp); }

                            Vector3 lSlopeDirection = Vector3.Cross(lSlopeRight, mState.GroundSurfaceNormal);

                            mState.MovementSlideAdjust = lSlopeDirection * (lWorldGravity.magnitude * _MinSlopeGravityCoefficient * lPercent * rDeltaTime);
                        }
                    }
                }

                // ----------------------------------------------------------------------
                // Test for collisions
                // ----------------------------------------------------------------------

                if (_IsCollisionEnabled)
                {
                    Vector3 lPositionForColliding = _Transform.position + lActualMovement + mState.MovementGroundAdjust + mState.MovementSlideAdjust + mState.MovementCounterAdjust;

                    Vector3 lDelta = lLastPosition - _Transform.position;
                    Vector3 lStepMovement = lPositionForColliding - lLastPosition;
                    Vector3 lSafeMovement = lStepMovement;
                    Vector3 lRemainingMovement = Vector3.zero;

                    // Test for collisions and get the safe movement and the remaining movement
                    bool lHasCollisions = ProcessCollisions(lDelta, ref lSafeMovement, ref lRemainingMovement);
                    if (lHasCollisions) { mState.IsColliding = true; }

                    // Remove the remaining movement from out overall movement (this leaves us with safe movement)
                    mState.MovementCounterAdjust = mState.MovementCounterAdjust - lRemainingMovement;

                    // See if we can deflect the remaining movement
                    if (lRemainingMovement.sqrMagnitude > EPSILON_SQR)
                    {
                        // Calculate the deflected movement
                        Vector3 lDeflectedMovement = lRemainingMovement - Vector3.Project(lRemainingMovement, mState.ColliderHit.normal);

                        // Render out information about the collision
                        if (_ShowDebug)
                        {
                            Graphics.GraphicsManager.DrawArrow(mState.ColliderHit.point, mState.ColliderHit.point + mState.ColliderHit.normal, Color.red, null, 5f);
                            Graphics.GraphicsManager.DrawArrow(mState.ColliderHit.point, mState.ColliderHit.point + lDeflectedMovement.normalized, Color.blue, null, 5f);
                        }

                        // If we're grounded, we may limit the vertical deflection
                        if (mState.IsGrounded)
                        {
                            // If the deflected movement is pushing us into the ground, then we may need to remove the vertical component
                            Vector3 lVerticalDeflectedMovement = Vector3.Project(lDeflectedMovement, lActorUp);
                            float lVerticalDeflectedDot = Vector3.Dot(lVerticalDeflectedMovement, lActorUp);

                            // Remove any push into the air
                            if (lVerticalDeflectedDot > 0f)
                            {
                                lDeflectedMovement = lDeflectedMovement - lVerticalDeflectedMovement;
                            }
                            // Remove any push into the ground
                            else if (lVerticalDeflectedDot < 0f)
                            {
                                // TRT 06/21/2017 - We don't want to remove vertical deflection unless we're right at the ground
                                if (mState.MovementGroundAdjust.magnitude < _SkinWidth)
                                {
                                    lDeflectedMovement = lDeflectedMovement - lVerticalDeflectedMovement;
                                }
                            }
                        }

                        // If we've deflected back against the direction we wanted to move, remove it to prevent stuttering and oscillating.
                        Vector3 lDeflectedMovementProj = Vector3.Project(lDeflectedMovement, mState.Movement.normalized);
                        float lDeflectedMovementDot = Vector3.Dot(lDeflectedMovementProj.normalized, mState.Movement.normalized);
                        if (lDeflectedMovementDot < 0f)
                        {
                            lDeflectedMovement = lDeflectedMovement - lDeflectedMovementProj;
                        }

                        // If the collision is pushing us down, but the accumulated velocity is pushing us up... cancel it
                        Vector3 lHitDirection = Vector3.Project(mState.ColliderHit.normal, lActorUp);

                        // TRT 06/21/2017 - Adding some buffer so that we don't prevent jumping 
                        // agains near-vertical surfaces.
                        float lDownHitDirectionDot = Vector3.Dot(lHitDirection, lActorUp);
                        if (lDownHitDirectionDot < -0.05f)
                        {
                            Vector3 lVerticalAccumulatedGravity = Vector3.Project(mAccumulatedVelocity, lActorUp);
                            if (Vector3.Dot(lVerticalAccumulatedGravity.normalized, lActorUp) > 0f)
                            {
                                mAccumulatedVelocity = Vector3.zero;
                            }
                        }

                        // TRT 06/21/2017 - Only set the property based on horizontal deflection
                        Vector3 lHorizontalDeflectedMovement = lDeflectedMovement - Vector3.Project(lDeflectedMovement, lActorUp);
                        mState.IsMovementBlocked = (lHorizontalDeflectedMovement.sqrMagnitude < 0.0002f);

//                        // If we've gone straight into a wall, there is no deflected movement and we want to get out of the stepping.
//                        if (mState.Movement.sqrMagnitude > 0f && lDeflectedMovement.sqrMagnitude < EPSILON_SQR)
//                        {
//                            mState.IsMovementBlocked = true;

//                            lLastPosition = _Transform.position + lActualMovement + mState.MovementGroundAdjust + mState.MovementSlideAdjust + mState.MovementCounterAdjust;
//#if UNITY_EDITOR
//                            mStepPositions.Add(lLastPosition);
//#endif
//                            break;
//                        }

                        // Change the direction of any remaining future steps
                        lDesiredDirection = lDeflectedMovement.normalized;
                        lDesiredDistance = lDesiredDistance - (lRemainingMovement.magnitude - lDeflectedMovement.magnitude);

                        // Now, test if this deflection is going to cause a collision
                        lRemainingMovement = Vector3.zero;
                        ProcessCollisions(lLastPosition - _Transform.position + lSafeMovement, ref lDeflectedMovement, ref lRemainingMovement);

                        // Only allow the safe part of this deflected movement to be applied
                        mState.MovementCounterAdjust = mState.MovementCounterAdjust + lDeflectedMovement;
                    }
                }

                // ----------------------------------------------------------------------
                // Test for max slope
                // ----------------------------------------------------------------------

                if (mState.IsGrounded && _MaxSlopeAngle > 0f)
                {
                    if (mState.GroundSurfaceAngle > _MaxSlopeAngle)
                    {
                        Vector3 lPositionForMaxSlope = _Transform.position + lActualMovement + mState.MovementGroundAdjust + mState.MovementSlideAdjust + mState.MovementCounterAdjust;

                        Vector3 lStepMovement = lPositionForMaxSlope - lLastPosition;
                        Vector3 lVerticalStepMovement = Vector3.Project(lStepMovement, lActorUp);
                        Vector3 lLateralStepMovement = lStepMovement - lVerticalStepMovement;

                        // Check if our movement is forcing us to go up. If so, we need to stop it
                        if (Vector3.Dot(lVerticalStepMovement.normalized, lActorUp) > 0f)
                        {
                            if (_ShowDebug)
                            {
                                Graphics.GraphicsManager.DrawLine(lLastPosition + (lActorUp * _MaxStepHeight), lLastPosition + (lActorUp * _MaxStepHeight) + (lLateralStepMovement.normalized * 2f), Color.magenta, null, 2f);
                            }

                            RaycastHit lHitInfo;

                            // If we hit, may not bea ble to simply step over (we DO NOT used DesiredDirection because collision changes that)
                            if (RaycastExt.SafeRaycast(lLastPosition + (lActorUp * _MaxStepHeight), lLateralStepMovement.normalized, out lHitInfo, 2f, _GroundingLayers, _Transform, mIgnoreTransforms))
                            {
                                // Let's see if the slope of this higher position is still too steep. If not,
                                // we can step onto it and stop limiting the movement.
                                float lAngle = Vector3.Angle(lHitInfo.normal, lActorUp);
                                if (lAngle > _MaxSlopeAngle)
                                {
                                    // Remove the step movement (including the vertical component)
                                    mState.MovementSlideAdjust = mState.MovementSlideAdjust - lStepMovement;

                                    // Treat the slope like a wall and find the lateral deflection
                                    Vector3 lVerticalHitNormal = Vector3.Project(lHitInfo.normal, lActorUp);
                                    Vector3 lLateralHitNormal = lHitInfo.normal - lVerticalHitNormal;

                                    // Calculate the deflected movement
                                    Vector3 lDeflectedMovement = lLateralStepMovement - Vector3.Project(lLateralStepMovement, lLateralHitNormal.normalized);

                                    // If we've gone straight into a wall, there is no deflected movement and we want to get out of the stepping.
                                    mState.IsMovementBlocked = (lDeflectedMovement.sqrMagnitude < 0.0002f);
                                    if (lDeflectedMovement.sqrMagnitude < EPSILON_SQR)
                                    {
                                        lLastPosition = _Transform.position + lActualMovement + mState.MovementGroundAdjust + mState.MovementSlideAdjust + mState.MovementCounterAdjust;
#if UNITY_EDITOR
                                        mStepPositions.Add(lLastPosition);
#endif
                                        break;
                                    }

                                    // Apply the deflected movement through the slide adjust
                                    mState.MovementSlideAdjust = mState.MovementSlideAdjust + lDeflectedMovement;

                                    // Change the direction of any remaining future steps
                                    lDesiredDirection = lDeflectedMovement.normalized;
                                }
                            }
                        }
                    }
                }

                // ----------------------------------------------------------------------
                // Step cleanup
                // ----------------------------------------------------------------------

                lLastPosition = _Transform.position + lActualMovement + mState.MovementGroundAdjust + mState.MovementSlideAdjust + mState.MovementCounterAdjust;

#if UNITY_EDITOR
                mStepPositions.Add(lLastPosition);
#endif

                // Get out if we've reached the max distance
                if (lActualDistance >= lDesiredDistance)
                {
                    break;
                }

            } // end step while

            // Find the delta from where we expected to end up to where we actually ended
            Vector3 lDesiredPosition = _Transform.position + mState.Movement + mState.MovementPlatformAdjust + mState.MovementForceAdjust + mState.MovementGroundAdjust + mState.MovementSlideAdjust + mState.MovementCounterAdjust;
            mState.MovementCounterAdjust = mState.MovementCounterAdjust + (lLastPosition - lDesiredPosition);

            // ----------------------------------------------------------------------
            // Tilting
            // ----------------------------------------------------------------------

            bool lAllowPushBack = _AllowPushback;
            bool lStopOnRotationCollision = _StopOnRotationCollision;
            bool lUseOrientation = _OrientToGround || mPrevState.IsTilting || mState.IsTilting;

            // Orient if we can
            if (lUseOrientation)
            {
                mOrientToGroundNormalTarget = Vector3.up;

                // Determine the target ground normal we want to get to
                if (_OrientToGround)
                {
                    // If we're told to go do a specific normal, do it
                    if (mTargetGroundNormal.sqrMagnitude > 0f)
                    {
                        mOrientToGroundNormalTarget = mTargetGroundNormal;
                    }
                    // If we're "keeping" orientation or NOT jumping, stay with the surface direction
                    else if (mState.IsGrounded || _KeepOrientationInAir || (mAccumulatedForceVelocity.sqrMagnitude == 0f && mState.GroundSurfaceDistance < _OrientToGroundDistance))
                    {
                        mOrientToGroundNormalTarget = mState.GroundSurfaceDirectNormal;
                    }
                }

                // Determine our tilt and the angle to the goal
                Vector3 lTiltUp = (mTilt * mYaw).Up();
                float lTiltAngle = Vector3.Angle(lTiltUp, mOrientToGroundNormalTarget);

                // If we're finished tilting, we may need to pretend we're not
                if (lTiltAngle == 0f)
                {
                    mOrientToSpeed = 0f;

                    // If we're normalized, ensure we reset the properties
                    if (!_OrientToGround && lTiltUp == Vector3.up && mState.IsGrounded)
                    {
                        mState.IsTilting = false;

                        // With tilting done, we need to reset our rotations
                        lUseOrientation = false;
                        mYaw = _Transform.rotation;
                        mTilt = Quaternion.identity;
                    }
                    // If we haven't hit the ground, we may be falling through objects that
                    // we rotated into. If that's the case, pretend we're tilting until
                    // we are no longer grounded.
                    else if (mPrevState.IsTilting && (!_OrientToGround || !mState.IsGrounded))
                    {
                        mState.IsTilting = true;

                        lAllowPushBack = true;
                        lStopOnRotationCollision = true;
                    }
                }
                // At this point, we really do need to tilt
                else
                {
                    bool lIgnoreTilt = false;

                    // Grab the angle. If we're dealing with a drastic angle change, then
                    // we want to make sure we're not dealing with a "blip".
                    float lGroundSurfaceAngle = mState.GroundSurfaceAngle;
                    if (!mPrevState.IsTilting && lTiltAngle > 45f)
                    {
                        int lStateCount = Mathf.Min(_StateCount, 20);
                        for (int i = 0; i < lStateCount; i++)
                        {
                            int lStateIndex = (mStateIndex + i < _StateCount ? mStateIndex + i : mStateIndex + i - _StateCount);
                            if (mStates[lStateIndex] != null) { lGroundSurfaceAngle = lGroundSurfaceAngle + mStates[lStateIndex].GroundSurfaceAngle; }
                        }

                        lGroundSurfaceAngle = lGroundSurfaceAngle / (float)lStateCount;
                        if (mState.GroundSurfaceAngle - lGroundSurfaceAngle > 30f)
                        {
                            lIgnoreTilt = true;
                        }
                    }

                    // If we're not ignoring the angle change, we need to see if we should move there smoothly.
                    if (!lIgnoreTilt)
                    {
                        if (!mPrevState.IsTilting)
                        {
                            // The angle is so small, immediately move to it
                            if (_OrientToGroundSpeed == 0f || _MinOrientToGroundAngleForSpeed == 0f || lTiltAngle < _MinOrientToGroundAngleForSpeed)
                            {
                                mOrientToGroundNormal = mOrientToGroundNormalTarget;
                            }
                            // Determine the speed that we'll use to get to the new angle. We want
                            // to go faster for smaller angles.
                            else
                            {
                                mState.IsTilting = true;

                                lAllowPushBack = true;
                                lStopOnRotationCollision = true;

                                //mOrientToSpeed = lTiltAngle / _OrientToGroundSpeed * rDeltaTime;
                                float lFactor = Mathf.Max((lTiltAngle - _MinOrientToGroundAngleForSpeed) / (180f - _MinOrientToGroundAngleForSpeed), 0.1f);
                                mOrientToSpeed = ((lTiltAngle / _OrientToGroundSpeed) / lFactor);

                                mOrientToGroundNormal = Vector3.RotateTowards(mOrientToGroundNormal, mOrientToGroundNormalTarget, mOrientToSpeed * rDeltaTime * Mathf.Deg2Rad, 0f);
                            }
                        }
                        // If we're really close to the angle, go there
                        else if (lTiltAngle < 0.1f)
                        {
                            mOrientToGroundNormal = mOrientToGroundNormalTarget;
                        }
                        // Continue tilting
                        else
                        {
                            mState.IsTilting = true;

                            lAllowPushBack = true;
                            lStopOnRotationCollision = true;

                            float lFactor = Mathf.Max((lTiltAngle - _MinOrientToGroundAngleForSpeed) / (180f - _MinOrientToGroundAngleForSpeed), 0.1f);
                            mOrientToSpeed = Mathf.Max(((lTiltAngle / _OrientToGroundSpeed) / lFactor), mOrientToSpeed);

                            mOrientToGroundNormal = Vector3.RotateTowards(mOrientToGroundNormal, mOrientToGroundNormalTarget, mOrientToSpeed * rDeltaTime * Mathf.Deg2Rad, 0f);
                        }
                    }

                    // Determine the final tilt
                    mTilt = QuaternionExt.FromToRotation(lTiltUp, mOrientToGroundNormal) * mTilt;
                    if (QuaternionExt.IsEqual(mTilt, Quaternion.identity))
                    {
                        mTilt = Quaternion.identity;
                        mState.IsTilting = false;
                    }
                }
            }
            // If we're not allowed to orient to the ground, we're going to make sure
            // the character is always oreinted up. 
            else if (!_OrientToGround)
            {
                // We shouldn't have to do this, but in the case, where a wierd collision
                // forces us over, we'll upright ourselves.
                float lTiltAngle = Vector3.Angle(lActorUp, Vector3.up);

                // TRT 2/25/2016
                if (lTiltAngle > 0f && mTilt.eulerAngles.sqrMagnitude < 0.001f)
                {
                    Quaternion lUpFix = QuaternionExt.FromToRotation(_Transform.up, Vector3.up);
                    mYaw = lUpFix * mYaw;

                    mTilt = Quaternion.identity;
                }
            }

            // ----------------------------------------------------------------------
            // Finalize the movement and rotation
            // ----------------------------------------------------------------------

            if (mState.Movement.sqrMagnitude > 0f)
            {
                //int x = 0;
            }

            // Get the distance to the ground (even if we penetrate)
            lGroundDistance = GetGroundDistance(lLastPosition, lActorUp);

            // Determine the actual movement change
            lFinalMovement = lLastPosition - _Transform.position;

            // Determine the actual rotation
            lFinalRotate = _Transform.rotation.RotationTo(_InvertRotationOrder ? mYaw * mTilt : mTilt * mYaw);

            // ----------------------------------------------------------------------
            // Handle pushback
            // ----------------------------------------------------------------------

            // If we're on a steep angle for some reason, we may need to push back to ensure we don't penetrate
            lAllowPushBack = lAllowPushBack || (_MaxSlopeAngle > 0f && mState.GroundSurfaceAngle >= _MaxSlopeAngle);

            // If we allow push back, do an extra collision check to see if we've penetrated
            if (_IsCollisionEnabled && (lAllowPushBack || lStopOnRotationCollision))
            {
                for (int i = 0; i < BodyShapes.Count; i++)
                {
                    BodyShape lBodyShape = BodyShapes[i];
                    if ((lBodyShape.IsEnabledOnGround && mState.IsGrounded) ||
                        (lBodyShape.IsEnabledAboveGround && !mState.IsGrounded))
                    {
                        // If there are collisions, we need to respond
                        List<BodyShapeHit> lBodyShapeHits = lBodyShape.CollisionOverlap(lFinalMovement, lFinalRotate, _CollisionLayers);
                        if (lBodyShapeHits != null && lBodyShapeHits.Count > 0)
                        {
                            // Push us away from the collision
                            if (lAllowPushBack)
                            {
                                Vector3 lDirection = (lBodyShapeHits[0].HitOrigin - lBodyShapeHits[0].HitPoint).normalized;
                                lFinalMovement = lFinalMovement + (lDirection * -lBodyShapeHits[0].HitDistance);
                            }

                            // Clear any rotation that occurred
                            if (lStopOnRotationCollision)
                            {
                                mYaw = mPrevState.RotationYaw;
                                mTilt = mPrevState.RotationTilt;
                                lFinalRotate = Quaternion.identity;

                                mOrientToGroundNormal = (mTilt * mYaw).Up();
                            }

                            // No need to continue
                            break;
                        }
                    }
                }
            }

            // ----------------------------------------------------------------------
            // Apply smooth stepping
            // ----------------------------------------------------------------------

            mState.IsSteppingUp = false;
            mState.IsSteppingDown = false;

            // Check if we can smooth step
            bool lAllowSmoothStep = true;
            if (lAllowSmoothStep && !_FixGroundPenetration) { lAllowSmoothStep = false; }
            if (lAllowSmoothStep && !mState.IsGrounded) { lAllowSmoothStep = false; }
            if (lAllowSmoothStep && mState.GroundSurfaceAngle > EPSILON) { lAllowSmoothStep = false; }
            if (lAllowSmoothStep && mPrevState.GroundSurfaceAngle > EPSILON) { lAllowSmoothStep = false; }
            if (lAllowSmoothStep && mState.MovementSlideAdjust.sqrMagnitude > EPSILON_SQR) { lAllowSmoothStep = false; }
            if (lAllowSmoothStep && mState.MovementPlatformAdjust.sqrMagnitude > EPSILON_SQR) { lAllowSmoothStep = false; }

            if (lAllowSmoothStep)
            {
                Vector3 lFinalVerticalMovement = Vector3.Project(lFinalMovement, lActorUp);
                Vector3 lFinalLateralMovement = lFinalMovement - lFinalVerticalMovement;

                float lFinalVerticalMovementDot = Vector3.Dot(lFinalVerticalMovement, lActorUp);

                // Penetration has occurred and we need to raise
                if (lFinalVerticalMovementDot > EPSILON)
                {
                    if (_StepUpSpeed > 0f)
                    {
                        mState.IsSteppingUp = true;

                        lFinalVerticalMovement = Vector3.MoveTowards(Vector3.zero, lFinalVerticalMovement, Mathf.Max(lFinalLateralMovement.magnitude, Time.deltaTime * 1f) * _StepUpSpeed);
                        lFinalMovement = lFinalLateralMovement + lFinalVerticalMovement;
                    }
                }
                // Gap occurs and we need to lower
                else if (lFinalVerticalMovementDot < -EPSILON)
                {
                    if (_StepDownSpeed > 0f)
                    {
                        bool lStepDown = true;

                        // TRT 06/21/2017 - Added to keep step down from kicking in when falling
                        Vector3 lVerticalAccumulatedVelocity = Vector3.Project(mAccumulatedVelocity, lActorUp);
                        if (Vector3.Dot(lVerticalAccumulatedVelocity.normalized, lActorUp) < 0f)
                        {
                            Vector3 lFrameGravity = (_Gravity.sqrMagnitude == 0f ? UnityEngine.Physics.gravity : _Gravity) * rDeltaTime;
                            if (lVerticalAccumulatedVelocity.magnitude > lFrameGravity.magnitude * 2f) { lStepDown = false; }
                        }

                        if (lStepDown)
                        {
                            mState.IsSteppingDown = true;

                            lFinalVerticalMovement = Vector3.MoveTowards(Vector3.zero, lFinalVerticalMovement, Mathf.Max(lFinalLateralMovement.magnitude, Time.deltaTime * 1f) * _StepDownSpeed);
                            lFinalMovement = lFinalLateralMovement + lFinalVerticalMovement;
                        }
                    }
                }
            }
            // Since we're not smooth stepping, ensure we don't sink
            else if (_FixGroundPenetration && lGroundDistance < 0f)
            {
                mState.IsGrounded = true;

                // If we're on a max slope, we won't rise or we'll eventually go up the mountain
                if (_MaxSlopeAngle > 0f && mState.GroundSurfaceAngle < _MaxSlopeAngle)
                {
                    lFinalMovement = lFinalMovement + (lActorUp * -lGroundDistance);
                }
            }

            // ----------------------------------------------------------------------
            // Apply the limits
            // ----------------------------------------------------------------------

            // Apply rotation limits
            if (_FreezeRotationX || _FreezeRotationY || _FreezeRotationZ)
            {
                Vector3 lEuler = lFinalRotate.eulerAngles;

                if (_FreezeRotationX) { lEuler.x = 0f; }
                if (_FreezeRotationY) { lEuler.y = 0f; }
                if (_FreezeRotationZ) { lEuler.z = 0f; }
                lFinalRotate.eulerAngles = lEuler;
            }

            // Apply movmeent limits
            if (lFinalMovement.sqrMagnitude > 0f)
            {
                if (_FreezePositionX) { lFinalMovement.x = 0f; }
                if (_FreezePositionY) { lFinalMovement.y = 0f; }
                if (_FreezePositionZ) { lFinalMovement.z = 0f; }
            }

            // ----------------------------------------------------------------------
            // Apply the results
            // ----------------------------------------------------------------------

            Quaternion lFinalRotation = _Transform.rotation * lFinalRotate;
            Vector3 lFinalPosition = _Transform.position + lFinalMovement;

            // Allow hooks to modify the final position
            if (mOnPreControllerMove != null)
            {
                // Set info here so it can be used by the callback
                mState.RotationYaw = mYaw;
                mState.RotationTilt = mTilt;
                mState.Rotation = lFinalRotation;
                mState.Position = lFinalPosition;

                // Determine the per second velocity
                mState.Velocity = (lFinalPosition - mPrevState.Position) / Time.deltaTime; // (Time.smoothDeltaTime > 0f ? Time.smoothDeltaTime : Time.deltaTime);

                mOnPreControllerMove(this, ref lFinalPosition, ref lFinalRotation);
            }

            if (_ShowDebug)
            {
                Utilities.Debug.Log.FileScreenWrite(string.Format("isG:{0} gDist:{1:f3} gAng:{2:f3} gFwdAng:{3:f3} spd:{4:f3}", mState.IsGrounded, lGroundDistance, mState.GroundSurfaceAngle, mState.GroundSurfaceForwardAngle, (lFinalMovement.magnitude / Time.deltaTime)), 1);
                Utilities.Debug.Log.FileScreenWrite(string.Format("isUp:{0} isDn:{1} isUser:{2} isBlk:{3} isTlt:{4}", mState.IsSteppingUp, mState.IsSteppingDown, mState.IsMoveRequested, mState.IsMovementBlocked, mState.IsTilting), 2);
                //Utilities.Debug.Log.FileScreenWrite("p-isG:" + mPrevState.IsGrounded + " p-dist:" + mPrevState.GroundSurfaceDirectDistance.ToString("f8"), 1);
                //Utilities.Debug.Log.FileScreenWrite("mvm:" + mState.Movement.magnitude.ToString("f3") + " des-dist:" + lDesiredDistance.ToString("f3") + " act-dist:" + lActualDistance.ToString("f3") + (mState.Movement.sqrMagnitude > 0f ? " ************************" : ""), 2);
                //Utilities.Debug.Log.FileScreenWrite("vel:" + (mState.Movement.magnitude / rDeltaTime).ToString("f3") + " act-vel:" + (lActualDistance / rDeltaTime).ToString("f3"), 3);
                Utilities.Debug.Log.FileWrite(".");
            }

            _Transform.rotation = lFinalRotation;
            _Transform.position = lFinalPosition;

            // ----------------------------------------------------------------------
            // Record and clean up
            // ----------------------------------------------------------------------

            mState.RotationYaw = mYaw;
            mState.RotationTilt = mTilt;
            mState.Rotation = lFinalRotation;
            mState.Position = lFinalPosition;
            mState.Velocity = (lFinalPosition - mPrevState.Position) / Time.deltaTime; // (Time.smoothDeltaTime > 0f ? Time.smoothDeltaTime : Time.deltaTime);

            // Override the ground if it's forced
            if (mTargetGround != null)
            {
                mState.IsGrounded = true;
                mState.Ground = mTargetGround;
            }

            // Set the ground information
            if (mState.Ground != null)
            {
                mState.GroundPosition = mState.Ground.position;
                mState.GroundRotation = mState.Ground.rotation;
            }

            // Clear out any vertical accumulated velocity
            if (mState.IsGrounded)
            {
                Vector3 lVerticalAccumulatedVelocity = Vector3.Project(mAccumulatedVelocity, lActorUp);
                Vector3 lLateralAccumulatedVelocity = mAccumulatedVelocity - lVerticalAccumulatedVelocity;

                mAccumulatedVelocity = Vector3.zero + lLateralAccumulatedVelocity;
            }

            // Clear out the ignore flag if needed
            if (mIgnoreUseTransform && mAppliedForces.Count == 0 && mAccumulatedVelocity.sqrMagnitude < EPSILON_SQR)
            {
                mIgnoreUseTransform = false;
            }

            // Debug the processing
#if UNITY_EDITOR
            if (_ShowDebug)
            {
                Graphics.GraphicsManager.DrawLines(mStepPositions, Color.gray, null, 5f);

                for (int i = 0; i < mStepPositions.Count; i++)
                {
                    Graphics.GraphicsManager.DrawPoint(mStepPositions[i], Color.green, null, 5f);
                }

                Graphics.GraphicsManager.DrawPoint(mStepPositions[mStepPositions.Count - 1], Color.magenta, null, 5f);

                // Grounding radius
                Graphics.GraphicsManager.DrawCircle(_Transform.position, _BaseRadius, Color.blue, _Transform.up);
            }
#endif
        }

        /// <summary>
        /// Ground we want the actor "virtually" parented to
        /// </summary>
        /// <param name="rGround"></param>
        public void SetGround(Transform rGround)
        {
            mTargetGround = rGround;

            if (mTargetGround == null)
            {
                mAccumulatedVelocity = Vector3.zero;
                mAccumulatedMovement = Vector3.zero;
            }
        }

        /// <summary>
        /// Sets and absolute tilt to orient the actor to
        /// </summary>
        /// <param name="rPosition"></param>
        public void SetTargetGroundNormal(Vector3 rTargetGroundNormal)
        {
            if (_OrientToGround)
            {
                mTargetGroundNormal = rTargetGroundNormal;
            }
        }

        /// <summary>
        /// Sets the absolut yaw of the actor, keeping the current tilt.
        /// </summary>
        /// <param name="rYaw">Absolution rotation absolute the actor's y-axis</param>
        public void SetYaw(Quaternion rYaw)
        {
            mTargetRotation = rYaw;
            mTargetTilt = mTilt;
        }

        /// <summary>
        /// Sets and absolute rotation of the actor. We remove the current tilt from the
        /// parameter and use the remaining rotation as the yaw.
        /// </summary>
        /// <param name="rRotation">Absolute rotation</param>
        public void SetRotation(Quaternion rRotation)
        {
            mTargetRotation = Quaternion.Inverse(mTilt) * rRotation;
            mTargetTilt = mTilt;
        }

        /// <summary>
        /// Sets an absolute rotation of the actor using the yaw and tilt values.
        /// </summary>
        /// <param name="rRotation"></param>
        public void SetRotation(Quaternion rYaw, Quaternion rTilt)
        {
            mTargetRotation = rYaw;
            mTargetTilt = rTilt;
        }

        /// <summary>
        /// Rotates (yaw) this frame. This rotation
        /// is relative to the actor's current rotation
        /// </summary>
        /// <param name="rYaw">Rotation around the character's y-axis</param>
        public void Rotate(Quaternion rYaw)
        {
            mTargetRotate = mTargetRotate * rYaw;
            mTargetTilt = Quaternion.identity;
        }

        /// <summary>
        /// Sets an absolution "yaw" rotation and "pitch" rotation for the frame. This rotation
        /// is relative to the actor's current rotation
        /// </summary>
        /// <param name="rYaw">Rotation around the character's y-axis</param>
        /// <param name="rTilt">Rotation around the character's x-axis</param>
        public void Rotate(Quaternion rYaw, Quaternion rTilt)
        {
            mTargetRotate = mTargetRotate * rYaw;
            mTargetTilt = mTargetTilt * rTilt;
        }

        /// <summary>
        /// Sets a velocity that will cause rotation over time.
        /// </summary>
        /// <param name="rVelocity"></param>
        public void SetRotationVelocity(Vector3 rVelocity)
        {
            mTargetRotationVelocity = rVelocity;
        }

        /// <summary>
        /// Sets and absolute position to move the actor to
        /// </summary>
        /// <param name="rPosition"></param>
        public void SetPosition(Vector3 rPosition)
        {
            mTargetPosition = rPosition;
        }

        /// <summary>
        /// Sets an absolute movement for the frame. This is in addition
        /// to any velocity that is set.
        /// </summary>
        /// <param name="rMovement"></param>
        public void Move(Vector3 rMovement)
        {
            mTargetMove = mTargetMove + rMovement;
        }

        /// <summary>
        /// Sets an absolute movement for the frame relative to how the actor is facing. This is in addition
        /// to any velocity that is set.
        /// </summary>
        /// <param name="rMovement"></param>
        public void RelativeMove(Vector3 rMovement)
        {
            mTargetMove = mTargetMove + (_Transform.rotation * rMovement);
        }

        /// <summary>
        /// Sets a velocity that will cause movement over time.
        /// </summary>
        /// <param name="rVelocity"></param>
        public void SetVelocity(Vector3 rVelocity)
        {
            mTargetVelocity = rVelocity;
        }

        /// <summary>
        /// Sets a velocity that will cause movement over time.
        /// </summary>
        /// <param name="rVelocity"></param>
        public void SetRelativeVelocity(Vector3 rVelocity)
        {
            mTargetVelocity = _Transform.rotation * rVelocity;
        }

        /// <summary>
        /// Grabs the closest point on the actor's body shapes to the origin
        /// </summary>
        /// <param name="rOrigin">Position we're testing from</param>
        /// <returns>Position on the body shape surfaces that is the closest point or Vector3.zero if no point is found</returns>
        public Vector3 ClosestPoint(Vector3 rOrigin)
        {
            Vector3 lClosestPoint = Vector3Ext.Null;
            float lClosestDistance = float.MaxValue;

            for (int i = 0; i < BodyShapes.Count; i++)
            {
                Vector3 lPoint = BodyShapes[i].ClosestPoint(rOrigin);
                if (lPoint != Vector3Ext.Null)
                {
                    float lDistance = (lPoint - rOrigin).sqrMagnitude;
                    if (lDistance < lClosestDistance)
                    {
                        lClosestPoint = lPoint;
                        lClosestDistance = lDistance;
                    }
                }
            }

            return lClosestPoint;
        }

        /// <summary>
        /// Determines if we're meant to ignore the specified collider
        /// </summary>
        /// <param name="rCollider">Collider to test</param>
        /// <returns>Tells if we are meant to ignore the collision</returns>
        public bool IsIgnoringCollision(Collider rCollider)
        {
            if (mIgnoreCollisions == null) { return false; }
            return mIgnoreCollisions.Contains(rCollider);
        }

        /// <summary>
        /// Clears any colliders that were meant to be ignored
        /// </summary>
        public void ClearIgnoreCollisions()
        {
            if (mIgnoreCollisions != null)
            {
                for (int i = 0; i < mIgnoreCollisions.Count; i++)
                {
                    for (int j = 0; j < BodyShapes.Count; j++)
                    {
                        if (BodyShapes[j].Colliders != null)
                        {
                            for (int k = 0; k < BodyShapes[j].Colliders.Length; k++)
                            {
                                UnityEngine.Physics.IgnoreCollision(BodyShapes[j].Colliders[k], mIgnoreCollisions[i], false);
                            }
                        }
                    }
                }

                mIgnoreCollisions.Clear();
                mIgnoreTransforms.Clear();
            }
        }

        /// <summary>
        /// Sets a collider to be ignored or not by the character controller
        /// </summary>
        /// <param name="rCollider">Collider to ignore or not</param>
        /// <param name="rIgnore">Flag to determine if we are ignoring</param>
        public void IgnoreCollision(Collider rCollider, bool rIgnore = true)
        {
            // Get out if there is no work to do
            if (rIgnore && IsIgnoringCollision(rCollider)) { return; }
            if (!rIgnore && !IsIgnoringCollision(rCollider)) { return; }

            // First, ensure any unity colliders are disabled
            for (int i = 0; i < BodyShapes.Count; i++)
            {
                if (BodyShapes[i].Colliders != null)
                {
                    for (int j = 0; j < BodyShapes[i].Colliders.Length; j++)
                    {
                        UnityEngine.Physics.IgnoreCollision(BodyShapes[i].Colliders[j], rCollider, rIgnore);
                    }
                }
            }

            // Add the collider to our list
            if (rIgnore)
            {
                if (mIgnoreCollisions == null) { mIgnoreCollisions = new List<Collider>(); }
                if (!mIgnoreCollisions.Contains(rCollider)) { mIgnoreCollisions.Add(rCollider); }

                if (mIgnoreTransforms == null) { mIgnoreTransforms = new List<Transform>(); }
                if (!mIgnoreTransforms.Contains(rCollider.transform)) { mIgnoreTransforms.Add(rCollider.transform); }
            }
            // Remove the collider from our list
            else
            {
                if (mIgnoreCollisions != null) { mIgnoreCollisions.Remove(rCollider); }
                if (mIgnoreTransforms != null) { mIgnoreTransforms.Remove(rCollider.transform); }
            }
        }

        /// <summary>
        /// Simple test to get the ground information that is directly under the actor. This is meant to be fast.
        /// </summary>
        /// <param name="rPosition">Position to test</param>
        /// <param name="rActorUp">Actor's up vector</param>
        /// <param name="rWorldUp">World's up vector</param>
        /// <param name="rGroundHitInfo"></param>
        /// <returns>Boolean that determines if a hit took place</returns>
        protected bool TestGrounding(Vector3 rPosition, Vector3 rActorUp, Vector3 rWorldUp, out RaycastHit rGroundHitInfo)
        {
            Vector3 lRayStart = rPosition + (rActorUp * _GroundingStartOffset);
            Vector3 lRayDirection = -rActorUp;
            float lRayDistance = _GroundingStartOffset + _GroundingDistance;

            // Start with a simple ray. This would be the object directly under the actor
            bool lGroundHit = false;
            if (_IsGroundingLayersEnabled)
            {
                lGroundHit = RaycastExt.SafeRaycast(lRayStart, lRayDirection, out rGroundHitInfo, lRayDistance, _GroundingLayers, _Transform, mIgnoreTransforms);
            }
            else
            {
                lGroundHit = RaycastExt.SafeRaycast(lRayStart, lRayDirection, out rGroundHitInfo, lRayDistance, -1, _Transform, mIgnoreTransforms);
            }

            if (lGroundHit)
            {
                rGroundHitInfo.distance = rGroundHitInfo.distance - _GroundingStartOffset;
            }

            return lGroundHit;
        }

        /// <summary>
        /// Determines if we're able to continue moving based on the angle of the slope. It's a little more complex
        /// as we need to "step" forward to try to hit a bad slope. Then, we squeeze to find the exact point where the
        /// slope becomes invalid.
        /// </summary>
        /// <param name="rPosition">Position of the actor</param>
        /// <param name="rActorUp">Actor's up vector</param>
        /// <param name="rMovement">Movement we are trying to achieve</param>
        /// <param name="rCurrentGroundSurfaceAngle">Current angle the character starts on</param>
        /// <param name="rSafeMovement">Amount of movement that can occur before we get to the angle</param>
        /// <param name="rGroundNormal">Bad slope that we stop at</param>
        /// <returns>Determines if a slope change was hit</returns>
        protected bool TestSlope(Vector3 rPosition, Vector3 rActorUp, Vector3 rMovement, Vector3 rPlatformMovement, float rCurrentGroundSurfaceAngle, ref Vector3 rSafeMovement, ref Vector3 rGroundNormal)
        {
            if (rMovement.sqrMagnitude == 0f)
            {
                return false;
            }

            //if (rMovement == rPlatformMovement)
            //{
            //    if (_OrientToGround && _IsGravityRelative)
            //    {
            //        return false;
            //    }
            //}

            Vector3 lUserMovement = rMovement - rPlatformMovement;

            Vector3 lMovementDirection = lUserMovement.normalized;
            float lMovementDistance = lUserMovement.magnitude;

            // First, shoot a ray forward to determine if we end up hitting a slope at all
            RaycastHit lHitInfo;

            bool lGroundHit = false;
            if (_IsGroundingLayersEnabled)
            {
                lGroundHit = RaycastExt.SafeRaycast(rPosition + (rActorUp * EPSILON), lMovementDirection, out lHitInfo, lMovementDistance + _SkinWidth, _GroundingLayers, _Transform, mIgnoreTransforms);
            }
            else
            {
                lGroundHit = RaycastExt.SafeRaycast(rPosition + (rActorUp * EPSILON), lMovementDirection, out lHitInfo, lMovementDistance + _SkinWidth, -1, _Transform, mIgnoreTransforms);
            }

            if (!lGroundHit)
            {
                return false;
            }

            // TRT 10/25: We shouldn't need this any more. Jumping while moving caused this condition to
            //             occur and we'd sink in when we shouldn't.
            //
            // We hit a slope at "root + epsilon". However, it may just be a small bump we can step over. So, we'll
            // do another check to see if we also hit something above step height.
            //RaycastHit lStepHitInfo;
            //if (!RaycastExt.SafeRaycast(rPosition + (rActorUp * (_MaxStepHeight + EPSILON)), lMovementDirection, lMovementDistance + _OverlapRadius, _Transform, out lStepHitInfo))
            //{
            //    return false;
            //}

            // Test if there's anything further above the hit point. If not, we may be able to step over it
            Vector3 lLocalHitPoint = _Transform.InverseTransformPoint(lHitInfo.point);
            if (lLocalHitPoint.y < _MaxStepHeight)
            {
                RaycastHit lStepHitInfo;

                if (_IsGroundingLayersEnabled)
                {
                    lGroundHit = RaycastExt.SafeRaycast(rPosition + (rActorUp * _MaxStepHeight), lMovementDirection, out lStepHitInfo, _MinStepDepth * 2f, _GroundingLayers, _Transform, mIgnoreTransforms);
                }
                else
                {
                    lGroundHit = RaycastExt.SafeRaycast(rPosition + (rActorUp * _MaxStepHeight), lMovementDirection, out lStepHitInfo, _MinStepDepth * 2f, -1, _Transform, mIgnoreTransforms);
                }

                if (!lGroundHit)
                {
                    return false;
                }

                float lStepDepth = (_MinStepDepth > 0f ? _MinStepDepth : Radius);
                if (lStepHitInfo.distance >= lStepDepth)
                {
                    Vector3 lStepNormalProj = Vector3.Project(lStepHitInfo.normal, rActorUp);
                    if (lStepNormalProj.sqrMagnitude == 0) { return false; }

                    float lStepNormalDot = Vector3.Dot(lStepNormalProj.normalized, rActorUp);
                    if (lStepNormalDot < 0.1f) { return false; }
                }
            }

            // If we're not dealing with a slope (ie an actor-up facing normal), we can stop looking
            Vector3 lHitNormalProj = Vector3.Project(lHitInfo.normal, rActorUp);
            if (Vector3.Dot(lHitNormalProj.normalized, rActorUp) == 0f)
            {
                return false;
            }

            //com.ootii.Graphics.GraphicsManager.DrawPoint(_Transform.position + rMovement, Color.blue, null, 10f);
            //com.ootii.Graphics.GraphicsManager.DrawPoint(_Transform.position, Color.black, null, 10f);
            //com.ootii.Graphics.GraphicsManager.DrawPoint(lHitInfo.point, Color.red, null, 10f);
            //com.ootii.Graphics.GraphicsManager.DrawLine(_Transform.position, _Transform.position + rGroundNormal, Color.black, null, 10f);
            //com.ootii.Graphics.GraphicsManager.DrawLine(lHitInfo.point, _Transform.position + lHitInfo.normal, Color.red, null, 10f);
            //com.ootii.Graphics.GraphicsManager.DrawLine(_Transform.position, _Transform.position + rMovement, Color.blue, null, 10f);

            //com.ootii.Graphics.GraphicsManager.DrawPoint(rPosition + (rActorUp * _MaxStepHeight), Color.magenta, null, 10f);
            //com.ootii.Graphics.GraphicsManager.DrawLine(rPosition + (rActorUp * _MaxStepHeight), rPosition + (rActorUp * _MaxStepHeight) + lMovementDirection, Color.magenta, null, 10f);

            float lHitAngle = Vector3.Angle(lHitInfo.normal, rActorUp);
            rGroundNormal = lHitInfo.normal;

            // Phase #2 - Squeeze the ends to find the exact point the slope starts
            // We have to process the raycast at least once
            bool lSlopeHit = false;

            Vector3 lLastGood = rPosition;

            Vector3 lStart = rPosition;
            Vector3 lEnd = rPosition + (lMovementDirection * lHitInfo.distance);
            Vector3 lMid = lStart + ((lEnd - lStart) / 2f);

            float lDistanceSqr = (lEnd - lStart).sqrMagnitude;

            float lMinDistanceSqr = _SlopeMovementStep * _SlopeMovementStep;
            if (lMinDistanceSqr > lDistanceSqr) { lMinDistanceSqr = lDistanceSqr; }

            int lCounter = 0;
            while (lCounter < 20 && lDistanceSqr > EPSILON_SQR && lDistanceSqr >= lMinDistanceSqr)
            {
                lCounter++;
                lSlopeHit = false;

                if (_IsGroundingLayersEnabled)
                {
                    lGroundHit = RaycastExt.SafeRaycast(lMid + (rActorUp * _MaxStepHeight), -rActorUp, out lHitInfo, _MaxStepHeight + 0.05f, _GroundingLayers, _Transform, mIgnoreTransforms);
                }
                else
                {
                    lGroundHit = RaycastExt.SafeRaycast(lMid + (rActorUp * _MaxStepHeight), -rActorUp, out lHitInfo, _MaxStepHeight + 0.05f, -1, _Transform, mIgnoreTransforms);
                }

                if (lGroundHit)
                {
                    float lGroundSurfaceAngle = Vector3.Angle(lHitInfo.normal, rActorUp);
                    if (lGroundSurfaceAngle == lHitAngle)
                    {
                        lSlopeHit = true;
                    }
                }

                // Close the gap
                if (lSlopeHit)
                {
                    lEnd = lMid;
                }
                else
                {
                    lStart = lMid;
                    lLastGood = lMid;
                }

                lDistanceSqr = (lEnd - lStart).sqrMagnitude;

                // Determine the new mid
                lMid = lStart + ((lEnd - lStart) / 2f);
            }

            rSafeMovement = lLastGood - rPosition;

            // Return that we had an invalid movement
            return true;
        }

        /// <summary>
        /// Inner function responsible for each movement step in a single frame. This allows
        /// us to compartmentalize our logic a bit.
        /// </summary>
        /// <param name="rSegmentIndex">Index of the segment for this frame</param>
        /// <param name="rSegmentPositionDelta">Starting position change we need to factor in</param>
        /// <param name="rSegmentMovement">Amount of movement we want to do</param>
        /// <param name="rOrientToGround">Determines if we should orient to the ground</param>
        /// <returns></returns>
        //protected bool ProcessMovement(int rSegmentIndex, Vector3 rSegmentPositionDelta, bool rOrientToGround, ref Vector3 rSegmentMovement, ref Vector3 rRemainingMovement)
        //{
        //    bool lIsSlopePushingDown = false;
        //    bool lIsSlopePushingUp = false;

        //    Vector3 lActorUp = (mTilt * mYaw).Up();

        //    // In order to test an acurate ground, we need to test the ground including
        //    // the platform movement. Otherwise, the platform will get ahead of us and we won't
        //    // be testing accurately. However, we don't want to overcompensate as we 
        //    // step closer to the actual endpoint (which includes the platform movement). So, we 
        //    // remove old movement as if it came from the platform.
        //    //bool lPlatformMovementExists = false;
        //    //Vector3 lPostPlatformMovement = Vector3.zero;

        //    Vector3 lPlatformMovement = mState.MovementPlatformAdjust;
        //    if (lPlatformMovement.sqrMagnitude > 0f)
        //    {
        //        if (rSegmentPositionDelta.sqrMagnitude < lPlatformMovement.sqrMagnitude)
        //        {
        //            //lPlatformMovementExists = true;
        //            lPlatformMovement = lPlatformMovement - rSegmentPositionDelta;
        //        }
        //        else
        //        {
        //            lPlatformMovement = Vector3.zero;
        //        }
        //    }

        //    // ----------------------------------------------------------------------
        //    // Get the new ground information for the segment (again, assume the
        //    // platform movement is valid so we get the right ground info)
        //    // ----------------------------------------------------------------------
        //    RaycastHit lGroundHitInfo;
        //    bool lIsGrounded = ProcessGrounding(_Transform.position + lPlatformMovement, rSegmentPositionDelta, lActorUp, mWorldUp, _BaseRadius, out lGroundHitInfo);

        //    //Log.FileWrite("");
        //    //Log.FileWrite("seg:" + rSegmentIndex + " is-g:" + mState.IsGrounded + " g:" + (mState.Ground == null ? "null" : mState.Ground.name) + " f-pos:" + StringHelper.ToString(_Transform.position + rSegmentPositionDelta) + " f+m-pos:" + StringHelper.ToString(_Transform.position + rSegmentPositionDelta + rSegmentMovement) + " d-pos:" + StringHelper.ToString(rSegmentPositionDelta) + " mvm:" + StringHelper.ToString(rSegmentMovement));
        //    //Log.FileWrite("actor-to-surface angle:" + Vector3.Angle(lActorUp, mState.GroundSurfaceNormal) + " actor-up:" + StringHelper.ToString(lActorUp) + " g-nml:" + StringHelper.ToString(mState.GroundSurfaceNormal));

        //    // If we're stepping down, we consider ourselves grounded
        //    if (!lIsGrounded && mPrevState.IsSteppingDown && mState.GroundSurfaceDirectDistance < _MaxStepHeight && mState.GroundSurfaceDistance <= mPrevState.GroundSurfaceDistance)
        //    {
        //        lIsGrounded = true;
        //        mState.IsGrounded = true;
        //    }

        //    // ----------------------------------------------------------------------
        //    // If we're penetrating into the ground, push out
        //    // ----------------------------------------------------------------------

        //    // We're testing our "current" position for the segment. So, the first time through, this
        //    // is the position we're currently at. Usually that means we'll catch a "step-up" first. However,
        //    // if we have multiple segments or if an object moves under us (like a platform), we could get a pop-up here. 
        //    if (_FixGroundPenetration)
        //    {
        //        Vector3 lVerticalPlatformMovement = Vector3.Project(mState.MovementPlatformAdjust, lActorUp);
        //        if (lIsGrounded && !mPrevState.IsSteppingUp && !mPrevState.IsSteppingDown && mState.GroundSurfaceDistance + lVerticalPlatformMovement.magnitude < 0f)
        //        {
        //            // Finally, do a speed test. We don't want to pop if we don't have to
        //            if (rSegmentMovement.sqrMagnitude > 0.01f)
        //            {
        //                rSegmentMovement = rSegmentMovement + ((mTilt * mYaw).Up() * -(mState.GroundSurfaceDistance + lVerticalPlatformMovement.magnitude));
        //            }
        //        }
        //    }

        //    // ----------------------------------------------------------------------
        //    // Determine if there is a slope change
        //    // ----------------------------------------------------------------------
        //    if (lIsGrounded && rSegmentMovement.sqrMagnitude > 0f)
        //    {
        //        Vector3 lSafeMovement = Vector3.zero;
        //        Vector3 lGroundSurfaceNormal = mState.GroundSurfaceNormal;

        //        if (TestSlope(_Transform.position + rSegmentPositionDelta, lActorUp, rSegmentMovement, lPlatformMovement, mState.GroundSurfaceAngle, ref lSafeMovement, ref lGroundSurfaceNormal))
        //        {
        //            float lHitAngle = Vector3.Angle(lGroundSurfaceNormal, lActorUp);
        //            if (lHitAngle > (_MaxSlopeAngle > 0f ? _MaxSlopeAngle - 0.5f : MAX_GROUNDING_ANGLE) && lHitAngle < 90f - EPSILON)
        //            //if (lHitAngle < 85f && (_MaxSlopeAngle > 0f && lHitAngle > _MaxSlopeAngle - 0.5f))
        //            {
        //                // Treat the normal as if it's a vertical wall.
        //                Vector3 lGroundSurfaceProj = Vector3.Project(lGroundSurfaceNormal, lActorUp);
        //                lGroundSurfaceNormal = (lGroundSurfaceNormal - lGroundSurfaceProj).normalized;

        //                // Push back from slope point that we are ending at
        //                lSafeMovement = lSafeMovement + (lGroundSurfaceNormal * _SkinWidth);

        //                // Deflect the remaining movement off this "wall"
        //                rRemainingMovement = rSegmentMovement - lSafeMovement;
        //                rRemainingMovement = rRemainingMovement - Vector3.Project(rRemainingMovement, lGroundSurfaceNormal);
        //            }
        //            else
        //            {
        //                //Log.FileWrite("PM() slope hit, not max. nml:" + StringHelper.ToString(lGroundSurfaceNormal));
        //                rRemainingMovement = rSegmentMovement - lSafeMovement;
        //                rRemainingMovement = rRemainingMovement - Vector3.Project(rRemainingMovement, lGroundSurfaceNormal);
        //            }

        //            rSegmentMovement = lSafeMovement;

        //            if (rSegmentMovement.magnitude < EPSILON)
        //            {
        //                if (rRemainingMovement.magnitude < EPSILON)
        //                {
        //                    rRemainingMovement = Vector3.zero;
        //                }

        //                return true;
        //            }

        //            // Since there will be movement, we need to check grounding for this new position

        //            // TRT 3/16/2016 - When on very steep slopes, can cause 'is grounded' to be false. This
        //            //                 will enable 'in-air' only colliders and can cause unwanted issues when
        //            //                 we're really not in the air.
        //            //lIsGrounded = ProcessGrounding(_Transform.position + rSegmentMovement, rSegmentPositionDelta, lActorUp, mWorldUp, _BaseRadius, out lGroundHitInfo);
        //        }
        //    }

        //    // ----------------------------------------------------------------------
        //    // Clamp to slope
        //    // ----------------------------------------------------------------------
        //    if (mPrevState.IsGrounded && mState.GroundSurfaceAngle > EPSILON && rSegmentMovement.sqrMagnitude > 0f)
        //    {
        //        // TRT 11/13/15: Added this so that we don't do the logic when we're forcing the ground to
        //        //               be something like a ladder. In this case, a slanted floor can push us away
        //        //               from the ladder. Since we're trying to find a slope... makes sense the slope is the same ground as before
        //        if (mPrevState.Ground == mState.Ground)
        //        {
        //            Vector3 lUserMovement = (rSegmentIndex == 0 ? rSegmentMovement - lPlatformMovement : rSegmentMovement);

        //            RaycastHit lForwardGroundHitInfo;
        //            bool lIsValidGroundTest = TestGrounding(_Transform.position + rSegmentPositionDelta + lUserMovement, lActorUp, mWorldUp, out lForwardGroundHitInfo);

        //            // Ensure we are over the sloping ground directly (no spherecast)
        //            if (lIsValidGroundTest && lForwardGroundHitInfo.collider.transform == mState.Ground)
        //            {
        //                // Test if we're on a slope. If the forward normal and ground normal are the same, changes
        //                // are we are on a slope with a (near) constant angle.
        //                float lDeltaAngle = Vector3.Angle(lForwardGroundHitInfo.normal, mState.GroundSurfaceNormal);

        //                // If the angle of the ground has some slope and is nearly the same...
        //                if (lDeltaAngle < 5f)
        //                {
        //                    // If we're not moving up (ie a jump)...
        //                    if (Vector3.Dot(mState.MovementForceAdjust.normalized, mWorldUp) <= 0f)
        //                    {
        //                        // If we've gotten here, it's safe to say we're on a slope.
        //                        Vector3 lSlopeRight = Vector3.Cross(mState.GroundSurfaceNormal, -mWorldUp).normalized;
        //                        Vector3 lSlopeDirection = Vector3.Cross(lSlopeRight, mState.GroundSurfaceNormal).normalized;

        //                        // If we're moving in the direction of the slope (not against it, but down with it), we'll adjust the
        //                        // vertical component of the movement to compensate for the slope
        //                        float lMovementDot = Vector3.Dot(lUserMovement.normalized, lSlopeDirection);
        //                        if (lMovementDot > 0f && _IsGravityEnabled)
        //                        {
        //                            Vector3 lSlopeMovement = Vector3.Project(lUserMovement, lSlopeDirection) + Vector3.Project(lUserMovement, lSlopeRight);
        //                            lUserMovement = lSlopeMovement.normalized * lUserMovement.magnitude;

        //                            lIsSlopePushingDown = true;

        //                            // We'll consider this whole movement the slope adjust as it will be tested
        //                            // later if we should stop pushing down.
        //                            mState.MovementSlideAdjust = lUserMovement;
        //                        }
        //                        else if (lMovementDot < 0f)
        //                        {
        //                            lIsSlopePushingUp = true;

        //                            // We'll consider this whole movement the slope adjust as it will be tested
        //                            // later if we should stop pushing down.
        //                            mState.MovementSlideAdjust = lUserMovement;
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }

        //    // ----------------------------------------------------------------------
        //    // Collisions
        //    // ----------------------------------------------------------------------
        //    if (_IsCollisionEnabled)
        //    {
        //        bool lIsSlope = false;

        //        // Clear out the hit list so we can refill it
        //        for (int i = 0; i < mBodyShapeHits.Count; i++) { BodyShapeHit.Release(mBodyShapeHits[i]); }
        //        mBodyShapeHits.Clear();

        //        Quaternion lCurrentRotation = _Transform.rotation;
        //        _Transform.rotation = (_InvertRotationOrder ? mYaw * mTilt : mTilt * mYaw);

        //        // For each body shape, we want to see if there will be a collision
        //        // as we attempt to move. If there is, we may need to stop our deflect
        //        // our movement.
        //        for (int i = 0; i < BodyShapes.Count; i++)
        //        {
        //            if (lIsGrounded && !BodyShapes[i].IsEnabledOnGround) { continue; }
        //            if (!lIsGrounded && !BodyShapes[i].IsEnabledAboveGround) { continue; }

        //            // This one is a little trickier. We want to see if we're on a "slope" or "ramp". We'll
        //            // define that as any angular surface whose angle is consistant.
        //            if (mState.GroundSurfaceAngle > 5f && !BodyShapes[i].IsEnabledOnSlope)
        //            {
        //                // TRT 11/12: Removed the condition because steep angles were ignoring this as the ground
        //                // surface distance was greater than the radius.
        //                //if (mState.GroundSurfaceDistance < _BaseRadius)
        //                {
        //                    lIsSlope = true;

        //                    int lStateCount = Mathf.Min(_StateCount, 20);
        //                    for (int j = 0; j < lStateCount; j++)
        //                    {
        //                        int lStateIndex = (mStateIndex + j < _StateCount ? mStateIndex + j : mStateIndex + j - _StateCount);
        //                        if (mStates[lStateIndex].GroundSurfaceAngle != mState.GroundSurfaceAngle)
        //                        {
        //                            lIsSlope = false;
        //                            break;
        //                        }
        //                    }

        //                    if (lIsSlope) { continue; }
        //                }
        //            }

        //            // If we got here, we have a valid shape to test collisions against
        //            Vector3 lSegmentMovementDirection = rSegmentMovement.normalized;

        //            BodyShapeHit[] lBodyShapeHits = BodyShapes[i].CollisionCastAll(rSegmentPositionDelta, lSegmentMovementDirection, rSegmentMovement.magnitude, _CollisionLayers);
        //            if (lBodyShapeHits != null && lBodyShapeHits.Length > 0)
        //            {
        //                for (int j = 0; j < lBodyShapeHits.Length; j++)
        //                {
        //                    if (lBodyShapeHits[j] == null) { continue; }

        //                    // Test if we're hitting an object connected to our current platform
        //                    Transform lCurrentTransform = lBodyShapeHits[j].HitCollider.transform;
        //                    while (lCurrentTransform != null)
        //                    {
        //                        lBodyShapeHits[j].IsPlatformHit = (lCurrentTransform == mState.Ground);
        //                        if (lBodyShapeHits[j].IsPlatformHit) { break; }

        //                        lCurrentTransform = lCurrentTransform.parent;
        //                    }

        //                    // If we're on a slope and we've pushed ourselves down to move with it, we don't
        //                    // want that collision to stop us.
        //                    if (lIsSlopePushingDown && lBodyShapeHits[j].IsPlatformHit)
        //                    {
        //                        float lDeltaAngle = Vector3.Angle(lBodyShapeHits[j].HitNormal, mState.GroundSurfaceNormal);
        //                        if (lDeltaAngle < 2f)
        //                        {
        //                            continue;
        //                        }
        //                        else
        //                        {
        //                            // Without this, we get a small bump as we hit the bottom of the ramp. With it,
        //                            // if the player is moving a minicule amount, we get a small bump going up the ramp.
        //                            // Better with it.

        //                            // TRT 01/23/17: Removed because it caused issues when the ground is the walls as well (ie 1 mesh)
        //                            //continue;
        //                        }
        //                    }

        //                    // Check if the hit is below our step height
        //                    if (_MaxStepHeight > 0f && lBodyShapeHits[j].HitRootDistance < _MaxStepHeight)
        //                    {
        //                        Vector3 lVerticalMovement = Vector3.Project(rSegmentMovement, lActorUp);
        //                        if (lVerticalMovement.sqrMagnitude == 0f)
        //                        {
        //                            //continue;
        //                        }
        //                    }

        //                    // Only handle colliders whose hit normal collides with the direction we're moving. Otherwise,
        //                    // it's not in the way. For platform hits, we need to remove the platform movement first
        //                    if (lBodyShapeHits[j].IsPlatformHit)
        //                    {
        //                        Vector3 lNonPlatformMovement = rSegmentMovement - lPlatformMovement;
        //                        if (Vector3.Dot(lNonPlatformMovement.normalized, lBodyShapeHits[j].HitNormal) > -EPSILON)
        //                        {
        //                            continue;
        //                        }
        //                    }
        //                    // Here we can use the full movement
        //                    else if (Vector3.Dot(lSegmentMovementDirection, lBodyShapeHits[j].HitNormal) > -EPSILON)
        //                    {
        //                        continue;
        //                    }

        //                    // If we get here, re-allocate and then add the shape
        //                    BodyShapeHit lBodyShapeHit = BodyShapeHit.Allocate(lBodyShapeHits[j]);
        //                    mBodyShapeHits.Add(lBodyShapeHit);
        //                }

        //                // Release our local allocations
        //                for (int j = 0; j < lBodyShapeHits.Length; j++)
        //                {
        //                    BodyShapeHit.Release(lBodyShapeHits[j]);
        //                }
        //            }
        //        }

        //        // Sort the collisions
        //        if (mBodyShapeHits.Count > 1)
        //        {
        //            mBodyShapeHits = mBodyShapeHits.OrderBy(x => x.HitDistance).ToList();
        //        }

        //        // We only process one collision at a time. Otherwise, we could be bouncing
        //        // around. So, this is the closest collision that occured
        //        //for (int i = 0; i < mBodyShapeHits.Count; i = mBodyShapeHits.Count)
        //        if (mBodyShapeHits.Count > 0)
        //        {
        //            int i = 0;
        //            BodyShapeHit lBodyShapeHit = mBodyShapeHits[i];

        //            // Store the fact that we're colliding with something
        //            mState.IsColliding = true;
        //            mState.Collider = lBodyShapeHit.HitCollider;
        //            mState.ColliderHit = lBodyShapeHit.Hit;
        //            mState.ColliderHit.point = lBodyShapeHit.HitPoint;
        //            mState.ColliderHit.normal = lBodyShapeHit.HitNormal;
        //            mState.ColliderHitOrigin = lBodyShapeHit.HitOrigin;

        //            // Store the hit normal for easy access
        //            Vector3 lMovementHitNormal = lBodyShapeHit.HitNormal;
        //            float lMovementHitAngle = Vector3.Angle(lMovementHitNormal, lActorUp);

        //            // We need to support 'step-up' for solid body shapes. We'll basically ignore the
        //            // collision which will cause us to penetrate. Then our normal step-up process will continue.
        //            if (mState.IsGrounded &&
        //                (lMovementHitAngle < 2f || lMovementHitAngle >= MAX_GROUNDING_ANGLE) &&
        //                (_MaxStepHeight > 0f && lBodyShapeHit.HitRootDistance < _MaxStepHeight))
        //            {
        //                mState.IsPoppingUp = true;

        //                // Grab the lateral distance to the edge
        //                Vector3 lToHitPoint = lBodyShapeHit.HitPoint - (_Transform.position + rSegmentPositionDelta);
        //                Vector3 lVerticalToHitPoint = Vector3.Project(lToHitPoint, lActorUp);
        //                Vector3 lLateralToHitPoint = lToHitPoint - lVerticalToHitPoint;

        //                // If our movement exceeds it, allow us to move up to the edge. Then, 
        //                // continue with the remaining movement.
        //                if (rSegmentMovement.sqrMagnitude > lLateralToHitPoint.sqrMagnitude)
        //                {
        //                    // Determine how much to move to get past the edge
        //                    Vector3 lPreCollisionMovement = rSegmentMovement.normalized * (lLateralToHitPoint.magnitude + _SkinWidth);

        //                    // Use the remaining movement to continue
        //                    rRemainingMovement = rRemainingMovement + (rSegmentMovement - lPreCollisionMovement);

        //                    // Reset this segment's movement to what we discovered
        //                    rSegmentMovement = lPreCollisionMovement;
        //                }
        //                // If there isn't extra movement, we'll add some to get our axis past the edge
        //                else
        //                {
        //                    Vector3 lVerticalSegmentMovement = Vector3.Project(rSegmentMovement, lActorUp);
        //                    Vector3 lLateralSegmentMovement = rSegmentMovement - lVerticalSegmentMovement;

        //                    rSegmentMovement = rSegmentMovement + (lLateralSegmentMovement.normalized * (lLateralToHitPoint.magnitude + _SkinWidth));
        //                }
        //            }
        //            // If we're not popping up, we need to check for collisions and adjust our movement so we 
        //            // don't penetrate. Remaining movement will be handled next update.
        //            else
        //            {
        //                // If there is a positive distance, this is the initial room that
        //                // we have before we get to the collider.
        //                Vector3 lPreCollisionMovement = Vector3.zero;
        //                if (lBodyShapeHit.HitDistance > COLLISION_BUFFER - EPSILON)
        //                {
        //                    // Move forward to the collision point
        //                    lPreCollisionMovement = rSegmentMovement.normalized * Mathf.Min(lBodyShapeHit.HitDistance - COLLISION_BUFFER, rSegmentMovement.magnitude);
        //                }
        //                // If there is a negative distance, we've penetrated the collider and
        //                // we need to back up before we can continue.
        //                else if (lBodyShapeHit.HitDistance < COLLISION_BUFFER + EPSILON)
        //                {
        //                    // From the point on the body shape center/axis to the hit point
        //                    Vector3 lFromOrigin = lBodyShapeHit.HitPoint - lBodyShapeHit.HitOrigin;

        //                    // Pull back from the original position along the inverted collision vector (HitDistance is negative)
        //                    lPreCollisionMovement = lFromOrigin.normalized * (lBodyShapeHit.HitDistance - COLLISION_BUFFER);

        //                    // TRT 01/23/17: Added to keep from getting pushed up when the collision occurs on a slope
        //                    // and reports the hit vector is the same direction as our actor's "up"
        //                    if (lMovementHitAngle == 0f)
        //                    {
        //                        // TRT 01/31/2017: Replaced as there's times the hit angle is valid at 0 (ie falling off a ledge)
        //                        //lPreCollisionMovement = Vector3.zero;
        //                        float lMinMagnitude = Mathf.Min(lPreCollisionMovement.magnitude, rSegmentMovement.magnitude);
        //                        lPreCollisionMovement = lPreCollisionMovement.normalized * lMinMagnitude;
        //                    }
        //                }

        //                // If we've hit a max angle, remove any upward push
        //                if (lMovementHitAngle > (_MaxSlopeAngle > 0f ? _MaxSlopeAngle - 0.5f : MAX_GROUNDING_ANGLE) && lMovementHitAngle < 90f - EPSILON)
        //                {
        //                    Vector3 lVerticalPreCollisionMovement = Vector3.Project(lPreCollisionMovement, lActorUp);
        //                    if (Vector3.Dot(lPreCollisionMovement, lActorUp) > 0f)
        //                    {
        //                        lPreCollisionMovement = lPreCollisionMovement - lVerticalPreCollisionMovement;
        //                        lPreCollisionMovement = Vector3.zero;
        //                    }
        //                }

        //                // Track the amount of remaining movement we can deflect
        //                rRemainingMovement = rRemainingMovement + (rSegmentMovement - lPreCollisionMovement);

        //                // Reset this segment's movement to what we discovered
        //                rSegmentMovement = lPreCollisionMovement;
        //            }

        //            // After we reach the collider, this is the additional movement that needs to occur
        //            if (rRemainingMovement.sqrMagnitude > 0f)
        //            {
        //                // Normally, the remaining movement will simply be used for the next segment. However,
        //                // we need to ensure we're not being pushed into places we shouldn't go
        //                if (mState.Ground != null && mState.Ground == lBodyShapeHit.HitCollider.transform)
        //                {
        //                    // If we're moving in the direciton of the platform's movement we may not break
        //                    if (mState.MovementPlatformAdjust.sqrMagnitude > 0f && Vector3.Dot(rRemainingMovement.normalized, mState.MovementPlatformAdjust.normalized) > 0f)
        //                    {
        //                        // If our movement is less than the platform's movement
        //                        if (rRemainingMovement.sqrMagnitude < mState.MovementPlatformAdjust.sqrMagnitude + EPSILON)
        //                        {
        //                            // TRT: Removing so we can collide with a platform structure (like the ship cabin)
        //                            //lPreCollisionMovement = rSegmentMovement;
        //                            //rRemainingMovement = Vector3.zero;
        //                        }
        //                    }
        //                }

        //                // If the angle we're dealing with is steeper than we can move, we want to treat it as a vertical wall

        //                // Deflect the remaining movement based on what we hit
        //                Vector3 lDeflectedRemainingSegmentMovement = rRemainingMovement - Vector3.Project(rRemainingMovement, lBodyShapeHit.HitNormal);

        //                Vector3 lVerticalDeflectedRemainingSegmentMovement = Vector3.Project(lDeflectedRemainingSegmentMovement, lActorUp);
        //                float lVerticalDeflectedRemainingSegmentMovementDot = Vector3.Dot(lVerticalDeflectedRemainingSegmentMovement.normalized, lActorUp);

        //                // If we're already grounded, we don't want the remaining movement to push us into the ground
        //                if (mState.IsGrounded && mState.IsGroundSurfaceDirect && lVerticalDeflectedRemainingSegmentMovementDot < 0f)
        //                {
        //                    if (!lIsSlopePushingDown)
        //                    {
        //                        lDeflectedRemainingSegmentMovement = lDeflectedRemainingSegmentMovement - lVerticalDeflectedRemainingSegmentMovement;
        //                    }
        //                }
        //                // If we're getting pushed up by, we may need to stop it
        //                //else if (mState.IsGrounded && lVerticalDeflectedRemainingSegmentMovementDot > 0f)
        //                else if (lVerticalDeflectedRemainingSegmentMovementDot > 0f)
        //                {
        //                    // If we've hit the max angle, remove an upward push
        //                    if (lMovementHitAngle > (_MaxSlopeAngle > 0f ? _MaxSlopeAngle - 0.5f : MAX_GROUNDING_ANGLE) && lMovementHitAngle < 90f - EPSILON)
        //                    {
        //                        // We do this by treating the steep slope like a wall. Remove any of it's 'up' value
        //                        Vector3 lVerticalMovementHitNormal = Vector3.Project(lMovementHitNormal, lActorUp);
        //                        if (Vector3.Dot(lVerticalMovementHitNormal.normalized, lActorUp) > 0f)
        //                        {
        //                            lMovementHitNormal = (lMovementHitNormal - lVerticalMovementHitNormal).normalized;
        //                        }

        //                        lDeflectedRemainingSegmentMovement = rRemainingMovement - Vector3.Project(rRemainingMovement, lMovementHitNormal);
        //                        //lRemainingSegmentMovement = lRemainingSegmentMovement - lDeflectedVerticalRemainingSegmentMovement;
        //                    }
        //                }
        //                // If the collision stopped our forward movement, we probably want to stop the upward movement 
        //                else if (mState.IsGrounded && mState.Ground != lBodyShapeHit.HitCollider.transform)
        //                {
        //                    Vector3 lLateralMovement = lDeflectedRemainingSegmentMovement - lVerticalDeflectedRemainingSegmentMovement;
        //                    float lGroundSurfaceHitAngle = Vector3.Angle(lBodyShapeHit.HitNormal, mState.GroundSurfaceNormal);

        //                    // When the hit angle for the normals is < 90, that means the angle
        //                    // between the surfaces is > 90. When it's greater than 0, we're going up
        //                    // a ramp. If it's a head on collision, stop the upward ramp movement. This
        //                    // keeps us from stopping a jump against wall that is == 90 degrees.
        //                    if (lLateralMovement.sqrMagnitude < EPSILON_SQR && lGroundSurfaceHitAngle < 89f)
        //                    {
        //                        lDeflectedRemainingSegmentMovement = Vector3.zero;
        //                    }
        //                }

        //                rRemainingMovement = lDeflectedRemainingSegmentMovement;
        //            }

        //            // Update our grounding information so we can get correct normals
        //            lIsGrounded = ProcessGrounding(_Transform.position + lPlatformMovement, rSegmentPositionDelta, lActorUp, mWorldUp, _BaseRadius, out lGroundHitInfo);
        //            if (!lIsGrounded && mPrevState.IsSteppingDown && mState.GroundSurfaceDirectDistance < _MaxStepHeight && mState.GroundSurfaceDistance <= mPrevState.GroundSurfaceDistance)
        //            {
        //                lIsGrounded = true;
        //                mState.IsGrounded = true;
        //            }

        //            // If we're already grounded, we don't want the movement to push us into the ground.
        //            if (mState.IsGrounded && mState.IsGroundSurfaceDirect && rSegmentMovement.sqrMagnitude > 0f)
        //            {
        //                // Check if the movmeent is pushing us down
        //                Vector3 lVerticalSegmentMovement = Vector3.Project(rSegmentMovement, lActorUp);
        //                float lVerticalSegmentMovementDot = Vector3.Dot(lVerticalSegmentMovement, lActorUp);
        //                //if (Vector3.Dot(lVerticalSegmentMovement.normalized, lActorUp) < 0f)
        //                if (lVerticalSegmentMovementDot < -EPSILON)
        //                {
        //                    // There are some exceptions (platforms and slopes)
        //                    if (Vector3.Dot(mState.MovementPlatformAdjust, lActorUp) >= 0f)
        //                    {
        //                        // Don't remove any downward movement due to sliding
        //                        Vector3 lVerticalSlideMovement = Vector3.Project(mState.MovementSlideAdjust, lActorUp);

        //                        // Determine the amount of vertical movement there is and counter it
        //                        rSegmentMovement = rSegmentMovement - (lVerticalSegmentMovement - lVerticalSlideMovement);

        //                        // Get rid of the remaining movement
        //                        rRemainingMovement = Vector3.zero;
        //                    }
        //                }

        //                // As we remove the vertical component, we may find that the remaining movement still
        //                // pushes us into the collider. If so, we just want to stop.
        //                float lDeltaAngle = Vector3.Angle(rRemainingMovement.normalized, rSegmentMovement.normalized);
        //                if (lDeltaAngle < 0.1f)
        //                {
        //                    if (!lIsSlopePushingUp || lBodyShapeHit.HitCollider.transform != mState.Ground)
        //                    {
        //                        rRemainingMovement = Vector3.zero;
        //                    }
        //                }
        //            }

        //            // If we have a force pushing us up (like a jump), but we
        //            // collide with something pushing us down, we want to cancel the force.
        //            if (mState.MovementForceAdjust.sqrMagnitude > 0f)
        //            {
        //                float lBodyShapeHitUpDot = Vector3.Dot(lBodyShapeHit.HitNormal, lActorUp);
        //                if (lBodyShapeHitUpDot < -EPSILON)
        //                {
        //                    // TRT 3/27/16 - In the case of a perpendicular collision, we don't actually
        //                    // want it to stop our vertical movement.
        //                    float lHitDot = Vector3.Dot(lBodyShapeHit.Hit.normal, lActorUp);
        //                    if (Mathf.Abs(lHitDot) > EPSILON)
        //                    {
        //                        Vector3 lVerticalAccumulatedGravity = Vector3.Project(mAccumulatedVelocity, mWorldUp);
        //                        if (Vector3.Dot(lVerticalAccumulatedGravity.normalized, mWorldUp) > 0f)
        //                        {
        //                            mAccumulatedVelocity = Vector3.zero;
        //                        }
        //                    }
        //                }
        //            }
        //        }

        //        // Reset the rotation
        //        _Transform.rotation = lCurrentRotation;
        //    }

        //    // TRT 10/1/16: Added the "lIsGrounded" condition to prevent hanging when we hit a edge while jumping/falling
        //    if (lIsGrounded)
        //    {
        //        // Check if the remaining movement causes us to bounce back against our movement direction. If so, we'll stop
        //        // so that we don't get stuttering.
        //        Vector3 lRemainingSegmentMovementProj = Vector3.Project(rRemainingMovement, mState.Movement.normalized);
        //        if (Vector3.Dot(lRemainingSegmentMovementProj.normalized, mState.Movement.normalized) < 0f)
        //        {
        //            // We'll keep ther vertical component only
        //            rRemainingMovement = Vector3.Project(rRemainingMovement, mWorldUp);
        //        }
        //    }

        //    // ----------------------------------------------------------------------
        //    // Determine the tilting based on the ground.
        //    // ----------------------------------------------------------------------
        //    if (rOrientToGround)
        //    {
        //        //Log.FileWrite("PM() orient to ground nml:" + StringHelper.ToString(mState.GroundSurfaceNormal));
        //        if (_OrientToGround || mPrevState.IsTilting || mState.IsTilting)
        //        {
        //            mOrientToGroundNormalTarget = Vector3.up;

        //            if (_OrientToGround)
        //            {
        //                // If we're told to go do a specific normal, do it
        //                if (mTargetGroundNormal.sqrMagnitude > 0f)
        //                {
        //                    mOrientToGroundNormalTarget = mTargetGroundNormal;
        //                }
        //                // If we're "keeping" orientation or NOT jumping, stay with the surface direction
        //                else if (lIsGrounded || _KeepOrientationInAir || (mAccumulatedForceVelocity.sqrMagnitude == 0f && mState.GroundSurfaceDistance < _OrientToGroundDistance))
        //                {
        //                    if (_MaxSlopeAngle == 0f || mState.GroundSurfaceAngle < _MaxSlopeAngle - 0.5f)
        //                    {
        //                        mOrientToGroundNormalTarget = mState.GroundSurfaceDirectNormal;
        //                    }
        //                }
        //            }

        //            // Determine the final tilt
        //            mOrientToGroundNormal = mOrientToGroundNormalTarget;

        //            Vector3 lTiltUp = (mTilt * mYaw).Up();
        //            mTilt = QuaternionExt.FromToRotation(lTiltUp, mOrientToGroundNormal) * mTilt;
        //        }
        //    }

        //    return true;
        //}

        /// <summary>
        /// Determines if a collision has occured and returns the safe movement and the remaining movement that it can't do.
        /// </summary>
        /// <param name="rSegmentPositionDelta"></param>
        /// <param name="rSegmentMovement"></param>
        /// <param name="rRemainingMovement"></param>
        /// <returns></returns>
        protected bool ProcessCollisions(Vector3 rSegmentPositionDelta, ref Vector3 rSegmentMovement, ref Vector3 rRemainingMovement)
        {
            // Clear out the hit list so we can refill it
            for (int i = 0; i < mBodyShapeHits.Count; i++) { BodyShapeHit.Release(mBodyShapeHits[i]); }
            mBodyShapeHits.Clear();

            //Quaternion lCurrentRotation = _Transform.rotation;
            _Transform.rotation = (_InvertRotationOrder ? mYaw * mTilt : mTilt * mYaw);

            // For each body shape, we want to see if there will be a collision
            // as we attempt to move. If there is, we may need to stop our deflect
            // our movement.
            for (int i = 0; i < BodyShapes.Count; i++)
            {
                if (!mState.IsGrounded && !BodyShapes[i].IsEnabledAboveGround) { continue; }
                if (mState.IsGrounded && !BodyShapes[i].IsEnabledOnGround) { continue; }
                if (mState.IsGrounded && mState.GroundSurfaceAngle > 5f && !BodyShapes[i].IsEnabledOnSlope) { continue; }

                // If we got here, we have a valid shape to test collisions against
                Vector3 lSegmentMovementDirection = rSegmentMovement.normalized;

                BodyShapeHit[] lBodyShapeHits = BodyShapes[i].CollisionCastAll(rSegmentPositionDelta, lSegmentMovementDirection, rSegmentMovement.magnitude, _CollisionLayers);
                if (lBodyShapeHits != null && lBodyShapeHits.Length > 0)
                {
                    for (int j = 0; j < lBodyShapeHits.Length; j++)
                    {
                        if (lBodyShapeHits[j] == null) { continue; }

                        // Don't collide if we're are using the max step height
                        if (mState.IsGrounded && _MaxStepHeight > 0f && lBodyShapeHits[j].HitRootDistance < _MaxStepHeight)
                        {
                            continue;
                        }

                        // If we are using full colliders (ie no Max Step Height), we don't want to consider the floor a collision.
                        float lHitTiltDot = Vector3.Dot(lBodyShapeHits[j].HitNormal, _Transform.up);
                        if (_MaxStepHeight <= 0f && lHitTiltDot > 0.8f)
                        {
                            continue;
                        }

                        // Only handle collision whose hit normal collides with the direction we're moving.
                        float lMovementCollsionDot = Vector3.Dot(lSegmentMovementDirection, lBodyShapeHits[j].HitNormal);
                        if (lMovementCollsionDot > -EPSILON)
                        {
                            continue;
                        }

                        // If we get here, re-allocate and then add the shape
                        BodyShapeHit lBodyShapeHit = BodyShapeHit.Allocate(lBodyShapeHits[j]);
                        mBodyShapeHits.Add(lBodyShapeHit);
                    }

                    // Release our local allocations
                    for (int j = 0; j < lBodyShapeHits.Length; j++)
                    {
                        BodyShapeHit.Release(lBodyShapeHits[j]);
                    }
                }
            }

            // Sort the collisions
            if (mBodyShapeHits.Count > 1)
            {
                mBodyShapeHits = mBodyShapeHits.OrderBy(x => x.HitDistance).ToList();
            }

            // We only process one collision at a time. Otherwise, we could be bouncing
            // around. So, this is the closest collision that occured
            //for (int i = 0; i < mBodyShapeHits.Count; i = mBodyShapeHits.Count)
            if (mBodyShapeHits.Count > 0)
            {
                int i = 0;
                BodyShapeHit lBodyShapeHit = mBodyShapeHits[i];

                // Store the fact that we're colliding with something
                mState.IsColliding = true;
                mState.Collider = lBodyShapeHit.HitCollider;
                mState.ColliderHit = lBodyShapeHit.Hit;
                mState.ColliderHit.point = lBodyShapeHit.HitPoint;
                mState.ColliderHit.normal = lBodyShapeHit.HitNormal;
                mState.ColliderHitOrigin = lBodyShapeHit.HitOrigin;

                // If there is a positive distance, this is the initial room that
                // we have before we get to the collider.
                Vector3 lPreCollisionMovement = Vector3.zero;
                if (lBodyShapeHit.HitDistance > COLLISION_BUFFER)
                {
                    // Move forward to the collision point
                    lPreCollisionMovement = rSegmentMovement.normalized * Mathf.Min(lBodyShapeHit.HitDistance - COLLISION_BUFFER, rSegmentMovement.magnitude);
                }
                // If there is a negative distance, we've penetrated the collider and
                // we need to back up before we can continue.
                else if (lBodyShapeHit.HitDistance < COLLISION_BUFFER)
                {
                    // From the point on the body shape center/axis to the hit point
                    Vector3 lFromOrigin = lBodyShapeHit.HitPoint - lBodyShapeHit.HitOrigin;

                    // Pull back from the original position along the inverted collision vector (HitDistance is negative)
                    lPreCollisionMovement = lFromOrigin.normalized * (lBodyShapeHit.HitDistance - COLLISION_BUFFER);
                }

                // Track the amount of remaining movement we can deflect
                rRemainingMovement = rRemainingMovement + (rSegmentMovement - lPreCollisionMovement);

                // Reset this segment's movement to what we discovered
                rSegmentMovement = lPreCollisionMovement;
            }

            return (mBodyShapeHits.Count > 0);
        }

        /// <summary>
        /// Used to determine if the actor is on a surface (ground) or not. This is a simple 
        /// test function that primarily focuses on gathering angles and such.
        /// </summary>
        /// <param name="rActorPosition">Position of the actor</param>
        /// <param name="rOffset">Any position offset</param>
        /// <param name="rActorUp">Up vector of the actor</param>
        /// <param name="rWorldUp">Up vector ofthe world</param>
        /// <param name="rGroundRadius">Radius for the sphere cast</param>
        /// <param name="rGroundHitInfo">RaycastHit information</param>
        /// <returns>Determines if the actor is grounded or not</returns>
        protected bool ProcessGrounding(Vector3 rActorPosition, Vector3 rOffset, Vector3 rActorUp, Vector3 rWorldUp, float rGroundRadius, out RaycastHit rGroundHitInfo)
        {
            Vector3 lRayStart = rActorPosition + rOffset + (rActorUp * _GroundingStartOffset);
            Vector3 lRayDirection = -rActorUp;
            float lRayDistance = _GroundingStartOffset + _GroundingDistance;

            // Start with a simple ray. This would be the object directly under the actor
            bool lIsGrounded = false;

            if (_IsGroundingLayersEnabled)
            {
                lIsGrounded = RaycastExt.SafeRaycast(lRayStart, lRayDirection, out rGroundHitInfo, lRayDistance, _GroundingLayers, _Transform, mIgnoreTransforms);
            }
            else
            {
                lIsGrounded = RaycastExt.SafeRaycast(lRayStart, lRayDirection, out rGroundHitInfo, lRayDistance, -1, _Transform, mIgnoreTransforms);
            }

            if (lIsGrounded)
            {
                // TRT 12/11/16 - Modify the skin width for off-mesh links. Links are about 0.05f above the surface.
                float lSkinWidth = (!mIgnoreUseTransform && _UseTransformPosition ? _AltSkinWidth : _SkinWidth);
                lIsGrounded = (rGroundHitInfo.distance - _GroundingStartOffset < lSkinWidth + EPSILON);

                // Whether we're grounded or not, return the ground information
                mState.Ground = rGroundHitInfo.collider.gameObject.transform;
                mState.GroundPosition = mState.Ground.position;
                mState.GroundRotation = mState.Ground.rotation;

                mState.GroundSurfaceAngle = Vector3.Angle(rGroundHitInfo.normal, rActorUp);
                mState.GroundSurfaceNormal = rGroundHitInfo.normal;
                mState.GroundSurfaceDistance = rGroundHitInfo.distance - _GroundingStartOffset;
                mState.GroundSurfacePoint = rGroundHitInfo.point;
                mState.GroundSurfaceDirection = lRayDirection;

                Vector3 lForwardSlopeDirection = _Transform.forward;
                Vector3 lGroundNormal = mState.GroundSurfaceNormal;
                Vector3.OrthoNormalize(ref lGroundNormal, ref lForwardSlopeDirection);
                mState.GroundSurfaceForwardAngle = -Vector3Ext.SignedAngle(_Transform.forward, lForwardSlopeDirection, _Transform.right);

                mState.IsGroundSurfaceDirect = true;
                mState.GroundSurfaceDirectNormal = mState.GroundSurfaceNormal;
                mState.GroundSurfaceDirectDistance = mState.GroundSurfaceDistance;

                // Just in case we need to shoot the sphere cast
                lRayDistance = rGroundHitInfo.distance + rGroundRadius;

                // TRT 9/4/16: On non-uniform slopes (like terrain), we do a last check for grounding
                if (!lIsGrounded && rGroundHitInfo.collider is TerrainCollider)
                {
                    if (mState.GroundSurfaceAngle > 10f && mState.GroundSurfaceDistance < 0.03f)
                    {
                        lIsGrounded = true;
                    }
                }
            }
            else
            {
                // We're not grounded, but we may not want to revert to the previous normal
                if (_KeepOrientationInAir)
                {
                    mState.GroundSurfaceNormal = mPrevState.GroundSurfaceNormal;
                    mState.GroundSurfaceDirectNormal = mPrevState.GroundSurfaceDirectNormal;
                }
            }

            // If we aren't grounded, do a sphere test
            if (!lIsGrounded)
            {
                bool lIgnore = false;
                Vector3 lClosestPoint = rGroundHitInfo.point;

                // Test if there's additional colliders to support us. We increate the ground radius
                // to reachout to points that are at our diagonal.
                lRayStart = rActorPosition + rOffset + (rActorUp * rGroundRadius);

                int lHits = 0;
                Collider[] lColliders = null;

                if (_IsGroundingLayersEnabled)
                {
                    lHits = RaycastExt.SafeOverlapSphere(lRayStart, rGroundRadius * ONE_OVER_COS45, out lColliders, _GroundingLayers, _Transform, mIgnoreTransforms);
                }
                else
                {
                    lHits = RaycastExt.SafeOverlapSphere(lRayStart, rGroundRadius * ONE_OVER_COS45, out lColliders, -1, _Transform, mIgnoreTransforms);
                }

                // With one or no colliders, this is easy
                if (lColliders == null || lHits == 0)
                {
                    lIgnore = true;
                    lIsGrounded = false;
                }
                // With one collider, we just test if it's close enough to ground us.
                // If not, we need to slide down... hence we're not grounded.
                else if (lHits == 1)
                {
                    lClosestPoint = GeometryExt.ClosestPoint(lRayStart, lColliders[0]);
                    if (lClosestPoint != Vector3Ext.Null)
                    {
                        Vector3 lLocalOrbitPoint = _Transform.InverseTransformPoint(lClosestPoint - rOffset);
                        lLocalOrbitPoint.y = 0f;

                        // If any of our hit points are super close to the root, we have to consider 
                        // ourselves grounded.  This just keeps us safe
                        if (lLocalOrbitPoint.magnitude > (rGroundRadius * 0.25f))
                        {
                            float lMaxAngle = (_MaxSlopeAngle > 0f ? _MaxSlopeAngle - 0.5f : MAX_GROUNDING_ANGLE);

                            // The problem we have is if we're on a steep slope that isn't too steep.
                            // The hit will be outside of our desired range. So, we need to test.
                            if (rGroundHitInfo.collider == lColliders[0] && mState.GroundSurfaceAngle < lMaxAngle + EPSILON)
                            {
                                lIgnore = false;
                                lIsGrounded = true;
                            }
                            else
                            {
                                lIgnore = true;
                                lIsGrounded = false;

                                // TRT 7/17/16 - Need to reset the distance based on the direct
                                if (mState.GroundSurfaceDistance < 0.01f)
                                {
                                    mState.GroundSurfaceDistance = float.MaxValue;
                                }
                            }
                        }
                    }
                }
                // With more than one colliders, we need to see if they are spread out enough to
                // support the object. We do this by gathering "orbit points". If there's enough
                // angular difference between the points, we know we have supports. Otherwise, the
                // supports are all one one side and we still slide down.
                else
                {
                    bool lTestOrbitAngles = true;

                    int lOrbitAnglesCount = 0;
                    Vector3[] lOrbitPoints = new Vector3[lHits];
                    Vector3[] lLocalOrbitPoints = new Vector3[lHits];
                    float[] lOrbitAngles = new float[lHits];

                    // First, gather all the points. Imagine we're orbiting the ray start and
                    // collecting all the hit points. We want the angular difference between them.
                    for (int i = 0; i < lHits; i++)
                    {
                        if (lColliders[i] == rGroundHitInfo.collider) { continue; }

                        Vector3 lOrbitPoint = GeometryExt.ClosestPoint(lRayStart, lColliders[i]);
                        if (lOrbitPoint == Vector3Ext.Null) { continue; }

                        Vector3 lLocalOrbitPoint = _Transform.InverseTransformPoint(lOrbitPoint - rOffset);

                        // If the hit is too high, we won't count it as grounding
                        if (lLocalOrbitPoint.y >= rGroundRadius + EPSILON)
                        {
                            continue;
                        }

                        // If the hit it too low (meaning too far way, we won't count it as grounding
                        if (-lLocalOrbitPoint.y > _SkinWidth + EPSILON)
                        {
                            continue;
                        }

                        // Since we're in the safe zone, figure out the lateral distance
                        lLocalOrbitPoint.y = 0f;

                        // If any of our hit points are super close to the root, we have to consider 
                        // ourselves grounded.  This just keeps us safe
                        if (lLocalOrbitPoint.magnitude < (rGroundRadius * 0.25f))
                        {
                            lClosestPoint = lOrbitPoint;

                            lIsGrounded = true;
                            lTestOrbitAngles = false;
                            break;
                        }

                        // We're now far enough away to think about the orbit angle
                        lOrbitPoints[lOrbitAnglesCount] = lOrbitPoint;
                        lLocalOrbitPoints[lOrbitAnglesCount] = lLocalOrbitPoint;
                        lOrbitAngles[lOrbitAnglesCount] = Vector3Ext.SignedAngle(lLocalOrbitPoint.normalized, _Transform.forward);
                        lOrbitAnglesCount++;
                    }

                    // With the hit points, we can grab the angular difference between them and see if
                    // they are far enough appart to create supports.
                    if (lTestOrbitAngles)
                    {
                        if (lOrbitAnglesCount > 0)
                        {
                            lIgnore = true;
                            for (int i = 0; i < lOrbitAnglesCount; i++)
                            {
                                for (int j = i + 1; j < lOrbitAnglesCount; j++)
                                {
                                    float lLocalAngle = Vector3.Angle(lLocalOrbitPoints[i], lLocalOrbitPoints[j]);
                                    if (lLocalAngle > 60f)
                                    {
                                        lClosestPoint = lOrbitPoints[i];
                                        if (Vector3.SqrMagnitude(lOrbitPoints[j] - lRayStart) < Vector3.SqrMagnitude(lClosestPoint - lRayStart))
                                        {
                                            lClosestPoint = lOrbitPoints[j];
                                        }

                                        lIgnore = false;
                                        lIsGrounded = true;

                                        break;
                                    }
                                }

                                if (!lIgnore) { break; }
                            }
                        }
                        else
                        {
                            lIgnore = true;
                        }
                    }

                    // Clean up based on the results
                    if (lIgnore)
                    {
                        lIsGrounded = false;
                    }
                }

                // "Ignoring" means that we DON'T consider this hit a grounding event and we'll
                // essencially slide off the edge. The collision detection part will cause that to happen.
                //
                // If we are "Not Ignoring", that means we need to consider this edge a grounding event
                // and we don't want to slide. This is important if we're over something like a gap.
                if (!lIgnore)
                {
                    Vector3 lLocalOrbitPoint = _Transform.InverseTransformPoint(lClosestPoint - rOffset);

                    Vector3 lToClosestPoint = lClosestPoint - lRayStart;
                    lRayDirection = lToClosestPoint.normalized;
                    lRayDistance = lToClosestPoint.magnitude + _SkinWidth;

                    RaycastHit lRaycastHit;

                    bool lGroundHit = false;
                    if (_IsGroundingLayersEnabled)
                    {
                        lGroundHit = RaycastExt.SafeRaycast(lRayStart, lRayDirection, out lRaycastHit, lRayDistance, _GroundingLayers, _Transform, mIgnoreTransforms);
                    }
                    else
                    {
                        lGroundHit = RaycastExt.SafeRaycast(lRayStart, lRayDirection, out lRaycastHit, lRayDistance, -1, _Transform, mIgnoreTransforms);
                    }

                    if (lGroundHit)
                    {
                        float lGroundDistance = lRaycastHit.distance - rGroundRadius;
                        lIsGrounded = lGroundDistance < (_SkinWidth + EPSILON) * ONE_OVER_COS45;

                        mState.Ground = lRaycastHit.collider.gameObject.transform;
                        mState.GroundPosition = mState.Ground.position;
                        mState.GroundRotation = mState.Ground.rotation;

                        mState.GroundSurfaceAngle = Vector3.Angle(lRaycastHit.normal, rActorUp);
                        mState.GroundSurfaceNormal = lRaycastHit.normal;
                        mState.GroundSurfaceDistance = -lLocalOrbitPoint.y;
                        mState.GroundSurfacePoint = lClosestPoint;
                        mState.GroundSurfaceDirection = lRayDirection;
                        mState.IsGroundSurfaceDirect = false;

                        Vector3 lForwardSlopeDirection = _Transform.forward;
                        Vector3 lGroundNormal = mState.GroundSurfaceNormal;
                        Vector3.OrthoNormalize(ref lGroundNormal, ref lForwardSlopeDirection);
                        mState.GroundSurfaceForwardAngle = -Vector3Ext.SignedAngle(_Transform.forward, lForwardSlopeDirection, _Transform.right);

                        rGroundHitInfo = lRaycastHit;
                    }
                }
            }

            // Set the final grounding state
            mState.IsGrounded = lIsGrounded;

            // Debug info
            //DebugDraw.DrawSphereMesh(rGroundHitInfo.point, 0.02f, (lIsGrounded ? Color.green : Color.yellow), 1f);

            // Return the grounded value
            return lIsGrounded;
        }

        /// <summary>
        /// Grabs the ground distance from the specified point
        /// </summary>
        /// <param name="rPosition">Position we want to try to ground</param>
        /// <returns>Float that is the distance. A negative value means that we are sunk. A posative distance means there's a gap. float.Max means no ground was reached.</returns>
        protected float GetGroundDistance(Vector3 rPosition, Vector3 rActorUp)
        {
            RaycastHit lHitInfo;
            Vector3 lRayStart = rPosition + (rActorUp * _GroundingStartOffset);
            Vector3 lRayDirection = -rActorUp;
            float lRayDistance = _GroundingStartOffset + _GroundingDistance;

            // Start with a simple ray. This would be the object directly under the actor
            bool lIsHit = false;
            bool lIsGrounded = false;

            if (_IsGroundingLayersEnabled)
            {
                lIsHit = RaycastExt.SafeRaycast(lRayStart, lRayDirection, out lHitInfo, lRayDistance, _GroundingLayers, _Transform, mIgnoreTransforms);
            }
            else
            {
                lIsHit = RaycastExt.SafeRaycast(lRayStart, lRayDirection, out lHitInfo, lRayDistance, -1, _Transform, mIgnoreTransforms);
            }

            float lDistance = float.MaxValue;

            if (lIsHit)
            {
                float lGroundingDistance = (_ForceGrounding ? _ForceGroundingDistance : _SkinWidth);

                // TRT 12/11/16 - Modify the skin width for off-mesh links. Links are about 0.05f above the surface.
                float lSkinWidth = (!mIgnoreUseTransform && _UseTransformPosition ? _AltSkinWidth : lGroundingDistance);
                lIsGrounded = (lHitInfo.distance - _GroundingStartOffset < lSkinWidth + EPSILON);

                lDistance = lHitInfo.distance - _GroundingStartOffset;

                mState.Ground = lHitInfo.collider.transform;

                mState.GroundSurfacePoint = lHitInfo.point;
                mState.GroundSurfaceDistance = lDistance;
                mState.GroundSurfaceDirection = lRayDirection;
                mState.GroundSurfaceNormal = lHitInfo.normal;
                mState.GroundSurfaceAngle = Vector3.Angle(lHitInfo.normal, rActorUp);

                Vector3 lForwardSlopeDirection = _Transform.forward;
                Vector3 lGroundNormal = mState.GroundSurfaceNormal;
                Vector3.OrthoNormalize(ref lGroundNormal, ref lForwardSlopeDirection);
                mState.GroundSurfaceForwardAngle = -Vector3Ext.SignedAngle(_Transform.forward, lForwardSlopeDirection, _Transform.right);

                mState.IsGroundSurfaceDirect = true;
                mState.GroundSurfaceDirectDistance = lDistance;
                mState.GroundSurfaceDirectNormal = lHitInfo.normal;
            }

            // If we aren't grounded, do a sphere test
            if (!lIsGrounded)
            {
                bool lIgnore = false;
                Vector3 lClosestPoint = lHitInfo.point;

                // Test if there's additional colliders to support us. We increate the ground radius
                // to reachout to points that are at our diagonal.
                lRayStart = rPosition + (rActorUp * _BaseRadius);

                if (_ShowDebug)
                {
                    Graphics.GraphicsManager.DrawSphere(lRayStart, _BaseRadius, Color.magenta, 0.1f);
                    Graphics.GraphicsManager.DrawSphere(lRayStart, _BaseRadius * ONE_OVER_COS45, Color.magenta, 0.1f);
                }

                int lHits = 0;
                Collider[] lColliders = null;

                if (_IsGroundingLayersEnabled)
                {
                    lHits = RaycastExt.SafeOverlapSphere(lRayStart, _BaseRadius * ONE_OVER_COS45, out lColliders, _GroundingLayers, _Transform, mIgnoreTransforms);
                }
                else
                {
                    lHits = RaycastExt.SafeOverlapSphere(lRayStart, _BaseRadius * ONE_OVER_COS45, out lColliders, -1, _Transform, mIgnoreTransforms);
                }

                // With one or no colliders, this is easy
                if (lColliders == null || lHits == 0)
                {
                    lIgnore = true;
                    lIsGrounded = false;
                }
                // With one collider, we just test if it's close enough to ground us.
                // If not, we need to slide down... hence we're not grounded.
                else if (lHits == 1)
                {
                    lClosestPoint = GeometryExt.ClosestPoint(rPosition + (_Transform.up * 0.05f), lColliders[0]);
                    if (lClosestPoint != Vector3Ext.Null)
                    {
                        if (_ShowDebug)
                        {
                            Graphics.GraphicsManager.DrawPoint(lClosestPoint, Color.red);
                        }

                        Vector3 lLocalOrbitPoint = _Transform.InverseTransformPoint(lClosestPoint);
                        lLocalOrbitPoint.y = 0f;

                        if (lLocalOrbitPoint.magnitude < _BaseRadius)
                        {
                            lIgnore = false;
                            lIsGrounded = true;
                        }
                        else
                        {
                            lIgnore = true;
                            lIsGrounded = false;
                        }

                        //// If any of our hit points are super close to the root, we have to consider 
                        //// ourselves grounded.  This just keeps us safe
                        //if (lLocalOrbitPoint.magnitude > (_BaseRadius * 0.25f))
                        //{
                        //    float lMaxAngle = (_MaxSlopeAngle > 0f ? _MaxSlopeAngle - 0.5f : MAX_GROUNDING_ANGLE);

                        //    // The problem we have is if we're on a steep slope that isn't too steep.
                        //    // The hit will be outside of our desired range. So, we need to test.
                        //    if (lHitInfo.collider == lColliders[0] && mState.GroundSurfaceAngle < lMaxAngle + EPSILON)
                        //    {
                        //        lIgnore = false;
                        //        lIsGrounded = true;
                        //    }
                        //    else
                        //    {
                        //        lIgnore = true;
                        //        lIsGrounded = false;

                        //        // TRT 7/17/16 - Need to reset the distance based on the direct
                        //        if (mState.GroundSurfaceDistance < 0.01f)
                        //        {
                        //            mState.GroundSurfaceDistance = float.MaxValue;
                        //        }
                        //    }
                        //}
                    }
                }
                // With more than one colliders, we need to see if they are spread out enough to
                // support the object. We do this by gathering "orbit points". If there's enough
                // angular difference between the points, we know we have supports. Otherwise, the
                // supports are all one one side and we still slide down.
                else
                {
                    bool lTestOrbitAngles = true;

                    int lOrbitAnglesCount = 0;
                    Vector3[] lOrbitPoints = new Vector3[lHits];
                    Vector3[] lLocalOrbitPoints = new Vector3[lHits];
                    float[] lOrbitAngles = new float[lHits];

                    // First, gather all the points. Imagine we're orbiting the ray start and
                    // collecting all the hit points. We want the angular difference between them.
                    for (int i = 0; i < lHits; i++)
                    {
                        if (lColliders[i] == lHitInfo.collider) { continue; }

                        Vector3 lOrbitPoint = GeometryExt.ClosestPoint(rPosition + (_Transform.up * 0.05f), lColliders[i]);
                        if (lOrbitPoint == Vector3Ext.Null) { continue; }

                        if (_ShowDebug)
                        {
                            Graphics.GraphicsManager.DrawPoint(lOrbitPoint, Color.red);
                        }

                        Vector3 lLocalOrbitPoint = _Transform.InverseTransformPoint(lOrbitPoint);

                        // If the hit is too high, we won't count it as grounding
                        if (lLocalOrbitPoint.y >= _BaseRadius + EPSILON)
                        {
                            continue;
                        }

                        //// If the hit it too low (meaning too far way, we won't count it as grounding
                        //if (-lLocalOrbitPoint.y > _SkinWidth + EPSILON)
                        //{
                        //    continue;
                        //}

                        // Since we're in the safe zone, figure out the lateral distance
                        lLocalOrbitPoint.y = 0f;

                        // If any of our hit points are super close to the root, we have to consider 
                        // ourselves grounded.  This just keeps us safe
                        if (lLocalOrbitPoint.magnitude < _BaseRadius)
                        {
                            lClosestPoint = lOrbitPoint;

                            lIsGrounded = true;
                            lTestOrbitAngles = false;
                            break;
                        }

                        // We're now far enough away to think about the orbit angle
                        lOrbitPoints[lOrbitAnglesCount] = lOrbitPoint;
                        lLocalOrbitPoints[lOrbitAnglesCount] = lLocalOrbitPoint;
                        lOrbitAngles[lOrbitAnglesCount] = Vector3Ext.SignedAngle(lLocalOrbitPoint.normalized, _Transform.forward);
                        lOrbitAnglesCount++;
                    }

                    // With the hit points, we can grab the angular difference between them and see if
                    // they are far enough appart to create supports.
                    if (lTestOrbitAngles)
                    {
                        if (lOrbitAnglesCount > 0)
                        {
                            lIgnore = true;
                            for (int i = 0; i < lOrbitAnglesCount; i++)
                            {
                                for (int j = i + 1; j < lOrbitAnglesCount; j++)
                                {
                                    float lLocalAngle = Vector3.Angle(lLocalOrbitPoints[i], lLocalOrbitPoints[j]);
                                    if (lLocalAngle > 60f)
                                    {
                                        lClosestPoint = lOrbitPoints[i];
                                        if (Vector3.SqrMagnitude(lOrbitPoints[j] - lRayStart) < Vector3.SqrMagnitude(lClosestPoint - lRayStart))
                                        {
                                            lClosestPoint = lOrbitPoints[j];
                                        }

                                        lIgnore = false;
                                        lIsGrounded = true;

                                        break;
                                    }
                                }

                                if (!lIgnore) { break; }
                            }
                        }
                        else
                        {
                            lIgnore = true;
                        }
                    }

                    // Clean up based on the results
                    if (lIgnore)
                    {
                        lIsGrounded = false;
                    }
                }

                // "Ignoring" means that we DON'T consider this hit a grounding event and we'll
                // essencially slide off the edge. The collision detection part will cause that to happen.
                //
                // If we are "Not Ignoring", that means we need to consider this edge a grounding event
                // and we don't want to slide. This is important if we're over something like a gap.
                if (!lIgnore)
                {
                    Vector3 lLocalOrbitPoint = _Transform.InverseTransformPoint(lClosestPoint);

                    //Vector3 lToClosestPoint = lClosestPoint - lRayStart;
                    //lRayDirection = lToClosestPoint.normalized;
                    //lRayDistance = lToClosestPoint.magnitude + _SkinWidth;

                    //Vector3 lToClosestPoint = lClosestPoint - lRayStart;

                    lRayStart = lClosestPoint + (rActorUp * _BaseRadius);
                    lRayDirection = -rActorUp;
                    lRayDistance = _BaseRadius + _SkinWidth;

                    RaycastHit lRaycastHit;

                    bool lGroundHit = false;
                    if (_IsGroundingLayersEnabled)
                    {
                        lGroundHit = RaycastExt.SafeRaycast(lRayStart, lRayDirection, out lRaycastHit, lRayDistance, _GroundingLayers, _Transform, mIgnoreTransforms);
                    }
                    else
                    {
                        lGroundHit = RaycastExt.SafeRaycast(lRayStart, lRayDirection, out lRaycastHit, lRayDistance, -1, _Transform, mIgnoreTransforms);
                    }

                    if (lGroundHit)
                    {
                        lDistance = lRaycastHit.distance - _BaseRadius - lLocalOrbitPoint.y;
                        lIsGrounded = lDistance < (_SkinWidth + EPSILON) * ONE_OVER_COS45;

                        mState.Ground = lRaycastHit.collider.gameObject.transform;
                        mState.GroundPosition = mState.Ground.position;
                        mState.GroundRotation = mState.Ground.rotation;

                        mState.GroundSurfaceAngle = Vector3.Angle(lRaycastHit.normal, rActorUp);
                        mState.GroundSurfaceNormal = lRaycastHit.normal;
                        mState.GroundSurfaceDistance = lDistance;
                        mState.GroundSurfacePoint = lClosestPoint;
                        mState.GroundSurfaceDirection = lRayDirection;
                        mState.IsGroundSurfaceDirect = false;

                        Vector3 lForwardSlopeDirection = _Transform.forward;
                        Vector3 lGroundNormal = mState.GroundSurfaceNormal;
                        Vector3.OrthoNormalize(ref lGroundNormal, ref lForwardSlopeDirection);
                        mState.GroundSurfaceForwardAngle = -Vector3Ext.SignedAngle(_Transform.forward, lForwardSlopeDirection, _Transform.right);

                        if (_ShowDebug)
                        {
                            Utilities.Debug.Log.FileScreenWrite("multip hit grounding l-y:" + lLocalOrbitPoint.y.ToString("f3") + " angle:" + mState.GroundSurfaceAngle.ToString("f3") + " nml:" + Helpers.StringHelper.ToString(mState.GroundSurfaceNormal), 6);
                        }

                        lHitInfo = lRaycastHit;
                    }
                }
            }

            return lDistance;
        }

        /// <summary>
        /// Grab the acceleration to use in our movement
        /// </summary>
        /// <param name="rDeltaTime">Delta time to be used with the forces</param>
        /// <returns>The sum of our forces</returns>
        protected Vector3 ProcessForces(float rDeltaTime)
        {
            Vector3 lAcceleration = Vector3.zero;

            // Apply each force
            if (mAppliedForces != null)
            {
                for (int i = mAppliedForces.Count - 1; i >= 0; i--)
                {
                    Force lForce = mAppliedForces[i];
                    if (lForce.StartTime == 0f) { lForce.StartTime = Time.time; }

                    // If the force is no longer valid, remove it
                    if (lForce.Value.sqrMagnitude == 0f)
                    {
                        mAppliedForces.RemoveAt(i);
                        Force.Release(lForce);
                    }
                    // If the force has started, look to apply it
                    else if (lForce.StartTime <= Time.time)
                    {
                        // For an impulse, apply it and remove it
                        if (lForce.Type == ForceMode.Impulse)
                        {
                            lAcceleration += (lForce.Value / _Mass);

                            mAppliedForces.RemoveAt(i);
                            Force.Release(lForce);
                        }
                        // Determine if the force has expired
                        else if (lForce.Duration > 0f && lForce.StartTime + lForce.Duration < Time.time)
                        {
                            mAppliedForces.RemoveAt(i);
                            Force.Release(lForce);
                        }
                        // Since it hasn't expired, apply it
                        else
                        {
                            lAcceleration += (lForce.Value / _Mass);
                        }
                    }
                }
            }

            return lAcceleration;
        }

        /// <summary>
        /// If we're on an object, we need to use it's position and rotation as a basis for our actor
        /// </summary>
        /// <param name="rActorPosition">Actor's current position</param>
        /// <param name="rActorUp">Actor's up vector</param>
        /// <param name="rMovement">Desired movement</param>
        /// <param name="rState">Current state</param>
        /// <param name="rPrevState">Previous state</param>
        protected void ProcessPlatforming(ActorState rPrevState)
        {
            Transform lGround = rPrevState.Ground;
            bool lIsGrounded = rPrevState.IsGrounded;

            if (mTargetGround != null)
            {
                lIsGrounded = true;
                lGround = mTargetGround;
            }

            if (lGround == null) { return; }

            Vector3 lGroundMove = Vector3.zero;
            Quaternion lGroundRotate = Quaternion.identity;

            // If we're still not grounded an there was not adjusted move, we must simply need to stop
            if (lIsGrounded)
            {
                Quaternion lSwing = Quaternion.identity;
                Quaternion lTwist = Quaternion.identity;

                if (lGround != null && lGround == rPrevState.PrevGround)
                {
                    // Rotation change
                    Matrix4x4 lOldMatrix = Matrix4x4.TRS(mPrevState.GroundPosition, mPrevState.GroundRotation, lGround.lossyScale);
                    Matrix4x4 lNewMatrix = Matrix4x4.TRS(lGround.position, lGround.rotation, lGround.lossyScale);

                    if (lOldMatrix != lNewMatrix)
                    {
                        Vector3 lForward = lOldMatrix.inverse.MultiplyVector(_Transform.forward);
                        lForward = lNewMatrix.MultiplyVector(lForward);

                        Vector3 lUp = lOldMatrix.inverse.MultiplyVector(_Transform.up);
                        lUp = lNewMatrix.MultiplyVector(lUp);

                        Quaternion lRotation = Quaternion.LookRotation(lForward, lUp);
                        lRotation.DecomposeSwingTwist(lUp, ref lSwing, ref lTwist);
                        mYaw = lTwist;
                        mTilt = lSwing;

                        // If we're not orienting to the ground, we need to undo any tilting
                        if (!_OrientToGround)
                        {
                            mTilt = QuaternionExt.FromToRotation(lUp, mWorldUp) * mTilt;
                        }

                        // Position change
                        Vector3 lPosition = lOldMatrix.inverse.MultiplyPoint(_Transform.position);
                        lPosition = lNewMatrix.MultiplyPoint(lPosition);

                        lGroundMove = (lPosition - _Transform.position);
                    }
                }

                mState.RotationPlatformAdjust = lGroundRotate;
                mState.MovementPlatformAdjust = lGroundMove;
            }
        }

        /// <summary>
        /// Grabs the body shape associated with the name
        /// </summary>
        /// <param name="rName"></param>
        /// <returns></returns>
        public BodyShape GetBodyShape(string rName)
        {
            for (int i = 0; i < BodyShapes.Count; i++)
            {
                if (BodyShapes[i].Name == rName)
                {
                    return BodyShapes[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Adds a body shape to the list
        /// </summary>
        /// <param name="rBodyShape">Body shape to add</param>
        public void AddBodyShape(BodyShape rBodyShape)
        {
            if (BodyShapes.Contains(rBodyShape)) { return; }

            rBodyShape._CharacterController = this;
            rBodyShape._Parent = _Transform;
            BodyShapes.Add(rBodyShape);

            SerializeBodyShapes();
        }

        /// <summary>
        /// Removes a body shape
        /// </summary>
        /// <param name="rBodyShape">Body shape to remove</param>
        public void RemoveBodyShape(BodyShape rBodyShape)
        {
            if (rBodyShape == null) { return; }

            if (BodyShapes.Contains(rBodyShape))
            {
                rBodyShape.DestroyUnityColliders();
                BodyShapes.Remove(rBodyShape);
            }

            SerializeBodyShapes();
        }

        /// <summary>
        /// Removes a body shape based on the name
        /// </summary>
        /// <param name="rName">Name of the body shape to remove</param>
        public void RemoveBodyShape(string rName)
        {
            for (int i = BodyShapes.Count - 1; i >= 0; i--)
            {
                if (BodyShapes[i].Name == rName)
                {
                    BodyShapes[i].DestroyUnityColliders();
                    BodyShapes.RemoveAt(i);
                }
            }

            SerializeBodyShapes();
        }

        /// <summary>
        /// Removes a body shape based on the name
        /// </summary>
        /// <param name="rName">Name of the body shape to remove</param>
        public void RemoveBodyShapes()
        {
            for (int i = BodyShapes.Count - 1; i >= 0; i--)
            {
                BodyShapes[i].DestroyUnityColliders();
            }

            BodyShapes.Clear();
            mBodyShapeDefinitions.Clear();
        }

        /// <summary>
        /// Processes the shapes and store thier definitions so we can deserialize later
        /// </summary>
        public void SerializeBodyShapes()
        {
            mBodyShapeDefinitions.Clear();

            for (int i = 0; i < BodyShapes.Count; i++)
            {
                string lDefinition = BodyShapes[i].Serialize();
                mBodyShapeDefinitions.Add(lDefinition);
            }
        }

        /// <summary>
        /// Processes the definitions and updates the shapes to match.
        /// </summary>
        public void DeserializeBodyShapes()
        {
            int lBodyShapeCount = BodyShapes.Count;
            int lBodyShapeDefCount = mBodyShapeDefinitions.Count;

            // First, remove any extra motions that may exist
            for (int i = lBodyShapeCount - 1; i > lBodyShapeDefCount; i--)
            {
                BodyShapes.RemoveAt(i);
            }

            // We need to match the motion definitions to the motions
            for (int i = 0; i < lBodyShapeDefCount; i++)
            {
                string lDefinition = mBodyShapeDefinitions[i];
                JSONNode lDefinitionNode = JSONNode.Parse(lDefinition);
                if (lDefinitionNode == null) { continue; }

                BodyShape lBodyShape = null;
                string lTypeString = lDefinitionNode["Type"].Value;

                Type lType = Type.GetType(lTypeString);
                if (lType == null) { continue; }

                // If don't have a motion matching the type, we need to create one
                if (BodyShapes.Count <= i || lTypeString != BodyShapes[i].GetType().AssemblyQualifiedName)
                {
                    lBodyShape = Activator.CreateInstance(lType) as BodyShape;
                    if (BodyShapes.Count <= i)
                    {
                        BodyShapes.Add(lBodyShape);
                    }
                    else
                    {
                        BodyShapes[i] = lBodyShape;
                    }
                }
                // Grab the matching motion
                else
                {
                    lBodyShape = BodyShapes[i];
                }

                // Fill the motion with data from the definition
                if (lBodyShape != null)
                {
                    lBodyShape._Parent = transform;
                    lBodyShape._CharacterController = this;
                    lBodyShape.Deserialize(lDefinition);
                }
            }
        }

        // **************************************************************************************************
        // Following properties and function only valid while editing
        // **************************************************************************************************

        // Stores the index in our list
        public int EditorBodyShapeIndex = 0;

        public bool EditorShowAdvanced = false;

        public bool EditorCollideWithObjects = true;

        public bool EditorWalkOnWalls = false;

        public bool EditorSlideOnSlopes = false;

        public bool EditorRespondToColliders = false;


#if UNITY_EDITOR

        /// <summary>
        /// Allows us to draw to the editor
        /// </summary>
        public void OnSceneGUI()
        {
            Color lHandleColor = Handles.color;

            if (_Transform == null) { _Transform = transform; }

            Handles.color = new Color(0.5f, 0.5f, 0.5f, 0.25f);

            Vector3 lPosition = _Transform.position + (_Transform.rotation * _OverlapCenter);
            Handles.DrawWireArc(lPosition, _Transform.forward, _Transform.up, 360f, _OverlapRadius);
            Handles.DrawWireArc(lPosition, _Transform.up, _Transform.forward, 360f, _OverlapRadius);
            Handles.DrawWireArc(lPosition, _Transform.right, _Transform.up, 360f, _OverlapRadius);

            Handles.color = new Color(0f, 1f, 0f, 0.5f);

            for (int i = 0; i < BodyShapes.Count; i++)
            {
                if (BodyShapes[i]._Parent == null)
                {
                    BodyShapes[i]._Parent = _Transform;
                }

                if (BodyShapes[i] is BodySphere)
                {
                    BodySphere lShape = BodyShapes[i] as BodySphere;

                    float lRadius = lShape._Radius;
                    Transform lTransform = (lShape._Transform != null ? lShape._Transform : _Transform);
                    lPosition = lTransform.position + (lTransform.rotation * lShape._Offset);

                    Handles.DrawWireArc(lPosition, lTransform.forward, lTransform.up, 360f, lRadius);
                    Handles.DrawWireArc(lPosition, lTransform.up, lTransform.forward, 360f, lRadius);
                    Handles.DrawWireArc(lPosition, lTransform.right, lTransform.up, 360f, lRadius);

#if UNITY_4 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4 || UNITY_5_5
                    Handles.SphereCap(0, lPosition, Quaternion.identity, 0.05f);
#else
                    Handles.SphereHandleCap(0, lPosition, Quaternion.identity, 0.05f, EventType.Layout | EventType.Repaint);
#endif
                }
                else if (BodyShapes[i] is BodyCapsule)
                {
                    BodyCapsule lShape = BodyShapes[i] as BodyCapsule;

                    float lRadius = lShape._Radius;

                    lPosition = (lShape._Transform == null ? lShape._Parent.position + (lShape._Parent.rotation * lShape._Offset) : lShape._Transform.position + (lShape._Transform.rotation * lShape._Offset));

                    Vector3 lEndPosition = (lShape._EndTransform == null ? lShape._Parent.position + (lShape._Parent.rotation * lShape._EndOffset) : lShape._EndTransform.position + (lShape._EndTransform.rotation * lShape._EndOffset));

                    //Transform lTransform = (lShape._Transform != null ? lShape._Transform : _Transform);
                    //Vector3 lPosition = lTransform.position + (lTransform.rotation * lShape._Offset);

                    //Transform lEndTransform = (lShape._EndTransform != null ? lShape._EndTransform : _Transform);
                    //Vector3 lEndPosition = lEndTransform.position + (lEndTransform.rotation * lShape._EndOffset);

                    Vector3 lDirection = (lEndPosition - lPosition).normalized;
                    Quaternion lRotation = (lDirection.sqrMagnitude == 0f ? Quaternion.identity : Quaternion.LookRotation(lDirection, _Transform.up));

                    Vector3 lForward = lRotation * Vector3.forward;
                    Vector3 lRight = lRotation * Vector3.right;
                    Vector3 lUp = lRotation * Vector3.up;

                    Handles.DrawWireArc(lPosition, lForward, lUp, 360f, lRadius);
                    Handles.DrawWireArc(lPosition, lUp, lRight, 180f, lRadius);
                    Handles.DrawWireArc(lPosition, lRight, -lUp, 180f, lRadius);

                    Handles.DrawWireArc(lEndPosition, lForward, lUp, 360f, lRadius);
                    Handles.DrawWireArc(lEndPosition, lUp, -lRight, 180f, lRadius);
                    Handles.DrawWireArc(lEndPosition, lRight, lUp, 180f, lRadius);

                    Handles.DrawLine(lPosition + (lRight * lRadius), lEndPosition + (lRight * lRadius));
                    Handles.DrawLine(lPosition + (-lRight * lRadius), lEndPosition + (-lRight * lRadius));
                    Handles.DrawLine(lPosition + (lUp * lRadius), lEndPosition + (lUp * lRadius));
                    Handles.DrawLine(lPosition + (-lUp * lRadius), lEndPosition + (-lUp * lRadius));

#if UNITY_4 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4 || UNITY_5_5
                    Handles.SphereCap(0, lPosition, Quaternion.identity, 0.025f);
                    Handles.SphereCap(0, lEndPosition, Quaternion.identity, 0.025f);
#else
                    Handles.SphereHandleCap(0, lPosition, Quaternion.identity, 0.025f, EventType.Layout | EventType.Repaint);
                    Handles.SphereHandleCap(0, lEndPosition, Quaternion.identity, 0.025f, EventType.Layout | EventType.Repaint);
#endif
                }
            }

            if (mState.IsColliding)
            {
                Handles.color = Color.red;
#if UNITY_4 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4 || UNITY_5_5
                Handles.SphereCap(0, mState.ColliderHit.point, Quaternion.identity, 0.025f);
#else
                Handles.SphereHandleCap(0, mState.ColliderHit.point, Quaternion.identity, 0.025f, EventType.Layout | EventType.Repaint);
#endif
                Handles.DrawLine(mState.ColliderHit.point, mState.ColliderHit.point + mState.ColliderHit.normal);

                Handles.color = Color.magenta;
#if UNITY_4 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4 || UNITY_5_5
                Handles.SphereCap(0, mState.ColliderHitOrigin, Quaternion.identity, 0.025f);
#else
                Handles.SphereHandleCap(0, mState.ColliderHitOrigin, Quaternion.identity, 0.025f, EventType.Layout | EventType.Repaint);
#endif
            }

            Handles.color = lHandleColor;
        }

#endif
    }
}
