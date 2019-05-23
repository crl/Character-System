using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Geometry;
using com.ootii.Messages;

namespace com.ootii.Actors.LifeCores
{
    /// <summary>
    /// Provides a simple foundation for interactable items. This would include things
    /// like doors, chests, pick-up items, etc.
    /// </summary>
    public partial class InteractableCore : MonoBehaviour, IInteractableCore
    {
        // Color names that could be used in materials
        private static string[] MATERIAL_COLORS = new string[] { "_Color", "_MainColor", "_BorderColor", "_OutlineColor" };

        /// <summary>
        /// Determines if the interactable is actually interactable
        /// </summary>
        public bool _IsEnabled = true;
        public bool IsEnabled
        {
            get { return _IsEnabled; }
            set { _IsEnabled = value; }
        }

        ///// <summary>
        ///// Defines the interaction motion that we'll use
        ///// </summary>
        //public string _Motion = "BasicInteraction";
        //public string Motion
        //{
        //    get { return _Motion; }
        //    set { _Motion = value; }
        //}

        /// <summary>
        /// Defines the form that tells us which animation to use
        /// </summary>
        public int _Form = 0;
        public int Form
        {
            get { return _Form; }
            set { _Form = value; }
        }

        /// <summary>
        /// Determine if the actor is moved to the object
        /// </summary>
        public bool _ForcePosition = true;
        public bool ForcePosition
        {
            get { return _ForcePosition; }
            set { _ForcePosition = value; }
        }

        /// <summary>
        /// Determine if the actor is rotated to the object
        /// </summary>
        public bool _ForceRotation = true;
        public bool ForceRotation
        {
            get { return _ForceRotation; }
            set { _ForceRotation = value; }
        }

        /// <summary>
        /// Transform that represents the position and rotation to be in
        /// to activate the interactable.
        /// </summary>
        public Transform _TargetLocation = null;
        public Transform TargetLocation
        {
            get { return _TargetLocation; }
            set { _TargetLocation = value; }
        }

        /// <summary>
        /// Max distance used to activate the interaction
        /// </summary>
        public float _TargetDistance = 2f;
        public float TargetDistance
        {
            get { return _TargetDistance; }
            set { _TargetDistance = value; }
        }

        /// <summary>
        /// Determines if we use raycasting to activate the interactable.
        /// </summary>
        public bool _UseRaycast = true;
        public bool UseRaycast
        {
            get { return _UseRaycast; }
            set { _UseRaycast = value; }
        }

        /// <summary>
        /// Area we can use for raycast targeting
        /// </summary>
        public Collider _RaycastCollider = null;
        public Collider RaycastCollider
        {
            get { return _RaycastCollider; }
            set { _RaycastCollider = value; }
        }

        /// <summary>
        /// Area that the character enters that allows the interactable to be interacted with
        /// </summary>
        public Collider _TriggerCollider = null;
        public Collider TriggerCollider
        {
            get { return _TriggerCollider; }
            set { _TriggerCollider = value; }
        }

        /// <summary>
        /// Renderer that we'll use to highlight the interactable.
        /// </summary>
        public Renderer _HighlightRenderer = null;
        public Renderer HighlightRenderer
        {
            get { return _HighlightRenderer; }
            set { _HighlightRenderer = value; }
        }

        /// <summary>
        /// Material used to highlight the target
        /// </summary>
        public Material _HighlightMaterial = null;
        public Material HighlightMaterial
        {
            get { return _HighlightMaterial; }
            set { _HighlightMaterial = value; }
        }

        /// <summary>
        /// Color to apply to the highlight
        /// </summary>
        public Color _HighlightColor = Color.white;
        public Color HighlightColor
        {
            get { return _HighlightColor; }
            set { _HighlightColor = value; }
        }

        /// <summary>
        /// Determines if the interactable is focused on or not
        /// </summary>
        protected bool mHasFocus = false;
        public bool HasFocus
        {
            get { return mHasFocus; }
            set { mHasFocus = value; }
        }

        /// <summary>
        /// Event for when the interactiable has been triggered
        /// </summary>
        public MessageEvent ActivatedEvent = null;

        // Store the last instance created
        protected Material mMaterialInstance = null;

        // Actor that is activating the interactable
        //protected MotionController mActivator = null;

        // Current motion that is performing the interaction
        protected BasicInteraction mMotion = null;

        // Keep a list of objects in the trigger area
        protected List<Collider> mTriggeredList = null;

        /// <summary>
        /// Use this for initialization
        /// </summary>
        protected virtual void Start()
        {
            // Create a trigger proxy so this interactable can tell when the
            // character enters the area.
            if (_TriggerCollider != null)
            {
                mTriggeredList = new List<Collider>();

                ColliderProxy lProxy = _TriggerCollider.gameObject.GetComponent<ColliderProxy>();
                if (lProxy == null) { lProxy = _TriggerCollider.gameObject.AddComponent<ColliderProxy>(); }

                lProxy.Target = gameObject;
            }
        }

        /// <summary>
        /// Update is called once per frame after all the updates have been run
        /// </summary>
        protected virtual void LateUpdate()
        {
            // If the interactable doesn't have focus this frame, we need to remove any highlight
            if (!mHasFocus && mMaterialInstance != null)
            {
                StopFocus();
            }

            // Clear the focus this frame
            mHasFocus = false;
        }

        /// <summary>
        /// Determine if the activator is in position
        /// </summary>
        /// <param name="rTransform">Transform of the activator</param>
        /// <returns>Boolean that determines if the activator is in position</returns>
        public virtual bool TestActivator(Transform rActivator)
        {
            if (_TriggerCollider != null)
            {
                for (int i = mTriggeredList.Count - 1; i >= 0; i--)
                {
                    if (mTriggeredList[i] == null)
                    {
                        mTriggeredList.RemoveAt(i);
                        continue;
                    }

                    if (object.ReferenceEquals(mTriggeredList[i].transform, rActivator)) { return true; }
                }

                return false;
            }

            return true;
        }

