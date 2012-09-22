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
        static string server = "localhost"; // "test.mosquitto.org"
        static int port = 1883;

        static void Main(string[] args)
        {
            Thread[] listeners = new Thread[100];

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
                c.ClientId = "writer";

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
                    c.Publish("root", string.Format("This is message {0}!", count), QualityOfService.ExactlyOnce, null).Await();
                    Console.WriteLine("WRITER wrote...");

                    Thread.Sleep(10000);
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
        }

        static void DemandWorked(Task task)
        {
            task.Await();
        }
    }
}
