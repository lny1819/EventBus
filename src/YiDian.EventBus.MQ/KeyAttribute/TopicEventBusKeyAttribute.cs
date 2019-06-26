using System;

namespace YiDian.EventBus.MQ.KeyAttribute
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
}