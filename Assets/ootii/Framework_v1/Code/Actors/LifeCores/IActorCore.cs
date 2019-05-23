using UnityEngine;
using com.ootii.Actors.Attributes;
using com.ootii.Messages;

namespace com.ootii.Actors.LifeCores
{
    /// <summary>
    /// Foundation for PC/NPCs that have a heart-beat and whose life needs to be managed. An
    /// actor is typically something that is alive, can be hurt, and can die.
    /// </summary>
    public interface IActorCore : ILifeCore, IActorStateSource, IDamageable
    {
        /// <summary>
        /// Transform that is the actor
        /// </summary>
        Transform Transform { get; }

        /// <summary>
        /// Attribute source that the actor uses
        /// </summary>
        IAttributeSource AttributeSource { get; }

        /// <summary>
        /// Determines if the actior is alive
        /// </summary>
        bool IsAlive { get; set; }

        /// <summary>
        /// Called when the actor is about to be affected by something like a spell, poison, etc.
        /// The sub-class would override this function and interrogate the message as needed.
        /// </summary>
        /// <param name="rMessage">Message describing what's happening</param>
        /// <returns>Returns true if the affect should continue or false if not</returns>
        bool TestAffected(IMessage rMessage);

        /// <summary>
        /// Allows a message to be passed in and will send the message to the reactors
        /// </summary>
        /// <param name="rMessage">Message to be processed</param>
        void SendMessage(IMessage rMessage);
    }
}
