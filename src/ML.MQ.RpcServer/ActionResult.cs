namespace ML.MQ.RpcServer
{
    public class ActionResult<T> : ActionResult
    {
        public ActionResult(T t)
        {
            result = t;
        }
        public static implicit operator ActionResult<T>(T t)
        {
            return new ActionResult<T>(t);
        }
    }
    public abstract class ActionResult
    {
        protected object result = null;
        internal object GetResult()
        {
            return result;
        }
    }
}
