using System;
using System.Collections;
using System.Reflection;
using System.Text;
using UnityEngine;
using com.ootii.Geometry;
using com.ootii.Helpers;
using com.ootii.Utilities;
using com.ootii.Utilities.Debug;

namespace com.ootii.Data.Serializers
{
    /// <summary>
    /// Helper class that provides support for basic JSON serialization and
    /// deserialization.
    /// </summary>
    public class JSONSerializer
    {
        // ID to identify the root object
        public const string RootObjectID = "[OOTII_ROOT]";

        /// <summary>
        /// Root object that will be the reference point for serializing/deserializing GameObjects and
        /// Transforms. This is to help support prefabs.
        /// </summary>
        public static GameObject RootObject = null;

        /// <summary>
        /// Serialize the object into a basic JSON string. This function isnt meant to be
        /// super robust. It provides simple serialization.
        /// </summary>
        /// <param name="rObject">Object to serialize</param>
        /// <returns>String that represents the serialized version of the object</returns>
        public static string Serialize(object rObject, bool rIncludeProperties)
        {
            if (rObject == null) { return ""; }

            StringBuilder lJSON = new StringBuilder();
            lJSON.Append("{");
            lJSON.Append("__Type");
            lJSON.Append(" : ");
            lJSON.Append("\"");
            lJSON.Append(rObject.GetType().AssemblyQualifiedName);
            lJSON.Append("\"");

            //if (rObject.GetType().IsPrimitive)
            if (ReflectionHelper.IsPrimitive(rObject.GetType()))
            {
                string lValueString = SerializeValue(rObject);
                lJSON.Append(", ");
                lJSON.Append("__Value");
                lJSON.Append(" : ");
                lJSON.Append(lValueString);
            }
            else if (rObject.GetType() == typeof(string))
            {
                string lValueString = SerializeValue(rObject);
                lJSON.Append(", ");
                lJSON.Append("__Value");
                lJSON.Append(" : ");
                lJSON.Append(lValueString);
            }
            else
            {
                // We don't always include properties because sometimes this can cause
                // recursion. So, we'll only do this selectively.
                if (rIncludeProperties)
                {
                    // Cycle through all the properties and serialize what we know about
                    PropertyInfo[] lProperties = rObject.GetType().GetProperties();
                    foreach (PropertyInfo lProperty in lProperties)
                    {
                        if (!lProperty.CanRead) { continue; }
                        if (!lProperty.CanWrite) { continue; }

                        //object[] lAttributes = lProperty.GetCustomAttributes(typeof(SerializationIgnoreAttribute), true);
                        //if (lAttributes != null && lAttributes.Length > 0) { continue; }
                        if (ReflectionHelper.IsDefined(lProperty, typeof(SerializationIgnoreAttribute))) { continue; }

                        // Store the field
                        string lName = lProperty.Name;

                        // Grab the value
                        object lValue = null;

                        try
                        {
                            lValue = lProperty.GetValue(rObject, null);
                            if (lValue == null) { continue; }
                        }
                        catch
                        {
                            lValue = rObject;
                        }

                        // Serialize the field and add it
                        string lValueString = SerializeValue(lValue);
                        lJSON.Append(", ");
                        lJSON.Append(lName);
                        lJSON.Append(" : ");
                        lJSON.Append(lValueString);
                    }
                }

                // Cycle through all the fields and serialize what we know about
                FieldInfo[] lFields = rObject.GetType().GetFields();
                foreach (FieldInfo lField in lFields)
                {
                    if (lField.IsInitOnly) { continue; }
                    if (lField.IsLiteral) { continue; }

                    //object[] lAttributes = lField.GetCustomAttributes(typeof(System.NonSerializedAttribute), true);
                    //if (lAttributes != null && lAttributes.Length > 0) { continue; }
                    if (ReflectionHelper.IsDefined(lField, typeof(NonSerializedAttribute))) { continue; }

                    // If we're not serializing, we need to move on.
                    // NOTE: None of this really matters right now since Unity isn't supporting BindingFlags 
                    // in the GetFields() call. If you add any flags, you always get back an empty array. So,
                    // any fields to be serialized must be public (for now).
                    if (!lField.IsPublic)
                    {
                        //lAttributes = lField.GetCustomAttributes(typeof(SerializeField), true);
                        //if (lAttributes == null || lAttributes.Length == 0)
                        //{
                        //    lAttributes = lField.GetCustomAttributes(typeof(SerializableAttribute), true);
                        //    if (lAttributes == null || lAttributes.Length == 0) { continue; }
                        //}

                        if (!ReflectionHelper.IsDefined(lField, typeof(SerializeField)))
                        {
                            if (!ReflectionHelper.IsDefined(lField, typeof(SerializableAttribute))) { continue; }
                        }
                    }

                    // Store the field
                    string lName = lField.Name;

                    // Grab the value
                    object lValue = lField.GetValue(rObject);
                    if (lValue == null) { continue; }

                    // Serialize the field and add it
                    string lValueString = SerializeValue(lValue);
                    lJSON.Append(", ");
                    lJSON.Append(lName);
                    lJSON.Append(" : ");
                    lJSON.Append(lValueString);
                }
            }

            lJSON.Append("}");

            return lJSON.ToString();
        }

