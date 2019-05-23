namespace com.ootii.Actors.LifeCores
{
    /// <summary>
    /// Delegate for raising events
    /// </summary>
    /// <param name="rCore">ActorCore that raised the event</param>
    public delegate void LifeCoreDelegate(ILifeCore rCore, object rUserData = null);
}