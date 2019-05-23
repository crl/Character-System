namespace com.ootii.Actors.Combat
{
    /// <summary>
    /// Defines the ways that damage can be applied
    /// </summary>
    public partial class EnumImpactType
    {
        /// <summary>
        /// Enum values
        /// </summary>
        public const int OTHER = 0;
        public const int SLASH = 1;
        public const int PIERCE = 2;
        public const int BLUDGEON = 3;
        public const int IMMERSE = 4;
        public const int MENTAL = 5;        

        /// <summary>
        /// Contains a mapping from ID to names
        /// </summary>
        public static string[] Names = new string[] 
        {
            "Other",
            "Slash",
            "Pierce",
            "Bludgeon",
            "Immerse",
            "Mental",
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
