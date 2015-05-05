// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.CompilerServices;

namespace System.Reflection.Metadata
{
    /// <summary>
    /// Represents any metadata entity (type reference/definition/specification, method definition, custom attribute, etc.) or value (string, blob, guid, user string).
    /// </summary>
    /// <remarks>
    /// Use <see cref="Handle"/> to store multiple kinds of handles.
    /// </remarks>
    public struct Handle : IEquatable<Handle>
    {
        private readonly int _value;

        // bits:
        //    7: IsVirtual
        // 0..6: token type
        private readonly byte _vType;

        /// <summary>
        /// Creates <see cref="Handle"/> from a token or a token combined with a virtual flag.
        /// </summary>
        internal static Handle FromVToken(uint vToken)
        {
            return new Handle((byte)(vToken >> TokenTypeIds.RowIdBitCount), (int)(vToken & TokenTypeIds.RIDMask));
        }

        internal Handle(byte vType, int value)
        {
            _vType = vType;
            _value = value;

            Debug.Assert(value >= 0);

            // No table can have more than 2^24 rows.
            // User String heap is also limited by 2^24 since user strings have tokens in IL.
            // We limit the size of #Blob, #String and #GUID heaps to 2^29 (max compressed integer) in order 
            // to keep the sizes of corresponding handles to 32 bit. As a result we don't support reading metadata 
            // files with heaps larger than 0.5GB.
            Debug.Assert(IsHeapHandle && value <= HeapHandleType.OffsetMask || 
                         !IsHeapHandle && value <= TokenTypeIds.RIDMask);
        }

        // for entity handles:
        internal int RowId
        {
            get
            {
                Debug.Assert(!IsHeapHandle);
                return _value;
            }
        }

        // for heap handles:
        internal int Offset
        {
            get
            {
                Debug.Assert(IsHeapHandle);
                return _value;
            }
        }

        /// <summary>
        /// Token type (0x##000000), does not include virtual flag.
        /// </summary>
        internal uint EntityHandleType
        {
            get { return Type << TokenTypeIds.RowIdBitCount; }
        }

        /// <summary>
        /// Small token type (0x##), does not include virtual flag.
        /// </summary>
        internal uint Type
        {
            get { return _vType & HandleType.TypeMask; }
        }

        /// <summary>
        /// Value stored in an <see cref="EntityHandle"/>.
        /// </summary>
        internal uint EntityHandleValue
        {
            get
            {
                Debug.Assert((_value & TokenTypeIds.RIDMask) == _value);
                return (uint)_vType << TokenTypeIds.RowIdBitCount | (uint)_value;
            }
        }

        /// <summary>
        /// Value stored in a concrete entity handle (see <see cref="TypeDefinitionHandle"/>, <see cref="MethodDefinitionHandle"/>, etc.).
        /// </summary>
        internal uint SpecificEntityHandleValue
        {
            get
            {
                Debug.Assert((_value & TokenTypeIds.RIDMask) == _value);
                return (_vType & HandleType.VirtualBit) << TokenTypeIds.RowIdBitCount | (uint)_value;
            }
        }

        internal byte VType
        {
            get { return _vType; }
        }

        internal bool IsVirtual
        {
            get { return (_vType & HandleType.VirtualBit) != 0; }
        }

        internal bool IsHeapHandle
        {
            get { return (_vType & HandleType.HeapMask) == HandleType.HeapMask; }
        }

        public HandleKind Kind
        {
            get { return HandleType.ToHandleKind(_vType & HandleType.TypeMask); }
        }

        public bool IsNil
        {
            // virtual handles are never nil
            get { return ((uint)_value | (_vType & HandleType.VirtualBit)) == 0; }
        }

        internal bool IsEntityOrUserStringHandle
        {
            get { return Type <= HandleType.UserString; }
        }

