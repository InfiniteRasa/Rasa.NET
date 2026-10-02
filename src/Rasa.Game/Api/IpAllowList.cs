using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

namespace Rasa.Api
{
    /// <summary>
    /// The addresses a listener answers: single addresses and CIDR ranges, IPv4 or IPv6. A list
    /// with nothing in it answers everybody. An entry that cannot be read matches nobody, and
    /// does not make the list empty: a list of one mistyped address answers no one, rather than
    /// everyone.
    /// </summary>
    public sealed class IpAllowList
    {
        private readonly List<(byte[] Network, int PrefixLength)> _entries = new List<(byte[], int)>();

        /// <summary>Nothing was configured: every address is answered.</summary>
        public bool AllowsAll { get; private set; }

        /// <summary>The entries that are no address and no range.</summary>
        public IReadOnlyList<string> Invalid { get; private set; } = Array.Empty<string>();

        /// <summary>How many addresses and ranges are on the list.</summary>
        public int Count => _entries.Count;

        /// <summary>Everybody.</summary>
        public static IpAllowList Open { get; } = Parse(null);

        /// <summary>
        /// Reads the configured entries. One entry may hold several addresses, separated by
        /// commas, semicolons or spaces; blank ones are nothing.
        /// </summary>
        public static IpAllowList Parse(IEnumerable<string> configured)
        {
            var list = new IpAllowList();
            var invalid = new List<string>();
            var any = false;

            var entries = (configured ?? Enumerable.Empty<string>())
                .Where(entry => entry != null)
                .SelectMany(entry => entry.Split(new[] { ',', ';', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries));

            foreach (var entry in entries)
            {
                any = true;

                if (TryParseEntry(entry, out var network, out var prefixLength))
                    list._entries.Add((network, prefixLength));
                else
                    invalid.Add(entry);
            }

            list.AllowsAll = !any;
            list.Invalid = invalid;

            return list;
        }

        private static bool TryParseEntry(string entry, out byte[] network, out int prefixLength)
        {
            network = null;
            prefixLength = 0;

            var slash = entry.IndexOf('/');
            var host = slash < 0 ? entry : entry.Substring(0, slash);

            if (!IPAddress.TryParse(host, out var address))
                return false;

            // "::ffff:10.0.0.1" is 10.0.0.1, whose range lengths are counted in its 32 bits.
            var mapped = address.IsIPv4MappedToIPv6;
            address = Normalize(address);
            network = address.GetAddressBytes();
            prefixLength = network.Length * 8;

            if (slash < 0)
                return true;

            if (!int.TryParse(entry.Substring(slash + 1), out var length))
                return false;

            if (mapped)
                length -= 96;

            if (length < 0 || length > network.Length * 8)
                return false;

            prefixLength = length;

            return true;
        }

        /// <summary>An IPv4 address that arrived on an IPv6 socket, as the IPv4 address it is.</summary>
        public static IPAddress Normalize(IPAddress address)
        {
            return address != null && address.IsIPv4MappedToIPv6 ? address.MapToIPv4() : address;
        }

        /// <summary>Whether an address is answered.</summary>
        public bool Allows(IPAddress address)
        {
            if (AllowsAll)
                return true;

            if (address == null)
                return false;

            address = Normalize(address);

            if (address.AddressFamily != AddressFamily.InterNetwork && address.AddressFamily != AddressFamily.InterNetworkV6)
                return false;

            var bytes = address.GetAddressBytes();

            foreach (var (network, prefixLength) in _entries)
                if (network.Length == bytes.Length && SamePrefix(network, bytes, prefixLength))
                    return true;

            return false;
        }

        private static bool SamePrefix(byte[] network, byte[] address, int prefixLength)
        {
            var whole = prefixLength / 8;

            for (var i = 0; i < whole; i++)
                if (network[i] != address[i])
                    return false;

            var rest = prefixLength % 8;

            if (rest == 0)
                return true;

            var mask = (byte)(0xFF << (8 - rest));

            return (network[whole] & mask) == (address[whole] & mask);
        }
    }
}
