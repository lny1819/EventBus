using System;
namespace EventModels.userinfo
{
    public enum OrderState
    {
        SUBMIT = 0,
        QUEUED = 4,
        PARTFINISHED = 5,
        FINISHED = 6,
        CANCELING = 7,
        CANCELED = 9,
        LEFTDELETED = 10,
        FAIL = 11,
        DeleteExpired = 12,
        UnKnow = 999,
        Init = -2,
        Accept = -1
    }
}
