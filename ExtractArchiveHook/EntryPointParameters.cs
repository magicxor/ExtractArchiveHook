using System;

namespace ExtractArchiveHook
{
    [Serializable]
    public sealed class EntryPointParameters
    {
        public string Message;
        public int HostProcessId;
    }
}
