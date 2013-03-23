using Ninject;
using Ninject.Modules;
using MQTT.Broker.Network;
using MQTT.Domain;

namespace MQTT.Broker
{
    public static class BrokerFactory
    {
        static readonly IKernel Kernel = new StandardKernel(new MqttProductionModule());

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