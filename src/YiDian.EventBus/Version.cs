using System;

namespace YiDian.EventBus
{
    public struct Version
    {
        public byte A { get; set; }
        public byte B { get; set; }

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
        public static Version Parse(ushort version)
        {
            Version v = new Version
            {
                A= (byte)(version >> 8),
                B = (byte)((version << 8) >> 8),
            };
            return v;
        }
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
        public override string ToString()
        {
            return $"{A}.{B}";
        }
        public byte[] ToBytes()
        {
            var res = new byte[4];
            res[0] = A;
            res[1] = B;
            return res;
        }
        public ushort GetValue()
        {
            return BitConverter.ToUInt16(new byte[] { B, A });
        }
        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return A ^ B;
        }

        public static bool operator ==(Version a, Version b)
        {
            return a.A == b.A && a.B == b.B;
        }
        public static bool operator !=(Version a, Version b)
        {
            return a.A != b.A || a.B != b.B;
        }
        public static bool operator >(Version a, Version b)
        {
            if (a.A > b.A) return true;
            if (a.A < b.A) return false;

            if (a.B > b.B) return true;
            if (a.B < b.B) return false;

            return false;
        }
        public static bool operator <(Version a, Version b)
        {
            if (a.A > b.A) return false;
            if (a.A < b.A) return true;

            if (a.B > b.B) return false;
            if (a.B < b.B) return true;

            return false;
        }

        public static bool operator <=(Version a, Version b)
        {
            return !(a > b);
        }
        public static bool operator >=(Version a, Version b)
        {
            return !(a < b);
        }
    }
}
