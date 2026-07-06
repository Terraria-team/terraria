using System;
using UnityEngine;

public readonly struct BlockID : IEquatable<BlockID>, IComparable<BlockID>
{
    public readonly ushort Value;

    public BlockID(ushort value) => Value = value;

    public Color GetColor()
    {
        return Value == 0 ? Color.white : Color.red;
    }
    
    public static explicit operator ushort(BlockID id) => id.Value;
    public static explicit operator BlockID(ushort value) => new(value);

    public bool Equals(BlockID other) => Value == other.Value;
    public override bool Equals(object obj) => obj is BlockID other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();

    public int CompareTo(BlockID other) => Value.CompareTo(other.Value);
    public static bool operator ==(BlockID left, BlockID right) => left.Equals(right);
    public static bool operator !=(BlockID left, BlockID right) => !left.Equals(right);
}