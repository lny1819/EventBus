using System;

namespace YiDian.EventBus
{
    /// <summary>
    /// 消息路由键特性
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true, Inherited = true)]
    public class KeyIndex : Attribute
    {
        /// <summary>
        /// 消息路由键
        /// </summary>
        /// <param name="index">顺序编号</param>
        public KeyIndex(int index)
        {
            Index = index;
        }
        /// <summary>
        /// 编号
        /// </summary>
        public int Index { get; }
    }
}