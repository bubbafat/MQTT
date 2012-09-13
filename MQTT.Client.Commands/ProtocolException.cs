using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MQTT.Types;

namespace MQTT.Client.Commands
{
    [Serializable]
    public class ProtocolException : Exception
    {
        public ProtocolException(CommandMessage cmd)
            : base(string.Format("Protocol Exception parsing command: {0}", cmd.ToString()))
        {
        }

        public ProtocolException(CommandMessage cmd, string message)
            : base(string.Format("Protocol Exception parsing command: {0}.  {1}", cmd.ToString(), message))
        {
        }

        public ProtocolException(string message)
            : base(string.Format("Protocol Exception with message: {0}", message))
        {
        }
    }
}
