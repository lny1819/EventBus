using EventModels.depthdata;
using EventModels.es_quote;
using System.Collections.Generic;
using System.Threading.Tasks;
using YiDian.EventBus.MQ.Rpc;
using YiDian.EventBus.MQ.Rpc.Abstractions;

namespace RpcTest
{
    public class HomeController : RpcController
    {
        public async Task<ActionResult<string>> GetId(int a, int b)
        {
            await Task.Delay(20);
            return (a + b).ToString();
        }
        public ActionResult<Exchange> GetExchange()
        {
            return new Exchange()
            {
                ExchangeName = "香港恒生期货交易所",
                ExchangeNo = "HKEX"
            };
        }
        /// <summary>
        /// 获取Core
        /// </summary>
        /// <returns></returns>
        public ActionResult<CoreInfo> GetCore()
        {
            return new CoreInfo()
            {
                AccountNo = "zs"
            };
        }
        public ActionResult<List<Contract>> GetContracts()
        {
            return new List<Contract>()
            {
                new Contract(){ CommodityNo="HSI", ExchangeNo="HKEX", InstrumentID="2002"},
                new Contract(){ CommodityNo="HSI", ExchangeNo="HKEX", InstrumentID="2003"}
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
