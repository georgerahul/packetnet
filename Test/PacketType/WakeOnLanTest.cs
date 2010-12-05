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
 * Copyright 2010 Evan Plaice <evanplaice@gmail.com>
 * Copyright 2010 Chris Morgan <chmorgan@gmail.com>
 */

using System;
using System.Net.NetworkInformation;
using NUnit.Framework;
using SharpPcap;
using PacketDotNet;

namespace Test.PacketType
{
    [TestFixture]
    public class WakeOnLanTest
    {
        [Test]
        public void CreateFromValues()
        {
            var physicalAddress = PhysicalAddress.Parse("CA-FE-BA-BE-C0-01");
            var wol = new WakeOnLanPacket(physicalAddress);

            Assert.IsTrue(wol.IsValid());
            Assert.AreEqual(wol.DestinationMAC, physicalAddress);

            // convert the wol packet back into bytes
            var wolBytes = wol.Bytes;

            // and now parse it back from bytes into a wol packet
            var wol2 = new WakeOnLanPacket(wolBytes, 0);

            // make sure the packets match
            Assert.AreEqual(wol, wol2);
        }

        [Test]
        public void WakeOnLanParsing()
        {
            var dev = new OfflinePcapDevice("../../CaptureFiles/wol.pcap");
            dev.Open();

            SharpPcap.Packets.RawPacket rawPacket;

            int packetIndex = 0;
            while((rawPacket = dev.GetNextRawPacket()) != null)
            {
                var p = SharpPcapRawPacketToPacket.RawPacketToPacket(rawPacket);
                Assert.IsNotNull(p);

                var wol = PacketDotNet.WakeOnLanPacket.GetEncapsulated(p);
                Assert.IsNotNull(p);

                if(packetIndex == 0)
                    Assert.AreEqual(wol.DestinationMAC, PhysicalAddress.Parse("00-0D-56-DC-9E-35"));

                if(packetIndex == 3)
                    Assert.AreEqual(wol.DestinationMAC, PhysicalAddress.Parse("00-90-27-85-CF-01"));

                packetIndex++;
            }
            dev.Close();
        }
    }
}