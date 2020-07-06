using System;
using System.Threading.Tasks;

namespace YiDian.EventBus
{
    public interface IBytesHandler
    {
        Task<bool> Handle(string routingKey, ReadOnlyMemory<byte> datas);
    }
}
