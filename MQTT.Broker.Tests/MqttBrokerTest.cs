//using MQTT.Broker;
//using Microsoft.VisualStudio.TestTools.UnitTesting;
//using System;
//using System.Net;
//using MQTT.Types;
//using MQTT.Domain;
//using System.Collections.Generic;
//using System.Threading;
//using MQTT.Commands;

//namespace MQTT.Broker.Tests
//{


//    [TestClass()]
//    public class MqttBrokerTest
//    {


//        private TestContext testContextInstance;

//        public TestContext TestContext
//        {
//            get
//            {
//                return testContextInstance;
//            }
//            set
//            {
//                testContextInstance = value;
//            }
//        }

//        #region Additional test attributes
//        // 
//        //You can use the following additional attributes as you write your tests:
//        //
//        //Use ClassInitialize to run code before running the first test in the class
//        //[ClassInitialize()]
//        //public static void MyClassInitialize(TestContext testContext)
//        //{
//        //}
//        //
//        //Use ClassCleanup to run code after all tests in a class have run
//        //[ClassCleanup()]
//        //public static void MyClassCleanup()
//        //{
//        //}
//        //
//        //Use TestInitialize to run code before running each test
//        //[TestInitialize()]
//        //public void MyTestInitialize()
//        //{
//        //}
//        //
//        //Use TestCleanup to run code after each test has run
//        //[TestCleanup()]
//        //public void MyTestCleanup()
//        //{
//        //}
//        //
//        #endregion


//        [TestMethod()]
//        public void ListenTest()
//        {
//            OldFactory.Initialize(
//                new Dictionary<Type, Type>
//                {
//                    { typeof(IMqttBroker), typeof(MqttNetworkBroker) },
//                });

//            using (MqttBroker broker = new MqttBroker())
//            {
//                IPEndPoint endpoint = new IPEndPoint(IPAddress.IPv6Loopback, 1883);
//                broker.Listen(endpoint);
//                Assert.AreEqual(0, broker.ConnectionCount);

//                Client.Client c = new Client.Client("clientid");
//                c.Connect(endpoint).Wait();
//                Assert.IsTrue(c.IsConnected);
//                c.Disconnect(TimeSpan.FromSeconds(1));
//                Assert.IsFalse(c.IsConnected);
//            }
//        }

//        [TestMethod()]
//        public void PingPongTest()
//        {
//            OldFactory.Initialize(
//                new Dictionary<Type, Type>
//                {
//                    { typeof(IMqttBroker), typeof(MqttNetworkBroker) },
//                });

//            using (MqttBroker broker = new MqttBroker())
//            {
//                IPEndPoint endpoint = new IPEndPoint(IPAddress.IPv6Loopback, 1883);
//                broker.Listen(endpoint);
//                Assert.AreEqual(0, broker.ConnectionCount);

//                using(Client.Client ping = new Client.Client("ping"))
//                using (Client.Client pong = new Client.Client("pong"))
//                {
//                    int[] pings = new int[1];
//                    int[] pongs = new int[1];

//                    ping.Connect(endpoint).Wait();
//                    pong.Connect(endpoint).Wait();

//                    ping.Subscribe(new Commands.Subscription[]
//                    {
//                        new Commands.Subscription("pong", QualityOfService.ExactlyOnce),
//                    });

//                    pong.Subscribe(new Commands.Subscription[]
//                    {
//                        new Commands.Subscription("ping", QualityOfService.ExactlyOnce),
//                    });

//                    ping.OnUnsolicitedMessage += (object sender, ClientCommandEventArgs e) =>
//                    {
//                        pongs[0]++;
//                    };


//                    pong.OnUnsolicitedMessage += (object sender, ClientCommandEventArgs e) =>
//                    {
//                        pings[0]++;
//                    };

//                    while (pongs[0] < 10)
//                    {
//                        int current = pongs[0];

//                        ManualResetEvent published = new ManualResetEvent(false);
//                        ping.Publish("ping", "ping", QualityOfService.ExactlyOnce,
//                            (MqttCommand command) =>
//                            {
//                                published.Set();
//                            }).Wait();

//                        Assert.IsTrue(published.WaitOne(TimeSpan.FromSeconds(30)),
//                            "The ping timed out");

//                        published.Reset();

//                        pong.Publish("pong", "pong", QualityOfService.ExactlyOnce,
//                            (MqttCommand command) =>
//                            {
//                                published.Set();
//                            }).Wait();

//                        Assert.IsTrue(published.WaitOne(TimeSpan.FromSeconds(30)),
//                            "The pong timed out");
//                    }
//                }
//            }
//        }
//    }
//}
