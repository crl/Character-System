using System;
using System.Collections.Generic;
using UnityEngine;
using com.ootii.Collections;
using com.ootii.Geometry;

namespace com.ootii.Actors.LifeCores
{
    /// <summary>
    /// Particle cores give us a way to manage a particle for a limited amount of time.
    /// </summary>
    public class ParticleCore : MonoBehaviour, ILifeCore
    {
        /// <summary>
        /// Floating point error constant
        /// </summary>
        public const float EPSILON = 0.001f;

        // Color names that could be used in materials
        private static string[] MATERIAL_COLORS = new string[] { "_Color", "_MainColor", "_TintColor", "_EmissionColor", "_BorderColor", "_ReflectColor", "_RimColor", "_CoreColor" };

        /// <summary>
        /// Prefab this core was created from. We'll use it to
        /// release the GameObject once done
        /// </summary>
        protected GameObject mPrefab = null;
        public GameObject Prefab
        {
            get { return mPrefab; }
            set { mPrefab = value; }
        }

        /// <summary>
        /// Cache the transform for use
        /// </summary>
        [NonSerialized]
        public Transform _Transform = null;
        public Transform Transform
        {
            get { return _Transform; }
            set { _Transform = value; }
        }

        /// <summary>
        /// Amount of time to keep the particles active for before terminating
        /// </summary>
        public float _MaxAge = 1f;
        public virtual float MaxAge
        {
            get { return _MaxAge; }
            set { _MaxAge = value; }
        }

        /// <summary>
        /// Age of the life core so we know when to expire it
        /// </summary>
        protected float mAge = 0f;
        public virtual float Age
        {
            get { return mAge; }
            set { mAge = value; }
        }

        /// <summary>
        /// GameObject that holds ongoing effects
        /// </summary>
        public GameObject _LifeRoot = null;
        public GameObject LifeRoot
        {
            get { return _LifeRoot; }
            set { _LifeRoot = value; }
        }

