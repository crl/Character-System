using UnityEngine;

namespace com.ootii.Networking
{
    /// <summary>
    /// Holds all the state aniamtor parameters that will be updated sent.
    /// The array we store the data in is typically store newest at the front
    /// </summary>
    public struct ActorNetworkState
    {
        // Time of the state
        public float Time;

        // Transform properties
        public Vector3 Position;
        public Quaternion Rotation;

        // Parameter values
        public bool IsGrounded;
        public int Stance;
        public float InputX;
        public float InputY;
        public float InputMagnitude;
        public float InputMagnitudeAvg;
        public float InputAngleFromAvatar;
        public float InputAngleFromCamera;
        public int L0MotionPhase;
        public int L0MotionForm;
        public int L0MotionParameter;
        public float L0MotionStateTime;
        public int L1MotionPhase;
        public int L1MotionForm;
        public int L1MotionParameter;
        public float L1MotionStateTime;
    }
}
