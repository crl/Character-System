using UnityEngine;

namespace com.ootii.Actors.AnimationControllers
{
    /// <summary>
    /// Interface to help identify cover motions that we may need to move
    /// </summary>
    public interface ICoverMotion : IMotionControllerMotion
    {
        /// <summary>
        /// Determines if we're currently existing cover
        /// </summary>
        bool IsExiting { get; }

        /// <summary>
        /// Forces us to exit cover and then deactivate
        /// </summary>
        /// <param name="rExtrapolatePosition">Determines if we extrapolate the postion from our camera rig.</param>
        /// <param name="rUseCameraRotation">Determines if we keep the camera rotation vs. the anchor rotation.</param>
        void ExitCover(bool rExtrapolatePosition = false, bool rUseCameraRotation = false);

        /// <summary>
        /// Forces us to exit cover and then deactivate
        /// </summary>
        /// <param name="rPosition">Position to exit to</param>
        /// <param name="rRotation">Rotation to exit to</param>
        void ExitCover(Vector3 rPosition, Quaternion rRotation);
    }
}
