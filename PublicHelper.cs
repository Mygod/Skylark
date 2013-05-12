using System;
using System.Net;
using System.Net.Sockets;
using Microsoft.Win32;

namespace Mygod.Skylark
{
    public static partial class Helper
    {
        public static string GetDefaultExtension(string mimeType)
        {
            try
            {
                var key = Registry.ClassesRoot.OpenSubKey(@"MIME\Database\Content Type\" + mimeType, false);
                var value = key != null ? key.GetValue("Extension", null) : null;
                return value != null ? value.ToString() : null;
            }
            catch
            {
                return null;
            }
        }

        public static string GetMime(string contentType)
        {
            try
            {
                return contentType.Split(';')[0]; // kill that "; charset=utf-8" stupid stuff
            }
            catch
            {
                return contentType;
            }
        }

        private static TimeSpan? offset;
        private static TimeSpan Offset
        {
            get { return (offset ?? (offset = NtpClient.GetNetworkTime("time.windows.com") - DateTime.UtcNow)).Value; }
        }
        public static DateTime UtcNow { get { return DateTime.UtcNow.ToSuper(); } }

        public static DateTime ToSuper(this DateTime value)
        {
            return DateTime.SpecifyKind(value.Add(Offset), DateTimeKind.Unspecified);
        }
    }

    /// <summary>
    /// Static class to receive the time from a NTP server.
    /// </summary>
    public static class NtpClient
    {
        /// <summary>
        /// Gets the current DateTime from <paramref name="ntpServer"/>.
        /// </summary>
        /// <param name="ntpServer">The hostname of the NTP server.</param>
        /// <returns>A DateTime containing the current time.</returns>
        public static DateTime GetNetworkTime(string ntpServer)
        {
            var address = Dns.GetHostEntry(ntpServer).AddressList;
            if (address == null || address.Length == 0)
                throw new ArgumentException("Could not resolve ip address from '" + ntpServer + "'.", "ntpServer");
            var ep = new IPEndPoint(address[0], 123);
            return GetNetworkTime(ep);
        }

        /// <summary>
        /// Gets the current DateTime form <paramref name="ep"/> IPEndPoint.
        /// </summary>
        /// <param name="ep">The IPEndPoint to connect to.</param>
        /// <returns>A DateTime containing the current time.</returns>
        private static DateTime GetNetworkTime(EndPoint ep)
        {
            var s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.Connect(ep);
            var ntpData = new byte[48]; // RFC 2030 
            ntpData[0] = 0x1B;
            for (var i = 1; i < 48; i++) ntpData[i] = 0;
            s.Send(ntpData);
            s.Receive(ntpData);
            const byte offsetTransmitTime = 40;
            ulong intpart = 0;
            ulong fractpart = 0;
            for (var i = 0; i <= 3; i++) intpart = 256 * intpart + ntpData[offsetTransmitTime + i];
            for (var i = 4; i <= 7; i++) fractpart = 256 * fractpart + ntpData[offsetTransmitTime + i];
            var milliseconds = (intpart * 1000 + fractpart * 1000 / 0x100000000L);
            s.Close();
            return new DateTime(1900, 1, 1) + TimeSpan.FromTicks((long)milliseconds * TimeSpan.TicksPerMillisecond);
        }
    }
}