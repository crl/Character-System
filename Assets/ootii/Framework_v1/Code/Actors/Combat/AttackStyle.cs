using System;
using UnityEngine;

namespace com.ootii.Actors.Combat
{
    /// <summary>
    /// An AttackStyle gives us details about a specific attack. We'll use
    /// these details to determine who can be hit and when.
    /// </summary>
    [Serializable]
    public class AttackStyle : ICombatStyle
    {
        /// <summary>
        /// Unique ID for the attack style (within the list)
        /// </summary>
        public string _Name = "";
        public string Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        /// <summary>
        /// Type of weapon this style can be associated with
        /// </summary>
        public string _ItemType = "";
        public string ItemType
        {
            get { return _ItemType; }
            set { _ItemType = value; }
        }

        /// <summary>
        /// Helps define the animation that is tied to the style 
        /// </summary>
        public int _Form = -1;
        public int Form
        {
            get
            {
                if (_Form > -1) { return _Form; }
                return _ParameterID;
            }

            set
            {
                _Form = value;
                _Style = value;
                _ParameterID = value;
            }
        }

        /// <summary>
        /// Parameter value sent to the animator through the attack motion
        /// </summary>
        public int _Parameter = 0;
        public int Parameter
        {
            get { return _Parameter; }
            set { _Parameter = value; }
        }

        /// <summary>
        /// Helps define the animation that is tied to the style. (Note: This is deprecated... use Form)
        /// </summary>
        public int _ParameterID = 0;
        //public int ParameterID
        //{
        //    get { return _ParameterID; }
        //    set
        //    {
        //        _Style = value;
        //        _ParameterID = value;
        //    }
        //}

        /// <summary>
        /// Helps define the animation that is tied to the style. (Note: This is deprecated... use Form)
        /// </summary>
        public int _Style = 0;
        //public int Style
        //{
        //    get { return _ParameterID; }
        //    set
        //    {
        //        _Style = value;
        //        _ParameterID = value;
        //    }
        //}

        /// <summary>
        /// Defines the inventory slot ID that holds the weapon that is doing the attack
        /// </summary>
        public string _InventorySlotID = "";
        public string InventorySlotID
        {
            get { return _InventorySlotID; }
            set { _InventorySlotID = value; }
        }

        /// <summary>
        /// Delay before the attack can be used again.
        /// </summary>
        public float _Delay = 0f;
        public float Delay
        {
            get { return _Delay; }
            set { _Delay = value; }
        }

        /// <summary>
        /// Determines if the attack is able to be stopped.
        /// </summary>
        public bool _IsInterruptible = true;
        public bool IsInterruptible
        {
            get { return _IsInterruptible; }
            set { _IsInterruptible = value; }
        }

        /// <summary>
        /// Flags that determine the effects that the combat style has.
        /// </summary>
        public int _Effects = EnumCombatStyleEffect.NONE;
        public int Effects
        {
            get { return _Effects; }
            set { _Effects = value; }
        }

        /// <summary>
        /// Direction of the attack relative to the character's forward
        /// </summary>
        public Vector3 _Forward = Vector3.forward;
        public Vector3 Forward
        {
            get { return _Forward; }
            set { _Forward = value; }
        }

        /// <summary>
        /// Horizontal field-of-attack centered on the Forward. This determines
        /// the horizontal range of the attack.
        /// </summary>
        public float _HorizontalFOA = 120f;
        public float HorizontalFOA
        {
            get { return _HorizontalFOA; }
            set { _HorizontalFOA = value; }
        }

        /// <summary>
        /// Vertical field-of-attack centered on the Forward. This determines
        /// the vertical range of the attack.
        /// </summary>
        public float _VerticalFOA = 90f;
        public float VerticalFOA
        {
            get { return _VerticalFOA; }
            set { _VerticalFOA = value; }
        }

        /// <summary>
        /// Minimum range for the attack (0 means use the combatant + weapon)
        /// </summary>
        public float _MinRange = 0f;
        public float MinRange
        {
            get { return _MinRange; }
            set { _MinRange = value; }
        }

        /// <summary>
        /// Maximum range for the attack (0 means use the combatant + weapon)
        /// </summary>
        public float _MaxRange = 0f;
        public float MaxRange
        {
            get { return _MaxRange; }
            set { _MaxRange = value; }
        }

        /// <summary>
        /// Amount to multiply the damage by
        /// </summary>
        public float _DamageModifier = 1f;
        public float DamageModifier
        {
            get { return _DamageModifier; }
            set { _DamageModifier = value; }
        }

        /// <summary>
        /// Determines the next attack style to use during a chain
        /// </summary>
        public int _NextAttackStyleIndex = -1;
        public int NextAttackStyleIndex
        {
            get { return _NextAttackStyleIndex; }
            set { _NextAttackStyleIndex = value; }
        }

        /// <summary>
        /// Track the last time the attack was used
        /// </summary>
        protected float mLastAttackTime = 0f;
        public float LastAttackTime
        {
            get { return mLastAttackTime; }
            set { mLastAttackTime = value; }
        }          

        /// <summary>
        /// Default constructor
        /// </summary>
        public AttackStyle()
        {
        }

        /// <summary>
        /// Copy constructor
        /// </summary>
        /// <param name="rSource">Source to copy from</param>
        public AttackStyle(AttackStyle rSource)
        {
            if (rSource == null) { return; }

            _Name = rSource._Name;
            _ParameterID = rSource._ParameterID;
            _Style = rSource._Style;
            _IsInterruptible = rSource._IsInterruptible;
            _Forward = rSource._Forward;
            _HorizontalFOA = rSource._HorizontalFOA;
            _VerticalFOA = rSource._VerticalFOA;
            _MinRange = rSource._MinRange;
            _MaxRange = rSource._MaxRange;
            _DamageModifier = rSource._DamageModifier;
        }
    }
}
