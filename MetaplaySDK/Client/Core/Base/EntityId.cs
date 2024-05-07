// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

using Metaplay.Core.Model;
using System;
using static System.FormattableString;

namespace Metaplay.Core
{
    /// <summary>
    /// Uniquely identifies an Entity in the system (eg, Player, Session, or PushNotifier).
    /// Used for routing messages within the server cluster.
    ///
    /// Stores an EntityKind and a raw unique Value.
    /// </summary>
    [MetaSerializable]
    public struct EntityId : IComparable<EntityId>, IEquatable<EntityId>, IComparable
    {
        // Top 6 bits used for EntityKind, bottom 58 for unique value
        public const int    KindShift   = 58;
        public const ulong  ValueMask   = (1L << KindShift) - 1;

        [MetaMember(1)] public ulong Raw { get; private set; }

        private EntityId(ulong raw) { Raw = raw; }

        public static EntityId None => Create(EntityKind.None, 0);

        public static EntityId Create(EntityKind kind, ulong value)
        {
            if (kind == EntityKind.None && value != 0)
                throw new ArgumentException("Value must be zero for EntityKind.None", nameof(value));
            if (kind.Value < 0 || kind.Value >= EntityKind.MaxValue)
                throw new ArgumentException($"Invalid EntityKind value {kind.Value}", nameof(kind));
            if (value < 0 || value > ValueMask)
                throw new ArgumentException($"Invalid EntityId value {kind}:{value}", nameof(value));

            return new EntityId(((ulong)kind.Value << KindShift) + (value & ValueMask));
        }

        /// <summary>
        /// Unsafe create EntityId from raw value. Doesn't do any checks on the input value, so don't
        /// use unless you know what you're doing!
        /// </summary>
        /// <param name="raw"></param>
        /// <returns></returns>
        public static EntityId FromRaw(ulong raw) => new EntityId(raw);

        public static EntityId CreateRandom(EntityKind kind)
        {
            // \note not guaranteed to be unique, must check uniqueness after creating
            // \note guid provides much better randomness than simple Random.Next()
            ulong value = ((ulong)Guid.NewGuid().GetHashCode() << 32) + (ulong)Guid.NewGuid().GetHashCode();
            return new EntityId(((ulong)kind.Value << 58) + (value & ValueMask));
        }

        public static EntityId ParseFromString(string str)
        {
            if (str is null)
                throw new ArgumentNullException(nameof(str), "Cannot parse null string into EntityId");

            if (!TryParseFromString(str, out EntityId entityId, out string errorStr))
                throw new FormatException(errorStr);

            return entityId;
        }

        /// <summary>
        /// Tries to parse an <see cref="EntityId"/> out of an entity id string. Returns false if unsuccessful. Guaranteed to not throw.
        /// </summary>
        /// <param name="str">The entity id string to parse.</param>
        /// <param name="entityId"> The resulting entity ID. EntityId.None if parsing was unsuccessful.</param>
        /// <param name="errorStr"> If unsuccessful, the error string describes why parsing failed.</param>
        /// <returns>True if successfully parsed, false otherwise.</returns>
        public static bool TryParseFromString(string str, out EntityId entityId, out string errorStr)
        {
            entityId = None;
            errorStr = null;

            if (string.IsNullOrEmpty(str))
            {
                errorStr = "Cannot parse null or empty string into EntityId";
                return false;
            }

            if (str == "None")
            {
                return true;
            }

            string[] parts = str.Split(':');
            if (parts.Length != 2)
            {
                errorStr = Invariant($"Invalid EntityId format '{str}'");
                return false;
            }

            // None must not have value
            if (parts[0] == "None")
            {
                errorStr = Invariant($"EntityId None must not have value '{str}'");
                return false;
            }

            // Parse kind
            if (!EntityKindRegistry.TryFromName(parts[0], out EntityKind kind))
            {
                errorStr = Invariant($"Invalid EntityKind in {str}");
                return false;
            }

            // Parse unique id
            if (!TryParseValue(parts[1], out ulong id, out string valueErrorStr))
            {
                errorStr = valueErrorStr;
                return false;
            }

            if (id > ValueMask)
            {
                errorStr = Invariant($"Invalid value in {str}");
                return false;
            }
            
            entityId = Create(kind, id);
            return true;
        }

