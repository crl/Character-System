using System;
using System.Reflection;
using UnityEngine;

namespace com.ootii.Geometry
{
    /// <summary>
    /// Extension for the standard GameObject
    /// </summary>
    public static class GameObjectExt
    {
        ///// <summary>
        ///// Searches up the ancestory chain to find the first instance of the component
        ///// </summary>
        ///// <returns>First component found or null</returns>
        //public static object GetComponentInAncestors(this GameObject rThis, Type rType)
        //{
        //    Transform lParent = rThis.transform;
        //    while (lParent != null)
        //    {
        //        Component lComponent = lParent.gameObject.GetComponent(rType);
        //        if (lComponent != null) { return lComponent; }

        //        lParent = lParent.parent;
        //    }

        //    return null;
        //}

        /// <summary>
        /// Determines if this game object is in a chain where one of its parents
        /// is the specified parent.
        /// </summary>
        /// <param name="rThis">Child game object whose relationship we're testing.</param>
        /// <param name="rParent">Parent game object we want to see if the child belongs to.</param>
        /// <returns>True if transform is a child (or in the child chain) of the parent.</returns>
        public static bool IsChildOf(this GameObject rThis, GameObject rParent)
        {
            if (rThis == null) { return false; }
            if (rParent == null) { return false; }

            Transform lSearchParent = rParent.transform;

            Transform lParent = rThis.transform;
            while (lParent != null)
            {
                if (lParent == lSearchParent) { return true; }

                lParent = lParent.parent;
            }

            return false;
        }

        /// <summary>
        /// Looks for the specified component in the heirarchy chain
        /// </summary>
        /// <param name="rThis">GameObject we're starting with</param>
        public static object GetComponentInParents(this GameObject rThis, Type rType)
        {
            if (rThis == null) { return null; }

            Transform lParent = rThis.transform;
            while (lParent != null)
            {
                Component lComponent = lParent.gameObject.GetComponent(rType);
                if (lComponent != null) { return lComponent; }

                lParent = lParent.parent;
            }

            return null;
        }

        /// <summary>
        /// Looks for the specified component in the heirarchy chain
        /// </summary>
        /// <param name="rThis">GameObject we're starting with</param>
        public static T GetComponentInParents<T>(this GameObject rThis) where T : Component
        {
            if (rThis == null) { return null; }

            Transform lParent = rThis.transform;
            while (lParent != null)
            {
                T lComponent = lParent.GetComponent<T>();
                if (lComponent != null) { return lComponent; }

                lParent = lParent.parent;
            }

            return null;
        }

        /// <summary>
        /// Creates a copy of a component and returns it
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="comp"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        public static T GetCopyOf<T>(this Component rThis, T rOther) where T : Component
        {
            Type lType = rThis.GetType();
            if (lType != rOther.GetType()) return null; // type mis-match

#if !UNITY_EDITOR && (NETFX_CORE || WINDOWS_UWP || UNITY_WP8 || UNITY_WP_8_1 || UNITY_WSA || UNITY_WSA_8_0 || UNITY_WSA_8_1 || UNITY_WSA_10_0)
            BindingFlags lFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.DeclaredOnly;
#else
            BindingFlags lFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Default | BindingFlags.DeclaredOnly;
#endif

            PropertyInfo[] lPropertyInfos = lType.GetProperties(lFlags);
            foreach (var lPropertyInfo in lPropertyInfos)
            {
                if (lPropertyInfo.CanWrite)
                {
                    try
                    {
                        lPropertyInfo.SetValue(rThis, lPropertyInfo.GetValue(rOther, null), null);
                    }
                    catch { } // In case of NotImplementedException being thrown. For some reason specifying that exception didn't seem to catch it, so I didn't catch anything specific.
                }
            }

            FieldInfo[] lFieldInfos = lType.GetFields(lFlags);
            foreach (var lFieldInfo in lFieldInfos)
            {
                lFieldInfo.SetValue(rThis, lFieldInfo.GetValue(rOther));
            }

            return rThis as T;
        }
    }
}
