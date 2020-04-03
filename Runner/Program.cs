using System;
using System.Text;
using AleXr64.FastHttpPostReceiver;

namespace Runner
{
    class Program
    {
        static void Main(string[] args)
        {
            var l = new HttpPostListener(9090);
            l.OnDataReceived += L_OnDataReceived;
            l.Start();

            var key = Console.ReadKey();
            while(key.Key != ConsoleKey.Spacebar)
            {
                key = Console.ReadKey();
            }

            l.Stop();
        }

        private static void L_OnDataReceived(HttpPostData data)
        {
            Console.WriteLine($"Query string: {data.Query}");
            foreach(var header in data.Headers)
            {
                Console.WriteLine($"Header: {header.Name} Value: {header.Value}");
            }
            
            Console.WriteLine($"Data: {Encoding.UTF8.GetString(data.Message)}");
            Console.WriteLine();
        }
    }
}
