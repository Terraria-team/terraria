using System;
using UnityEngine;

public readonly struct ItemID : IEquatable<ItemID>, IComparable<ItemID>
{
    public readonly int Value;
    public ItemID(int value) => Value = value;
    
    public static explicit operator int(ItemID id) => id.Value;
    
    public static explicit operator ItemID(int value) => new(value);
    public bool Equals(ItemID other) => Value == other.Value;
    public override bool Equals(object obj) => obj is ItemID other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
    public override string ToString() => Value.ToString();

    public int CompareTo(ItemID other) => Value.CompareTo(other.Value);
    public static bool operator ==(ItemID left, ItemID right) => left.Equals(right);
    public static bool operator !=(ItemID left, ItemID right) => !left.Equals(right);
    
    public ItemData ItemData => DataManager.Items.Get(Value);
}