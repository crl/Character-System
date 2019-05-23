using UnityEngine;
using com.ootii.Actors.LifeCores;
using com.ootii.Geometry;
using com.ootii.Helpers;
using com.ootii.Messages;

namespace com.ootii.Demos
{
    /// <summary>
    /// Simple class for managing a door
    /// </summary>
    public class demo_OpenCore : MonoBehaviour
    {
        /// <summary>
        /// Determines if the door is currently opened or closed
        /// </summary>
        public bool _IsOpen = false;
        public bool IsOpen
        {
            get { return _IsOpen; }
        }

        /// <summary>
        /// Determines if we disable the interactable when activated
        /// </summary>
        public bool _DisableOnActivate = false;
        public bool DisableOnActivate
        {
            get { return _DisableOnActivate; }
            set { _DisableOnActivate = value; }
        }

        /// <summary>
        /// Axis around which we'll open
        /// </summary>
        public Vector3 _OpenAxis = Vector3.up;
        public Vector3 OpenAxis
        {
            get { return _OpenAxis; }
            set { _OpenAxis = value; }
        }

        /// <summary>
        /// The angle that represents fully open
        /// </summary>
        public float _OpenAngle = 90f;
        public float OpenAngle
        {
            get { return _OpenAngle; }
            set { _OpenAngle = value; }
        }

        /// <summary>
        /// Speed at which we open and close
        /// </summary>
        public float _OpenSpeed = 0.5f;
        public float OpenSpeed
        {
            get { return _OpenSpeed; }
            set { _OpenSpeed = value; }
        }

        /// <summary>
        /// Determines if we're actively opening or closing
        /// </summary>
        protected bool mIsActive = false;

        /// <summary>
        /// Determines the progress of the open/close
        /// </summary>
        protected float mProgress = 0f;

        /// <summary>
        /// Used for the callbacks
        /// </summary>
        protected InteractableCore mInteractableCore = null;

        /// <summary>
        /// Opens the door
        /// </summary>
        public void Open()
        {
            _IsOpen = true;
            mIsActive = true;
            mProgress = 0f;
        }

        /// <summary>
        /// Closes the door
        /// </summary>
        public void Close()
        {
            _IsOpen = false;
            mIsActive = true;
            mProgress = 0f;
        }

        /// <summary>
        /// Update is called once per frame
        /// </summary>
        protected virtual void Update()
        {
            if (!mIsActive) { return; }

            mProgress = mProgress + (Time.deltaTime / _OpenSpeed);

            //Quaternion lSwing = Quaternion.identity;
            //Quaternion lTwist = Quaternion.identity;
            //transform.rotation.DecomposeSwingTwist(_OpenAxis, ref lSwing, ref lTwist);

            float lAngle = 0f; // lTwist.eulerAngles.y;

            if (_IsOpen)
            {
                lAngle = _OpenAngle * NumberHelper.SmoothStep(0f, 1f, mProgress);
                transform.localRotation = Quaternion.AngleAxis(lAngle, _OpenAxis);
            }
            else
            {
                lAngle = _OpenAngle * NumberHelper.SmoothStep(1f, 0f, mProgress);
                transform.localRotation = Quaternion.AngleAxis(lAngle, _OpenAxis);
            }

            // Determine if we're done
            if (mProgress >= 1f)
            {
                mIsActive = false;

                if (mInteractableCore != null)
                {
                    mInteractableCore.OnActivatedCompleted();
                }

                mInteractableCore = null;
            }
        }

        /// <summary>
        /// Event handler for the interactable
        /// </summary>
        /// <param name="rMessage"></param>
        public void OnInteractableActivated(IMessage rMessage)
        {
            if (mIsActive) { return; }

            _IsOpen = !_IsOpen;
            mIsActive = true;
            mProgress = 0f;

            if (rMessage.Data is GameObject)
            {
                mInteractableCore = ((GameObject)rMessage.Data).GetComponent<InteractableCore>();
                if (_DisableOnActivate) { mInteractableCore.IsEnabled = false; }
            }
        }
    }
}
