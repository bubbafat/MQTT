using System;
using System.Text;
using System.Threading;
using MQTT.Client;
using MQTT.Commands;
using MQTT.Domain;

namespace TestMqEcho
{
    class Program
    {
        private static readonly ManualResetEvent stopEvent = new ManualResetEvent(false);
        private static readonly object consoleLock = new object();

        static void Main()
        {
            Console.CancelKeyPress += Console_CancelKeyPress;

            const string server = "test.mosquitto.org";
            const int port = 1883;

            string clientName = Guid.NewGuid().ToString();

            using (var client = new MqttClient(clientName))
            {
                client.OnUnsolicitedMessage += client_OnUnsolicitedMessage;
                client.Connect(server, port).Wait();
                client.Subscribe("#").Wait();

                stopEvent.WaitOne(-1);
            }
        }

        static void Console_CancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            stopEvent.Set();
        }

        static void client_OnUnsolicitedMessage(object sender, ClientCommandEventArgs e)
        {
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

                lock (consoleLock)
                {
                    Console.WriteLine(result);
                }
            }
        }
    }
}