        /// <summary>
        /// Serializes an object as an individual property. 
        /// </summary>
        /// <param name="rName">Name representing this property</param>
        /// <param name="rValue">Value of the property</param>
        /// <returns>JSON string representing the property</returns>
        public static string SerializeValue(string rName, object rValue)
        {
            if (rValue == null) { return ""; }

            StringBuilder lJSON = new StringBuilder();
            lJSON.Append("{");
            lJSON.Append(rName);
            lJSON.Append(" : ");
            lJSON.Append(SerializeValue(rValue));
            lJSON.Append("}");

            return lJSON.ToString();
        }

        /// <summary>
        /// Gets the type of object from the JSON string
        /// </summary>
        /// <param name="rJSON"></param>
        /// <returns></returns>
        public static Type GetType(string rJSON)
        {
            JSONNode lNode = JSONNode.Parse(rJSON);
            if (lNode == null) { return null; }

            string lTypeString = lNode["__Type"].Value;
            if (lTypeString == null || lTypeString.Length == 0) { return null; }

            return Type.GetType(lTypeString);
        }

        /// <summary>
        /// Deserialize to an object of the specified type
        /// </summary>
        /// <param name="rType"></param>
        /// <param name="rJSON"></param>
        /// <returns></returns>
        public static T DeserializeValue<T>(string rJSON)
        {
            Type lType = typeof(T);

            if (rJSON == null || rJSON.Length == 0) { return default(T); }

            JSONNode lNode = JSONNode.Parse(rJSON);
            if (lNode == null || lNode.Count == 0) { return default(T); }

            object lValue = DeserializeValue(lType, lNode[0]);
            if (lValue == null || lValue.GetType() != lType) { return default(T); }

            return (T)lValue;
        }

        /// <summary>
        /// Deserializes a basic JSON string into object form. It isnt mean to handle complex
        /// data types. This is just a simple helper.
        /// </summary>
        /// <typeparam name="T">Type (or base type) of object were deserializing to</typeparam>
        /// <param name="rJSON">Content that is being deserialized</param>
        /// <returns>Object that was deserialized</returns>
        public static T Deserialize<T>(string rJSON)
        {
            return (T)Deserialize(rJSON);
        }

        /// <summary>
        /// Deserializes a basic JSON string into object form. It isnt mean to handle complex
        /// data types. This is just a simple helper.
        /// </summary>
        /// <typeparam name="T">Type (or base type) of object were deserializing to</typeparam>
        /// <param name="rJSON">Content that is being deserialized</param>
        /// <returns>Object that was deserialized</returns>
        public static object Deserialize(string rJSON)
        {
            JSONNode lNode = JSONNode.Parse(rJSON);
            if (lNode == null) { return null; }

