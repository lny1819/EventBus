using System;
using System.Collections.Concurrent;
using System.Linq;

namespace YiDian.EventBus.MQ.KeyAttribute
{
    public class EntitysMetas
    {
        static readonly ConcurrentDictionary<Type, ConcurrentDictionary<string, Tuple<Func<object, object>, SetValueDelegate>>> metas = new ConcurrentDictionary<Type, ConcurrentDictionary<string, Tuple<Func<object, object>, SetValueDelegate>>>();

        public static ConcurrentDictionary<string, Tuple<Func<object, object>, SetValueDelegate>> GetTypeGetterSetter(Type type)
        {
            ConcurrentDictionary<string, Tuple<Func<object, object>, SetValueDelegate>> value = null;
            if (!metas.ContainsKey(type))
            {
                lock (metas)
                {
                    if (metas.ContainsKey(type)) value = metas[type];
                    else
                    {
                        value = new ConcurrentDictionary<string, Tuple<Func<object, object>, SetValueDelegate>>();
                        metas.TryAdd(type, value);
                    }
                }
            }
            else value = metas[type];
            if (value.Count > 0) return value;
            lock (value)
            {
                if (value.Count > 0) return value;
                type.GetProperties().ToList().ForEach(e =>
                {
                    var setter = CreatePropertySetter(e);
                    var getter = EmitGetter(e);
                    value.TryAdd(e.Name, new Tuple<Func<object, object>, SetValueDelegate>(getter, setter));
                });
            }
            return value;
        }
    }
}
