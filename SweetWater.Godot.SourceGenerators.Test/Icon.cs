using Godot;
using System;

namespace SweetWater.Godot.SourceGenerators.Test;

public partial class Icon : Sprite2D
{
    public event Action? Tick;

    private Action? _tok;

    public event Action? Tok
    {
        add => _tok += value;
        remove => _tok -= value;
    }

    public override void _Ready()
    {
        _OnReady();
    }
}