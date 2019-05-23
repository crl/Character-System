using System;
using UnityEngine;
using com.ootii.Actors;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.Combat;
using com.ootii.Actors.Inventory;
using com.ootii.Actors.LifeCores;
using com.ootii.Cameras;
using com.ootii.Data.Serializers;
using com.ootii.Geometry;
using com.ootii.Helpers;
using com.ootii.Timing;

namespace com.ootii.MotionControllerPacks
{
    /// <summary>
    /// Draws the sword and shield and moves into the idle pose.
    /// </summary>
    [MotionName("Basic Item Equip")]
    [MotionDescription("Equip the item based on the specified animation style.")]
    public class BasicItemEquip : MotionControllerMotion, IEquipStoreMotion
    {
        /// <summary>
        /// Preallocates string for the event tests
        /// </summary>
        public static string EVENT_EQUIP = "equip";

        /// <summary>
        /// Determines if we're using the IsInMotion() function to verify that
        /// the transition in the animator has occurred for this motion.
        /// </summary>
        public override bool VerifyTransition
        {
            get { return false; }
        }

        /// <summary>
        /// Trigger values for th emotion
        /// </summary>
        public int PHASE_UNKNOWN = 0;
        public int PHASE_START = 3150;

        /// <summary>
        /// Slot ID of the 'left hand' that the sword will be held in
        /// </summary>
        public string _SlotID = "RIGHT_HAND";
        public string SlotID
        {
            get { return _SlotID; }
            set { _SlotID = value; }
        }

        /// <summary>
        /// Item ID in the inventory to load
        /// </summary>
        public string _ItemID = "Sword_01";
        public string ItemID
        {
            get { return _ItemID; }
            set { _ItemID = value; }
        }

        /// <summary>
        /// Resource path to the sword that we'll instanciated
        /// </summary>
        public string _ResourcePath = "";
        public string ResourcePath
        {
            get { return _ResourcePath; }
            set { _ResourcePath = value; }
        }

        /// <summary>
        /// Determines if we'll add a weapon body shape to ensure 
        /// combatants don't get too close
        /// </summary>
        public bool _AddCombatantBodyShape = true;
        public bool AddCombatantBodyShape
        {
            get { return _AddCombatantBodyShape; }
            set { _AddCombatantBodyShape = value; }
        }

        /// <summary>
        /// Radius of the weapon body shape
        /// </summary>
        public float _CombatantBodyShapeRadius = 0.8f;
        public float CombatantBodyShapeRadius
        {
            get { return _CombatantBodyShapeRadius; }
            set { _CombatantBodyShapeRadius = value; }
        }

        /// <summary>
        /// Slot ID that is will hold the item. 
        /// This overrides any properties for one activation.
        /// </summary>
        [NonSerialized]
        public string _OverrideSlotID = null;

        [SerializationIgnore]
        public string OverrideSlotID
        {
            get { return _OverrideSlotID; }
            set { _OverrideSlotID = value; }
        }

        /// <summary>
        /// Item ID that is going to be unsheathed. 
        /// This overrides any properties for one activation.
        /// </summary>
        [NonSerialized]
        public string _OverrideItemID = null;

        [SerializationIgnore]
        public string OverrideItemID
        {
            get { return _OverrideItemID; }
            set { _OverrideItemID = value; }
        }

        /// <summary>
        /// Defines the source of our inventory items.
        /// </summary>
        [NonSerialized]
        protected IInventorySource mInventorySource = null;
        public IInventorySource InventorySource
        {
            get { return mInventorySource; }
            set { mInventorySource = value; }
        }

