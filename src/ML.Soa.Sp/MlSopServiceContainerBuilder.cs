using System;
using System.Collections.Generic;

namespace ML.Soa.Sp
{

    public class MlSopServiceContainerBuilder : ISoaServiceContainerBuilder
    {
        Dictionary<Type, object> dic;
        Dictionary<string, string> settings;
        public MlSopServiceContainerBuilder()
        {
            dic = new Dictionary<Type, object>();
            settings = new Dictionary<string, string>();
        }
        public void Add<T>(T t) where T : class
        {
            dic[typeof(T)] = t;
        }

        public ISoaServiceHost Build(string[] args)
        {
            var host = new DefaultServiceHost(this, args);
            return host;
        }
        public T Get<T>() where T : class
        {
            var flag = dic.TryGetValue(typeof(T), out object value);
            if (flag) return value as T;
            else return default(T);
        }

        public string GetSettings(string key)
        {
            settings.TryGetValue(key, out string value);
            return value;
        }

        public void SetSettings(string key, string fullName)
        {
            settings[key] = fullName;
        }
    }
}
