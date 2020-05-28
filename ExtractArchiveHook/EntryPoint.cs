using EasyHook;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace ExtractArchiveHook
{
    public class EntryPoint : IEntryPoint
    {
        private readonly CustomRemoteObject _remoteObject;
        
        public EntryPoint(RemoteHooking.IContext context, string channelName, EntryPointParameters parameter)
        {
            // connect to host
            _remoteObject = RemoteHooking.IpcConnectClient<CustomRemoteObject>(channelName);
            _remoteObject.TriggerPingEvent();
        }

        private LocalHook MsgAndCreateHook(string dll, string function, Delegate InNewProc, object InCallback)
        {
            _remoteObject.TriggerMessageEvent($"Create hook: {dll} - {function}");
            return LocalHook.Create(LocalHook.GetProcAddress(dll, function), InNewProc, InCallback);
        }

        public void Run(RemoteHooking.IContext context, string channelName, EntryPointParameters parameter)
        {
            try
            {
                using (var extractArchiveHook = MsgAndCreateHook("kernel32.dll", "DeleteFileW",
                    new DeleteFileWFnPtr(DeleteFileW_Hooked),
                    this))
                {
                    // Don't forget that all hooks will start deactivated.
                    // The following ensures that all threads are intercepted:
                    extractArchiveHook.ThreadACL.SetExclusiveACL(new int[1]);

                    _remoteObject.TriggerInjectionEvent(RemoteHooking.GetCurrentProcessId());
                    _remoteObject.TriggerMessageEvent(parameter.Message);

                    while (true)
                    {
                        Thread.Sleep(TimeSpan.FromMilliseconds(100));
                        _remoteObject.TriggerPingEvent();
                    }
                }
            }
            catch (Exception e)
            {
                // We should notice our host process about this error
                _remoteObject.TriggerExceptionEvent(e);
            }
            finally
            {
                _remoteObject.TriggerExitEvent();
            }
        }

        public void SendMessage(string message)
        {
            _remoteObject.TriggerMessageEvent(message);
        }

        [DllImport("kernel32.dll", CallingConvention = CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DeleteFileW([MarshalAs(UnmanagedType.LPWStr)] string lpFileName);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode, SetLastError = true)]
        private delegate bool DeleteFileWFnPtr(string lpFileName);

        private static bool DeleteFileW_Hooked(string lpFileName)
        {
            EntryPoint Self = (EntryPoint)HookRuntimeInfo.Callback;
            Self.SendMessage($"Delete: {lpFileName}");
            return true;
        }
    }
}