        /// <summary>
        /// Determines if the weapon is currently equipped
        /// </summary>
        protected bool mIsEquipped = false;
        public bool IsEquipped
        {
            get { return mIsEquipped; }
            set { mIsEquipped = value; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public BasicItemEquip()
            : base()
        {
            _Pack = BasicIdle.GroupName();
            _Category = EnumMotionCategories.UNKNOWN;

            _Priority = 20f;
            _ActionAlias = "";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "BasicEquipStore-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public BasicItemEquip(MotionController rController)
            : base(rController)
        {
            _Pack = BasicIdle.GroupName();
            _Category = EnumMotionCategories.UNKNOWN;

            _Priority = 20f;
            _ActionAlias = "";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "BasicEquipStore-SM"; }
#endif
        }

        /// <summary>
        /// Awake is called after all objects are initialized so you can safely speak to other objects. This is where
        /// reference can be associated.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            // If the input source is still null, see if we can grab a local input source
            if (mInventorySource == null && mMotionController != null)
            {
                mInventorySource = mMotionController.gameObject.GetComponent<IInventorySource>();
            }
        }

        /// <summary>
        /// Tests if this motion should be started. However, the motion
        /// isn't actually started.
        /// </summary>
        /// <returns></returns>
        public override bool TestActivate()
        {
            if (!mIsStartable) { return false; }
            if (!mActorController.IsGrounded) { return false; }
            if (mMotionController._InputSource == null) { return false; }
            if (mMotionLayer._AnimatorTransitionID != 0) { return false; }
            //if (mInventorySource == null) { return false; }

            // Since we're using BasicInventory, it can 
            if (mInventorySource != null && !mInventorySource.AllowMotionSelfActivation) { return false; }

            // Test if we should activate
            if (_ActionAlias.Length > 0 && mMotionController._InputSource.IsJustPressed(_ActionAlias))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Tests if the motion should continue. If it shouldn't, the motion
        /// is typically disabled
        /// </summary>
        /// <returns></returns>
        public override bool TestUpdate()
        {
            if (mIsActivatedFrame) { return true; }

            // If we've reached the exit state, leave. The delay is to ensure that we're not in an old motion's exit state
            if (mAge > 0.2f && mMotionController.State.AnimatorStates[mMotionLayer._AnimatorLayerIndex].StateInfo.IsTag("Exit"))
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Raised when a motion is being interrupted by another motion
        /// </summary>
        /// <param name="rMotion">Motion doing the interruption</param>
        /// <returns>Boolean determining if it can be interrupted</returns>
        public override bool TestInterruption(MotionControllerMotion rMotion)
        {
            if (rMotion.Category != EnumMotionCategories.DEATH)
            {
                if (!mIsEquipped)
                {
                    GameObject lItem = CreateItem();
                    if (lItem != null)
                    {
                        mIsEquipped = true;
                    }
                }
            }

            return base.TestInterruption(rMotion);
        }

        /// <summary>
        /// Called to start the specific motion. If the motion
        /// were something like 'jump', this would start the jumping process
        /// </summary>
        /// <param name="rPrevMotion">Motion that this motion is taking over from</param>
        public override bool Activate(MotionControllerMotion rPrevMotion)
        {
            mIsEquipped = false;

            // If we already have equipment in hand, we don't need to run this motion
            string lItemID = (_OverrideItemID != null && _OverrideItemID.Length > 0 ? _OverrideItemID : _ItemID);
            string lSlotID = (_OverrideSlotID != null && _OverrideSlotID.Length > 0 ? _OverrideSlotID : _SlotID);

            if (mInventorySource != null)
            {
                string lEquippedItemID = mInventorySource.GetItemID(lSlotID);
                if (lEquippedItemID != null && lEquippedItemID.Length > 0)
                {
                    if (lItemID != null && lItemID.Length > 0 && lItemID == lEquippedItemID)
                    {
                        return false;
                    }
                }
            }

            // Trigger the animation
            mMotionController.SetAnimatorMotionPhase(mMotionLayer.AnimatorLayerIndex, PHASE_START, _Form, 0, true);
            return base.Activate(rPrevMotion);
        }

        /// <summary>
        /// Raised when we shut the motion down
        /// </summary>
        public override void Deactivate()
        {
            // Clear for the next activation
            _OverrideSlotID = "";
            _OverrideItemID = "";

            // Continue with the deactivation
            base.Deactivate();
        }

        /// <summary>
        /// Allows the motion to modify the velocity before it is applied.
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        /// <param name="rMovement">Amount of movement caused by root motion this frame</param>
        /// <param name="rRotation">Amount of rotation caused by root motion this frame</param>
        /// <returns></returns>
        public override void UpdateRootMotion(float rDeltaTime, int rUpdateIndex, ref Vector3 rMovement, ref Quaternion rRotation)
        {
            rMovement = Vector3.zero;
            rRotation = Quaternion.identity;
        }

        /// <summary>
        /// Updates the motion over time. This is called by the controller
        /// every update cycle so animations and stages can be updated.
        /// </summary>
        /// <param name="rDeltaTime">Time since the last frame (or fixed update call)</param>
        /// <param name="rUpdateIndex">Index of the update to help manage dynamic/fixed updates. [0: Invalid update, >=1: Valid update]</param>
        public override void Update(float rDeltaTime, int rUpdateIndex)
        {
        }

        /// <summary>
        /// Raised by the animation when an event occurs
        /// </summary>
        public override void OnAnimationEvent(AnimationEvent rEvent)
        {
            if (rEvent == null) { return; }

            if (rEvent.stringParameter.Length == 0 || StringHelper.CleanString(rEvent.stringParameter) == EVENT_EQUIP)
            {
                if (!mIsEquipped)
                {
                    GameObject lItem = CreateItem();
                    if (lItem != null)
                    {
                        mIsEquipped = true;
                    }
                }
            }
        }

        /// <summary>
        /// Rotates the actor to the view over time
        /// </summary>
        protected virtual void RotateToView(float rSpeed)
        {
            // Grab the angle needed to get to our target forward
            Vector3 lCameraForward = mMotionController._CameraTransform.forward;
            float lAvatarToCamera = NumberHelper.GetHorizontalAngle(mMotionController._Transform.forward, lCameraForward, mMotionController._Transform.up);
            if (lAvatarToCamera == 0f) { return; }

            // If we have a camera, force it to the direction of the character
            BaseCameraRig lCameraRig = mMotionController.CameraRig as BaseCameraRig;
            if (lCameraRig is BaseCameraRig)
            {
                (lCameraRig).FrameLockForward = true;
            }

            float lInputFromSign = Mathf.Sign(lAvatarToCamera);
            float lInputFromAngle = Mathf.Abs(lAvatarToCamera);
            float lRotationAngle = (rSpeed / 60f) * TimeManager.Relative60FPSDeltaTime;

            // Establish the link if we're close enough
            if (lInputFromAngle <= lRotationAngle)
            {
                lRotationAngle = lInputFromAngle;
            }

            // Use the information and AC to determine our final rotation
            mRotation = Quaternion.AngleAxis(lInputFromSign * lRotationAngle, mMotionController._Transform.up);
        }

        /// <summary>
        /// Create the item to unsheathe
        /// </summary>
        /// <returns></returns>
        protected virtual GameObject CreateItem()
        {
            string lResourcePath = "";

            string lItemID = "";
            if (OverrideItemID != null && OverrideItemID.Length > 0)
            {
                lItemID = OverrideItemID;
            }
            else if (ResourcePath.Length > 0)
            {
                lResourcePath = ResourcePath;
            }
            else if (ItemID.Length > 0)
            {
                lItemID = ItemID;
            }

            string lSlotID = "";
            if (OverrideSlotID != null && OverrideSlotID.Length > 0)
            {
                lSlotID = OverrideSlotID;
            }
            else
            {
                lSlotID = SlotID;
            }

            ICombatant lCombatant = mMotionController.gameObject.GetComponent<ICombatant>();

            GameObject lItem = null;
            if (mInventorySource != null)
            {
                lItem = mInventorySource.EquipItem(lItemID, lSlotID, lResourcePath);
            }
            else
            {
                lItem = EquipItem(lItemID, lSlotID, lResourcePath);
            }

            if (lCombatant != null)
            {
                try
                {
                    lCombatant.PrimaryWeapon = lItem.GetComponent<IWeaponCore>();
                    if (lCombatant.PrimaryWeapon != null)
                    {
                        lCombatant.PrimaryWeapon.Owner = mMotionController.gameObject;
                    }
                }
                catch { }

                // Add another body shape in order to compensate for the pose
                if (_AddCombatantBodyShape)
                {
                    BodyCapsule lShape = new BodyCapsule();
                    lShape.Name = "Combatant Shape";
                    lShape.Radius = _CombatantBodyShapeRadius;
                    lShape.Offset = new Vector3(0f, 1.0f, 0f);
                    lShape.EndOffset = new Vector3(0f, 1.2f, 0f);
                    lShape.IsEnabledOnGround = true;
                    lShape.IsEnabledOnSlope = true;
                    lShape.IsEnabledAboveGround = true;
                    mActorController.AddBodyShape(lShape);
                }
            }

            return lItem;
        }

        /// <summary>
        /// Instantiates the specified item and equips it. We return the instantiated item.
        /// </summary>
        /// <param name="rItemID">String representing the name or ID of the item to equip</param>
        /// <param name="rSlotID">String representing the name or ID of the slot to equip</param>
        /// <param name="rResourcePath">Alternate resource path to override the ItemID's</param>
        /// <returns>GameObject that is the instance or null if it could not be created</returns>
        public virtual GameObject EquipItem(string rItemID, string rSlotID, string rResourcePath = "")
        {
            string lResourcePath = rResourcePath;

            Vector3 lLocalPosition = Vector3.zero;
            Quaternion lLocalRotation = Quaternion.identity;

            GameObject lGameObject = CreateAndMountItem(mMotionController.gameObject, lResourcePath, lLocalPosition, lLocalRotation, rSlotID);

            if (lGameObject != null)
            {
                IItemCore lItemCore = lGameObject.GetComponent<IItemCore>();
                if (lItemCore != null) { lItemCore.OnEquipped(); }
            }

            return lGameObject;
        }

        /// <summary>
        /// Creates the item and attaches it to the parent mount point
        /// </summary>
        /// <param name="rParent">GameObject that is the parent (typically a character)</param>
        /// <param name="rResourcePath">String that is the resource path to the item</param>
        /// <param name="rLocalPosition">Position the item will have relative to the parent mount point</param>
        /// <param name="rLocalRotation">Rotation the item will have relative to the parent mount pont</param>
        /// <returns></returns>
        protected virtual GameObject CreateAndMountItem(GameObject rParent, string rResourcePath, Vector3 rLocalPosition, Quaternion rLocalRotation, string rParentMountPoint = "Left Hand", string rItemMountPoint = "Handle")
        {
            GameObject lItem = null;

            if (rResourcePath.Length > 0)
            {
                // Create and mount if we need to
                if (lItem == null)
                {
                    Animator lAnimator = rParent.GetComponentInChildren<Animator>();
                    if (lAnimator != null)
                    {
                        UnityEngine.Object lResource = Resources.Load(rResourcePath);
                        if (lResource != null)
                        {
                            lItem = GameObject.Instantiate(lResource) as GameObject;
                            MountItem(rParent, lItem, rLocalPosition, rLocalRotation, rParentMountPoint);
                        }
                        else
                        {
                            Debug.LogWarning("Resource not found. Resource Path: " + rResourcePath);
                        }
                    }
                }
                // Inform the combatant of the change
                else
                {
                    ICombatant lCombatant = mMotionController.gameObject.GetComponent<ICombatant>();
                    if (lCombatant != null)
                    {
                        IWeaponCore lWeaponCore = lItem.GetComponent<IWeaponCore>();
                        if (lWeaponCore != null)
                        {
                            string lCleanParentMountPoint = StringHelper.CleanString(rParentMountPoint);
                            if (lCleanParentMountPoint == "righthand")
                            {
                                lCombatant.PrimaryWeapon = lWeaponCore;
                            }
                            else if (lCleanParentMountPoint == "lefthand" || lCleanParentMountPoint == "leftlowerarm")
                            {
                                lCombatant.SecondaryWeapon = lWeaponCore;
                            }
                        }
                    }
                }
            }

            return lItem;
        }

        /// <summary>
        /// Mounts the item to the specified position based on the ItemCore
        /// </summary>
        /// <param name="rParent">Parent GameObject</param>
        /// <param name="rItem">Child GameObject that is this item</param>
        /// <param name="rLocalPosition">Vector3 that is the local position to set when the item is parented.</param>
        /// <param name="rLocalRotation">Quaternion that is the local rotation to set when the item is parented.</param>
        /// <param name="rParentMountPoint">Name of the parent mount point we're tying the item to</param>
        /// <param name="rItemMountPoint">Name of the child mount point we're tying the item to</param>
        protected virtual void MountItem(GameObject rParent, GameObject rItem, Vector3 rLocalPosition, Quaternion rLocalRotation, string rParentMountPoint, string rItemMountPoint = "Handle")
        {
            if (rParent == null || rItem == null) { return; }

            bool lIsConnected = false;

            if (!lIsConnected)
            {
                Transform lParentBone = FindTransform(rParent.transform, rParentMountPoint);
                rItem.transform.parent = lParentBone;

                //IItemCore lItemCore = InterfaceHelper.GetComponent<IItemCore>(rItem);
                IItemCore lItemCore = rItem.GetComponent<IItemCore>();
                if (lItemCore != null)
                {
                    lItemCore.Owner = mMotionController.gameObject;

                    if (rLocalPosition.sqrMagnitude == 0f && QuaternionExt.IsIdentity(rLocalRotation))
                    {
                        rItem.transform.localPosition = (lItemCore != null ? lItemCore.LocalPosition : Vector3.zero);
                        rItem.transform.localRotation = (lItemCore != null ? lItemCore.LocalRotation : Quaternion.identity);
                    }
                    else
                    {
                        rItem.transform.localPosition = rLocalPosition;
                        rItem.transform.localRotation = rLocalRotation;
                    }
                }
                else
                {
                    rItem.transform.localPosition = rLocalPosition;
                    rItem.transform.localRotation = rLocalRotation;
                }
            }

            if (rItem != null)
            {
                // Reenable the item as needed
                rItem.SetActive(true);
                rItem.hideFlags = HideFlags.None;

                // Inform the combatant of the change
                ICombatant lCombatant = mMotionController.gameObject.GetComponent<ICombatant>();
                if (lCombatant != null)
                {
                    IWeaponCore lWeaponCore = rItem.GetComponent<IWeaponCore>();
                    if (lWeaponCore != null)
                    {
                        string lCleanParentMountPoint = StringHelper.CleanString(rParentMountPoint);
                        if (lCleanParentMountPoint == "righthand")
                        {
                            lCombatant.PrimaryWeapon = lWeaponCore;
                        }
                        else if (lCleanParentMountPoint == "lefthand" || lCleanParentMountPoint == "leftlowerarm")
                        {
                            lCombatant.SecondaryWeapon = lWeaponCore;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Attempts to find a matching transform
        /// </summary>
        /// <param name="rParent">Parent transform where we'll start the search</param>
        /// <param name="rName">Name or identifier of the transform we want</param>
        /// <returns>Transform matching the name or the parent if not found</returns>
        protected Transform FindTransform(Transform rParent, string rName)
        {
            Transform lTransform = null;

            // Check by HumanBone name
            if (lTransform == null)
            {
                Animator lAnimator = rParent.GetComponentInChildren<Animator>();
                if (lAnimator != null)
                {
                    if (BasicInventory.UnityBones == null)
                    {
                        BasicInventory.UnityBones = System.Enum.GetNames(typeof(HumanBodyBones));
                        for (int i = 0; i < BasicInventory.UnityBones.Length; i++)
                        {
                            BasicInventory.UnityBones[i] = StringHelper.CleanString(BasicInventory.UnityBones[i]);
                        }
                    }

                    string lCleanName = StringHelper.CleanString(rName);
                    for (int i = 0; i < BasicInventory.UnityBones.Length; i++)
                    {
                        if (BasicInventory.UnityBones[i] == lCleanName)
                        {
                            lTransform = lAnimator.GetBoneTransform((HumanBodyBones)i);
                            break;
                        }
                    }
                }
            }

            // Check if by exact name
            if (lTransform == null)
            {
                lTransform = rParent.transform.FindTransform(rName);
            }

            // Default to the root
            if (lTransform == null)
            {
                lTransform = rParent.transform;
            }

            return lTransform;
        }

        // **************************************************************************************************
        // Following properties and function only valid while editing
        // **************************************************************************************************

#if UNITY_EDITOR

        /// <summary>
        /// Allow the constraint to render it's own GUI
        /// </summary>
        /// <returns>Reports if the object's value was changed</returns>
        public override bool OnInspectorGUI()
        {
            bool lIsDirty = false;

            if (EditorHelper.IntField("Form", "Within the animator state, defines which animator flow will run. This is used to control animations.", Form, mMotionController))
            {
                lIsDirty = true;
                Form = EditorHelper.FieldIntValue;
            }

            if (EditorHelper.TextField("Action Alias", "Action alias that is used to trigger the equipping of the item.", ActionAlias, mMotionController))
            {
                lIsDirty = true;
                ActionAlias = EditorHelper.FieldStringValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.TextField("Slot ID", "Slot ID of the slot the item should be held in.", SlotID, mMotionController))
            {
                lIsDirty = true;
                SlotID = EditorHelper.FieldStringValue;
            }

            if (EditorHelper.TextField("Item ID", "Item ID that defines the item that will be equipped.", ItemID, mMotionController))
            {
                lIsDirty = true;
                ItemID = EditorHelper.FieldStringValue;
            }

            string lNewResourcePath = EditorHelper.FileSelect(new GUIContent("Resource Path", "Override path to the prefab resource that is the item."), ResourcePath, "fbx,prefab");
            if (lNewResourcePath != ResourcePath)
            {
                lIsDirty = true;
                ResourcePath = lNewResourcePath;
            }

            GUILayout.Space(5f);

            if (EditorHelper.BoolField("Add Body Shape", "Determines if we'll add an extra body shape to account for the stance.", AddCombatantBodyShape, mMotionController))
            {
                lIsDirty = true;
                AddCombatantBodyShape = EditorHelper.FieldBoolValue;
            }

            if (AddCombatantBodyShape)
            {
                if (EditorHelper.FloatField("Body Shape Radius", "Radius to make the body shape.", CombatantBodyShapeRadius, mMotionController))
                {
                    lIsDirty = true;
                    CombatantBodyShapeRadius = EditorHelper.FieldFloatValue;
                }
            }

            return lIsDirty;
        }

#endif

        #region Auto-Generated
        // ************************************ START AUTO GENERATED ************************************

        /// <summary>
        /// Determines if we're using auto-generated code
        /// </summary>
        public override bool HasAutoGeneratedCode
        {
            get { return true; }
        }

#if UNITY_EDITOR

        /// <summary>
        /// New way to create sub-state machines without destroying what exists first.
        /// </summary>
        protected override void CreateStateMachine()
        {
            int rLayerIndex = mMotionLayer._AnimatorLayerIndex;
            MotionController rMotionController = mMotionController;

            UnityEditor.Animations.AnimatorController lController = null;

            Animator lAnimator = rMotionController.Animator;
            if (lAnimator == null) { lAnimator = rMotionController.gameObject.GetComponent<Animator>(); }
            if (lAnimator != null) { lController = lAnimator.runtimeAnimatorController as UnityEditor.Animations.AnimatorController; }
            if (lController == null) { return; }

            while (lController.layers.Length <= rLayerIndex)
            {
                UnityEditor.Animations.AnimatorControllerLayer lNewLayer = new UnityEditor.Animations.AnimatorControllerLayer();
                lNewLayer.name = "Layer " + (lController.layers.Length + 1);
                lNewLayer.stateMachine = new UnityEditor.Animations.AnimatorStateMachine();
                lController.AddLayer(lNewLayer);
            }

            UnityEditor.Animations.AnimatorControllerLayer lLayer = lController.layers[rLayerIndex];

            UnityEditor.Animations.AnimatorStateMachine lLayerStateMachine = lLayer.stateMachine;

            UnityEditor.Animations.AnimatorStateMachine lSSM_37928 = MotionControllerMotion.EditorFindSSM(lLayerStateMachine, "BasicEquipStore-SM");
            if (lSSM_37928 == null) { lSSM_37928 = lLayerStateMachine.AddStateMachine("BasicEquipStore-SM", new Vector3(192, -1008, 0)); }

#if USE_ARCHERY_MP || OOTII_AYMP
            ArcheryPackDefinition.ExtendBasicEquipStore(rMotionController, rLayerIndex);
#endif

#if USE_SWORD_SHIELD_MP || OOTII_SSMP
            SwordShieldPackDefinition.ExtendBasicEquipStore(rMotionController, rLayerIndex);
#endif

#if USE_SPELL_CASTING_MP || OOTII_SCMP
            SpellCastingPackDefinition.ExtendBasicEquipStore(rMotionController, rLayerIndex);
#endif

#if USE_SHOOTER_MP || OOTII_SHMP
            ShooterPackDefinition.ExtendBasicEquipStore(rMotionController, rLayerIndex);
#endif


            // Run any post processing after creating the state machine
            OnStateMachineCreated();
        }

#endif

        // ************************************ END AUTO GENERATED ************************************
        #endregion
    }
}
