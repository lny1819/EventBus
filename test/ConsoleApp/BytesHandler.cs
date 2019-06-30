using System.Threading.Tasks;
using YiDian.EventBus;
using Utils;

namespace ConsoleApp
{
    internal class BytesHandler : IBytesHandler
    {
        public ValueTask<bool> Handle(string routingKey, byte[] datas)
        {
            var info = datas.ByteToEsQuote();
            return new ValueTask<bool>(Task.FromResult(true));
        }

    }
}