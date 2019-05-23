using System;
using System.Collections.Generic;
using UnityEngine;
using com.ootii.Collections;
using com.ootii.Geometry;

namespace com.ootii.Actors.LifeCores
{
    /// <summary>
    /// Decale cores give us a way to manage a decals for a limited amount of time.
    /// </summary>
    public class DecalCore : MonoBehaviour, ILifeCore
    {
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
        /// Amount of time to keep the particles active for before terminating
        /// </summary>
        public float _MaxAge = 5f;
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
        /// Awake is called when the script instance is being loaded.
        /// </summary>
        public virtual void Activate()
        {
            mAge = 0f;

            AudioSource lAudioSource = gameObject.GetComponent<AudioSource>();
            if (lAudioSource != null)
            {
                lAudioSource.Play();
            }
        }

        /// <summary>
        /// Update is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        public virtual void Update()
        {
            mAge = mAge + Time.deltaTime;

            // If they have all stopped, we can destory
            if (mAge >= _MaxAge)
            {
                Release();
            }
        }

        /// <summary>
        /// Releases the GameObject the core is tied to. We'll either send it back
        /// to the pool or destroy it.
        /// </summary>
        public virtual void Release()
        {
            if (mPrefab != null)
            {
                GameObjectPool.Release(mPrefab, gameObject);
            }
            else
            {
                GameObject.Destroy(gameObject);
            }
        }
    }
}
