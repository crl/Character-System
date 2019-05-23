using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.Attributes;
using com.ootii.Actors.Combat;
using com.ootii.Data.Serializers;
using com.ootii.Helpers;
using com.ootii.Messages;
using com.ootii.Reactors;

namespace com.ootii.Actors.LifeCores
{
    /// <summary>
    /// Determines the capabilities of the actor and provides access to
    /// core specific functionality.
    /// </summary>
    public class ActorCore : MonoBehaviour, IActorCore
    {
        /// <summary>
        /// GameObject that owns the IAttributeSource we really want
        /// </summary>
        public GameObject _AttributeSourceOwner = null;
        public GameObject AttributeSourceOwner
        {
            get { return _AttributeSourceOwner; }
            set { _AttributeSourceOwner = value; }
        }

        /// <summary>
        /// Defines the source of the attributes that control our health
        /// </summary>
        [NonSerialized]
        protected IAttributeSource mAttributeSource = null;
        public IAttributeSource AttributeSource
        {
            get { return mAttributeSource; }
            set { mAttributeSource = value; }
        }

        /// <summary>
        /// Transform that is the actor
        /// </summary>
        public Transform Transform
        {
            get { return gameObject.transform; }
        }

        /// <summary>
        /// Determines if the actor is actually alive
        /// </summary>
        public bool _IsAlive = true;
        public virtual bool IsAlive
        {
            get { return _IsAlive; }
            set { _IsAlive = value; }
        }

        /// <summary>
        /// Attribute identifier that represents the health attribute
        /// </summary>
        public string _HealthID = "Health";
        public string HealthID
        {
            get { return _HealthID; }
            set { _HealthID = value; }
        }

        /// <summary>
        /// Motion name to use when damage is taken
        /// </summary>
        public string _DamagedMotion = "Damaged";
        public string DamagedMotion
        {
            get { return _DamagedMotion; }
            set { _DamagedMotion = value; }
        }

        /// <summary>
        /// Motion name to use when death occurs
        /// </summary>
        public string _DeathMotion = "Death";
        public string DeathMotion
        {
            get { return _DeathMotion; }
            set { _DeathMotion = value; }
        }

        /// <summary>
        /// States allow us to store information about the actor's disposition, stance, etc
        /// </summary>
        public List<ActorCoreState> _States = new List<ActorCoreState>();
        public List<ActorCoreState> States
        {
            get { return _States; }
            set { _States = value; }
        }

        /// <summary>
        /// Reactors that are active on the actor. These respond to events like status changes and messages
        /// </summary>
        public List<ReactorAction> _Reactors = new List<ReactorAction>();
        public List<ReactorAction> Reactors
        {
            get { return _Reactors; }
            set { _Reactors = value; }
        }

        /// <summary>
        /// Serialized reactors since Unity can't serialize derived classes
        /// </summary>
        public List<string> _ReactorDefinitions = new List<string>();

        /// <summary>
        /// Effects that are active on the actor. These can do things like modify heal over time.
        /// </summary>
        public List<ActorCoreEffect> _Effects = new List<ActorCoreEffect>();
        public List<ActorCoreEffect> Effects
        {
            get { return _Effects; }
            set { _Effects = value; }
        }

        /// <summary>
        /// Serialized effects since Unity can't serialized derived classes
        /// </summary>
        public List<string> _EffectDefinitions = new List<string>();

        /// <summary>
        /// Serialized events that may be used by the reactors. We have to store them
        /// here since Unity won't serialize them and support polymorphism.
        /// DO NOT SHARE INDEXES - even if this mean re-storing the same transform between motors
        /// </summary>
        public List<ReactorActionEvent> _StoredUnityEvents = new List<ReactorActionEvent>();

        // Quick look-up for state indexes
        protected Dictionary<string, int> mStateHash = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

#if UNITY_EDITOR

        // Keeps the state selected in the editor
        public int EditorStateIndex = -1;

        // Keeps the reactor selected in the editor
        public int EditorReactorIndex = -1;

        // Keeps the effect selected in the editor
        public int EditorEffectIndex = -1;

#endif

        /// <summary>
        /// Once the objects are instanciated, awake is called before start. Use it
        /// to setup references to other objects
        /// </summary>
        protected virtual void Awake()
        {
            // Object that will provide access to attributes
            if (_AttributeSourceOwner != null)
            {
                AttributeSource = InterfaceHelper.GetComponent<IAttributeSource>(_AttributeSourceOwner);
            }

            // If the input source is still null, see if we can grab a local input source
            if (AttributeSource == null)
            {
                AttributeSource = InterfaceHelper.GetComponent<IAttributeSource>(gameObject);
                if (AttributeSource != null) { _AttributeSourceOwner = gameObject; }
            }

            // Create and initialize the components
            InstantiateStates();
            InstantiateReactors();
            InstantiateEffects();
        }

