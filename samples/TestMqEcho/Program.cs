using System;
using System.Text;
using System.Threading;
using MQTT.Client;
using MQTT.Commands;
using MQTT.Domain;
using MQTT.Types;

namespace TestMqEcho
{
    class Program
    {
        private static readonly ManualResetEvent stopEvent = new ManualResetEvent(false);
        private static readonly object consoleLock = new object();

        static void Main()
        {
            Console.CancelKeyPress += Console_CancelKeyPress;

//            const string server = "localhost";
//            const string server = "broker.mqttdashboard.com";
            const string server = "test.mosquitto.org";

            const string clientName = "horvick";

            using (var client = new MqttClient(clientName))
            {
                client.OnUnsolicitedMessage += client_OnUnsolicitedMessage;
                client.OnNetworkDisconnected += client_OnNetworkDisconnected;
                client.Connect(server, cleanSession: true).Await();
                client.Subscribe("#", QualityOfService.AtMostOnce).Await();

                stopEvent.WaitOne(-1);
            }
        }

        static void client_OnNetworkDisconnected(object sender, NetworkDisconenctedEventArgs e)
        {
            stopEvent.Set();
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
