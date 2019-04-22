/*
This file is part of PacketDotNet

PacketDotNet is free software: you can redistribute it and/or modify
it under the terms of the GNU Lesser General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

PacketDotNet is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU Lesser General Public License for more details.

You should have received a copy of the GNU Lesser General Public License
along with PacketDotNet.  If not, see <http://www.gnu.org/licenses/>.
*/
/*
 *  Copyright 2010 Chris Morgan <chmorgan@gmail.com>
 */

using System;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Runtime.Serialization.Formatters.Binary;
using NUnit.Framework;
using PacketDotNet;
using PacketDotNet.Utils;
using SharpPcap;
using SharpPcap.LibPcap;

namespace Test.PacketType
{
    [TestFixture]
    public class IPv6PacketTest
    {
        // icmpv6
        public void VerifyPacket0(Packet p, RawCapture rawCapture, LinkLayers linkLayers)
        {
            Assert.IsNotNull(p);
            Console.WriteLine(p.ToString());

            Assert.AreEqual(linkLayers, rawCapture.LinkLayerType);

            if (linkLayers == LinkLayers.Ethernet)
            {
                var e = (EthernetPacket) p;
                Assert.AreEqual(PhysicalAddress.Parse("00-A0-CC-D9-41-75"), e.SourceHardwareAddress);
                Assert.AreEqual(PhysicalAddress.Parse("33-33-00-00-00-02"), e.DestinationHardwareAddress);
            }

            var ip = p.Extract<IPPacket>();
            Console.WriteLine("ip {0}", ip);
            Assert.AreEqual(IPAddress.Parse("fe80::2a0:ccff:fed9:4175"), ip.SourceAddress);
            Assert.AreEqual(IPAddress.Parse("ff02::2"), ip.DestinationAddress);
            Assert.AreEqual(IPVersion.IPv6, ip.Version);
            Assert.AreEqual(ProtocolType.IcmpV6, ip.Protocol);
            Assert.AreEqual(16, ip.PayloadPacket.Bytes.Length, "ip.PayloadPacket.Bytes.Length mismatch");
            Assert.AreEqual(255, ip.HopLimit);
            Assert.AreEqual(255, ip.TimeToLive);
            Console.WriteLine("Failed: ip.ComputeIPChecksum() not implemented.");
            Assert.AreEqual(1221145299, rawCapture.Timeval.Seconds);
            Assert.AreEqual(453568.000, rawCapture.Timeval.MicroSeconds);
        }

        public void VerifyPacket1(Packet p, RawCapture rawCapture, LinkLayers linkLayers)
        {
            Assert.IsNotNull(p);
            Console.WriteLine(p.ToString());

            Assert.AreEqual(linkLayers, rawCapture.LinkLayerType);

            if (linkLayers == LinkLayers.Ethernet)
            {
                var e = (EthernetPacket) p;
                Assert.AreEqual(PhysicalAddress.Parse("F894C22EFAD1"), e.SourceHardwareAddress);
                Assert.AreEqual(PhysicalAddress.Parse("333300000016"), e.DestinationHardwareAddress);
            }

            var ip = p.Extract<IPv6Packet>();
            Console.WriteLine("ip {0}", ip);
            Assert.AreEqual(IPAddress.Parse("fe80::d802:3589:15cf:3128"), ip.SourceAddress);
            Assert.AreEqual(IPAddress.Parse("ff02::16"), ip.DestinationAddress);
            Assert.AreEqual(IPVersion.IPv6, ip.Version);
            Assert.AreEqual(ProtocolType.IcmpV6, ip.Protocol);
            Assert.AreEqual(28, ip.PayloadPacket.Bytes.Length, "ip.PayloadPacket.Bytes.Length mismatch");
            Assert.AreEqual(1, ip.HopLimit);
            Assert.AreEqual(1, ip.TimeToLive);
            Assert.AreEqual(0x3a, (byte) ip.Protocol);
            Console.WriteLine("Failed: ip.ComputeIPChecksum() not implemented.");
            Assert.AreEqual(1543415539, rawCapture.Timeval.Seconds);
            Assert.AreEqual(841441.000, rawCapture.Timeval.MicroSeconds);
            Assert.AreEqual(1, ip.ExtensionHeaders.Count);
            Assert.AreEqual(6, ip.ExtensionHeaders[0].Payload.Length);
        }

