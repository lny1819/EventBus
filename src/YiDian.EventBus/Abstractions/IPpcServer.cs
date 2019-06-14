using System;
using System.Collections.Generic;
using System.Text;

namespace YiDian.EventBus.Abstractions
{
    public interface IPpcServer
    {
        RpcServerConfig Configs { get; }
    }
}