        /// <summary>
        /// Parses EntityId from string, and validates the Kind is <paramref name="expectedKind"/>. On failure
        /// throws FormatException. Note that there is no special case for <see cref="EntityKind.None"/> kind.
        /// It is not accepted unless <paramref name="expectedKind"/> is <see cref="EntityKind.None"/>.
        /// </summary>
        public static EntityId ParseFromStringWithKind(EntityKind expectedKind, string str)
        {
            EntityId entityId = ParseFromString(str);
            if (entityId.Kind != expectedKind)
                throw new FormatException($"Illegal EntityKind in {str}");
            return entityId;
        }

        public static bool operator ==(EntityId a, EntityId b) => a.Raw == b.Raw;
        public static bool operator !=(EntityId a, EntityId b) => a.Raw != b.Raw;

        public readonly bool Equals(EntityId other) => this == other;

        public override readonly bool Equals(object obj) => (obj is EntityId) ? (this == (EntityId)obj) : false;

        public readonly int CompareTo(EntityId other) => Raw.CompareTo(other.Raw);

        public readonly EntityKind Kind  => EntityKind.FromValue((int)(Raw >> KindShift));
        public readonly ulong      Value => (Raw & ValueMask);

        /// <summary>
        /// Check if the EntityId is a valid one:
        /// - The EntityKind is a valid, existing value
        /// - The EntityId is not a None (including illegal Nones with Value != 0)
        /// </summary>
        public readonly bool IsValid
        {
            get
            {
                // \note Also return false for invalid EntityIds where Kind == None and Value != 0
                EntityKind kind = Kind;
                return EntityKindRegistry.IsValid(kind) && (kind != EntityKind.None);
            }
        }

        public readonly bool IsOfKind(EntityKind kind) => Kind == kind;

        // \note manually calculated for 58 bits of id
        public const string ValidIdCharacters       = "023456789ABCDEFGHJKLMNOPQRSTUVWXYZabcdefghijkmnopqrstuvwxyz"; // 1, I, and l omitted to avoid confusion
        public const int    NumValidIdCharacters    = 59; // must be ValidIdCharacters.Length
        public const int    IdLength                = 10;

        public static string ValueToString(ulong val)
        {
            // \todo [petri] only do these once somewhere
            MetaDebug.Assert(ValidIdCharacters.Length == NumValidIdCharacters, "NumValidCharacters != ValidIdCharacters.Length");
            MetaDebug.Assert(System.Math.Pow(NumValidIdCharacters, IdLength) >= (1ul << KindShift), "NumIdCharacters is too small");
            MetaDebug.Assert(System.Math.Pow(NumValidIdCharacters, IdLength - 1) < (1ul << KindShift), "NumIdCharacters is too large");

#if NETCOREAPP
            Span<char> chars = stackalloc char[IdLength];
#else
            char[] chars = new char[IdLength];
#endif
            for (int ndx = IdLength - 1; ndx >= 0; ndx--)
            {
                chars[ndx] = ValidIdCharacters[(int)(val % NumValidIdCharacters)];
                val /= NumValidIdCharacters;
            }
            MetaDebug.Assert(val == 0, "Remainder left when converting id to string");
            return new string(chars);
        }

        static bool TryParseValue(string str, out ulong value, out string errorStr)
        {
            value = 0;
            errorStr = null;

            if (str.Length != IdLength)
            {
                errorStr = Invariant($"EntityId values are required to be exactly {IdLength} characters, got {str.Length} in '{str}'");
                return false;
            }

            ulong id = 0;
            for (int ndx = 0; ndx < IdLength; ndx++)
            {
                int v = ValidIdCharacters.IndexOf(str[ndx]);
                if (v == -1)
                {
                    errorStr = Invariant($"Invalid EntityId character '{str[ndx]}'");
                    return false;
                }
                id = id * NumValidIdCharacters + (uint)v;
            }

            if (id < 0 || id > ValueMask)
            {
                errorStr = Invariant($"Invalid EntityId value '{str}'");
                return false;
            }

            value = id;
            return true;
        }

        public readonly (string, string) GetKindValueStrings() => (Kind.ToString(), ValueToString(Value));

        public override readonly int GetHashCode() => Raw.GetHashCode();

        public override readonly string ToString()
        {
            EntityKind kind = Kind;
            if (kind == EntityKind.None)
                return (Value == 0) ? "None" : $"InvalidNone:{ValueToString(Value)}"; // Value != 0 is an invalid value
            else
                return $"{kind}:{ValueToString(Value)}";
        }

        int IComparable.CompareTo(object obj) => (obj is EntityId other) ? CompareTo(other) : 1;
    }
}
