using UnityEngine;

namespace Puffercat.Uxt.Utils
{
    public static class IkUtility
    {
        public static RigidTransformData MatchStickWithHandlePoints(Transform handleBottom, Transform handleTop)
        {
            var pos1 = handleBottom.position;
            var pos2 = handleTop.position;
            var up = (pos2 - pos1).normalized;
            var averagePosition = Vector3.Lerp(pos1, pos2, 0.5f);
            var averageRotation = Quaternion.Slerp(handleBottom.rotation, handleTop.rotation, 0.5f);
            return new RigidTransformData(averagePosition, averageRotation);
        }

        public static RigidTransformData MoveParentToMatchChild(Transform parent, Transform child,
            RigidTransformData target)
        {
            var matrix = target.ToMatrix() * child.worldToLocalMatrix * parent.localToWorldMatrix;
            return new RigidTransformData(matrix.GetPosition(), matrix.rotation);
        }
    }
}