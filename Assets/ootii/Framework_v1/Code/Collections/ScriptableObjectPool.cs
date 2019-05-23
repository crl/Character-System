using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using com.ootii.Helpers;

namespace com.ootii.Collections
{
    /// <summary>
    /// Represents a pool of ScriptableObject that we can pull from in order
    /// to prevent constantly reallocating new objects. This collection
    /// is meant to be fast, so we limit the "lock" that we use and do not
    /// track the instances that we hand out.
    /// 
    /// ScriptableObject use a different pool that ObjectPool since all ScriptableObjects
    /// are still of type "ScriptableObject". 
    /// </summary>
    public sealed class ScriptableObjectPool
	{
		/// <summary>
		/// Number of items to grow the array by if needed
		/// </summary>
		private int mGrowSize = 5;

        /// <summary>
        /// Template from which we'll build the pool
        /// </summary>
        private ScriptableObject mTemplate = null;

		/// <summary>
		/// Pool objects
		/// </summary>
		private PooledObject[] mPool;

        /// <summary>
        /// Determines if we're doing a shallow or deep copy
        /// </summary>
        private bool mDeepCopy = true;
		
		/// <summary>
		/// Initializes a new instance of the ObjectPool class.
		/// </summary>
        /// <param name="rTemplate">Template game object we'll use to instantiate our elements with.</param>
		/// <param name="size">The size of the object pool.</param>
		public ScriptableObjectPool(ScriptableObject rTemplate, int rSize, bool rDeepCopy)
		{
            mDeepCopy = rDeepCopy;
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
        public ScriptableObjectPool(ScriptableObject rTemplate, int rSize, int rGrowSize, bool rDeepCopy)
		{
            mDeepCopy = rDeepCopy;
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
		public ScriptableObject Allocate()
		{
            ScriptableObject lInstance = null;

            // Find a released GameObject
            for (int i = mPool.Length - 1; i >= 0; i--)
            {
                if (mPool[i].IsReleased)
                {
                    mPool[i].IsReleased = false;
                    lInstance = mPool[i].ScriptableObject;

                    break;
                }
            }
			
			// Creates extra items if needed
     		if (lInstance == null && mGrowSize > 0)
			{
                int lCurrentLength = mPool.Length;
				Resize(lCurrentLength + mGrowSize, true);

                mPool[lCurrentLength].IsReleased = false;
                lInstance = mPool[lCurrentLength].ScriptableObject;
			}

            // Show the instance
            if (lInstance != null)
            {
                lInstance.hideFlags = HideFlags.None;
            }
			
			return lInstance;
		}
		
		/// <summary>
		/// Sends an item back to the pool.
		/// </summary>
		/// <param name="rInstance">Object to return</param>
		public void Release(ScriptableObject rInstance)
		{
            if (rInstance == null) { return; }

            for (int i = mPool.Length - 1; i >= 0; i--)
            {
                if (object.ReferenceEquals(mPool[i].ScriptableObject, rInstance))
                {
                    rInstance.hideFlags = HideFlags.HideInHierarchy;

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
                    ScriptableObject lInstance = null;

                    if (mDeepCopy)
                    {
                        lInstance = DeepCopy(mTemplate, true);
                    }
                    else
                    {
                        lInstance = ScriptableObject.Instantiate(mTemplate);
                    }

                    lInstance.hideFlags = HideFlags.HideInHierarchy;

                    lNewPool[i] = new PooledObject();
					lNewPool[i].ScriptableObject = lInstance;
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

            public ScriptableObject ScriptableObject;
        }

        #region Static Functions

        // ****************************** Static Functions ******************************

        /// <summary>
        /// Tracks all the template pools
        /// </summary>
        private static Dictionary<ScriptableObject, ScriptableObjectPool> sPool = new Dictionary<ScriptableObject, ScriptableObjectPool>();

        /// <summary>
        /// Holds deep copies of the first item we find. This way, we can reference the copies
        /// </summary>
        private static Dictionary<ScriptableObject, ScriptableObject> sDeepCopyRegister = new Dictionary<ScriptableObject, ScriptableObject>();

        /// <summary>
        /// Allocates a game object from the specified pool
        /// </summary>
        /// <param name="rPrefab">GameObject that is our template</param>
        /// <returns></returns>
        public static void Initialize(ScriptableObject rPrefab, bool rDeepCopy)
        {
            if (rPrefab == null) { return; }

            if (!sPool.ContainsKey(rPrefab))
            {
                ScriptableObjectPool lPool = new ScriptableObjectPool(rPrefab, 5, rDeepCopy);
                sPool.Add(rPrefab, lPool);
            }
        }

        /// <summary>
        /// Allocates a game object from the specified pool
        /// </summary>
        /// <param name="rPrefab">GameObject that is our template</param>
        /// <returns></returns>
        public static ScriptableObject Allocate(ScriptableObject rPrefab, bool rDeepCopy)
        {
            if (rPrefab == null) { return null; }

            if (!sPool.ContainsKey(rPrefab))
            {
                ScriptableObjectPool lPool = new ScriptableObjectPool(rPrefab, 5, rDeepCopy);
                sPool.Add(rPrefab, lPool);
            }

            return sPool[rPrefab].Allocate();
        }

        /// <summary>
        /// Release the instance to the specified pool
        /// </summary>
        /// <param name="rPrefab">GameObject that specifies the pool</param>
        /// <param name="rGameObject">GameObject instance to release</param>
        public static void Release(ScriptableObject rPrefab, ScriptableObject rInstance)
        {
            if (rPrefab == null || rInstance == null) { return; }

            if (sPool.ContainsKey(rPrefab))
            {
                sPool[rPrefab].Release(rInstance);
            }
        }

        /// <summary>
        /// Provides a deep copy of the ScriptableObject. This is important when
        /// properties of the ScriptableObject are ScriptableObjects themselves.
        /// </summary>
        /// <param name="rTemplate">ScriptableObject we are copying</param>
        /// <returns>Cloned ScriptableObject whose ScriptableObject fields are also cloned</returns>
        public static ScriptableObject DeepCopy(ScriptableObject rTemplate, bool rRoot = false)
        {
            if (rTemplate == null) { return null; }

            if (rRoot) { sDeepCopyRegister.Clear(); }

            // If we've already copied this object, use it
            if (sDeepCopyRegister.ContainsKey(rTemplate))
            {
                return sDeepCopyRegister[rTemplate];
            }

            // Create an instance and store the copy
            ScriptableObject lInstance = ScriptableObject.Instantiate(rTemplate);
            if (rRoot)
            {
                sDeepCopyRegister.Add(lInstance, lInstance);
            }
            else
            {
                sDeepCopyRegister.Add(rTemplate, lInstance);
            }

            // For other ScriptObjects, we want to keep the references internal to the first deep copy we make
            FieldInfo[] lFields = lInstance.GetType().GetFields();
            foreach (FieldInfo lField in lFields)
            {
                if (lField.IsInitOnly) { continue; }
                if (lField.IsLiteral) { continue; }
                if (!lField.IsPublic) { continue; }

                //object[] lAttributes = lField.GetCustomAttributes(typeof(System.NonSerializedAttribute), true);
                //if (lAttributes != null && lAttributes.Length > 0) { continue; }
                if (!ReflectionHelper.IsDefined(lField, typeof(System.NonSerializedAttribute))) { continue; }

                Type lFieldType = lField.FieldType;
                object lFieldValue = lField.GetValue(lInstance);

                // Clone the ScriptableObject field
                if (typeof(ScriptableObject).IsAssignableFrom(lFieldType))
                {
                    ScriptableObject lScriptableObjectValue = lFieldValue as ScriptableObject;
                    ScriptableObject lNewValue = DeepCopy(lScriptableObjectValue);

                    lField.SetValue(lInstance, lNewValue);
                }
                // Clone the ScriptableObject list
                //else if (lFieldType.IsGenericType && lFieldType.GetGenericTypeDefinition() == typeof(List<>))
                else if (ReflectionHelper.IsGenericType(lFieldType) && lFieldType.GetGenericTypeDefinition() == typeof(List<>))
                {
                    Type lElementType = lFieldType.GetGenericArguments()[0];
                    if (typeof(ScriptableObject).IsAssignableFrom(lElementType))
                    {
                        IList lListValue = lFieldValue as IList;
                        IList lNewListValue = Activator.CreateInstance(lFieldType) as IList;

                        for (int i = 0; i < lListValue.Count; i++)
                        {
                            ScriptableObject lScriptableObjectValue = lListValue[i] as ScriptableObject;
                            ScriptableObject lNewValue = DeepCopy(lScriptableObjectValue);

                            lNewListValue.Add(lNewValue);
                        }

                        lField.SetValue(lInstance, lNewListValue);
                    }
                }
            }

            if (rRoot) { sDeepCopyRegister.Clear(); }

            return lInstance;
        }

        #endregion
    }
}
