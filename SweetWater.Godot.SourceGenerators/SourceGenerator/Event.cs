using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace SweetWater.Godot.SourceGenerators.SourceGenerator;

public sealed class Event<T> where T : Delegate
{
    private readonly List<T> delegates = new(0);

    public void Invoke(Action<T> action)
    {
        foreach (var it in delegates.ToArray()) action.Invoke(it);
    }

    public static Event<T> operator +(Event<T> left, (object owner, T @delegate) right)
    {
        lock (GetLock(left))
        {
            if (right.@delegate == null) return left;
            left ??= new Event<T>();
            left.delegates.Add(right.@delegate);
            AutoRemoveHandler.Register(left, right.owner, right.@delegate);
            return left;
        }
    }

    public static Event<T> operator +(Event<T> left, T right)
    {
        lock (GetLock(left))
        {
            if (right == null) return left;
            left ??= new Event<T>();
            left.delegates.Add(right);
            return left;
        }
    }

    public static Event<T> operator -(Event<T> left, T right)
    {
        lock (GetLock(left))
        {
            if (right == null) return left;
            left ??= new Event<T>();
            left.delegates.Remove(right);
            return left;
        }
    }

    private static object GetLock(object obj)
    {
        lock (typeof(EventHandler))
        {
            return obj ?? typeof(EventHandler);
        }
    }

    public override string ToString() => $"{typeof(T)}";

    private static class AutoRemoveHandler
    {
        private static List<(Event<T> handler, WeakReference owner, T @delegate)> collect = new();
        private static readonly List<(Event<T> handler, WeakReference owner, T @delegate)> registry = new();
        private static readonly Thread thread;
        private static readonly int interval = 60 * 1000;

        static AutoRemoveHandler()
        {
            thread = new Thread(() =>
            {
                while (true)
                {
                    Thread.Sleep(interval);
                    if (registry.Count != 0)
                    {
                        lock (typeof(AutoRemoveHandler))
                        {
                            collect.AddRange(registry);
                            registry.Clear();
                        }
                    }
                    if (collect.Count == 0) continue;
                    collect = collect
                    .Where(v =>
                    {
                        var isAlive = v.owner.IsAlive;
                        if (!isAlive) v.handler -= v.@delegate;
                        return v.owner.IsAlive;
                    })
                    .ToList();
                }
            });
            thread.Start();
        }

        public static void Register(Event<T> handler, object owner, T @delegate)
        {
            lock (typeof(AutoRemoveHandler))
            {
                registry.Add((handler, new WeakReference(owner), @delegate));
            }
        }
    }
}
