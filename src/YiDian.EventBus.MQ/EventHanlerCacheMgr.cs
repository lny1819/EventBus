using Autofac;
using System;
using System.Collections.Concurrent;

namespace YiDian.EventBus.MQ
{
    internal class EventHanlerCacheMgr
    {
        readonly ILifetimeScope _autofac;
        readonly string _lifeName;
        public EventHanlerCacheMgr(int length, ILifetimeScope autofac, string lifeName)
        {
            _autofac = autofac ?? throw new ArgumentNullException(nameof(ILifetimeScope));
            _lifeName = lifeName;
            CacheLength = length;
        }
        readonly ConcurrentDictionary<Type, ConcurrentStack<IBytesHandler>> dynamicDics = new ConcurrentDictionary<Type, ConcurrentStack<IBytesHandler>>();
        readonly ConcurrentDictionary<Type, ConcurrentStack<IEventHandler>> typeDics = new ConcurrentDictionary<Type, ConcurrentStack<IEventHandler>>();

        public int CacheLength { get; set; }

        /// <summary>
        /// 从缓存中获取实例
        /// </summary>
        /// <param name="type"></param>
        /// <param name="handler"></param>
        /// <param name="scope"></param>
        /// <returns>是否是从缓存中获取</returns>
        public void GetDynamicHandler(Type type, out IBytesHandler handler, out ILifetimeScope scope)
        {
            scope = null;
            if (CacheLength == 0)
            {
                scope = _autofac.BeginLifetimeScope(_lifeName);
                handler = scope.ResolveOptional(type) as IBytesHandler;
                return;
            }
            if (!dynamicDics.ContainsKey(type))
            {
                lock (dynamicDics)
                {
                    if (!dynamicDics.ContainsKey(type)) dynamicDics.TryAdd(type, new ConcurrentStack<IBytesHandler>());
                }
            }
            var stack = dynamicDics[type];
            var flag = stack.TryPop(out handler);
            if (!flag)
            {
                scope = _autofac.BeginLifetimeScope(_lifeName);
                handler = scope.ResolveOptional(type) as IBytesHandler;
            }
        }
        public void ResteDymaicHandler(IBytesHandler handler, Type type, ILifetimeScope scope)
        {
            if (CacheLength == 0)
            {
                scope?.Dispose();
                return;
            }
            var stack = dynamicDics[type];
            if (stack.Count < CacheLength) stack.Push(handler);
            else scope?.Dispose();
        }
        public void GetIIntegrationEventHandler(Type type, out IEventHandler handler, out ILifetimeScope scope)
        {
            scope = null;
            if (CacheLength == 0)
            {
                scope = _autofac.BeginLifetimeScope(_lifeName);
                handler = scope.ResolveOptional(type) as IEventHandler;
                return;
            }
            if (!typeDics.ContainsKey(type))
            {
                lock (typeDics)
                {
                    if (!typeDics.ContainsKey(type)) typeDics.TryAdd(type, new ConcurrentStack<IEventHandler>());
                }
            }
            var stack = typeDics[type];
            var flag = stack.TryPop(out handler);
            if (!flag)
            {
                scope = _autofac.BeginLifetimeScope(_lifeName);
                handler = scope.ResolveOptional(type) as IEventHandler;
            }
        }
        public void ResteTypeHandler(IEventHandler handler, Type type, ILifetimeScope scope)
        {
            if (CacheLength == 0)
            {
                scope?.Dispose();
                return;
            }
            var stack = typeDics[type];
            if (stack.Count < CacheLength) stack.Push(handler);
            else scope?.Dispose();
        }
    }
}
