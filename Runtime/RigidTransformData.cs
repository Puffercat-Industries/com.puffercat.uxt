using UnityEngine;

namespace Puffercat.Uxt
{
    public struct RigidTransformData
    {
        public Vector3 Translation;
        public Quaternion Rotation;

        public RigidTransformData(Vector3 translation, Quaternion rotation)
        {
            Translation = translation;
            Rotation = rotation;
        }

        public void ApplyTo(Transform transform)
        {
            transform.position = Translation;
            transform.rotation = Rotation;
        }

        public Matrix4x4 ToMatrix()
        {
            return Matrix4x4.TRS(Translation, Rotation, Vector3.one);
        }
    }
}