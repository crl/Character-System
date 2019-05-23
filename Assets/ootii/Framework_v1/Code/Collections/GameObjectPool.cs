using System;
using System.Collections.Generic;
using UnityEngine;

namespace com.ootii.Collections
{
	/// <summary>
	/// Represents a pool of GameObjects that we can pull from in order
	/// to prevent constantly reallocating new objects. This collection
	/// is meant to be fast, so we limit the "lock" that we use and do not
	/// track the instances that we hand out.
    /// 
    /// GameObjects use a different pool that ObjectPool since all GameObjects
    /// are still of type "GameObject". It's the components that are different.
    /// 
    /// GameObject instances are deep copies (unlike ScriptableObjects)
	/// </summary>
	public sealed class GameObjectPool
	{
		/// <summary>
		/// Number of items to grow the array by if needed
		/// </summary>
		private int mGrowSize = 5;

        /// <summary>
        /// Template from which we'll build the pool
        /// </summary>
        private GameObject mTemplate = null;

		/// <summary>
		/// Pool objects
		/// </summary>
		private PooledObject[] mPool;
		
		/// <summary>
		/// Initializes a new instance of the ObjectPool class.
		/// </summary>
        /// <param name="rTemplate">Template game object we'll use to instantiate our elements with.</param>
		/// <param name="size">The size of the object pool.</param>
		public GameObjectPool(GameObject rTemplate, int rSize)
		{
            mTemplate = rTemplate;

			// Initialize the pool
			Resize(rSize, false);
		}

        /// <summary>
        /// Initializes a new instance of the ObjectPool class.
        /// </summary>
        /// <param name="rTemplate">Template game object we'll use to instantiate our elements with.</param>
        /// <param name="rSize">The initial size of the object pool.</param>
        /// <param name="rGrowize">Increment to grow the pool by when needed</param>
        public GameObjectPool(GameObject rTemplate, int rSize, int rGrowSize)
		{
            mTemplate = rTemplate;
			mGrowSize = rGrowSize;
			
			// Initialize the pool
			Resize(rSize, false);
		}

		/// <summary>
		/// The total size of the pool
		/// </summary>
		/// <value>The length.</value>
		public int Length
		{
			get { return mPool.Length; }
		}
		
		/// <summary>
		/// Pulls an item from the object pool or creates more
		/// if needed.
		/// </summary>
		/// <returns>Object of the specified type</returns>
		public GameObject Allocate()
		{
            GameObject lInstance = null;

            // Find a released GameObject
            for (int i = mPool.Length - 1; i >= 0; i--)
            {
                if (mPool[i].IsReleased)
                {
                    mPool[i].IsReleased = false;
                    lInstance = mPool[i].GameObject;

                    break;
                }
            }
			
			// Creates extra items if needed
     		if (lInstance == null && mGrowSize > 0)
			{
                int lCurrentLength = mPool.Length;
				Resize(lCurrentLength + mGrowSize, true);

                mPool[lCurrentLength].IsReleased = false;
                lInstance = mPool[lCurrentLength].GameObject;
			}

            // Activate the instance
            if (lInstance != null)
            {
                lInstance.SetActive(true);
            }
			
			return lInstance;
		}
		
		/// <summary>
		/// Sends an item back to the pool.
		/// </summary>
		/// <param name="rInstance">Object to return</param>
		public void Release(GameObject rInstance)
		{
            if (rInstance == null) { return; }

            for (int i = mPool.Length - 1; i >= 0; i--)
            {
                if (object.ReferenceEquals(mPool[i].GameObject, rInstance))
                {
                    rInstance.SetActive(false);
                    rInstance.hideFlags = HideFlags.HideInHierarchy;

                    rInstance.transform.parent = null;

                    mPool[i].IsReleased = true;
                    return;
                }
            }
		}
		
		/// <summary>
		/// Rebuilds the pool with new instances
		/// 
		/// Note:
		/// This is a fast pool so we don't track the instances
		/// that are handed out. Releasing an instance also overwrites
		/// what was there. That means we can't have a "ReleaseAll"
		/// function that allows the array to be used again. The best
		/// we can do is abandon what we have given out and rebuild all our instances.
		/// </summary>
		/// <param name="rInstance">Object to return</param>
		public void Reset()
		{
			// Determine the length to initialize
			int lLength = mGrowSize;
			if (mPool != null) { lLength = mPool.Length; }
			
			// Rebuild our elements
			Resize(lLength, false);
		}
		
		/// <summary>
		/// Resize the pool array
		/// </summary>
		/// <param name="rSize">New size of the pool</param>
		/// <param name="rCopyExisting">Determines if we copy contents from the old pool</param>
		public void Resize(int rSize, bool rCopyExisting)
		{
			lock(this)
			{
				int lCount = 0;
				
				// Build the new array and copy the contents
				PooledObject[] lNewPool = new PooledObject[rSize];
				
				if (mPool != null && rCopyExisting)
				{
					lCount = mPool.Length;
					Array.Copy(mPool, lNewPool, Math.Min(lCount, rSize));
				}
				
				// Allocate items in the new array
				for (int i = lCount; i < rSize; i++)
				{
                    GameObject lInstance = GameObject.Instantiate(mTemplate);
                    lInstance.SetActive(false);
                    lInstance.hideFlags = HideFlags.HideInHierarchy;

                    lNewPool[i] = new PooledObject();
					lNewPool[i].GameObject = lInstance;
                    lNewPool[i].IsReleased = true;
				}
				
				// Replace the old array
				mPool = lNewPool;
			}
		}

        /// <summary>
        /// Tracks all the objects in our pool
        /// </summary>
        private struct PooledObject
        {
            public bool IsReleased;

            public GameObject GameObject;
        }

        #region Static Functions

        // ****************************** Static Functions ******************************

        /// <summary>
        /// Tracks all the template pools
        /// </summary>
        private static Dictionary<GameObject, GameObjectPool> sPool = new Dictionary<GameObject, GameObjectPool>();

        /// <summary>
        /// Allocates a game object from the specified pool
        /// </summary>
        /// <param name="rPrefab">GameObject that is our template</param>
        /// <returns></returns>
        public static void Initialize(GameObject rPrefab)
        {
            if (rPrefab == null) { return; }

            if (!sPool.ContainsKey(rPrefab))
            {
                GameObjectPool lPool = new GameObjectPool(rPrefab, 5);
                sPool.Add(rPrefab, lPool);
            }
        }

        /// <summary>
        /// Allocates a game object from the specified pool
        /// </summary>
        /// <param name="rPrefab">GameObject that is our template</param>
        /// <returns></returns>
        public static GameObject Allocate(GameObject rPrefab)
        {
            if (rPrefab == null) { return null; }

            if (!sPool.ContainsKey(rPrefab))
            {
                GameObjectPool lPool = new GameObjectPool(rPrefab, 5);
                sPool.Add(rPrefab, lPool);
            }

            return sPool[rPrefab].Allocate();
        }

        /// <summary>
        /// Release the instance to the specified pool
        /// </summary>
        /// <param name="rPrefab">GameObject that specifies the pool</param>
        /// <param name="rGameObject">GameObject instance to release</param>
        public static void Release(GameObject rPrefab, GameObject rGameObject)
        {
            if (rPrefab == null || rGameObject == null) { return; }

            if (sPool.ContainsKey(rPrefab))
            {
                sPool[rPrefab].Release(rGameObject);
            }
        }

        #endregion
    }
}
