using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using MQTT.Client.Commands;
using System.Threading.Tasks;
using MQTT.Types;
using MQTT.Client;
using System.Threading;

namespace MQTT.ConsoleApp
{
    class Program
    {
        static void DemandWorked(Task task)
        {
            task.Wait();
            if(task.IsFaulted)
            {
                Console.WriteLine(task.Exception.ToString());
                throw task.Exception;
            }

            if(!task.IsCompleted)
            {
                Console.WriteLine("Task did not complete?  WTF???");
                throw new Exception("WTF?");
            }
        }

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

            Console.WriteLine("STARTING...");

            listener.Start();
            listener.Join();

            Console.WriteLine("DONE");
        }

        private static void ThreadAction()
        {
            using (MQTT.Client.Client c = new Client.Client("mosquito/bubbafat"))
            {
                c.OnUnsolicitedMessage += new UnsolicitedMessageCallback(c_OnUnsolicitedMessage);

                IPEndPoint test = new IPEndPoint(Dns.GetHostAddresses("test.mosquitto.org")[0], 1883);

                DemandWorked(c.Connect(test));
                DemandWorked(c.Subscribe(new string[] { "#" }));

                while (true)
                {
                    Thread.Sleep(1000);
                }

                c.Disconnect(TimeSpan.FromSeconds(5));
            }
        }

        static void c_OnUnsolicitedMessage(object sender, ClientCommandEventArgs e)
        {
            ClientCommand command = e.Command;

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
    }
}