        // Test that we can load and parse an IPv6 packet
        // for multiple LinkLayers types
        [TestCase("../../CaptureFiles/ipv6_icmpv6_packet.pcap", LinkLayers.Ethernet)]
        [TestCase("../../CaptureFiles/ipv6_icmpv6_packet_raw_linklayer.pcap", LinkLayers.RawLegacy)]
        public void IPv6PacketTestParsing(string pcapPath, LinkLayers linkLayers)
        {
            var dev = new CaptureFileReaderDevice(pcapPath);
            dev.Open();

            RawCapture rawCapture;
            var packetIndex = 0;
            while ((rawCapture = dev.GetNextPacket()) != null)
            {
                var p = Packet.ParsePacket(rawCapture.LinkLayerType, rawCapture.Data);
                Console.WriteLine("got packet");
                switch (packetIndex)
                {
                    case 0:
                    {
                        VerifyPacket0(p, rawCapture, linkLayers);
                        break;
                    }
                    default:
                    {
                        Assert.Fail("didn't expect to get to packetIndex " + packetIndex);
                        break;
                    }
                }

                packetIndex++;
            }

            dev.Close();
        }

        // Test that we can load and parse an with IPv6 packet with an extended (Hop by Hop) header 
        [TestCase("../../CaptureFiles/ipv6_icmpv6_hopbyhop_packet.pcap", LinkLayers.Ethernet)]
        public void IPv6PacketHopByHopTestParsing(string pcapPath, LinkLayers linkLayers)
        {
            var dev = new CaptureFileReaderDevice(pcapPath);
            dev.Open();

            RawCapture rawCapture;
            var packetIndex = 0;
            while ((rawCapture = dev.GetNextPacket()) != null)
            {
                var p = Packet.ParsePacket(rawCapture.LinkLayerType, rawCapture.Data);
                switch (packetIndex)
                {
                    case 0:
                    {
                        VerifyPacket1(p, rawCapture, linkLayers);
                        break;
                    }
                    default:
                    {
                        Assert.Fail("didn't expect to get to packetIndex " + packetIndex);
                        break;
                    }
                }

                packetIndex++;
            }

            dev.Close();
        }

        [Test]
        public void BinarySerialization()
        {
            var dev = new CaptureFileReaderDevice("../../CaptureFiles/ipv6_icmpv6_packet.pcap");
            dev.Open();

            RawCapture rawCapture;
            var foundipv6 = false;
            while ((rawCapture = dev.GetNextPacket()) != null)
            {
                var p = Packet.ParsePacket(rawCapture.LinkLayerType, rawCapture.Data);
                var ip = p.Extract<IPv6Packet>();
                if (ip == null)
                {
                    continue;
                }

                foundipv6 = true;

                var memoryStream = new MemoryStream();
                var serializer = new BinaryFormatter();
                serializer.Serialize(memoryStream, ip);

                memoryStream.Seek(0, SeekOrigin.Begin);
                var deserializer = new BinaryFormatter();
                var fromFile = (IPv6Packet) deserializer.Deserialize(memoryStream);

                Assert.AreEqual(ip.Bytes, fromFile.Bytes);
                Assert.AreEqual(ip.BytesSegment.Bytes, fromFile.BytesSegment.Bytes);
                Assert.AreEqual(ip.BytesSegment.BytesLength, fromFile.BytesSegment.BytesLength);
                Assert.AreEqual(ip.BytesSegment.Length, fromFile.BytesSegment.Length);
                Assert.AreEqual(ip.BytesSegment.NeedsCopyForActualBytes, fromFile.BytesSegment.NeedsCopyForActualBytes);
                Assert.AreEqual(ip.BytesSegment.Offset, fromFile.BytesSegment.Offset);
                Assert.AreEqual(ip.Color, fromFile.Color);
                Assert.AreEqual(ip.HeaderData, fromFile.HeaderData);
                Assert.AreEqual(ip.PayloadData, fromFile.PayloadData);
                Assert.AreEqual(ip.DestinationAddress, fromFile.DestinationAddress);
                Assert.AreEqual(ip.HeaderLength, fromFile.HeaderLength);
                Assert.AreEqual(ip.HopLimit, fromFile.HopLimit);
                Assert.AreEqual(ip.NextHeader, fromFile.NextHeader);
                Assert.AreEqual(ip.PayloadLength, fromFile.PayloadLength);
                Assert.AreEqual(ip.Protocol, fromFile.Protocol);
                Assert.AreEqual(ip.SourceAddress, fromFile.SourceAddress);
                Assert.AreEqual(ip.TimeToLive, fromFile.TimeToLive);
                Assert.AreEqual(ip.TotalLength, fromFile.TotalLength);
                Assert.AreEqual(ip.Version, fromFile.Version);
                Assert.AreEqual(ip.FlowLabel, fromFile.FlowLabel);
                Assert.AreEqual(ip.TrafficClass, fromFile.TrafficClass);

                //Method Invocations to make sure that a deserialized packet does not cause 
                //additional errors.

                ip.PrintHex();
                ip.UpdateCalculatedValues();
            }

            dev.Close();
            Assert.IsTrue(foundipv6, "Capture file contained no ipv6 packets");
        }

