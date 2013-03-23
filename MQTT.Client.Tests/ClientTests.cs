﻿using System;
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
            var c = new Client(new MockMqttClient()) {ClientId = "clientId"};

            Assert.IsFalse(c.IsConnected);
            c.Connect(new IPEndPoint(IPAddress.Loopback, 1883)).Wait();
            Assert.IsTrue(c.IsConnected);
            c.Disconnect(TimeSpan.FromSeconds(1));
            Assert.IsFalse(c.IsConnected);
        }
    }
}
