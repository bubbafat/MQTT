using Ninject;
using Ninject.Modules;
using MQTT.Domain;

namespace MQTT.Client.Console
{
    internal static class Factory
    {
        static readonly IKernel Kernel = new StandardKernel(new MqttClientModule());

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
            Bind<IMqttClient>().To<MqttNetworkClient>();
            Bind<INetworkInterface>().To<NetworkInterface>();
        }
    }
}