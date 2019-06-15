using System;
using System.Collections.Generic;
using System.Reflection;

namespace YiDian.EventBus.MQ.KeyAttribute
{
    public class TypeEventBusMetas
    {
        public Func<object, object> Property { get; set; }
        public int Index { get; set; }

        static Dictionary<Type, Tuple<string, Dictionary<string, TypeEventBusMetas>>> dictionary = new Dictionary<Type, Tuple<string, Dictionary<string, TypeEventBusMetas>>>();
        static void LoadMatchingAssemblies()
        {
            var finder = new AppDomainTypeFinder();
            var dir = AppDomain.CurrentDomain.BaseDirectory;
            var assemblys = finder.LoadMatchingAssemblies(dir);
            foreach (Assembly assembly in assemblys)
            {
                GetMQEvent(assembly);
            }
        }

        static TypeEventBusMetas()
        {
            LoadMatchingAssemblies();
        }
        static void GetMQEvent(Assembly assembly)
        {
            var types = assembly.GetTypes();
            foreach (var type in types)
            {
                if (!type.IsSubclassOf(typeof(IntegrationMQEvent))) continue;
                var keynameatt = type.GetCustomAttribute<KeyNameAttribute>();
                if (keynameatt != null) AddKeyName(type, keynameatt.Name);
                var props = type.GetProperties();
                var hash = new HashSet<int>();
                foreach (var p in props)
                {
                    var att = p.GetCustomAttribute<KeyIndexAttribute>();
                    if (att != null)
                    {
                        if (hash.Contains(att.Index)) throw new ArgumentException("索引重复" + nameof(Index));
                        hash.Add(att.Index);
                        AddKeyIndex(type, p.Name, new TypeEventBusMetas() { Index = att.Index, Property = FastInvoke.EmitGetter(p) });
                    }
                }
            }
        }
        static void AddKeyName(Type type, string name)
        {
            if (!dictionary.ContainsKey(type)) dictionary[type] = new Tuple<string, Dictionary<string, TypeEventBusMetas>>(name, new Dictionary<string, TypeEventBusMetas>());
        }
        static void AddKeyIndex(Type type, string name, TypeEventBusMetas meta)
        {
            if (!dictionary.ContainsKey(type)) dictionary[type] = new Tuple<string, Dictionary<string, TypeEventBusMetas>>("", new Dictionary<string, TypeEventBusMetas>());
            var dic = dictionary[type].Item2;
            dic.Add(name, meta);
        }
        public static Dictionary<string, TypeEventBusMetas> GetKeys(Type type, out string keyname)
        {
            keyname = "";
            var flag = dictionary.TryGetValue(type, out Tuple<string, Dictionary<string, TypeEventBusMetas>> res);
            if (flag)
            {
                keyname = res.Item1;
                return res.Item2;
            }
            return null;
        }
    }
}
