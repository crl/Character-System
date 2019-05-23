using System;
using UnityEngine;
using com.ootii.Data.Serializers;
using com.ootii.Helpers;

namespace com.ootii.Actors.LifeCores
{
    /// <summary>
    /// Effects are applied to an actor in order to modify or impact them over time.
    /// This could be a poison that damages the actor over time, fire, healing, etc.
    /// </summary>
    [Serializable]
    public abstract class ActorCoreEffect
    {
        /// <summary>
        /// Semi unique ID that helps identify the source of the effect. This can be
        /// used to ensure we don't add the effect more than once.
        /// </summary>
        public string _SourceID = "";
        public string SourceID
        {
            get { return _SourceID; }
            set { _SourceID = value; }
        }

        /// <summary>
        /// Determines if the action can run
        /// </summary>
        public bool _IsEnabled = true;
        public bool IsEnabled
        {
            get { return _IsEnabled; }
            set { _IsEnabled = value; }
        }

        /// <summary>
        /// Name of the action
        /// </summary>
        public string _Name = "";
        public virtual string Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        /// <summary>
        /// Max age at which the effect expires
        /// </summary>
        public float _MaxAge = 2f;
        public virtual float MaxAge
        {
            get { return _MaxAge; }
            set { _MaxAge = value; }
        }

        /// <summary>
        /// Time in seconds between each instance of the effect
        /// </summary>
        public float _TriggerDelay = 0.5f;
        public float TriggerDelay
        {
            get { return _TriggerDelay; }
            set { _TriggerDelay = value; }
        }

        /// <summary>
        /// ActorCore the effect is tied to
        /// </summary>
        protected IActorCore mActorCore = null;
        public IActorCore ActorCore
        {
            get { return mActorCore; }
            set { mActorCore = value; }
        }

        /// <summary>
        /// Current age of the effect
        /// </summary>
        protected float mAge = 0f;
        public virtual float Age
        {
            get { return mAge; }
            set { mAge = value; }
        }

        /// <summary>
        /// Last time that the effect was triggered
        /// </summary>
        protected float mTriggerTime = 0f;
        public float TriggerTime
        {
            get { return mTriggerTime; }
            set { mTriggerTime = value; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public ActorCoreEffect()
        {
        }

        /// <summary>
        /// ActorCore constructor
        /// </summary>
        public ActorCoreEffect(ActorCore rActorCore)
        {
            mActorCore = rActorCore;
        }

        /// <summary>
        /// Used to initialize any effects prior to them being activated
        /// </summary>
        public virtual void Awake()
        {
        }

        /// <summary>
        /// Clears the effect so it can be used again
        /// </summary>
        public virtual void Clear()
        {
            mAge = 0f;
            mTriggerTime = 0f;

            mActorCore = null;
        }


        /// <summary>
        /// Called when the effect is first activated
        /// </summary>
        /// <param name="rTriggerDelay">Time between triggering the effect</param>
        /// <param name="rMaxAge">Age after which the effect expires</param>
        public virtual void Activate(float rTriggerDelay, float rMaxAge)
        {
            mAge = 0f;
            mTriggerTime = 0f;

            MaxAge = rMaxAge;
            TriggerDelay = rTriggerDelay;

            // Trigger the effect immediately
            TriggerEffect();
        }

        /// <summary>
        /// Called when the effect is meant to be deactivated
        /// </summary>
        public virtual void Deactivate()
        {
        }

        /// <summary>
        /// Called each frame that the effect is active
        /// </summary>
        /// <returns>Boolean that determines if the effect is still active or not</returns>
        public virtual bool Update()
        {
            // Determine if the effect should stay active
            mAge = mAge + Time.deltaTime;
            if (_MaxAge > 0f && mAge > _MaxAge)
            {
                Deactivate();
                return false;
            }

            // Determine if it's time to trigger the effect
            if (_TriggerDelay > 0f && mTriggerTime + _TriggerDelay < Time.time)
            {
                TriggerEffect();
            }

            // Keep the effect updating
            return true;
        }

        /// <summary>
        /// Raised when the effect should be triggered
        /// </summary>
        public virtual void TriggerEffect()
        {
            mTriggerTime = Time.time;
        }

        /// <summary>
        /// Releases the effect as an allocation
        /// </summary>
        public virtual void Release()
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

            if (EditorHelper.FloatField("Max Age", "Max age of the effect before it expires.", MaxAge, rTarget))
            {
                lIsDirty = true;
                MaxAge = EditorHelper.FieldFloatValue;
            }

            if (EditorHelper.FloatField("Trigger Delay", "Time in seconds between the triggering of the effect.", TriggerDelay, rTarget))
            {
                lIsDirty = true;
                TriggerDelay = EditorHelper.FieldFloatValue;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}
