using Godot;
using System;

namespace SweetWater.Godot.SourceGenerators.Test;

public partial class Ticktok : Icon
{
    public event Action? Tt;

    public new event Action? Tick;

    public override void _Ready()
    {
        _OnReady();
    }
}
