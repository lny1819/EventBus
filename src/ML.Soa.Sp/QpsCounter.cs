using Microsoft.Extensions.Logging;
using ML.Fulturetrade.EventBus.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace ML.Soa.Sp
{
    /// <summary>
    /// 最多支持14个计数的计数器
    /// </summary>
    public class QpsCounter : IQpsCounter
    {
        int a, b, c, d, e, f, g, h, i, j, k, l, m, n = 0;
        int dic_index = -1;
        long ta, tb, tc, td, te, tf, tg, th, ti, tj, tk, tl, tm, tn = 0;
        readonly ConcurrentDictionary<string, int> dic_count = new ConcurrentDictionary<string, int>(14, 14);
        readonly ConcurrentDictionary<int, string> key_name = new ConcurrentDictionary<int, string>(14, 14);
        readonly ConcurrentDictionary<int, List<int>> record = new ConcurrentDictionary<int, List<int>>(14, 14);
        readonly ILogger logger;
        DateTime lastWrite = DateTime.Now;
        int logflag = 0;
        private bool _enabled = true;
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                _enabled = value;
                if (_enabled)
                {
                    if (Interlocked.Increment(ref logflag) == 1)
                    {
                        ThreadPool.UnsafeQueueUserWorkItem((obj) =>
                        {
                            Logs();
                        }, null);
                    }
                    else Interlocked.Decrement(ref logflag);
                }
                else
                {
                    for (; ; )
                    {
                        if (logflag == 0) break;
                        Thread.Yield();
                    }
                }
            }
        }

        public QpsCounter(ILogger _logger, bool enabled)
        {
            for (int x = 0; x < 14; x++)
            {
                record.TryAdd(x, new List<int>());
            }
            logger = _logger;
            Enabled = enabled;
        }

        private void Logs()
        {
            while (true)
            {
                if (!Enabled)
                {
                    Interlocked.Decrement(ref logflag);
                    return;
                }
                Thread.Sleep(1000);
                record[0].Add(Interlocked.Exchange(ref a, 0));
                record[1].Add(Interlocked.Exchange(ref b, 0));
                record[2].Add(Interlocked.Exchange(ref c, 0));
                record[3].Add(Interlocked.Exchange(ref d, 0));
                record[4].Add(Interlocked.Exchange(ref e, 0));
                record[5].Add(Interlocked.Exchange(ref f, 0));
                record[6].Add(Interlocked.Exchange(ref g, 0));
                record[7].Add(Interlocked.Exchange(ref h, 0));
                record[8].Add(Interlocked.Exchange(ref i, 0));
                record[9].Add(Interlocked.Exchange(ref j, 0));
                record[10].Add(Interlocked.Exchange(ref k, 0));
                record[11].Add(Interlocked.Exchange(ref l, 0));
                record[12].Add(Interlocked.Exchange(ref m, 0));
                record[13].Add(Interlocked.Exchange(ref n, 0));
                if ((DateTime.Now - lastWrite).TotalSeconds > 5)
                {
                    lastWrite = DateTime.Now;
                    int length = 0;
                    lock (dic_count)
                    {
                        length = dic_count.Count;
                    }
                    if (length == 0) continue;
                    var sb = new StringBuilder();
                    long bit = 0;
                    for (int x = 0; x < length; x++)
                    {
                        var avg = record[x].Average();
                        sb.Append(key_name[x]);
                        sb.Append(":qps=");
                        sb.Append(avg.ToString());
                        sb.Append(",max=");
                        sb.Append(record[x].Max().ToString());
                        sb.Append(",min=");
                        sb.Append(record[x].Min().ToString());
                        record[x].Clear();
                        switch (x)
                        {
                            case 0:
                                if (ta <= 0) break;
                                sb.Append(" total=");
                                sb.Append(ta.ToString());
                                break;
                            case 1:
                                if (tb <= 0) break;
                                sb.Append(" total=");
                                sb.Append(tb.ToString());
                                break;
                            case 2:
                                if (tb <= 0) break;
                                sb.Append(" total=");
                                sb.Append(tc.ToString());
                                break;
                            case 3:
                                if (tb <= 0) break;
                                sb.Append(" total=");
                                sb.Append(td.ToString());
                                break;
                            case 4:
                                if (tb <= 0) break;
                                sb.Append(" total=");
                                sb.Append(te.ToString());
                                break;
                            case 5:
                                if (tb <= 0) break;
                                sb.Append(" total=");
                                sb.Append(tf.ToString());
                                break;
                            case 6:
                                if (tb <= 0) break;
                                sb.Append(" total=");
                                sb.Append(tg.ToString());
                                break;
                            case 7:
                                if (tb <= 0) break;
                                sb.Append(" total=");
                                sb.Append(th.ToString());
                                break;
                            case 8:
                                if (tb <= 0) break;
                                sb.Append(" total=");
                                sb.Append(ti.ToString());
                                break;
                            case 9:
                                if (tb <= 0) break;
                                sb.Append(" total=");
                                sb.Append(tj.ToString());
                                break;
                            case 10:
                                if (tb <= 0) break;
                                sb.Append(" total=");
                                sb.Append(tk.ToString());
                                break;
                            case 11:
                                if (tb <= 0) break;
                                sb.Append(" total=");
                                sb.Append(tl.ToString());
                                break;
                            case 12:
                                if (tb <= 0) break;
                                sb.Append(" total=");
                                sb.Append(tm.ToString());
                                break;
                            case 13:
                                if (tb <= 0) break;
                                sb.Append(" total=");
                                sb.Append(tn.ToString());
                                break;
                        }
                        sb.Append(";");
                        if (avg > 0)
                        {
                            bit |= ((long)1 << x);
                        }
                    }
                    sb.Append(Environment.NewLine);
                    if (bit != 0)
                    {
                        logger.LogInformation(sb.ToString());
                    }
                }
            }
        }

        /// <summary>
        /// 给指定名称的计数器的计数+1
        /// </summary>
        /// <param name="key"></param>
        public void Add(string key)
        {
            if (!Enabled) return;
            var index = GetIndex(key);
            switch (index)
            {
                case 0:
                    Interlocked.Increment(ref a);
                    break;
                case 1:
                    Interlocked.Increment(ref b);
                    break;
                case 2:
                    Interlocked.Increment(ref c);
                    break;
                case 3:
                    Interlocked.Increment(ref d);
                    break;
                case 4:
                    Interlocked.Increment(ref e);
                    break;
                case 5:
                    Interlocked.Increment(ref f);
                    break;
                case 6:
                    Interlocked.Increment(ref g);
                    break;
                case 7:
                    Interlocked.Increment(ref h);
                    break;
                case 8:
                    Interlocked.Increment(ref i);
                    break;
                case 9:
                    Interlocked.Increment(ref j);
                    break;
                case 10:
                    Interlocked.Increment(ref k);
                    break;
                case 11:
                    Interlocked.Increment(ref l);
                    break;
                case 12:
                    Interlocked.Increment(ref m);
                    break;
                case 13:
                    Interlocked.Increment(ref n);
                    break;
            }
        }
        public void Add(string key, int length, bool writeTotal = false)
        {
            if (!Enabled) return;
            var index = GetIndex(key);
            switch (index)
            {
                case 0:
                    Interlocked.Add(ref a, length);
                    if (writeTotal) Interlocked.Add(ref ta, length);
                    break;
                case 1:
                    Interlocked.Add(ref b, length);
                    if (writeTotal) Interlocked.Add(ref tb, length);
                    break;
                case 2:
                    Interlocked.Add(ref c, length);
                    if (writeTotal) Interlocked.Add(ref tc, length);
                    break;
                case 3:
                    Interlocked.Add(ref d, length);
                    if (writeTotal) Interlocked.Add(ref td, length);
                    break;
                case 4:
                    Interlocked.Add(ref e, length);
                    if (writeTotal) Interlocked.Add(ref te, length);
                    break;
                case 5:
                    Interlocked.Add(ref f, length);
                    if (writeTotal) Interlocked.Add(ref tf, length);
                    break;
                case 6:
                    Interlocked.Add(ref g, length);
                    if (writeTotal) Interlocked.Add(ref tg, length);
                    break;
                case 7:
                    Interlocked.Add(ref h, length);
                    if (writeTotal) Interlocked.Add(ref th, length);
                    break;
                case 8:
                    Interlocked.Add(ref i, length);
                    if (writeTotal) Interlocked.Add(ref ti, length);
                    break;
                case 9:
                    Interlocked.Add(ref j, length);
                    if (writeTotal) Interlocked.Add(ref tj, length);
                    break;
                case 10:
                    Interlocked.Add(ref k, length);
                    if (writeTotal) Interlocked.Add(ref tk, length);
                    break;
                case 11:
                    Interlocked.Add(ref l, length);
                    if (writeTotal) Interlocked.Add(ref tl, length);
                    break;
                case 12:
                    Interlocked.Add(ref m, length);
                    if (writeTotal) Interlocked.Add(ref tm, length);
                    break;
                case 13:
                    Interlocked.Add(ref n, length);
                    if (writeTotal) Interlocked.Add(ref tn, length);
                    break;
            }
        }

        public void Set(string key, int length)
        {
            if (!Enabled) return;
            var index = GetIndex(key);
            switch (index)
            {
                case 0:
                    Interlocked.Exchange(ref a, length);
                    break;
                case 1:
                    Interlocked.Exchange(ref b, length);
                    break;
                case 2:
                    Interlocked.Exchange(ref c, length);
                    break;
                case 3:
                    Interlocked.Exchange(ref d, length);
                    break;
                case 4:
                    Interlocked.Exchange(ref e, length);
                    break;
                case 5:
                    Interlocked.Exchange(ref f, length);
                    break;
                case 6:
                    Interlocked.Exchange(ref g, length);
                    break;
                case 7:
                    Interlocked.Exchange(ref h, length);
                    break;
                case 8:
                    Interlocked.Exchange(ref i, length);
                    break;
                case 9:
                    Interlocked.Exchange(ref j, length);
                    break;
                case 10:
                    Interlocked.Exchange(ref k, length);
                    break;
                case 11:
                    Interlocked.Exchange(ref l, length);
                    break;
                case 12:
                    Interlocked.Exchange(ref m, length);
                    break;
                case 13:
                    Interlocked.Exchange(ref n, length);
                    break;
            }
        }
        int GetIndex(string key)
        {
            key = key.ToLower();
            var flag = dic_count.TryGetValue(key, out int index);
            if (!flag)
            {
                lock (dic_count)
                {
                    if (!dic_count.ContainsKey(key))
                    {
                        index = Interlocked.Increment(ref dic_index);
                        dic_count[key] = index;
                        key_name[index] = key;
                    }
                }
            }
            if (index >= 14) throw new ArgumentOutOfRangeException("only support less then 14 counter");
            return index;
        }
    }
}
