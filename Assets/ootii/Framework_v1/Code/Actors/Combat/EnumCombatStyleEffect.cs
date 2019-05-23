using System;

namespace com.ootii.Actors.Combat
{
    /// <summary>
    /// Defines the effects that should be applied due to the combat style. Note that we
    /// can only go up to 32 entries.
    /// </summary>
    public partial class EnumCombatStyleEffect
    {
        public const int NONE = 0;
        public const int KNOCK_BACK = (1 << 1);
        public const int KNOCK_DOWN = (1 << 2);
        public const int STUN = (1 << 3);
        public const int DISARM = (1 << 4);
    }
}
