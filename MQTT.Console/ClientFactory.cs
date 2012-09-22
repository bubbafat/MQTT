using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ninject;
using Ninject.Modules;
using MQTT.Domain;

namespace MQTT.Client.Console
{
    internal static class Factory
    {
        static IKernel Kernel = new StandardKernel(new MqttClientModule());

        public static T Get<T>()
        {
            return Kernel.Get<T>();
        }
    }

    internal class MqttClientModule : NinjectModule
    {
        public override void Load()
        {
            Bind<ICommandReader>().To<CommandReader>();
            Bind<ICommandWriter>().To<CommandWriter>();
            Bind<IMqttBroker>().To<MqttNetworkBroker>();
            Bind<INetworkInterface>().To<NetworkInterface>();
        }
    }
}