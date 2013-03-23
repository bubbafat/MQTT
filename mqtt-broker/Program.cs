using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Broker;
using System.Net;
using System.Threading;

namespace mqtt_broker
{
    class Program
    {
        static void Main(string[] args)        
        {
            using (var broker = BrokerFactory.Get<MqttBroker>())
            {
                var endpoint = new IPEndPoint(IPAddress.Any, 1883);
                broker.Listen(endpoint);

                Console.Write("Listening");

                while (true)
                {
                    Console.Write(".");
                    Thread.Sleep(10000);
                }
            }
        }
    }
}
