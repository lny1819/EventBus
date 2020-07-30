using System;
using System.Text;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace YiDian.EventBus.MQ.Rpc
{
    internal class MQRpcClient : IMQRpcClient
    {
        readonly MQRpcClientBase mqRpc;

        public MQRpcClient(string serverName, MQRpcClientBase client, int timeOut)
        {
            mqRpc = client;
            TimeOut = timeOut;
            ServerId = serverName.ToLower();
            Encode = Encoding.UTF8;
        }
        public int TimeOut { get; }
        public Encoding Encode { get; set; }
        public bool IsConnect { get; set; }
        public string ServerId { get; }

        public bool IsConnected => mqRpc.IsConnnected;

        static readonly DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToUnixTimestamp(DateTime dateTime)
        {
            return Convert.ToInt64((dateTime.ToUniversalTime() - start).TotalSeconds);
        }
        public ResponseBase<TOut> Call<TOut>(string uri)
        {
            return Call<string, TOut>(uri, "", ContentType.Text);
        }
        public ResponseBase<TOut> Call<Tin, TOut>(string uri, Tin data, ContentType type)
        {
            var task = CallAsync<Tin, TOut>(uri, data, type);
            task.Wait();
            if (task.IsCompletedSuccessfully)
                return task.Result;
            return new ResponseBase<TOut>()
            {
                ServerState = -1,
                ServerMsg = task.IsFaulted ? task.Exception.Message : "未知错误"
            };
        }
        public Task<ResponseBase<TOut>> CallAsync<TOut>(string uri)
        {
            return CallAsync<string, TOut>(uri, "", ContentType.Text);
        }
        public async Task<ResponseBase<TOut>> CallAsync<Tin, TOut>(string uri, Tin data, ContentType type)
        {
            var datas = new byte[2000];
            var write = new RPCWrite(datas, Encode);
            var encoding = "encoding:" + Encode.WebName;
            write.WriteString(encoding);
            var url = "url:" + "rpc://" + ServerId + uri;
            write.WriteString(url);
            var now = "clientTime:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            write.WriteString(now);
            var contenttype = "content-type:" + GetContentTypeName(type);
            write.WriteString(contenttype);
            write.WriteContent(type, data, typeof(Tin));
            var t1 = mqRpc.Request(ServerId, write.GetDatas(), out _);
            var t2 = Task.Delay(TimeOut * 1000);
            await Task.WhenAny(new Task[] { t1, t2 });
            if (!t1.IsCompletedSuccessfully) return new ResponseBase<TOut>() { ServerState = -1, ServerMsg = "请求超时" };
            var rsp = t1.Result;
            var analysis = new HeadersAnalysis(rsp);
            var seralize = CreateSeralize(analysis.ContentType, analysis.Encode);
            var res = seralize.DeserializeObject(analysis.GetBodys(), typeof(TOut));
            var state = int.Parse(analysis.Headers["state"]);
            var msg = analysis.Headers["msg"];
            if (state == 200) return new ResponseBase<TOut> { ServerState = state, ServerMsg = msg, Data = (TOut)res };
            return new ResponseBase<TOut> { ServerState = state, ServerMsg = msg, Data = default };
        }
        private IEventSeralize CreateSeralize(ContentType contentType, Encoding encoding)
        {
            switch (contentType)
            {
                case ContentType.Text:
                    return new TextSeralize(encoding);
                case ContentType.Json:
                    return new JsonSerializer(encoding);
                default:
                    return new DefaultYDSeralizer(encoding);
            }
        }
        private string GetContentTypeName(ContentType type)
        {
            switch (type)
            {
                case ContentType.Json:
                    return "json";
                case ContentType.Text:
                    return "text";
                default:
                    return "yddata";
            }
        }

        public void Cancel(long mid)
        {
            throw new NotImplementedException();
        }
    }
}
