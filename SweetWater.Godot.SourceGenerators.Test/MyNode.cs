using Godot;
using System;

namespace SweetWater.Godot.SourceGenerators.Test;

public partial class MyNode : Node
{
    [OnReady("Xxx")]
    public Node2D String { get; set; }

    [OnReady("yyy")]
    public Timer obj;

    [OnReady("gdfds")]
    public Node obj2;
    public override void _Ready()
    {
        _OnReady();
        Test?.Invoke();
    }

    public event Action? Test;

    [OnEvent("Test")]
    public void OnEventMethod()
    {
        GD.Print("OnEventMethod");
    }
}