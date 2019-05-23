using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using com.ootii.Actors.Attributes;
using com.ootii.Helpers;

[CanEditMultipleObjects]
[CustomEditor(typeof(BasicAttributes))]
public class BasicAttributesEditor : Editor
{
    // Helps us keep track of when the list needs to be saved. This
    // is important since some changes happen in scene.
    private bool mIsDirty;

    // The actual class we're stroing
    private BasicAttributes mTarget;
    private SerializedObject mTargetSO;

    // List object for our Items
    private ReorderableList mAttributeItemList;

    // Index to the Attribute types
    private int mAttributeItemTypeIndex = 0;

    /// <summary>
    /// Called when the script object is loaded
    /// </summary>
    void OnEnable()
    {
        // Grab the serialized objects
        mTarget = (BasicAttributes)target;
        mTargetSO = new SerializedObject(target);

        // Initialize the cache
        if (!Application.isPlaying)
        {
            mTarget.Awake();
        }

        // Temporary
        int lCount = 0;
        for (int i = mTarget.Items.Count - 1; i >= 0; i--)
        {
            if (mTarget.Items[i] == null) { lCount++;  mTarget.Items.RemoveAt(i); }
        }

        if (lCount > 0 && mTarget.Items.Count == 0)
        {
            mTarget.SetAttributeValue<float>("Health", 100f);
        }

        // Create the list of items to display
        InstantiateAttributeItemList();
    }

