using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using MQTT.Client;
using MQTT.Commands;
using MQTT.Domain;
using MQTT.Types;

namespace MQTT.ConsoleApp
{
    internal class Program
    {
        private const string Server = "test.mosquitto.org";
        private const int Port = 1883;
        private static readonly string topic = "dn_sample";

        private static readonly ManualResetEvent waitForSubscribed = new ManualResetEvent(false);
        private static readonly object conLock = new object();

        private static readonly ManualResetEvent done = new ManualResetEvent(false);

        private static void Main()
        {
            Console.CancelKeyPress += Console_CancelKeyPress;
            var listeners = new Thread[Environment.ProcessorCount*2];

            for (int i = 0; i < listeners.Length; i++)
            {
                listeners[i] = new Thread(ThreadAction);
            }

            var writer = new Thread(WriterAction);

            Console.WriteLine("STARTING...");

            writer.Start();

            for (int i = 0; i < listeners.Length; i++)
            {
                listeners[i].Start(string.Format("{0}_L_{1}", topic, i));
            }

            writer.Join();
            foreach (Thread t in listeners)
            {
                t.Join();
            }

            Console.WriteLine("DONE");
        }

        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            done.Set();
        }

        private static void WriterAction()
        {
            try
            {
                DoWriteAction();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                done.Set();
            }
        }


        private static void DoWriteAction()
        {
            using (var c = new MqttClient(string.Format("{0}_{1}", topic, Thread.CurrentThread.ManagedThreadId)))
            {
                c.OnUnsolicitedMessage += c_OnUnsolicitedMessage;

                IPAddress address =
                    Dns.GetHostAddresses(Server)
                       .First(a => a.AddressFamily == AddressFamily.InterNetwork);

                var test = new IPEndPoint(address, Port);

                Console.WriteLine("WRITER connecting...");
                DemandWorked(c.Connect(test));
                Console.WriteLine("WRITER connected...");

                Console.WriteLine("Waiting for subscribe to finish...");
                waitForSubscribed.WaitOne();
                Console.WriteLine("Subscribe is ready!");

                int count = 0;
                while (done.WaitOne(0) == false)
                {
                    count++;
                    // Console.WriteLine("WRITER writing...");
                    c.Publish(topic, string.Format("This is message {0}!", count), QualityOfService.AtLeastOnce, null); // .Await();
                    // Console.WriteLine("WRITER wrote...");

                    Thread.Sleep(50);
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

        private static void DoThreadAction(object nameObj)
        {
            string name = nameObj.ToString();

            using (var c = new MqttClient(name))
            {
                c.OnUnsolicitedMessage += (c_OnUnsolicitedMessage);

                IPAddress address =
                    Dns.GetHostAddresses(Server)
                       .First(a => a.AddressFamily == AddressFamily.InterNetwork);

                var test = new IPEndPoint(address, Port);

                Console.WriteLine("{0} connecting...", name);
                DemandWorked(c.Connect(test));
                Console.WriteLine("{0} connected...", name);

                Console.WriteLine("{0} subscribing...", name);
                DemandWorked(c.Subscribe(
                    new[]
                        {
                            new Subscription(topic, QualityOfService.AtLeastOnce),
                        }, null));
                Console.WriteLine("{0} subscribed...", name);

                waitForSubscribed.Set();
                done.WaitOne(-1);
                c.Disconnect(TimeSpan.FromSeconds(5));
            }
        }

        private static void c_OnUnsolicitedMessage(object sender, ClientCommandEventArgs e)
        {
            return;
            MqttCommand command = e.Command;

            var p = command as Publish;
            if (p != null)
            {
                var sb = new StringBuilder();
                if (p.Header.Duplicate)
                {
                    sb.Append("!!! DUPLICATE !!! - ");
                }
                sb.AppendFormat("TOPIC: {0}", p.Topic);
                sb.AppendLine();
                foreach (char c in Encoding.ASCII.GetChars(p.Message))
                {
                    if (!char.IsControl(c))
                    {
                        sb.Append(c);
                    }
                }

                sb.AppendLine();
                var result = sb.ToString();

                lock (conLock)
                {
                    Console.WriteLine(result);
                }
            }

        }

        private static void DemandWorked(Task task)
        {
            task.Await();
        }
    }
}