            string lTypeString = lNode["__Type"].Value;
            if (lTypeString == null || lTypeString.Length == 0) { return null; }

            Type lType = Type.GetType(lTypeString);

            //if (lType.IsPrimitive)
            if (ReflectionHelper.IsPrimitive(lType))
            {
                JSONNode lValueNode = lNode["__Value"];
                return DeserializeValue(lType, lValueNode);
            }
            else if (lType == typeof(string))
            {
                JSONNode lValueNode = lNode["__Value"];
                return DeserializeValue(lType, lValueNode);
            }
            else
            {
                object lObject = null;

                try
                {
                    lObject = Activator.CreateInstance(lType);
                }
                catch (Exception lException)
                {
                    Debug.Log(String.Format("JSONSerializer.Deserialize() {0} {1} {2}", lTypeString, lException.Message, lException.StackTrace));
                }

                if (lObject == null) { return null; }

                // Cycle through all the properties. Unfortunately Binding flags dont seem to 
                // be working. So, we need to check them all
                PropertyInfo[] lProperties = lObject.GetType().GetProperties();
                foreach (PropertyInfo lProperty in lProperties)
                {
                    if (!lProperty.CanWrite) { continue; }

                    //object[] lAttributes = lProperty.GetCustomAttributes(typeof(SerializationIgnoreAttribute), true);
                    //if (lAttributes != null && lAttributes.Length > 0) { continue; }
                    if (ReflectionHelper.IsDefined(lProperty, typeof(SerializationIgnoreAttribute))) { continue; }

                    JSONNode lValueNode = lNode[lProperty.Name];
                    if (lValueNode != null)
                    {
                        object lValue = DeserializeValue(lProperty.PropertyType, lValueNode);
                        if (lValue != null) { lProperty.SetValue(lObject, lValue, null); }
                    }
                }

                FieldInfo[] lFields = lObject.GetType().GetFields();
                foreach (FieldInfo lField in lFields)
                {
                    if (lField.IsInitOnly) { continue; }
                    if (lField.IsLiteral) { continue; }

                    //object[] lAttributes = lField.GetCustomAttributes(typeof(System.NonSerializedAttribute), true);
                    //if (lAttributes != null && lAttributes.Length > 0) { continue; }
                    if (ReflectionHelper.IsDefined(lField, typeof(NonSerializedAttribute))) { continue; }

                    JSONNode lValueNode = lNode[lField.Name];
                    if (lValueNode != null)
                    {
                        object lValue = DeserializeValue(lField.FieldType, lValueNode);
                        if (lValue != null) { lField.SetValue(lObject, lValue); }
                    }
                }

                return lObject;
            }
        }

