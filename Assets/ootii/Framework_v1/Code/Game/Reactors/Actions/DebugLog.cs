using System;
using UnityEngine;
using com.ootii.Base;
using com.ootii.Helpers;

namespace com.ootii.Reactors
{
    /// <summary>
    /// Basic reactor used for when a death message comes in.
    /// </summary>
    [Serializable]
    [BaseName("Debug Log")]
    [BaseDescription("Writes a debug statement to the console.")]
    public class DebugLog : ReactorAction
    {
        /// <summary>
        /// Text to write to the console
        /// </summary>
        public string _Text = "";
        public string Text
        {
            get { return _Text; }
            set { _Text = value; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public DebugLog() : base()
        {
        }

        /// <summary>
        /// ActorCore constructor
        /// </summary>
        public DebugLog(GameObject rOwner) : base(rOwner)
        {
        }

        /// <summary>
        /// Called when the reactor is first activated
        /// </summary>
        /// <returns>Determines if other reactors should process.</returns>
        public override bool Activate()
        {
            base.Activate();

            string lName = (mOwner != null ? mOwner.name : "");
            int lID = (mMessage != null ? mMessage.ID : 0);
            Debug.Log(string.Format("[{0:f3}] name:{1} id:{2} msg:{3}", Time.time, lName, lID, _Text));

            Deactivate();

            // Allow other reactors to continue
            return true;
        }

        #region Editor Functions

#if UNITY_EDITOR

        /// <summary>
        /// Called when the inspector needs to draw
        /// </summary>
        public override bool OnInspectorGUI(UnityEditor.SerializedObject rTargetSO, UnityEngine.Object rTarget)
        {
            bool lIsDirty = base.OnInspectorGUI(rTargetSO, rTarget);

            if (EditorHelper.TextField("Text", "Text to write to the console.", Text, rTarget))
            {
                lIsDirty = true;
                Text = EditorHelper.FieldStringValue;
            }

            return lIsDirty;
        }

#endif

        #endregion
    }
}
