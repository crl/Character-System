using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using com.ootii.Actors.Attributes;
using com.ootii.Actors.LifeCores;
using com.ootii.Base;
using com.ootii.Helpers;
using com.ootii.Reactors;

[CanEditMultipleObjects]
[CustomEditor(typeof(ActorCore))]
public class ActorCoreEditor : Editor
{
    // Helps us keep track of when the list needs to be saved. This
    // is important since some changes happen in scene.
    private bool mIsDirty;

    // The actual class we're storing
    private ActorCore mTarget;
    private SerializedObject mTargetSO;

    // Store the reactor types
    private ReorderableList mStateList;

    // Store the reactor types
    private int mReactorIndex = 0;
    private ReorderableList mReactorList;
    private List<Type> mReactorTypes = new List<Type>();
    private List<string> mReactorNames = new List<string>();

    // Store the action types
    private int mEffectIndex = 0;
    private ReorderableList mEffectList;
    private List<Type> mEffectTypes = new List<Type>();
    private List<string> mEffectNames = new List<string>();

    /// <summary>
    /// Called when the object is selected in the editor
    /// </summary>
    private void OnEnable()
    {
        // Grab the serialized objects
        mTarget = (ActorCore)target;
        mTargetSO = new SerializedObject(target);

        // Update the effects so they can update with the definitions.
        if (!UnityEngine.Application.isPlaying)
        {
            mTarget.InstantiateStates();
            mTarget.InstantiateReactors();
            mTarget.InstantiateEffects();
        }

        // Create the list of items to display
        InstantiateStateList();

        // Generate the list to display
        Assembly lReactorAssembly = Assembly.GetAssembly(typeof(ReactorAction));
        Type[] lReactorTypes = lReactorAssembly.GetTypes().OrderBy(x => x.Name).ToArray<Type>();
        for (int i = 0; i < lReactorTypes.Length; i++)
        {
            Type lType = lReactorTypes[i];
            if (lType.IsAbstract) { continue; }
            if (typeof(ReactorAction).IsAssignableFrom(lType))
            {
                mReactorTypes.Add(lType);
                mReactorNames.Add(BaseNameAttribute.GetName(lType));
            }
        }

        // Create the list of items to display
        InstantiateReactorList();

        // Generate the list to display
        Assembly lEffectAssembly = Assembly.GetAssembly(typeof(ActorCoreEffect));
        Type[] lEffectTypes = lEffectAssembly.GetTypes().OrderBy(x => x.Name).ToArray<Type>();
        for (int i = 0; i < lEffectTypes.Length; i++)
        {
            Type lType = lEffectTypes[i];
            if (lType.IsAbstract) { continue; }
            if (typeof(ActorCoreEffect).IsAssignableFrom(lType))
            {
                mEffectTypes.Add(lType);
                mEffectNames.Add(BaseNameAttribute.GetName(lType));
            }
        }

        // Create the list of items to display
        InstantiateEffectList();
    }

    /// <summary>
    /// This function is called when the scriptable object goes out of scope.
    /// </summary>
    private void OnDisable()
    {
    }

