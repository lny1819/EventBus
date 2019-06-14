namespace YiDian.EventBus.Abstractions
{
    public class KeySubHandler<T>
             where T : IntegrationMQEvent
    {
        SubManagerA _mgr;
        readonly string _key;
        public KeySubHandler(string key, SubManagerA mgr)
        {
            _key = key;
            _mgr = mgr;
        }
        public void Add<TD, TH>()
          where TD : T
          where TH : IIntegrationEventHandler<TD>
        {
            _mgr.AddSubscription<TD, TH>(_key);
        }
        public void RemoveHandler()
        {
            _mgr.RemoveHandlers(_key);
        }
    }
}
