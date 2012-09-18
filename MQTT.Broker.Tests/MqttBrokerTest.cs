using MQTT.Broker;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Net;
using MQTT.Types;
using MQTT.Domain;
using System.Collections.Generic;
using System.Threading;

namespace MQTT.Broker.Tests
{


    [TestClass()]
    public class MqttBrokerTest
    {


        private TestContext testContextInstance;

        public TestContext TestContext
        {
            get
            {
                return testContextInstance;
            }
            set
            {
                testContextInstance = value;
            }
        }

        #region Additional test attributes
        // 
        //You can use the following additional attributes as you write your tests:
        //
        //Use ClassInitialize to run code before running the first test in the class
        //[ClassInitialize()]
        //public static void MyClassInitialize(TestContext testContext)
        //{
        //}
        //
        //Use ClassCleanup to run code after all tests in a class have run
        //[ClassCleanup()]
        //public static void MyClassCleanup()
        //{
        //}
        //
        //Use TestInitialize to run code before running each test
        //[TestInitialize()]
        //public void MyTestInitialize()
        //{
        //}
        //
        //Use TestCleanup to run code after each test has run
        //[TestCleanup()]
        //public void MyTestCleanup()
        //{
        //}
        //
        #endregion


        [TestMethod()]
        public void ListenTest()
        {
            Factory.Initialize(
                new Dictionary<Type, Type>
                {
                    { typeof(IMqttBroker), typeof(MqttNetworkBroker) },
                });

            using (MqttBroker broker = new MqttBroker())
            {
                IPEndPoint endpoint = new IPEndPoint(IPAddress.IPv6Loopback, 1883);
                broker.Listen(endpoint);
                Assert.AreEqual(0, broker.ConnectionCount);

                Client.Client c = new Client.Client("clientid");
                c.Connect(endpoint).Wait();
                Assert.IsTrue(c.IsConnected);
                c.Disconnect(TimeSpan.FromSeconds(1));
                Assert.IsFalse(c.IsConnected);
            }
        }
    }
}
