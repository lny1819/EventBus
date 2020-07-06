using System;
using System.Text;
using System.Runtime.CompilerServices;

namespace YiDian.EventBus.MQ.Rpc
{
    public class MQRpcClient : IMQRpcClient
    {
        readonly MQRpcClientBase mqRpc;

        public MQRpcClient(string serverName, MQRpcClientBase client, int timeOut, IEventSeralize seralize = null)
        {
            this.Seralize = seralize ?? new DefaultSeralizer();
            mqRpc = client;
            TimeOut = timeOut;
            ServerId = serverName.ToLower();
            Encode = Encoding.UTF8;
        }
        public int TimeOut { get; }
        public event EventHandler ConnectionError;
        public Encoding Encode { get; set; }
        public bool IsConnect { get; set; }
        public string ServerId { get; }

        public IEventSeralize Seralize { get; }

        public ResponseBase<TOut> Call<TOut, Tin>(string uri, Tin data) where Tin : IMQEvent where TOut : IMQEvent
        {
            var bytes = Seralize.Serialize(data);
            return Call<TOut>(uri, bytes);
        }
        static readonly DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToUnixTimestamp(DateTime dateTime)
        {
            return Convert.ToInt64((dateTime.ToUniversalTime() - start).TotalSeconds);
        }
        public ResponseBase<T> Call<T>(string uri) where T : IMQEvent
        {
            return Call<T>(uri, new byte[0]);
        }
        private ResponseBase<T> Call<T>(string uri, byte[] data) where T : IMQEvent
        {
            var now = BitConverter.GetBytes(ToUnixTimestamp(DateTime.Now));
            var newdata = new byte[data.Length + now.Length];
            Buffer.BlockCopy(now, 0, newdata, 0, now.Length);
            Buffer.BlockCopy(data, 0, newdata, now.Length, data.Length);
            var task = mqRpc.Request(ServerId, uri, newdata, out _);
            var flag = task.Wait(TimeOut * 1000);
            if (!flag || !task.IsCompletedSuccessfully)
            {
                return new ResponseBase<T>() { ServerState = -1, ServerMsg = "请求已超时" };
            }
            var res = Encode.GetString(task.Result).JsonTo(typeof(ResponseBase<T>));
            return res as ResponseBase<T>;
        }
        //private async ResponseBase<T> CallAsync<T>(string uri, byte[] data)
        //{
        //    var now = BitConverter.GetBytes(ToUnixTimestamp(DateTime.Now));
        //    var newdata = new byte[data.Length + now.Length];
        //    Buffer.BlockCopy(now, 0, newdata, 0, now.Length);
        //    Buffer.BlockCopy(data, 0, newdata, now.Length, data.Length);
        //    var res = await mqRpc.Request(ServerId, uri, newdata, out _);
        //    Seralize.DeserializeObject(res, typeof())
        //}

        public void Cancel(long mid)
        {
            throw new NotImplementedException();
        }
    }
}
