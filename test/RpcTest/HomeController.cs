using EventModels.depthdata;
using EventModels.es_quote;
using YiDian.EventBus.MQ.Rpc;
using YiDian.EventBus.MQ.Rpc.Abstractions;

namespace RpcTest
{
    public class HomeController : RpcController
    {
        public ActionResult<string> GetId(string a, string b)
        {
            return a + b;
        }
        public ActionResult<Exchange> GetOrderAction(string orderId)
        {
            return new Exchange()
            {
                ExchangeName = "香港恒生期货交易所",
                ExchangeNo = "HKEX"
            };
        }
        public ActionResult<Exchange> GetOrderAction2([FromBody]TradeRecord rcd)
        {
            return new Exchange()
            {
                ExchangeName = "香港恒生期货交易所",
                ExchangeNo = "HKEX"
            };
        }
    }
}
