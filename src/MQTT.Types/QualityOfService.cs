using System;

namespace MQTT.Types
{
    [Flags]
    public enum QualityOfService
    {
        AtMostOnce = 0,
        AtLeastOnce = 1,
        ExactlyOnce = 2,
    }
}
