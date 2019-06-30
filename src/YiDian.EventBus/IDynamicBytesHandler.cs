using System.Threading.Tasks;

namespace YiDian.EventBus
{
    public interface IBytesHandler
    {
        ValueTask<bool> Handle(string routingKey, byte[] datas);
    }
}
