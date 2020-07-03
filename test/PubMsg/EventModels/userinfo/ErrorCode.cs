using System;
namespace EventModels.userinfo
{
    public enum ErrorCode
    {
        Success = 0,
        CanotFindOrder = 1000,
        NotFillOverTime = 1001,
        NotInTradeTime = 1102,
        NotAllowContract = 1103,
        NotAllowInForceCover = 1104,
        EmptyOrderNo = 1105,
        InsufficientFunds = 1199,
        UpStreamCheck = 1200,
        CanotFindFundAccount = 5000,
        UnKnowError = -1
    }
}
