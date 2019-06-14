using System.IO;
using System.Threading.Tasks;

namespace YiDian.EventBus
{
    public interface IDynamicBytesHandler
    {
        Task Handle(string routingKey, MemoryStream datas);
    }
}
