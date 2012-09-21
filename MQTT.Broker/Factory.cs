using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Ninject;
using Ninject.Modules;
using MQTT.Broker.Network;

namespace MQTT.Broker
{
    public static class Factory
    {
        static IKernel Kernel = new StandardKernel(new MqttProductionModule());

        public static T Get<T>()
        {
            return Kernel.Get<T>();
        }
    }

    internal class MqttProductionModule : NinjectModule 
    {
        public override void  Load()
        {
            Bind<IConnectionManager>().To<TcpClientManager>();
            Bind<ICommandReader>().To<CommandReader>();
            Bind<ICommandWriter>().To<CommandWriter>();
            Bind<IActiveConnectionManager>().To<ActiveConnectionManager>();
            Bind<INewConnectionManager>().To<NewConnectionManager>();
        }
    }
}