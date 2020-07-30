using System;
using System.Collections.Generic;
using YiDian.EventBus.MQ.Rpc.Route;

namespace YiDian.EventBus.MQ.Rpc.Abstractions
{
    /// <summary>
    /// RPC请求上下文
    /// </summary>
    public class Request
    {
        internal ActionInfo Action { get; set; }
        /// <summary>
        /// 请求头
        /// </summary>
        public Dictionary<string, string> Headers { get; internal set; }
        /// <summary>
        /// 请求地址
        /// </summary>
        public Uri Url { get; set; }
        /// <summary>
        /// 请求体序列化方式
        /// </summary>
        public IEventSeralize Seralize { get; internal set; }
        /// <summary>
        /// 请求体内容长度
        /// </summary>
        public long ContentLength { get; internal set; }
        /// <summary>
        /// 请求体
        /// </summary>
        public ReadOnlyMemory<byte> Body { get; internal set; }
        /// <summary>
        /// 接收请求时间
        /// </summary>
        public DateTime InTime { get; internal set; }
    }
}
