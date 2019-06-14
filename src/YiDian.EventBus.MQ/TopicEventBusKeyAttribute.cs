using System;

namespace YiDian.EventBus.MQ
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class KeyIndexAttribute : Attribute
    {
        public KeyIndexAttribute(int index)
        {
            Index = index;
        }
        public int Index { get; }
    }
    [AttributeUsage(AttributeTargets.Class)]
    public class KeyNameAttribute : Attribute
    {
        public KeyNameAttribute(string name)
        {
            Name = name;
        }
        public string Name { get; }
    }
}