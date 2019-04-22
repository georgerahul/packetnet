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
using System.Runtime.Serialization.Formatters.Binary;
using NUnit.Framework;
using PacketDotNet;
using SharpPcap;
using SharpPcap.LibPcap;

namespace Test.PacketType
{
    [TestFixture]
    public class IcmpV6PacketTest
    {
        [Test]
        public void BinarySerialization()
        {
            var dev = new CaptureFileReaderDevice("../../CaptureFiles/ipv6_icmpv6_packet.pcap");
            dev.Open();

            RawCapture rawCapture;
            var foundicmpv6 = false;
            while ((rawCapture = dev.GetNextPacket()) != null)
            {
                var p = Packet.ParsePacket(rawCapture.LinkLayerType, rawCapture.Data);
                var icmpv6 = p.Extract<IcmpV6Packet>();
                if (icmpv6 == null)
                {
                    continue;
                }

                foundicmpv6 = true;

                Stream outFile = File.Create("icmpv6.dat");
                var serializer = new BinaryFormatter();
                serializer.Serialize(outFile, icmpv6);
                outFile.Close();

                Stream inFile = File.OpenRead("icmpv6.dat");
                var deserializer = new BinaryFormatter();
                var fromFile = (IcmpV6Packet) deserializer.Deserialize(inFile);
                inFile.Close();

                Assert.AreEqual(icmpv6.Bytes, fromFile.Bytes);
                Assert.AreEqual(icmpv6.BytesSegment.Bytes, fromFile.BytesSegment.Bytes);
                Assert.AreEqual(icmpv6.BytesSegment.BytesLength, fromFile.BytesSegment.BytesLength);
                Assert.AreEqual(icmpv6.BytesSegment.Length, fromFile.BytesSegment.Length);
                Assert.AreEqual(icmpv6.BytesSegment.NeedsCopyForActualBytes, fromFile.BytesSegment.NeedsCopyForActualBytes);
                Assert.AreEqual(icmpv6.BytesSegment.Offset, fromFile.BytesSegment.Offset);
                Assert.AreEqual(icmpv6.Checksum, fromFile.Checksum);
                Assert.AreEqual(icmpv6.Color, fromFile.Color);
                Assert.AreEqual(icmpv6.HeaderData, fromFile.HeaderData);
                Assert.AreEqual(icmpv6.PayloadData, fromFile.PayloadData);
                Assert.AreEqual(icmpv6.Code, fromFile.Code);
                Assert.AreEqual(icmpv6.Type, fromFile.Type);
            }

            dev.Close();
            Assert.IsTrue(foundicmpv6, "Capture file contained no icmpv6 packets");
        }

        /// <summary>
        /// Test that the checksum can be recalculated properly
        /// </summary>
        [Test]
        public void Checksum()
        {
            var dev = new CaptureFileReaderDevice("../../CaptureFiles/ipv6_icmpv6_packet.pcap");
            dev.Open();
            var rawCapture = dev.GetNextPacket();
            dev.Close();

            var p = Packet.ParsePacket(rawCapture.LinkLayerType, rawCapture.Data);

            // save the checksum
            var icmpv6 = p.Extract<IcmpV6Packet>();
            Assert.IsNotNull(icmpv6);
            var savedChecksum = icmpv6.Checksum;

            // now zero the checksum out
            icmpv6.Checksum = 0;

            // and recalculate the checksum
            icmpv6.UpdateCalculatedValues();

            // compare the checksum values to ensure that they match
            Assert.AreEqual(savedChecksum, icmpv6.Checksum);
        }

        /// <summary>
        /// Test that we can parse a icmp v4 request and reply
        /// </summary>
        [Test]
        public void IcmpV6Parsing()
        {
            var dev = new CaptureFileReaderDevice("../../CaptureFiles/ipv6_icmpv6_packet.pcap");
            dev.Open();
            var rawCapture = dev.GetNextPacket();
            dev.Close();

            var p = Packet.ParsePacket(rawCapture.LinkLayerType, rawCapture.Data);

            Assert.IsNotNull(p);

            var icmpv6 = p.Extract<IcmpV6Packet>();
            Console.WriteLine(icmpv6.GetType());

            Assert.AreEqual(IcmpV6Types.RouterSolicitation, icmpv6.Type);
            Assert.AreEqual(0, icmpv6.Code);
            Assert.AreEqual(0x5d50, icmpv6.Checksum);

            // Payload differs based on the icmp.Type field
        }

        [Test]
        public void PrintString()
        {
            Console.WriteLine("Loading the sample capture file");
            var dev = new CaptureFileReaderDevice("../../CaptureFiles/ipv6_icmpv6_packet.pcap");
            dev.Open();
            Console.WriteLine("Reading packet data");
            var rawCapture = dev.GetNextPacket();
            var p = Packet.ParsePacket(rawCapture.LinkLayerType, rawCapture.Data);

            Console.WriteLine("Parsing");
            var icmpv6 = p.Extract<IcmpV6Packet>();

            Console.WriteLine("Printing human readable string");
            Console.WriteLine(icmpv6.ToString());
        }

        [Test]
        public void PrintVerboseString()
        {
            Console.WriteLine("Loading the sample capture file");
            var dev = new CaptureFileReaderDevice("../../CaptureFiles/ipv6_icmpv6_packet.pcap");
            dev.Open();
            Console.WriteLine("Reading packet data");
            var rawCapture = dev.GetNextPacket();
            var p = Packet.ParsePacket(rawCapture.LinkLayerType, rawCapture.Data);

            Console.WriteLine("Parsing");
            var icmpV6 = p.Extract<IcmpV6Packet>();

            Console.WriteLine("Printing human readable string");
            Console.WriteLine(icmpV6.ToString(StringOutputType.Verbose));
        }
    }
}