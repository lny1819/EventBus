namespace YiDian.Soa.Sp
{
    public interface IAppRun
    {
        void Run(ISoaServiceHost host, string name, string[] args);
        string Name { get; }
    }
}
