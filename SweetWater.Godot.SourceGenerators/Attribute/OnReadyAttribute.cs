using System;

namespace SweetWater.Godot.SourceGenerators.Attribute;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public sealed class OnReadyAttribute : System.Attribute
{
    public string NodePath;

    public OnReadyAttribute(string nodePath = "") => NodePath = nodePath;
}