        /// <summary>
        /// Starts the focus process for the interactable
        /// </summary>
        public virtual void StartFocus()
        {
            mHasFocus = true;

            if (mMaterialInstance == null)
            {
                AddMaterial(_HighlightRenderer, _HighlightMaterial);
            }
        }

        /// <summary>
        /// Stops the focus process for the interactable
        /// </summary>
        public virtual void StopFocus()
        {
            if (mMaterialInstance != null)
            {
                RemoveMaterial(_HighlightRenderer, mMaterialInstance);
            }

            mHasFocus = false;
            mMaterialInstance = null;
        }

        /// <summary>
        /// Raised when the motion triggers the interactable
        /// </summary>
        /// <param name="rMotion">BasicInteraction motion</param>
        public virtual void OnActivated(BasicInteraction rMotion)
        {
            mMotion = rMotion;

            if (ActivatedEvent != null)
            {
                Message lMessage = Message.Allocate();
                lMessage.ID = EnumMessageID.MSG_INTERACTION_ACTIVATE;
                lMessage.Data = this.gameObject;

                ActivatedEvent.Invoke(lMessage);

                Message.Release(lMessage);
            }

            //IsEnabled = false;

            //StopFocus();
        }

        /// <summary>
        /// Callback used when the activation may take some time
        /// </summary>
        public virtual void OnActivatedCompleted()
        {
        }

        /// <summary>
        /// Capture Unity's collision event. We use triggers since IsKinematic Rigidbodies don't
        /// raise collisions... only triggers.
        /// </summary>
        protected virtual void OnTriggerEnter(Collider rCollider)
        {
            if (rCollider == null) { return; }

            // Record the collider
            if (!mTriggeredList.Contains(rCollider))
            {
                mTriggeredList.Add(rCollider);

                // Set the interactable core on the motion if needed
                if (!_UseRaycast)
                {
                    MotionController lMC = rCollider.gameObject.GetComponent<MotionController>();
                    if (lMC != null)
                    {
                        BasicInteraction lInteractionMotion = lMC.GetMotion<BasicInteraction>();
                        if (lInteractionMotion != null && !lInteractionMotion.IsActive)
                        {
                            lInteractionMotion.InteractableCore = this;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Capture Unity's collision event
        /// </summary>
        protected virtual void OnTriggerStay(Collider rCollider)
        {
        }

        /// <summary>
        /// Capture Unity's collision event
        /// </summary>
        protected virtual void OnTriggerExit(Collider rCollider)
        {
            if (mTriggeredList.Contains(rCollider))
            {
                mTriggeredList.Remove(rCollider);

                // Clear the interactable core on the motion if needed
                if (!_UseRaycast)
                {
                    MotionController lMC = rCollider.gameObject.GetComponent<MotionController>();
                    if (lMC != null)
                    {
                        BasicInteraction lInteractionMotion = lMC.GetMotion<BasicInteraction>();
                        if (lInteractionMotion != null && !lInteractionMotion.IsActive && lInteractionMotion.Interactable == this.gameObject)
                        {
                            lInteractionMotion.InteractableCore = null;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Adds a material instance to the specified target
        /// </summary>
        /// <param name="rTarget"></param>
        protected virtual void AddMaterial(Renderer rRenderer, Material rMaterial)
        {
            if (rRenderer == null) { return; }
            if (rMaterial == null) { return; }

            if (rRenderer != null)
            {
                for (int i = 0; i < rRenderer.materials.Length; i++)
                {
                    if (rRenderer.materials[i] == mMaterialInstance) { return; }
                }

                Material[] lMaterials = new Material[rRenderer.materials.Length + 1];
                Array.Copy(rRenderer.materials, lMaterials, rRenderer.materials.Length);

                //mMaterialInstance = Material.Instantiate(rMaterial);
                lMaterials[rRenderer.materials.Length] = rMaterial;

                rRenderer.materials = lMaterials;

                mMaterialInstance = rRenderer.materials[rRenderer.materials.Length - 1];

                // Set the material color
                for (int i = 0; i < MATERIAL_COLORS.Length; i++)
                {
                    if (mMaterialInstance.HasProperty(MATERIAL_COLORS[i]))
                    {
                        mMaterialInstance.SetColor(MATERIAL_COLORS[i], HighlightColor);
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Removes the material instance from the specified target
        /// </summary>
        /// <param name="rTarget"></param>
        /// <param name="rMaterialInstance"></param>
        protected virtual void RemoveMaterial(Renderer rRenderer, Material rMaterialInstance)
        {
            if (rRenderer == null) { return; }
            if (rMaterialInstance == null) { return; }

            if (rRenderer != null)
            {
                // Check if the material exists
                bool lFound = false;
                for (int i = 0; i < rRenderer.materials.Length; i++)
                {
                    if (rRenderer.materials[i] == mMaterialInstance) { lFound = true; }
                }

                if (!lFound) { return; }

                // Remove the material
                int lNewIndex = 0;
                Material[] lMaterials = new Material[rRenderer.materials.Length - 1];

                for (int i = 0; i < rRenderer.materials.Length; i++)
                {
                    if (rRenderer.materials[i] != rMaterialInstance)
                    {
                        lMaterials[lNewIndex] = rRenderer.materials[i];
                        lNewIndex++;
                    }
                }

                rRenderer.materials = lMaterials;

                // Remove the material instance
                mMaterialInstance = null;
            }
        }

#if UNITY_EDITOR
        public bool EditorShowEvents = false;
#endif
    }
}
