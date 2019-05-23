using System;
using UnityEngine;
using com.ootii.Actors.AnimationControllers;
using com.ootii.Actors.Combat;
using com.ootii.Actors.Inventory;
using com.ootii.Data.Serializers;
using com.ootii.Geometry;
using com.ootii.Helpers;

namespace com.ootii.MotionControllerPacks
{
    /// <summary>
    /// Sheathes the sword and shield and moves into the idle pose.
    /// </summary>
    [MotionName("Basic Item Store")]
    [MotionDescription("Store the item based on the specified animation style.")]
    public class BasicItemStore : MotionControllerMotion, IEquipStoreMotion
    {
        /// <summary>
        /// Preallocates string for the event tests
        /// </summary>
        public static string EVENT_STORE = "store";

        /// <summary>
        /// Trigger values for th emotion
        /// </summary>
        public int PHASE_UNKNOWN = 0;
        public int PHASE_START = 3155;

        /// <summary>
        /// Determines if we're using the IsInMotion() function to verify that
        /// the transition in the animator has occurred for this motion.
        /// </summary>
        public override bool VerifyTransition
        {
            get { return false; }
        }

        /// <summary>
        /// ID of the 'right hand' slot the sword is held in
        /// </summary>
        public string _SlotID = "RIGHT_HAND";
        public string SlotID
        {
            get { return _SlotID; }
            set { _SlotID = value; }
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
        /// Used when we don't have an Inventory Source to store what item will be stored
        /// </summary>
        protected GameObject mEquippedItem = null;
        public GameObject EquippedItem
        {
            get { return mEquippedItem; }
            set { mEquippedItem = value; }
        }

        /// <summary>
        /// Default constructor
        /// </summary>
        public BasicItemStore()
            : base()
        {
            _Pack = BasicIdle.GroupName();
            _Category = EnumMotionCategories.UNKNOWN;

            _Priority = 8f;
            _ActionAlias = "";

#if UNITY_EDITOR
            if (_EditorAnimatorSMName.Length == 0) { _EditorAnimatorSMName = "BasicEquipStore-SM"; }
#endif
        }

        /// <summary>
        /// Controller constructor
        /// </summary>
        /// <param name="rController">Controller the motion belongs to</param>
        public BasicItemStore(MotionController rController)
            : base(rController)
        {
            _Pack = BasicIdle.GroupName();
            _Category = EnumMotionCategories.UNKNOWN;

            _Priority = 8f;
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

            // Since we're using BasicInventory, it can 
            if (mInventorySource != null && !mInventorySource.AllowMotionSelfActivation) { return false; }

            // Determine if we should activate the motion
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
                if (mIsEquipped)
                {
                    mIsEquipped = false;
                    StoreItem();
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
            mIsEquipped = true;

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

            if (rEvent.stringParameter.Length == 0 || StringHelper.CleanString(rEvent.stringParameter) == EVENT_STORE)
            {
                if (mIsEquipped)
                {
                    mIsEquipped = false;
                    StoreItem();
                }
            }
        }

        /// <summary>
        /// Create the item to unsheathe
        /// </summary>
        /// <returns></returns>
        protected virtual void StoreItem()
        {
            string lSlotID = "";
            if (OverrideSlotID.Length > 0)
            {
                lSlotID = OverrideSlotID;
            }
            else
            {
                lSlotID = SlotID;
            }

            GameObject lWeapon = mEquippedItem;

            ICombatant lCombatant = mMotionController.gameObject.GetComponent<ICombatant>();
            if (lCombatant != null)
            {
                if (lCombatant.PrimaryWeapon != null)
                {
                    lWeapon = lCombatant.PrimaryWeapon.gameObject;
                    lCombatant.PrimaryWeapon.Owner = null;
                }

                lCombatant.PrimaryWeapon = null;
            }

            if (mInventorySource != null)
            {
                mInventorySource.StoreItem(lSlotID);
            }
            else
            {
                if (lWeapon == null)
                {
                    Transform lParentBone = FindTransform(mMotionController._Transform, _SlotID);
                    Transform lWeaponTransform = lParentBone.GetChild(lParentBone.childCount - 1);
                    if (lWeaponTransform != null) { lWeapon = lWeaponTransform.gameObject; }
                }

                if (lWeapon != null)
                {
                    GameObject.Destroy(lWeapon);
                    mEquippedItem = null;
                }
            }

            // Remove a body sphere we may have added
            mMotionController._ActorController.RemoveBodyShape("Combatant Shape");
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

            if (EditorHelper.TextField("Action Alias", "Action alias that is used to store the item.", ActionAlias, mMotionController))
            {
                lIsDirty = true;
                ActionAlias = EditorHelper.FieldStringValue;
            }

            GUILayout.Space(5f);

            if (EditorHelper.TextField("Slot ID", "ID of the slot the item should be held in.", SlotID, mMotionController))
            {
                lIsDirty = true;
                SlotID = EditorHelper.FieldStringValue;
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
