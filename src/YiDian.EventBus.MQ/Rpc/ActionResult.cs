namespace YiDian.EventBus.MQ.Rpc
{
    /// <summary>
    /// RPC服务数据响应格式
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ActionResult<T> : ActionResult
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        public ActionResult(T t)
        {
            result = t;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="t"></param>
        public static implicit operator ActionResult<T>(T t)
        {
            return new ActionResult<T>(t);
        }
    }
    /// <summary>
    /// RPC服务数据响应格式
    /// </summary>
    public abstract class ActionResult
    {
        /// <summary>
        /// 数据
        /// </summary>
        protected object result = null;
        internal object GetResult()
        {
            return result;
        }
    }
}
