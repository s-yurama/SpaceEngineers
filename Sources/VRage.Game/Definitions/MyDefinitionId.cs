﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using ProtoBuf;
using VRage.Utils;
using VRage.ObjectBuilders;
using VRage.Serialization;

namespace VRage.Game
{
    /// <summary>
    /// Prefer getting definition ID using object builder used to create the item.
    /// If you have automatic rifle, in its Init method create new MyDefinitionId
    /// using TypeId and SubtypeName of object builder.
    /// Do not write specific values in code, as data comes from XML and if those
    /// change, code needs to change as well.
    /// </summary>
    public struct MyDefinitionId : IEquatable<MyDefinitionId>
    {
        /// <summary>
        /// Creates a new definition ID from a given content.
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public static MyDefinitionId FromContent(MyObjectBuilder_Base content)
        {
            return new MyDefinitionId(content.TypeId, content.SubtypeId);
        }

        /// <summary>
        /// Attempts to create a definition ID from a definition string, which has the form (using ores as an example) "MyObjectBuilder_Ore/Iron".
        /// The first part must represent an existing type. If it does not, an exception will be thrown. The second (the subtype) is not enforced.
        /// See <see cref="TryParse"/> for a parsing method that does not throw an exception.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static MyDefinitionId Parse(string id)
        {
            MyDefinitionId output;
            if (!TryParse(id, out output))
                throw new ArgumentException("The provided type does not conform to a definition ID.", "id");
            return output;
        }

        /// <summary>
        /// Attempts to create a definition ID from a definition string, which has the form (using ores as an example) "MyObjectBuilder_Ore/Iron".
        /// The first part must represent an existing type, while the second (the subtype) is not enforced.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="definitionId"></param>
        /// <returns></returns>
        public static bool TryParse(string id, out MyDefinitionId definitionId)
        {
            if (string.IsNullOrEmpty(id))
            {
                definitionId = new MyDefinitionId();
                return false;
            }

            var slashIndex = id.IndexOf('/');
            if (slashIndex == -1)
            {
                definitionId = new MyDefinitionId();
                return false;
            }
            var typeId = id.Substring(0, slashIndex).Trim();
            MyObjectBuilderType result;
            if (MyObjectBuilderType.TryParse(typeId, out result))
            {
                var subtypeId = id.Substring(slashIndex + 1).Trim();
                if (subtypeId == "(null)")
                    subtypeId = null;
                definitionId = new MyDefinitionId(result, subtypeId);
                return true;
            }
            definitionId = new MyDefinitionId();
            return false;
        }


        #region Comparer
        public class DefinitionIdComparerType : IEqualityComparer<MyDefinitionId>
        {
            public bool Equals(MyDefinitionId x, MyDefinitionId y)
            {
                return x.TypeId == y.TypeId && x.SubtypeId == y.SubtypeId;
            }

            public int GetHashCode(MyDefinitionId obj)
            {
                return obj.GetHashCode();
            }
        }

        public static readonly DefinitionIdComparerType Comparer = new DefinitionIdComparerType();
        #endregion

        public readonly MyObjectBuilderType TypeId;
        public readonly MyStringHash SubtypeId;

        public string SubtypeName
        {
            get { return SubtypeId.ToString(); }
        }

        public MyDefinitionId(MyObjectBuilderType type) :
            this(type, MyStringHash.GetOrCompute(null))
        {
        }

        public MyDefinitionId(MyObjectBuilderType type, string subtypeName) :
            this(type, MyStringHash.GetOrCompute(subtypeName))
        {
        }

        public MyDefinitionId(MyObjectBuilderType type, MyStringHash subtypeId)
        {
            TypeId = type;
            SubtypeId = subtypeId;
        }

        public MyDefinitionId(MyRuntimeObjectBuilderId type, MyStringHash subtypeId)
        {
            TypeId = (MyObjectBuilderType)type;
            SubtypeId = subtypeId;
        }

        public override int GetHashCode()
        {
            return (int)((((uint)TypeId.GetHashCode()) << 16) ^ ((uint)SubtypeId.GetHashCode()));
        }

        /// <summary>
        /// Safer hash code. It is unique in more situations than GetHashCode would be,
        /// but it may still require full check.
        /// </summary>
        /// <returns>64-bit hash code.</returns>
        public long GetHashCodeLong()
        {
            return (long)((((ulong)TypeId.GetHashCode()) << 32) ^ ((ulong)SubtypeId.GetHashCode()));
        }

        public override bool Equals(object obj)
        {
            return (obj is MyDefinitionId) && Equals((MyDefinitionId)obj);
        }

        public override string ToString()
        {
            string typeId = !TypeId.IsNull ? TypeId.ToString() : "(null)";
            string subtypeName = !string.IsNullOrEmpty(SubtypeName) ? SubtypeName : "(null)";
            return string.Format("{0}/{1}", typeId, subtypeName);
        }

        public bool Equals(MyDefinitionId other)
        {
            return this.TypeId    == other.TypeId &&
                   this.SubtypeId == other.SubtypeId;
        }

        public static bool operator == (MyDefinitionId l, MyDefinitionId r)
        {
            return l.Equals(r);
        }

        public static bool operator != (MyDefinitionId l, MyDefinitionId r)
        {
            return !l.Equals(r);
        }

        public static implicit operator MyDefinitionId(SerializableDefinitionId v)
        {
            //Debug.Assert(v.TypeId != MyObjectBuilderType.Invalid, "Deserialized invalid definition ID. This should not happen.");
            return new MyDefinitionId(v.TypeId, v.SubtypeName);
        }

        public static implicit operator SerializableDefinitionId(MyDefinitionId v)
        {
            Debug.Assert(v.TypeId != MyObjectBuilderType.Invalid, "Serializing invalid definition ID. This should not happen.");
            return new SerializableDefinitionId(v.TypeId, v.SubtypeName);
        }
    }

    [ProtoContract]
    public struct DefinitionIdBlit
    {
        // 6B (ushort + int)
        [ProtoMember]
        [NoSerialize]
        public MyRuntimeObjectBuilderId TypeId;

        [Serialize]
        private ushort m_typeIdSerialized
        {
            get { return TypeId.Value; }
            set { TypeId = new MyRuntimeObjectBuilderId(value); }
        }

        [ProtoMember]
        public MyStringHash SubtypeId;

        public DefinitionIdBlit(MyObjectBuilderType type, MyStringHash subtypeId)
        {
            TypeId = (MyRuntimeObjectBuilderId)type;
            SubtypeId = subtypeId;
        }

        public DefinitionIdBlit(MyRuntimeObjectBuilderId typeId, MyStringHash subtypeId)
        {
            TypeId = typeId;
            SubtypeId = subtypeId;
        }

        public bool IsValid
        {
            get { return TypeId.Value != 0; }
        }

        public static implicit operator MyDefinitionId(DefinitionIdBlit id)
        {
            var type = (MyObjectBuilderType)id.TypeId;
            Debug.Assert(MyStringHash.IsKnown(id.SubtypeId));
            return new MyDefinitionId(type, id.SubtypeId);
        }

        public static implicit operator DefinitionIdBlit(MyDefinitionId id)
        {
            return new DefinitionIdBlit(id.TypeId, id.SubtypeId);
        }

        public override string ToString()
        {
            return ((MyDefinitionId)this).ToString();
        }
    }

    public static class MyObjectBuilderExtensions
    {
        public static MyDefinitionId GetId(this MyObjectBuilder_Base self)
        {
            return new MyDefinitionId(self.TypeId, self.SubtypeId);
        }
    }
}