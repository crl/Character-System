using System;
using UnityEngine;

namespace com.ootii.Actors.Attributes
{
    /// <summary>
    /// Provides a high level definition of the type
    /// of sense that is being used by the actor
    /// </summary>
    public class EnumAttributeTypes
    {
        /// <summary>
        /// Enum values
        /// </summary>
        public const int TAG = 0;
        public const int STRING = 1;
        public const int FLOAT = 2;
        public const int INT = 3;
        public const int BOOL = 4;
        public const int VECTOR2 = 5;
        public const int VECTOR3 = 6;
        public const int VECTOR4 = 7;
        public const int QUATERNION = 8;
        public const int TRANSFORM = 9;
        public const int GAMEOBJECT = 10;
        //public const int UNITY_OBJECT = 11;
        //public const int OBJECT = 12;
        //public const int DATE = 13;

        /// <summary>
        /// Contains a mapping from ID to names
        /// </summary>
        public static string[] Names = new string[] 
        {
            "Tag",
            "String",
            "Float",
            "Integer",
            "Boolean",
            "Vector2",
            "Vector3",
            "Vector4",
            "Quaternion",
            "Transform",
            "GameObject"
            //"Unity Object",
            //"System Object"
            //"Date",
        };

        /// <summary>
        /// Contains a mapping from ID to types
        /// </summary>
        public static Type[] Types = new Type[]
        {
            null,
            typeof(string),
            typeof(float),
            typeof(int),
            typeof(bool),
            typeof(Vector2),
            typeof(Vector3),
            typeof(Vector4),
            typeof(Quaternion),
            typeof(Transform),
            typeof(GameObject)
            //typeof(UnityEngine.Object),
            //typeof(object)
            //typeof(DateTime),
        };

        /// <summary>
        /// Retrieve the index of the specified type
        /// </summary>
        /// <param name="rType"></param>
        /// <returns></returns>
        public static int GetEnum(Type rType)
        {
            for (int i = 0; i < Types.Length; i++)
            {
                if (Types[i] == rType) { return i; }
            }

            return 0;
        }

        /// <summary>
        /// Retrieve the index of the specified type
        /// </summary>
        /// <param name="rType"></param>
        /// <returns></returns>
        public static string GetName(Type rType)
        {
            for (int i = 0; i < Types.Length; i++)
            {
                if (Types[i] == rType) { return Names[i]; }
            }

            return "Tag";
        }
    }
}