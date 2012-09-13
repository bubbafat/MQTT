using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Threading.Tasks;
using MQTT.Types;
using System.Threading;
using MQTT.Domain;
using MQTT.Commands;
using MQTT.Client;

namespace MQTT.ConsoleApp
{
    class Program
    {
        static string server = "localhost"; // "test.mosquitto.org"
        static int port = 1883;

        static void Main(string[] args)
        {
            Factory.Initialize(
                new Dictionary<Type, Type>
                {
                    { typeof(IMqttBroker), typeof(MqttNetworkBroker) },
                });

            Thread listener = new Thread(() =>
                {
                    ThreadAction();
                });

            Thread writer = new Thread(() =>
                {
                    WriterAction();
                });

            Console.WriteLine("STARTING...");

            writer.Start();
            listener.Start();

            writer.Join();
            listener.Join();

            Console.WriteLine("DONE");
        }

        private static void WriterAction()
        {
            using (MQTT.Client.Client c = new MQTT.Client.Client("writer"))
            {
                c.OnUnsolicitedMessage += new UnsolicitedMessageCallback(c_OnUnsolicitedMessage);

                IPAddress address = Dns.GetHostAddresses(server).Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).First();
                IPEndPoint test = new IPEndPoint(address, port);

                DemandWorked(c.Connect(test));

                while (true)
                {
                    Thread.Sleep(5000);
                    c.Publish("root", "This is the message!", QualityOfService.AtMostOnce, null);
                }

                c.Disconnect(TimeSpan.FromSeconds(5));
            }
        }

        private static void ThreadAction()
        {
            using (MQTT.Client.Client c = new MQTT.Client.Client("listener"))
            {
                c.OnUnsolicitedMessage += new UnsolicitedMessageCallback(c_OnUnsolicitedMessage);

                IPAddress address = Dns.GetHostAddresses(server).Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).First();
                IPEndPoint test = new IPEndPoint(address, port);

                DemandWorked(c.Connect(test));
                DemandWorked(c.Subscribe(
                    new Subscription[] 
                    {
                        new Subscription("#", QualityOfService.ExactlyOnce), 
                    }));

                while (true)
                {
                    Thread.Sleep(1000);
                }

                c.Disconnect(TimeSpan.FromSeconds(5));
            }
        }

        static void c_OnUnsolicitedMessage(object sender, ClientCommandEventArgs e)
        {
            MqttCommand command = e.Command;

            Publish p = command as Publish;
            if (p != null)
            {
                if (p.Header.Duplicate)
                {
                    Console.WriteLine("!!! DUPLICATE !!!");
                }

                Console.WriteLine("{0}", p.Topic);
                foreach (char c in Encoding.ASCII.GetChars(p.Message))
                {
                    if (!char.IsControl(c))
                    {
                        Console.Write(c);
                    }
                }

                Console.WriteLine();
            }
        }

        static void DemandWorked(Task task)
        {
            task.Wait();
            if (task.IsFaulted)
            {
                Console.WriteLine(task.Exception.ToString());
                throw task.Exception;
            }

            if (!task.IsCompleted)
            {
                Console.WriteLine("Task did not complete?  WTF???");
                throw new Exception("WTF?");
            }
        }
    }
}
