using YiDian.EventBus.Abstractions;
using Newtonsoft.Json;
using System;
using System.Text;
using YiDian.EventBus;
using System.Runtime.CompilerServices;

namespace YiDian.EventBusMQ
{
    public class MLRpcClient : IMLRpcClient
    {
        IRpcClient mqRpc;
        public string ServerName { get; }
        public MLRpcClient(string serverName, IRpcClient client)
        {
            mqRpc = client;
            ServerName = serverName.ToLower();
            Encode = Encoding.UTF8;
            //Hand();
        }
        public event Action ConnectionError;
        public Encoding Encode { get; set; }
        public bool IsConnect { get; set; }
        public ResponseBase<TOut> Call<TOut, Tin>(string uri, Tin data)
        {
            var bytes = Encode.GetBytes(ToJson(data));
            return Call<TOut>(uri, bytes);
        }
        static DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToUnixTimestamp(DateTime dateTime)
        {
            return Convert.ToInt64((dateTime.ToUniversalTime() - start).TotalSeconds);
        }
        public string ToJson(object o)
        {
            if (o == null) return "";
            string value;
            var type = o.GetType();
            if (type == typeof(String))
            {
                value = o.ToString();
            }
            else if (type.IsEnum) value = ((int)o).ToString();
            else if (type.IsValueType) value = o.ToString();
            else value = JsonConvert.SerializeObject(o);
            return value;
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
            var d = mqRpc.Request(ServerName, uri, newdata);
            if (d == null)
            {
                //Hand();
                return new ResponseBase<T>() { ServerState = -1, ServerMsg = "请求已超时" };
            }
            var res = JsonConvert.DeserializeObject(Encode.GetString(d), typeof(ResponseBase<T>));
            return res as ResponseBase<T>;
        }

        private void Hand()
        {
            if (Call<string>("_hand", new byte[0]).ServerState != 0)
            {
                ConnectionError?.Invoke();
                IsConnect = false;
            }
            else IsConnect = true;
        }
    }
}
