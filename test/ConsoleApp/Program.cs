using System;
using System.Reflection;
using System.Threading.Tasks;

namespace ConsoleApp
{
    class Program
    {
        static void Main(string[] args)
        {
            var t = typeof(CB);
            WriteProperty(t);
            //var task = WithTask();
            //var awaiter = task.GetAwaiter();
            //awaiter.UnsafeOnCompleted(() =>
            //{
            //    var f = task.IsCompletedSuccessfully;
            //    Console.WriteLine("2");
            //});
            //Console.WriteLine("Hello World!");
            Console.ReadKey();
        }

        private static void WriteProperty(Type t)
        {
            foreach (var p in t.GetProperties())
            {
                Console.Write(p.Name);
                Console.Write(" ");
                Console.Write(p.PropertyType.Name);
                Console.WriteLine();
            }
        }

        static Task<int> WithTask()
        {
            return Task.Delay(1000).ContinueWith<int>(x =>
            {
                throw new ArgumentException();
            });
        }
    }
    [AttributeUsage(AttributeTargets.Property, Inherited = false, AllowMultiple = true)]
    sealed class MyAttribute : Attribute
    {
        // See the attribute guidelines at 
        //  http://go.microsoft.com/fwlink/?LinkId=85236
        readonly string positionalString;

        // This is a positional argument
        public MyAttribute(string positionalString)
        {
            this.positionalString = positionalString;

            // TODO: Implement code here

            throw new NotImplementedException();
        }

        public string PositionalString
        {
            get { return positionalString; }
        }

        // This is a named argument
        public int NamedInt { get; set; }
    }
    public class CA
    {
        [My("zs")]
        public string Name { get; set; }
        public int Age { get; set; }
        public EA EA { get; set; }
    }
    public class CB
    {
        public CA CA { get; set; }
        public string XA { get; set; }
        public double P { get; set; }
    }
    public enum EA
    {
        A = 2,
        B = 3
    }

}