        /// <summary>
        /// Determines if particles will be moved to match the underlying surface
        /// </summary>
        public bool _SkimSurface = false;
        public bool SkimSurface
        {
            get { return _SkimSurface; }

            set
            {
                _SkimSurface = value;

                // If we have to attract particles, prep the structures
                if (_SkimSurface && mParticleSystems != null && mParticleSystems.Length > 0)
                {
                    if (mLifeParticlesArray == null)
                    {
                        mLifeParticlesArray = new List<ParticleSystem.Particle[]>();

                        for (int i = 0; i < mParticleSystems.Length; i++)
                        {
#if UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4
                            mLifeParticlesArray.Add(new ParticleSystem.Particle[mParticleSystems[i].maxParticles]);
#else
                            mLifeParticlesArray.Add(new ParticleSystem.Particle[mParticleSystems[i].main.maxParticles]);
#endif
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Distance from the surface to place the particles
        /// </summary>
        public float _SkimSurfaceDistance = 0.05f;
        public float SkimSurfaceDistance
        {
            get { return _SkimSurfaceDistance; }
            set { _SkimSurfaceDistance = value; }
        }

        /// <summary>
        /// Layer value for surface skinning
        /// </summary>
        public int _SkimSurfaceLayers = -1;
        public int SkimSurfaceLayers
        {
            get { return _SkimSurfaceLayers; }
            set { _SkimSurfaceLayers = value; }
        }

        /// <summary>
        /// Target that the particles will move towards. We do this by rotating
        /// the core towards this object.
        /// </summary>
        public Transform _Attractor = null;
        public Transform Attractor
        {
            get { return _Attractor; }

            set
            {
                _Attractor = value;

                // If we have to attract particles, prep the structures
                if (_Attractor != null && mParticleSystems != null && mParticleSystems.Length > 0)
                {
                    if (mLifeParticlesArray == null)
                    {
                        mLifeParticlesArray = new List<ParticleSystem.Particle[]>();

                        for (int i = 0; i < mParticleSystems.Length; i++)
                        {
#if UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4
                            mLifeParticlesArray.Add(new ParticleSystem.Particle[mParticleSystems[i].maxParticles]);
#else
                            mLifeParticlesArray.Add(new ParticleSystem.Particle[mParticleSystems[i].main.maxParticles]);
#endif
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Target offset that the particles will move towards.
        /// </summary>
        public Vector3 _AttractorOffset = Vector3.zero;
        public Vector3 AttractorOffset
        {
            get { return _AttractorOffset; }
            set { _AttractorOffset = value; }
        }

        /// <summary>
        /// Speed at which we change the projector's alpha
        /// </summary>
        public float _AudioFadeInSpeed = 0f;
        public float AudioFadeInSpeed
        {
            get { return _AudioFadeInSpeed; }
            set { _AudioFadeInSpeed = value; }
        }

        /// <summary>
        /// Speed at which we change the projector's alpha
        /// </summary>
        public float _AudioFadeOutSpeed = 1f;
        public float AudioFadeOutSpeed
        {
            get { return _AudioFadeOutSpeed; }
            set { _AudioFadeOutSpeed = value; }
        }

        /// <summary>
        /// Speed at which we change the projector's alpha
        /// </summary>
        public float _LightFadeInSpeed = 0f;
        public float LightFadeInSpeed
        {
            get { return _LightFadeInSpeed; }
            set { _LightFadeInSpeed = value; }
        }

        /// <summary>
        /// Speed at which we change the projector's alpha
        /// </summary>
        public float _LightFadeOutSpeed = 1f;
        public float LightFadeOutSpeed
        {
            get { return _LightFadeOutSpeed; }
            set { _LightFadeOutSpeed = value; }
        }

        /// <summary>
        /// Speed at which we change the projector's alpha
        /// </summary>
        public float _ProjectorFadeInSpeed = 0f;
        public float ProjectorFadeInSpeed
        {
            get { return _ProjectorFadeInSpeed; }
            set { _ProjectorFadeInSpeed = value; }
        }

        /// <summary>
        /// Speed at which we change the projector's alpha
        /// </summary>
        public float _ProjectorFadeOutSpeed = 1f;
        public float ProjectorFadeOutSpeed
        {
            get { return _ProjectorFadeOutSpeed; }
            set { _ProjectorFadeOutSpeed = value; }
        }

        /// <summary>
        /// Callback to be notified when the particle is destroyed
        /// </summary>
        [NonSerialized]
        public LifeCoreDelegate OnReleasedEvent = null;

        // Track the instance of the life effects
        protected GameObject mLifeInstance = null;
        public GameObject LifeInstance
        {
            get { return mLifeInstance; }
        }

        // Particle system associated with the effect
        protected ParticleSystem[] mParticleSystems = null;

        // Holder for all the particles
        protected List<ParticleSystem.Particle[]> mLifeParticlesArray = null;

        // Projectors associated with the effect
        protected Projector[] mProjectors = null;

        // Max alpha value for the projector
        protected Dictionary<Projector, float> mProjectorAlpha = null;

        // Determines if we're waiting for the effects to shut down
        protected bool mIsShuttingDown = false;

        /// <summary>
        /// Awake is called when the script instance is being loaded.
        /// </summary>
        public virtual void Awake()
        {
            _Transform = gameObject.transform;
        }

        /// <summary>
        /// Used to start the particle core all over
        /// </summary>
        public virtual void Play()
        {
            mAge = 0;
            mIsShuttingDown = false;

            if (_LifeRoot != null)
            {
                Vector3 lScale = _LifeRoot.transform.localScale;
                Quaternion lRotation = _LifeRoot.transform.rotation;
                Vector3 lPosition = _LifeRoot.transform.position;

                mLifeInstance = GameObject.Instantiate(_LifeRoot);
                mLifeInstance.transform.parent = _Transform;
                mLifeInstance.transform.localScale = lScale;
                mLifeInstance.transform.localRotation = lRotation;
                mLifeInstance.transform.localPosition = lPosition;

                if (mLifeInstance != null)
                {
                    mParticleSystems = mLifeInstance.GetComponentsInChildren<ParticleSystem>();
                    mProjectors = mLifeInstance.GetComponentsInChildren<Projector>();

                    if (mProjectors != null && _ProjectorFadeInSpeed > 0f)
                    {
                        if (mProjectorAlpha == null) { mProjectorAlpha = new Dictionary<Projector, float>(); }
                        mProjectorAlpha.Clear();

                        // Store the max alpha of the material
                        for (int i = 0; i < mProjectors.Length; i++)
                        {
                            Material lMaterial = mProjectors[i].material;

                            // Ensure we're not messing with the original material
                            if (!lMaterial.name.EndsWith("(Clone)"))
                            {
                                lMaterial = new Material(lMaterial) { name = lMaterial.name + " (Clone)" };
                                mProjectors[i].material = lMaterial;
                            }

                            // Set the material color
                            for (int j = 0; j < MATERIAL_COLORS.Length; j++)
                            {
                                if (lMaterial.HasProperty(MATERIAL_COLORS[j]))
                                {
                                    Color lColor = lMaterial.GetColor(MATERIAL_COLORS[j]);

                                    mProjectorAlpha.Add(mProjectors[i], lColor.a);

                                    lColor.a = 0f;
                                    lMaterial.SetColor(MATERIAL_COLORS[j], lColor);

                                    break;
                                }
                            }
                        }
                    }
                }

                // If we have to attract particles, prep the structures
                if (_Attractor != null || _SkimSurface)
                {
                    if (mParticleSystems != null && mParticleSystems.Length > 0)
                    {
                        mLifeParticlesArray = new List<ParticleSystem.Particle[]>();

                        for (int i = 0; i < mParticleSystems.Length; i++)
                        {
#if UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4
                            mLifeParticlesArray.Add(new ParticleSystem.Particle[mParticleSystems[i].maxParticles]);
#else
                            mLifeParticlesArray.Add(new ParticleSystem.Particle[mParticleSystems[i].main.maxParticles]);
#endif
                        }
                    }
                }

                StartEffects(mLifeInstance);
            }

            // Since we have an attractor, point to it
            if (_Attractor != null)
            {
                Vector3 lStartPosition = _Transform.position;
                Vector3 lParticleTarget = _Attractor.position + (_Attractor.rotation * _AttractorOffset);
                Vector3 lDirection = (lParticleTarget - lStartPosition).normalized;
                _Transform.rotation = Quaternion.LookRotation(lDirection, Vector3.up);
            }

            // Play the particles
            if (mParticleSystems != null)
            {
                for (int i = 0; i < mParticleSystems.Length; i++)
                {
                    // If we're using an attractor, we need to disable trails for a bit
                    if (_Attractor != null && mLifeParticlesArray != null)
                    {
                        int lParticleCount = mParticleSystems[i].GetParticles(mLifeParticlesArray[i]);
                        if (lParticleCount > 0 && mLifeParticlesArray != null)
                        {
                            // Reset each particle
                            for (int j = 0; j < lParticleCount; j++)
                            {
                                mLifeParticlesArray[i][j].position = Vector3.zero;
                                mLifeParticlesArray[i][j].velocity = Vector3.zero;
                            }

                            mParticleSystems[i].SetParticles(mLifeParticlesArray[i], lParticleCount);
                        }
                    }

                    mParticleSystems[i].Play(true);
                }
            }
        }

        /// <summary>
        /// Stop the particle core. This will may release it as well.
        /// </summary>
        public virtual void Stop(bool rHardStop = false)
        {
            if (!mIsShuttingDown)
            {
                mIsShuttingDown = true;

                // Stop the life particles
                StopEffects(mLifeInstance);
            }
        }

        /// <summary>
        /// Update is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        public virtual void Update()
        {
            mAge = mAge + Time.deltaTime;

            // Check if it's time to stop the particles
            if (!mIsShuttingDown && (_MaxAge > 0f && mAge >= _MaxAge))
            {
                Stop();
            }

            bool lAreLifeParticlesAlive = UpdateEffects(mLifeInstance, mIsShuttingDown);

            // If they have all stopped, we can destory
            if (mIsShuttingDown && !lAreLifeParticlesAlive)
            {
                lAreLifeParticlesAlive = UpdateEffects(mLifeInstance, mIsShuttingDown);
                Release();
            }
        }

        /// <summary>
        /// Update is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        public virtual void LateUpdate()
        {
            UpdateLifeParticles();
        }

        /// <summary>
        /// Start the effects based on the fade out speed
        /// </summary>
        /// <param name="rInstance"></param>
        protected void StartEffects(GameObject rInstance)
        {
            if (rInstance == null) { return; }

            // Ensure particles are running
            //ParticleSystem[] lParticleSystems = rInstance.GetComponentsInChildren<ParticleSystem>();
            if (mParticleSystems != null)
            {
                for (int i = 0; i < mParticleSystems.Length; i++)
                {
                    if (!mParticleSystems[i].IsAlive(true))
                    {
                        mParticleSystems[i].Play(true);
                    }
                }
            }

            // Check if sounds are alive
            AudioSource[] lAudioSources = rInstance.GetComponentsInChildren<AudioSource>();
            for (int i = 0; i < lAudioSources.Length; i++)
            {
                if (!lAudioSources[i].isPlaying && AudioFadeInSpeed <= 0f)
                {
                    lAudioSources[i].Play();
                }
            }

            // Check if lights are alive
            Light[] lLights = rInstance.GetComponentsInChildren<Light>();
            for (int i = 0; i < lLights.Length; i++)
            {
                if (lLights[i].intensity == 0f && LightFadeInSpeed <= 0f)
                {
                    lLights[i].intensity = 1f;
                }
            }

            // Ensure projectors are running
            //Projector[] lProjectors = rInstance.GetComponentsInChildren<Projector>();
            if (mProjectors != null)
            {
                for (int i = 0; i < mProjectors.Length; i++)
                {
                    //if (mProjectors[i].material.HasProperty("_Alpha"))
                    //{
                    //    float lAlpha = mProjectors[i].material.GetFloat("_Alpha");
                    //    if (lAlpha == 0f && ProjectorFadeInSpeed <= 0f)
                    //    {
                    //        mProjectors[i].material.SetFloat("_Alpha", 1f);
                    //    }
                    //}

                    Material lMaterial = mProjectors[i].material;
                    for (int j = 0; j < MATERIAL_COLORS.Length; j++)
                    {
                        if (lMaterial.HasProperty(MATERIAL_COLORS[j]))
                        {
                            Color lColor = lMaterial.GetColor(MATERIAL_COLORS[j]);
                            if (lColor.a == 0f && ProjectorFadeInSpeed <= 0f)
                            {
                                lColor.a = 1f;
                                mProjectors[i].material.SetColor(MATERIAL_COLORS[j], lColor);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Stops the effects based on the fade out speed
        /// </summary>
        /// <param name="rInstance"></param>
        protected void StopEffects(GameObject rInstance)
        {
            if (rInstance == null) { return; }

            // Ensure particles are running
            //ParticleSystem[] lParticleSystems = rInstance.GetComponentsInChildren<ParticleSystem>();
            if (mParticleSystems != null)
            {
                for (int i = 0; i < mParticleSystems.Length; i++)
                {
                    if (mParticleSystems[i].IsAlive(true))
                    {
#if UNITY_5_0 || UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4
                        mParticleSystems[i].Stop(true);
#else
                        mParticleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
#endif
                    }
                }
            }

            // Check if sounds are alive
            AudioSource[] lAudioSources = rInstance.GetComponentsInChildren<AudioSource>();
            for (int i = 0; i < lAudioSources.Length; i++)
            {
                if (lAudioSources[i].isPlaying && AudioFadeOutSpeed <= 0f)
                {
                    lAudioSources[i].Stop();
                }
            }

            // Check if lights are alive
            Light[] lLights = rInstance.GetComponentsInChildren<Light>();
            for (int i = 0; i < lLights.Length; i++)
            {
                if (lLights[i].intensity > 0f && LightFadeOutSpeed <= 0f)
                {
                    lLights[i].intensity = 0f;
                }
            }

            // Ensure projectors are running
            //Projector[] lProjectors = rInstance.GetComponentsInChildren<Projector>();
            if (mProjectors != null)
            {
                for (int i = 0; i < mProjectors.Length; i++)
                {
                    //if (mProjectors[i].material.HasProperty("_Alpha"))
                    //{
                    //    float lAlpha = mProjectors[i].material.GetFloat("_Alpha");
                    //    if (lAlpha > 0f && ProjectorFadeOutSpeed <= 0f)
                    //    {
                    //        mProjectors[i].material.SetFloat("_Alpha", 0f);
                    //    }
                    //}

                    Material lMaterial = mProjectors[i].material;
                    for (int j = 0; j < MATERIAL_COLORS.Length; j++)
                    {
                        if (lMaterial.HasProperty(MATERIAL_COLORS[j]))
                        {
                            Color lColor = lMaterial.GetColor(MATERIAL_COLORS[j]);
                            if (lColor.a > 0f && ProjectorFadeOutSpeed <= 0f)
                            {
                                lColor.a = 0f;
                                mProjectors[i].material.SetColor(MATERIAL_COLORS[j], lColor);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Update the effects and fade them in and out as needed
        /// </summary>
        /// <param name="rInstance">Instance we're processing</param>
        /// <param name="rShutDown">Determines if we're shutting down or not</param>
        /// <returns></returns>
        protected bool UpdateEffects(GameObject rInstance, bool rShutDown)
        {
            if (rInstance == null) { return false; }

            bool lIsAlive = false;

            if (!rShutDown)
            {
                // Ensure particles are running
                //ParticleSystem[] lParticleSystems = rInstance.GetComponentsInChildren<ParticleSystem>();
                if (mParticleSystems != null)
                {
                    for (int i = 0; i < mParticleSystems.Length; i++)
                    {
                        if (mParticleSystems[i].IsAlive(true))
                        {
                            lIsAlive = true;
                        }
                    }
                }

                // Check if sounds are alive
                if (AudioFadeInSpeed > 0f)
                {
                    AudioSource[] lAudioSources = rInstance.GetComponentsInChildren<AudioSource>();
                    for (int i = 0; i < lAudioSources.Length; i++)
                    {
                        if (lAudioSources[i].isPlaying)
                        {
                            lIsAlive = true;

                            if (lAudioSources[i].volume < 1f)
                            {
                                lAudioSources[i].volume = Mathf.Clamp01(lAudioSources[i].volume - (AudioFadeInSpeed * Time.deltaTime));
                            }
                        }
                    }
                }

                // Check if lights are alive
                if (LightFadeInSpeed > 0f)
                {
                    Light[] lLights = rInstance.GetComponentsInChildren<Light>();
                    for (int i = 0; i < lLights.Length; i++)
                    {
                        lIsAlive = true;

                        if (lLights[i].intensity < 1f)
                        {
                            lLights[i].intensity = Mathf.Clamp01(lLights[i].intensity + (LightFadeInSpeed * Time.deltaTime));
                        }
                    }
                }

                // Ensure projectors are running
                if (mProjectors != null && ProjectorFadeInSpeed > 0f)
                {
                    //Projector[] lProjectors = rInstance.GetComponentsInChildren<Projector>();
                    for (int i = 0; i < mProjectors.Length; i++)
                    {
                        float lMaxAlpha = 1f;
                        if (mProjectorAlpha.ContainsKey(mProjectors[i])) { lMaxAlpha = mProjectorAlpha[mProjectors[i]]; }

                        //if (mProjectors[i].material.HasProperty("_Alpha"))
                        //{
                        //    float lAlpha = mProjectors[i].material.GetFloat("_Alpha");
                        //    if (lAlpha < lMaxAlpha)
                        //    {
                        //        lAlpha = Mathf.Clamp(lAlpha + (ProjectorFadeInSpeed * Time.deltaTime), 0f, lMaxAlpha);
                        //        mProjectors[i].material.SetFloat("_Alpha", lAlpha);
                        //    }
                        //}

                        Material lMaterial = mProjectors[i].material;
                        for (int j = 0; j < MATERIAL_COLORS.Length; j++)
                        {
                            if (lMaterial.HasProperty(MATERIAL_COLORS[j]))
                            {
                                lIsAlive = true;

                                Color lColor = lMaterial.GetColor(MATERIAL_COLORS[j]);
                                if (lColor.a < lMaxAlpha)
                                {
                                    lColor.a = Mathf.Clamp(lColor.a + (ProjectorFadeInSpeed * Time.deltaTime), 0f, lMaxAlpha);
                                    mProjectors[i].material.SetColor(MATERIAL_COLORS[j], lColor);
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // Ensure particles are running
                //ParticleSystem[] lParticleSystems = rInstance.GetComponentsInChildren<ParticleSystem>();
                if (mParticleSystems != null)
                {
                    for (int i = 0; i < mParticleSystems.Length; i++)
                    {
                        if (mParticleSystems[i].IsAlive(true))
                        {
                            lIsAlive = true;
                        }
                    }
                }

                // Check if sounds are alive
                AudioSource[] lAudioSources = rInstance.GetComponentsInChildren<AudioSource>();
                for (int i = 0; i < lAudioSources.Length; i++)
                {
                    if (lAudioSources[i].isPlaying && lAudioSources[i].volume > 0f)
                    {
                        if (AudioFadeOutSpeed <= 0f)
                        {
                            lAudioSources[i].volume = 0f;
                        }
                        else
                        {
                            lAudioSources[i].volume = Mathf.Clamp01(lAudioSources[i].volume - (AudioFadeOutSpeed * Time.deltaTime));
                        }

                        if (lAudioSources[i].volume > 0f) { lIsAlive = true; }
                    }
                }

                // Check if lights are alive
                Light[] lLights = rInstance.GetComponentsInChildren<Light>();
                for (int i = 0; i < lLights.Length; i++)
                {
                    if (lLights[i].intensity > 0f)
                    {
                        if (LightFadeOutSpeed <= 0f)
                        {
                            lLights[i].intensity = 0f;
                        }
                        else
                        {
                            lLights[i].intensity = Mathf.Clamp01(lLights[i].intensity - (LightFadeOutSpeed * Time.deltaTime));
                        }

                        if (lLights[i].intensity > 0f) { lIsAlive = true; }
                    }
                }

                // Ensure projectors are running
                //Projector[] lProjectors = rInstance.GetComponentsInChildren<Projector>();
                if (mProjectors != null)
                {
                    for (int i = 0; i < mProjectors.Length; i++)
                    {
                        //if (mProjectors[i].material.HasProperty("_Alpha"))
                        //{
                        //    float lAlpha = mProjectors[i].material.GetFloat("_Alpha");
                        //    if (lAlpha > 0f)
                        //    {
                        //        if (ProjectorFadeOutSpeed <= 0f)
                        //        {
                        //            lAlpha = 0f;
                        //            mProjectors[i].material.SetFloat("_Alpha", lAlpha);
                        //        }
                        //        else
                        //        {
                        //            lAlpha = Mathf.Clamp01(lAlpha - (ProjectorFadeOutSpeed * Time.deltaTime));
                        //            mProjectors[i].material.SetFloat("_Alpha", lAlpha);
                        //        }

                        //        if (lAlpha > 0f) { lIsAlive = true; }
                        //    }
                        //}

                        Material lMaterial = mProjectors[i].material;
                        for (int j = 0; j < MATERIAL_COLORS.Length; j++)
                        {
                            if (lMaterial.HasProperty(MATERIAL_COLORS[j]))
                            {
                                Color lColor = lMaterial.GetColor(MATERIAL_COLORS[j]);
                                if (lColor.a > 0f)
                                {
                                    if (ProjectorFadeOutSpeed <= 0f)
                                    {
                                        lColor.a = 0f;
                                        mProjectors[i].material.SetColor(MATERIAL_COLORS[j], lColor);
                                    }
                                    else
                                    {
                                        lColor.a = Mathf.Clamp01(lColor.a - (ProjectorFadeOutSpeed * Time.deltaTime));
                                        mProjectors[i].material.SetColor(MATERIAL_COLORS[j], lColor);
                                    }

                                    if (lColor.a > 0f) { lIsAlive = true; }
                                }
                            }
                        }
                    }
                }
            }

            return lIsAlive;
        }

        /// <summary>
        /// Updates the position and rotation of the life particles based on criteria
        /// </summary>
        protected virtual void UpdateLifeParticles()
        {
            // If we're using an attractor, pull the particles
            if (_Attractor != null)
            {
                Vector3 lStartPosition = _Transform.position;
                Vector3 lParticleTarget = _Attractor.position + (_Attractor.rotation * _AttractorOffset);
                Vector3 lDirection = (lParticleTarget - lStartPosition).normalized;
                _Transform.rotation = Quaternion.LookRotation(lDirection, Vector3.up);

                lParticleTarget = _Transform.InverseTransformPoint(lParticleTarget);
                lDirection = lParticleTarget.normalized;

                bool lAreLifeParticlesAlive = (mParticleSystems != null && mParticleSystems.Length > 0);
                if (lAreLifeParticlesAlive)
                {
                    lAreLifeParticlesAlive = false;
                    for (int i = 0; i < mParticleSystems.Length; i++)
                    {
                        if (mParticleSystems[i].IsAlive(true))
                        {
                            if (_Attractor != null)
                            {
                                int lParticleCount;

                                // Get the individual particles
                                lParticleCount = mParticleSystems[i].GetParticles(mLifeParticlesArray[i]);
                                if (lParticleCount > 0 && mLifeParticlesArray != null)
                                {
                                    // Update each particle
                                    for (int j = 0; j < lParticleCount; j++)
                                    {
#if UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4
                                        float lTime = (mLifeParticlesArray[i][j].startLifetime - mLifeParticlesArray[i][j].lifetime) / mLifeParticlesArray[i][j].startLifetime;
#else
                                        float lTime = mLifeParticlesArray[i][j].remainingLifetime / mLifeParticlesArray[i][j].startLifetime;
#endif

                                        //mLifeParticlesArray[i][j].velocity = Vector3.zero;
                                        mLifeParticlesArray[i][j].position = Vector3.Lerp(lParticleTarget, Vector3.zero, lTime);
                                    }

                                    mParticleSystems[i].SetParticles(mLifeParticlesArray[i], lParticleCount);
                                }
                            }
                        }
                    }
                }
            }

            // If we're skimming the ground, set the particle placement
            if (_SkimSurface)
            {
                float lDistance = 5f;
                Vector3 lUp = _Transform.up;
                Vector3 lDown = -lUp;
                //Vector3 lForward = _LifeRoot.transform.forward;
                Vector3 lCastOffset = lUp * 1f;
                Vector3 lHitOffset = lUp * _SkimSurfaceDistance;

                bool lAreLifeParticlesAlive = (mParticleSystems != null && mParticleSystems.Length > 0);
                if (lAreLifeParticlesAlive)
                {
                    lAreLifeParticlesAlive = false;
                    for (int i = 0; i < mParticleSystems.Length; i++)
                    {
                        if (mParticleSystems[i].IsAlive(true))
                        {
                            int lParticleCount;

#if UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4
                            bool lIsLocalSpace = (mParticleSystems[i].simulationSpace == ParticleSystemSimulationSpace.Local);
#else
                            bool lIsLocalSpace = (mParticleSystems[i].main.simulationSpace == ParticleSystemSimulationSpace.Local);
#endif

                            // Get the individual particles
                            lParticleCount = mParticleSystems[i].GetParticles(mLifeParticlesArray[i]);
                            if (lParticleCount > 0 && mLifeParticlesArray != null)
                            {
                                // Update each particle
                                for (int j = 0; j < lParticleCount; j++)
                                {
                                    Vector3 lStart = mLifeParticlesArray[i][j].position;
                                    if (lIsLocalSpace) { lStart = _LifeRoot.transform.TransformPoint(lStart); }

                                    lStart = lStart + lCastOffset;

                                    RaycastHit lHitInfo;
#if UNITY_5_1 || UNITY_5_2
                                    if (UnityEngine.Physics.Raycast(lStart, lDown, out lHitInfo, lDistance, _SkimSurfaceLayers))
#else
                                    if (UnityEngine.Physics.Raycast(lStart, lDown, out lHitInfo, lDistance, _SkimSurfaceLayers, QueryTriggerInteraction.Ignore))
#endif
                                    {
                                        Vector3 lPosition = lHitInfo.point + lHitOffset;
                                        if (lIsLocalSpace) { lPosition = _LifeRoot.transform.InverseTransformPoint(lPosition); }
                                        mLifeParticlesArray[i][j].position = lPosition;

#if UNITY_5_1 || UNITY_5_2 || UNITY_5_3 || UNITY_5_4
#else
                                        Quaternion lDeltaRotation = Quaternion.FromToRotation(lHitInfo.normal, Vector3.forward);
                                        Quaternion lRotation = lDeltaRotation * _LifeRoot.transform.rotation;

                                        mLifeParticlesArray[i][j].axisOfRotation = lRotation.Forward();
                                        mLifeParticlesArray[i][j].rotation3D = lRotation.eulerAngles;
#endif
                                    }
                                }

                                mParticleSystems[i].SetParticles(mLifeParticlesArray[i], lParticleCount);
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Releases the GameObject the core is tied to. We'll either send it back
        /// to the pool or destroy it.
        /// </summary>
        public virtual void Release()
        {
            if (OnReleasedEvent != null) { OnReleasedEvent(this, null); }

            if (mLifeInstance != null) { GameObject.Destroy(mLifeInstance); }

            GameObject.Destroy(gameObject);

            mAge = 0f;
            mIsShuttingDown = false;
            OnReleasedEvent = null;
        }
    }
}
