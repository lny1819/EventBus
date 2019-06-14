namespace YiDian.EventBus.Abstractions
{
    public interface IQpsCounter
    {
        void Add(string key);
        void Add(string key, int length, bool writeTotal = false);
        void Set(string key, int length);
        bool Enabled { get; set; }
    }
}