        /// <summary>
        /// Allows us to deserialize into an existing object
        /// </summary>
        /// <param name="rJSON"></param>
        /// <param name="rObject"></param>
        public static void DeserializeInto(string rJSON, ref object rObject)
        {
            if (rJSON == null || rJSON.Length == 0) { return; }

            JSONNode lNode = JSONNode.Parse(rJSON);
            if (lNode == null || lNode.Count == 0) { return; }

            // If the target is null, instanciate an object
            if (rObject == null)
            {
                string lType = lNode["__Type"].Value;
                if (lType == null || lType.Length == 0) { return; }

                try
                {
                    rObject = Activator.CreateInstance(Type.GetType(lType));
                }
                catch (Exception lException)
                {
                    Log.ConsoleWriteError(String.Format("JSONSerializer.DeserializeInto() {0} {1} {2}", lType, lException.Message, lException.StackTrace));
                }

                if (rObject == null) { return; }
            }

            // Cycle through all the properties. Unfortunately Binding flags dont seem to 
            // be working. So, we need to check them all
            FieldInfo[] lFields = rObject.GetType().GetFields();
            foreach (FieldInfo lField in lFields)
            {
                if (lField.IsInitOnly) { continue; }
                if (lField.IsLiteral) { continue; }

                //object[] lAttributes = lField.GetCustomAttributes(typeof(System.NonSerializedAttribute), true);
                //if (lAttributes != null && lAttributes.Length > 0) { continue; }
                if (ReflectionHelper.IsDefined(lField, typeof(NonSerializedAttribute))) { continue; }

                JSONNode lValueNode = lNode[lField.Name];
                if (lValueNode != null)
                {
                    object lValue = DeserializeValue(lField.FieldType, lValueNode);
                    if (lValue != null) { lField.SetValue(rObject, lValue); }
                }
            }

            PropertyInfo[] lProperties = rObject.GetType().GetProperties();
            foreach (PropertyInfo lProperty in lProperties)
            {
                if (!lProperty.CanWrite) { continue; }

                //object[] lAttributes = lProperty.GetCustomAttributes(typeof(SerializationIgnoreAttribute), true);
                //if (lAttributes != null && lAttributes.Length > 0) { continue; }
                if (ReflectionHelper.IsDefined(lProperty, typeof(SerializationIgnoreAttribute))) { continue; }

                JSONNode lValueNode = lNode[lProperty.Name];
                if (lValueNode != null)
                {
                    object lValue = DeserializeValue(lProperty.PropertyType, lValueNode);
                    if (lValue != null) { lProperty.SetValue(rObject, lValue, null); }
                }
            }
        }

