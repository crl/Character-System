using System;

namespace com.ootii.Actors.Attributes
{
    /// <summary>
    /// Interface used to abstract how attributes and attribute values are retrieved. Implement from this interface
    /// as needed in order to allow other assets to provide access to your character's attributes.
    /// </summary>
    public interface IAttributeSource
    {
        /// <summary>
        /// Determines if the attribute exists.
        /// </summary>
        /// <param name="rAttributeID">String representing the name or ID of the attribute we're checking</param>
        /// <returns></returns>
        bool AttributeExists(string rAttributeID);

        /// <summary>
        /// Determines if all the attributes in the comma delimited value exist.
        /// </summary>
        /// <param name="rAttributes">Comma delimited list of tags to test for</param>
        /// <param name="rRequireAll">Determines if all must exist or just one</param>
        /// <returns>True or false</returns>
        bool AttributesExist(string rAttributeIDs, bool rRequireAll = true);

        /// <summary>
        /// Gets the type of the attribute
        /// </summary>
        /// <param name="rAttributeID"></param>
        /// <returns></returns>
        Type GetAttributeType(string rAttributeID);

        /// <summary>
        /// Given the specified attribute, grab the float value
        /// </summary>
        /// <param name="rAttributeID">string representing the attribute type we want</param>
        /// <param name="rDefault">Default value if the attribute isn't found</param>
        /// <returns>Value of the attribute or default if not found.</returns>
        T GetAttributeValue<T>(string rAttributeID, T rDefault = default(T));

        /// <summary>
        /// Given the specified attribute, set the value associated with the attribute
        /// </summary>
        /// <param name="rAttributeID">String representing the name or ID of the item we want</param>
        /// <param name="rValue">value to set on the attribute</param>
        void SetAttributeValue<T>(string rAttributeID, T rValue);
    }
}
