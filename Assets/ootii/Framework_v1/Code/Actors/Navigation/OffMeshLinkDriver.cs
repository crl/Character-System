using UnityEngine;
using System.Collections;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Helpers;

#if !(UNITY_4 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4)
using UnityEngine.AI;
#endif

namespace com.ootii.Actors.Navigation
{
    /// <summary>
    /// Defines different ways to move on the offmesh link
    /// </summary>
    public enum OffMeshLinkMoveType
    {
        AutoDetect,
        Teleport,
        Linear,
        Parabola,
        Curve,
        Drop,
        ClimbOnto
    }

    /// <summary>
    /// Component that is added temporarily to travel across Nav Mesh Off-Mesh Links. This
    /// allows us to travel in lots of different ways.
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class OffMeshLinkDriver : MonoBehaviour
    {
        /// <summary>
        /// Nav Mesh Agent we're dealing with
        /// </summary>
        protected NavMeshAgent mNavMeshAgent = null;
        public NavMeshAgent NavMeshAgent
        {
            get { return mNavMeshAgent; }            
            set { mNavMeshAgent = value; }
        }

        /// <summary>
        /// Movement type to use
        /// </summary>
        protected OffMeshLinkMoveType mMoveType = OffMeshLinkMoveType.AutoDetect;
        public OffMeshLinkMoveType MoveType
        {
            get { return mMoveType; }
            set { mMoveType = value; }
        }

        /// <summary>
        /// Speed of the movement
        /// </summary>
        protected float mSpeed = 0f;
        public float Speed
        {
            get { return mSpeed; }
            set { mSpeed = value; }
        }

        /// <summary>
        /// Height when using  parabola
        /// </summary>
        protected float mHeight = 1f;
        public float Height
        {
            get { return mHeight; }
            set { mHeight = value; }
        }

        /// <summary>
        /// Curve we can use with the movement
        /// </summary>
        protected AnimationCurve mCurve = null;
        public AnimationCurve Curve
        {
            get { return mCurve; }
            set { mCurve = value; }
        }

        /// <summary>
        /// Determines if the mover has finished
        /// </summary>
        protected bool mHasCompleted = false;
        public bool HasCompleted
        {
            get { return mHasCompleted; }
        }

        /// <summary>
        /// Information specific to this off-mesh link
        /// </summary>
        protected OffMeshLinkData mOffMeshLinkData;

        /// <summary>
        /// Motion Controller tied to this actor
        /// </summary>
        protected MotionController mMotionController = null;

        /// <summary>
        /// Start position of the actor when this began
        /// </summary>
        protected Vector3 mStartPosition = Vector3.zero;

        /// <summary>
        /// Start is called on the frame when a script is enabled just before any of the Update methods is called the first time.
        /// </summary>
        /// <returns>IEnumerator for our coroutine</returns>
        public IEnumerator Start()
        {
            mMotionController = gameObject.GetComponent<MotionController>();

            mNavMeshAgent = gameObject.GetComponent<NavMeshAgent>();
            mNavMeshAgent.autoTraverseOffMeshLink = false;

            mOffMeshLinkData = mNavMeshAgent.currentOffMeshLinkData;

            mHasCompleted = false;
            mStartPosition = mMotionController._Transform.position;
            
            // Determine our speed
            float lSpeed = (mSpeed > 0f ? mSpeed : mNavMeshAgent.speed);

            // Only process while we're on an off-mesh link
            if (mNavMeshAgent.isOnOffMeshLink)
            {
                // Determine the movement style to use
                if (mMoveType == OffMeshLinkMoveType.AutoDetect)
                {
                    // If the link handles a 'drop-down', select the mover type
                    if (mOffMeshLinkData.offMeshLink != null)
                    {
                        // If the link handles a 'climb up', select the mover type
                        if (mOffMeshLinkData.offMeshLink.area == NavMesh.GetAreaFromName("Climb Onto"))
                        {
                            MoveType = OffMeshLinkMoveType.ClimbOnto;
                        }
                    }
                    // Change the type if we're on a drop-down
                    else if (mOffMeshLinkData.linkType == OffMeshLinkType.LinkTypeDropDown)
                    {
                        MoveType = OffMeshLinkMoveType.Drop;
                    }

                    // Default to something usable
                    if (mMoveType == OffMeshLinkMoveType.AutoDetect) { mMoveType = OffMeshLinkMoveType.Parabola; }
                }

                // Process based on the type
                if (mMoveType == OffMeshLinkMoveType.Teleport)
                {
                    mNavMeshAgent.Warp(mOffMeshLinkData.endPos);
                }
                else if (mMoveType == OffMeshLinkMoveType.Linear)
                {
                    yield return mMotionController.StartCoroutine(MoveLinear(lSpeed));
                }
                else if (mMoveType == OffMeshLinkMoveType.Parabola)
                {
                    yield return mMotionController.StartCoroutine(MoveParabola(mHeight, lSpeed));
                }
                else if (mMoveType == OffMeshLinkMoveType.Curve)
                {
                    yield return mMotionController.StartCoroutine(MoveCurve(lSpeed));
                }
                else if (mMoveType == OffMeshLinkMoveType.Drop)
                {
                    yield return mMotionController.StartCoroutine(MoveDrop(lSpeed));
                }
                else if (mMoveType == OffMeshLinkMoveType.ClimbOnto)
                {
                    yield return mMotionController.StartCoroutine(ClimbUp(lSpeed));
                }

                // We're finally done
                mNavMeshAgent.CompleteOffMeshLink();
            }

            // Flag the move as complete
            mHasCompleted = true;
        }

        /// <summary>
        /// Moves the agent to the link in a straight line
        /// </summary>
        /// <param name="speed">Speed at which we'll move</param>
        protected IEnumerator MoveLinear(float rSpeed)
        {
            Vector3 lEndPos = mOffMeshLinkData.endPos + Vector3.up * mNavMeshAgent.baseOffset;
            while (mNavMeshAgent.transform.position != lEndPos)
            {
                mNavMeshAgent.transform.position = Vector3.MoveTowards(mNavMeshAgent.transform.position, lEndPos, rSpeed * Time.deltaTime);
                yield return null;
            }
        }

        /// <summary>
        /// Moves across the off-mesh line as a parabola.
        /// </summary>
        /// <param name="rHeight">Height of the move</param>
        /// <param name="speed">Speed at which we'll move</param>
        /// <returns></returns>
        protected IEnumerator MoveParabola(float rHeight, float rSpeed)
        {
            // First, move to the nav mesh start position
            Vector3 lTargetPosition = mOffMeshLinkData.startPos + (Vector3.up * mNavMeshAgent.baseOffset);
            while (mMotionController._Transform.position != lTargetPosition)
            {
                mMotionController._Transform.position = Vector3.MoveTowards(mMotionController._Transform.position, lTargetPosition, rSpeed * Time.deltaTime);
                yield return null;
            }

            // Start moving along the arc
            float lDuration = Vector3.Distance(mOffMeshLinkData.startPos, mOffMeshLinkData.endPos) / rSpeed;

            Vector3 lStartPos = mNavMeshAgent.transform.position;
            Vector3 lEndPos = mOffMeshLinkData.endPos + (Vector3.up * mNavMeshAgent.baseOffset);

            float lNormalizedTime = 0.0f;
            while (lNormalizedTime < 1.0f)
            {
                float lYOffset = rHeight * 4.0f * (lNormalizedTime - lNormalizedTime * lNormalizedTime);
                mNavMeshAgent.transform.position = Vector3.Lerp(lStartPos, lEndPos, lNormalizedTime) + (Vector3.up * lYOffset);

                lNormalizedTime = lNormalizedTime + (Time.deltaTime / lDuration);
                yield return null;
            }
        }

        /// <summary>
        /// Moves across the off-mesh line following a curve.
        /// </summary>
        /// <param name="speed">Speed at which we'll move</param>
        protected IEnumerator MoveCurve(float rSpeed)
        {
            float lDuration = Vector3.Distance(mOffMeshLinkData.startPos, mOffMeshLinkData.endPos) / rSpeed;

            Vector3 lStartPos = mNavMeshAgent.transform.position;
            Vector3 lEndPos = mOffMeshLinkData.endPos + (Vector3.up * mNavMeshAgent.baseOffset);

            float lNormalizedTime = 0.0f;
            while (lNormalizedTime < 1.0f)
            {
                float lYOffset = (mCurve != null ? mCurve.Evaluate(lNormalizedTime) : 0f);
                mNavMeshAgent.transform.position = Vector3.Lerp(lStartPos, lEndPos, lNormalizedTime) + (Vector3.up * lYOffset);

                lNormalizedTime = lNormalizedTime + (Time.deltaTime / lDuration);
                yield return null;
            }
        }

        /// <summary>
        /// Drop of a height, but disable 
        /// </summary>
        /// <param name="rHeight">Height of the move</param>
        /// <param name="speed">Speed at which we'll move</param>
        /// <returns></returns>
        protected IEnumerator MoveDrop(float rSpeed)
        {
            // First, move to the nav mesh start position
            Vector3 lTargetPosition = mOffMeshLinkData.startPos + (Vector3.up * mNavMeshAgent.baseOffset);
            while (mMotionController._Transform.position != lTargetPosition)
            {
                mMotionController._Transform.position = Vector3.MoveTowards(mMotionController._Transform.position, lTargetPosition, rSpeed * Time.deltaTime);
                yield return null;
            }

            // Determine how long we'll drop for
            float lFallStart = 0f;
            float lDuration = Vector3.Distance(mOffMeshLinkData.startPos, mOffMeshLinkData.endPos) / rSpeed;

            Vector3 lStartPos = mNavMeshAgent.transform.position; 
            Vector3 lEndPos = mOffMeshLinkData.endPos + (Vector3.up * mNavMeshAgent.baseOffset);

            float lNormalizedTime = 0.0f;
            while (lNormalizedTime < 1.0f)
            {
                // Determine if we're actually falling now
                if (lFallStart == 0f && !mMotionController.IsGrounded) { lFallStart = lNormalizedTime; }

                // Use the parabola to give us time to clear the edge
                Vector3 lNewPosition = Vector3.Lerp(lStartPos, lEndPos, lNormalizedTime);

                // Remove any vertical motion while we're grounded
                float lPercent = 1f - (lFallStart == 0f ? 0f : Mathf.Clamp01((lNormalizedTime - lFallStart) / (1f - lFallStart)));
                lNewPosition = lNewPosition + (Vector3.Project(mStartPosition - lNewPosition, mMotionController._Transform.up) * lPercent);

                // Finally, set the position
                mNavMeshAgent.transform.position = lNewPosition;

                lNormalizedTime = lNormalizedTime + (Time.deltaTime / lDuration);
                yield return null;
            }
        }

        /// <summary>
        /// Moves the agent to the link in a straight line
        /// </summary>
        /// <param name="speed">Speed at which we'll move</param>
        protected IEnumerator ClimbUp(float rSpeed)
        {
            bool lStoredUpdatePosition = mNavMeshAgent.updatePosition;
            bool lStoredUpdateRotation = mNavMeshAgent.updateRotation;

            mNavMeshAgent.updatePosition = false;
            mNavMeshAgent.updateRotation = false;

            // First, move to the nav mesh start position
            Vector3 lTargetPosition = mOffMeshLinkData.startPos + (Vector3.up * mNavMeshAgent.baseOffset);
            while (mMotionController._Transform.position != lTargetPosition)
            {
                mMotionController._Transform.position = Vector3.MoveTowards(mMotionController._Transform.position, lTargetPosition, rSpeed * Time.deltaTime);

                float lAngle = NumberHelper.GetHorizontalAngle(mMotionController._Transform.forward, (mOffMeshLinkData.endPos - mOffMeshLinkData.startPos).normalized, mMotionController._Transform.up);
                mMotionController._Transform.rotation = mMotionController._Transform.rotation * Quaternion.AngleAxis(lAngle * 0.05f, Vector3.up);

                yield return null;
            }

            // Next, lets see if we can find a climb motion that will handle our movement
            NavigationMessage lMessage = NavigationMessage.Allocate();
            lMessage.ID = NavigationMessage.MSG_NAVIGATE_CLIMB;

            mMotionController.SendMessage(lMessage);

            if (lMessage.IsHandled)
            {
                // Wait for the climb to finish
                mMotionController.ActorController.UseTransformPosition = false;

                MotionControllerMotion lMotion = lMessage.Recipient as MotionControllerMotion;
                while (lMotion != null && (lMotion.QueueActivation || lMotion.IsActive))
                {
                    yield return null;
                }

                mMotionController.ActorController.UseTransformPosition = true;

                // Finally, move to the nav mesh end position
                lTargetPosition = mOffMeshLinkData.endPos + (Vector3.up * mNavMeshAgent.baseOffset);
                while (mMotionController._Transform.position != lTargetPosition)
                {
                    mMotionController._Transform.position = Vector3.MoveTowards(mMotionController._Transform.position, lTargetPosition, rSpeed * Time.deltaTime);
                    yield return null;
                }
            }
            else
            {
                yield return mMotionController.StartCoroutine(MoveParabola(mHeight, rSpeed));
            }

            mNavMeshAgent.Warp(mMotionController._Transform.position);
            mNavMeshAgent.updatePosition = lStoredUpdatePosition;
            mNavMeshAgent.updateRotation = lStoredUpdateRotation;
        }
    }
}

