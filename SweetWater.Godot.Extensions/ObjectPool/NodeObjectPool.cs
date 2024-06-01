#nullable enable
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using Godot;
using Microsoft.Extensions.ObjectPool;
using Environment = System.Environment;

namespace SweetWater.Godot.Extensions.ObjectPool;

public class NodeObjectPool<T> : IDisposable where T : Node
{
    private readonly Func<T> _createFunc;
    private readonly Func<T, bool> _returnFunc;
    private readonly int _maxCapacity;
    private int _numItems;

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private protected readonly ConcurrentQueue<T> _items = new();

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private protected T? _fastItem;

#pragma warning disable CS0414 // Field is assigned but its value is never used
    private volatile bool _isDisposed;
#pragma warning restore CS0414 // Field is assigned but its value is never used

    /// <summary>
    /// Creates an instance of <see cref="DefaultObjectPool{T}"/>.
    /// </summary>
    /// <param name="policy">The pooling policy to use.</param>
    public NodeObjectPool(IPooledObjectPolicy<T> policy)
        : this(policy, Environment.ProcessorCount * 2)
    {
    }

    /// <summary>
    /// Creates an instance of <see cref="DefaultObjectPool{T}"/>.
    /// </summary>
    /// <param name="policy">The pooling policy to use.</param>
    /// <param name="maximumRetained">The maximum number of objects to retain in the pool.</param>
    public NodeObjectPool(IPooledObjectPolicy<T> policy, int maximumRetained)
    {
        // cache the target interface methods, to avoid interface lookup overhead
        _createFunc = policy.Create;
        _returnFunc = policy.Return;
        _maxCapacity = maximumRetained - 1; // -1 to account for _fastItem
    }

    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public E Get<E>() where E : T
    {
        T? item = _fastItem;
        if (item == null || Interlocked.CompareExchange(ref _fastItem, null, item) != item)
        {
            if (_items.TryDequeue(out item))
            {
                Interlocked.Decrement(ref _numItems);
                return (E)item;
            }

            // no object available, so go get a brand new one
            return (E)_createFunc();
        }

        return (E)item;
    }

    public void Return(T obj)
    {
        // When the node is not returned to the pool, queue free it
        if (!ReturnCore(obj))
        {
            QueueFreeNode(obj);
        }
    }

    /// <summary>
    /// Returns an object to the pool.
    /// </summary>
    /// <returns>true if the object was returned to the pool</returns>
    private protected bool ReturnCore(T obj)
    {
        if (!_returnFunc(obj))
        {
            // policy says to drop this object
            return false;
        }

        if (_fastItem != null || Interlocked.CompareExchange(ref _fastItem, obj, null) != null)
        {
            if (Interlocked.Increment(ref _numItems) <= _maxCapacity)
            {
                _items.Enqueue(obj);
                return true;
            }

            // no room, clean up the count and drop the object on the floor
            Interlocked.Decrement(ref _numItems);
            return false;
        }

        return true;
    }

    public void Dispose()
    {
        _isDisposed = true;

        QueueFreeNode(_fastItem);
        _fastItem = null;

        while (_items.TryDequeue(out T? item))
        {
            QueueFreeNode(item);
        }
    }

    public static void QueueFreeNode(T? node)
    {
        if (node == null) return;
        Node parent = node.GetParentOrNull<Node>();
        if (parent != null)
        {
            parent.RemoveChild(node);
        }

        node.CallDeferred(Node.MethodName.QueueFree);
    }
}