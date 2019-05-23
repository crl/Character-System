using System;
using System.Collections.Generic;
using UnityEngine;
using com.ootii.Data.Serializers;
using com.ootii.Helpers;
using com.ootii.Messages;
using com.ootii.Utilities;

namespace com.ootii.Actors.Attributes
{
    /// <summary>
    /// Delegate for when an attribute's value changes
    /// </summary>
    /// <param name="rAttribute">Attribute with the new value.</param>
    /// <param name="rOldValue">Old value of the attribute.</param>
    public delegate void BasicAttributeValueChangedDelegate(BasicAttribute rAttribute, object rOldValue);

    /// <summary>
    /// Creates a simple attribute system. That is both the "AttributeSource" and the
    /// character attributes.
    /// 
    /// If you use a more advanced attribute system, simply create an "AttributeSource" 
    /// that represents a bridge for your system.
    /// 
    /// I used a dictionary for a cache for performance (1000 gets):
    /// Stored as a List<int> = 0.41 ms
    /// Stored as a List<string> = 0.65 ms
    /// Stored as a Dictionary<int> = 0.10 ms
    /// Stored as a Dictionary<string> = 0.15 ms
    /// Stored as an Array of ints = 1.1 ms
    /// Stored as an Array of strings = 0.81 ms
    /// Stored as HashKeys<int> = 0.09ms
    /// Stored as HashKeys<string> = 0.18 ms
    /// 
    /// </summary>
    public class BasicAttributes : MonoBehaviour, IAttributeSource
    {
        /// <summary>
        /// List of attributes
        /// </summary>
        protected List<BasicAttribute> mItems = new List<BasicAttribute>();
        public List<BasicAttribute> Items
        {
            get { return mItems; }
        }

        /// <summary>
        /// Event for when an attribute value changes
        /// </summary>
        public MessageEvent AttributeValueChangedEvent = null;

        /// <summary>
        /// Event for when the value of an attribute changes
        /// </summary>
        [NonSerialized]
        public BasicAttributeValueChangedDelegate OnAttributeValueChangedEvent = null;

        // Cached used for fast retrieval of the items.
        protected Dictionary<string, BasicAttribute> mItemCache = new Dictionary<string, BasicAttribute>(StringComparer.OrdinalIgnoreCase);

        // Stores the JSON value of our memories since Unity can't put ScriptableObjects in prefabs
        [SerializeField]
        protected List<string> mItemDefinitions = new List<string>();

        /// <summary>
        /// Awake is used to initialize any variables or game state before the game starts.
        /// </summary>
        public void Awake()
        {
            // Deserialize from our definitions
            OnAfterDeserialize();

            // Initialize our cache for fast retrieval
            if (mItemCache == null) { mItemCache = new Dictionary<string, BasicAttribute>(StringComparer.OrdinalIgnoreCase); }
            mItemCache.Clear();

            for (int i = 0; i < mItems.Count; i++)
            {
                mItems[i].Attributes = this;
                mItemCache.Add(mItems[i].ID, mItems[i]);
            }
        }

        /// <summary>
        /// Determines if the attribute exists.
        /// </summary>
        /// <param name="rID">String representing the name or ID of the attribute we're checking</param>
        /// <returns></returns>
        public virtual bool AttributeExists(string rID)
        {
            return mItemCache.ContainsKey(rID);
        }

