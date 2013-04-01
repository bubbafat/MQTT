
namespace MQTT.Types
{
    public class MessageIdSequence
    {
        readonly object _lock = new object();
        ushort _last = 1;

        public MessageId Next()
        {
            ushort temp;

            lock (_lock)
            {
                temp = _last++;
                if (temp == 0)
                {
                    temp = _last++;                    
                }
            }

            return new MessageId(temp);
        }
    }
}
