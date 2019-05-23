using System;

namespace com.ootii.Actors.Attributes
{
    /// <summary>
    /// Interface used to abstract attributes themselves.
    /// </summary>
    public interface IAttribute
    {
        /// <summary>
        /// Unique identifier. This should not be changed once added to the Basic Attributes
        /// </summary>
        string ID { get; }

        /// <summary>
        ///  Defines the type that this attribute item represents
        /// </summary>
        Type ValueType { get; }

        /// <summary>
        /// Determines if the attribute is valid. If not, it may have been
        /// destroyed and we shouldn't hold a reference to it.
        /// </summary>
        bool IsValid { get; set; }

        /// <summary>
        /// Retrieves the value of the attribute item. Since this is the base
        /// class, the value type must be specified and must match the actual type
        /// </summary>
        /// <typeparam name="T1">Type of the memory item</typeparam>
        /// <returns>Value of the attribute item</returns>
        T1 GetValue<T1>();

        /// <summary>
        /// Sets the value of the attribute item. Since this is the base
        /// class, the value type must be specified and must match the actual type
        /// </summary>
        /// <typeparam name="T1">Type of the attribute item</typeparam>
        /// <param name="rValue">Value to set</param>
        void SetValue<T1>(T1 rValue);
    }
}
