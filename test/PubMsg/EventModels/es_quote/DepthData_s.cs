using System;
using System.Collections.Generic;
using YiDian.EventBus;
using YiDian.EventBus.MQ;
using YiDian.EventBus.MQ.KeyAttribute;
namespace EventModels.es_quote
{
    public partial class DepthData: IYiDianSeralize
    {
        public uint ToBytes(WriteStream stream)
        {
            uint size = 5;
            var span = stream.Advance(4);
            stream.WriteByte(4);
             size +=stream.WriteHeader(EventPropertyType.L_Date,2);
             size +=stream.WriteHeader(EventPropertyType.L_32,2);
             size +=stream.WriteHeader(EventPropertyType.L_64,53);
             size +=stream.WriteHeader(EventPropertyType.L_Str,5);
             size +=stream.WriteIndex(44);
             size +=stream.WriteDate(LocalTime);
             size +=stream.WriteIndex(60);
             size +=stream.WriteDate(InTime);
             size +=stream.WriteIndex(24);
             size +=stream.WriteInt32((int)CommodityType);
             size +=stream.WriteIndex(49);
             size +=stream.WriteInt32((int)TradingState);
             size +=stream.WriteIndex(1);
             size +=stream.WriteDouble(AskPrice1);
             size +=stream.WriteIndex(2);
             size +=stream.WriteDouble(AskPrice2);
             size +=stream.WriteIndex(3);
             size +=stream.WriteDouble(AskPrice3);
             size +=stream.WriteIndex(4);
             size +=stream.WriteDouble(AskPrice4);
             size +=stream.WriteIndex(5);
             size +=stream.WriteDouble(AskPrice5);
             size +=stream.WriteIndex(6);
             size +=stream.WriteUInt64(AskVolume1);
             size +=stream.WriteIndex(7);
             size +=stream.WriteUInt64(AskVolume2);
             size +=stream.WriteIndex(8);
             size +=stream.WriteUInt64(AskVolume3);
             size +=stream.WriteIndex(9);
             size +=stream.WriteUInt64(AskVolume4);
             size +=stream.WriteIndex(10);
             size +=stream.WriteUInt64(AskVolume5);
             size +=stream.WriteIndex(11);
             size +=stream.WriteDouble(AveragePrice);
             size +=stream.WriteIndex(12);
             size +=stream.WriteDouble(BidPrice1);
             size +=stream.WriteIndex(13);
             size +=stream.WriteDouble(BidPrice2);
             size +=stream.WriteIndex(14);
             size +=stream.WriteDouble(BidPrice3);
             size +=stream.WriteIndex(15);
             size +=stream.WriteDouble(BidPrice4);
             size +=stream.WriteIndex(16);
             size +=stream.WriteDouble(BidPrice5);
             size +=stream.WriteIndex(17);
             size +=stream.WriteUInt64(BidVolume1);
             size +=stream.WriteIndex(18);
             size +=stream.WriteUInt64(BidVolume2);
             size +=stream.WriteIndex(19);
             size +=stream.WriteUInt64(BidVolume3);
             size +=stream.WriteIndex(20);
             size +=stream.WriteUInt64(BidVolume4);
             size +=stream.WriteIndex(21);
             size +=stream.WriteUInt64(BidVolume5);
             size +=stream.WriteIndex(22);
             size +=stream.WriteDouble(ClosePrice);
             size +=stream.WriteIndex(26);
             size +=stream.WriteDouble(HighestPrice);
             size +=stream.WriteIndex(27);
             size +=stream.WriteDouble(HisHighPrice);
             size +=stream.WriteIndex(28);
             size +=stream.WriteDouble(HisLowPrice);
             size +=stream.WriteIndex(30);
             size +=stream.WriteDouble(LastPrice);
             size +=stream.WriteIndex(31);
             size +=stream.WriteUInt64(ImpliedBidQty);
             size +=stream.WriteIndex(32);
             size +=stream.WriteDouble(LowestPrice);
             size +=stream.WriteIndex(33);
             size +=stream.WriteUInt64(OpenInterest);
             size +=stream.WriteIndex(34);
             size +=stream.WriteDouble(OpenPrice);
             size +=stream.WriteIndex(35);
             size +=stream.WriteDouble(PreClosePrice);
             size +=stream.WriteIndex(36);
             size +=stream.WriteDouble(PreDelta);
             size +=stream.WriteIndex(37);
             size +=stream.WriteDouble(CurrDelta);
             size +=stream.WriteIndex(38);
             size +=stream.WriteDouble(TurnoverRate);
             size +=stream.WriteIndex(39);
             size +=stream.WriteInt64(PreOpenInterest);
             size +=stream.WriteIndex(40);
             size +=stream.WriteDouble(PreSettlementPrice);
             size +=stream.WriteIndex(41);
             size +=stream.WriteDouble(SettlementPrice);
             size +=stream.WriteIndex(42);
             size +=stream.WriteUInt64(TotalVolume);
             size +=stream.WriteIndex(43);
             size +=stream.WriteDouble(Turnover);
             size +=stream.WriteIndex(45);
             size +=stream.WriteUInt64(Volume);
             size +=stream.WriteIndex(46);
             size +=stream.WriteDouble(ImpliedAskPrice);
             size +=stream.WriteIndex(47);
             size +=stream.WriteUInt64(ImpliedAskQty);
             size +=stream.WriteIndex(48);
             size +=stream.WriteDouble(ImpliedBidPrice);
             size +=stream.WriteIndex(50);
             size +=stream.WriteUInt64(Q5DAvgQty);
             size +=stream.WriteIndex(51);
             size +=stream.WriteDouble(PERatio);
             size +=stream.WriteIndex(52);
             size +=stream.WriteUInt64(TotalValue);
             size +=stream.WriteIndex(53);
             size +=stream.WriteDouble(NegotiableValue);
             size +=stream.WriteIndex(54);
             size +=stream.WriteDouble(PositionTrend);
             size +=stream.WriteIndex(55);
             size +=stream.WriteDouble(ChangeSpeed);
             size +=stream.WriteIndex(56);
             size +=stream.WriteDouble(ChangeRate);
             size +=stream.WriteIndex(57);
             size +=stream.WriteDouble(Swing);
             size +=stream.WriteIndex(58);
             size +=stream.WriteUInt64(TotalBidQty);
             size +=stream.WriteIndex(59);
             size +=stream.WriteUInt64(TotalAskQty);
             size +=stream.WriteIndex(0);
             size +=stream.WriteString(CurrencyNo);
             size +=stream.WriteIndex(23);
             size +=stream.WriteString(CommodityNo);
             size +=stream.WriteIndex(25);
             size +=stream.WriteString(ExchangeID);
             size +=stream.WriteIndex(29);
             size +=stream.WriteString(InstrumentID);
             size +=stream.WriteIndex(61);
             size +=stream.WriteString(Source);
            BitConverter.TryWriteBytes(span, size);
            return size;
        }
        public void BytesTo(ReadStream stream)
        {
            var headers = stream.ReadHeaders();
            if (headers.TryGetValue(EventPropertyType.L_8, out byte count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    stream.Advance(1);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_Date, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 44){ LocalTime = stream.ReadDate();continue;}
                    if (index == 60){ InTime = stream.ReadDate();continue;}
                    stream.Advance(11);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_16, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    stream.Advance(2);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_32, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 24){ CommodityType = (CommodityType)stream.ReadInt32();continue;}
                    if (index == 49){ TradingState = (TradingState)stream.ReadInt32();continue;}
                    stream.Advance(4);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_64, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 1){ AskPrice1 = stream.ReadDouble();continue;}
                    if (index == 2){ AskPrice2 = stream.ReadDouble();continue;}
                    if (index == 3){ AskPrice3 = stream.ReadDouble();continue;}
                    if (index == 4){ AskPrice4 = stream.ReadDouble();continue;}
                    if (index == 5){ AskPrice5 = stream.ReadDouble();continue;}
                    if (index == 6){ AskVolume1 = stream.ReadUInt64();continue;}
                    if (index == 7){ AskVolume2 = stream.ReadUInt64();continue;}
                    if (index == 8){ AskVolume3 = stream.ReadUInt64();continue;}
                    if (index == 9){ AskVolume4 = stream.ReadUInt64();continue;}
                    if (index == 10){ AskVolume5 = stream.ReadUInt64();continue;}
                    if (index == 11){ AveragePrice = stream.ReadDouble();continue;}
                    if (index == 12){ BidPrice1 = stream.ReadDouble();continue;}
                    if (index == 13){ BidPrice2 = stream.ReadDouble();continue;}
                    if (index == 14){ BidPrice3 = stream.ReadDouble();continue;}
                    if (index == 15){ BidPrice4 = stream.ReadDouble();continue;}
                    if (index == 16){ BidPrice5 = stream.ReadDouble();continue;}
                    if (index == 17){ BidVolume1 = stream.ReadUInt64();continue;}
                    if (index == 18){ BidVolume2 = stream.ReadUInt64();continue;}
                    if (index == 19){ BidVolume3 = stream.ReadUInt64();continue;}
                    if (index == 20){ BidVolume4 = stream.ReadUInt64();continue;}
                    if (index == 21){ BidVolume5 = stream.ReadUInt64();continue;}
                    if (index == 22){ ClosePrice = stream.ReadDouble();continue;}
                    if (index == 26){ HighestPrice = stream.ReadDouble();continue;}
                    if (index == 27){ HisHighPrice = stream.ReadDouble();continue;}
                    if (index == 28){ HisLowPrice = stream.ReadDouble();continue;}
                    if (index == 30){ LastPrice = stream.ReadDouble();continue;}
                    if (index == 31){ ImpliedBidQty = stream.ReadUInt64();continue;}
                    if (index == 32){ LowestPrice = stream.ReadDouble();continue;}
                    if (index == 33){ OpenInterest = stream.ReadUInt64();continue;}
                    if (index == 34){ OpenPrice = stream.ReadDouble();continue;}
                    if (index == 35){ PreClosePrice = stream.ReadDouble();continue;}
                    if (index == 36){ PreDelta = stream.ReadDouble();continue;}
                    if (index == 37){ CurrDelta = stream.ReadDouble();continue;}
                    if (index == 38){ TurnoverRate = stream.ReadDouble();continue;}
                    if (index == 39){ PreOpenInterest = stream.ReadInt64();continue;}
                    if (index == 40){ PreSettlementPrice = stream.ReadDouble();continue;}
                    if (index == 41){ SettlementPrice = stream.ReadDouble();continue;}
                    if (index == 42){ TotalVolume = stream.ReadUInt64();continue;}
                    if (index == 43){ Turnover = stream.ReadDouble();continue;}
                    if (index == 45){ Volume = stream.ReadUInt64();continue;}
                    if (index == 46){ ImpliedAskPrice = stream.ReadDouble();continue;}
                    if (index == 47){ ImpliedAskQty = stream.ReadUInt64();continue;}
                    if (index == 48){ ImpliedBidPrice = stream.ReadDouble();continue;}
                    if (index == 50){ Q5DAvgQty = stream.ReadUInt64();continue;}
                    if (index == 51){ PERatio = stream.ReadDouble();continue;}
                    if (index == 52){ TotalValue = stream.ReadUInt64();continue;}
                    if (index == 53){ NegotiableValue = stream.ReadDouble();continue;}
                    if (index == 54){ PositionTrend = stream.ReadDouble();continue;}
                    if (index == 55){ ChangeSpeed = stream.ReadDouble();continue;}
                    if (index == 56){ ChangeRate = stream.ReadDouble();continue;}
                    if (index == 57){ Swing = stream.ReadDouble();continue;}
                    if (index == 58){ TotalBidQty = stream.ReadUInt64();continue;}
                    if (index == 59){ TotalAskQty = stream.ReadUInt64();continue;}
                    stream.Advance(8);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_Str, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    if (index == 0){ CurrencyNo = stream.ReadString();continue;}
                    if (index == 23){ CommodityNo = stream.ReadString();continue;}
                    if (index == 25){ ExchangeID = stream.ReadString();continue;}
                    if (index == 29){ InstrumentID = stream.ReadString();continue;}
                    if (index == 61){ Source = stream.ReadString();continue;}
                     var c = stream.ReadInt32();stream.Advance(c);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_Array, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    var c = stream.ReadInt32();stream.Advance(c);
                }
            }
            if (headers.TryGetValue(EventPropertyType.L_N, out count))
            {
                for (var i = 0; i < count; i++)
                {
                    var index = stream.ReadByte();
                    var l = stream.ReadInt32();
                    stream.Advance(l);
                }
            }
        }
        public uint Size()
        {
                var size=529+WriteStream.GetStringSize(CurrencyNo)+WriteStream.GetStringSize(CommodityNo)+WriteStream.GetStringSize(ExchangeID)+WriteStream.GetStringSize(InstrumentID)+WriteStream.GetStringSize(Source)+ 0;
                return size;
        }
    }
}
