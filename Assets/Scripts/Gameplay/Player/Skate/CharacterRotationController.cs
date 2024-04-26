using ProjectJetSetRadio.Gameplay.CustomDebug;
using UnityEngine;

namespace ProjectJetSetRadio.Gameplay.Visual
{
    public class CharacterRotationController : MonoBehaviour
    {
        private Collider[] allocatedDetectionArray = new Collider[10];

        public void AlignToDirection(Vector3 newForwardDirection, Vector3 upwardDir)
        {
            transform.localRotation = Quaternion.LookRotation(newForwardDirection, upwardDir);
        }

        public void AlignToDirection(Vector3 newForwardDirection)
        {
            transform.localRotation = Quaternion.LookRotation(newForwardDirection, Vector3.up);
        }
    }
}
