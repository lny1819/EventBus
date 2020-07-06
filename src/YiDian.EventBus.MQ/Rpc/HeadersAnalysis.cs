using System;
using System.Collections.Generic;
using System.Text;

namespace YiDian.EventBus.MQ.Rpc
{
    public class HeadersAnalysis
    {
        readonly byte[] orginal;
        Dictionary<string, string> dic;
        int offset = 0;
        public HeadersAnalysis(byte[] datas)
        {
            orginal = datas;
            Encoding = Encoding.ASCII;
            dic = new Dictionary<string, string>();
            Do();
        }
        public Dictionary<string, string> Headers { get; }
        private void Do()
        {
            for (; ; )
            {
                if (!ReadLine(out string value))
                {
                    AnalysisStringValue(value);
                    if ((offset + 1) < orginal.Length && orginal[offset + 1] == a_r)
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
            dic.TryAdd(arr[0], arr[1]);
        }

        private void DealBodyDatas()
        {
        }

        Encoding Encoding { get; }
        const byte a_r = (byte)'\r';
        internal bool ReadLine(out string value)
        {
            value = string.Empty;
            var i = 0;
            for (; ; )
            {
                if (orginal[offset + i] == a_r)
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
            var value = Encoding.GetString(orginal, offset, i);
            offset += i + 1;
            return value;
        }
    }
}
