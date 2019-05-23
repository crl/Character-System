namespace com.ootii.Actors.LifeCores
{
    /// <summary>
    /// A state source is used to help manage the state or life of actors. This can be used
    /// to determine what actors are doing, how they are feeling, etc.
    /// </summary>
    public interface IActorStateSource
    {
        /// <summary>
        /// Determines if the state variable actually exists.
        /// </summary>
        /// <param name="rName">Name of the state variable whose value we are interested in</param>
        /// <returns>Boolean that determines if the state variable actually exists</returns>
        bool StateExists(string rName);

        /// <summary>
        /// Removes the specified state variable.
        /// </summary>
        /// <param name="rName">Name of the state variable to remove</param>
        void RemoveState(string rName);

        /// <summary>
        /// Retrieves the value of the specified state variable.
        /// </summary>
        /// <param name="rName">Name of the state variable whose value we are interested in</param>
        /// <returns>Integer that is the value of the specified state variable</returns>
        int GetStateValue(string rName);

        /// <summary>
        /// Sets the value of the specified state variable. Adds it if it does't exist.
        /// </summary>
        /// <param name="rName">Name of the state variable whose value we are interested in</param>
        /// <param name="rValue">Integer that is the value to set</param>
        void SetStateValue(string rName, int rValue);
    }
}
