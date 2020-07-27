using System;
namespace EventModels.depthdata
{
    public enum CommodityType
    {
        NONE = 0,
        SPOT = 1,
        FUTURES = 2,
        OPTION = 3,
        SPREAD_MONTH = 4,
        SPREAD_COMMODITY = 5,
        BUL = 6,
        BER = 7,
        STD = 8,
        STG = 9,
        PRT = 10,
        DIRECTFOREX = 11,
        INDIRECTFOREX = 12,
        CROSSFOREX = 13,
        INDEX = 14,
        STOCK = 15
    }
}
