using System;
namespace EventModels.userinfo
{
    public enum OrderType
    {
        MARKET = 0,
        LIMIT = 1,
        STOP_MARKET = 2,
        STOP_LIMIT = 3,
        OPT_EXEC = 4,
        OPT_ABANDON = 5,
        REQQUOT = 6,
        RSPQUOT = 7,
        ICEBERG = 8,
        GHOST = 9
    }
}
