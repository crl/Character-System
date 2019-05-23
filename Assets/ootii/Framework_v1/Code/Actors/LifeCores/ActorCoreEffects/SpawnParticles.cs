using UnityEngine;
using com.ootii.Collections;

namespace com.ootii.Actors.LifeCores
{
    /// <summary>
    /// Instantiates a particle system and adds it to the character
    /// </summary>
    public class SpawnParticles : ActorCoreEffect
    {
        /// <summary>
        /// Particle core that actually plays the particles and expires
        /// </summary>
        protected ParticleCore mParticleCore = null;

        /// <summary>
        /// Current age of the effect
        /// </summary>
        public override float Age
        {
            get { return mAge; }
            
            set
            {
                mAge = value;
                if (mParticleCore != null) { mParticleCore.Age = value; }
            }
        }

        /// <summary>
        /// Max age of the effect
        /// </summary>
        public override float MaxAge
        {
            get { return _MaxAge; }

            set
            {
                _MaxAge = value;
                if (mParticleCore != null) { mParticleCore.MaxAge = value; }
            }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public SpawnParticles() : base()
        {
        }

        /// <summary>
        /// ActorCore constructor
        /// </summary>
        public SpawnParticles(ActorCore rActorCore) : base(rActorCore)
        {
            mActorCore = rActorCore;
        }

        /// <summary>
        /// Sets the message that will be run each time damage should be processed
        /// </summary>
        /// <param name="rMessage">Message containing information about the damage</param>
        /// <param name="rTriggerDelay">Time in seconds between triggering</param>
        /// <param name="rMaxAge">Max amount of time the effect can last</param>
        public virtual void Activate(float rMaxAge, GameObject rPrefab, Transform rParent = null)
        {
            if (rPrefab == null) { return; }

            GameObject lInstance = GameObject.Instantiate(rPrefab);
            if (lInstance != null)
            {
                mParticleCore = lInstance.GetComponent<ParticleCore>();
                if (mParticleCore != null)
                {
                    lInstance.transform.parent = (rParent != null ? rParent : mActorCore.Transform);
                    lInstance.transform.localPosition = Vector3.zero;
                    lInstance.transform.localRotation = Quaternion.identity;

                    mParticleCore.Age = 0f;
                    mParticleCore.MaxAge = rMaxAge;
                    mParticleCore.Prefab = rPrefab;
                    mParticleCore.OnReleasedEvent = OnParticleCoreReleased;
                    mParticleCore.Play();
                    base.Activate(0f, rMaxAge);                
                }
                else
                {
                    GameObject.Destroy(lInstance);
                }
            }
        }

        /// <summary>
        /// Called when the effect is meant to be deactivated
        /// </summary>
        public override void Deactivate()
        {
            if (mParticleCore == null)
            {
                base.Deactivate();
            }
            else
            {
                mParticleCore.Stop();
            }
        }

        /// <summary>
        /// Called each frame that the effect is active
        /// </summary>
        /// <returns>Boolean that determines if the effect is still active or not</returns>
        public override bool Update()
        {
            if (mParticleCore == null) { return false; }

            return true;
        }

        /// <summary>
        /// Called when the particle core has finished running and is released
        /// </summary>
        /// <param name="rCore"></param>
        private void OnParticleCoreReleased(ILifeCore rCore, object rUserData = null)
        {
            if (mParticleCore != null)
            {
                mParticleCore.OnReleasedEvent = null;
                mParticleCore = null;
            }

            base.Deactivate();
        }

        /// <summary>
        /// Releases the effect as an allocation
        /// </summary>
        public override void Release()
        {
            SpawnParticles.Release(this);
        }

        #region Editor Functions

#if UNITY_EDITOR

        /// <summary>
        /// Called when the inspector needs to draw
        /// </summary>
        public override bool OnInspectorGUI(UnityEngine.Object rTarget)
        {
            bool lIsDirty = base.OnInspectorGUI(rTarget);

            return lIsDirty;
        }

#endif

        #endregion

        // ******************************** OBJECT POOL ********************************

        /// <summary>
        /// Allows us to reuse objects without having to reallocate them over and over
        /// </summary>
        private static ObjectPool<SpawnParticles> sPool = new ObjectPool<SpawnParticles>(5, 5);

        /// <summary>
        /// Pulls an object from the pool.
        /// </summary>
        /// <returns></returns>
        public static SpawnParticles Allocate()
        {
            SpawnParticles lInstance = sPool.Allocate();
            return lInstance;
        }

        /// <summary>
        /// Returns an element back to the pool.
        /// </summary>
        /// <param name="rEdge"></param>
        public static void Release(SpawnParticles rInstance)
        {
            if (rInstance == null) { return; }

            rInstance.Clear();
            sPool.Release(rInstance);
        }
    }
}
