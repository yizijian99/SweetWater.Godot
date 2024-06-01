using System.Diagnostics.CodeAnalysis;
using Godot;
using Microsoft.Extensions.ObjectPool;

namespace SweetWater.Godot.Extensions.ObjectPool;

[SuppressMessage("ReSharper", "InconsistentNaming")]
public class NodeObjectPools<E> where E : Node
{
    private Dictionary<Type, NodeObjectPool<E>> pools = new();

    public static NodeObjectPools<Node> Create()
    {
        return new NodeObjectPools<Node>();
    }

    public void Register<T>(string packedScenePath, Func<Node, bool> resetFunc = null) where T : E
    {
        IPooledObjectPolicy<E> policy =
            (IPooledObjectPolicy<E>)new PackedScenePooledObjectPolicy<T>(packedScenePath, resetFunc);
        NodeObjectPool<E> objectPool = new(policy);
        pools.TryAdd(typeof(T), objectPool);
    }

    public T Get<T>() where T : E
    {
        Type type = typeof(T);
        if (!pools.TryGetValue(type, out NodeObjectPool<E> objectPool))
        {
            throw new NotSupportedException($"No object pool of type {type} is registered");
        }

        return objectPool.Get<T>();
    }

    public void Return<T>(T obj) where T : E
    {
        Type type = typeof(T);
        if (!pools.TryGetValue(type, out NodeObjectPool<E> objectPool))
        {
            NodeObjectPool<Node>.QueueFreeNode(obj);
            return;
        }

        objectPool.Return(obj);
    }
}