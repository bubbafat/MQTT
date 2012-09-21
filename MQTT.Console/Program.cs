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
            OldFactory.Initialize(
                new Dictionary<Type, Type>
                {
                    { typeof(IMqttBroker), typeof(MqttNetworkBroker) },
                });

            Thread[] listeners = new Thread[2];

            for (int i = 0; i < listeners.Length; i++)
            {
                listeners[i] = new Thread(ThreadAction);
            }

            Thread writer = new Thread(() =>
                {
                    WriterAction();
                });

            Console.WriteLine("STARTING...");

            writer.Start();

            for (int i = 0; i < listeners.Length; i++)
            {
                listeners[i].Start(i);
            }

            writer.Join();
            for (int i = 0; i < listeners.Length; i++)
            {
                listeners[i].Join();
            }

            Console.WriteLine("DONE");
        }

        static ManualResetEvent waitForSubscribed = new ManualResetEvent(false);

        private static void WriterAction()
        {
            using (MQTT.Client.Client c = new MQTT.Client.Client("writer"))
            {
                c.OnUnsolicitedMessage += new UnsolicitedMessageCallback(c_OnUnsolicitedMessage);

                IPAddress address = Dns.GetHostAddresses(server).Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).First();
                IPEndPoint test = new IPEndPoint(address, port);

                Console.WriteLine("WRITER connecting...");
                DemandWorked(c.Connect(test));
                Console.WriteLine("WRITER connected...");

                Console.WriteLine("Waiting for subscribe to finish...");
                waitForSubscribed.WaitOne();
                Console.WriteLine("Subscribe is ready!");

                int count = 0;
                while (true)
                {
                    count++;
                    Console.WriteLine("WRITER writing...");
                    c.Publish("root", string.Format("This is message {0}!", count), QualityOfService.ExactlyOnce, null).Wait();
                    Console.WriteLine("WRITER wrote...");
                }

                c.Disconnect(TimeSpan.FromSeconds(5));
            }
        }

        private static void ThreadAction(object name)
        {
            string lname = string.Format("listener {0}", name.ToString());

            using (MQTT.Client.Client c = new MQTT.Client.Client(lname))
            {
                c.OnUnsolicitedMessage += new UnsolicitedMessageCallback(c_OnUnsolicitedMessage);

                IPAddress address = Dns.GetHostAddresses(server).Where(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork).First();
                IPEndPoint test = new IPEndPoint(address, port);

                Console.WriteLine("{0} connecting...", lname);
                DemandWorked(c.Connect(test));
                Console.WriteLine("{0} connected...", lname);

                Console.WriteLine("{0} subscribing...", lname);
                DemandWorked(c.Subscribe(
                    new Subscription[] 
                    {
                        new Subscription("#", QualityOfService.ExactlyOnce), 
                    }, null));
                Console.WriteLine("{0} subscribed...", lname);

                waitForSubscribed.Set();

                while (true)
                {
                    Thread.Sleep(100);
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
