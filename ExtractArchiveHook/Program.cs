using EasyHook;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Remoting;
using System.Security.Principal;
using System.Threading;

namespace ExtractArchiveHook
{
    public static class Program
    {
        public static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private static bool IsRadStudioSetupProcess(Process p)
        {
            try
            {
                return p.MainModule.FileName.Contains(@"Embarcadero\Studio")
                    && WindowsApi.FindWindow("TBasedInstallerDlg", null) != IntPtr.Zero;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static void Main(string[] args)
        {
            if (!IsAdministrator())
            {
                throw new Exception("Run this app as administrator");
            }

            Console.WriteLine("Waiting for process...");

            Process process = null;
            while (process == null)
            {
                process = Process.GetProcessesByName("bds")
                    .Where(IsRadStudioSetupProcess)
                    .FirstOrDefault();
                if (process == null)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(500));
                }
                else
                {
                    Console.WriteLine($"Process found: {process.Id} - {process.ProcessName}");
                }
            }

            DateTime? latestPing = null;
            bool exited = false;

            var remoteObject = new CustomRemoteObject();
            remoteObject.InjectionEvent += (sender, eventArgs) => { Console.WriteLine($"Injected into {eventArgs.ClientProcessId}"); };
            remoteObject.MessageEvent += (sender, eventArgs) => { Console.WriteLine($"Message: {eventArgs.Message}"); };
            remoteObject.PingEvent += (sender, eventArgs) => { latestPing = DateTime.UtcNow; };
            remoteObject.ExitEvent += (sender, eventArgs) => { exited = true; };
            remoteObject.ExceptionEvent += (sender, eventArgs) => { Console.WriteLine($"Exception: {eventArgs.SerializedException}"); };

            string channelName = null;
            var ipcServerChannel = RemoteHooking.IpcCreateServer<CustomRemoteObject>(ref channelName, WellKnownObjectMode.Singleton, remoteObject);
            
            var parameter = new EntryPointParameters
            {
                Message = "hello world",
                HostProcessId = RemoteHooking.GetCurrentProcessId(),
            };

            var processId = process.Id;
            RemoteHooking.Inject(processId,
                InjectionOptions.Default | InjectionOptions.DoNotRequireStrongName,
                typeof(EntryPointParameters).Assembly.Location,
                typeof(EntryPointParameters).Assembly.Location,
                channelName,
                parameter);

            while (!process.HasExited)
            {
                Thread.Sleep(TimeSpan.FromMilliseconds(100));
                if (exited)
                {
                    Console.WriteLine("Exit event received");
                    break;
                }
                /*
                if (latestPing.HasValue && DateTime.UtcNow.Subtract(latestPing.Value) > TimeSpan.FromSeconds(2))
                {
                    Console.WriteLine("Ping timeout");
                    break;
                }
                */
            }
        }
    }
}
