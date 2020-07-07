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
        int bodyoffset = 0;
        public HeadersAnalysis(ReadOnlyMemory<byte> datas)
        {
            orginal = datas;
            dic = new Dictionary<string, string>();
            Do();
        }
        public DateTime ClientDate { get; private set; }
        public Uri Url { get; private set; }
        public Encoding Encode { get; private set; }
        public ContentType ContentType { get; set; }
        public long ContentLength { get; private set; }
        public Dictionary<string, string> Headers { get; }
        private void Do()
        {
            for (; ; )
            {
                if (ReadLine(out string value))
                {
                    AnalysisStringValue(value);
                    if ((offset + 1) < orginal.Length && orginal.Span[offset] == a_r)
                    {
                        offset += 1;
                        DealBodyDatas();
                        return;
                    }
                    continue;
                }
                break;
            }
        }

        private void AnalysisStringValue(string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            var index = value.IndexOf(':');
            var tag = value.Substring(0, index);
            var text = value.Substring(index + 1);
            if (string.Compare("clientTime", tag, true) == 0)
            {
                ClientDate = DateTime.Parse(text);
            }
            else if (string.Compare("encoding", tag, true) == 0)
            {
                Encode = Encoding.GetEncoding(text);
            }
            else if (string.Compare("url", tag, true) == 0)
            {
                Url = new Uri(text);
            }
            else if (string.Compare("content-type", tag, true) == 0)
            {
                if (string.Compare("json", text, true) == 0)
                {
                    ContentType = ContentType.Json;
                }
                else if (string.Compare("text", text, true) == 0)
                {
                    ContentType = ContentType.Text;
                }
                else if (string.Compare("yddata", text, true) == 0)
                {
                    ContentType = ContentType.YDData;
                }
            }
            else dic.TryAdd(tag, text);
        }

        private void DealBodyDatas()
        {
            var length = BitConverter.ToInt32(orginal.Slice(offset, 4).Span);
            offset += 4;
            ContentLength = length;
            bodyoffset = offset;
        }
        public ReadOnlyMemory<byte> GetBodys()
        {
            return orginal.Slice(bodyoffset, (int)ContentLength);
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
