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
using MQTT.Client.Console;

namespace MQTT.ConsoleApp
{
    class Program
    {
        private const string server = "test.mosquitto.org";
        private const int port = 1883;
        private readonly static string topic = Guid.NewGuid().ToString();

        static void Main(string[] args)
        {
            Thread[] listeners = new Thread[Environment.ProcessorCount * 2];

            for (int i = 0; i < listeners.Length; i++)
            {
                listeners[i] = new Thread(ThreadAction);
            }

            Thread writer = new Thread(WriterAction);

            Console.WriteLine("STARTING...");

            writer.Start();

            for (int i = 0; i < listeners.Length; i++)
            {
                listeners[i].Start(string.Format("{0}_LISTENER_{1}", topic, i));
            }

            writer.Join();
            for (int i = 0; i < listeners.Length; i++)
            {
                listeners[i].Join();
            }

            Console.WriteLine("DONE");
        }

        static readonly ManualResetEvent waitForSubscribed = new ManualResetEvent(false);

        private static void WriterAction()
        {
            while (true)
            {
                try
                {
                    DoWriteAction();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    Thread.Sleep(5000);
                }
            }
        }

        private static void DoWriteAction()
        {
            using (MQTT.Client.Client c = Factory.Get<MQTT.Client.Client>())
            {
                c.ClientId = string.Format("{0}_WRITER_{1}", topic, Thread.CurrentThread.ManagedThreadId);

                c.OnUnsolicitedMessage += new UnsolicitedMessageCallback(c_OnUnsolicitedMessage);

                IPAddress address =
                    Dns.GetHostAddresses(server)
                       .First(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

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
                    c.Publish(topic, string.Format("This is message {0}!", count), QualityOfService.ExactlyOnce, null).Await();
                    Console.WriteLine("WRITER wrote...");

                    Thread.Sleep(5000);
                }

                c.Disconnect(TimeSpan.FromSeconds(5));
            }
        }

        private static void ThreadAction(object name)
        {
            try
            {
                DoThreadAction(name);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void DoThreadAction(object name)
        {
            string lname = string.Format("listener {0}", name.ToString());

            using (MQTT.Client.Client c = Factory.Get<MQTT.Client.Client>())
            {
                c.ClientId = lname;

                c.OnUnsolicitedMessage += new UnsolicitedMessageCallback(c_OnUnsolicitedMessage);

                IPAddress address =
                    Dns.GetHostAddresses(server)
                       .First(a => a.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork);

                IPEndPoint test = new IPEndPoint(address, port);

                Console.WriteLine("{0} connecting...", lname);
                DemandWorked(c.Connect(test));
                Console.WriteLine("{0} connected...", lname);

                Console.WriteLine("{0} subscribing...", lname);
                DemandWorked(c.Subscribe(
                    new Subscription[] 
                    {
                        new Subscription(topic, QualityOfService.ExactlyOnce), 
                    }, null));
                Console.WriteLine("{0} subscribed...", lname);

                waitForSubscribed.Set();

                while (true)
                {
                    Thread.Sleep(1000);
                }

                c.Disconnect(TimeSpan.FromSeconds(5));
            }
        }

        readonly static object conLock = new object();
        static void c_OnUnsolicitedMessage(object sender, ClientCommandEventArgs e)
        {
            MqttCommand command = e.Command;

            Publish p = command as Publish;
            if (p != null)
            {
                lock (conLock)
                {
                    if (p.Header.Duplicate)
                    {
                        Console.Write("!!! DUPLICATE !!! - ");
                    }

                    Console.WriteLine("TOPIC: {0}", p.Topic);
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
        }

        static void DemandWorked(Task task)
        {
            task.Await();
        }
    }
}
