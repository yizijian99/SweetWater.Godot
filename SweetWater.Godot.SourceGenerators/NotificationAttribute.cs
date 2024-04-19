using System;

namespace Godot
{
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class NotificationAttribute : Attribute
    {
        public long What;

        public NotificationAttribute(long what) => What = what;
    }
}