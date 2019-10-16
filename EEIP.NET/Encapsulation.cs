using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace Sres.Net.EEIP
{
    public class Encapsulation
    {
        public CommandsEnum Command { get; set; }
        public UInt16 Length { get; set; }
        public UInt32 SessionHandle { get; set; }
        public StatusEnum Status { get; }
        private byte[] SenderContext = new byte[8];
        private UInt32 Options = 0;
        public List<byte> CommandSpecificData = new List<byte>();

        /// <summary>
        /// Table 2-3.3 Error Codes
        /// </summary>
        public enum StatusEnum : UInt32
        {
            Success = 0x0000,
            InvalidCommand = 0x0001,
            InsufficientMemory = 0x0002,
            IncorrectData = 0x0003,
            InvalidSessionHandle = 0x0064,
            InvalidLength = 0x0065,
            UnsupportedEncapsulationProtocol = 0x0069
        }

        /// <summary>
        /// Table 2-3.2 Encapsulation Commands
        /// </summary>
        public enum CommandsEnum : UInt16
        {
            NOP = 0x0000,
            ListServices = 0x0004,
            ListIdentity = 0x0063,
            ListInterfaces = 0x0064,
            RegisterSession = 0x0065,
            UnRegisterSession = 0x0066,
            SendRRData = 0x006F,
            SendUnitData = 0x0070,
            IndicateStatus = 0x0072,
            Cancel = 0x0073
        }

        public byte[] ToBytes()
        {
            var data = new List<byte>();
            data.AddRange(BitConverter.GetBytes((UInt16)this.Command));
            data.AddRange(BitConverter.GetBytes((UInt16)this.Length));
            data.AddRange(BitConverter.GetBytes((UInt32)this.SessionHandle));
            data.AddRange(BitConverter.GetBytes((UInt32)this.Status));
            data.AddRange(SenderContext);
            data.AddRange(BitConverter.GetBytes((UInt32)this.Options));
            data.AddRange(CommandSpecificData);

            return data.ToArray();
        }

        /// <summary>
        /// Table 2-4.4 CIP Identity Item
        /// </summary>
        public class CIPIdentityItem
        {
            public UInt16 ItemTypeCode;                                     //Code indicating item type of CIP Identity (0x0C)
            public UInt16 ItemLength;                                       //Number of bytes in item which follow (length varies depending on Product Name string)
            public UInt16 EncapsulationProtocolVersion;                     //Encapsulation Protocol Version supported (also returned with Register Sesstion reply).
            public SocketAddress SocketAddress = new SocketAddress();       //Socket Address (see section 2-6.3.2)
            public UInt16 VendorID1;                                        //Device manufacturers Vendor ID
            public UInt16 DeviceType1;                                      //Device Type of product
            public UInt16 ProductCode1;                                     //Product Code assigned with respect to device type
            public byte[] Revision1 = new byte[2];                          //Device revision
            public UInt16 Status1;                                          //Current status of device
            public UInt32 SerialNumber1;                                      //Serial number of device
            public byte ProductNameLength;                          
            public string ProductName1;                                     //Human readable description of device
            public byte State1;                                             //Current state of device

            public static CIPIdentityItem getCIPIdentityItem(int startingByte, byte[] receivedData)
            {
                startingByte += 2;            //Skipped ItemCount
                CIPIdentityItem cipIdentityItem = new CIPIdentityItem();
                cipIdentityItem.ItemTypeCode = Convert.ToUInt16(receivedData[0+startingByte]
                                                                    | (receivedData[1 + startingByte] << 8));
                cipIdentityItem.ItemLength = Convert.ToUInt16(receivedData[2 + startingByte]
                                                                    | (receivedData[3 + startingByte] << 8));
                cipIdentityItem.EncapsulationProtocolVersion = Convert.ToUInt16(receivedData[4 + startingByte]
                                                                    | (receivedData[5 + startingByte] << 8));
                cipIdentityItem.SocketAddress.SIN_family = Convert.ToUInt16(receivedData[7 + startingByte]
                                                    | (receivedData[6 + startingByte] << 8));
                cipIdentityItem.SocketAddress.SIN_port = Convert.ToUInt16(receivedData[9 + startingByte]
                                                    | (receivedData[8 + startingByte] << 8));
                cipIdentityItem.SocketAddress.SIN_Address = (UInt32)(receivedData[13 + startingByte]
                                                    | (receivedData[12 + startingByte] << 8)
                                                    | (receivedData[11 + startingByte] << 16)
                                                    | (receivedData[10 + startingByte] << 24)
                                                    );
                cipIdentityItem.VendorID1 = Convert.ToUInt16(receivedData[22 + startingByte]
                                    | (receivedData[23 + startingByte] << 8));
                cipIdentityItem.DeviceType1 = Convert.ToUInt16(receivedData[24 + startingByte]
                                    | (receivedData[25 + startingByte] << 8));
                cipIdentityItem.ProductCode1 = Convert.ToUInt16(receivedData[26 + startingByte]
                    | (receivedData[27 + startingByte] << 8));
                cipIdentityItem.Revision1[0] = receivedData[28 + startingByte];
                cipIdentityItem.Revision1[1] = receivedData[29 + startingByte];
                cipIdentityItem.Status1 = Convert.ToUInt16(receivedData[30 + startingByte]
                    | (receivedData[31 + startingByte] << 8));
                cipIdentityItem.SerialNumber1 = (UInt32)(receivedData[32 + startingByte]
                                                    | (receivedData[33 + startingByte] << 8)
                                                    | (receivedData[34 + startingByte] << 16)
                                                    | (receivedData[35 + startingByte] << 24));
                cipIdentityItem.ProductNameLength = receivedData[36 + startingByte];
                cipIdentityItem.ProductName1 = Encoding.ASCII.GetString(receivedData, 37 + startingByte, cipIdentityItem.ProductNameLength);
                cipIdentityItem.State1 = receivedData[receivedData.Length - 1];
                return cipIdentityItem;
            }
            /// <summary>
            /// Converts an IP-Address in UIint32 Format (Received by Device)
            /// </summary>
            public static string getIPAddress(UInt32 address)
            {
                return ((byte)(address >> 24)).ToString()+"." + ((byte)(address >> 16)).ToString()+"."+((byte)(address >> 8)).ToString()+"."+((byte)(address)).ToString();
            }
        }

        /// <summary>
        /// Socket Address (see section 2-6.3.2)
        /// </summary>
        public class SocketAddress
        {
            public UInt16 SIN_family;
            public UInt16 SIN_port;
            public UInt32 SIN_Address;
            public byte[] SIN_Zero = new byte[8];
        }

        public class CommonPacketFormat
        {
            public UInt16 ItemCount = 2;
            public UInt16 AddressTypeID = 0x0000;
            public UInt16 AddressLength = 0;
            public List<byte> AddressData = new List<byte>();
            public UInt16 DataTypeID = 0xB2; //0xB2 = Unconnected Data Item
            public UInt16 DataLength = 8;
            public UInt16? SequenceNumber = null;
            public List<byte> Data = new List<byte>();
            public UInt16 SockaddrInfoItem_O_T = 0x8001; //8000 for O->T and 8001 for T->O - Volume 2 Table 2-6.9
            public UInt16 SockaddrInfoLength = 16;
            public SocketAddress SocketaddrInfo_O_T = null;

            public byte[] ToBytes()
            {
                var returnValue = new List<byte>();

                if (SocketaddrInfo_O_T != null)
                    this.ItemCount=0x03;
                returnValue.AddRange(BitConverter.GetBytes((UInt16)(this.ItemCount)));
                returnValue.AddRange(BitConverter.GetBytes((UInt16)this.AddressTypeID));
                returnValue.AddRange(BitConverter.GetBytes((UInt16)this.AddressLength));
                returnValue.AddRange(AddressData);
                returnValue.AddRange(BitConverter.GetBytes((UInt16)this.DataTypeID));
                returnValue.AddRange(BitConverter.GetBytes((UInt16)this.DataLength));
                if (this.SequenceNumber.HasValue)
                {
                    returnValue.AddRange(BitConverter.GetBytes((UInt16)SequenceNumber));
                }
                returnValue.AddRange(Data);

                // Add Socket Address Info Item
                if (SocketaddrInfo_O_T != null)
                {
                    returnValue.AddRange(BitConverter.GetBytes((UInt16)this.SockaddrInfoItem_O_T));
                    returnValue.AddRange(BitConverter.GetBytes((UInt16)this.SockaddrInfoLength));
                    returnValue.AddRange(BitConverter.GetBytes((UInt16)this.SocketaddrInfo_O_T.SIN_family));
                    returnValue.AddRange(BitConverter.GetBytes((UInt16)this.SocketaddrInfo_O_T.SIN_port));
                    returnValue.AddRange(BitConverter.GetBytes((UInt32)this.SocketaddrInfo_O_T.SIN_Address));
                    returnValue.AddRange(this.SocketaddrInfo_O_T.SIN_Zero);
                }
                
                return returnValue.ToArray();
            }
        }
    }
}
