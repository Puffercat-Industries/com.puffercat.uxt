﻿using System;
using System.Runtime.InteropServices;
using UnityEngine;

namespace Puffercat.Uxt.Utils
{
    [StructLayout(LayoutKind.Explicit), Serializable]
    public struct SerializedGuid : IComparable, IComparable<Guid>, IEquatable<Guid>
    {
        public static SerializedGuid NewGuid()
        {
            return new SerializedGuid()
            {
                Guid = System.Guid.NewGuid()
            };
        }

        public SerializedGuid(string guidString) : this()
        {
            Guid = new System.Guid(guidString);
        }

        public SerializedGuid(Guid guid) : this()
        {
            Guid = guid;
        }

        [FieldOffset(0)] public Guid Guid;
        [FieldOffset(0), SerializeField] public Int32 GuidPart1;
        [FieldOffset(4), SerializeField] public Int32 GuidPart2;
        [FieldOffset(8), SerializeField] public Int32 GuidPart3;
        [FieldOffset(12), SerializeField] public Int32 GuidPart4;

        public static SerializedGuid FromInts(Int32 part1, Int32 part2, Int32 part3, Int32 part4)
        {
            return new SerializedGuid
            {
                GuidPart1 = part1,
                GuidPart2 = part2,
                GuidPart3 = part3,
                GuidPart4 = part4
            };
        }

        public void ToInts(out Int32 part1, out Int32 part2, out Int32 part3, out Int32 part4)
        {
            part1 = GuidPart1;
            part2 = GuidPart2;
            part3 = GuidPart3;
            part4 = GuidPart4;
        }
        
        public static implicit operator Guid(SerializedGuid uGuid)
        {
            return uGuid.Guid;
        }

        public Int32 CompareTo(object obj)
        {
            if (obj == null)
                return -1;

            if (obj is SerializedGuid)
                return ((SerializedGuid)obj).Guid.CompareTo(Guid);

            if (obj is Guid)
                return ((Guid)obj).CompareTo(Guid);

            return -1;
        }

        public Int32 CompareTo(Guid other)
        {
            return Guid.CompareTo(other);
        }

        public Boolean Equals(Guid other)
        {
            return Guid == other;
        }

        public override Boolean Equals(object obj)
        {
            if (obj == null)
                return false;

            if (obj is SerializedGuid)
                return (SerializedGuid)obj == Guid;

            if (obj is Guid)
                return (Guid)obj == Guid;

            return false;
        }

        public override Int32 GetHashCode()
        {
            return Guid.GetHashCode();
        }

        public override string ToString()
        {
            return Guid.ToString();
        }
    }
}