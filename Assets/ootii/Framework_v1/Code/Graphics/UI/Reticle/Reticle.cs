using System;
using System.Collections.Generic;
using UnityEngine;
using com.ootii.Actors.Attributes;
using com.ootii.Geometry;

namespace com.ootii.Graphics.UI
{
    /// <summary>
    /// Provides a method for managing the reticle we display and 
    /// selecting targets under the reticle
    /// </summary>
    public class Reticle : MonoBehaviour, IReticle
    {
        /// <summary>
        /// Dracks the default state of the reticle so we can reset it.
        /// </summary>
        public static bool DefaultIsVisible = false;

        /// <summary>
        /// Provides global access to the reticle
        /// </summary>
        public static IReticle Instance = null;

        /// <summary>
        /// Determines if the reticle is visible
        /// </summary>
        public bool _IsVisible = true;
        public bool IsVisible
        {
            get { return _IsVisible; }
            set { _IsVisible = value; }
        }

        /// <summary>
        /// Width and height of the reticle
        /// </summary>
        public Vector2 _Size = new Vector2(32f, 32f);
        public Vector2 Size
        {
            get { return _Size; }
            set { _Size = value; }
        }

        /// <summary>
        /// Offset from the center
        /// </summary>
        public Vector2 _Offset = new Vector2(0f, 0f);
        public Vector2 Offset
        {
            get { return _Offset; }
            set { _Offset = value; }
        }

        /// <summary>
        /// Determines how much to fill the circle
        /// </summary>
        protected float mFillPercent = 0f;
        public float FillPercent
        {
            get { return mFillPercent; }

            set
            {
                if (value != mFillPercent)
                {
                    CreateTexture(value);
                    mFillPercent = value;
                }
            }
        }

        /// <summary>
        /// Texture that will be used as the Reticle
        /// </summary>
        public Texture2D _BGTexture;
        public Texture2D BGTexture
        {
            get { return _BGTexture; }

            set
            {
                _BGTexture = value;

                if (Application.isPlaying)
                {
                    CreateTexture(mFillPercent);
                }
            }
        }

        /// <summary>
        /// Texture that will be used as the Reticle
        /// </summary>
        public Texture2D _FillTexture;
        public Texture2D FillTexture
        {
            get { return _FillTexture; }

            set
            {
                _FillTexture = value;

                if (Application.isPlaying)
                {
                    CreateTexture(mFillPercent);
                }
            }
        }

        /// <summary>
        /// Transform used for raycasting
        /// </summary>
        public Transform _RaycastRoot = null;
        public virtual Transform RaycastRoot
        {
            get { return _RaycastRoot; }
            set { _RaycastRoot = value; }
        }

        /// <summary>
        /// Rectangel representing the position and size of the texture
        /// </summary>
        protected Rect mScreenRect = new Rect();

        /// <summary>
        /// Support blitting
        /// </summary>
        protected Material mClearMaterial = null;
        protected Material mBlitMaterial = null;
        protected RenderTexture mRenderTexture = null;

        /// <summary>
        /// Runs before any Update is called
        /// </summary>
        void Start()
        {
            // Create our global instance
            if (Reticle.Instance == null)
            {
                Reticle.DefaultIsVisible = IsVisible;
                Reticle.Instance = this;
            }

            // Ensure we have a casting root
            if (RaycastRoot == null && Camera.main != null) { RaycastRoot = Camera.main.transform; }

            // Ensure the position is set
            if (_FillTexture != null) { FillTexture = _FillTexture; }
            if (_BGTexture != null) { BGTexture = _BGTexture; }

            // Initialize the material
            FillPercent = 1f;
            FillPercent = 0f;
        }

        /// <summary>
        /// Finds the target within the specified conditions that has the at least one matching tag
        /// </summary>
        /// <param name="rHitInfo">RaycastHit with the hit information</param>
        /// <param name="rMinDistance">Distance from the camera to start the ray</param>
        /// <param name="rMaxDistance">Max distance to shoot the ray</param>
        /// <param name="rRadius">Radius of the ray (use 0 for a simple ray)</param>
        /// <param name="rLayerMask">Layers to collide with</param>
        /// <param name="rTags">Comma delimited tags where one must exist</param>
        /// <param name="rIgnore">Object that we'll ignore collision with</param>
        /// <param name="rIgnoreList">Objects that we'll ignore collisions with</param>
        /// <returns>Returns true if a collision occurs or false if not</returns>
        public virtual bool FindTarget(out RaycastHit rHitInfo, float rMinDistance, float rMaxDistance, float rRadius, int rLayerMask = -1, string rTags = "", Transform rIgnore = null, List<Transform> rIgnoreList = null)
        {
            bool lRayHit = false;
            rHitInfo = RaycastExt.EmptyHitInfo;

            RaycastHit[] lHitInfos;
            int lHitCount = RaycastAll(out lHitInfos, rMinDistance, rMaxDistance, rRadius, rLayerMask, rIgnore, rIgnoreList);

            if (lHitCount > 0)
            {
                if (rTags == null || rTags.Length == 0)
                {
                    lRayHit = true;
                    rHitInfo = lHitInfos[0];
                }
                else
                {
                    for (int i = 0; i < lHitCount; i++)
                    {
                        IAttributeSource lAttributeSource = lHitInfos[i].collider.gameObject.GetComponent<IAttributeSource>();
                        if (lAttributeSource != null && lAttributeSource.AttributesExist(rTags, false))
                        {
                            lRayHit = true;
                            rHitInfo = lHitInfos[i];

                            break;
                        }
                    }
                }
            }

            return lRayHit;
        }

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
        public virtual int RaycastAll(out RaycastHit[] rHitInfos, float rMinDistance, float rMaxDistance, float rRadius, int rLayerMask = -1, Transform rIgnore = null, List<Transform> rIgnoreList = null)
        {
            if (RaycastRoot == null && Camera.main != null) { RaycastRoot = Camera.main.transform; }

            Transform lOrigin = (RaycastRoot != null ? RaycastRoot : null);
            if (lOrigin == null) { lOrigin = transform; }

            Vector3 lStart = lOrigin.position + (lOrigin.forward * rMinDistance);
            Vector3 lDirection = lOrigin.forward;

            int lHitCount = 0;

            if (rRadius <= 0)
            {
                lHitCount = RaycastExt.SafeRaycastAll(lStart, lDirection, out rHitInfos, rMaxDistance - rMinDistance, rLayerMask, rIgnore, rIgnoreList, true);
            }
            else
            {
                lHitCount = RaycastExt.SafeSphereCastAll(lStart, lDirection, rRadius, out rHitInfos, rMaxDistance - rMinDistance, rLayerMask, rIgnore, rIgnoreList, true);
            }

            if (lHitCount > 1)
            {
                Array.Sort(rHitInfos, 0, lHitCount, RaycastExt.HitDistanceComparer);
            }

            //Graphics.GraphicsManager.DrawLine(lStart, lStart + (lDirection * (rMaxDistance - rMinDistance)), Color.red);

            //for (int i = 0; i < lHitCount; i++)
            //{
            //    Graphics.GraphicsManager.DrawPoint(rHitInfos[i].point, Color.red);
            //}

            return lHitCount;
        }

