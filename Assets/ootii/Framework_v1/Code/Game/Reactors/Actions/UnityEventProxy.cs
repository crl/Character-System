using System;
using UnityEngine;
using UnityEngine.Events;
using com.ootii.Base;
using com.ootii.Actors.LifeCores;
using com.ootii.Messages;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace com.ootii.Reactors
{
    /// <summary>
    /// Basic reactor used for when a damage message comes in.
    /// </summary>
    [Serializable]
    [BaseName("Unity Event Proxy")]
    [BaseDescription("Reactor that calls the Unity Event when it is activated. ")]
    public class UnityEventProxy : ReactorAction
    {
        /// <summary>
        /// Index into the stored unity events
        /// </summary>
        public int _StoredUnityEventIndex = -1;

        // ActorCore the reactor belongs to
        [NonSerialized]
        protected ActorCore mActorCore = null;

        /// <summary>
        /// Default constructor
        /// </summary>
        public UnityEventProxy() : base()
        {
        }

        /// <summary>
        /// ActorCore constructor
        /// </summary>
        public UnityEventProxy(GameObject rOwner) : base(rOwner)
        {
            mActorCore = rOwner.GetComponent<ActorCore>();
        }

        /// <summary>
        /// Initialize the proxy if one doesn't exist
        /// </summary>
        public override void Awake()
        {
            if (mOwner != null)
            {
                mActorCore = mOwner.GetComponent<ActorCore>();
            }

            if (_StoredUnityEventIndex < 0)
            {
                _StoredUnityEventIndex = mActorCore.StoreUnityEvent(-1, new ReactorActionEvent());
            }
        }

        /// <summary>
        /// Called when the reactor is first activated
        /// </summary>
        /// <returns>Determines if other reactors should process.</returns>
        public override bool Activate()
        {
            base.Activate();

            // Grab and invoke the event
            if (_StoredUnityEventIndex >= 0 && _StoredUnityEventIndex < mActorCore._StoredUnityEvents.Count)
            {
                ReactorActionEvent lEvent = mActorCore._StoredUnityEvents[_StoredUnityEventIndex];
                if (lEvent != null) { lEvent.Invoke(this); }
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
        }

        #region Editor Functions

#if UNITY_EDITOR

        /// <summary>
        /// Called when the inspector needs to draw
        /// </summary>
        public override bool OnInspectorGUI(UnityEditor.SerializedObject rTargetSO, UnityEngine.Object rTarget)
        {
            bool lIsDirty = base.OnInspectorGUI(rTargetSO, rTarget);

            if (mActorCore == null && mOwner != null)
            {
                mActorCore = mOwner.GetComponent<ActorCore>();
            }

            if (_StoredUnityEventIndex < 0)
            {
                lIsDirty = true;
                _StoredUnityEventIndex = mActorCore.StoreUnityEvent(-1, new ReactorActionEvent());
            }

            //EditorGUILayout.LabelField(_StoredUnityEventIndex.ToString() + " of " + mActorCore._StoredUnityEvents.Count);

            SerializedProperty lArray = rTargetSO.FindProperty("_StoredUnityEvents");
            if (lArray.isArray && lArray.arraySize > _StoredUnityEventIndex)
            {
                SerializedProperty lItem = lArray.GetArrayElementAtIndex(_StoredUnityEventIndex);

                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(lItem);
                if (EditorGUI.EndChangeCheck())
                {
                    lIsDirty = true;
                }
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}