        #region States

        /// <summary>
        /// Processes the reactor definitions and update the reactors to match the definitions.
        /// </summary>
        public void InstantiateStates()
        {
            mStateHash.Clear();

            for (int i = 0; i < _States.Count; i++)
            {
                mStateHash.Add(_States[i]._Name, i);
            }
        }

        /// <summary>
        /// Determines if the state variable actually exists
        /// </summary>
        /// <param name="rName">Name of the state variable whose value we are interested in</param>
        /// <returns>Boolean that determines if the state variable actually exists</returns>
        public bool StateExists(string rName)
        {
            return mStateHash.ContainsKey(rName);
        }

        /// <summary>
        /// Removes the specified state variable
        /// </summary>
        /// <param name="rName">Name of the state variable to remove</param>
        public void RemoveState(string rName)
        {
            bool lIsRemoved = false;

            for (int i = _States.Count - 1; i >= 0; i--)
            {
                if (string.Compare(_States[i]._Name, rName) == 0)
                {
                    lIsRemoved = true;
                    _States.RemoveAt(i);
                }
            }

            if (lIsRemoved)
            {
                InstantiateStates();
            }
        }

        /// <summary>
        /// Retrieves the value of the specified state variable.
        /// </summary>
        /// <param name="rName">Name of the state variable whose value we are interested in</param>
        /// <returns>Integer that is the value of the specified state variable</returns>
        public int GetStateValue(string rName)
        {
            if (mStateHash.ContainsKey(rName))
            {
                return _States[mStateHash[rName]].Value;
            }

            return 0;
        }

        /// <summary>
        /// Sets the value of the specified state variable.
        /// </summary>
        /// <param name="rName">Name of the state variable whose value we are interested in</param>
        /// <param name="rValue">Integer that is the value to set</param>
        public void SetStateValue(string rName, int rValue)
        {
            if (!mStateHash.ContainsKey(rName))
            {
                ActorCoreState lState = new ActorCoreState();
                lState.Name = rName;
                lState.Value = rValue;

                _States.Add(lState);
                InstantiateStates();
            }

            int lOldValue = _States[mStateHash[rName]].Value;
            _States[mStateHash[rName]].Value = rValue;

            // Activate the reactor as needed
            for (int i = 0; i < _Reactors.Count; i++)
            {
                if (_Reactors[i].IsEnabled)
                {
                    bool lActivate = _Reactors[i].TestActivate(lOldValue, rValue);
                    if (lActivate)
                    {
                        bool lAllowOthers = Reactors[i].Activate();
                        if (!lAllowOthers) { break; }
                    }
                }
            }
        }

        #endregion

        #region Reactors

        /// <summary>
        /// Processes the reactor definitions and update the reactors to match the definitions.
        /// </summary>
        public void InstantiateReactors()
        {
            int lDefCount = _ReactorDefinitions.Count;

            // First, remove any extra items that may exist
            for (int i = _Reactors.Count - 1; i >= lDefCount; i--)
            {
                _Reactors.RemoveAt(i);
            }

            // We need to match the definitions to the item
            for (int i = 0; i < lDefCount; i++)
            {
                string lDefinition = _ReactorDefinitions[i];

                Type lType = JSONSerializer.GetType(lDefinition);
                if (lType == null) { continue; }

                ReactorAction lReactor = null;

                // If we don't have an item matching the type, we need to create one
                if (_Reactors.Count <= i || !lType.Equals(_Reactors[i].GetType()))
                {
                    lReactor = Activator.CreateInstance(lType) as ReactorAction;
                    lReactor.Owner = this.gameObject;

                    if (_Reactors.Count <= i)
                    {
                        _Reactors.Add(lReactor);
                    }
                    else
                    {
                        _Reactors[i] = lReactor;
                    }
                }
                // Grab the matching item
                else
                {
                    lReactor = _Reactors[i];
                }

                // Fill the item with data from the definition
                if (lReactor != null)
                {
                    lReactor.Deserialize(lDefinition);
                }
            }

            // Allow each item to initialize now that it has been deserialized
            for (int i = 0; i < _Reactors.Count; i++)
            {
                _Reactors[i].Owner = this.gameObject;
                _Reactors[i].Awake();
            }
        }

