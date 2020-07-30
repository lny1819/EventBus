using System;

namespace YiDian.EventBus
{
    /// <summary>
    /// 消息管理器 APP版本
    /// </summary>
    public struct Version
    {
        /// <summary>
        /// A
        /// </summary>
        public byte A { get; set; }
        /// <summary>
        /// 
        /// </summary>
        public byte B { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public static Version Parse(string version)
        {
            Version v = new Version();
            var arr = version.Split('.');
            if (arr.Length != 2) throw new ArgumentException("version");
            if (!byte.TryParse(arr[0], out byte temp)) throw new ArgumentException("version must be number contact by '.'");
            v.A = temp;
            if (!byte.TryParse(arr[1], out temp)) throw new ArgumentException("version must be number contact by '.'");
            v.B = temp;
            return v;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="version"></param>
        /// <returns></returns>
        public static Version Parse(ushort version)
        {
            Version v = new Version
            {
                A= (byte)(version >> 8),
                B = (byte)((version << 8) >> 8),
            };
            return v;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="version"></param>
        /// <param name="v"></param>
        /// <returns></returns>
        public static bool TryParse(string version, out Version v)
        {
            v = new Version();
            var arr = version.Split('.');
            if (arr.Length != 2) return false;
            if (!byte.TryParse(arr[0], out byte temp)) return false;
            v.A = temp;
            if (!byte.TryParse(arr[1], out temp)) return false;
            v.B = temp;
            return true;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"{A}.{B}";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public byte[] ToBytes()
        {
            var res = new byte[4];
            res[0] = A;
            res[1] = B;
            return res;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public ushort GetValue()
        {
            return BitConverter.ToUInt16(new byte[] { B, A });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return A ^ B;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator ==(Version a, Version b)
        {
            return a.A == b.A && a.B == b.B;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator !=(Version a, Version b)
        {
            return a.A != b.A || a.B != b.B;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator >(Version a, Version b)
        {
            if (a.A > b.A) return true;
            if (a.A < b.A) return false;

            if (a.B > b.B) return true;
            if (a.B < b.B) return false;

            return false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator <(Version a, Version b)
        {
            if (a.A > b.A) return false;
            if (a.A < b.A) return true;

            if (a.B > b.B) return false;
            if (a.B < b.B) return true;

            return false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator <=(Version a, Version b)
        {
            return !(a > b);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool operator >=(Version a, Version b)
        {
            return !(a < b);
        }
    }
}
