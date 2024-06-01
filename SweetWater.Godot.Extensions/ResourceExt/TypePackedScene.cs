using System.Diagnostics.CodeAnalysis;
using Godot;
using Godot.Collections;

namespace SweetWater.Godot.Extensions.ResourceExt;

[GlobalClass]
// ReSharper disable once PartialTypeWithSinglePart
public partial class TypePackedScene : Resource
{
    [SuppressMessage("ReSharper", "UnusedAutoPropertyAccessor.Global")]
    protected virtual PackedScene PackedScene { get; set; }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public Dictionary _Bundled
    {
        get => PackedScene._Bundled;
        set => PackedScene._Bundled = value;
    }

    public virtual Type Type => typeof(object);

    public bool CanInstantiate() => PackedScene.CanInstantiate();

    public SceneState GetState() => PackedScene.GetState();

    public virtual Node Instantiate(PackedScene.GenEditState editState = PackedScene.GenEditState.Disabled)
        => PackedScene.Instantiate(editState);

    public T InstantiateOrNull<T>(PackedScene.GenEditState editState = PackedScene.GenEditState.Disabled)
        where T : class => PackedScene.Instantiate(editState) as T;

    public Error Pack(Node path) => PackedScene.Pack(path);
}