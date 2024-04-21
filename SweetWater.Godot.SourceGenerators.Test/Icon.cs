using Godot;
using System;

namespace SweetWater.Godot.SourceGenerators.Test;

public partial class Icon : Sprite2D
{
    public event Action? Tick;

    public Action? _tok;

    public event Action? Tok
    {
        add => _tok += value;
        remove => _tok -= value;
    }

    public override void _Ready()
    {
        _OnReady();
        GD.Print(EventName.Tick);
        GD.Print(EventName.Tok);
        Tick += () => GD.Print("Tick");
        Tok += () => GD.Print("Tok");
        Tick?.Invoke();
        _tok?.Invoke();
    }
}