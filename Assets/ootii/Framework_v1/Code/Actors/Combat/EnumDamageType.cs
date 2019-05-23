namespace com.ootii.Actors.Combat
{
    /// <summary>
    /// Defines the types of standard damage we are dealing with
    /// </summary>
    public partial class EnumDamageType
    {
        /// <summary>
        /// Enum values
        /// </summary>
        public const int OTHER = 0;
        public const int PHYSICAL = 1;
        public const int FIRE = 2;
        public const int COLD = 3;
        public const int ELECTRIC = 4;
        public const int POISON = 5;
        public const int ACID = 6;
        public const int SOUND = 7;
        public const int SMELL = 8;
        public const int ARCANE = 9;
        public const int PSYCHIC = 10;
        public const int HOLY = 11;
        public const int UNHOLY = 12;

        /// <summary>
        /// Contains a mapping from ID to names
        /// </summary>
        public static string[] Names = new string[] 
        {
            "Other",
            "Physical",
            "Fire",
            "Cold",
            "Electric",
            "Poison",
            "Acid",
            "Sound",
            "Smell",
            "Arcane",
            "Psychic",
            "Holy",
            "Unholy"
        };

        /// <summary>
        /// Retrieve the index of the specified name
        /// </summary>
        /// <param name="rName">Name of the enumeration</param>
        /// <returns>ID of the enumeration or 0 if it's not found</returns>
        public static int GetEnum(string rName)
        {
            for (int i = 0; i < Names.Length; i++)
            {
                if (Names[i].ToLower() == rName.ToLower()) { return i; }
            }

            return 0;
        }
    }
}
