namespace ML.Soa.Sp
{

    public interface ISoaServiceContainerBuilder
    {
        void Add<T>(T t) where T : class;
        T Get<T>() where T : class;
        string GetSettings(string key);
        void SetSettings(string key, string fullName);
        ISoaServiceHost Build(string[] args = null);
    }
}
