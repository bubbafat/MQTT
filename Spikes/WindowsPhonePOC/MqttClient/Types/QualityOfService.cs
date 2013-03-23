using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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