    /// <summary>
    /// Called when the inspector needs to draw
    /// </summary>
    public override void OnInspectorGUI()
    {
        // Pulls variables from runtime so we have the latest values.
        mTargetSO.Update();

        GUILayout.Space(5);

        EditorHelper.DrawInspectorTitle("ootii Basic Attributes");

        EditorHelper.DrawInspectorDescription("Allows us to assign Attributes to actors.", MessageType.None);

        GUILayout.Space(5);

        //EditorGUILayout.LabelField("Attributes", EditorStyles.boldLabel, GUILayout.Height(16f));

        GUILayout.BeginVertical(EditorHelper.GroupBox);

        mAttributeItemList.DoLayoutList();

        if (mAttributeItemList.index >= mAttributeItemList.count) { mAttributeItemList.index = mAttributeItemList.count - 1; }

        if (mAttributeItemList.index >= 0)
        {
            GUILayout.Space(5f);
            GUILayout.BeginVertical(EditorHelper.Box);

            bool lListIsDirty = DrawAttributeItemDetail(mTarget.Items[mAttributeItemList.index]);
            if (lListIsDirty) { mIsDirty = true; }

            GUILayout.EndVertical();
        }

        EditorGUILayout.EndVertical();


        // Show the events
        GUILayout.BeginHorizontal();

        if (GUILayout.Button(new GUIContent("Events"), EditorStyles.boldLabel))
        {
            mTarget.EditorShowEvents = !mTarget.EditorShowEvents;
        }

        GUILayout.FlexibleSpace();

        if (GUILayout.Button(new GUIContent(mTarget.EditorShowEvents ? "-" : "+"), EditorStyles.boldLabel))
        {
            mTarget.EditorShowEvents = !mTarget.EditorShowEvents;
        }

        GUILayout.EndHorizontal();

        GUILayout.BeginVertical(EditorHelper.GroupBox);
        EditorHelper.DrawInspectorDescription("Assign functions to be called when specific events take place.", MessageType.None);

        if (mTarget.EditorShowEvents)
        {
            GUILayout.BeginVertical(EditorHelper.Box);

            SerializedProperty lActivatedEvent = mTargetSO.FindProperty("AttributeValueChangedEvent");
            if (lActivatedEvent != null)
            {
                EditorGUI.BeginChangeCheck();
                EditorGUILayout.PropertyField(lActivatedEvent);
                if (EditorGUI.EndChangeCheck())
                {
                    mIsDirty = true;
                }
            }

            GUILayout.EndVertical();
        }

        GUILayout.EndVertical();

        GUILayout.Space(5);

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

            // Serialize to the definitions
            mTarget.OnBeforeSerialize();

            // Clear out the dirty flag
            mIsDirty = false;
        }
    }

    #region Attribute

    /// <summary>
    /// Create the reorderable list
    /// </summary>
    private void InstantiateAttributeItemList()
    {
        mAttributeItemList = new ReorderableList(mTarget.Items, typeof(BasicAttribute), true, true, true, true);
        mAttributeItemList.drawHeaderCallback = DrawAttributeItemListHeader;
        mAttributeItemList.drawFooterCallback = DrawAttributeItemListFooter;
        mAttributeItemList.drawElementCallback = DrawAttributeItemListItem;
        mAttributeItemList.onAddCallback = OnAttributeItemListItemAdd;
        mAttributeItemList.onRemoveCallback = OnAttributeItemListItemRemove;
        mAttributeItemList.onSelectCallback = OnAttributeItemListItemSelect;
        mAttributeItemList.onReorderCallback = OnAttributeItemListReorder;
        mAttributeItemList.footerHeight = 17f;

        if (mTarget.EditorItemIndex >= 0 && mTarget.EditorItemIndex < mAttributeItemList.count)
        {
            mAttributeItemList.index = mTarget.EditorItemIndex;
        }
        else
        {
            mAttributeItemList.index = -1;
        }
    }

    /// <summary>
    /// Header for the list
    /// </summary>
    /// <param name="rRect"></param>
    private void DrawAttributeItemListHeader(Rect rRect)
    {
        EditorGUI.LabelField(rRect, "Attributes");

        Rect lNoteRect = new Rect(rRect.width + 12f, rRect.y, 11f, rRect.height);
        EditorGUI.LabelField(lNoteRect, "-", EditorStyles.miniLabel);

        if (GUI.Button(rRect, "", EditorStyles.label))
        {
            mAttributeItemList.index = -1;
            OnAttributeItemListItemSelect(mAttributeItemList);
        }
    }

    /// <summary>
    /// Allows us to draw each item in the list
    /// </summary>
    /// <param name="rRect"></param>
    /// <param name="rIndex"></param>
    /// <param name="rIsActive"></param>
    /// <param name="rIsFocused"></param>
    private void DrawAttributeItemListItem(Rect rRect, int rIndex, bool rIsActive, bool rIsFocused)
    {
        if (rIndex < mTarget.Items.Count)
        {
            BasicAttribute lItem = mTarget.Items[rIndex];

            rRect.y += 2;

            float lNameWidth = EditorGUIUtility.labelWidth;

            Rect lNameRect = new Rect(rRect.x, rRect.y, lNameWidth, EditorGUIUtility.singleLineHeight);
            EditorGUI.LabelField(lNameRect, lItem.ID);

            Rect lValueRect = new Rect(rRect.x + lNameWidth + 5f, rRect.y, rRect.width - (lNameWidth + 5f), EditorGUIUtility.singleLineHeight);
            bool lIsDirty = lItem.OnInspectorGUI(lValueRect);
            if (lIsDirty) { mIsDirty = true; }
        }
    }

    /// <summary>
    /// Footer for the list
    /// </summary>
    /// <param name="rRect"></param>
    private void DrawAttributeItemListFooter(Rect rRect)
    {
        Rect lMotionRect = new Rect(rRect.x, rRect.y + 1, rRect.width - 4 - 28 - 28, 16);
        mAttributeItemTypeIndex = EditorGUI.Popup(lMotionRect, mAttributeItemTypeIndex, EnumAttributeTypes.Names);

        Rect lAddRect = new Rect(rRect.x + rRect.width - 28 - 28 - 1, rRect.y + 1, 28, 15);
        if (GUI.Button(lAddRect, new GUIContent("+", "Add Item."), EditorStyles.miniButtonLeft)) { OnAttributeItemListItemAdd(mAttributeItemList); }

        Rect lDeleteRect = new Rect(lAddRect.x + lAddRect.width, lAddRect.y, 28, 15);
        if (GUI.Button(lDeleteRect, new GUIContent("-", "Delete Item."), EditorStyles.miniButtonRight)) { OnAttributeItemListItemRemove(mAttributeItemList); };
    }

    /// <summary>
    /// Allows us to add to a list
    /// </summary>
    /// <param name="rList"></param>
    private void OnAttributeItemListItemAdd(ReorderableList rList)
    {
        mTarget.AddAttribute("", EnumAttributeTypes.Types[mAttributeItemTypeIndex]);

        mAttributeItemList.index = mTarget.Items.Count - 1;
        OnAttributeItemListItemSelect(rList);

        mIsDirty = true;
    }

    /// <summary>
    /// Allows us process when a list is selected
    /// </summary>
    /// <param name="rList"></param>
    private void OnAttributeItemListItemSelect(ReorderableList rList)
    {
        mTarget.EditorItemIndex = rList.index;
    }

    /// <summary>
    /// Allows us to stop before removing the item
    /// </summary>
    /// <param name="rList"></param>
    private void OnAttributeItemListItemRemove(ReorderableList rList)
    {
        if (EditorUtility.DisplayDialog("Warning!", "Are you sure you want to delete the item?", "Yes", "No"))
        {
            int lIndex = rList.index;

            rList.index--;

            mTarget.RemoveAttribute(mTarget.Items[lIndex]);

            OnAttributeItemListItemSelect(rList);

            mIsDirty = true;
        }
    }

    /// <summary>
    /// Allows us to process after the motions are reordered
    /// </summary>
    /// <param name="rList"></param>
    private void OnAttributeItemListReorder(ReorderableList rList)
    {
        mIsDirty = true;
    }

    /// <summary>
    /// Renders the currently selected step
    /// </summary>
    /// <param name="rStep"></param>
    private bool DrawAttributeItemDetail(BasicAttribute rItem)
    {
        bool lIsDirty = false;

        EditorHelper.DrawSmallTitle("Attribute - " + EnumAttributeTypes.GetName(rItem.ValueType));

        bool lIsValueDirty = rItem.OnInspectorGUI(mTarget);
        if (lIsValueDirty) { lIsDirty = true; }

        return lIsDirty;
    }

    #endregion

}