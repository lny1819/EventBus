namespace YiDian.EventBus
{
    public abstract class IntegrationMQEvent
    {
        public IntegrationMQEvent()
        {

        }
        /// <summary>
        /// 错误代码  0表示成功 其他参考ErrorCode下类型
        /// </summary>
        public int ErrorCode { get; set; }
        public string ErrorMsg { get; set; }
    }
}