        /// <summary>
        /// Determines if all the attributes in the comma delimited value exist.
        /// </summary>
        /// <param name="rIDs">Comma delimited list of tags to test for</param>
        /// <param name="rRequireAll">Determines if all must exist or just one</param>
        /// <returns>True or false</returns>
        public virtual bool AttributesExist(string rIDs, bool rRequireAll = true)
        {
            if (rIDs == null || rIDs.Length == 0) { return false; }

            int lCount = StringHelper.Split(rIDs, ',');
            for (int i = lCount - 1; i >= 0; i--)
            {
                StringHelper.SharedStrings[i] = StringHelper.SharedStrings[i].Trim();

                if (mItemCache.ContainsKey(StringHelper.SharedStrings[i]))
                {
                    if (!rRequireAll)
                    {
                        return true;
                    }
                }
                // If it's not found, we may be done
                else if (rRequireAll)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Adds the attribute and returns it.
        /// </summary>
        /// <param name="rID">Attribute ID to add</param>
        /// <param name="rType">Type of attribute we'll add. It must be one of the supported types and null means tag.</param>
        /// <returns>Attribute that matches the ID</returns>
        public virtual BasicAttribute AddAttribute(string rID, Type rType = null)
        {
            // Ensure we have an ID
            if (rID.Length == 0) { rID = "Attribute"; }

            // Ensure we have a unique ID
            string lID = rID;

            int i = 0;
            while (mItemCache.ContainsKey(lID))
            {
                i++;
                lID = rID + " (" + i + ")";
            }

            // Add the attribute 
            BasicAttribute lItem = null;

            Type lType = null;
            int lTypeIndex = EnumAttributeTypes.GetEnum(rType);
            if (lTypeIndex == 0) { lType = typeof(BasicAttributeTag); }
            if (lTypeIndex == 1) { lType = typeof(BasicAttributeString); }
            if (lTypeIndex == 2) { lType = typeof(BasicAttributeFloat); }
            if (lTypeIndex == 3) { lType = typeof(BasicAttributeInt); }
            if (lTypeIndex == 4) { lType = typeof(BasicAttributeBool); }
            if (lTypeIndex == 5) { lType = typeof(BasicAttributeVector2); }
            if (lTypeIndex == 6) { lType = typeof(BasicAttributeVector3); }
            if (lTypeIndex == 7) { lType = typeof(BasicAttributeVector4); }
            if (lTypeIndex == 8) { lType = typeof(BasicAttributeQuaternion); }
            if (lTypeIndex == 9) { lType = typeof(BasicAttributeTransform); }
            if (lTypeIndex == 10) { lType = typeof(BasicAttributeGameObject); }

            if (lType != null)
            {
                lItem = Activator.CreateInstance(lType) as BasicAttribute; // ScriptableObject.CreateInstance(lType) as BasicAttribute;
                lItem.Attributes = this;
                lItem._ID = lID;

                mItems.Add(lItem);
                mItemCache.Add(lID, lItem);
            }

            return lItem;
        }

        /// <summary>
        /// Adds the attribute and returns it.
        /// </summary>
        /// <param name="rID">Attribute ID to add</param>
        /// <param name="rValue">Value to add to the attribute</param>
        /// <returns>Attribute that matches the ID</returns>
        public virtual BasicAttribute AddAttribute<T>(string rID, T rValue = default(T))
        {
            BasicAttribute lItem = AddAttribute(rID, typeof(T));
            lItem.SetValue<T>(rValue);

            return lItem;
        }

        /// <summary>
        /// Renames the attribute and fixes the cache
        /// </summary>
        /// <param name="rOldName">Old name of the attribute to change from.</param>
        /// <param name="rNewName">New name of the attribute to change to.</param>
        public virtual void RenameAttribute(string rOldName, string rNewName)
        {
            if (!mItemCache.ContainsKey(rOldName)) { return; }

            BasicAttribute lItem = mItemCache[rOldName];

            lItem._ID = rNewName;

            mItemCache.Remove(rOldName);
            mItemCache.Add(rNewName, lItem);
        }

        /// <summary>
        /// Returns the attribute associated with the name or ID
        /// </summary>
        /// <param name="rID">String representing the name or ID of the attribute we're checking</param>
        /// <returns>BasicAttribute if one is found or NULL if not</returns>
        public virtual BasicAttribute GetAttribute(string rID)
        {
            if (!mItemCache.ContainsKey(rID)) { return null; }
            return mItemCache[rID];
        }

        /// <summary>
        /// Returns the attribute associated with the name or ID
        /// </summary>
        /// <param name="rID">String representing the name or ID of the attribute we're checking</param>
        /// <typeparam name="T">Type of the BasicAttribute descendant we want.</typeparam>
        /// <returns>BasicAttribute if one is found or NULL if not</returns>
        public virtual T GetAttribute<T>(string rID) where T : BasicAttribute
        {
            if (!mItemCache.ContainsKey(rID)) { return null; }
            return mItemCache[rID] as T;
        }

        /// <summary>
        /// Removes the specified attribute from the list of items
        /// </summary>
        /// <param name="rAttribute"></param>
        public virtual void RemoveAttribute(BasicAttribute rItem)
        {
            if (!mItemCache.ContainsKey(rItem.ID)) { return; }

            rItem.Attributes = null;
            rItem.IsValid = false;

            mItems.Remove(rItem);
            mItemCache.Remove(rItem.ID);
        }

        /// <summary>
        /// Removes the specified attribute from the list of items
        /// </summary>
        /// <param name="rID">String representing the name or ID of the attribute we're removing</param>
        /// <param name="rAttribute"></param>
        public virtual void RemoveAttribute(string rID)
        {
            if (!mItemCache.ContainsKey(rID)) { return; }

            BasicAttribute lItem = mItemCache[rID];

            lItem.Attributes = null;
            lItem.IsValid = false;

            mItems.Remove(lItem);
            mItemCache.Remove(rID);
        }

        /// <summary>
        /// Gets the type of the attribute
        /// </summary>
        /// <param name="rID">ID of the attribute to get the type of</param>
        /// <returns></returns>
        public virtual Type GetAttributeType(string rID)
        {
            if (!mItemCache.ContainsKey(rID)) { return null; }
            return mItemCache[rID].ValueType;
        }

        /// <summary>
        /// Returns the attribute value as the specified type
        /// </summary>
        /// <typeparam name="T">Type that we're expecting to get back</typeparam>
        /// <param name="rID">Attribute that we're looking for</param>
        /// <param name="rDefault">Default value if the attribute isn't found</param>
        /// <returns>Attribute value as the specified type</returns>
        public virtual T GetAttributeValue<T>(string rID, T rDefault = default(T))
        {
            if (!mItemCache.ContainsKey(rID)) { return rDefault; }
            return mItemCache[rID].GetValue<T>();
        }

        /// <summary>
        /// Given the specified attribute, set the value associated with the attribute
        /// </summary>
        /// <typeparam name="T">Type of attribute to set</typeparam>
        /// <param name="rAttributeID">String representing the name or ID of the item we want</param>
        /// <param name="rValue">value to set on the attribute</param>
        public virtual void SetAttributeValue<T>(string rID, T rValue)
        {
            // Add the attribute
            if (!mItemCache.ContainsKey(rID)) { AddAttribute(rID, typeof(T)); }
            mItemCache[rID].SetValue<T>(rValue);
        }

        /// <summary>
        /// Due to Unity's serialization limits, we have to do this ourselves:
        /// 1. GameObjects don't allow for lists of derived types
        /// 2. You can use ScriptableObjects, but prefabs don't support ScriptableObjects
        /// 3. ISerializationCallbackReceiver can't use Find()
        /// 
        /// This function is called BEFORE the items are serialized
        /// </summary>
        public void OnBeforeSerialize()
        {
            JSONSerializer.RootObject = gameObject;

            // Replace the definitions 
            for (int i = 0; i < mItems.Count; i++)
            {
                string lJSON = JSONSerializer.Serialize(mItems[i], false);

                if (mItemDefinitions.Count > i) { mItemDefinitions[i] = lJSON; }
                else { mItemDefinitions.Add(lJSON); }

                //com.ootii.Utilities.Debug.Log.FileWrite(lJSON);
            }

            // Remove excess definitions
            for (int i = mItemDefinitions.Count - 1; i >= mItems.Count; i--)
            {
                mItemDefinitions.RemoveAt(i);
            }

            JSONSerializer.RootObject = null;
        }

        /// <summary>
        /// Due to Unity's serialization limits, we have to do this ourselves:
        /// 1. GameObjects don't allow for lists of derived types
        /// 2. You can use ScriptableObjects, but prefabs don't support ScriptableObjects
        /// 3. ISerializationCallbackReceiver can't use Find()
        /// 
        /// This function is called AFTER the items have been deserialized
        /// </summary>
        public void OnAfterDeserialize()
        {
            JSONSerializer.RootObject = gameObject;

            for (int i = 0; i < mItemDefinitions.Count; i++)
            {
                Type lType = null;

                JSONNode lNode = JSONNode.Parse(mItemDefinitions[i]);
                if (lNode != null)
                {
                    string lTypeString = lNode["__Type"].Value;
                    if (lTypeString != null && lTypeString.Length > 0) { lType = Type.GetType(lTypeString); }
                }

                // Fill an existing existing attribute
                if (lType != null && mItems.Count > i && mItems[i].GetType() == lType)
                {
                    object lItemObj = mItems[i];
                    JSONSerializer.DeserializeInto(mItemDefinitions[i], ref lItemObj);
                }
                // Create a new attribute
                else
                {
                    BasicAttribute lAttribute = JSONSerializer.Deserialize(mItemDefinitions[i]) as BasicAttribute;
                    if (lAttribute != null)
                    {
                        if (mItems.Count <= i) { mItems.Add(lAttribute); }
                        else { mItems[i] = lAttribute; }
                    }
                }
            }

            // Get rid of excess items
            for (int i = mItems.Count - 1; i > mItemDefinitions.Count - 1; i--)
            {
                mItems.RemoveAt(i);
            }

            JSONSerializer.RootObject = null;
        }

        // **************************************************************************************************
        // Following properties and function only valid while editing
        // **************************************************************************************************

#if UNITY_EDITOR

        /// <summary>
        /// Allows us to re-open the last selected motor
        /// </summary>
        public int EditorItemIndex = 0;

        /// <summary>
        /// Show the events section in the editor
        /// </summary>
        public bool EditorShowEvents = false;

#endif
    }
}
