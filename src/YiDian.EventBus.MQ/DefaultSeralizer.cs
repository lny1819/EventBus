using System;
using System.Collections.Generic;
using System.Text;

namespace YiDian.EventBus.MQ
{
    public class DefaultSeralizer : IEventSeralize
    {
        private Encoding encoding;

        public DefaultSeralizer(Encoding encoding)
        {
            this.encoding = encoding;
        }

        public object DeserializeObject(ReadOnlyMemory<byte> data, Type type)
        {
            bool isArray = false;
            var ilist = type.GetInterface(typeof(IList<>).FullName);
            if (ilist != null)
            {
                isArray = true;
                type = ilist.GetGenericArguments()[0];
            }
            if (data.Length == 0)
            {
                if (!isArray && type.IsValueType) return 0;
                return null;
            }
            var stream = new ReadStream(data) { Encoding = encoding };
            if (!isArray)
            {
                if (type.IsValueType)
                {
                    if (type == typeof(byte)) return stream.ReadByte();
                    else if (type == typeof(short)) return stream.ReadInt16();
                    else if (type == typeof(ushort)) return stream.ReadUInt16();
                    else if (type == typeof(int)) return stream.ReadInt32();
                    else if (type == typeof(uint)) return stream.ReadUInt32();
                    else if (type == typeof(long)) return stream.ReadInt64();
                    else if (type == typeof(ulong)) return stream.ReadUInt64();
                    else if (type == typeof(double)) return stream.ReadDouble();
                    else if (type == typeof(DateTime)) return stream.ReadDate();
                    else if (type == typeof(bool)) return stream.ReadByte() == 1;
                    else throw new NotImplementedException();
                }
                else if (type == typeof(string)) return stream.ReadString();
                else
                {
                    if (type.GetInterface(typeof(IYiDianSeralize).FullName) == null)
                        throw new ArgumentException(nameof(type), "event type must instance of IYiDianSeralize");
                    return stream.ReadEventObj(type);
                }
            }
            if (type.IsValueType)
            {
                if (type == typeof(byte)) return stream.ReadArrayByte().ToArray();
                else if (type == typeof(short)) return stream.ReadArrayInt16();
                else if (type == typeof(ushort)) return stream.ReadArrayUInt16();
                else if (type == typeof(int)) return stream.ReadArrayInt32();
                else if (type == typeof(uint)) return stream.ReadArrayUInt32();
                else if (type == typeof(long)) return stream.ReadArrayInt64();
                else if (type == typeof(ulong)) return stream.ReadArrayUInt64();
                else if (type == typeof(double)) return stream.ReadArrayDouble();
                else if (type == typeof(DateTime)) return stream.ReadArrayDate();
                else if (type == typeof(bool)) return stream.ReadArrayBool();
                else throw new NotImplementedException();
            }
            else if (type == typeof(string)) return stream.ReadArrayString();
            else return stream.ReadArray(type);
        }

        public ReadOnlyMemory<byte> Serialize<T>(T @event)
        {
            return Serialize(@event, typeof(T));
        }

        public ReadOnlyMemory<byte> Serialize(object obj, Type type)
        {
            return SerializeInternal(obj, type, new byte[2000], 0).GetBytes();
        }
        public int Serialize(object obj, Type type, byte[] bs, int offset)
        {
            return SerializeInternal(obj, type, bs, offset).Length;
        }
        WriteStream SerializeInternal(object obj, Type type, byte[] bs, int offset)
        {
            bool isArray = false;
            var ilist = type.GetInterface(typeof(IList<>).FullName);
            if (ilist != null)
            {
                isArray = true;
                type = ilist.GetGenericArguments()[0];
            }
            var stream = new WriteStream(bs, offset) { Encoding = encoding };
            var span = stream.Advance(4);
            if (!isArray)
            {
                if (type.IsValueType)
                {
                    if (type == typeof(byte)) stream.WriteByte((byte)obj);
                    else if (type == typeof(short)) stream.WriteInt16((short)obj);
                    else if (type == typeof(ushort)) stream.WriteUInt16((ushort)obj);
                    else if (type == typeof(int)) stream.WriteInt32((int)obj);
                    else if (type == typeof(uint)) stream.WriteUInt32((uint)obj);
                    else if (type == typeof(long)) stream.WriteInt64((long)obj);
                    else if (type == typeof(ulong)) stream.WriteUInt64((ulong)obj);
                    else if (type == typeof(double)) stream.WriteDouble((double)obj);
                    else if (type == typeof(DateTime)) stream.WriteDate((DateTime)obj);
                    else if (type == typeof(bool)) stream.WriteByte(((bool)obj) ? (byte)1 : (byte)0);
                    else throw new NotImplementedException();
                }
                else if (type == typeof(string)) stream.WriteString((string)obj);
                else
                {
                    if (type.GetInterface(typeof(IYiDianSeralize).FullName) == null)
                        throw new ArgumentException(nameof(obj), "event must instance of IYiDianSeralize");
                    var seralize = (IYiDianSeralize)obj;
                    seralize.ToBytes(stream);
                }
                BitConverter.TryWriteBytes(span, stream.Length);
                return stream;
            }
            if (type.IsValueType)
            {
                if (type == typeof(byte)) stream.WriteArrayByte((byte[])obj);
                else if (type == typeof(short)) stream.WriteArrayInt16((IEnumerable<short>)obj);
                else if (type == typeof(ushort)) stream.WriteArrayUInt16((IEnumerable<ushort>)obj);
                else if (type == typeof(int)) stream.WriteArrayInt32((IEnumerable<int>)obj);
                else if (type == typeof(uint)) stream.WriteArrayUInt32((IEnumerable<uint>)obj);
                else if (type == typeof(long)) stream.WriteArrayInt64((IEnumerable<long>)obj);
                else if (type == typeof(ulong)) stream.WriteArrayUInt64((IEnumerable<ulong>)obj);
                else if (type == typeof(double)) stream.WriteArrayDouble((IEnumerable<double>)obj);
                else if (type == typeof(DateTime)) stream.WriteArrayDate((IEnumerable<DateTime>)obj);
                else if (type == typeof(bool)) stream.WriteArrayBool((IEnumerable<bool>)obj);
                else throw new NotImplementedException();
            }
            else if (type == typeof(string)) stream.WriteArrayString((IEnumerable<string>)obj);
            else stream.WriteEventArray((IEnumerable<IYiDianSeralize>)obj);
            BitConverter.TryWriteBytes(span, stream.Length);
            return stream;
        }
    }
}
