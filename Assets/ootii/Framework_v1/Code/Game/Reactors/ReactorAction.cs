using System;
using UnityEngine;
using com.ootii.Data.Serializers;
using com.ootii.Helpers;
using com.ootii.Messages;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Reactors
{
    /// <summary>
    /// Reactors are used in response to state changes and message on the actor. This allows us
    /// to have custom responses to events that occur.
    /// </summary>
    [Serializable]
    public abstract class ReactorAction
    {
        /// <summary>
        /// Defines the activation types available
        /// </summary>
        public static string[] ACTIVATION_STYLES = new string[] { "Message Received", "State Set", "State Changed" };

        /// <summary>
        /// GameObject that owns the reactor
        /// </summary>
        [NonSerialized]
        protected GameObject mOwner = null;
        public virtual GameObject Owner
        {
            get { return mOwner; }
            set { mOwner = value; }
        }

        /// <summary>
        /// Determines how the reactor is activated
        /// </summary>
        public int _ActivationType = 0;
        public int ActivationType
        {
            get { return _ActivationType; }
            set { _ActivationType = value; }
        }

        /// <summary>
        /// Name of the state to test
        /// </summary>
        public string _ActivationStateName = "";
        public string ActivationStateName
        {
            get { return _ActivationStateName; }
            set { _ActivationStateName = value; }
        }

        /// <summary>
        /// Value of the state to test for
        /// </summary>
        public int _ActivationValue = 0;
        public int ActivationValue
        {
            get { return _ActivationValue; }
            set { _ActivationValue = value; }
        }

        /// <summary>
        /// Determines if the reactor can run
        /// </summary>
        public bool _IsEnabled = true;
        public bool IsEnabled
        {
            get { return _IsEnabled; }
            set { _IsEnabled = value; }
        }

        /// <summary>
        /// Determines if the reactor is running
        /// </summary>
        public bool _IsActive = true;
        public bool IsActive
        {
            get { return _IsActive; }
            set { _IsActive = value; }
        }
        
        /// <summary>
        /// Determines if the reactor has priority over lower reactors
        /// </summary>
        public float _Priority = 0f;
        public float Priority
        {
            get { return _Priority; }
            set { _Priority = value; }
        }

        /// <summary>
        /// Name of the reactor
        /// </summary>
        public string _Name = "";
        public virtual string Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        // Optional message that we're processing. We don't allocate or 
        // release the message. We are just holding it.
        [NonSerialized]
        protected IMessage mMessage = null;
        public IMessage Message
        {
            get { return mMessage; }
            set { mMessage = value; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ReactorAction()
        {
        }

        /// <summary>
        /// ActorCore constructor
        /// </summary>
        public ReactorAction(GameObject rOwner)
        {
            mOwner = rOwner;
        }

        /// <summary>
        /// Used to initialize any reactors prior to them being activated
        /// </summary>
        public virtual void Awake()
        {
        }

        /// <summary>
        /// Clears the reactor so it can be used again
        /// </summary>
        public virtual void Clear()
        {
            mOwner = null;
        }

        /// <summary>
        /// Used to test if the reactor should process
        /// </summary>
        /// <returns></returns>
        public virtual bool TestActivate(int rOldState, int rNewState)
        {
            if (mOwner == null) { return false; }
            if (rOldState == rNewState) { return false; }

            // Setting the state value
            if (_ActivationType == 1)
            {
                if (_ActivationValue == 0 || rNewState == _ActivationValue)
                {
                    return true;
                }
            }
            // Leaving state value
            else if (_ActivationType == 2)
            {
                if (_ActivationValue == 0 || rOldState == _ActivationValue)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Used to test if the reactor should process
        /// </summary>
        /// <returns></returns>
        public virtual bool TestActivate(IMessage rMessage)
        {
            if (rMessage == null) { return false; }
            if (mOwner == null) { return false; }

            if (_ActivationType != 0) { return false; }
            if (_ActivationValue > 0 && _ActivationValue != rMessage.ID) { return false; }

            mMessage = rMessage;

            return true;
        }

        /// <summary>
        /// Called when the reactor is first activated. Determines if we should be 
        /// continuing to activate other reactors.
        /// </summary>
        /// <returns>Determines if other reactors should process.</returns>
        public virtual bool Activate()
        {
            _IsActive = true;

            return true;
        }

        /// <summary>
        /// Called when the reactor is meant to be deactivated
        /// </summary>
        public virtual void Deactivate()
        {
            _IsActive = false;
        }

        /// <summary>
        /// Called each frame that the reactor is active
        /// </summary>
        public virtual void Update()
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

        // Determines if we'll show editor information
        public bool _EditorShowActivationType = true;

        /// <summary>
        /// Called when the inspector needs to draw
        /// </summary>
        public virtual bool OnInspectorGUI(UnityEditor.SerializedObject rTargetSO, UnityEngine.Object rTarget)
        {
            bool lIsDirty = false;

            GUILayout.Space(3f);

            if (_EditorShowActivationType)
            {
                if (EditorHelper.PopUpField("Activation Type", "Determines how the reactor is activate", ActivationType, ReactorAction.ACTIVATION_STYLES, rTarget))
                {
                    lIsDirty = true;
                    ActivationType = EditorHelper.FieldIntValue;
                }

                // State set
                if (_ActivationType == 1)
                {
                    if (EditorHelper.TextField("State", "Name of the state that is being tested.", ActivationStateName, rTarget))
                    {
                        lIsDirty = true;
                        ActivationStateName = EditorHelper.FieldStringValue;
                    }

                    if (EditorHelper.IntField("Value", "New value of the state that is being set.", ActivationValue, rTarget))
                    {
                        lIsDirty = true;
                        ActivationValue = EditorHelper.FieldIntValue;
                    }
                }
                // State changed
                else if (_ActivationType == 2)
                {
                    if (EditorHelper.TextField("State", "Name of the state that is being tested.", ActivationStateName, rTarget))
                    {
                        lIsDirty = true;
                        ActivationStateName = EditorHelper.FieldStringValue;
                    }

                    if (EditorHelper.IntField("Value", "Old value of the state that is being changed.", ActivationValue, rTarget))
                    {
                        lIsDirty = true;
                        ActivationValue = EditorHelper.FieldIntValue;
                    }
                }
                // Message received
                else if (_ActivationType == 0)
                {
                    if (EditorHelper.IntField("Message ID", "Message ID to look for and activate on. If 0, all messages will be tested.", ActivationValue, rTarget))
                    {
                        lIsDirty = true;
                        ActivationValue = EditorHelper.FieldIntValue;
                    }
                }

                EditorHelper.DrawLine();
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}