        internal int Token
        {
            get
            {
                Debug.Assert(IsEntityOrUserStringHandle);
                Debug.Assert(!IsVirtual);
                Debug.Assert((_value & TokenTypeIds.RIDMask) == _value);

                return _vType << TokenTypeIds.RowIdBitCount | _value;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is Handle && Equals((Handle)obj);
        }

        public bool Equals(Handle other)
        {
            return _value == other._value && _vType == other._vType;
        }

        public override int GetHashCode()
        {
            return _value ^ (_vType << 24);
        }

        public static bool operator ==(Handle left, Handle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Handle left, Handle right)
        {
            return !left.Equals(right);
        }

        internal static int Compare(Handle left, Handle right)
        {
            // All virtual tokens will be sorted after non-virtual tokens.
            // The order of handles that differ in kind is undefined, 
            // but we include it so that we ensure consistency with == and != operators.
            return ((long)(uint)left._value | (long)left._vType << 32).CompareTo((long)(uint)right._value | (long)right._vType << 32);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidCast()
        {
            throw new InvalidCastException();
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowInvalidCodedIndex()
        {
            throw new BadImageFormatException(MetadataResources.InvalidCodedIndex);
        }

        public static readonly ModuleDefinitionHandle ModuleDefinition = new ModuleDefinitionHandle(1);
        public static readonly AssemblyDefinitionHandle AssemblyDefinition = new AssemblyDefinitionHandle(1);
    }

    /// <summary>
    /// Represents a metadata entity (type reference/definition/specification, method definition, custom attribute, etc.). 
    /// </summary>
    /// <remarks>
    /// Use <see cref="EntityHandle"/> to store multiple kinds of entity handles.
    /// It has smaller memory footprint than <see cref="Handle"/>.
    /// </remarks>
    public struct EntityHandle : IEquatable<EntityHandle>
    {
        // bits:
        //     31: IsVirtual
        // 24..30: type
        //  0..23: row id
        private readonly uint _vToken;

        internal EntityHandle(uint vToken)
        {
            _vToken = vToken;
        }

        public static implicit operator Handle(EntityHandle handle)
        {
            return Handle.FromVToken(handle._vToken);
        }

        public static explicit operator EntityHandle(Handle handle)
        {
            if (handle.IsHeapHandle)
            {
                Handle.ThrowInvalidCast();
            }

            return new EntityHandle(handle.EntityHandleValue);
        }

        internal uint Type
        {
            get { return _vToken & TokenTypeIds.TypeMask; }
        }

        internal uint VType
        {
            get { return _vToken & (TokenTypeIds.VirtualBit | TokenTypeIds.TypeMask); }
        }

        internal bool IsVirtual
        {
            get { return (_vToken & TokenTypeIds.VirtualBit) != 0; }
        }

        public bool IsNil
        {
            // virtual handle is never nil
            get { return (_vToken & (TokenTypeIds.VirtualBit | TokenTypeIds.RIDMask)) == 0; }
        }

        internal int RowId
        {
            get { return (int)(_vToken & TokenTypeIds.RIDMask); }
        }

        /// <summary>
        /// Value stored in a specific entity handle (see <see cref="TypeDefinitionHandle"/>, <see cref="MethodDefinitionHandle"/>, etc.).
        /// </summary>
        internal uint SpecificHandleValue
        {
            get { return _vToken & (TokenTypeIds.VirtualBit | TokenTypeIds.RIDMask); }
        }

        public HandleKind Kind
        {
            get { return HandleType.ToHandleKind(Type >> TokenTypeIds.RowIdBitCount); }
        }

        internal int Token
        {
            get
            {
                Debug.Assert(!IsVirtual);
                return (int)_vToken;
            }
        }

        public override bool Equals(object obj)
        {
            return obj is EntityHandle && Equals((EntityHandle)obj);
        }

        public bool Equals(EntityHandle other)
        {
            return _vToken == other._vToken;
        }

        public override int GetHashCode()
        {
            return unchecked((int)_vToken);
        }

        public static bool operator ==(EntityHandle left, EntityHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(EntityHandle left, EntityHandle right)
        {
            return !left.Equals(right);
        }

        internal static int Compare(EntityHandle left, EntityHandle right)
        {
            // All virtual tokens will be sorted after non-virtual tokens.
            // The order of handles that differ in kind is undefined, 
            // but we include it so that we ensure consistency with == and != operators.
            return left._vToken.CompareTo(right._vToken);
        }

        public static readonly ModuleDefinitionHandle ModuleDefinition = new ModuleDefinitionHandle(1);
        public static readonly AssemblyDefinitionHandle AssemblyDefinition = new AssemblyDefinitionHandle(1);
    }

    public struct ModuleDefinitionHandle : IEquatable<ModuleDefinitionHandle>
    {
        private const uint tokenType = TokenTypeIds.Module;
        private const byte tokenTypeSmall = (byte)HandleType.Module;
        private readonly int _rowId;

        internal ModuleDefinitionHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static ModuleDefinitionHandle FromRowId(int rowId)
        {
            return new ModuleDefinitionHandle(rowId);
        }

        public static implicit operator Handle(ModuleDefinitionHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(ModuleDefinitionHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator ModuleDefinitionHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Handle.ThrowInvalidCast();
            }

            return new ModuleDefinitionHandle(handle.RowId);
        }

        public static explicit operator ModuleDefinitionHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new ModuleDefinitionHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(ModuleDefinitionHandle left, ModuleDefinitionHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is ModuleDefinitionHandle && ((ModuleDefinitionHandle)obj)._rowId == _rowId;
        }

        public bool Equals(ModuleDefinitionHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(ModuleDefinitionHandle left, ModuleDefinitionHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct AssemblyDefinitionHandle : IEquatable<AssemblyDefinitionHandle>
    {
        private const uint tokenType = TokenTypeIds.Assembly;
        private const byte tokenTypeSmall = (byte)HandleType.Assembly;
        private readonly int _rowId;

        internal AssemblyDefinitionHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static AssemblyDefinitionHandle FromRowId(int rowId)
        {
            return new AssemblyDefinitionHandle(rowId);
        }

        public static implicit operator Handle(AssemblyDefinitionHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(AssemblyDefinitionHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator AssemblyDefinitionHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Handle.ThrowInvalidCast();
            }

            return new AssemblyDefinitionHandle(handle.RowId);
        }

        public static explicit operator AssemblyDefinitionHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new AssemblyDefinitionHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(AssemblyDefinitionHandle left, AssemblyDefinitionHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is AssemblyDefinitionHandle && ((AssemblyDefinitionHandle)obj)._rowId == _rowId;
        }

        public bool Equals(AssemblyDefinitionHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(AssemblyDefinitionHandle left, AssemblyDefinitionHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct InterfaceImplementationHandle : IEquatable<InterfaceImplementationHandle>
    {
        private const uint tokenType = TokenTypeIds.InterfaceImpl;
        private const byte tokenTypeSmall = (byte)HandleType.InterfaceImpl;
        private readonly int _rowId;

        internal InterfaceImplementationHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static InterfaceImplementationHandle FromRowId(int rowId)
        {
            return new InterfaceImplementationHandle(rowId);
        }

        public static implicit operator Handle(InterfaceImplementationHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(InterfaceImplementationHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator InterfaceImplementationHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Handle.ThrowInvalidCast();
            }

            return new InterfaceImplementationHandle(handle.RowId);
        }

        public static explicit operator InterfaceImplementationHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new InterfaceImplementationHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(InterfaceImplementationHandle left, InterfaceImplementationHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is InterfaceImplementationHandle && ((InterfaceImplementationHandle)obj)._rowId == _rowId;
        }

        public bool Equals(InterfaceImplementationHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(InterfaceImplementationHandle left, InterfaceImplementationHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct MethodDefinitionHandle : IEquatable<MethodDefinitionHandle>
    {
        private const uint tokenType = TokenTypeIds.MethodDef;
        private const byte tokenTypeSmall = (byte)HandleType.MethodDef;
        private readonly int _rowId;

        private MethodDefinitionHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static MethodDefinitionHandle FromRowId(int rowId)
        {
            return new MethodDefinitionHandle(rowId);
        }

        public static implicit operator Handle(MethodDefinitionHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(MethodDefinitionHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator MethodDefinitionHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Handle.ThrowInvalidCast();
            }

            return new MethodDefinitionHandle(handle.RowId);
        }

        public static explicit operator MethodDefinitionHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new MethodDefinitionHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(MethodDefinitionHandle left, MethodDefinitionHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is MethodDefinitionHandle && ((MethodDefinitionHandle)obj)._rowId == _rowId;
        }

        public bool Equals(MethodDefinitionHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(MethodDefinitionHandle left, MethodDefinitionHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct MethodImplementationHandle : IEquatable<MethodImplementationHandle>
    {
        private const uint tokenType = TokenTypeIds.MethodImpl;
        private const byte tokenTypeSmall = (byte)HandleType.MethodImpl;
        private readonly int _rowId;

        private MethodImplementationHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static MethodImplementationHandle FromRowId(int rowId)
        {
            return new MethodImplementationHandle(rowId);
        }

        public static implicit operator Handle(MethodImplementationHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(MethodImplementationHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator MethodImplementationHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Handle.ThrowInvalidCast();
            }

            return new MethodImplementationHandle(handle.RowId);
        }

        public static explicit operator MethodImplementationHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new MethodImplementationHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(MethodImplementationHandle left, MethodImplementationHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is MethodImplementationHandle && ((MethodImplementationHandle)obj)._rowId == _rowId;
        }

        public bool Equals(MethodImplementationHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(MethodImplementationHandle left, MethodImplementationHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct MethodSpecificationHandle : IEquatable<MethodSpecificationHandle>
    {
        private const uint tokenType = TokenTypeIds.MethodSpec;
        private const byte tokenTypeSmall = (byte)HandleType.MethodSpec;
        private readonly int _rowId;

        private MethodSpecificationHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static MethodSpecificationHandle FromRowId(int rowId)
        {
            return new MethodSpecificationHandle(rowId);
        }

        public static implicit operator Handle(MethodSpecificationHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(MethodSpecificationHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator MethodSpecificationHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Handle.ThrowInvalidCast();
            }

            return new MethodSpecificationHandle(handle.RowId);
        }

        public static explicit operator MethodSpecificationHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new MethodSpecificationHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(MethodSpecificationHandle left, MethodSpecificationHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is MethodSpecificationHandle && ((MethodSpecificationHandle)obj)._rowId == _rowId;
        }

        public bool Equals(MethodSpecificationHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(MethodSpecificationHandle left, MethodSpecificationHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct TypeDefinitionHandle : IEquatable<TypeDefinitionHandle>
    {
        private const uint tokenType = TokenTypeIds.TypeDef;
        private const byte tokenTypeSmall = (byte)HandleType.TypeDef;
        private readonly int _rowId;

        private TypeDefinitionHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static TypeDefinitionHandle FromRowId(int rowId)
        {
            return new TypeDefinitionHandle(rowId);
        }

        public static implicit operator Handle(TypeDefinitionHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(TypeDefinitionHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator TypeDefinitionHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Handle.ThrowInvalidCast();
            }

            return new TypeDefinitionHandle(handle.RowId);
        }

        public static explicit operator TypeDefinitionHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new TypeDefinitionHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(TypeDefinitionHandle left, TypeDefinitionHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is TypeDefinitionHandle && ((TypeDefinitionHandle)obj)._rowId == _rowId;
        }

        public bool Equals(TypeDefinitionHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(TypeDefinitionHandle left, TypeDefinitionHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct ExportedTypeHandle : IEquatable<ExportedTypeHandle>
    {
        private const uint tokenType = TokenTypeIds.ExportedType;
        private const byte tokenTypeSmall = (byte)HandleType.ExportedType;
        private readonly int _rowId;

        private ExportedTypeHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static ExportedTypeHandle FromRowId(int rowId)
        {
            return new ExportedTypeHandle(rowId);
        }

        public static implicit operator Handle(ExportedTypeHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(ExportedTypeHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator ExportedTypeHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Handle.ThrowInvalidCast();
            }

            return new ExportedTypeHandle(handle.RowId);
        }

        public static explicit operator ExportedTypeHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new ExportedTypeHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(ExportedTypeHandle left, ExportedTypeHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is ExportedTypeHandle && ((ExportedTypeHandle)obj)._rowId == _rowId;
        }

        public bool Equals(ExportedTypeHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(ExportedTypeHandle left, ExportedTypeHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct TypeReferenceHandle : IEquatable<TypeReferenceHandle>
    {
        private const uint tokenType = TokenTypeIds.TypeRef;
        private const byte tokenTypeSmall = (byte)HandleType.TypeRef;
        private readonly int _rowId;

        private TypeReferenceHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static TypeReferenceHandle FromRowId(int rowId)
        {
            return new TypeReferenceHandle(rowId);
        }

        public static implicit operator Handle(TypeReferenceHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(TypeReferenceHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator TypeReferenceHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Handle.ThrowInvalidCast();
            }

            return new TypeReferenceHandle(handle.RowId);
        }

        public static explicit operator TypeReferenceHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new TypeReferenceHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(TypeReferenceHandle left, TypeReferenceHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is TypeReferenceHandle && ((TypeReferenceHandle)obj)._rowId == _rowId;
        }

        public bool Equals(TypeReferenceHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(TypeReferenceHandle left, TypeReferenceHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct TypeSpecificationHandle : IEquatable<TypeSpecificationHandle>
    {
        private const uint tokenType = TokenTypeIds.TypeSpec;
        private const byte tokenTypeSmall = (byte)HandleType.TypeSpec;
        private readonly int _rowId;

        private TypeSpecificationHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static TypeSpecificationHandle FromRowId(int rowId)
        {
            return new TypeSpecificationHandle(rowId);
        }

        public static implicit operator Handle(TypeSpecificationHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(TypeSpecificationHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator TypeSpecificationHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Handle.ThrowInvalidCast();
            }

            return new TypeSpecificationHandle(handle.RowId);
        }

        public static explicit operator TypeSpecificationHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new TypeSpecificationHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(TypeSpecificationHandle left, TypeSpecificationHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is TypeSpecificationHandle && ((TypeSpecificationHandle)obj)._rowId == _rowId;
        }

        public bool Equals(TypeSpecificationHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(TypeSpecificationHandle left, TypeSpecificationHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct MemberReferenceHandle : IEquatable<MemberReferenceHandle>
    {
        private const uint tokenType = TokenTypeIds.MemberRef;
        private const byte tokenTypeSmall = (byte)HandleType.MemberRef;
        private readonly int _rowId;

        private MemberReferenceHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static MemberReferenceHandle FromRowId(int rowId)
        {
            return new MemberReferenceHandle(rowId);
        }

        public static implicit operator Handle(MemberReferenceHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(MemberReferenceHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator MemberReferenceHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Handle.ThrowInvalidCast();
            }

            return new MemberReferenceHandle(handle.RowId);
        }

        public static explicit operator MemberReferenceHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new MemberReferenceHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(MemberReferenceHandle left, MemberReferenceHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is MemberReferenceHandle && ((MemberReferenceHandle)obj)._rowId == _rowId;
        }

        public bool Equals(MemberReferenceHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(MemberReferenceHandle left, MemberReferenceHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct FieldDefinitionHandle : IEquatable<FieldDefinitionHandle>
    {
        private const uint tokenType = TokenTypeIds.FieldDef;
        private const byte tokenTypeSmall = (byte)HandleType.FieldDef;
        private readonly int _rowId;

        private FieldDefinitionHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static FieldDefinitionHandle FromRowId(int rowId)
        {
            return new FieldDefinitionHandle(rowId);
        }

        public static implicit operator Handle(FieldDefinitionHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(FieldDefinitionHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator FieldDefinitionHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Handle.ThrowInvalidCast();
            }

            return new FieldDefinitionHandle(handle.RowId);
        }

        public static explicit operator FieldDefinitionHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new FieldDefinitionHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(FieldDefinitionHandle left, FieldDefinitionHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is FieldDefinitionHandle && ((FieldDefinitionHandle)obj)._rowId == _rowId;
        }

        public bool Equals(FieldDefinitionHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(FieldDefinitionHandle left, FieldDefinitionHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct EventDefinitionHandle : IEquatable<EventDefinitionHandle>
    {
        private const uint tokenType = TokenTypeIds.Event;
        private const byte tokenTypeSmall = (byte)HandleType.Event;
        private readonly int _rowId;

        private EventDefinitionHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static EventDefinitionHandle FromRowId(int rowId)
        {
            return new EventDefinitionHandle(rowId);
        }

        public static implicit operator Handle(EventDefinitionHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(EventDefinitionHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator EventDefinitionHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Handle.ThrowInvalidCast();
            }

            return new EventDefinitionHandle(handle.RowId);
        }

        public static explicit operator EventDefinitionHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new EventDefinitionHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(EventDefinitionHandle left, EventDefinitionHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is EventDefinitionHandle && ((EventDefinitionHandle)obj)._rowId == _rowId;
        }

        public bool Equals(EventDefinitionHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(EventDefinitionHandle left, EventDefinitionHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct PropertyDefinitionHandle : IEquatable<PropertyDefinitionHandle>
    {
        private const uint tokenType = TokenTypeIds.Property;
        private const byte tokenTypeSmall = (byte)HandleType.Property;
        private readonly int _rowId;

        private PropertyDefinitionHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static PropertyDefinitionHandle FromRowId(int rowId)
        {
            return new PropertyDefinitionHandle(rowId);
        }

        public static implicit operator Handle(PropertyDefinitionHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(PropertyDefinitionHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator PropertyDefinitionHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Handle.ThrowInvalidCast();
            }

            return new PropertyDefinitionHandle(handle.RowId);
        }

        public static explicit operator PropertyDefinitionHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new PropertyDefinitionHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(PropertyDefinitionHandle left, PropertyDefinitionHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is PropertyDefinitionHandle && ((PropertyDefinitionHandle)obj)._rowId == _rowId;
        }

        public bool Equals(PropertyDefinitionHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(PropertyDefinitionHandle left, PropertyDefinitionHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct StandaloneSignatureHandle : IEquatable<StandaloneSignatureHandle>
    {
        private const uint tokenType = TokenTypeIds.Signature;
        private const byte tokenTypeSmall = (byte)HandleType.Signature;
        private readonly int _rowId;

        private StandaloneSignatureHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static StandaloneSignatureHandle FromRowId(int rowId)
        {
            return new StandaloneSignatureHandle(rowId);
        }

        public static implicit operator Handle(StandaloneSignatureHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(StandaloneSignatureHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator StandaloneSignatureHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Handle.ThrowInvalidCast();
            }

            return new StandaloneSignatureHandle(handle.RowId);
        }

        public static explicit operator StandaloneSignatureHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new StandaloneSignatureHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(StandaloneSignatureHandle left, StandaloneSignatureHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is StandaloneSignatureHandle && ((StandaloneSignatureHandle)obj)._rowId == _rowId;
        }

        public bool Equals(StandaloneSignatureHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(StandaloneSignatureHandle left, StandaloneSignatureHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct ParameterHandle : IEquatable<ParameterHandle>
    {
        private const uint tokenType = TokenTypeIds.ParamDef;
        private const byte tokenTypeSmall = (byte)HandleType.ParamDef;
        private readonly int _rowId;

        private ParameterHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static ParameterHandle FromRowId(int rowId)
        {
            return new ParameterHandle(rowId);
        }

        public static implicit operator Handle(ParameterHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(ParameterHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator ParameterHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Handle.ThrowInvalidCast();
            }

            return new ParameterHandle(handle.RowId);
        }

        public static explicit operator ParameterHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new ParameterHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(ParameterHandle left, ParameterHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is ParameterHandle && ((ParameterHandle)obj)._rowId == _rowId;
        }

        public bool Equals(ParameterHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(ParameterHandle left, ParameterHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct GenericParameterHandle : IEquatable<GenericParameterHandle>
    {
        private const uint tokenType = TokenTypeIds.GenericParam;
        private const byte tokenTypeSmall = (byte)HandleType.GenericParam;
        private readonly int _rowId;

        private GenericParameterHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static GenericParameterHandle FromRowId(int rowId)
        {
            return new GenericParameterHandle(rowId);
        }

        public static implicit operator Handle(GenericParameterHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(GenericParameterHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator GenericParameterHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Handle.ThrowInvalidCast();
            }

            return new GenericParameterHandle(handle.RowId);
        }

        public static explicit operator GenericParameterHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new GenericParameterHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(GenericParameterHandle left, GenericParameterHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is GenericParameterHandle && ((GenericParameterHandle)obj)._rowId == _rowId;
        }

        public bool Equals(GenericParameterHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(GenericParameterHandle left, GenericParameterHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct GenericParameterConstraintHandle : IEquatable<GenericParameterConstraintHandle>
    {
        private const uint tokenType = TokenTypeIds.GenericParamConstraint;
        private const byte tokenTypeSmall = (byte)HandleType.GenericParamConstraint;
        private readonly int _rowId;

        private GenericParameterConstraintHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static GenericParameterConstraintHandle FromRowId(int rowId)
        {
            return new GenericParameterConstraintHandle(rowId);
        }

        public static implicit operator Handle(GenericParameterConstraintHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(GenericParameterConstraintHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator GenericParameterConstraintHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Handle.ThrowInvalidCast();
            }

            return new GenericParameterConstraintHandle(handle.RowId);
        }

        public static explicit operator GenericParameterConstraintHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new GenericParameterConstraintHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(GenericParameterConstraintHandle left, GenericParameterConstraintHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is GenericParameterConstraintHandle && ((GenericParameterConstraintHandle)obj)._rowId == _rowId;
        }

        public bool Equals(GenericParameterConstraintHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(GenericParameterConstraintHandle left, GenericParameterConstraintHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct ModuleReferenceHandle : IEquatable<ModuleReferenceHandle>
    {
        private const uint tokenType = TokenTypeIds.ModuleRef;
        private const byte tokenTypeSmall = (byte)HandleType.ModuleRef;
        private readonly int _rowId;

        private ModuleReferenceHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static ModuleReferenceHandle FromRowId(int rowId)
        {
            return new ModuleReferenceHandle(rowId);
        }

        public static implicit operator Handle(ModuleReferenceHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(ModuleReferenceHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator ModuleReferenceHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Handle.ThrowInvalidCast();
            }

            return new ModuleReferenceHandle(handle.RowId);
        }

        public static explicit operator ModuleReferenceHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new ModuleReferenceHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(ModuleReferenceHandle left, ModuleReferenceHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is ModuleReferenceHandle && ((ModuleReferenceHandle)obj)._rowId == _rowId;
        }

        public bool Equals(ModuleReferenceHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(ModuleReferenceHandle left, ModuleReferenceHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct AssemblyReferenceHandle : IEquatable<AssemblyReferenceHandle>
    {
        private const uint tokenType = TokenTypeIds.AssemblyRef;
        private const byte tokenTypeSmall = (byte)HandleType.AssemblyRef;

        // bits:
        //     31: IsVirtual
        // 24..30: 0
        //  0..23: Heap offset or Virtual index
        private readonly uint _value;

        internal enum VirtualIndex
        {
            System_Runtime,
            System_Runtime_InteropServices_WindowsRuntime,
            System_ObjectModel,
            System_Runtime_WindowsRuntime,
            System_Runtime_WindowsRuntime_UI_Xaml,
            System_Numerics_Vectors,

            Count
        }

        private AssemblyReferenceHandle(uint value)
        {
            _value = value;
        }

        internal static AssemblyReferenceHandle FromRowId(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            return new AssemblyReferenceHandle((uint)rowId);
        }

        internal static AssemblyReferenceHandle FromVirtualIndex(VirtualIndex virtualIndex)
        {
            Debug.Assert(virtualIndex < VirtualIndex.Count);
            return new AssemblyReferenceHandle(TokenTypeIds.VirtualBit | (uint)virtualIndex);
        }

        public static implicit operator Handle(AssemblyReferenceHandle handle)
        {
            return Handle.FromVToken(handle.VToken);
        }

        public static implicit operator EntityHandle(AssemblyReferenceHandle handle)
        {
            return new EntityHandle(handle.VToken);
        }

        public static explicit operator AssemblyReferenceHandle(Handle handle)
        {
            if (handle.Type != tokenTypeSmall)
            {
                Handle.ThrowInvalidCast();
            }

            return new AssemblyReferenceHandle(handle.SpecificEntityHandleValue);
        }

        public static explicit operator AssemblyReferenceHandle(EntityHandle handle)
        {
            if (handle.Type != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new AssemblyReferenceHandle(handle.SpecificHandleValue);
        }

        internal uint Value
        {
            get { return _value; }
        }

        private uint VToken
        {
            get { return _value | tokenType; }
        }

        public bool IsNil
        {
            get { return _value == 0; }
        }

        internal bool IsVirtual
        {
            get { return (_value & TokenTypeIds.VirtualBit) != 0; }
        }

        internal int RowId { get { return (int)(_value & TokenTypeIds.RIDMask); } }

        public static bool operator ==(AssemblyReferenceHandle left, AssemblyReferenceHandle right)
        {
            return left._value == right._value;
        }

        public override bool Equals(object obj)
        {
            return obj is AssemblyReferenceHandle && ((AssemblyReferenceHandle)obj)._value == _value;
        }

        public bool Equals(AssemblyReferenceHandle other)
        {
            return _value == other._value;
        }

        public override int GetHashCode()
        {
            return _value.GetHashCode();
        }

        public static bool operator !=(AssemblyReferenceHandle left, AssemblyReferenceHandle right)
        {
            return left._value != right._value;
        }
    }

    public struct CustomAttributeHandle : IEquatable<CustomAttributeHandle>
    {
        private const uint tokenType = TokenTypeIds.CustomAttribute;
        private const byte tokenTypeSmall = (byte)HandleType.CustomAttribute;
        private readonly int _rowId;

        private CustomAttributeHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static CustomAttributeHandle FromRowId(int rowId)
        {
            return new CustomAttributeHandle(rowId);
        }

        public static implicit operator Handle(CustomAttributeHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(CustomAttributeHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator CustomAttributeHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Handle.ThrowInvalidCast();
            }

            return new CustomAttributeHandle(handle.RowId);
        }

        public static explicit operator CustomAttributeHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new CustomAttributeHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return _rowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(CustomAttributeHandle left, CustomAttributeHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is CustomAttributeHandle && ((CustomAttributeHandle)obj)._rowId == _rowId;
        }

        public bool Equals(CustomAttributeHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(CustomAttributeHandle left, CustomAttributeHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct DeclarativeSecurityAttributeHandle : IEquatable<DeclarativeSecurityAttributeHandle>
    {
        private const uint tokenType = TokenTypeIds.DeclSecurity;
        private const byte tokenTypeSmall = (byte)HandleType.DeclSecurity;
        private readonly int _rowId;

        private DeclarativeSecurityAttributeHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static DeclarativeSecurityAttributeHandle FromRowId(int rowId)
        {
            return new DeclarativeSecurityAttributeHandle(rowId);
        }

        public static implicit operator Handle(DeclarativeSecurityAttributeHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(DeclarativeSecurityAttributeHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator DeclarativeSecurityAttributeHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Handle.ThrowInvalidCast();
            }

            return new DeclarativeSecurityAttributeHandle(handle.RowId);
        }

        public static explicit operator DeclarativeSecurityAttributeHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new DeclarativeSecurityAttributeHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return _rowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(DeclarativeSecurityAttributeHandle left, DeclarativeSecurityAttributeHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is DeclarativeSecurityAttributeHandle && ((DeclarativeSecurityAttributeHandle)obj)._rowId == _rowId;
        }

        public bool Equals(DeclarativeSecurityAttributeHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(DeclarativeSecurityAttributeHandle left, DeclarativeSecurityAttributeHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct ConstantHandle : IEquatable<ConstantHandle>
    {
        private const uint tokenType = TokenTypeIds.Constant;
        private const byte tokenTypeSmall = (byte)HandleType.Constant;
        private readonly int _rowId;

        private ConstantHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static ConstantHandle FromRowId(int rowId)
        {
            return new ConstantHandle(rowId);
        }

        public static implicit operator Handle(ConstantHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(ConstantHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator ConstantHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Handle.ThrowInvalidCast();
            }

            return new ConstantHandle(handle.RowId);
        }

        public static explicit operator ConstantHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new ConstantHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(ConstantHandle left, ConstantHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is ConstantHandle && ((ConstantHandle)obj)._rowId == _rowId;
        }

        public bool Equals(ConstantHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(ConstantHandle left, ConstantHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct ManifestResourceHandle : IEquatable<ManifestResourceHandle>
    {
        private const uint tokenType = TokenTypeIds.ManifestResource;
        private const byte tokenTypeSmall = (byte)HandleType.ManifestResource;
        private readonly int _rowId;

        private ManifestResourceHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static ManifestResourceHandle FromRowId(int rowId)
        {
            return new ManifestResourceHandle(rowId);
        }

        public static implicit operator Handle(ManifestResourceHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(ManifestResourceHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator ManifestResourceHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Handle.ThrowInvalidCast();
            }

            return new ManifestResourceHandle(handle.RowId);
        }

        public static explicit operator ManifestResourceHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new ManifestResourceHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(ManifestResourceHandle left, ManifestResourceHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is ManifestResourceHandle && ((ManifestResourceHandle)obj)._rowId == _rowId;
        }

        public bool Equals(ManifestResourceHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(ManifestResourceHandle left, ManifestResourceHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    public struct AssemblyFileHandle : IEquatable<AssemblyFileHandle>
    {
        private const uint tokenType = TokenTypeIds.File;
        private const byte tokenTypeSmall = (byte)HandleType.File;
        private readonly int _rowId;

        private AssemblyFileHandle(int rowId)
        {
            Debug.Assert(TokenTypeIds.IsValidRowId(rowId));
            _rowId = rowId;
        }

        internal static AssemblyFileHandle FromRowId(int rowId)
        {
            return new AssemblyFileHandle(rowId);
        }

        public static implicit operator Handle(AssemblyFileHandle handle)
        {
            return new Handle(tokenTypeSmall, handle._rowId);
        }

        public static implicit operator EntityHandle(AssemblyFileHandle handle)
        {
            return new EntityHandle((uint)(tokenType | handle._rowId));
        }

        public static explicit operator AssemblyFileHandle(Handle handle)
        {
            if (handle.VType != tokenTypeSmall)
            {
                Handle.ThrowInvalidCast();
            }

            return new AssemblyFileHandle(handle.RowId);
        }

        public static explicit operator AssemblyFileHandle(EntityHandle handle)
        {
            if (handle.VType != tokenType)
            {
                Handle.ThrowInvalidCast();
            }

            return new AssemblyFileHandle(handle.RowId);
        }

        public bool IsNil
        {
            get
            {
                return RowId == 0;
            }
        }

        internal int RowId { get { return _rowId; } }

        public static bool operator ==(AssemblyFileHandle left, AssemblyFileHandle right)
        {
            return left._rowId == right._rowId;
        }

        public override bool Equals(object obj)
        {
            return obj is AssemblyFileHandle && ((AssemblyFileHandle)obj)._rowId == _rowId;
        }

        public bool Equals(AssemblyFileHandle other)
        {
            return _rowId == other._rowId;
        }

        public override int GetHashCode()
        {
            return _rowId.GetHashCode();
        }

        public static bool operator !=(AssemblyFileHandle left, AssemblyFileHandle right)
        {
            return left._rowId != right._rowId;
        }
    }

    // #UserString heap handle
    public struct UserStringHandle : IEquatable<UserStringHandle>
    {
        // bits:
        //     31: 0
        // 24..30: 0
        //  0..23: index
        private readonly int _offset;

        private UserStringHandle(int offset)
        {
            // #US string indices must fit into 24bits since they are used in IL stream tokens
            Debug.Assert((offset & 0xFF000000) == 0);
            _offset = offset;
        }

        internal static UserStringHandle FromOffset(int heapOffset)
        {
            return new UserStringHandle(heapOffset);
        }

        public static implicit operator Handle(UserStringHandle handle)
        {
            return new Handle((byte)HandleType.UserString, handle._offset);
        }

        public static explicit operator UserStringHandle(Handle handle)
        {
            if (handle.VType != HandleType.UserString)
            {
                Handle.ThrowInvalidCast();
            }

            return new UserStringHandle(handle.Offset);
        }

        public bool IsNil
        {
            get { return _offset == 0; }
        }

        internal int GetHeapOffset()
        {
            return _offset;
        }

        public static bool operator ==(UserStringHandle left, UserStringHandle right)
        {
            return left._offset == right._offset;
        }

        public override bool Equals(object obj)
        {
            return obj is UserStringHandle && ((UserStringHandle)obj)._offset == _offset;
        }

        public bool Equals(UserStringHandle other)
        {
            return _offset == other._offset;
        }

        public override int GetHashCode()
        {
            return _offset.GetHashCode();
        }

        public static bool operator !=(UserStringHandle left, UserStringHandle right)
        {
            return left._offset != right._offset;
        }
    }

    // #String heap handle
    public struct StringHandle : IEquatable<StringHandle>
    {
        // bits:
        //     31: IsVirtual
        // 29..31: type (non-virtual: String, DotTerminatedString; virtual: VirtualString, WinRTPrefixedString)
        //  0..28: Heap offset or Virtual index
        private readonly uint _value;

        internal enum VirtualIndex
        {
            System_Runtime_WindowsRuntime,
            System_Runtime,
            System_ObjectModel,
            System_Runtime_WindowsRuntime_UI_Xaml,
            System_Runtime_InteropServices_WindowsRuntime,
            System_Numerics_Vectors,

            Dispose,

            AttributeTargets,
            AttributeUsageAttribute,
            Color,
            CornerRadius,
            DateTimeOffset,
            Duration,
            DurationType,
            EventHandler1,
            EventRegistrationToken,
            Exception,
            GeneratorPosition,
            GridLength,
            GridUnitType,
            ICommand,
            IDictionary2,
            IDisposable,
            IEnumerable,
            IEnumerable1,
            IList,
            IList1,
            INotifyCollectionChanged,
            INotifyPropertyChanged,
            IReadOnlyDictionary2,
            IReadOnlyList1,
            KeyTime,
            KeyValuePair2,
            Matrix,
            Matrix3D,
            Matrix3x2,
            Matrix4x4,
            NotifyCollectionChangedAction,
            NotifyCollectionChangedEventArgs,
            NotifyCollectionChangedEventHandler,
            Nullable1,
            Plane,
            Point,
            PropertyChangedEventArgs,
            PropertyChangedEventHandler,
            Quaternion,
            Rect,
            RepeatBehavior,
            RepeatBehaviorType,
            Size,
            System,
            System_Collections,
            System_Collections_Generic,
            System_Collections_Specialized,
            System_ComponentModel,
            System_Numerics,
            System_Windows_Input,
            Thickness,
            TimeSpan,
            Type,
            Uri,
            Vector2,
            Vector3,
            Vector4,
            Windows_Foundation,
            Windows_UI,
            Windows_UI_Xaml,
            Windows_UI_Xaml_Controls_Primitives,
            Windows_UI_Xaml_Media,
            Windows_UI_Xaml_Media_Animation,
            Windows_UI_Xaml_Media_Media3D,

            Count
        }

        private StringHandle(uint value)
        {
            Debug.Assert((value & HeapHandleType.TypeMask) == StringHandleType.String ||
                         (value & HeapHandleType.TypeMask) == StringHandleType.VirtualString ||
                         (value & HeapHandleType.TypeMask) == StringHandleType.WinRTPrefixedString ||
                         (value & HeapHandleType.TypeMask) == StringHandleType.DotTerminatedString);

            _value = value;
        }

        internal static StringHandle FromOffset(int heapOffset)
        {
            return new StringHandle(StringHandleType.String | (uint)heapOffset);
        }

        internal static StringHandle FromVirtualIndex(VirtualIndex virtualIndex)
        {
            Debug.Assert(virtualIndex < VirtualIndex.Count);
            return new StringHandle(StringHandleType.VirtualString | (uint)virtualIndex);
        }

        internal StringHandle WithWinRTPrefix()
        {
            Debug.Assert(StringKind == StringKind.Plain);
            return new StringHandle(StringHandleType.WinRTPrefixedString | _value);
        }

        internal StringHandle WithDotTermination()
        {
            Debug.Assert(StringKind == StringKind.Plain);
            return new StringHandle(StringHandleType.DotTerminatedString | _value);
        }

        internal StringHandle SuffixRaw(int prefixByteLength)
        {
            Debug.Assert(StringKind == StringKind.Plain);
            Debug.Assert(prefixByteLength >= 0);
            return new StringHandle(StringHandleType.String | (_value + (uint)prefixByteLength));
        }

        public static implicit operator Handle(StringHandle handle)
        {
            // VTT... -> V111 10TT
            return new Handle(
                (byte)((handle._value & HeapHandleType.VirtualBit) >> 24 | HandleType.String | (handle._value & HeapHandleType.NonVirtualTypeMask) >> 26),
                (int)(handle._value & HeapHandleType.OffsetMask));
        }

        public static explicit operator StringHandle(Handle handle)
        {
            if ((handle.VType & HandleType.StringOrNamespaceMask) != HandleType.String)
            {
                Handle.ThrowInvalidCast();
            }

            // V111 10TT -> VTT...
            return new StringHandle(
                (handle.VType & HandleType.VirtualBit) << 24 | 
                (handle.VType & HandleType.StringHeapTypeMask) << HeapHandleType.OffsetBitCount | 
                (uint)handle.Offset);
        }

        internal bool IsVirtual
        {
            get { return (_value & HeapHandleType.VirtualBit) != 0; }
        }

        public bool IsNil
        {
            get
            {
                // virtual strings are never nil, so include virtual bit
                return (_value & (HeapHandleType.VirtualBit | HeapHandleType.OffsetMask)) == 0;
            }
        }

        internal int GetHeapOffset()
        {
            // WinRT prefixed strings are virtual, the value is a heap offset
            Debug.Assert(!IsVirtual || StringKind == StringKind.WinRTPrefixed);
            return (int)(_value & HeapHandleType.OffsetMask);
        }

        internal VirtualIndex GetVirtualIndex()
        {
            Debug.Assert(IsVirtual && StringKind != StringKind.WinRTPrefixed);
            return (VirtualIndex)(_value & HeapHandleType.OffsetMask);
        }

        internal StringKind StringKind
        {
            get { return (StringKind)(_value >> HeapHandleType.OffsetBitCount); }
        }

        public override bool Equals(object obj)
        {
            return obj is StringHandle && Equals((StringHandle)obj);
        }

        public bool Equals(StringHandle other)
        {
            return _value == other._value;
        }

        public override int GetHashCode()
        {
            return unchecked((int)_value);
        }

        public static bool operator ==(StringHandle left, StringHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(StringHandle left, StringHandle right)
        {
            return !left.Equals(right);
        }
    }

    /// <summary>
    /// A handle that represents a namespace definition. 
    /// </summary>
    public struct NamespaceDefinitionHandle : IEquatable<NamespaceDefinitionHandle>
    {
        // At this time, IsVirtual is always false because namespace names come from type definitions 
        // and type forwarders only, which never get their namespaces projected.
        // 
        // For Namespace, the offset is to the null-terminated full name of the 
        // namespace in the #String heap.
        //
        // For SyntheticNamespace, the offset points to the dot-terminated simple name of the namespace
        // in the #String heap. This is used to represent namespaces that are parents of other namespaces
        // but no type definitions or forwarders of their own.

        // bits:
        //     31: IsVirtual (0)
        // 29..31: type (non-virtual: Namespace, SyntheticNamespace)
        //  0..28: Heap offset or Virtual index
        private readonly uint _value;

        private NamespaceDefinitionHandle(uint value)
        {
            // note: no virtual namespaces allowed
            Debug.Assert((value & HeapHandleType.TypeMask) == NamespaceHandleType.Namespace ||
                         (value & HeapHandleType.TypeMask) == NamespaceHandleType.SyntheticNamespace);

            _value = value;
        }

        internal static NamespaceDefinitionHandle FromFullNameOffset(int stringHeapOffset)
        {
            return new NamespaceDefinitionHandle(NamespaceHandleType.Namespace | (uint)stringHeapOffset);
        }

        internal static NamespaceDefinitionHandle FromSimpleNameOffset(int stringHeapOffset)
        {
            return new NamespaceDefinitionHandle(NamespaceHandleType.SyntheticNamespace | (uint)stringHeapOffset);
        }

        public static implicit operator Handle(NamespaceDefinitionHandle handle)
        {
            // VTT... -> V111 11TT
            return new Handle(
                (byte)((handle._value & HeapHandleType.VirtualBit) >> 24 | HandleType.Namespace | (handle._value & HeapHandleType.NonVirtualTypeMask) >> 2),
                (int)(handle._value & HeapHandleType.OffsetMask));
        }

        public static explicit operator NamespaceDefinitionHandle(Handle handle)
        {
            // namespaces currently can't be virtual:
            if ((handle.VType & (HandleType.VirtualBit | HandleType.StringOrNamespaceMask)) != HandleType.Namespace)
            {
                Handle.ThrowInvalidCast();
            }

            // V111 11TT -> VTT...
            return new NamespaceDefinitionHandle(
                (handle.VType & HandleType.VirtualBit) << 24 |
                (handle.VType & HandleType.StringHeapTypeMask) << HeapHandleType.OffsetBitCount |
                (uint)handle.Offset);
        }

        public bool IsNil
        {
            get
            {
                // virtual strings are never nil, so include virtual bit
                return (_value & (HeapHandleType.VirtualBit | HeapHandleType.OffsetMask)) == 0;
            }
        }

        internal bool IsVirtual
        {
            get { return (_value & HeapHandleType.VirtualBit) != 0; }
        }

        internal int GetHeapOffset()
        {
            Debug.Assert(!IsVirtual);
            return (int)(_value & HeapHandleType.OffsetMask);
        }

        internal NamespaceKind NamespaceKind
        {
            get { return (NamespaceKind)(_value >> HeapHandleType.OffsetBitCount); }
        }

        internal bool HasFullName
        {
            get { return NamespaceKind != NamespaceKind.Synthetic; }
        }

        internal StringHandle GetFullName()
        {
            Debug.Assert(!IsVirtual);
            Debug.Assert(HasFullName);
            return StringHandle.FromOffset(GetHeapOffset());
        }

        public override bool Equals(object obj)
        {
            return obj is NamespaceDefinitionHandle && Equals((NamespaceDefinitionHandle)obj);
        }

        public bool Equals(NamespaceDefinitionHandle other)
        {
            return _value == other._value;
        }

        public override int GetHashCode()
        {
            return unchecked((int)_value);
        }

        public static bool operator ==(NamespaceDefinitionHandle left, NamespaceDefinitionHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(NamespaceDefinitionHandle left, NamespaceDefinitionHandle right)
        {
            return !left.Equals(right);
        }
    }

    // #Blob heap handle
    public struct BlobHandle : IEquatable<BlobHandle>
    {
        // bits:
        //     31: IsVirtual
        // 29..30: 0
        //  0..28: Heap offset or Virtual Value (16 bits) + Virtual Index (8 bits)
        private readonly uint _value;

        internal enum VirtualIndex : byte
        {
            Nil,

            // B0 3F 5F 7F 11 D5 0A 3A
            ContractPublicKeyToken,

            // 00, 24, 00, 00, 04, ...
            ContractPublicKey,

            // Template for projected AttributeUsage attribute blob
            AttributeUsage_AllowSingle,

            // Template for projected AttributeUsage attribute blob with AllowMultiple=true
            AttributeUsage_AllowMultiple,

            Count
        }

        private BlobHandle(uint value)
        {
            _value = value;
        }

        internal static BlobHandle FromOffset(int heapOffset)
        {
            return new BlobHandle((uint)heapOffset);
        }

        internal static BlobHandle FromVirtualIndex(VirtualIndex virtualIndex, ushort virtualValue)
        {
            Debug.Assert(virtualIndex < VirtualIndex.Count);
            return new BlobHandle(TokenTypeIds.VirtualBit | (uint)(virtualValue << 8) | (uint)virtualIndex);
        }

        internal const int TemplateParameterOffset_AttributeUsageTarget = 2;

        internal unsafe void SubstituteTemplateParameters(byte[] blob)
        {
            Debug.Assert(blob.Length >= TemplateParameterOffset_AttributeUsageTarget + 4);

            fixed (byte* ptr = &blob[TemplateParameterOffset_AttributeUsageTarget])
            {
                *((uint*)ptr) = VirtualValue;
            }
        }

        public static implicit operator Handle(BlobHandle handle)
        {
            // V... -> V111 0001
            return new Handle(
                (byte)((handle._value & HeapHandleType.VirtualBit) >> 24 | HandleType.Blob), 
                (int)(handle._value & HeapHandleType.OffsetMask));
        }

        public static explicit operator BlobHandle(Handle handle)
        {
            if ((handle.VType & HandleType.TypeMask) != HandleType.Blob)
            {
                Handle.ThrowInvalidCast();
            }

            return new BlobHandle(
                (handle.VType & HandleType.VirtualBit) << TokenTypeIds.RowIdBitCount |
                (uint)handle.Offset);
        }

        public bool IsNil
        {
            get { return _value == 0; }
        }

        internal int GetHeapOffset()
        {
            Debug.Assert(!IsVirtual);
            return (int)_value;
        }

        internal VirtualIndex GetVirtualIndex()
        {
            Debug.Assert(IsVirtual);
            return (VirtualIndex)(_value & 0xff);
        }

        internal bool IsVirtual
        {
            get { return (_value & TokenTypeIds.VirtualBit) != 0; }
        }

        private ushort VirtualValue
        {
            get { return unchecked((ushort)(_value >> 8)); }
        }

        public override bool Equals(object obj)
        {
            return obj is BlobHandle && Equals((BlobHandle)obj);
        }

        public bool Equals(BlobHandle other)
        {
            return _value == other._value;
        }

        public override int GetHashCode()
        {
            return unchecked((int)_value);
        }

        public static bool operator ==(BlobHandle left, BlobHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(BlobHandle left, BlobHandle right)
        {
            return !left.Equals(right);
        }
    }

    // #Guid heap handle
    public struct GuidHandle : IEquatable<GuidHandle>
    {
        // The Guid heap is an array of GUIDs, each 16 bytes wide. 
        // Its first element is numbered 1, its second 2, and so on.
        private readonly int _index;

        private GuidHandle(int index)
        {
            _index = index;
        }

        internal static GuidHandle FromIndex(int heapIndex)
        {
            return new GuidHandle(heapIndex);
        }

        public static implicit operator Handle(GuidHandle handle)
        {
            return new Handle((byte)HandleType.Guid, handle._index);
        }

        public static explicit operator GuidHandle(Handle handle)
        {
            if (handle.VType != HandleType.Guid)
            {
                Handle.ThrowInvalidCast();
            }

            return new GuidHandle(handle.Offset);
        }

        public bool IsNil
        {
            get { return _index == 0; }
        }

        internal int Index
        {
            get { return _index; }
        }

        public override bool Equals(object obj)
        {
            return obj is GuidHandle && Equals((GuidHandle)obj);
        }

        public bool Equals(GuidHandle other)
        {
            return _index == other._index;
        }

        public override int GetHashCode()
        {
            return _index;
        }

        public static bool operator ==(GuidHandle left, GuidHandle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(GuidHandle left, GuidHandle right)
        {
            return !left.Equals(right);
        }
    }
}
