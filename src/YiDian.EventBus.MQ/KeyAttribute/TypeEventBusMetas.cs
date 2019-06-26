using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace YiDian.EventBus.MQ.KeyAttribute
{
    internal class EventAttrMeta
    {
        public Func<object, object> Property { get; set; }
        public int Index { get; set; }
        public string Name { get; set; }
    }
    internal class TypeEventBusMetas
    {

        static Dictionary<Type, List<EventAttrMeta>> dictionary = new Dictionary<Type, List<EventAttrMeta>>();
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
                if (!(type.GetInterfaces().Where(x => x == typeof(IMQEvent)).Count() > 0)) continue;
                var props = type.GetProperties();
                var hash = new HashSet<int>();
                foreach (var p in props)
                {
                    var att = p.GetCustomAttribute<KeyIndexAttribute>();
                    if (att != null)
                    {
                        if (hash.Contains(att.Index)) throw new ArgumentException("repeat set KeyIndexAttribute index");
                        hash.Add(att.Index);
                        AddKeyIndex(type, new EventAttrMeta() { Index = att.Index, Property = FastInvoke.EmitGetter(p), Name = p.Name });
                    }
                }
            }
        }
        static void AddKeyIndex(Type type, EventAttrMeta meta)
        {
            if (!dictionary.ContainsKey(type)) dictionary[type] = new List<EventAttrMeta>();
            dictionary[type].Add(meta);
            var list = dictionary[type];
            if (list.Count > 1)
            {
                list = list.OrderBy(e => e.Index).ToList();
            }
            dictionary[type] = list;
        }
        public static List<EventAttrMeta> GetProperties(Type type)
        {
            if (!dictionary.ContainsKey(type)) return null;
            return dictionary[type];
        }
    }
}
