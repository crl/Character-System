using System.Collections.Generic;
using UnityEngine;

namespace com.ootii.Graphics.UI
{
    /// <summary>
    /// Provides a method for managing the reticle we display and 
    /// selecting targets under the reticle
    /// </summary>
    public interface IReticle
    {
        /// <summary>
        /// Determines if the reticle is visible
        /// </summary>
        bool IsVisible { get; set; }

        /// <summary>
        /// Width and height of the reticle
        /// </summary>
        Vector2 Size { get; set; }

        /// <summary>
        /// Determines how much to fill the reticle
        /// </summary>
        float FillPercent { get; set; }

        /// <summary>
        /// Texture that will be used as the Reticle
        /// </summary>
        Texture2D BGTexture { get; set; }

        /// <summary>
        /// Texture that will be used as the Reticle
        /// </summary>
        Texture2D FillTexture { get; set; }

        /// <summary>
        /// Transform used for raycasting
        /// </summary>
        Transform RaycastRoot { get; set; }

        /// <summary>
        /// Casts a ray out of the reticle (camera) and returns all the items that the ray hits
        /// </summary>
        /// <param name="rHitInfos">RaycastHit info that describes what are hit</param>
        /// <param name="rMinDistance">Starting distance of the ray</param>
        /// <param name="rMaxDistance">Max distance of the ray</param>
        /// <param name="rRadius">Radius to use in order to cast a sphere</param>
        /// <param name="rLayerMask">Mask of things to hit</param>
        /// <param name="rIgnore">Transform to ignore</param>
        /// <param name="rIgnoreList">List of transforms to ignore</param>
        /// <returns>Number of items the ray hits</returns>
        int RaycastAll(out RaycastHit[] rHitInfos, float rMinDistance, float rMaxDistance, float rRadius, int rLayerMask = -1, Transform rIgnore = null, List<Transform> rIgnoreList = null);
    }
}
