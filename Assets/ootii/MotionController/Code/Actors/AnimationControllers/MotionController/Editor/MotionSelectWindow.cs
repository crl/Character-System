using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEngine;
using UnityEditor;
using com.ootii.Base;
using com.ootii.Helpers;

namespace com.ootii.Actors.AnimationControllers
{
    /// <summary>
    /// Generic selection window for types given a specific base type.
    /// </summary>
    public class MotionSelectWindow : EditorWindow
    {
        /// <summary>
        /// Delegate to set the parent list
        /// </summary>
        /// <param name="rParent"></param>
        public delegate void OnSelectedDelegate(List<Type> rTypes);

        /// <summary>
        /// Search string to initialize
        /// </summary>
        private string mSearchString = "";
        public string SearchString
        {
            get { return mSearchString; }

            set
            {
                mSearchString = value;
                MotionSelectWindow.GetFilteredMotionTypes(mFilteredTypes, mMasterTypes, mSearchString);
            }
        }

        /// <summary>
        /// Extra data that is passed back with the callback
        /// </summary>
        private object mUserData = null;
        public object UserData
        {
            get { return mUserData; }
            set { mUserData = value; }
        }

        /// <summary>
        /// Function to call when the parent is selected
        /// </summary>
        public OnSelectedDelegate OnSelectedEvent = null;

        // Items that should be shown
        private List<MotionSelectType> mFilteredTypes = new List<MotionSelectType>();

        // Master list of items to select from
        private List<MotionSelectType> mMasterTypes = new List<MotionSelectType>();

        // Master list of item categories to select from
        //private List<MotionSelectTypeTag> mMasterTypeTags = new List<MotionSelectTypeTag>();

        // Track the selected item in the list
        private int mSelectedItemIndex = -1;

        // Editor control
        private Vector2 mScrollPosition = new Vector2();

        /// <summary>
        /// Initializes the window
        /// </summary>
        public void Awake()
        {
            MotionSelectWindow.GetMasterMotionTypes(mMasterTypes);
            MotionSelectWindow.GetFilteredMotionTypes(mFilteredTypes, mMasterTypes, mSearchString);
        }