        [Test]
        public void ConstructFromValues()
        {
            var sourceAddress = RandomUtils.GetIPAddress(IPVersion.IPv6);
            var destinationAddress = RandomUtils.GetIPAddress(IPVersion.IPv6);
            var ipPacket = new IPv6Packet(sourceAddress, destinationAddress);

            Assert.AreEqual(sourceAddress, ipPacket.SourceAddress);
            Assert.AreEqual(destinationAddress, ipPacket.DestinationAddress);
        }

        [Test]
        public void PrintString()
        {
            Console.WriteLine("Loading the sample capture file");
            var dev = new CaptureFileReaderDevice("../../CaptureFiles/ipv6_http.pcap");
            dev.Open();
            Console.WriteLine("Reading packet data");
            var rawCapture = dev.GetNextPacket();
            var p = Packet.ParsePacket(rawCapture.LinkLayerType, rawCapture.Data);

            Console.WriteLine("Parsing");
            var ip = p.Extract<IPv6Packet>();

            Console.WriteLine("Printing human readable string");
            Console.WriteLine(ip.ToString());
        }

        [Test]
        public void PrintVerboseString()
        {
            Console.WriteLine("Loading the sample capture file");
            var dev = new CaptureFileReaderDevice("../../CaptureFiles/ipv6_http.pcap");
            dev.Open();
            Console.WriteLine("Reading packet data");
            var rawCapture = dev.GetNextPacket();
            var p = Packet.ParsePacket(rawCapture.LinkLayerType, rawCapture.Data);

            Console.WriteLine("Parsing");
            var ip = p.Extract<IPv6Packet>();

            Console.WriteLine("Printing human readable string");
            Console.WriteLine(ip.ToString(StringOutputType.Verbose));
        }

        [Test]
        public void RandomPacket()
        {
            IPv6Packet.RandomPacket();
        }

        /// <summary>
        /// Test that we can load and parse an IPv6 TCP packet and that
        /// the computed tcp checksum matches the expected checksum
        /// </summary>
        [Test]
        public void TCPChecksumIPv6()
        {
            var dev = new CaptureFileReaderDevice("../../CaptureFiles/ipv6_http.pcap");
            dev.Open();

            // checksums from wireshark of the capture file
            int[] expectedChecksum =
            {
                0x41a2,
                0x4201,
                0x5728,
                0xf448,
                0xee07,
                0x939c,
                0x63e4,
                0x4590,
                0x3725,
                0x3723
            };

            var packetIndex = 0;
            RawCapture rawCapture;
            while ((rawCapture = dev.GetNextPacket()) != null)
            {
                var p = Packet.ParsePacket(rawCapture.LinkLayerType, rawCapture.Data);
                var t = p.Extract<TcpPacket>();
                Assert.IsNotNull(t, "Expected t to not be null");
                Assert.IsTrue(t.ValidChecksum, "t.ValidChecksum isn't true");

                // compare the computed checksum to the expected one
                Assert.AreEqual(expectedChecksum[packetIndex],
                                t.CalculateTcpChecksum(),
                                "Checksum mismatch");

                packetIndex++;
            }

            dev.Close();
        }

        // Test that we can correctly set the data section of a IPv6 packet
        [Test]
        public void TCPDataIPv6()
        {
            var s = "-++++=== HELLLLOOO ===++++-";
            var data = System.Text.Encoding.UTF8.GetBytes(s);

            //create random packet
            var p = TcpPacket.RandomPacket();

            //replace pkt's data with our string
            p.PayloadData = data;

            //sanity check
            Assert.AreEqual(s, System.Text.Encoding.Default.GetString(p.PayloadData));
        }
    }
}