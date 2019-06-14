

namespace YiDian.EventBus
{
    public delegate object FastInvokeHandler(object target, object[] paramters);
    public delegate void SetValueDelegate(object target, object arg);
    public delegate void VoidMethodExecutor(object target, object[] parameters);
}
