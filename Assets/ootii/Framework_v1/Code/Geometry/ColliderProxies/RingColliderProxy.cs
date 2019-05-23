using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.ootii.Geometry
{
    /// <summary>
    /// Proxy collider that creates a ring of collision volumes
    /// </summary>
    public class RingColliderProxy : ColliderProxy
    {
        /// <summary>
        /// Segments that make up the ring.
        /// </summary>
        public int _Segments = 16;
        public int Segments
        {
            get { return _Segments; }
            set { _Segments = value; }
        }

        /// <summary>
        /// Thickness of the ring wall.
        /// </summary>
        public float _Thickness = 0.5f;
        public float Thickness
        {
            get { return _Thickness; }
            set { _Thickness = value; }
        }

        /// <summary>
        /// Normal of the ring (typically up).
        /// </summary>
        public Vector3 _Normal = Vector3.up;
        public Vector3 Normal
        {
            get { return _Normal; }
            set { _Normal = value; }
        }

        /// <summary>
        /// Starting direction of the ring (typically forward).
        /// </summary>
        public Vector3 _Forward = Vector3.forward;
        public Vector3 Forward
        {
            get { return _Forward; }
            set { _Forward = value; }
        }

        /// <summary>
        /// Speed of the enabling as degrees per second
        /// </summary>
        public float _Speed = 0f;
        public float Speed
        {
            get { return _Speed; }
            set { _Speed = value; }
        }

        // Angle used to define a single segment
        protected float mSegmentAngle = 0f;

        // Determine if the colliders will be enabled or not
        protected bool mEnable = true;

        // Determines if we're actively updating
        protected bool mIsUpdating = true;

        // Elapsed angle (0 to 360) over the time we're enabling
        protected float mElapsedAngle = 0f;

        // Track the colliders we created
        protected List<Collider> mColliders = new List<Collider>();

        /// <summary>
        /// Runs before any Start or Update functions are run
        /// </summary>
        protected override void Awake()
        {
            base.Awake();
        }

        /// <summary>
        /// Runs before any Update functions are run
        /// </summary>
        protected void Start()
        {
            mSegmentAngle = 360f / ((float)_Segments);

            Vector3 lScale = gameObject.transform.lossyScale;

            float lSurfaceLength = 1.1f * (mSegmentAngle * Mathf.Deg2Rad);
            float lThickness = _Thickness / lScale.x;

            Vector3 lSurfacePoint = _Forward;
            Quaternion lRotation = Quaternion.AngleAxis(mSegmentAngle, _Normal);

            // Create the template collider
            GameObject lChildTemplate = new GameObject("collider", new Type[] { typeof(BoxCollider), typeof(ColliderProxy) } );

            BoxCollider lBoxCollider = lChildTemplate.GetComponent<BoxCollider>();
            lBoxCollider.enabled = (_Speed == 0f);
            lBoxCollider.isTrigger = true;

            ColliderProxy lColliderProxy = lChildTemplate.GetComponent<ColliderProxy>();
            lColliderProxy.Target = _Target;

            // Create the child colliders
            GameObject lChild = lChildTemplate;
            for (int i = 0; i < _Segments; i++)
            {
                lSurfacePoint = lRotation * lSurfacePoint;

                if (lChild == null) { lChild = GameObject.Instantiate(lChildTemplate); }

                lChild.transform.parent = transform;
                lChild.transform.localScale = new Vector3(lSurfaceLength, 1f, lThickness);
                lChild.transform.localPosition = lSurfacePoint;
                lChild.transform.forward = (lChild.transform.position - gameObject.transform.position).normalized;

                mColliders.Add(lChild.GetComponent<BoxCollider>());

                lChild = null;
            }

            // Determine if we're updating
            mEnable = true;
            mIsUpdating = (_Speed != 0f);
        }

        /// <summary>
        /// Allows us to reset the proxy
        /// </summary>
        public override void Reset()
        {
            EnableColliders(false);
            EnableColliders(true, _Speed);
        }

        /// <summary>
        /// Enables/disables the colliders based on the speed
        /// </summary>
        public override void EnableColliders(bool rEnable, float rSpeed = 0f)
        {
            if (rSpeed == 0f)
            {
                for (int i = 0; i < mColliders.Count; i++)
                {
                    mColliders[i].enabled = rEnable;
                }

                mIsUpdating = false;
            }
            else
            {
                _Speed = rSpeed;

                mEnable = rEnable;
                mIsUpdating = true;
                mElapsedAngle = 0f;
            }
        }

        /// <summary>
        /// Called each frame to enable and disable the ring
        /// </summary>
        protected void Update()
        {
            if (!mIsUpdating) { return; }

            float lDirection = Mathf.Sign(_Speed);
            float lSpeed = Mathf.Abs(_Speed);

            mElapsedAngle = mElapsedAngle + (lSpeed * Time.deltaTime);

            // Process clockwise
            if (lDirection > 0f)
            {
                float lSegment = Mathf.Floor(mElapsedAngle / mSegmentAngle);
                for (int i = 0; i <= lSegment && i < mColliders.Count; i++)
                {
                    if (mColliders[i].enabled != mEnable) { mColliders[i].enabled = mEnable; }
                }

                if (lSegment >= mColliders.Count) { mIsUpdating = false; }
            }
            // Process counter-clockwise
            else if (lDirection < 0f)
            {
                float lSegment = Mathf.Floor((360f - mElapsedAngle) / mSegmentAngle);
                for (int i = (int)lSegment; i < mColliders.Count; i++)
                {
                    if (mColliders[i].enabled != mEnable) { mColliders[i].enabled = mEnable; }
                }

                if (lSegment <= 0) { mIsUpdating = false; }
            }
            // Stop processing
            else
            {
                mIsUpdating = false;
            }            
        }
    }
}
