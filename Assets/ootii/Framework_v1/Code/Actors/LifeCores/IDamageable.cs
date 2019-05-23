using com.ootii.Actors.Combat;

namespace com.ootii.Actors.LifeCores
{
    /// <summary>
    /// Interface that identifies an actor can be damaged and killed. The 
    /// implementation is up to the developer
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// Tells the implementor that it has been damaged using the message.
        /// </summary>
        /// <param name="rMessage">Message that defines the damage amount and type.</param>
        /// <returns>Determines if the damage was applied.</returns>
        bool OnDamaged(DamageMessage rMessage);
    }
}
