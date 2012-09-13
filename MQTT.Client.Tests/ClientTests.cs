using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTT.Types;
using System.Net;
using MQTT.Client.Commands;
using System.Threading.Tasks;

namespace MQTT.Client.Tests
{
    [TestClass]
    public class ClientTests
    {
        [TestInitialize]
        public void Initialize()
        {
            Factory.Initialize(
                new Dictionary<Type, Type>
                {
                    { typeof(IMqttBroker), typeof(MockMqttBroker) },
                });
        }

        [TestMethod]
        public void Connect()
        {
            Client c = new Client("clientid");
            Assert.IsFalse(c.IsConnected);
            c.Connect(new IPEndPoint(IPAddress.Loopback, 1883));
            Assert.IsTrue(c.IsConnected);

//            Task<ClientCommand> response = c.WaitFor(CommandMessage.CONNACK, MessageId.Any, TimeSpan.FromSeconds(5));
//            response.Wait();
//            Assert.AreEqual(CommandMessage.CONNACK, response.Result.CommandMessage);
        }
    }
}