        /// <summary>
        /// Actually performs the property serialization. This way, we
        /// can abstract it a bit.
        /// </summary>
        /// <param name="rName"></param>
        /// <param name="rValue"></param>
        /// <returns></returns>
        private static string SerializeValue(object rValue)
        {
            if (rValue == null) { return ""; }

            StringBuilder lJSON = new StringBuilder("");

            Type lType = rValue.GetType();

            if (lType == typeof(string))
            {
                lJSON.Append("\"");
                lJSON.Append((string)rValue);
                lJSON.Append("\"");
                //lJSON = "\"" + ((string)rValue) + "\"";
            }
            else if (lType == typeof(int))
            {
                lJSON.Append(((int)rValue).ToString());
                //lJSON = ((int)rValue).ToString();
            }
            else if (lType == typeof(float))
            {
                lJSON.Append(((float)rValue).ToString("G8"));
                //lJSON = ((float)rValue).ToString("G8");
            }
            else if (lType == typeof(bool))
            {
                lJSON.Append(((bool)rValue).ToString());
                //lJSON = ((bool)rValue).ToString();
            }
            else if (lType == typeof(Vector2))
            {
                lJSON.Append("\"");
                lJSON.Append(((Vector2)rValue).ToString("G8"));
                lJSON.Append("\"");
                //lJSON = "\"" + ((Vector2)rValue).ToString("G8") + "\"";
            }
            else if (lType == typeof(Vector3))
            {
                lJSON.Append("\"");
                lJSON.Append(((Vector3)rValue).ToString("G8"));
                lJSON.Append("\"");
                //lJSON = "\"" + ((Vector3)rValue).ToString("G8") + "\"";
            }
            else if (lType == typeof(Vector4))
            {
                lJSON.Append("\"");
                lJSON.Append(((Vector4)rValue).ToString("G8"));
                lJSON.Append("\"");
                //lJSON = "\"" + ((Vector4)rValue).ToString("G8") + "\"";
            }
            else if (lType == typeof(Quaternion))
            {
                lJSON.Append("\"");
                lJSON.Append(((Quaternion)rValue).ToString("G8"));
                lJSON.Append("\"");
                //lJSON = "\"" + ((Quaternion)rValue).ToString("G8") + "\"";
            }
            else if (lType == typeof(HumanBodyBones))
            {
                lJSON.Append(((int)rValue).ToString());
                //lJSON = ((int)rValue).ToString();
            }
            else if (lType == typeof(Transform))
            {
                Transform lValue = rValue as Transform;
                if (lValue != null)
                {
                    string lRootPath = (RootObject != null ? GetFullPath(RootObject.transform) : "");

                    string lPath = GetFullPath(lValue);
                    if (lRootPath.Length > 0f) { lPath = ReplaceFirst(lPath, lRootPath, RootObjectID); }

                    lJSON.Append("\"");
                    lJSON.Append(lPath);
                    lJSON.Append("\"");
                    //lJSON = "\"" + ((Transform)rValue).name + "\"";
                }
            }
            else if (lType == typeof(GameObject))
            {
                GameObject lValue = rValue as GameObject;
                if (lValue != null)
                {
                    string lRootPath = (RootObject != null ? GetFullPath(RootObject.transform) : "");

                    string lPath = GetFullPath(lValue.transform);
                    if (lRootPath.Length > 0f) { lPath = ReplaceFirst(lPath, lRootPath, RootObjectID); }

                    lJSON.Append("\"");
                    lJSON.Append(lPath);
                    lJSON.Append("\"");
                    //lJSON = "\"" + ((Transform)rValue).name + "\"";
                }
            }
            else if (lType == typeof(Component))
            {
                Component lValue = rValue as Component;
                if (lValue != null)
                {
                    string lRootPath = (RootObject != null ? GetFullPath(RootObject.transform) : "");

                    string lPath = GetFullPath(lValue.transform);
                    if (lRootPath.Length > 0f) { lPath = ReplaceFirst(lPath, lRootPath, RootObjectID); }

                    lJSON.Append("\"");
                    lJSON.Append(lPath);
                    lJSON.Append("\"");
                    //lJSON = "\"" + ((Transform)rValue).name + "\"";
                }
            }
            else if (rValue is IList)
            {
                lJSON.Append("[");
                for (int i = 0; i < ((IList)rValue).Count; i++)
                {
                    if (i > 0) { lJSON.Append(","); }
                    lJSON.Append(SerializeValue(((IList)rValue)[i]));
                }
                lJSON.Append("]");

                //StringBuilder lItemJSON = new StringBuilder("[");
                //for (int i = 0; i < ((IList)rValue).Count; i++)
                //{
                //    if (i > 0) { lItemJSON.Append(","); }
                //    lItemJSON.Append(SerializeValue(((IList)rValue)[i]));
                //}
                //lItemJSON.Append("]");

                //lJSON = lItemJSON.ToString();
            }
            else if (rValue is IDictionary)
            {
                lJSON.Append("[");
                foreach (object lKey in ((IDictionary)rValue).Keys)
                {
                    string lKeyValue = SerializeValue(lKey);
                    string lItemValue = SerializeValue(((IDictionary)rValue)[lKey]);

                    lJSON.Append("{ ");
                    lJSON.Append(lKeyValue);
                    lJSON.Append(" : ");
                    lJSON.Append(lItemValue);
                    lJSON.Append(" }");
                }
                lJSON.Append("]");

                //StringBuilder lItemJSON = new StringBuilder("[");
                //foreach (object lKey in ((IDictionary)rValue).Keys)
                //{
                //    string lKeyValue = SerializeValue(lKey);
                //    string lItemValue = SerializeValue(((IDictionary)rValue)[lKey]);

                //    lItemJSON.Append("{ ");
                //    lItemJSON.Append(lKeyValue);
                //    lItemJSON.Append(" : ");
                //    lItemJSON.Append(lItemValue);
                //    lItemJSON.Append(" }");
                //}
                //lItemJSON.Append("]");

                //lJSON = lItemJSON.ToString();
            }
            else if (lType == typeof(AnimationCurve))
            {
                lJSON.Append("\"");

                AnimationCurve lCurve = rValue as AnimationCurve;
                for (int i = 0; i < lCurve.keys.Length; i++)
                {
                    Keyframe lKey = lCurve.keys[i];
                    lJSON.Append(lKey.time.ToString("f5") + "|" + lKey.value.ToString("f5") + "|" + lKey.tangentMode.ToString() + "|" + lKey.inTangent.ToString("f5") + "|" + lKey.outTangent.ToString("f5"));

                    if (i < lCurve.keys.Length - 1) { lJSON.Append(";"); }
                }

                lJSON.Append("\"");
            }
            //else if (rValue is AnimationCurve)
            //{
            //    lJSON.Append("[");
            //    for (int i = 0; i < ((AnimationCurve)rValue).keys.Length; i++)
            //    {
            //        Keyframe lKeyFrame = ((AnimationCurve)rValue).keys[i];
            //        lJSON.Append(Serialize(lKeyFrame, true) + ", ");
            //    }
            //    lJSON.Append("]");
            //}
            else
            {
                lJSON.Append(Serialize(rValue, false));
                //lJSON = Serialize(rValue, false);
            }

            return lJSON.ToString();
        }

