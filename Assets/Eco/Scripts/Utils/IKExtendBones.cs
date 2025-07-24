using DitzelGames.FastIK;
using UnityEngine;

namespace Eco.Scripts.Utils
{
    public class IKExtendBones : FastIKFabric
    {
        protected override void LateUpdate()
        {
            ExtendBones();
            base.LateUpdate();
        }

        private void ExtendBones()
        {
            var firstBone = Bones[0];
            var neededLength = Vector3.Distance(firstBone.position, Target.position) / Bones.Length * 1.2f;

            for (int i = 0; i < BonesLength.Length; i++)
            {
                BonesLength[i] = neededLength;
            }
        }
    }
}
