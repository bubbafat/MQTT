using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MQTT.Types;
using System.Net;
using MQTT.Commands;
using System.Threading.Tasks;
using MQTT.Domain;

namespace MQTT.Client.Tests
{
    [TestClass]
    public class ClientTests
    {
        [TestMethod]
        public void Connect()
        {
            Client c = new Client(new MockMqttClient());
            c.ClientId = "clientId";

            Assert.IsFalse(c.IsConnected);
            c.Connect(new IPEndPoint(IPAddress.Loopback, 1883)).Wait();
            Assert.IsTrue(c.IsConnected);
            c.Disconnect(TimeSpan.FromSeconds(1));
            Assert.IsFalse(c.IsConnected);
        }
    }
}
