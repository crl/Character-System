using System;
using com.ootii.Data.Serializers;
using com.ootii.Helpers;

namespace com.ootii.Actors.LifeCores
{
    /// <summary>
    /// Holds the correlation between state name and value. I'm doing it this way in
    /// case we want to expand on states later.
    /// </summary>
    [Serializable]
    public class ActorCoreState
    {
        /// <summary>
        /// Name of the state
        /// </summary>
        public string _Name = "";
        public virtual string Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        /// <summary>
        /// ActorCore the effect is tied to
        /// </summary>
        public int _Value = 0;
        public int Value
        {
            get { return _Value; }
            set { _Value = value; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ActorCoreState()
        {
        }

        /// <summary>
        /// Serializes the object into a string
        /// </summary>
        /// <returns>JSON string representing the object</returns>
        public virtual string Serialize()
        {
            return JSONSerializer.Serialize(this, false);
        }

        /// <summary>
        /// Deserialize the object from a string
        /// </summary>
        /// <param name="rDefinition">JSON string</param>
        public virtual void Deserialize(string rDefinition)
        {
            object lThis = this;
            JSONSerializer.DeserializeInto(rDefinition, ref lThis);
        }

        #region Editor Functions

#if UNITY_EDITOR

        /// <summary>
        /// Called when the inspector needs to draw
        /// </summary>
        public virtual bool OnInspectorGUI(UnityEngine.Object rTarget)
        {
            bool lIsDirty = false;

            if (EditorHelper.TextField("Name", "Unique name of the state.", Name, rTarget))
            {
                lIsDirty = true;
                Name = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.IntField("Value", "Value associated with the state", Value, rTarget))
            {
                lIsDirty = true;
                Value = EditorHelper.FieldIntValue;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}