        /// <summary>
        /// Performs the actual deserialization. We do it here so we can abstract it some
        /// </summary>
        /// <param name="rType"></param>
        /// <param name="rValue"></param>
        /// <returns></returns>
        private static object DeserializeValue(Type rType, JSONNode rValue)
        {
            if (rValue == null) 
            { 
                return ReflectionHelper.GetDefaultValue(rType); 
            }
            else if (rType == typeof(string))
            {
                return rValue.Value;
            }
            else if (rType == typeof(int))
            {
                return rValue.AsInt;
            }
            else if (rType == typeof(float))
            {
                return rValue.AsFloat;
            }
            else if (rType == typeof(bool))
            {
                return rValue.AsBool;
            }
            else if (rType == typeof(Vector2))
            {
                Vector2 lValue = Vector2.zero;
                lValue = lValue.FromString(rValue.Value);

                return lValue;
            }
            else if (rType == typeof(Vector3))
            {
                Vector3 lValue = Vector3.zero;
                lValue = lValue.FromString(rValue.Value);

                return lValue;
            }
            else if (rType == typeof(Vector4))
            {
                Vector4 lValue = Vector4.zero;
                lValue = lValue.FromString(rValue.Value);

                return lValue;
            }
            else if (rType == typeof(Quaternion))
            {
                Quaternion lValue = Quaternion.identity;
                lValue = lValue.FromString(rValue.Value);

                return lValue;
            }
            else if (rType == typeof(HumanBodyBones))
            {
                return (HumanBodyBones)rValue.AsInt;
            }
            else if (rType == typeof(Transform))
            {
                string lName = rValue.Value;

                Transform lValue = null;
                if (lName.Contains(RootObjectID) && RootObject != null)
                {
                    lName = rValue.Value.Replace(RootObjectID, "");
                    if (lName.Length > 0 && lName.Substring(0, 1) == "/") { lName = lName.Substring(1); }

                    lValue = (lName.Length == 0 ? RootObject.transform : RootObject.transform.Find(lName));
                }
                else
                {
                    GameObject lGameObject = GameObject.Find(lName);
                    if (lGameObject != null) { lValue = lGameObject.transform; }
                }

                if (lValue == null)
                {
                    UnityEngine.Debug.LogWarning("ootii.JSONSerializer.DeserializeValue - Transform name '" + lName + "' not found, resulting in null");
                    return null;
                }

                return lValue;
            }
            else if (rType == typeof(GameObject))
            {
                string lName = rValue.Value;

                Transform lValue = null;
                if (lName.Contains(RootObjectID) && RootObject != null)
                {
                    lName = rValue.Value.Replace(RootObjectID, "");
                    if (lName.Length > 0 && lName.Substring(0, 1) == "/") { lName = lName.Substring(1); }

                    lValue = (lName.Length == 0 ? RootObject.transform : RootObject.transform.Find(lName));
                }
                else
                {
                    GameObject lGameObject = GameObject.Find(lName);
                    if (lGameObject != null) { lValue = lGameObject.transform; }
                }

                if (lValue == null)
                {
                    UnityEngine.Debug.LogWarning("ootii.JSONSerializer.DeserializeValue - GameObject name '" + lName + "' not found, resulting in null");
                    return null;
                }

                return lValue.gameObject;
            }
            else if (ReflectionHelper.IsAssignableFrom(typeof(Component), rType))
            {
                string lName = rValue.Value;

                Transform lValue = null;
                if (lName.Contains(RootObjectID) && RootObject != null)
                {
                    lName = rValue.Value.Replace(RootObjectID, "");
                    if (lName.Length > 0 && lName.Substring(0, 1) == "/") { lName = lName.Substring(1); }

                    lValue = (lName.Length == 0 ? RootObject.transform : RootObject.transform.Find(lName));
                }
                else
                {
                    GameObject lGameObject = GameObject.Find(lName);
                    if (lGameObject != null) { lValue = lGameObject.transform; }
                }

                if (lValue == null)
                {
                    UnityEngine.Debug.LogWarning("ootii.JSONSerializer.DeserializeValue - Component  name '" + lName + "' not found, resulting in null");
                    return null;
                }

                return lValue.gameObject.GetComponent(rType);
            }
            else if (typeof(IList).IsAssignableFrom(rType))
            {
                IList lList = null;
                Type lItemType = rType;

                JSONArray lItems = rValue.AsArray;
                
                //if (rType.IsGenericType)
                if (ReflectionHelper.IsGenericType(rType))
                {
                    lItemType = rType.GetGenericArguments()[0];
                    lList = Activator.CreateInstance(rType) as IList;
                }
                else if (rType.IsArray)
                {
                    lItemType = rType.GetElementType();
                    lList = Array.CreateInstance(lItemType, lItems.Count) as IList;
                }

                for (int i = 0; i < lItems.Count; i++)
                {
                    JSONNode lItem = lItems[i];
                    object lItemValue = DeserializeValue(lItemType, lItem);

                    if (lList.Count > i)
                    {
                        lList[i] = lItemValue;
                    }
                    else
                    {
                        lList.Add(lItemValue);
                    }
                }

                return lList;
            }
            else if (typeof(IDictionary).IsAssignableFrom(rType))
            {
                //if (!rType.IsGenericType) { return null; }
                if (!ReflectionHelper.IsGenericType(rType)) { return null; }

                Type lKeyType = rType.GetGenericArguments()[0];
                Type lItemType = rType.GetGenericArguments()[1];
                IDictionary lDictionary = Activator.CreateInstance(rType) as IDictionary;

                JSONArray lItems = rValue.AsArray;
                for (int i = 0; i < lItems.Count; i++)
                {
                    JSONNode lItem = lItems[i];

                    JSONClass lObject = lItem.AsObject;
                    foreach (string lKeyString in lObject.Dictionary.Keys)
                    {
                        object lKeyValue = DeserializeValue(lKeyType, lKeyString);
                        object lItemValue = DeserializeValue(lItemType, lItem[lKeyString]);

                        if (lDictionary.Contains(lKeyValue))
                        {
                            lDictionary[lKeyValue] = lItemValue;
                        }
                        else
                        {
                            lDictionary.Add(lKeyValue, lItemValue);
                        }
                    }
                }

                return lDictionary;
            }
            else if (rType == typeof(AnimationCurve))
            {
                if (rValue.Value.Length > 0)
                {
                    AnimationCurve lCurve = new AnimationCurve();

                    string[] lItems = rValue.Value.Split(';');
                    for (int i = 0; i < lItems.Length; i++)
                    {
                        string[] lElements = lItems[i].Split('|');
                        if (lElements.Length == 5)
                        {
                            int lIntValue = 0;
                            float lFloatValue = 0f;

                            Keyframe lKey = new Keyframe();
                            if (float.TryParse(lElements[0], out lFloatValue)) { lKey.time = lFloatValue; }
                            if (float.TryParse(lElements[1], out lFloatValue)) { lKey.value = lFloatValue; }
                            if (int.TryParse(lElements[2], out lIntValue)) { lKey.tangentMode = lIntValue; }
                            if (float.TryParse(lElements[3], out lFloatValue)) { lKey.inTangent = lFloatValue; }
                            if (float.TryParse(lElements[4], out lFloatValue)) { lKey.outTangent = lFloatValue; }

                            lCurve.AddKey(lKey);
                        }
                    }

                    return lCurve;
                }
            }
            //else if (rType == typeof(AnimationCurve))
            //{
            //    AnimationCurve lCurve = new AnimationCurve();

