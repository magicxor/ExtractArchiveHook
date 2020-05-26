using ExtractArchiveHook.EventArguments;
using System;

namespace ExtractArchiveHook
{
    public class CustomRemoteObject : MarshalByRefObject
    {
        public event EventHandler<InjectionEventEventArgs> InjectionEvent = delegate { };
        public event EventHandler<MessageEventArgs> MessageEvent = delegate { };
        public event EventHandler<EventArgs> PingEvent = delegate { };
        public event EventHandler<EventArgs> ExitEvent = delegate { };
        public event EventHandler<ExceptionEventArgs> ExceptionEvent = delegate { };

        public void TriggerInjectionEvent(int clientProcessId)
        {
            InjectionEvent(null, new InjectionEventEventArgs { ClientProcessId = clientProcessId });
        }

        public void TriggerMessageEvent(string message)
        {
            MessageEvent(null, new MessageEventArgs { Message = message });
        }

        public void TriggerPingEvent()
        {
            PingEvent(null, EventArgs.Empty);
        }

        public void TriggerExitEvent()
        {
            ExitEvent(null, EventArgs.Empty);
        }

        public void TriggerExceptionEvent(Exception exception)
        {
            ExceptionEvent(null, new ExceptionEventArgs
            {
                EventId = exception.HResult,
                Message = exception.Message,
                SerializedException = exception.ToString(),
            });
        }
    }
}
