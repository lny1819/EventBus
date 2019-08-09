using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ConsoleApp
{
    public class DictTest
    {
        static readonly Dictionary<ContractIdKey, string> dic = new Dictionary<ContractIdKey, string>();
        public static void Test()
        {
            A("hkex", "hsi", "1908");
        }
        public static void A(string a, string b, string c)
        {
            var key = new ContractIdKey(a, b, c);
            dic.Add(new ContractIdKey(a, b, c), a + b + c);
            var loop = 1000000;
            string s_key = "";
            var watch = Stopwatch.StartNew();
            for (var i = 0; i < loop; i++)
            {
                dic.TryGetValue(key, out s_key);
            }
            watch.Stop();
            Console.WriteLine(s_key + " : " + watch.ElapsedMilliseconds.ToString());

            Console.ReadKey();

            watch.Restart();
            for (var i = 0; i < loop; i++)
            {
                s_key = a + b + c;
            }
            watch.Stop();
            Console.WriteLine(s_key + " : " + watch.ElapsedMilliseconds.ToString());
        }
    }
    public struct ContractIdKey
    {
        public string exchange;
        public string commodity;
        public string contractid;
        public ContractIdKey(string exchange, string commodity, string contractid)
        {
            this.exchange = exchange;
            this.commodity = commodity;
            this.contractid = contractid;
        }

        internal static readonly _Compare Compare;
        static ContractIdKey()
        {
            Compare = new _Compare();
        }
        internal class _Compare : IEqualityComparer<ContractIdKey>
        {
            public bool Equals(ContractIdKey x, ContractIdKey y)
            {
                return string.Compare(x.commodity, y.commodity, true) == 0 && string.Compare(x.exchange, y.exchange, true) == 0 && string.Compare(x.contractid, y.contractid, true) == 0;
            }
            public int GetHashCode(ContractIdKey obj)
            {
                return 0;
            }
        }
    }
}
