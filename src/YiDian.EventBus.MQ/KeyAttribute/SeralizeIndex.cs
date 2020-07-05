using System;

namespace YiDian.EventBus.MQ.KeyAttribute
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class SeralizeIndex : Attribute
    {
        public SeralizeIndex(int v)
        {
            Index = v;
        }
        public int Index { get; }
    }
}