    /// <summary>
    /// Called when the inspector needs to draw
    /// </summary>
    public override void OnInspectorGUI()
    {
        // Pulls variables from runtime so we have the latest values.
        mTargetSO.Update();

        if (mReactorList.index >= mTarget._Reactors.Count)
        {
            mReactorList.index = -1;
            mTarget.EditorReactorIndex = -1;
        }

        if (mStateList.count != mTarget._States.Count)
        {
            InstantiateStateList();
        }

        if (mReactorList.count != mTarget._Reactors.Count)
        {
            InstantiateReactorList();
        }

        if (mEffectList.index >= mTarget._Effects.Count)
        {
            mEffectList.index = -1;
            mTarget.EditorEffectIndex = -1;
        }

        if (mEffectList.count != mTarget._Effects.Count)
        {
            InstantiateEffectList();
        }

        GUILayout.Space(5);

        EditorHelper.DrawInspectorTitle("ootii Actor Core");

        EditorHelper.DrawInspectorDescription("Very basic foundation for actors. This allows us to set some simple properties and control logic.", MessageType.None);

        GUILayout.Space(5);

        EditorGUILayout.LabelField("Sources", EditorStyles.boldLabel, GUILayout.Height(16f));

        GUILayout.BeginVertical(EditorHelper.Box);

        GameObject lNewAttributeSourceOwner = EditorHelper.InterfaceOwnerField<IAttributeSource>(new GUIContent("Attribute Source", "Attribute source we'll use to the actor's current health."), mTarget.AttributeSourceOwner, true);
        if (lNewAttributeSourceOwner != mTarget.AttributeSourceOwner)
        {
            mIsDirty = true;
            mTarget.AttributeSourceOwner = lNewAttributeSourceOwner;
        }

        GUILayout.EndVertical();

        GUILayout.Space(5);

        //GUILayout.BeginVertical(EditorHelper.Box);

        //if (EditorHelper.BoolField("Is Alive", "Determines if the actor is actually alive", mTarget.IsAlive))
        //{
        //    mIsDirty = true;
        //    mTarget.IsAlive = EditorHelper.FieldBoolValue;
        //}

        //if (EditorHelper.TextField("Health ID", "Attribute identifier that represents the health attribute", mTarget.HealthID))
        //{
        //    mIsDirty = true;
        //    mTarget.HealthID = EditorHelper.FieldStringValue;
        //}

        //GUILayout.Space(5);

        //if (EditorHelper.TextField("Damaged Motion", "Name of motion to activate when damage occurs and the message isn't handled.", mTarget.DamagedMotion))
        //{
        //    mIsDirty = true;
        //    mTarget.DamagedMotion = EditorHelper.FieldStringValue;
        //}

        //if (EditorHelper.TextField("Death Motion", "Name of motion to activate when death occurs and the message isn't handled.", mTarget.DeathMotion))
        //{
        //    mIsDirty = true;
        //    mTarget.DeathMotion = EditorHelper.FieldStringValue;
        //}

        //EditorGUILayout.EndVertical();

        //GUILayout.Space(5f);

        // Show the reactors
        EditorGUILayout.LabelField("States", EditorStyles.boldLabel, GUILayout.Height(16f));

        GUILayout.BeginVertical(EditorHelper.GroupBox);
        EditorHelper.DrawInspectorDescription("States allow us to store information about the actor's disposition, stance, etc.", MessageType.None);

        mStateList.DoLayoutList();

        if (mStateList.index >= 0)
        {
            GUILayout.Space(5f);
            GUILayout.BeginVertical(EditorHelper.Box);

            if (mStateList.index < mTarget._States.Count)
            {
                bool lListIsDirty = DrawStateDetailItem(mTarget._States[mStateList.index]);
                if (lListIsDirty) { mIsDirty = true; }
            }
            else
            {
                mStateList.index = -1;
            }

            GUILayout.EndVertical();
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space(5f);

        // Show the reactors
        EditorGUILayout.LabelField("Reactors", EditorStyles.boldLabel, GUILayout.Height(16f));

        GUILayout.BeginVertical(EditorHelper.GroupBox);
        EditorHelper.DrawInspectorDescription("Reactors apply logic based on a change in the actor state or messages.", MessageType.None);

        mReactorList.DoLayoutList();

        if (mReactorList.index >= 0)
        {
            GUILayout.Space(5f);
            GUILayout.BeginVertical(EditorHelper.Box);

            if (mReactorList.index < mTarget._Reactors.Count)
            {
                bool lListIsDirty = DrawReactorDetailItem(mTarget._Reactors[mReactorList.index]);
                if (lListIsDirty) { mIsDirty = true; }
            }
            else
            {
                mReactorList.index = -1;
            }

            GUILayout.EndVertical();
        }

        EditorGUILayout.EndVertical();

        GUILayout.Space(5f);

        // Show the effects
        EditorGUILayout.LabelField("Effects", EditorStyles.boldLabel, GUILayout.Height(16f));

        GUILayout.BeginVertical(EditorHelper.GroupBox);
        EditorHelper.DrawInspectorDescription("Effects that are modifying or controlling the actor. ", MessageType.None);

        mEffectList.DoLayoutList();

        if (mEffectList.index >= 0)
        {
            GUILayout.Space(5f);
            GUILayout.BeginVertical(EditorHelper.Box);

            if (mEffectList.index < mTarget._Effects.Count)
            {
                bool lListIsDirty = DrawEffectDetailItem(mTarget._Effects[mEffectList.index]);
                if (lListIsDirty) { mIsDirty = true; }
            }
            else
            {
                mEffectList.index = -1;
            }

            GUILayout.EndVertical();
        }

        EditorGUILayout.EndVertical();

        // If there is a change... update.
        if (mIsDirty)
        {
            // Flag the object as needing to be saved
            EditorUtility.SetDirty(mTarget);

#if UNITY_4_0 || UNITY_4_0_1 || UNITY_4_1 || UNITY_4_2 || UNITY_4_3 || UNITY_4_4 || UNITY_4_5 || UNITY_4_6 || UNITY_5_0 || UNITY_5_1 || UNITY_5_2
            EditorApplication.MarkSceneDirty();
#else
            if (!EditorApplication.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
            }
#endif

            // Pushes the values back to the runtime so it has the changes
            mTargetSO.ApplyModifiedProperties();

            // Clear out the dirty flag
            mIsDirty = false;
        }
    }

    #region States

    /// <summary>
    /// Create the reorderable list
    /// </summary>
    private void InstantiateStateList()
    {
        mStateList = new ReorderableList(mTarget._States, typeof(ActorCoreState), true, true, true, true);
        mStateList.drawHeaderCallback = DrawStateListHeader;
        mStateList.drawFooterCallback = DrawStateListFooter;
        mStateList.drawElementCallback = DrawStateListItem;
        mStateList.onAddCallback = OnStateListItemAdd;
        mStateList.onRemoveCallback = OnStateListItemRemove;
        mStateList.onSelectCallback = OnStateListItemSelect;
        mStateList.onReorderCallback = OnStateListReorder;
        mStateList.footerHeight = 17f;

        if (mTarget.EditorStateIndex >= 0 && mTarget.EditorStateIndex < mStateList.count)
        {
            mStateList.index = mTarget.EditorStateIndex;
        }
    }

    /// <summary>
    /// Header for the list
    /// </summary>
    /// <param name="rRect"></param>
    private void DrawStateListHeader(Rect rRect)
    {
        EditorGUI.LabelField(rRect, "States");

        Rect lNoteRect = new Rect(rRect.width + 12f, rRect.y, 11f, rRect.height);
        EditorGUI.LabelField(lNoteRect, "-", EditorStyles.miniLabel);

        if (GUI.Button(rRect, "", EditorStyles.label))
        {
            mStateList.index = -1;
            OnStateListItemSelect(mStateList);
        }
    }

    /// <summary>
    /// Allows us to draw each item in the list
    /// </summary>
    /// <param name="rRect"></param>
    /// <param name="rIndex"></param>
    /// <param name="rIsActive"></param>
    /// <param name="rIsFocused"></param>
    private void DrawStateListItem(Rect rRect, int rIndex, bool rIsActive, bool rIsFocused)
    {
        if (rIndex < mTarget._States.Count)
        {
            ActorCoreState lItem = mTarget._States[rIndex];
            if (lItem == null)
            {
                EditorGUI.LabelField(rRect, "NULL");
                return;
            }

            rRect.y += 2;

            //string lName = lItem.Name;
            //if (lName.Length == 0) { lName = BaseNameAttribute.GetName(lItem.GetType()); }

            //Rect lNameRect = new Rect(rRect.x, rRect.y, rRect.width, EditorGUIUtility.singleLineHeight);
            //EditorGUI.LabelField(lNameRect, lName);



            bool lIsDirty = false;
            float lHSpace = 5f;
            float lFixedWidth = lHSpace + 70f;

            //string lType = BaseNameAttribute.GetName(lItem.GetType());

            EditorGUILayout.BeginHorizontal();

            //Rect lTypeRect = new Rect(rRect.x, rRect.y, lFlexVSpace / 2f, EditorGUIUtility.singleLineHeight);
            //EditorGUI.LabelField(lTypeRect, lType);

            Rect lNameRect = new Rect(rRect.x, rRect.y, rRect.width - lFixedWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(lNameRect, lItem.Name);

            Rect lValueRect = new Rect(lNameRect.x + lNameRect.width + lHSpace, lNameRect.y, 70f, EditorGUIUtility.singleLineHeight);
            int lNewValue = EditorGUI.IntField(lValueRect, lItem.Value);
            if (lNewValue != lItem.Value)
            {
                lIsDirty = true;
                lItem.Value = lNewValue;
            }

            EditorGUILayout.EndHorizontal();

            // Update the item if there's a change
            if (lIsDirty)
            {
                mIsDirty = true;
            }
        }
    }

    /// <summary>
    /// Footer for the list
    /// </summary>
    /// <param name="rRect"></param>
    private void DrawStateListFooter(Rect rRect)
    {
        //Rect lMotionRect = new Rect(rRect.x, rRect.y + 1, rRect.width - 4 - 28 - 28, 16);
        //mStateIndex = EditorGUI.Popup(lMotionRect, mStateIndex, mStateNames.ToArray());

        Rect lAddRect = new Rect(rRect.x + rRect.width - 28 - 28 - 1, rRect.y + 1, 28, 15);
        if (GUI.Button(lAddRect, new GUIContent("+", "Add State."), EditorStyles.miniButtonLeft)) { OnStateListItemAdd(mStateList); }

        Rect lDeleteRect = new Rect(lAddRect.x + lAddRect.width, lAddRect.y, 28, 15);
        if (GUI.Button(lDeleteRect, new GUIContent("-", "Delete State."), EditorStyles.miniButtonRight)) { OnStateListItemRemove(mStateList); };
    }

    /// <summary>
    /// Allows us to add to a list
    /// </summary>
    /// <param name="rList"></param>
    private void OnStateListItemAdd(ReorderableList rList)
    {
        //if (mStateIndex >= mStateTypes.Count) { return; }

        //ActorCoreState lItem = Activator.CreateInstance(mStateTypes[mStateIndex]) as ActorCoreState;
        ActorCoreState lItem = new ActorCoreState();
        //lItem.ActorCore = mTarget;

        mTarget._States.Add(lItem);
        mTarget.InstantiateStates();

        mStateList.index = mTarget._States.Count - 1;
        OnStateListItemSelect(rList);

        mIsDirty = true;
    }

    /// <summary>
    /// Allows us process when a list is selected
    /// </summary>
    /// <param name="rList"></param>
    private void OnStateListItemSelect(ReorderableList rList)
    {
        mTarget.EditorStateIndex = rList.index;
    }

    /// <summary>
    /// Allows us to stop before removing the item
    /// </summary>
    /// <param name="rList"></param>
    private void OnStateListItemRemove(ReorderableList rList)
    {
        if (EditorUtility.DisplayDialog("Warning!", "Are you sure you want to delete the item?", "Yes", "No"))
        {
            int rIndex = rList.index;
            rList.index--;

            mTarget._States.RemoveAt(rIndex);
            mTarget.InstantiateStates();

            OnStateListItemSelect(rList);

            mIsDirty = true;
        }
    }

    /// <summary>
    /// Allows us to process after the motions are reordered
    /// </summary>
    /// <param name="rList"></param>
    private void OnStateListReorder(ReorderableList rList)
    {
        mIsDirty = true;
        mTarget.InstantiateStates();
    }

    /// <summary>
    /// Renders the currently selected step
    /// </summary>
    /// <param name="rStep"></param>
    private bool DrawStateDetailItem(ActorCoreState rItem)
    {
        bool lIsDirty = false;
        if (rItem == null)
        {
            EditorGUILayout.LabelField("NULL");
            return false;
        }

        if (rItem.Name.Length > 0)
        {
            EditorHelper.DrawSmallTitle(rItem.Name.Length > 0 ? rItem.Name : "Actor Core State");
        }
        else
        {
            string lName = BaseNameAttribute.GetName(rItem.GetType());
            EditorHelper.DrawSmallTitle(lName.Length > 0 ? lName : "Actor Core State");
        }

        // Render out the State specific inspectors
        bool lIsStateDirty = rItem.OnInspectorGUI(mTarget);
        if (lIsStateDirty) { lIsDirty = true; }

        if (lIsDirty)
        {
            mTarget.InstantiateStates();
        }

        return lIsDirty;
    }

    #endregion

    #region Reactors

    /// <summary>
    /// Create the reorderable list
    /// </summary>
    private void InstantiateReactorList()
    {
        mReactorList = new ReorderableList(mTarget._Reactors, typeof(ReactorAction), true, true, true, true);
        mReactorList.drawHeaderCallback = DrawReactorListHeader;
        mReactorList.drawFooterCallback = DrawReactorListFooter;
        mReactorList.drawElementCallback = DrawReactorListItem;
        mReactorList.onAddCallback = OnReactorListItemAdd;
        mReactorList.onRemoveCallback = OnReactorListItemRemove;
        mReactorList.onSelectCallback = OnReactorListItemSelect;
        mReactorList.onReorderCallback = OnReactorListReorder;
        mReactorList.footerHeight = 17f;

        if (mTarget.EditorReactorIndex >= 0 && mTarget.EditorReactorIndex < mReactorList.count)
        {
            mReactorList.index = mTarget.EditorReactorIndex;
        }
    }

    /// <summary>
    /// Header for the list
    /// </summary>
    /// <param name="rRect"></param>
    private void DrawReactorListHeader(Rect rRect)
    {
        EditorGUI.LabelField(rRect, "Reactors");

        Rect lNoteRect = new Rect(rRect.width + 12f, rRect.y, 11f, rRect.height);
        EditorGUI.LabelField(lNoteRect, "-", EditorStyles.miniLabel);

        if (GUI.Button(rRect, "", EditorStyles.label))
        {
            mReactorList.index = -1;
            OnReactorListItemSelect(mReactorList);
        }
    }

    /// <summary>
    /// Allows us to draw each item in the list
    /// </summary>
    /// <param name="rRect"></param>
    /// <param name="rIndex"></param>
    /// <param name="rIsActive"></param>
    /// <param name="rIsFocused"></param>
    private void DrawReactorListItem(Rect rRect, int rIndex, bool rIsActive, bool rIsFocused)
    {
        if (rIndex < mTarget._Reactors.Count)
        {
            ReactorAction lItem = mTarget._Reactors[rIndex];
            if (lItem == null)
            {
                EditorGUI.LabelField(rRect, "NULL");
                return;
            }

            rRect.y += 2;

            bool lIsDirty = false;
            float lHSpace = 5f;
            float lFlexVSpace = rRect.width - lHSpace - lHSpace - 40f - lHSpace - 16f;

            string lType = BaseNameAttribute.GetName(lItem.GetType());

            EditorGUILayout.BeginHorizontal();

            Rect lTypeRect = new Rect(rRect.x, rRect.y, lFlexVSpace / 2f, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(lTypeRect, lType);

            Rect lNameRect = new Rect(lTypeRect.x + lTypeRect.width + lHSpace, lTypeRect.y, lFlexVSpace / 2f, EditorGUIUtility.singleLineHeight);
            string lNewName = EditorGUI.TextField(lNameRect, lItem.Name);
            if (lNewName != lItem.Name)
            {
                lIsDirty = true;
                lItem.Name = lNewName;
            }

            Rect lPriorityRect = new Rect(lNameRect.x + lNameRect.width + lHSpace, lNameRect.y, 40f, EditorGUIUtility.singleLineHeight);
            float lNewPriority = EditorGUI.FloatField(lPriorityRect, lItem.Priority);
            if (lNewPriority != lItem.Priority)
            {
                lIsDirty = true;
                lItem.Priority = lNewPriority;
            }

            Rect lIsEnabledRect = new Rect(lPriorityRect.x + lPriorityRect.width + lHSpace, lPriorityRect.y, 16f, 16f);
            bool lNewIsEnabled = EditorGUI.Toggle(lIsEnabledRect, lItem.IsEnabled);
            if (lNewIsEnabled != lItem.IsEnabled)
            {
                lIsDirty = true;
                lItem.IsEnabled = lNewIsEnabled;
            }

            EditorGUILayout.EndHorizontal();

            // Update the item if there's a change
            if (lIsDirty)
            {
                mIsDirty = true;
                mTarget._ReactorDefinitions[rIndex] = lItem.Serialize();
            }
        }
    }

    /// <summary>
    /// Footer for the list
    /// </summary>
    /// <param name="rRect"></param>
    private void DrawReactorListFooter(Rect rRect)
    {
        Rect lMotionRect = new Rect(rRect.x, rRect.y + 1, rRect.width - 4 - 28 - 28, 16);
        mReactorIndex = EditorGUI.Popup(lMotionRect, mReactorIndex, mReactorNames.ToArray());

        Rect lAddRect = new Rect(rRect.x + rRect.width - 28 - 28 - 1, rRect.y + 1, 28, 15);
        if (GUI.Button(lAddRect, new GUIContent("+", "Add Reactor."), EditorStyles.miniButtonLeft)) { OnReactorListItemAdd(mReactorList); }

        Rect lDeleteRect = new Rect(lAddRect.x + lAddRect.width, lAddRect.y, 28, 15);
        if (GUI.Button(lDeleteRect, new GUIContent("-", "Delete Reactor."), EditorStyles.miniButtonRight)) { OnReactorListItemRemove(mReactorList); };
    }

    /// <summary>
    /// Allows us to add to a list
    /// </summary>
    /// <param name="rList"></param>
    private void OnReactorListItemAdd(ReorderableList rList)
    {
        if (mReactorIndex >= mReactorTypes.Count) { return; }

        ReactorAction lItem = Activator.CreateInstance(mReactorTypes[mReactorIndex]) as ReactorAction;
        lItem.Owner = mTarget.gameObject;

        mTarget._Reactors.Add(lItem);
        mTarget._ReactorDefinitions.Add(lItem.Serialize());

        mReactorList.index = mTarget._Reactors.Count - 1;
        OnReactorListItemSelect(rList);

        mIsDirty = true;
    }

    /// <summary>
    /// Allows us process when a list is selected
    /// </summary>
    /// <param name="rList"></param>
    private void OnReactorListItemSelect(ReorderableList rList)
    {
        mTarget.EditorReactorIndex = rList.index;
    }

    /// <summary>
    /// Allows us to stop before removing the item
    /// </summary>
    /// <param name="rList"></param>
    private void OnReactorListItemRemove(ReorderableList rList)
    {
        if (EditorUtility.DisplayDialog("Warning!", "Are you sure you want to delete the item?", "Yes", "No"))
        {
            int rIndex = rList.index;
            rList.index--;

            // Remove the assiciated event proxy if it's the last one. We can only remove
            // last ones so that we don't mess up other item indexes.
            if (mTarget._Reactors[rIndex] is UnityEventProxy)
            {
                int lEventIndex = ((UnityEventProxy)mTarget._Reactors[rIndex])._StoredUnityEventIndex;
                if (lEventIndex >= 0 && lEventIndex == mTarget._StoredUnityEvents.Count - 1)
                {
                    mTarget._StoredUnityEvents.RemoveAt(lEventIndex);
                }
            }

            // Remove the item
            mTarget._Reactors.RemoveAt(rIndex);
            mTarget._ReactorDefinitions.RemoveAt(rIndex);

            // Clear the event proxies if no one is using them
            bool lRemoveAllEvents = true;
            for (int i = 0; i < mTarget._Reactors.Count; i++)
            {
                if (mTarget._Reactors[i] is UnityEventProxy)
                {
                    lRemoveAllEvents = false;
                    break;
                }
            }

            if (lRemoveAllEvents) { mTarget._StoredUnityEvents.Clear(); }

            // Set the new index
            OnReactorListItemSelect(rList);

            mIsDirty = true;
        }
    }

    /// <summary>
    /// Allows us to process after the motions are reordered
    /// </summary>
    /// <param name="rList"></param>
    private void OnReactorListReorder(ReorderableList rList)
    {
        mIsDirty = true;

        // We need to update the motion defintions
        mTarget._ReactorDefinitions.Clear();

        for (int i = 0; i < mTarget._Reactors.Count; i++)
        {
            mTarget._ReactorDefinitions.Add(mTarget._Reactors[i].Serialize());
        }
    }

    /// <summary>
    /// Renders the currently selected step
    /// </summary>
    /// <param name="rStep"></param>
    private bool DrawReactorDetailItem(ReactorAction rItem)
    {
        bool lIsDirty = false;
        if (rItem == null)
        {
            EditorGUILayout.LabelField("NULL");
            return false;
        }

        if (rItem.Name.Length > 0)
        {
            EditorHelper.DrawSmallTitle(rItem.Name.Length > 0 ? rItem.Name : "Actor Core Reactor");
        }
        else
        {
            string lName = BaseNameAttribute.GetName(rItem.GetType());
            EditorHelper.DrawSmallTitle(lName.Length > 0 ? lName : "Actor Core Reactor");
        }

        string lDescription = BaseDescriptionAttribute.GetDescription(rItem.GetType());
        if (lDescription.Length > 0)
        {
            EditorHelper.DrawInspectorDescription(lDescription, MessageType.None);
        }

        if (EditorHelper.TextField("Name", "Friendly name of the reactor", rItem.Name, mTarget))
        {
            lIsDirty = true;
            rItem.Name = EditorHelper.FieldStringValue;
        }

        // Render out the Reactor specific inspectors
        bool lIsReactorDirty = rItem.OnInspectorGUI(mTargetSO, mTarget);
        if (lIsReactorDirty) { lIsDirty = true; }

        if (lIsDirty)
        {
            mTarget._ReactorDefinitions[mReactorList.index] = rItem.Serialize();
        }

        return lIsDirty;
    }

    #endregion

    #region Effects

    /// <summary>
    /// Create the reorderable list
    /// </summary>
    private void InstantiateEffectList()
    {
        mEffectList = new ReorderableList(mTarget._Effects, typeof(ActorCoreEffect), true, true, true, true);
        mEffectList.drawHeaderCallback = DrawEffectListHeader;
        mEffectList.drawFooterCallback = DrawEffectListFooter;
        mEffectList.drawElementCallback = DrawEffectListItem;
        mEffectList.onAddCallback = OnEffectListItemAdd;
        mEffectList.onRemoveCallback = OnEffectListItemRemove;
        mEffectList.onSelectCallback = OnEffectListItemSelect;
        mEffectList.onReorderCallback = OnEffectListReorder;
        mEffectList.footerHeight = 17f;

        if (mTarget.EditorEffectIndex >= 0 && mTarget.EditorEffectIndex < mEffectList.count)
        {
            mEffectList.index = mTarget.EditorEffectIndex;
        }
    }

    /// <summary>
    /// Header for the list
    /// </summary>
    /// <param name="rRect"></param>
    private void DrawEffectListHeader(Rect rRect)
    {
        EditorGUI.LabelField(rRect, "Actor Effects");

        Rect lNoteRect = new Rect(rRect.width + 12f, rRect.y, 11f, rRect.height);
        EditorGUI.LabelField(lNoteRect, "-", EditorStyles.miniLabel);

        if (GUI.Button(rRect, "", EditorStyles.label))
        {
            mEffectList.index = -1;
            OnEffectListItemSelect(mEffectList);
        }
    }

    /// <summary>
    /// Allows us to draw each item in the list
    /// </summary>
    /// <param name="rRect"></param>
    /// <param name="rIndex"></param>
    /// <param name="rIsActive"></param>
    /// <param name="rIsFocused"></param>
    private void DrawEffectListItem(Rect rRect, int rIndex, bool rIsActive, bool rIsFocused)
    {
        if (rIndex < mTarget._Effects.Count)
        {
            ActorCoreEffect lItem = mTarget._Effects[rIndex];
            if (lItem == null)
            {
                EditorGUI.LabelField(rRect, "NULL");
                return;
            }

            rRect.y += 2;

            string lName = lItem.Name;
            if (lName.Length == 0) { lName = BaseNameAttribute.GetName(lItem.GetType()); }

            Rect lNameRect = new Rect(rRect.x, rRect.y, rRect.width, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(lNameRect, lName);
        }
    }

    /// <summary>
    /// Footer for the list
    /// </summary>
    /// <param name="rRect"></param>
    private void DrawEffectListFooter(Rect rRect)
    {
        //if (Application.isPlaying)
        {
            Rect lMotionRect = new Rect(rRect.x, rRect.y + 1, rRect.width - 4 - 28 - 28, 16);
            mEffectIndex = EditorGUI.Popup(lMotionRect, mEffectIndex, mEffectNames.ToArray());
        }

        Rect lAddRect = new Rect(rRect.x + rRect.width - 28 - 28 - 1, rRect.y + 1, 28, 15);
        //if (Application.isPlaying)
        {
            if (GUI.Button(lAddRect, new GUIContent("+", "Add Actor Effect."), EditorStyles.miniButtonLeft)) { OnEffectListItemAdd(mEffectList); }
        }

        Rect lDeleteRect = new Rect(lAddRect.x + lAddRect.width, lAddRect.y, 28, 15);
        if (GUI.Button(lDeleteRect, new GUIContent("-", "Delete Actor Effect."), EditorStyles.miniButtonRight)) { OnEffectListItemRemove(mEffectList); };
    }

    /// <summary>
    /// Allows us to add to a list
    /// </summary>
    /// <param name="rList"></param>
    private void OnEffectListItemAdd(ReorderableList rList)
    {
        if (mEffectIndex >= mEffectTypes.Count) { return; }

        ActorCoreEffect lItem = Activator.CreateInstance(mEffectTypes[mEffectIndex]) as ActorCoreEffect;
        lItem.ActorCore = mTarget;

        mTarget._Effects.Add(lItem);
        mTarget._EffectDefinitions.Add(lItem.Serialize());

        mEffectList.index = mTarget._Effects.Count - 1;
        OnEffectListItemSelect(rList);

        mIsDirty = true;
    }

    /// <summary>
    /// Allows us process when a list is selected
    /// </summary>
    /// <param name="rList"></param>
    private void OnEffectListItemSelect(ReorderableList rList)
    {
        mTarget.EditorEffectIndex = rList.index;
    }

    /// <summary>
    /// Allows us to stop before removing the item
    /// </summary>
    /// <param name="rList"></param>
    private void OnEffectListItemRemove(ReorderableList rList)
    {
        if (EditorUtility.DisplayDialog("Warning!", "Are you sure you want to delete the item?", "Yes", "No "))
        {
            int rIndex = rList.index;
            rList.index--;

            mTarget._Effects.RemoveAt(rIndex);
            mTarget._EffectDefinitions.RemoveAt(rIndex);

            OnEffectListItemSelect(rList);

            mIsDirty = true;
        }
    }

    /// <summary>
    /// Allows us to process after the motions are reordered
    /// </summary>
    /// <param name="rList"></param>
    private void OnEffectListReorder(ReorderableList rList)
    {
        mIsDirty = true;

        // We need to update the motion defintions
        mTarget._EffectDefinitions.Clear();

        for (int i = 0; i < mTarget._Effects.Count; i++)
        {
            mTarget._EffectDefinitions.Add(mTarget._Effects[i].Serialize());
        }
    }

    /// <summary>
    /// Renders the currently selected step
    /// </summary>
    /// <param name="rStep"></param>
    private bool DrawEffectDetailItem(ActorCoreEffect rItem)
    {
        bool lIsDirty = false;
        if (rItem == null)
        {
            EditorGUILayout.LabelField("NULL");
            return false;
        }

        if (rItem.Name.Length > 0)
        {
            EditorHelper.DrawSmallTitle(rItem.Name.Length > 0 ? rItem.Name : "Actor Core Effect");
        }
        else
        {
            string lName = BaseNameAttribute.GetName(rItem.GetType());
            EditorHelper.DrawSmallTitle(lName.Length > 0 ? lName : "Actor Core Effect");
        }

        // Render out the Effect specific inspectors
        bool lIsEffectDirty = rItem.OnInspectorGUI(mTarget);
        if (lIsEffectDirty) { lIsDirty = true; }

        if (lIsDirty)
        {
            mTarget._EffectDefinitions[mEffectList.index] = rItem.Serialize();
        }

        return lIsDirty;
    }

#endregion
}