        /// <summary>
        /// Frame update for GUI objects. Heartbeat of the window that 
        /// allows us to update the UI
        /// </summary>
        public void OnGUI()
        {
            // *** ENTER SECTION ***
            GUILayout.BeginVertical(EditorHelper.GroupBox);

            GUILayout.Space(5f);

            GUILayout.BeginHorizontal();

            string lOriginalSearch = mSearchString;

            mSearchString = GUILayout.TextField(lOriginalSearch, GUI.skin.FindStyle("ToolbarSeachTextField"), GUILayout.ExpandWidth(true));
            if (mSearchString != lOriginalSearch)
            {
                mSelectedItemIndex = -1;
                MotionSelectWindow.GetFilteredMotionTypes(mFilteredTypes, mMasterTypes, mSearchString);
            }

            if (GUILayout.Button("", GUI.skin.FindStyle("ToolbarSeachCancelButton")))
            {
                GUI.FocusControl(null);

                mSearchString = "";
                mSelectedItemIndex = -1;
                MotionSelectWindow.GetFilteredMotionTypes(mFilteredTypes, mMasterTypes, mSearchString);
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(5f);

            GUILayout.EndVertical();

            // *** MID SECTION ***
            GUILayout.BeginHorizontal();

            mScrollPosition = GUILayout.BeginScrollView(mScrollPosition, ScrollArea);

            if (mFilteredTypes != null)
            {
                for (int i = 0; i < mFilteredTypes.Count; i++)
                {
                    GUILayout.BeginHorizontal();

                    mFilteredTypes[i].IsSelected = GUILayout.Toggle(mFilteredTypes[i].IsSelected, "", GUILayout.Width(16f));

                    GUIStyle lRowStyle = (i == mSelectedItemIndex ? EditorHelper.SelectedLabel : EditorHelper.Label);
                    if (GUILayout.Button(mFilteredTypes[i].Name, lRowStyle, GUILayout.MinWidth(100)))
                    {
                        mSelectedItemIndex = i;
                    }

                    GUILayout.EndHorizontal();
                }
            }

            GUILayout.EndScrollView();

            GUILayout.Space(5f);

            GUILayout.BeginVertical(EditorHelper.Box, GUILayout.MinWidth(200f));

            string lName = (mSelectedItemIndex < 0 || mSelectedItemIndex >= mFilteredTypes.Count ? "" : mFilteredTypes[mSelectedItemIndex].Name);
            GUILayout.Label(lName, TitleStyle, GUILayout.Height(17f));

            if (mSelectedItemIndex >= 0 && mSelectedItemIndex < mFilteredTypes.Count)
            {
                GUILayout.BeginHorizontal();

                string lPath = mFilteredTypes[mSelectedItemIndex].Path;

                if (GUILayout.Button(EditorGUIUtility.FindTexture("cs Script Icon"), GUI.skin.label, GUILayout.Width(20), GUILayout.Height(20)))
                {
                    foreach (var lAssetPath in AssetDatabase.GetAllAssetPaths())
                    {
                        if (lAssetPath.EndsWith(lPath))
                        {
                            var lScript = (MonoScript)AssetDatabase.LoadAssetAtPath(lAssetPath, typeof(MonoScript));
                            if (lScript != null && mFilteredTypes[mSelectedItemIndex].Type == lScript.GetClass())
                            {
                                AssetDatabase.OpenAsset(lScript);
                                break;
                            }
                        }
                    }
                }

                if (GUILayout.Button(lPath, PathStyle))
                {
                    foreach (var lAssetPath in AssetDatabase.GetAllAssetPaths())
                    {
                        if (lAssetPath.EndsWith(lPath))
                        {
                            var lScript = (MonoScript)AssetDatabase.LoadAssetAtPath(lAssetPath, typeof(MonoScript));
                            if (lScript != null && mFilteredTypes[mSelectedItemIndex].Type == lScript.GetClass())
                            {
                                AssetDatabase.OpenAsset(lScript);
                            }
                        }
                    }
                }

                GUILayout.FlexibleSpace();

                GUILayout.EndHorizontal();

                GUILayout.Space(3f);

                string lDescription = mFilteredTypes[mSelectedItemIndex].Description;
                GUILayout.Label(lDescription, DescriptionStyle);

                string lCategories = mFilteredTypes[mSelectedItemIndex].TypeTags;
                if (lCategories != null && lCategories.Length > 0)
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label("Tags: " + lCategories, TagsStyle);
                }
                else
                {
                    GUILayout.FlexibleSpace();
                }
            }
            else
            {
                GUILayout.FlexibleSpace();
            }

            GUILayout.EndVertical();

            GUILayout.EndHorizontal();

            // *** BUTTON SECTION ***
            GUILayout.BeginVertical(EditorHelper.GroupBox);

            GUILayout.Space(2f);

            GUILayout.BeginHorizontal();

            int lCount = 0;
            for (int i = 0; i < mMasterTypes.Count; i++)
            {
                if (mMasterTypes[i].IsSelected) { lCount++; }
            }

            if (GUILayout.Button("deselect all (" + lCount + ")", GUI.skin.label, GUILayout.Width(100f)))
            {
                for (int i = 0; i < mMasterTypes.Count; i++)
                {
                    mMasterTypes[i].IsSelected = false;
                }
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Select", GUILayout.Width(70)))
            {
                if (OnSelectedEvent != null)
                {
                    List<Type> lMotionTypes = new List<Type>();
                    for (int i = 0; i < mMasterTypes.Count; i++)
                    {
                        if (mMasterTypes[i].IsSelected)
                        {
                            lMotionTypes.Add(mMasterTypes[i].Type);
                        }
                    }

                    OnSelectedEvent(lMotionTypes);
                }

                mSelectedItemIndex = -1;
                Close();
            }

            if (GUILayout.Button("Cancel", GUILayout.Width(70)))
            {
                mSelectedItemIndex = -1;
                Close();
            }

            GUILayout.EndHorizontal();

            GUILayout.Space(2f);

            GUILayout.EndVertical();
        }

        /// <summary>
        /// Gathers the master list of items and categories
        /// </summary>
        /// <param name="rItems">MotionSelectItems that represent the motion types</param>
        /// <param name="rTags">Tag strings</param>
        /// <returns></returns>
        public static int GetMasterMotionTypes(List<MotionSelectType> rTypes)
        {
            Type lBaseType = typeof(MotionControllerMotion);

            rTypes.Clear();
            //rTypeTags.Clear();

            // Generate the list of motions to display
            Assembly lAssembly = Assembly.GetAssembly(lBaseType);
            Type[] lMotionTypes = lAssembly.GetTypes().OrderBy(x => x.Name).ToArray<Type>();
            for (int i = 0; i < lMotionTypes.Length; i++)
            {
                Type lType = lMotionTypes[i];
                if (lType.IsAbstract) { continue; }
                if (!lBaseType.IsAssignableFrom(lType)) { continue; }

                string lName = MotionNameAttribute.GetName(lType);
                if (lName == null || lName.Length == 0) { lName = BaseNameAttribute.GetName(lType); }

                string lDescription = MotionDescriptionAttribute.GetDescription(lType);
                if (lDescription == null || lDescription.Length == 0) { lDescription = BaseDescriptionAttribute.GetDescription(lType); }

                string lTypeTags = MotionTypeTagsAttribute.GetTypeTags(lType);

                MotionSelectType lItem = new MotionSelectType();
                lItem.Type = lType;
                lItem.Path = lType.Name + ".cs";

                //string[] lFolders = System.IO.Directory.GetFiles(Application.dataPath, lItem.Path, System.IO.SearchOption.AllDirectories);
                //if (lFolders.Length > 0) { lItem.Path = lFolders[0]; }

                lItem.Name = lName;
                lItem.Description = lDescription;
                lItem.TypeTags = lTypeTags;
                lItem.TypeTagArray = lTypeTags.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                rTypes.Add(lItem);

                //// Create the type tags
                //if (lItem.TypeTagArray != null)
                //{
                //    for (int j = 0; j < lItem.TypeTagArray.Length; j++)
                //    {
                //        bool lIsFound = false;
                //        for (int k = 0; k < rTypeTags.Count; k++)
                //        {
                //            if (string.Compare(rTypeTags[k].Tag, lItem.TypeTagArray[j], true) == 0)
                //            {
                //                lIsFound = true;
                //                break;
                //            }
                //        }

                //        if (!lIsFound)
                //        {
                //            MotionSelectTypeTag lTypeTag = new MotionSelectTypeTag();
                //            lTypeTag.Tag = lItem.TypeTagArray[j];

                //            rTypeTags.Add(lTypeTag);
                //        }
                //    }
                //}
            }

            return rTypes.Count;
        }

        /// <summary>
        /// Updates the list of filtered motion types based on the search string
        /// </summary>
        /// <param name="rFilteredTypes">List to fill with the filtered types</param>
        /// <param name="rMasterTypes">Master list of all types</param>
        /// <param name="rSearchString">Search parameters</param>
        /// <returns></returns>
        public static int GetFilteredMotionTypes(List<MotionSelectType> rFilteredTypes, List<MotionSelectType> rMasterTypes, string rSearchString)
        {
            rFilteredTypes.Clear();

            string[] lSearchItems = rSearchString.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Parse out the search values
            List<string> lSearchText = new List<string>();
            List<string> lSearchTags = new List<string>();
            for (int i = 0; i < lSearchItems.Length; i++)
            {
                string lText = lSearchItems[i];

                if (lText.Length > 4 && lText.IndexOf("tag:", StringComparison.OrdinalIgnoreCase) == 0)
                {
                    lText = lText.Substring(4);
                    if (lText.Length > 0 && !lSearchTags.Contains(lText, StringComparer.OrdinalIgnoreCase)) { lSearchTags.Add(lText); }
                }
                else
                {
                    if (lText.Length > 0 && !lSearchText.Contains(lText, StringComparer.OrdinalIgnoreCase)) { lSearchText.Add(lText); }
                }
            }

            // Generate the list of motions to display
            for (int i = 0; i < rMasterTypes.Count; i++)
            {
                MotionSelectType lItem = rMasterTypes[i];
                if (rFilteredTypes.Contains(lItem)) { continue; }

                bool lAddItem = true;

                // First, exclude all items that don't match the tag
                if (lAddItem && lSearchTags.Count > 0)
                {
                    for (int j = 0; j < lSearchTags.Count; j++)
                    {
                        if (!lItem.TypeTagArray.Contains(lSearchTags[j], StringComparer.OrdinalIgnoreCase))
                        {
                            lAddItem = false;
                        }
                    }
                }

                // Next, exclude all items that don't match the search
                if (lAddItem && lSearchText.Count > 0)
                {
                    for (int j = 0; j < lSearchText.Count; j++)
                    {
                        if (!lItem.Name.Contains(lSearchText[j], StringComparison.OrdinalIgnoreCase))
                        {
                            lAddItem = false;
                        }
                    }
                }

                if (lAddItem) { rFilteredTypes.Add(lItem); }
            }

            return rFilteredTypes.Count;
        }

        /// <summary>
        /// Style
        /// </summary>
        public static GUIStyle mScrollArea = null;
        public static GUIStyle ScrollArea
        {
            get
            {
                if (mScrollArea == null)
                {
                    Texture2D lTexture = Resources.Load<Texture2D>(EditorGUIUtility.isProSkin ? "Editor/OrangeGrayBox_pro" : "Editor/OrangeGrayBox");

                    mScrollArea = new GUIStyle(GUI.skin.box);
                    mScrollArea.normal.background = lTexture;
                    mScrollArea.padding = new RectOffset(2, 2, 2, 2);
                }

                return mScrollArea;
            }
        }

        /// <summary>
        /// Style
        /// </summary>
        private static GUIStyle mTitleStyle = null;
        public static GUIStyle TitleStyle
        {
            get
            {
                if (mTitleStyle == null)
                {
                    mTitleStyle = new GUIStyle(GUI.skin.label);
                    mTitleStyle.alignment = TextAnchor.UpperLeft;
                    mTitleStyle.fontSize = 14;
                    mTitleStyle.fontStyle = FontStyle.Bold;
                    mTitleStyle.wordWrap = false;
                }

                return mTitleStyle;
            }
        }

        /// <summary>
        /// Style
        /// </summary>
        private static GUIStyle mTagsStyle = null;
        public static GUIStyle TagsStyle
        {
            get
            {
                if (mTagsStyle == null)
                {
                    mTagsStyle = new GUIStyle(GUI.skin.label);
                    mTagsStyle.alignment = TextAnchor.UpperLeft;
                    mTagsStyle.fontSize = 10;
                    mTagsStyle.wordWrap = true;
                }

                return mTagsStyle;
            }
        }

        /// <summary>
        /// Style
        /// </summary>
        private static GUIStyle mDescriptionStyle = null;
        public static GUIStyle DescriptionStyle
        {
            get
            {
                if (mDescriptionStyle == null)
                {
                    mDescriptionStyle = new GUIStyle(GUI.skin.label);
                    mDescriptionStyle.alignment = TextAnchor.UpperLeft;
                    mDescriptionStyle.fontSize = 12;
                    mDescriptionStyle.wordWrap = true;
                }

                return mDescriptionStyle;
            }
        }

        /// <summary>
        /// Style
        /// </summary>
        private static GUIStyle mPathStyle = null;
        public static GUIStyle PathStyle
        {
            get
            {
                if (mPathStyle == null)
                {
                    mPathStyle = new GUIStyle(GUI.skin.label);
                    mPathStyle.alignment = TextAnchor.MiddleLeft;
                    mPathStyle.fontSize = 10;
                    mPathStyle.wordWrap = true;
                    mPathStyle.padding = new RectOffset(0, 0, 3, 0);
                }

                return mPathStyle;
            }
        }

        /// <summary>
        /// Defines the selection categories that exist
        /// </summary>
        public class MotionSelectTypeTag
        {
            public string Tag;

            public bool IsSelected;
        }

        /// <summary>
        /// Defines the selection items that exist
        /// </summary>
        public class MotionSelectType
        {
            public Type Type;

            public string Path;

            public string Name;

            public string Description;

            public string TypeTags;

            public string[] TypeTagArray;

            public bool IsSelected;
        }
    }
}

