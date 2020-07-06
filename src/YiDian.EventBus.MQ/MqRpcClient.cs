using System;
using System.Text;
using System.Runtime.CompilerServices;

namespace YiDian.EventBus.MQ
{
    public class MQRpcClient : IMQRpcClient
    {
        readonly MQRpcClientBase mqRpc;
        public string ServerName { get; }
        public MQRpcClient(string serverName, MQRpcClientBase client, int timeOut)
        {
            mqRpc = client;
            TimeOut = timeOut;
            ServerName = serverName.ToLower();
            Encode = Encoding.UTF8;
        }
        public int TimeOut { get; }
        public event EventHandler ConnectionError;
        public Encoding Encode { get; set; }
        public bool IsConnect { get; set; }
        public ResponseBase<TOut> Call<TOut, Tin>(string uri, Tin data)
        {
            var bytes = Encode.GetBytes(data.ToJson());
            return Call<TOut>(uri, bytes);
        }
        static readonly DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToUnixTimestamp(DateTime dateTime)
        {
            return Convert.ToInt64((dateTime.ToUniversalTime() - start).TotalSeconds);
        }
        public ResponseBase<T> Call<T>(string uri)
        {
            return Call<T>(uri, new byte[0]);
        }
        private ResponseBase<T> Call<T>(string uri, byte[] data)
        {
            var now = BitConverter.GetBytes(ToUnixTimestamp(DateTime.Now));
            var newdata = new byte[data.Length + now.Length];
            Buffer.BlockCopy(now, 0, newdata, 0, now.Length);
            Buffer.BlockCopy(data, 0, newdata, now.Length, data.Length);
            var task = mqRpc.Request(ServerName, uri, newdata);
            var flag = task.Wait(TimeOut * 1000);
            if (!flag || !task.IsCompletedSuccessfully)
            {
                return new ResponseBase<T>() { ServerState = -1, ServerMsg = "请求已超时" };
            }
            var res = Encode.GetString(task.Result).JsonTo(typeof(ResponseBase<T>));
            return res as ResponseBase<T>;
        }
    }
}
