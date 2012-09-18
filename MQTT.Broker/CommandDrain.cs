using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace MQTT.Broker
{
    internal class CommandDrain
    {
        Thread _drainThread;

        public void Start()
        {
            _drainThread = new Thread(DrainLoop);
            _drainThread.Start();            
        }

        public void Stop()
        {
            _drainThread.Abort();
        }

        private void DrainLoop()
        {
            while(true)
            {
                ActiveConnection conn;
                if (IncomingDataQueue.TryDequeue(out conn))
                {
                    conn.ReadCommand();
                }
            }
        }
    }
}
