using UnityEngine;
using com.ootii.Messages;

namespace com.ootii.Demos
{
    /// <summary>
    /// Simple class for having objects taken
    /// </summary>
    public class demo_TakeCore : MonoBehaviour
    {
        /// <summary>
        /// Event handler for the interactable
        /// </summary>
        /// <param name="rMessage"></param>
        public void OnInteractableActivated(IMessage rMessage)
        {
            if (rMessage.Data is GameObject)
            {
                GameObject.Destroy((GameObject)rMessage.Data);
            }
        }
    }
}
