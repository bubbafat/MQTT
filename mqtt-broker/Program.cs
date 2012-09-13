using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Broker;
using System.Net;
using System.Threading;
using MQTT.Types;
using MQTT.Domain;

namespace mqtt_broker
{
    class Program
    {
        static void Main(string[] args)
        {
            Factory.Initialize(
                new Dictionary<Type, Type>
                {
                    { typeof(IMqttBroker), typeof(MqttNetworkBroker) },
                });

            using (MqttBroker broker = new MqttBroker())
            {
                IPEndPoint endpoint = new IPEndPoint(IPAddress.Any, 1883);
                broker.Listen(endpoint);

                while (true)
                {
                    Thread.Sleep(1000);
                }
            }
        }
    }
}
