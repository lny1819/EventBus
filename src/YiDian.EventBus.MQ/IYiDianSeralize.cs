using System;
using System.Collections.Generic;
using System.Text;

namespace YiDian.EventBus.MQ
{
    /// <summary>
    /// 消息默认序列化接口
    /// </summary>
    public interface IYiDianSeralize
    {
        /// <summary>
        /// 序列化写入
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        uint ToBytes(WriteStream stream);
        /// <summary>
        /// 反序列化读取
        /// </summary>
        /// <param name="stream"></param>
        void BytesTo(ReadStream stream);
        /// <summary>
        /// 消息长度
        /// </summary>
        /// <param name="encoding"></param>
        /// <returns></returns>
        uint BytesSize(Encoding encoding);
    }
    /// <summary>
    /// 消息字段长度类型
    /// </summary>
    public enum EventPropertyType : byte
    {
        /// <summary>
        /// 8字节
        /// </summary>
        L_8,
        /// <summary>
        /// 16字节
        /// </summary>
        L_16,
        /// <summary>
        /// 32字节
        /// </summary>
        L_32,
        /// <summary>
        /// 日期，11字节
        /// </summary>
        L_Date,
        /// <summary>
        /// 64字节
        /// </summary>
        L_64,
        /// <summary>
        /// 字符串，可变长
        /// </summary>
        L_Str,
        /// <summary>
        /// 数组，可变长
        /// </summary>
        L_Array,
        /// <summary>
        /// 复合类型对象，可变长
        /// </summary>
        L_N
    }
}
