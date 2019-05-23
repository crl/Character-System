using System;
using UnityEngine;
using com.ootii.Base;
using com.ootii.Actors.LifeCores;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.Combat;
using com.ootii.Helpers;
using com.ootii.Messages;

namespace com.ootii.Reactors
{
    /// <summary>
    /// Basic reactor used for when a damage message comes in.
    /// </summary>
    [Serializable]
    [BaseName("Basic Damaged Reactor")]
    [BaseDescription("Basic reactor for handling messages of type DamageMessage. This is where damage is applied and any other effects or animations should be activated.")]
    public class BasicDamagedReactor : ReactorAction
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
        /// Motion name to use when damage is taken
        /// </summary>
        public string _DamagedMotion = "BasicDamaged";
        public string DamagedMotion
        {
            get { return _DamagedMotion; }
            set { _DamagedMotion = value; }
        }

        // ActorCore the reactor belongs to
        [NonSerialized]
        protected ActorCore mActorCore = null;

        /// <summary>
        /// Default constructor
        /// </summary>
        public BasicDamagedReactor() : base()
        {
            _ActivationType = 0;
        }

        /// <summary>
        /// ActorCore constructor
        /// </summary>
        public BasicDamagedReactor(GameObject rOwner) : base(rOwner)
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
            if (mActorCore == null || !mActorCore.IsAlive) { return false; }

            DamageMessage lDamageMessage = rMessage as DamageMessage;
            if (lDamageMessage == null) { return false; }

            if (lDamageMessage.Damage == 0f) { return false; }

            if (lDamageMessage.ID != 0)
            {
                if (lDamageMessage.ID == CombatMessage.MSG_DEFENDER_KILLED) { return false; }
                if (lDamageMessage.ID != CombatMessage.MSG_DEFENDER_ATTACKED) { return false; }
            }

            CombatMessage lCombatMessage = rMessage as CombatMessage;
            if (lCombatMessage != null && lCombatMessage.Defender != mActorCore.gameObject) { return false; }

            mMessage = lDamageMessage;

            return true;
        }

        /// <summary>
        /// Called when the reactor is first activated
        /// </summary>
        /// <returns>Determines if other reactors should process.</returns>
        public override bool Activate()
        {
            base.Activate();

            float lRemainingHealth = 0f;
            if (mActorCore.AttributeSource != null)
            {
                lRemainingHealth = mActorCore.AttributeSource.GetAttributeValue<float>(HealthID) - ((DamageMessage)mMessage).Damage;
                mActorCore.AttributeSource.SetAttributeValue(HealthID, lRemainingHealth);
            }

            // Forward to a death reactor
            if (lRemainingHealth <= 0f)
            {
                mMessage.ID = CombatMessage.MSG_DEFENDER_KILLED;
                mActorCore.SendMessage(mMessage);
            }
            // Process the damage
            else if (mMessage != null)
            {
                bool lPlayAnimation = ((DamageMessage)mMessage).AnimationEnabled;

                if (lPlayAnimation)
                {
                    MotionController lMotionController = mActorCore.gameObject.GetComponent<MotionController>();
                    if (lMotionController != null)
                    {
                        // Send the message to the MC to let it activate
                        mMessage.ID = CombatMessage.MSG_DEFENDER_DAMAGED;
                        lMotionController.SendMessage(mMessage);
                    }

                    if (!mMessage.IsHandled && DamagedMotion.Length > 0)
                    {
                        MotionControllerMotion lMotion = null;
                        if (lMotionController != null) { lMotion = lMotionController.GetMotion(DamagedMotion); }

                        if (lMotion != null)
                        {
                            lMotionController.ActivateMotion(lMotion);
                        }
                        else
                        {
                            int lID = Animator.StringToHash(DamagedMotion);
                            if (lID != 0)
                            {
                                Animator lAnimator = mActorCore.gameObject.GetComponent<Animator>();
                                if (lAnimator != null) { lAnimator.CrossFade(DamagedMotion, 0.25f, 0); }
                            }
                        }
                    }
                }
            }

            // Disable the reactor
            Deactivate();

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

            if (EditorHelper.TextField("Damaged Motion", "Name of motion to activate when damage occurs and the message isn't handled.", DamagedMotion, rTarget))
            {
                lIsDirty = true;
                DamagedMotion = EditorHelper.FieldStringValue;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}
