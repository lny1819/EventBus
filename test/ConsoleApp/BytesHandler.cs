using System.Threading.Tasks;
using YiDian.EventBus;
using Utils;

namespace ConsoleApp
{
    internal class BytesHandler : IBytesHandler
    {
        public Task<bool> Handle(string routingKey, byte[] datas)
        {
            var info = datas.ByteToEsQuote();
            return Task.FromResult(true);
        }

    }
}