using System;
using UnityEngine;
using com.ootii.Base;
using com.ootii.Actors.LifeCores;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.Combat;
using com.ootii.Messages;

namespace com.ootii.Reactors
{
    /// <summary>
    /// Basic reactor used for when a damage message comes in.
    /// </summary>
    [Serializable]
    [BaseName("Basic Attacked Reactor")]
    [BaseDescription("Basic reactor for handling messages where the owner has been attacked. Typically this is used to determine if we can avoid or block the attack.")]
    public class BasicAttackedReactor : ReactorAction
    {
        /// <summary>
        /// Determines if we send the attack to only the active motions
        /// </summary>
        public bool _LimitToActiveMotions = false;
        public bool LimitToActiveMotions
        {
            get { return _LimitToActiveMotions; }
            set { _LimitToActiveMotions = value; }
        }

        // ActorCore the reactor belongs to
        [NonSerialized]
        protected ActorCore mActorCore = null;

        /// <summary>
        /// Default constructor
        /// </summary>
        public BasicAttackedReactor() : base()
        {
            _ActivationType = 0;
        }

        /// <summary>
        /// ActorCore constructor
        /// </summary>
        public BasicAttackedReactor(GameObject rOwner) : base(rOwner)
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

            if (rMessage.ID != CombatMessage.MSG_DEFENDER_ATTACKED) { return false; }

            CombatMessage lCombatMessage = rMessage as CombatMessage;
            if (lCombatMessage != null && lCombatMessage.Defender != mActorCore.gameObject) { return false; }

            mMessage = lCombatMessage;

            return true;
        }

        /// <summary>
        /// Called when the reactor is first activated
        /// </summary>
        /// <returns>Determines if other reactors should process.</returns>
        public override bool Activate()
        {
            base.Activate();

            // Send the attack to the motion controller to see if any of the motions can handle it
            MotionController lMotionController = mActorCore.gameObject.GetComponent<MotionController>();

            // First send to any active motions. They have priority for handling.
            for (int i = 0; i < lMotionController.MotionLayers.Count; i++)
            {
                if (lMotionController.MotionLayers[i].ActiveMotion != null)
                {
                    lMotionController.MotionLayers[i].ActiveMotion.OnMessageReceived(mMessage);
                    if (mMessage.IsHandled) { break; }
                }
            }

            // If unhandled, send to all motions to see if one should handle it.
            if (!_LimitToActiveMotions && !mMessage.IsHandled)
            {
                lMotionController.SendMessage(mMessage);
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


            return lIsDirty;
        }

#endif

        #endregion
    }
}