            //    JSONArray lItems = rValue.AsArray;
            //    for (int i = 0; i < lItems.Count; i++)
            //    {
            //        object lItem = DeserializeValue(typeof(Keyframe), lItems[i]);
            //        if (lItem is Keyframe) { lCurve.AddKey((Keyframe)lItem); }
            //    }

            //    return lCurve;
            //}
            // Activator won't let me create a Keyframe using no constructors. So, 
            // we need this special case
            else if (rType == typeof(Keyframe))
            {
                Keyframe lKeyframe = new Keyframe(rValue["time"].AsFloat, rValue["value"].AsFloat, rValue["inTangent"].AsFloat, rValue["outTangent"].AsFloat);
                return lKeyframe;
            }

            // As a default, simply try to deserialize the string
            return Deserialize(rValue.ToString());
        }

        /// <summary>
        /// Quick function to tell us if we're dealing with a simple type or a complex type
        /// </summary>
        /// <param name="rType"></param>
        /// <returns></returns>
        private static bool IsSimpleType(Type rType)
        {
            if (rType == typeof(string)) { return true; }
            if (rType == typeof(int)) { return true; }
            if (rType == typeof(float)) { return true; }
            if (rType == typeof(bool)) { return true; }
            if (rType == typeof(Vector2)) { return true; }
            if (rType == typeof(Vector3)) { return true; }
            if (rType == typeof(Vector4)) { return true; }
            if (rType == typeof(Quaternion)) { return true; }
            if (rType == typeof(HumanBodyBones)) { return true; }
            if (rType == typeof(Transform)) { return true; }
            return false;
        }

