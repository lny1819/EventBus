

namespace YiDian.EventBus
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="target"></param>
    /// <param name="paramters"></param>
    /// <returns></returns>
    public delegate object FastInvokeHandler(object target, object[] paramters);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="target"></param>
    /// <param name="arg"></param>
    public delegate void SetValueDelegate(object target, object arg);
    /// <summary>
    /// 
    /// </summary>
    /// <param name="target"></param>
    /// <param name="parameters"></param>
    public delegate void VoidMethodExecutor(object target, object[] parameters);
}
