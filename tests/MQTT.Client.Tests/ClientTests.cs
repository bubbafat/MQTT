using System;
using System.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Net;

namespace MQTT.Client.Tests
{
    [TestClass]
    public class ClientTests
    {
        [TestMethod]
        public void Connect()
        {
            var c = new MqttClient("clientId", new MockMqttClient());

            Assert.IsFalse(c.IsConnected);
            c.Connect(new IPEndPoint(IPAddress.Loopback, 1883)).Wait();
            Assert.IsTrue(c.IsConnected);
            c.Disconnect(TimeSpan.FromSeconds(1));
            Assert.IsFalse(c.IsConnected);
        }

        [TestMethod]
        public void TimeoutFiresNotice()
        {
            ManualResetEvent pingedOut = new ManualResetEvent(false);

            var network = new MockMqttClient
                {
                    SendPingResponses = false
                };

            var c = new MqttClient("clientId", network)
                {
                    KeepAlivePingResponseMinimumWait = 5
                };

            c.OnKeepAliveExpired += (sender, args) =>
                {
                    pingedOut.Set();
                };

            c.Connect(new IPEndPoint(IPAddress.Loopback, 1883), 5).Wait();

            Assert.IsTrue(pingedOut.WaitOne(TimeSpan.FromSeconds(10)),
                          "The ping timeout never fired");
        }
    }
}
