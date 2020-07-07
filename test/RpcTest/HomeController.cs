//using EventModels.userinfo;
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
        //public ActionResult<RspUseAction> GetOrderAction(string orderId)
        //{
        //    return new RspUseAction()
        //    {
        //        Data = new RspUserOrderInfo() { Action = OrderActType.DELETE, Commodity = "HHI", ContractId = "2006", Exchange = "HKEX" },
        //        ErrorCode = 0
        //    };
        //}
    }
}
