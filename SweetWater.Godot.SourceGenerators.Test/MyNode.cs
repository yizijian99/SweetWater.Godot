using Godot;

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
        GD.Print(Icon.EventName.Tick);
        GD.Print(Ticktok.EventName.Tt);
        GD.Print(SignalName.Ready);
    }
}