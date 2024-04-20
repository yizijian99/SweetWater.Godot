using Godot;

namespace SweetWater.Godot.SourceGenerators.Test;

internal partial class MyNode : Node
{
    [OnReady("Xxx")]
    public string String { get; }
    
    [OnReady("yyy")]
    public Timer obj;
    
    [OnReady("gdfds")]
    public Node obj2;

    public override void _Ready()
    {
        base._Ready();
        _OnReady();
    }
}