namespace com.ootii.Helpers
{
    /// <summary>
    /// Static functions to help us deal with enumerations that are 'Flags'. Meaning
    /// we can have more than one value stored in the int. Note, we can only go up to
    /// 32 values.
    /// </summary>
    public class FlagsHelper
    {
        /// <summary>
        /// Determines if the test value is part of the set of values
        /// </summary>
        /// <param name="rValues">Variable that holds our combined values</param>
        /// <param name="rTest">Individual value we are testing for</param>
        /// <returns></returns>
        public static bool ContainsValue(int rValues, int rTest)
        {
            return (rValues & rTest) != 0;
        }

        /// <summary>
        /// Adds the specified value to our set of values
        /// </summary>
        /// <param name="rValues">Variable that holds our combined values.</param>
        /// <param name="rValue">Value that we want to add.</param>
        /// <returns>New combined values with the specified value</returns>
        public static int AddValue(int rValues, int rValue)
        {
            return (rValues | rValue);
        }

        /// <summary>
        /// Adds the specified value to our set of values
        /// </summary>
        /// <param name="rValues">Variable that holds our combined values.</param>
        /// <param name="rValue">Value that we want to add.</param>
        public static void AddValue(ref int rValues, int rValue)
        {
            rValues = (rValues | rValue);
        }

        /// <summary>
        /// Removes the specified value from our set of values
        /// </summary>
        /// <param name="rValues">Variable that holds our combined values</param>
        /// <param name="rValue">Value that we want to remove.</param>
        /// <returns>New combined values with out the specified value</returns>
        public static int RemoveValue(int rValues, int rValue)
        {
            return (rValues & (~rValue));
        }

        /// <summary>
        /// Removes the specified value from our set of values
        /// </summary>
        /// <param name="rValues">Variable that holds our combined values</param>
        /// <param name="rValue">Value that we want to remove.</param>
        public static void RemoveValue(ref int rValues, int rValue)
        {
            rValues = (rValues & (~rValue));
        }
    }
}