        /// <summary>
        /// Retrieves the full path to the transform
        /// </summary>
        /// <param name="rTransform">Transform we'll find the path for</param>
        /// <returns>Full path of the transform</returns>
        public static string GetFullPath(Transform rTransform)
        {
            string lPath = "";

            Transform lParent = rTransform;
            while (lParent != null)
            {
                if (lPath.Length > 0) { lPath = "/" + lPath; }
                lPath = lParent.name + lPath;

                lParent = lParent.parent;
            }

            return lPath;
        }

        /// <summary>
        /// Replaces the first instance of the search found
        /// </summary>
        /// <param name="rText"></param>
        /// <param name="rSearch"></param>
        /// <param name="rReplace"></param>
        /// <returns></returns>
        public static string ReplaceFirst(string rText, string rSearch, string rReplace)
        {
            int lIndex = rText.IndexOf(rSearch);
            if (lIndex < 0) { return rText; }

            return rText.Substring(0, lIndex) + rReplace + rText.Substring(lIndex + rSearch.Length);
        }

        ///// <summary>
        ///// Gets the default value for the type
        ///// </summary>
        ///// <param name="type"></param>
        ///// <returns></returns>
        //private static object GetDefault(Type rType)
        //{
        //    if (rType.IsValueType)
        //    {
        //        return Activator.CreateInstance(rType);
        //    }
        //    else
        //    {
        //        Vector3 lDummy = new Vector3();
        //        return lDummy.GetType().GetMethod("GetDefaultGeneric").MakeGenericMethod(rType).Invoke(lDummy, null);
        //    }
        //}
    }
}
