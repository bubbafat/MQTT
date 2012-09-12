using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using MQTT.Client.Commands;
using System.Threading.Tasks;
using MQTT.Types;
using MQTT.Client;
using System.Threading;

namespace MQTT.ConsoleApp
{
    class Program
    {
        static void DemandWorked(Task task)
        {
            task.Wait();
            if(task.IsFaulted)
            {
                Console.WriteLine(task.Exception.ToString());
                throw task.Exception;
            }

            if(!task.IsCompleted)
            {
                Console.WriteLine("Task did not complete?  WTF???");
                throw new Exception("WTF?");
            }
        }

        static void Main(string[] args)
        {
            Factory.Initialize(
                new Dictionary<Type, Type>
                {
                    { typeof(IMqttBroker), typeof(MqttNetworkBroker) },
                });

            Thread listener = new Thread(() =>
            {
                MQTT.Client.Client c = new Client.Client("mosquito/bubbafat");
                IPEndPoint test = new IPEndPoint(Dns.GetHostAddresses("test.mosquitto.org")[0], 1883);
                DemandWorked(c.Connect(test));

                Task<ClientCommand> response = c.Receive();
                DemandWorked(response);
                DemandWorked(c.Subscribe(new string[] { "#", }));

                try
                {
                    long received = 0;
                    while (true)
                    {
                        response = c.Receive();
                        DemandWorked(response);
                        if (response.Result.CommandMessage == CommandMessage.PUBLISH)
                        {
                            if (!response.Result.Header.Retain)
                            {
                                if (!(response.Result as Publish).Topic.Contains("radiovis"))
                                {
                                    received++;

                                    Console.WriteLine("[{0} : {1}] RECV: ", received, DateTime.Now.ToString());
                                    Console.WriteLine((response.Result as Publish).Topic);

                                    foreach (char ch in Encoding.ASCII.GetChars((response.Result as Publish).Message))
                                    {
                                        if (char.IsControl(ch))
                                        {
                                            Console.Write("?");
                                        }
                                        else
                                        {
                                            Console.Write(ch);
                                        }
                                    }

                                    Console.WriteLine();
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine(response.Result.CommandMessage);
                        }
                    }
                }
                finally
                {
                    c.Disconnect(TimeSpan.FromSeconds(5));
                }
            });


            Thread ping = new Thread(() =>
                {
                    MQTT.Client.Client c = new Client.Client("mosquito/bubbafatping");
                    IPEndPoint test = new IPEndPoint(Dns.GetHostAddresses("test.mosquitto.org")[0], 1883);
                    DemandWorked(c.Connect(test));

                    Task<ClientCommand> response = c.Receive();
                    DemandWorked(response);

                    DemandWorked(c.Subscribe(new string[] { "bubbafat/pong", }));

                    int sent = 0;
                    while(true)
                    {
                        if (sent++ < 5)
                        {
                            DemandWorked(c.Publish("bubbafat/ping", "ping"));
                        }

                        response = c.Receive();
                        DemandWorked(response);
                        if (response.Result.CommandMessage == CommandMessage.PUBLISH)
                        {
                            if (response.Result.Header.Duplicate)
                            {
                                Console.WriteLine("DUPLICATE!!!!!!!!!!!!!!!!");
                            }

                            Console.WriteLine("RECV: {0}", (response.Result as Publish).Message);
                        }
                        else if (response.Result.CommandMessage == CommandMessage.SUBACK)
                        {
                            SubAck ack = (SubAck)response.Result;
                            foreach (QualityOfService qos in ack.Grants)
                            {
                                Console.WriteLine("Grants: {0}", qos);
                            }
                        }
                        else
                        {
                            Console.WriteLine(response.Result.CommandMessage);
                        }
                    }

                    c.Disconnect(TimeSpan.FromSeconds(5));
                });

            Thread pong = new Thread(() =>
            {
                MQTT.Client.Client c = new Client.Client("mosquito/bubbafatpong");
                IPEndPoint test = new IPEndPoint(Dns.GetHostAddresses("test.mosquitto.org")[0], 1883);
                DemandWorked(c.Connect(test));

                Task<ClientCommand> response = c.Receive();
                DemandWorked(response);

                DemandWorked(c.Subscribe(new string[] { "bubbafat/ping", }));

                int sent = 0;
                while(true)
                {
                    if (sent++ < 5)
                    {
                        DemandWorked(c.Publish("bubbafat/pong", "pong"));
                    }
                    response = c.Receive();
                    DemandWorked(response);
                    if (response.Result.CommandMessage == CommandMessage.PUBLISH)
                    {
                        if (response.Result.Header.Duplicate)
                        {
                            Console.WriteLine("DUPLICATE!!!!!!!!!!!!!!!!");
                        }

                        Console.WriteLine("RECV: {0}", (response.Result as Publish).Message);
                    }
                    else if (response.Result.CommandMessage == CommandMessage.SUBACK)
                    {
                        SubAck ack = (SubAck)response.Result;
                        foreach (QualityOfService qos in ack.Grants)
                        {
                            Console.WriteLine("Grants: {0}", qos);
                        }
                    }
                    else
                    {
                        Console.WriteLine(response.Result.CommandMessage);
                    }
                    Thread.Sleep(500);
                }

                c.Disconnect(TimeSpan.FromSeconds(5));
            });

            Console.WriteLine("STARTING...");

            //listener.Start();
            //listener.Join();

            ping.Start();
            pong.Start();

            ping.Join();
            pong.Join();

            Console.WriteLine("DONE");
        }
    }
}