        /// <summary>
        /// Adds a reactor to the list
        /// </summary>
        /// <param name="rReactor">ReactorAction to add</param>
        public virtual void AddReactor(ReactorAction rReactor)
        {
            _Reactors.Add(rReactor);
            _ReactorDefinitions.Add(rReactor.Serialize());

            rReactor.Owner = this.gameObject;
            rReactor.Awake();
        }

        /// <summary>
        /// Removes the specified reactor from the list
        /// </summary>
        /// <param name="rReactor">ReactorAction to remove</param>
        public virtual void RemoveReactor(ReactorAction rReactor)
        {
            for (int i = 0; i < _Reactors.Count; i++)
            {
                if (_Reactors[i] == rReactor)
                {
                    _Reactors.RemoveAt(i);
                    _ReactorDefinitions.RemoveAt(i);

                    return;
                }
            }
        }

        /// <summary>
        /// Grabs the active effect whose name matches
        /// </summary>
        /// <param name="rName">Semi unique ID we're looking for</param>
        /// <returns>ActorCoreEffect that matches the arguments or null if none found</returns>
        public virtual ReactorAction GetReactor(string rNameID)
        {
            for (int i = 0; i < _Reactors.Count; i++)
            {
                if (string.Compare(_Reactors[i].Name, rNameID) == 0)
                {
                    return _Reactors[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Grabs the active effect whose type and name match
        /// </summary>
        /// <typeparam name="T">Type of effect to find</typeparam>
        /// <param name="rSourceID">Semi unique ID we're looking for</param>
        /// <returns>ActorCoreEffect that matches the arguments or null if none found</returns>
        public virtual T GetReactor<T>(string rName = null) where T : ReactorAction
        {
            Type lType = typeof(T);
            for (int i = 0; i < _Reactors.Count; i++)
            {
                if (rName == null || string.Compare(_Reactors[i].Name, rName) == 0)
                {
                    if (_Reactors[i].GetType() == lType)
                    {
                        return (T)_Reactors[i];
                    }
                }
            }

            return null;
        }

        #endregion

        #region Effects

        /// <summary>
        /// Processes the effect definitions and updates the effects to match the definitions.
        /// </summary>
        public void InstantiateEffects()
        {
            int lDefCount = _EffectDefinitions.Count;

            // First, remove any extra motors that may exist
            for (int i = _Effects.Count - 1; i >= lDefCount; i--)
            {
                _Effects.RemoveAt(i);
            }

            // We need to match the motor definitions to the motors
            for (int i = 0; i < lDefCount; i++)
            {
                string lDefinition = _EffectDefinitions[i];

                Type lType = JSONSerializer.GetType(lDefinition);
                if (lType == null) { continue; }

                ActorCoreEffect lEffect = null;

                // If don't have a motor matching the type, we need to create one
                if (_Effects.Count <= i || !lType.Equals(_Effects[i].GetType()))
                {
                    lEffect = Activator.CreateInstance(lType) as ActorCoreEffect;
                    lEffect.ActorCore = this;

                    if (_Effects.Count <= i)
                    {
                        _Effects.Add(lEffect);
                    }
                    else
                    {
                        _Effects[i] = lEffect;
                    }
                }
                // Grab the matching motor
                else
                {
                    lEffect = _Effects[i];
                }

                // Fill the motor with data from the definition
                if (lEffect != null)
                {
                    lEffect.Deserialize(lDefinition);
                }
            }

            // Allow each motion to initialize now that his has been deserialized
            for (int i = 0; i < _Effects.Count; i++)
            {
                _Effects[i].Awake();
            }
        }

        /// <summary>
        /// Grabs the active effect whose name matches
        /// </summary>
        /// <param name="rName">Semi unique ID we're looking for</param>
        /// <returns>ActorCoreEffect that matches the arguments or null if none found</returns>
        public virtual ActorCoreEffect GetActiveEffectFromName(string rName)
        {
            for (int i = 0; i < _Effects.Count; i++)
            {
                if (string.Compare(_Effects[i].Name, rName) == 0)
                {
                    return _Effects[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Grabs the active effect whose type and name match
        /// </summary>
        /// <typeparam name="T">Type of effect to find</typeparam>
        /// <param name="rSourceID">Semi unique ID we're looking for</param>
        /// <returns>ActorCoreEffect that matches the arguments or null if none found</returns>
        public virtual T GetActiveEffectFromName<T>(string rName = null) where T : ActorCoreEffect
        {
            for (int i = 0; i < _Effects.Count; i++)
            {
                if (rName == null || string.Compare(_Effects[i].Name, rName) == 0)
                {
                    if (_Effects[i].GetType() == typeof(T))
                    {
                        return (T)_Effects[i];
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Grabs the active effect whose type and source ID match
        /// </summary>
        /// <typeparam name="T">Type of effect to find</typeparam>
        /// <param name="rSourceID">Semi unique ID we're looking for</param>
        /// <returns>ActorCoreEffect that matches the arguments or null if none found</returns>
        public virtual T GetActiveEffectFromSourceID<T>(string rSourceID) where T : ActorCoreEffect
        {
            for (int i = 0; i < _Effects.Count; i++)
            {
                if (string.Compare(_Effects[i].SourceID, rSourceID) == 0)
                {
                    if (_Effects[i].GetType() == typeof(T))
                    {
                        return (T)_Effects[i];
                    }
                }
            }

            return null;
        }

        #endregion

        #region UnityEvents

        /// <summary>
        /// Grabs the UnityEvent stored at the specified index
        /// </summary>
        /// <param name="rIndex">Index of the stored event</param>
        /// <returns>UnityEvent at the index or null</returns>
        public ReactorActionEvent GetStoredUnityEvent(int rIndex)
        {
            if (rIndex < 0 || rIndex >= _StoredUnityEvents.Count)
            {
                return null;
            }

            return _StoredUnityEvents[rIndex];
        }

        /// <summary>
        /// Grabs the UnityEvent stored at the specified index. This version will update
        /// the index based on the result. Typically the editor uses this version.
        /// </summary>
        /// <param name="rIndex">Index of the stored event</param>
        /// <returns>UnityEvent at the index or null</returns>
        public ReactorActionEvent GetStoredGameObject(ref int rIndex)
        {
            if (rIndex < 0 || rIndex >= _StoredUnityEvents.Count)
            {
                rIndex = -1;
                return null;
            }

            if (_StoredUnityEvents[rIndex] == null)
            {
                rIndex = -1;
                return null;
            }

            return _StoredUnityEvents[rIndex];
        }

        /// <summary>
        /// Stores the event at the specified index. If we are passing a null UnityEvent,
        /// the index may be cleared and returned
        /// </summary>
        /// <param name="rIndex">Index to store the UnityEvent at</param>
        /// <param name="rObject">ReactorActionEvent to store</param>
        public int StoreUnityEvent(int rIndex, ReactorActionEvent rObject)
        {
            int lIndex = rIndex;

            if (rObject == null)
            {
                if (lIndex >= 0 && lIndex < _StoredUnityEvents.Count)
                {
                    _StoredUnityEvents[lIndex] = null;
                }

                lIndex = -1;
            }
            else
            {
                if (lIndex == -1)
                {
                    lIndex = _StoredUnityEvents.Count;
                    _StoredUnityEvents.Add(null);
                }

                _StoredUnityEvents[lIndex] = rObject;
            }

            return lIndex;
        }

        #endregion

        #region Message Handling

        /// <summary>
        /// Allows a message to be passed in and will send the message to the reactors
        /// </summary>
        /// <param name="rMessage">Message to be processed</param>
        public void SendMessage(IMessage rMessage)
        {
            // Activate the reactor as needed
            for (int i = 0; i < _Reactors.Count; i++)
            {
                if (_Reactors[i].IsEnabled)
                {
                    bool lActivate = _Reactors[i].TestActivate(rMessage);
                    if (lActivate)
                    {
                        bool lAllowOthers = _Reactors[i].Activate();
                        if (!lAllowOthers) { break; }
                    }
                }
            }
        }

        #endregion

        /// <summary>
        /// Run once per frame in order to manage the actor
        /// </summary>
        protected virtual void Update()
        {
            // Process each of the reactors effects
            for (int i = 0; i < _Reactors.Count; i++)
            {
                ReactorAction lReactor = _Reactors[i];
                if (lReactor._IsEnabled && lReactor._IsActive)
                {
                    lReactor.Update();
                }
            }
            
            // Process each of the active effects
            for (int i = 0; i < _Effects.Count; i++)
            {
                ActorCoreEffect lEffect = _Effects[i];
                bool lIsActive = lEffect.Update();

                // If the effect is no longer active, remove it
                if (!lIsActive)
                {
                    _Effects.RemoveAt(i);
                    i--;

                    lEffect.Release();
                }
            }
        }

        /// <summary>
        /// Called when the actor is about to be affected by something like a spell, poison, etc.
        /// The sub-class would override this function and interrogate the message as needed.
        /// </summary>
        /// <param name="rMessage">Message describing what's happening</param>
        /// <returns>Returns true if the affect should continue or false if not</returns>
        public virtual bool TestAffected(IMessage rMessage)
        {
            return true;
        }

        /// <summary>
        /// Called when the actor takes damage.
        /// </summary>
        /// <param name="rMessage">Message that defines the damage that is taken.</param>
        /// <returns>Determines if the damage was applied</returns>
        public virtual bool OnDamaged(DamageMessage rMessage)
        {
            if (!IsAlive) { return true; }

            float lRemainingHealth = 0f;
            if (AttributeSource != null)
            {
                if (rMessage is DamageMessage)
                {
                    lRemainingHealth = AttributeSource.GetAttributeValue<float>(HealthID) - ((DamageMessage)rMessage).Damage;
                    AttributeSource.SetAttributeValue(HealthID, lRemainingHealth);
                }
            }

            if (lRemainingHealth <= 0f)
            {
                OnKilled(rMessage);
            }
            else if (rMessage != null)
            {
                bool lPlayAnimation = true;
                if (rMessage is DamageMessage) { lPlayAnimation = ((DamageMessage)rMessage).AnimationEnabled; }

                if (lPlayAnimation)
                {
                    MotionController lMotionController = gameObject.GetComponent<MotionController>();
                    if (lMotionController != null)
                    {
                        // Send the message to the MC to let it activate
                        rMessage.ID = CombatMessage.MSG_DEFENDER_DAMAGED;
                        lMotionController.SendMessage(rMessage);
                    }

                    if (!rMessage.IsHandled && DamagedMotion.Length > 0)
                    {
                        MotionControllerMotion lMotion = null;
                        if (lMotionController != null) { lMotion = lMotionController.GetMotion(DamagedMotion); }

                        if (lMotion != null)
                        {
                            lMotionController.ActivateMotion(lMotion);
                        }
                        else
                        {
                            int lID = Animator.StringToHash(DeathMotion);
                            if (lID != 0)
                            {
                                Animator lAnimator = gameObject.GetComponent<Animator>();
                                if (lAnimator != null) { lAnimator.CrossFade(DamagedMotion, 0.25f, 0); }
                            }
                        }
                    }
                }
            }

            return true;
        }

        /// <summary>
        /// Tells the actor to die and triggers any effects or animations.
        /// </summary>
        /// <param name="rMessage">Message that defines the damage that is taken.</param>
        public virtual void OnKilled(DamageMessage rMessage)
        {
            IsAlive = false;

            if (AttributeSource != null && HealthID.Length > 0)
            {
                AttributeSource.SetAttributeValue(HealthID, 0f);
            }

            StartCoroutine(InternalDeath(rMessage));
        }

        /// <summary>
        /// Coroutine to play the death animation and disable the actor after a couple of seconds
        /// </summary>
        /// <param name="rDamageValue">Amount of damage to take</param>
        /// <param name="rDamageType">Damage type taken</param>
        /// <param name="rAttackAngle">Angle that the damage came from releative to the actor's forward</param>
        /// <param name="rBone">Transform that the damage it... if known</param>
        /// <returns></returns>
        protected virtual IEnumerator InternalDeath(IMessage rMessage)
        {
            ActorController lActorController = gameObject.GetComponent<ActorController>();
            MotionController lMotionController = gameObject.GetComponent<MotionController>();

            // Run the death animation if we can
            if (rMessage != null && lMotionController != null)
            {
                // Send the message to the MC to let it activate
                rMessage.ID = CombatMessage.MSG_DEFENDER_KILLED;
                lMotionController.SendMessage(rMessage);

                if (!rMessage.IsHandled && DeathMotion.Length > 0)
                {
                    MotionControllerMotion lMotion = lMotionController.GetMotion(DeathMotion);
                    if (lMotion != null)
                    {
                        lMotionController.ActivateMotion(lMotion);
                    }
                    else
                    {
                        int lID = Animator.StringToHash(DeathMotion);
                        if (lID != 0)
                        {
                            Animator lAnimator = gameObject.GetComponent<Animator>();
                            if (lAnimator != null)
                            {
                                try
                                {
                                    lAnimator.CrossFade(DeathMotion, 0.25f, 0);
                                }
                                catch { }
                            }
                        }
                    }
                }

                // Trigger the death animation
                yield return new WaitForSeconds(3.0f);

                // Shut down the MC
                lMotionController.enabled = false;
                lMotionController.ActorController.enabled = false;
            }

            // Disable all colliders
            Collider[] lColliders = gameObject.GetComponents<Collider>();
            for (int i = 0; i < lColliders.Length; i++)
            {
                lColliders[i].enabled = false;
            }

            if (lActorController != null) { lActorController.RemoveBodyShapes(); }
        }
    }
}
