namespace YiDian.EventBus.MQ
{
    public class ActionResult<T> : ActionResult where T : IMQEvent
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
