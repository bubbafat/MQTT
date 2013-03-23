using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Microsoft.Phone.Controls;
using MQTT.Broker;
using MQTT.Domain;
using MQTT.Client;
using Microsoft.Phone.Net.NetworkInformation;
using System.Threading;
using MQTT.Types;
using MQTT.Commands;
using System.Text;

namespace MqttListener
{
    public partial class MainPage : PhoneApplicationPage
    {
        private const string server = "test.mosquitto.org";
        private const int port = 1883;
        private readonly static string topic = "#";
        private volatile IPEndPoint ipEndpoint;
        private volatile MQTT.Client.Client client;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            string lname = Guid.NewGuid().ToString();

            client = new MQTT.Client.Client(new MqttNetworkBroker(new MQTT.Domain.NetworkInterface(new CommandReader(), new CommandWriter())));

            client.ClientId = lname;

            client.OnUnsolicitedMessage += new UnsolicitedMessageCallback(c_OnUnsolicitedMessage);

            DnsLookup(server, port);

            while (ipEndpoint == null)
            {
                Thread.Sleep(50);
            }

            client.Connect(ipEndpoint);

            client.Subscribe(
                new Subscription[] 
                    {
                        new Subscription(topic, QualityOfService.ExactlyOnce), 
                    }, null);
        }

        protected override void OnDoubleTap(GestureEventArgs e)
        {
            if (client.IsConnected)
            {
                client.Disconnect(TimeSpan.FromSeconds(1));
            }
            else
            {
                client.Connect(ipEndpoint);
                client.Subscribe(
                    new Subscription[] 
                    {
                        new Subscription(topic, QualityOfService.ExactlyOnce), 
                    }, null);
            }
        }

        static DateTime nextUpdate = DateTime.MinValue;

        void c_OnUnsolicitedMessage(object sender, ClientCommandEventArgs e)
        {
            System.Windows.Deployment.Current.Dispatcher.BeginInvoke(() =>
            {
                if (nextUpdate > DateTime.UtcNow)
                {
                    return;
                }

                nextUpdate = DateTime.UtcNow.AddSeconds(1);

                MqttCommand command = e.Command;

                Publish p = command as Publish;
                if (p != null)
                {
                    lock (this.textBlock1)
                    {
                        if (p.Header.Duplicate)
                        {
                            this.textBlock1.Text = "!!! DUPLICATE !!! - ";
                        }

                        StringBuilder sb = new StringBuilder();

                        sb.AppendFormat("TOPIC: {0}", p.Topic);
                        sb.AppendLine();
                        sb.AppendLine();

                        foreach (char c in UTF8Encoding.UTF8.GetChars(p.Message))
                        {
                            if (!char.IsControl(c))
                            {
                                sb.Append(c);
                            }
                        }


                        sb.AppendLine();
                        sb.AppendLine();
                        
                        this.textBlock1.Text = sb.ToString();
                    }
                }
            });
        }

        public void DnsLookup(string hostname, int port)
        {
            var endpoint = new DnsEndPoint(hostname, port, System.Net.Sockets.AddressFamily.InterNetwork);
            DeviceNetworkInformation.ResolveHostNameAsync(endpoint, OnNameResolved, null);
        }

        private void OnNameResolved(NameResolutionResult result)
        {
            this.ipEndpoint = result.IPEndPoints.First();
        }
    }
}