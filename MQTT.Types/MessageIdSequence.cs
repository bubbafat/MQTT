using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace MQTT.Types
{
    public class MessageIdSequence
    {
        readonly object _lock = new object();
        ushort _last = 0;

        public MessageId Next()
        {
            lock (_lock)
            {
                if (_last == ushort.MaxValue)
                {
                    _last = 0;
                }

                _last++;

                return new MessageId(_last);
            }
        }
    }
}
