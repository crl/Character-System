using System;
using System.Collections;
using UnityEngine;
using com.ootii.Base;
using com.ootii.Actors;
using com.ootii.Actors.LifeCores;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.Combat;
using com.ootii.Helpers;
using com.ootii.Messages;

namespace com.ootii.Reactors
{
    /// <summary>
    /// Basic reactor used for when a death message comes in.
    /// </summary>
    [Serializable]
    [BaseName("Basic Killed Reactor")]
    [BaseDescription("Basic reactor for handling messages of type DamageMessage where the owner is killed. This is activate the appropriate effects and animations.")]
    public class BasicKilledReactor : ReactorAction
    {
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
        /// Motion name to use when death occurs
        /// </summary>
        public string _DeathMotion = "";
        public string DeathMotion
        {
            get { return _DeathMotion; }
            set { _DeathMotion = value; }
        }

        /// <summary>
        /// Determines if we disable core components
        /// </summary>
        public bool _DisableComponents = true;
        public bool DisableComponents
        {
            get { return _DisableComponents; }
            set { _DisableComponents = value; }
        }

        /// <summary>
        /// Determines if we disable Colliders
        /// </summary>
        public bool _DisableColliders = true;
        public bool DisableColliders
        {
            get { return _DisableColliders; }
            set { _DisableColliders = value; }
        }

        /// <summary>
        /// Determines if we remove the body shapes on death
        /// </summary>
        public bool _RemoveBodyShapes = true;
        public bool RemoveBodyShapes
        {
            get { return _RemoveBodyShapes; }
            set { _RemoveBodyShapes = value; }
        }

        // ActorCore the reactor belongs to
        [NonSerialized]
        protected ActorCore mActorCore = null;

        /// <summary>
        /// Default constructor
        /// </summary>
        public BasicKilledReactor() : base()
        {
            _ActivationType = 0;
        }

        /// <summary>
        /// ActorCore constructor
        /// </summary>
        public BasicKilledReactor(GameObject rOwner) : base(rOwner)
        {
            _ActivationType = 0;
            mActorCore = rOwner.GetComponent<ActorCore>();
        }

        /// <summary>
        /// Initialize the reactor
        /// </summary>
        public override void Awake()
        {
            if (mOwner != null)
            {
                mActorCore = mOwner.GetComponent<ActorCore>();
            }
        }

        /// <summary>
        /// Used to test if the reactor should process
        /// </summary>
        /// <returns></returns>
        public override bool TestActivate(IMessage rMessage)
        {
            if (!base.TestActivate(rMessage)) { return false; }
            if (!mActorCore.IsAlive) { return false; }

            if (rMessage.ID != CombatMessage.MSG_DEFENDER_KILLED) { return false; }

            CombatMessage lCombatMessage = rMessage as CombatMessage;
            if (lCombatMessage != null && lCombatMessage.Defender != mActorCore.gameObject) { return false; }

            mMessage = rMessage;

            return true;
        }

        /// <summary>
        /// Called when the reactor is first activated
        /// </summary>
        /// <returns>Determines if other reactors should process.</returns>
        public override bool Activate()
        {
            base.Activate();

            mActorCore.IsAlive = false;

            if (mActorCore.AttributeSource != null && HealthID.Length > 0)
            {
                mActorCore.AttributeSource.SetAttributeValue(HealthID, 0f);
            }

            mActorCore.StartCoroutine(InternalDeath(mMessage));

            // Allow other reactors to continue
            return true;
        }

        /// <summary>
        /// Called when the reactor is meant to be deactivated
        /// </summary>
        public override void Deactivate()
        {
            base.Deactivate();

            mMessage = null;
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
            ActorController lActorController = mActorCore.gameObject.GetComponent<ActorController>();
            MotionController lMotionController = mActorCore.gameObject.GetComponent<MotionController>();

            // Run the death animation if we can
            if (rMessage != null && lMotionController != null)
            {
                // Send the message to the MC to let it activate
                rMessage.ID = CombatMessage.MSG_DEFENDER_KILLED;

                if (DeathMotion.Length > 0)
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
                            Animator lAnimator = mActorCore.gameObject.GetComponent<Animator>();
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
                else
                {
                    lMotionController.SendMessage(rMessage);
                }

                // Trigger the death animation
                yield return new WaitForSeconds(3.0f);

                // Shut down the MC
                if (_DisableComponents)
                {
                    lMotionController.enabled = false;
                    lMotionController.ActorController.enabled = false;
                }
            }

            // Disable all colliders
            if (_DisableColliders)
            {
                Collider[] lColliders = mActorCore.gameObject.GetComponents<Collider>();
                for (int i = 0; i < lColliders.Length; i++)
                {
                    lColliders[i].enabled = false;
                }
            }

            // Remove the body shapes
            if (_RemoveBodyShapes)
            {
                if (lActorController != null) { lActorController.RemoveBodyShapes(); }
            }

            // Stop the reactor
            Deactivate();
        }

        #region Editor Functions

#if UNITY_EDITOR

        /// <summary>
        /// Called when the inspector needs to draw
        /// </summary>
        public override bool OnInspectorGUI(UnityEditor.SerializedObject rTargetSO, UnityEngine.Object rTarget)
        {
            _EditorShowActivationType = false;
            bool lIsDirty = base.OnInspectorGUI(rTargetSO, rTarget);

            if (EditorHelper.TextField("Health ID", "Attribute identifier that represents the health attribute", HealthID, rTarget))
            {
                lIsDirty = true;
                HealthID = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.TextField("Death Motion", "Name of motion to activate when death occurs and the message isn't handled.", DeathMotion, rTarget))
            {
                lIsDirty = true;
                DeathMotion = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.BoolField("Disable Components", "Determines if we disable the MC and AC.", DisableComponents, rTarget))
            {
                lIsDirty = true;
                DisableComponents = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("Disable Colliders", "Determines if we disable colliders.", DisableColliders, rTarget))
            {
                lIsDirty = true;
                DisableColliders = EditorHelper.FieldBoolValue;
            }

            if (EditorHelper.BoolField("Remove Body Shapes", "Determines if we remove the body shapes on death.", RemoveBodyShapes, rTarget))
            {
                lIsDirty = true;
                RemoveBodyShapes = EditorHelper.FieldBoolValue;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}
