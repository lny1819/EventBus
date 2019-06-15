using System.IO;
using System.Threading.Tasks;

namespace YiDian.EventBus
{
    public interface IDynamicBytesHandler
    {
        Task<bool> Handle(string routingKey, byte[] datas);
    }
}
