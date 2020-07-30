using System;

namespace YiDian.EventBus.MQ.KeyAttribute
{
    /// <summary>
    /// 序列化序号
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = false)]
    public sealed class SeralizeIndex : Attribute
    {
        /// <summary>
        /// 创建一个序列化序号特性实例
        /// </summary>
        /// <param name="v">序号</param>
        public SeralizeIndex(int v)
        {
            Index = v;
        }
        /// <summary>
        /// 序号
        /// </summary>
        public int Index { get; }
    }
}
