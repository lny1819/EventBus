using System;

namespace YiDian.EventBus.MQ.KeyAttribute
{
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class KeyIndex : Attribute
    {
        public KeyIndex(int index)
        {
            Index = index;
        }
        public int Index { get; }
    }
}