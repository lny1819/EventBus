using System;
using System.Text;

namespace YiDian.EventBus.MQ
{
    public class DefaultSeralizer : IEventSeralize
    {
        static bool flag = false;
        private Encoding encoding;

        public DefaultSeralizer(Encoding encoding)
        {
            this.encoding = encoding;
        }

        public object DeserializeObject(ReadOnlyMemory<byte> data, Type type)
        {
            try
            {
                if (type.Name.ToLower() == "CommodityInfo".ToLower()) Console.Write(data.Length + " ");
                var constructor = type.GetConstructor(Type.EmptyTypes);
                var obj = constructor.Invoke(null) as IYiDianSeralize;
                if (obj == null) throw new ArgumentNullException("the type " + type.Name + " can not be convert as " + nameof(IYiDianSeralize));
                var readstream = new ReadStream(data) { Encoding = encoding };
                obj.BytesTo(ref readstream);
                return obj;
            }
            catch (Exception)
            {
                if (type.Name.ToLower() == "CommodityInfo".ToLower())
                {
                    if (!flag)
                    {
                        flag = true;
                        var datas = data.ToArray();
                        var s = Convert.ToBase64String(datas);
                        Console.WriteLine(s);
                    }
                }
                throw;
            }
        }

        public ReadOnlyMemory<byte> Serialize<T>(T @event)
        {
            return Serialize(@event, typeof(T));
        }

        public ReadOnlyMemory<byte> Serialize(object obj, Type type)
        {
            if (!(obj is IMQEvent)) throw new ArgumentException(nameof(obj), "event must instance of IMQEvent");
            if (!(obj is IYiDianSeralize seralize)) throw new ArgumentException(nameof(obj), "event must instance of IYiDianSeralize");
            var write = new WriteStream(2000) { Encoding = encoding };
            seralize.ToBytes(ref write);
            if (type.Name.ToLower() == "CommodityInfo".ToLower()) Console.Write(write.Length + " ");
            return write.GetBytes();
        }

        public int Serialize(object obj, Type type, byte[] bs, int offset)
        {
            if (!(obj is IMQEvent)) throw new ArgumentException(nameof(obj), "event must instance of IMQEvent");
            if (!(obj is IYiDianSeralize seralize)) throw new ArgumentException(nameof(obj), "event must instance of IYiDianSeralize");
            var write = new WriteStream(bs, offset) { Encoding = encoding };
            return (int)seralize.ToBytes(ref write);
        }
    }
}
