using System;
using System.Linq.Expressions;

namespace YiDian.EventBus
{
    /// <summary>
    /// 订阅方法 详细说明见具体的EventBus接口
    /// </summary>
    public class FanoutSubscriber : Subscriber<IFanoutEventBus>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventBus"></param>
        /// <param name="name"></param>
        public FanoutSubscriber(IFanoutEventBus eventBus, string name) : base(eventBus, name)
        {

        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TH"></typeparam>
        public void Subscribe<T, TH>()
                            where T : IMQEvent
                            where TH : IEventHandler<T>
        {
            __eventBus.Subscribe<T, TH>(__name);
        }
    }
    /// <summary>
    ///  订阅方法 详细说明见具体的EventBus接口
    /// </summary>
    public class TopicSubscriber : Subscriber<ITopicEventBus>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventBus"></param>
        /// <param name="name"></param>
        public TopicSubscriber(ITopicEventBus eventBus, string name) : base(eventBus, name)
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TH"></typeparam>
        /// <param name="where"></param>
        public void Subscribe<T, TH>(Expression<Func<T, bool>> where)
             where T : IMQEvent
             where TH : IEventHandler<T>
        {
            __eventBus.Subscribe<T, TH>(__name, where);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TH"></typeparam>
        public void Subscribe<T, TH>()
          where T : IMQEvent
          where TH : IEventHandler<T>
        {
            __eventBus.Subscribe<T, TH>(__name);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TH"></typeparam>
        /// <param name="subkey"></param>
        public void Subscribe<T, TH>(string subkey)
            where T : IMQEvent
            where TH : IEventHandler<T>
        {
            __eventBus.Subscribe<T, TH>(__name, subkey);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TH"></typeparam>
        /// <param name="subkey"></param>
        public void SubscribeBytes<TH>(string subkey)
            where TH : IBytesHandler
        {
            __eventBus.SubscribeBytes<TH>(__name, subkey);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TH"></typeparam>
        public void SubscribeBytes<TH>()
        where TH : IBytesHandler
        {
            __eventBus.SubscribeBytes<TH>(__name);
        }
    }
    /// <summary>
    ///  订阅方法 详细说明见具体的EventBus接口
    /// </summary>
    public class DirectSubscriber : Subscriber<IDirectEventBus>
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="eventBus"></param>
        /// <param name="name"></param>
        public DirectSubscriber(IDirectEventBus eventBus, string name) : base(eventBus, name)
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TH"></typeparam>
        public void Subscribe<T, TH>()
            where T : IMQEvent
            where TH : IEventHandler<T>
        {
            __eventBus.Subscribe<T, TH>(__name);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TH"></typeparam>
        /// <param name="key"></param>
        public void Subscribe<T, TH>(string key)
        where T : IMQEvent
        where TH : IEventHandler<T>
        {
            __eventBus.Subscribe<T, TH>(__name, key);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TH"></typeparam>
        /// <param name="key"></param>
        public void SubscribeBytes<TH>(string key)
            where TH : IBytesHandler
        {
            __eventBus.SubscribeBytes<TH>(__name, key);
        }
    }
    /// <summary>
    /// EventBus订阅类
    /// </summary>
    /// <typeparam name="TEventBus">消息总线类型</typeparam>
    public abstract class Subscriber<TEventBus> where TEventBus : IEventBus
    {
        /// <summary>
        /// 消息总线
        /// </summary>
        readonly protected TEventBus __eventBus;
        /// <summary>
        /// 队列名称
        /// </summary>
        readonly protected string __name;
        /// <summary>
        /// 创建一个EventBus订阅类实例
        /// </summary>
        /// <param name="eventBus">消息总线</param>
        /// <param name="name">队列名称</param>
        public Subscriber(TEventBus eventBus, string name)
        {
            __name = name;
            __eventBus = eventBus;
        }
    }
}
