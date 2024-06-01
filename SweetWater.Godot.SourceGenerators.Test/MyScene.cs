using Godot;
using SweetWater.Godot.SourceGenerators.Attribute;

namespace SweetWater.Godot.SourceGenerators.Test;

public partial class MyScene : Node2D
{
    [OnReady("MyNode")]
    public string MyNode;
}