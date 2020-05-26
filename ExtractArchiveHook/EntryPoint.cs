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

        public void Run(RemoteHooking.IContext context, string channelName, EntryPointParameters parameter)
        {
            try
            {
                using (var extractArchiveHook = LocalHook.Create(LocalHook.GetProcAddress("mia.lib", "ExtractArchive"),
                    new ExtractArchiveFnPtr(ExtractArchive_Hooked),
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

        #pragma warning disable CA2101
        [DllImport("mia.lib", CallingConvention = CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = true)]
        private static extern void ExtractArchive(string archive, int arg_ch, string extractPath, int arg_14h, int arg_18h, string password, int arg_20h, int arg_24h, int arg_28h, int arg_2ch);
        #pragma warning restore CA2101

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Ansi, SetLastError = true)]
        private delegate void ExtractArchiveFnPtr(string archive, int arg_ch, string extractPath, int arg_14h, int arg_18h, string password, int arg_20h, int arg_24h, int arg_28h, int arg_2ch);

        private static void ExtractArchive_Hooked(string archive, int arg_ch, string extractPath, int arg_14h, int arg_18h, string password, int arg_20h, int arg_24h, int arg_28h, int arg_2ch)
        {
            EntryPoint Self = (EntryPoint)HookRuntimeInfo.Callback;
            Self.SendMessage($"'{archive}'='{password}';");
            // call original API
            ExtractArchive(archive, arg_ch, extractPath, arg_14h, arg_18h, password, arg_20h, arg_24h, arg_28h, arg_2ch);
        }
    }
}
