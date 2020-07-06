using System;
using System.Collections.Generic;
using System.Text;

namespace YiDian.EventBus.MQ.Rpc
{
    public class RpcServerConfig
    {
        /// <summary>
        /// 服务器ID
        /// <para>连接在同一个MQ集群上的不同应用应该具有不同的名称</para>
        /// 通过配置相同名称的服务器达到服务器间的负载均衡
        /// </summary>
        public string ApplicationId { get; set; }

        public string MQName { get; set; }
        /// <summary>
        /// 批量从队列获取消息数量
        /// </summary>
        public ushort Fetchout { get; set; } = 200;

        public int ControllerCache { get; set; } = 2000;
        public double Delay { get; set; }
    }
}
