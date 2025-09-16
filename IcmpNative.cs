using System;
using System.Net;
using System.Runtime.InteropServices;

namespace PingMonitor
{
    internal static class IcmpNative
    {
        [StructLayout(LayoutKind.Sequential)]
        private struct IP_OPTION_INFORMATION
        {
            public byte Ttl;
            public byte Tos;
            public byte Flags;
            public byte OptionsSize;
            public IntPtr OptionsData;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ICMP_ECHO_REPLY
        {
            public uint Address;
            public uint Status;
            public uint RoundTripTime;
            public ushort DataSize;
            public ushort Reserved;
            public IntPtr Data;
            public IP_OPTION_INFORMATION Options;
        }

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern IntPtr IcmpCreateFile();

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern bool IcmpCloseHandle(IntPtr handle);

        // IPv4 API with explicit source address
        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint IcmpSendEcho2Ex(
            IntPtr IcmpHandle,
            IntPtr Event,
            IntPtr ApcRoutine,
            IntPtr ApcContext,
            uint SourceAddress,
            uint DestinationAddress,
            IntPtr RequestData,
            ushort RequestSize,
            ref IP_OPTION_INFORMATION RequestOptions,
            IntPtr ReplyBuffer,
            uint ReplySize,
            uint Timeout);

        public static (bool success, long rttMs, string? error) PingIPv4(string host, IPAddress? source, int timeoutMs = 2000)
        {
            try
            {
                var dest = ResolveIPv4(host);
                if (dest == null)
                    return (false, -1, "Destino IPv4 não encontrado");

                uint srcAddr = 0;
                if (source != null)
                {
                    if (source.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
                        return (false, -1, "Endereço de origem precisa ser IPv4");
                    srcAddr = ToUInt32(source);
                }

                var handle = IcmpCreateFile();
                if (handle == IntPtr.Zero || handle.ToInt64() == -1)
                    return (false, -1, "Falha ao abrir ICMP");

                try
                {
                    // Pequeno payload
                    byte[] data = new byte[] { 0x65, 0x63, 0x68, 0x6f }; // "echo"
                    IntPtr dataPtr = Marshal.AllocHGlobal(data.Length);
                    Marshal.Copy(data, 0, dataPtr, data.Length);

                    int replySize = Marshal.SizeOf<ICMP_ECHO_REPLY>() + data.Length + 32;
                    IntPtr replyBuffer = Marshal.AllocHGlobal(replySize);

                    var opts = new IP_OPTION_INFORMATION { Ttl = 128, Tos = 0, Flags = 0, OptionsSize = 0, OptionsData = IntPtr.Zero };

                    uint num = IcmpSendEcho2Ex(
                        handle,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        srcAddr,
                        ToUInt32(dest),
                        dataPtr,
                        (ushort)data.Length,
                        ref opts,
                        replyBuffer,
                        (uint)replySize,
                        (uint)timeoutMs);

                    try
                    {
                        if (num == 0)
                        {
                            int err = Marshal.GetLastWin32Error();
                            return (false, -1, $"ICMP erro {err}");
                        }

                        var reply = Marshal.PtrToStructure<ICMP_ECHO_REPLY>(replyBuffer);
                        bool ok = reply.Status == 0; // IP_SUCCESS
                        return (ok, ok ? reply.RoundTripTime : -1, ok ? null : $"Status {reply.Status}");
                    }
                    finally
                    {
                        Marshal.FreeHGlobal(replyBuffer);
                        Marshal.FreeHGlobal(dataPtr);
                    }
                }
                finally
                {
                    IcmpCloseHandle(handle);
                }
            }
            catch (Exception ex)
            {
                return (false, -1, ex.Message);
            }
        }

        private static IPAddress? ResolveIPv4(string host)
        {
            if (IPAddress.TryParse(host, out var ip) && ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                return ip;
            try
            {
                foreach (var addr in Dns.GetHostAddresses(host))
                {
                    if (addr.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        return addr;
                }
            }
            catch { }
            return null;
        }

        private static uint ToUInt32(IPAddress ip)
        {
            var bytes = ip.GetAddressBytes();
            // Convert big-endian bytes to UInt32
            return (uint)(bytes[0] << 24 | bytes[1] << 16 | bytes[2] << 8 | bytes[3]);
        }
    }
}
