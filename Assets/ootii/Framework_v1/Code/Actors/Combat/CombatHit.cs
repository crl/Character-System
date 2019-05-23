using UnityEngine;

namespace com.ootii.Actors.Combat
{
    /// <summary>
    /// Collision information that can be passed back about combatant
    /// </summary>
    public struct CombatHit
    {
        /// <summary>
        /// Gives us something to compare to
        /// </summary>
        public static CombatHit EMPTY = new CombatHit();

        /// <summary>
        /// Collider that is the object hit
        /// </summary>
        public Collider Collider;

        /// <summary>
        /// Point on the collider's surface that was hit (this could be an estimate)
        /// </summary>
        public Vector3 Point;

        /// <summary>
        /// Normal on the collider's surface that was hit
        /// </summary>
        public Vector3 Normal;

        /// <summary>
        /// Distance from the attack's origin to the closest pont
        /// </summary>
        public float Distance;

        /// <summary>
        /// Direction the attack was moving
        /// </summary>
        public Vector3 Vector;

        /// <summary>
        /// Hit "count" this represents in a single cycle.
        /// </summary>
        public float Index;

        /// <summary>
        /// Determines if two object are the same
        /// </summary>
        /// <param name="rOther">CombatHit to compare</param>
        /// <returns>Boolean if the combat hit values are equivallent</returns>
        public override bool Equals(object rOther)
        {
            return base.Equals(rOther);
        }

        /// <summary>
        /// Hash code for the instance
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        /// Comparator
        /// </summary>
        public static bool operator ==(CombatHit c1, CombatHit c2)
        {
            return c1.Equals(c2);
        }

        /// <summary>
        /// Comparator
        /// </summary>
        public static bool operator !=(CombatHit c1, CombatHit c2)
        {
            return !c1.Equals(c2);
        }
    }
}
