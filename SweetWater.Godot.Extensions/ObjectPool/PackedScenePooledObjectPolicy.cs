using System.Diagnostics.CodeAnalysis;
using Godot;
using Microsoft.Extensions.ObjectPool;

namespace SweetWater.Godot.Extensions.ObjectPool;

public class PackedScenePooledObjectPolicy<T> : DefaultPooledObjectPolicy<Node> where T : Node
{
    private string packedScenePath;
    private Func<Node, bool> resetFunc;

    public PackedScenePooledObjectPolicy(string packedScenePath, Func<Node, bool> resetFunc = null)
    {
        this.packedScenePath = packedScenePath;
        this.resetFunc = resetFunc;
    }

    public override T Create()
    {
        if (string.IsNullOrWhiteSpace(packedScenePath)) return null!;

        PackedScene packedScene = ResourceLoader.Load<PackedScene>(packedScenePath);
        if (packedScene == null || !packedScene.CanInstantiate()) return null!;

        return packedScene.InstantiateOrNull<T>();
    }

    [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global")]
    public override bool Return(Node obj)
    {
        if (obj is IResettable resettable)
        {
            return resettable.TryReset();
        }

        if (resetFunc != null)
        {
            return resetFunc(obj);
        }

        return true;
    }
}