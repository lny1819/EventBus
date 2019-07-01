using System;

namespace YiDian.EventBus.MQ.KeyAttribute
{
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class SeralizeIndex : Attribute
    {
        private int v;

        public SeralizeIndex(int v)
        {
            this.v = v;
        }
    }
}
