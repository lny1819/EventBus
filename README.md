# 仅提供.NET Core版本
基于RabbitMq实现，为用户提供简单，可靠，形式丰富的消息发布与订阅功能。
#通过依赖注入轻松使用，可定制的消息序列化方式

  public void ConfigService(SoaServiceContainerBuilder soa)
  
  {
  
      soa.UseRabbitMq(Configuration["mqconnstr"], Configuration["eventImsApi"])
      
           .UseMqRpcClient(Configuration["sysname"])
           
           .UseDirectEventBus()
           
           .UseFanoutEventBus()
           
           .UseTopicEventBus();
           
  }
  
  ...UseDirectEventBus(this SoaServiceContainerBuilder builder, int cacheLength, IEventSeralize seralizer = null, string broker_name = "");
  
  ...UseFanoutEventBus(this SoaServiceContainerBuilder builder, int cacheLength, IEventSeralize seralizer = null, string broker_name = "");
