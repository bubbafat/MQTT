using System;
using System.Threading;
using System.Threading.Tasks;

namespace MQTT.Types
{
    public static class TaskExtensions
    {
        public static Task Await(this Task task)
        {
            task.Wait();
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    if (task.Exception != null)
                    {
                        throw task.Exception;
                    }
                    throw new InvalidOperationException("The Task has faulted but no exception was found.");
                case TaskStatus.Canceled:
                    throw new OperationCanceledException();
                case TaskStatus.RanToCompletion:
                    return task;
                default:
                    throw new InvalidOperationException("I have no idea...");
            }
        }

        public static Task<T> Await<T>(this Task<T> task)
        {
            task.Wait();
            switch (task.Status)
            {
                case TaskStatus.Faulted:
                    if (task.Exception != null)
                    {
                        throw task.Exception;
                    }                        
                    throw new InvalidOperationException("The Task has faulted but no exception was found.");
                case TaskStatus.Canceled:
                    throw new OperationCanceledException();
                case TaskStatus.RanToCompletion:
                    return task;
                default:
                    throw new InvalidOperationException("I have no idea...");
            }
        }
    }
}