        /// <summary>
        /// Casts a ray out of the reticle (camera) to determine if we hit something.
        /// </summary>
        /// <param name="rHitInfo">RaycastHit with the hit information</param>
        /// <param name="rMinDistance">Distance from the camera to start the ray</param>
        /// <param name="rMaxDistance">Max distance to shoot the ray</param>
        /// <param name="rRadius">Radius of the ray (use 0 for a simple ray)</param>
        /// <param name="rLayerMask">Layers to collide with</param>
        /// <param name="rIgnore">Object that we'll ignore collision with</param>
        /// <returns>Returns true if a collision occurs or false if not</returns>
        public virtual bool Raycast(out RaycastHit rHitInfo, float rMinDistance, float rMaxDistance, float rRadius, int rLayerMask = -1, Transform rIgnore = null)
        {
            if (RaycastRoot == null && Camera.main != null) { RaycastRoot = Camera.main.transform; }

            Transform lOrigin = (RaycastRoot != null ? RaycastRoot : null);
            if (lOrigin == null) { lOrigin = transform; }

            Vector3 lStart = lOrigin.position + (lOrigin.forward * rMinDistance);
            Vector3 lDirection = lOrigin.forward;

            rHitInfo = RaycastExt.EmptyHitInfo;

            if (rRadius > 0f)
            {
                if (RaycastExt.SafeSphereCast(lStart, lDirection, rRadius, out rHitInfo, rMaxDistance - rMinDistance, rLayerMask, rIgnore, null, true))
                {
                    return true;
                }
            }

            // We do this even after the sphere cast so that we select the position in the reticle
            if (RaycastExt.SafeRaycast(lStart, lDirection, out rHitInfo, rMaxDistance, rLayerMask, rIgnore, null, true))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// OnGUI is called for rendering and handling GUI events.
        /// </summary>
        protected virtual void OnGUI()
        {
            if (!_IsVisible) { return; }

            Texture lTexture = mRenderTexture;

#if UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
            // Mac seems to have a problem with our render textures
            //lTexture = _BGTexture;
#endif

            if (lTexture == null) { return; }

            mScreenRect.x = ((Screen.width - _Size.x) / 2f) + _Offset.x;
            mScreenRect.y = ((Screen.height - _Size.y) / 2f) + _Offset.y;
            mScreenRect.width = _Size.x;
            mScreenRect.height = _Size.y;
            GUI.DrawTexture(mScreenRect, lTexture);
        }

        /// <summary>
        /// Generates the texture we'll display as the Reticle. Note that we CANNOT
        /// call this function in OnGUI.
        /// </summary>
        protected virtual void CreateTexture(float rPercent)
        {
            if (_BGTexture == null) { return; }

            if (mClearMaterial == null)
            {
                mClearMaterial = new Material(Shader.Find("Hidden/ClearBlit"));
            }

            if (mBlitMaterial == null)
            {
                mBlitMaterial = new Material(Shader.Find("Hidden/RadialBlit"));
            }

            if (mRenderTexture == null)
            {
                mRenderTexture = new RenderTexture(256, 256, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
                mRenderTexture.wrapMode = TextureWrapMode.Clamp;
                mRenderTexture.filterMode = FilterMode.Trilinear;
                mRenderTexture.anisoLevel = 2;

#if !(UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX)
                mRenderTexture.antiAliasing = 4;
#endif
            }

            // Clear the image
            UnityEngine.Graphics.Blit(_BGTexture, mRenderTexture, mClearMaterial, 0);

            // Fill with the background or fill based on the angle
            mBlitMaterial.SetFloat("_Angle", Mathf.Lerp(-3.1416f, 3.1416f, rPercent));
            mBlitMaterial.SetTexture("_FillTex", _FillTexture);
            UnityEngine.Graphics.Blit(_BGTexture, mRenderTexture, mBlitMaterial, 0);
        }
    }
}
