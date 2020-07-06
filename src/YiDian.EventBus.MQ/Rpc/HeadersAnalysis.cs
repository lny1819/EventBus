using System;
using System.Collections.Generic;
using System.Text;

namespace YiDian.EventBus.MQ.Rpc
{
    public enum ContentType
    {
        Json,
        YDData,
        Text
    }
    public class HeadersAnalysis
    {
        readonly ReadOnlyMemory<byte> orginal;
        readonly Dictionary<string, string> dic;
        int offset = 0;
        public HeadersAnalysis(ReadOnlyMemory<byte> datas)
        {
            orginal = datas;
            dic = new Dictionary<string, string>();
            Do();
        }
        public DateTime ClientDate { get; private set; }
        public Encoding Encoding { get; private set; }
        public ContentType ContentType { get; set; }
        public string Query { get; private set; }
        public long ContentLength { get; private set; }
        public Dictionary<string, string> Headers { get; }
        private void Do()
        {
            for (; ; )
            {
                if (!ReadLine(out string value))
                {
                    AnalysisStringValue(value);
                    if ((offset + 1) < orginal.Length && orginal.Span[offset + 1] == a_r)
                    {
                        offset += 1;
                        DealBodyDatas();
                    }
                    break;
                }
            }
        }

        private void AnalysisStringValue(string value)
        {
            var arr = value.Split(':');
            if (arr.Length != 2) throw new FormatException("数据格式错误");
            if (string.Compare("clientTime", arr[0], true) == 0)
            {
                ClientDate = DateTime.Parse(arr[1]);
            }
            else if (string.Compare("encoding", arr[0], true) == 0)
            {
                Encoding = Encoding.GetEncoding(arr[1]);
            }
            else if (string.Compare("url", arr[0], true) == 0)
            {
                var uri = new Uri(arr[1]);
                Query = uri.Query;
            }
            else if (string.Compare("content-type", arr[0], true) == 0)
            {
                if (string.Compare("json", arr[1], true) == 0)
                {
                    ContentType = ContentType.Json;
                }
                else if (string.Compare("text", arr[1], true) == 0)
                {
                    ContentType = ContentType.Text;
                }
                else if (string.Compare("yddata", arr[1], true) == 0)
                {
                    ContentType = ContentType.YDData;
                }
            }
            else if (string.Compare("content-length", arr[0], true) == 0)
            {
                ContentLength = long.Parse(arr[1]);
            }
            else dic.TryAdd(arr[0], arr[1]);
        }

        private void DealBodyDatas()
        {
        }

        const byte a_r = (byte)'\r';
        internal bool ReadLine(out string value)
        {
            value = string.Empty;
            var i = 0;
            for (; ; )
            {
                if (orginal.Span[offset + i] == a_r)
                {
                    value = ReadString(i);
                    return true;
                }
                i++;
                if ((offset + i) > orginal.Length) return false;
            }
        }

        private string ReadString(int i)
        {
            var value = Encoding.UTF8.GetString(orginal.Slice(offset, i).Span);
            offset += i + 1;
            return value;
        }
    }
}
