using System;
using System.IO;

namespace YiDian.EventBus
{
    /// <summary>
    /// 消息序列化接口定义
    /// </summary>
    public interface IEventSeralize
    {
        /// <summary>
        /// 将消息对象序列化
        /// </summary>
        /// <typeparam name="T">消息类型</typeparam>
        /// <param name="event">消息</param>
        /// <returns>字节数组</returns>
        ReadOnlyMemory<byte> Serialize<T>(T @event);
        /// <summary>
        /// 将消息对象序列化
        /// </summary>
        /// <param name="event">消息</param>
        /// <param name="type">消息类型</param>
        /// <returns>字节数组</returns>
        ReadOnlyMemory<byte> Serialize(object @event, Type type);
        /// <summary>
        /// 将消息对象序列化到指定的字节数据中
        /// </summary>
        /// <param name="event">消息</param>
        /// <param name="type">消息类型</param>
        /// <param name="bs">目标数组</param>
        /// <param name="offset">目标数组起始位置</param>
        /// <returns>数据字节长度</returns>
        int Serialize(object @event, Type type, byte[] bs, int offset);
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="data">字节数组</param>
        /// <param name="type">消息类型</param>
        /// <returns>消息</returns>
        object DeserializeObject(ReadOnlyMemory<byte> data, Type type);
        /// <summary>
        /// 获取消息对象序列化以后的字节长度
        /// </summary>
        /// <param name="obj">消息</param>
        /// <param name="type">消息类型</param>
        /// <returns>字节长度</returns>
        uint GetSize(object obj, Type type);
    }
}
