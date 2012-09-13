using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Client.Commands;
using System.Threading.Tasks;
using MQTT.Types;

namespace MQTT.Client
{
    public class PublishReceiveFlow : StateMachine
    {
        public PublishReceiveFlow(StateMachineManager manager)
            : base(manager)
        {
        }

        public override Task Start(ClientCommand msg, Action<ClientCommand> release)
        {
            if (release == null)
            {
                release = (ClientCommand p) => { };
            }

            switch (msg.Header.QualityOfService)
            {
                case QualityOfService.AtMostOnce:
                    return Task.Factory.StartNew(() => release(msg));
                case QualityOfService.AtLeastOnce:
                    return Send(new PubAck(msg.MessageId))
                         .ContinueWith((task) =>
                            Task.Factory.StartNew(() => release(msg)),
                            TaskContinuationOptions.OnlyOnRanToCompletion);
                case QualityOfService.ExactlyOnce:
                    return Send(new PubRec(msg.MessageId))
                        .ContinueWith((task) =>
                            WaitFor(CommandMessage.PUBREL, msg.MessageId, TimeSpan.FromSeconds(60)),
                            TaskContinuationOptions.OnlyOnRanToCompletion)
                        .ContinueWith((task) =>
                            Task.Factory.StartNew(() => release(msg)), 
                            TaskContinuationOptions.OnlyOnRanToCompletion)
                        .ContinueWith((task) =>
                            Send(new PubComp(msg.MessageId)),
                            TaskContinuationOptions.OnlyOnRanToCompletion);
                default:
                    throw new InvalidOperationException("Unknown QoS");
            }
        }
    }
}
