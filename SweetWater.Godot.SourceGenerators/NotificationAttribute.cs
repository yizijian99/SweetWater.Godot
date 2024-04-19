using System;

namespace Godot
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    public class NotificationAttribute : Attribute
    {
        public long What;

        public NotificationAttribute(long what) => What = what;
    }
}