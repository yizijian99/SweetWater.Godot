using System;

namespace Godot;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class OnEventAttribute : Attribute
{
    public string EventName;
    public string NodePath;
    public Type? NodeType;

    public OnEventAttribute(string eventName, string nodePath = ".", Type? nodeType = null)
    {
        EventName = eventName;
        NodePath = nodePath;
        NodeType = nodeType;
    }
}
