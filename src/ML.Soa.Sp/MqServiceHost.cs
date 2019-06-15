namespace YiDian.Soa.Sp
{
    public static class MqServiceHost
    {
        public static ISoaServiceContainerBuilder CreateBuilder()
        {
            var builder = new MlSopServiceContainerBuilder();
            return builder;
        }
    }
}
