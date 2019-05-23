using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.Animations;
#endif

namespace com.ootii.Actors.AnimationControllers
{
    /// <summary>
    /// Contains information about the motion pack that will be used
    /// when setting up the motions
    /// </summary>
    public abstract class MotionPackDefinition
    {
        /// <summary>
        /// Defines the friendly name of the motion pack
        /// </summary>
        public static string PackName
        {
            get { return ""; }
        }

#if UNITY_EDITOR

        /// <summary>
        /// Draws the inspector for the pack
        /// </summary>
        /// <returns>Determines if the editor has been modified</returns>
        public static bool OnPackInspector(MotionController rMotionController)
        {
            return false;
        }

        /// <summary>
        /// Creates the animator if needed
        /// </summary>
        /// <param name="rAnimatorController"></param>
        public static void SetupAnimatorController(AnimatorController rAnimatorController)
        {
            if (rAnimatorController.layers.Length < 1)
            {
                rAnimatorController.AddLayer("Base Layer");
                rAnimatorController.layers[0].iKPass = true;
            }

            if (rAnimatorController.layers.Length < 2)
            {
                rAnimatorController.AddLayer("Upper Layer");
                rAnimatorController.layers[1].iKPass = true;
                rAnimatorController.layers[1].avatarMask = AssetDatabase.LoadAssetAtPath<AvatarMask>("Assets/ootii/MotionController/Content/Animations/Upper.mask");
            }

            if (rAnimatorController.layers.Length < 3)
            {
                rAnimatorController.AddLayer("Arms Only Layer");
                rAnimatorController.layers[2].iKPass = true;
                rAnimatorController.layers[2].avatarMask = AssetDatabase.LoadAssetAtPath<AvatarMask>("Assets/ootii/MotionController/Content/Animations/ArmsOnly.mask");
            }

            CreateAnimatorParameter(rAnimatorController, "L0MotionForm", AnimatorControllerParameterType.Int);

            CreateAnimatorParameter(rAnimatorController, "L1MotionForm", AnimatorControllerParameterType.Int);

            CreateAnimatorParameter(rAnimatorController, "L2MotionPhase", AnimatorControllerParameterType.Int);
            CreateAnimatorParameter(rAnimatorController, "L2MotionForm", AnimatorControllerParameterType.Int);
            CreateAnimatorParameter(rAnimatorController, "L2MotionParameter", AnimatorControllerParameterType.Int);
            CreateAnimatorParameter(rAnimatorController, "L2MotionStateTime", AnimatorControllerParameterType.Float);
        }

        /// <summary>
        /// Creates an animator parameter of the specified type (if it doesn't exist)
        /// </summary>
        /// <param name="rAnimatorController">Controller we're adding the parameter to</param>
        /// <param name="rName">Name of the parameter</param>
        /// <param name="rType">Type of the parameter</param>
        public static void CreateAnimatorParameter(AnimatorController rAnimatorController, string rName, AnimatorControllerParameterType rType)
        {
            for (int i = 0; i < rAnimatorController.parameters.Length; i++)
            {
                if (rAnimatorController.parameters[i].name == rName)
                {
                    return;
                }
            }

            rAnimatorController.AddParameter(rName, rType);
        }

#endif

    }
}
