using UnityEngine;
using com.ootii.Actors.AnimationControllers;

namespace com.ootii.Actors.LifeCores
{
    /// <summary>
    /// Foundation for things the characters can interact with
    /// </summary>
    public interface IInteractableCore : ILifeCore
    {
        /// <summary>
        /// Determines if the interactable is enabled or not
        /// </summary>
        bool IsEnabled { get; set; }

        /// <summary>
        /// Defines the animation to use with this interactable
        /// </summary>
        int Form { get; set; }

        /// <summary>
        /// Determine if we need to force the actor position
        /// </summary>
        bool ForcePosition { get; set; }

        /// <summary>
        /// Determines if we need to force the actor to rotate
        /// </summary>
        bool ForceRotation { get; set; }

        /// <summary>
        /// Distance to move to before the motion can activate
        /// </summary>
        float TargetDistance { get; set; }

        /// <summary>
        /// Position and rotation to move to before the motion can activate
        /// </summary>
        Transform TargetLocation { get; set; }

        /// <summary>
        /// Determines if we'll use a raycast to trigger this interactable
        /// </summary>
        bool UseRaycast { get; set; }

        /// <summary>
        /// Area we can use for raycast targeting
        /// </summary>
        Collider RaycastCollider { get; set; }

        /// <summary>
        /// Determine if the activator is valid (in position, etc)
        /// </summary>
        bool TestActivator(Transform rTransform);

        /// <summary>
        /// Starts the focus process for the interactable
        /// </summary>
        void StartFocus();

        /// <summary>
        /// Stops the focus process for the interactable
        /// </summary>
        void StopFocus();

        /// <summary>
        /// Raised when the interactable is triggered
        /// </summary>
        void OnActivated(BasicInteraction rMotion);
    }
}
