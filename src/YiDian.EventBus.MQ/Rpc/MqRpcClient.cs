﻿using System;
using System.Text;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace YiDian.EventBus.MQ.Rpc
{
    public class MQRpcClient : IMQRpcClient
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
        public event EventHandler ConnectionError;
        public Encoding Encode { get; set; }
        public bool IsConnect { get; set; }
        public string ServerId { get; }


        static readonly DateTime start = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ToUnixTimestamp(DateTime dateTime)
        {
            return Convert.ToInt64((dateTime.ToUniversalTime() - start).TotalSeconds);
        }
        public ResponseBase<TOut> Call<TOut, Tin>(string uri, Tin data, ContentType type)
        {
            var datas = new byte[2000];
            var write = new RPCWrite(datas);
            var url = "url:" + "rpc://" + ServerId + uri;
            write.WriteString(url);
            var now = "clientTime:" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
            write.WriteString(now);
            var encoding = "encoding:" + Encode.WebName;
            write.WriteString(encoding);
            var contenttype = "content-type:" + GetContentTypeName(type);
            write.WriteString(contenttype);
            write.WriteContent(type, data, typeof(Tin), Encode);
            var task = mqRpc.Request(ServerId, write.GetDatas(), out _);
            var flag = task.Wait(TimeOut * 1000);
            if (!flag || !task.IsCompletedSuccessfully)
            {
                return new ResponseBase<TOut>() { ServerState = -1, ServerMsg = "请求已超时" };
            }
            var res = Encode.GetString(task.Result.Span).JsonTo(typeof(ResponseBase<TOut>));
            return res as ResponseBase<TOut>;
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
