using com.ootii.Graphics.UI;

namespace com.ootii.Game
{
    /// <summary>
    /// This object is for backwards compatability only. Please use the
    /// Reticle object instead.
    /// </summary>
    public class TargetingReticle : Reticle
    {
        /// <summary>
        /// Provides global access to the reticle
        /// </summary>
        public new static IReticle Instance
        {
            get { return Reticle.Instance; }
            set { Reticle.Instance = value; }
        }
    }
}