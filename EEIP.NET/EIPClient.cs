using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sres.Net.EEIP
{
    public class EEIPClient
    {
        TcpClient client;
        NetworkStream stream;
        protected UInt32 sessionHandle;
        private UInt32 connectionID_O_T;
        private UInt32 connectionID_T_O;
        private UInt32 multicastAddress;
        protected UInt16 ConnectionSerialNumber;
        public bool IsRegistered { get => this.sessionHandle != 0; }
        public bool IsConnected { get => this.ConnectionSerialNumber != 0; }
        private UInt16? ConnectionSequenceCounter = 0;
        private UInt64 context = 0x00000000;
        public ushort VendorID = 0x00FF; //Pretend to be Bosch Rexroth I guess
        public uint SerialNumber = 0xFFFF;
        public uint ConnectionSize = 504;
        public List<byte> ConnectionPath = new List<byte>();
        /// <summary>
        /// TCP-Port of the Server - Standard is 0xAF12 44818
        /// </summary>
        public ushort TCPPort { get; set; } = 0xAF12;
        public int TCPClientTimeout { get; set; } = 0;
        /// <summary>
        /// UDP-Port of the IO-Adapter - Standard is 0x08AE 2222
        /// </summary>
        public ushort TargetUDPPort { get; set; } = 0x08AE;
        /// <summary>
        /// UDP-Port of the Scanner - Standard is 0x08AE 2222
        /// </summary>
        public ushort OriginatorUDPPort { get; set; } = 0x08AE;
        /// <summary>
        /// IPAddress of the Ethernet/IP Device
        /// </summary>
        public string IPAddress { get; set; } = "172.0.0.1";
        /// <summary>
        ///X------- = 0= Client; 1= Server
        ///-XXX---- = Production Trigger, 0 = Cyclic, 1 = CoS, 2 = Application Object
        ///----XXXX = Transport class, 0 = Class 0, 1 = Class 1, 2 = Class 2, 3 = Class 3
        ///----------------Transport Type Trigger
        /// </summary>
        public byte TransportType { get; set; } = 0x01;
        public int TransportClass { get => (TransportType & 0x3); }
        /// <summary>
        /// Requested Packet Rate (RPI) in Microseconds Originator -> Target for Implicit-Messaging (Default 0x7A120 -> 500ms)
        /// </summary>
        public UInt32 RequestedPacketRate_O_T { get; set; } = 0x7A120;      //500ms
        /// <summary>
        /// Requested Packet Rate (RPI) in Microseconds Target -> Originator for Implicit-Messaging (Default 0x7A120 -> 500ms)
        /// </summary>
        public UInt32 RequestedPacketRate_T_O { get; set; } = 0x7A120;      //500ms
        /// <summary>
        /// "1" Indicates that multiple connections are allowed Originator -> Target for Implicit-Messaging (Default: TRUE) 
        /// </summary>
        public bool O_T_OwnerRedundant { get; set; } = true;                //For Forward Open
        /// <summary>
        /// "1" Indicates that multiple connections are allowed Target -> Originator for Implicit-Messaging (Default: TRUE) 
        /// </summary>
        public bool T_O_OwnerRedundant { get; set; } = true;                //For Forward Open
        /// <summary>
        /// With a fixed size connection, the amount of data shall be the size of specified in the "Connection Size" Parameter.
        /// With a variable size, the amount of data could be up to the size specified in the "Connection Size" Parameter
        /// Originator -> Target for Implicit Messaging (Default: True (Variable length))
        /// </summary>
        public bool O_T_VariableLength { get; set; } = true;                //For Forward Open
        /// <summary>
        /// With a fixed size connection, the amount of data shall be the size of specified in the "Connection Size" Parameter.
        /// With a variable size, the amount of data could be up to the size specified in the "Connection Size" Parameter
        /// Target -> Originator for Implicit Messaging (Default: True (Variable length))
        /// </summary>
        public bool T_O_VariableLength { get; set; } = true;                //For Forward Open
        /// <summary>
        /// The maximum size in bytes (only pure data without sequence count and 32-Bit Real Time Header (if present)) from Originator -> Target for Implicit Messaging (Default: 505)
        /// </summary>
        public UInt16 O_T_Length { get; set; } = 505;                //For Forward Open - Max 505
        /// <summary>
        /// The maximum size in bytes (only pure data woithout sequence count and 32-Bit Real Time Header (if present)) from Target -> Originator for Implicit Messaging (Default: 505)
        /// </summary>
        public UInt16 T_O_Length { get; set; } = 505;                //For Forward Open - Max 505
        /// <summary>
        /// Connection Type Originator -> Target for Implicit Messaging (Default: ConnectionType.Point_to_Point)
        /// </summary>
        public ConnectionType O_T_ConnectionType { get; set; } = ConnectionType.Point_to_Point;
        /// <summary>
        /// Connection Type Target -> Originator for Implicit Messaging (Default: ConnectionType.Multicast)
        /// </summary>
        public ConnectionType T_O_ConnectionType { get; set; } = ConnectionType.Multicast;
        /// <summary>
        /// Priority Originator -> Target for Implicit Messaging (Default: Priority.Scheduled)
        /// Could be: Priority.Scheduled; Priority.High; Priority.Low; Priority.Urgent
        /// </summary>
        public Priority O_T_Priority { get; set; } = Priority.Scheduled;
        /// <summary>
        /// Priority Target -> Originator for Implicit Messaging (Default: Priority.Scheduled)
        /// Could be: Priority.Scheduled; Priority.High; Priority.Low; Priority.Urgent
        /// </summary>
        public Priority T_O_Priority { get; set; } = Priority.Scheduled;
        /// <summary>
        /// Class Assembly (Consuming IO-Path - Outputs) Originator -> Target for Implicit Messaging (Default: 0x64)
        /// </summary>
        public byte O_T_InstanceID { get; set; } = 0x64;               //Ausgänge
        /// <summary>
        /// Class Assembly (Producing IO-Path - Inputs) Target -> Originator for Implicit Messaging (Default: 0x64)
        /// </summary>
        public byte T_O_InstanceID { get; set; } = 0x65;               //Eingänge
        /// <summary>
        /// Provides Access to the Class 1 Real-Time IO-Data Originator -> Target for Implicit Messaging    
        /// </summary>
        public byte[] O_T_IOData = new byte[505];   //Class 1 Real-Time IO-Data O->T   
        /// <summary>
        /// Provides Access to the Class 1 Real-Time IO-Data Target -> Originator for Implicit Messaging
        /// </summary>
        public byte[] T_O_IOData = new byte[505];    //Class 1 Real-Time IO-Data T->O  
        /// <summary>
        /// Used Real-Time Format Originator -> Target for Implicit Messaging (Default: RealTimeFormat.Header32Bit)
        /// Possible Values: RealTimeFormat.Header32Bit; RealTimeFormat.Heartbeat; RealTimeFormat.ZeroLength; RealTimeFormat.Modeless
        /// </summary>
        public RealTimeFormat O_T_RealTimeFormat { get; set; } = RealTimeFormat.Header32Bit;
        /// <summary>
        /// Used Real-Time Format Target -> Originator for Implicit Messaging (Default: RealTimeFormat.Modeless)
        /// Possible Values: RealTimeFormat.Header32Bit; RealTimeFormat.Heartbeat; RealTimeFormat.ZeroLength; RealTimeFormat.Modeless
        /// </summary>
        public RealTimeFormat T_O_RealTimeFormat { get; set; } = RealTimeFormat.Modeless;
        /// <summary>
        /// AssemblyObject for the Configuration Path in case of Implicit Messaging (Standard: 0x04)
        /// </summary>
        public byte AssemblyObjectClass { get; set; } = 0x04;
        /// <summary>
        /// ConfigurationAssemblyInstanceID is the InstanceID of the configuration Instance in the Assembly Object Class (Standard: 0x01)
        /// </summary>
        public byte ConfigurationAssemblyInstanceID { get; set; } = 0x01;
        /// <summary>
        /// Returns the Date and Time when the last Implicit Message has been received fŕom The Target Device
        /// Could be used to determine a Timeout
        /// </summary>        
        public DateTime LastReceivedImplicitMessage { get; set; }
    
        public EEIPClient()
        {
            Console.WriteLine("EEIP Library Version: " + Assembly.GetExecutingAssembly().GetName().Version.ToString());
            Console.WriteLine("Copyright (c) Stefan Rossmann Engineering Solutions");
            Console.WriteLine();
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            lock (this)
            {
                UdpClient u = (UdpClient)((UdpState)(ar.AsyncState)).u;
                
                System.Net.IPEndPoint e = (System.Net.IPEndPoint)((UdpState)(ar.AsyncState)).e;

                Byte[] receiveBytes = u.EndReceive(ar, ref e);
                string receiveString = Encoding.ASCII.GetString(receiveBytes);

                // EndReceive worked and we have received data and remote endpoint
                if (receiveBytes.Length > 0)
                {
                    UInt16 command = Convert.ToUInt16((receiveBytes[1] << 8) | receiveBytes[0]);
                    if (command == 0x63)
                    {
                        returnList.Add(Encapsulation.CIPIdentityItem.getCIPIdentityItem(24, receiveBytes));
                    }
                }
                var asyncResult = u.BeginReceive(new AsyncCallback(ReceiveCallback), (UdpState)(ar.AsyncState));
            }

        }
        public class UdpState
        {
            public System.Net.IPEndPoint e;
            public UdpClient u;

        }

        List<Encapsulation.CIPIdentityItem> returnList = new List<Encapsulation.CIPIdentityItem>();

        /// <summary>
        /// List and identify potential targets. This command shall be sent as braodcast massage using UDP.
        /// </summary>
        /// <returns>List<Encapsulation.CIPIdentityItem> contains the received informations from all devices </returns>	
        public List<Encapsulation.CIPIdentityItem> ListIdentity()
        {
            foreach (NetworkInterface ni in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (ni.NetworkInterfaceType == NetworkInterfaceType.Wireless80211 || ni.NetworkInterfaceType == NetworkInterfaceType.Ethernet)
                {
                    foreach (UnicastIPAddressInformation ip in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (ip.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                        {
                            System.Net.IPAddress mask = ip.IPv4Mask;
                            System.Net.IPAddress address = ip.Address;

                            String multicastAddress = (address.GetAddressBytes()[0] | (~(mask.GetAddressBytes()[0])) & 0xFF).ToString() + "." + (address.GetAddressBytes()[1] | (~(mask.GetAddressBytes()[1])) & 0xFF).ToString() + "." + (address.GetAddressBytes()[2] | (~(mask.GetAddressBytes()[2])) & 0xFF).ToString() + "." + (address.GetAddressBytes()[3] | (~(mask.GetAddressBytes()[3])) & 0xFF).ToString();

                            byte[] sendData = new byte[24];
                            sendData[0] = 0x63; //Command for "ListIdentity"
                            System.Net.Sockets.UdpClient udpClient = new System.Net.Sockets.UdpClient();
                            System.Net.IPEndPoint endPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(multicastAddress), 44818);
                            udpClient.Send(sendData, sendData.Length, endPoint);

                            UdpState s = new UdpState();
                            s.e = endPoint;
                            s.u = udpClient;

                            var asyncResult = udpClient.BeginReceive(new AsyncCallback(ReceiveCallback), s);

                            System.Threading.Thread.Sleep(1000);
                        }
                    }
                }
            }
            return returnList;
        }

        /// <summary>
        /// Sends a RegisterSession command to a target to initiate session
        /// </summary>
        /// <returns>Session Handle</returns>	
        public UInt32 RegisterSession()
        {
            if (this.sessionHandle != 0)
                return this.sessionHandle;

            Encapsulation encapsulation = new Encapsulation();
            encapsulation.Command = Encapsulation.CommandsEnum.RegisterSession;
            encapsulation.Length = 4;
            encapsulation.CommandSpecificData.AddRange(BitConverter.GetBytes((UInt16) 0x0001)); //Requested protocol version shall be set to 1.
            encapsulation.CommandSpecificData.AddRange(BitConverter.GetBytes((UInt16) 0x0000)); //Session options shall be set to "0"

            client = new TcpClient(this.IPAddress, this.TCPPort)
            {
                ReceiveTimeout = this.TCPClientTimeout,
                SendTimeout = this.TCPClientTimeout
            };
            stream = client.GetStream();

            byte[] recvData = new Byte[256];
            stream.Write(encapsulation.ToBytes(), 0, encapsulation.ToBytes().Length);
            stream.Read(recvData, 0, recvData.Length);

            this.sessionHandle = BitConverter.ToUInt32(recvData.ToArray(),4); //get session handle. bytes 4,5,6,7
            return this.sessionHandle;
        }

        /// <summary>
        /// Sends a UnRegisterSession command to a target to terminate session
        /// </summary> 
        public void UnRegisterSession()
        {
            //Check if we have an open session
            if (IsConnected)
            {
                ForwardClose();
            }

            Encapsulation encapsulation = new Encapsulation();
            encapsulation.Command = Encapsulation.CommandsEnum.UnRegisterSession;
            encapsulation.Length = 0;
            encapsulation.SessionHandle = this.sessionHandle;
 
            stream.Write(encapsulation.ToBytes(), 0, encapsulation.ToBytes().Length);

            client.Close();
            stream.Close();
            sessionHandle = 0;
        }

        System.Net.Sockets.UdpClient udpClientReceive;
        bool udpClientReceiveClosed = false;
        public virtual void ForwardOpen()
        {
            udpClientReceiveClosed = false;
            ushort o_t_headerOffset = 2;                    //Zählt den Sequencecount und evtl 32bit header zu der Länge dazu
            if (O_T_RealTimeFormat == RealTimeFormat.Header32Bit)
                o_t_headerOffset = 6;
            if (O_T_RealTimeFormat == RealTimeFormat.Heartbeat)
                o_t_headerOffset = 0;

            ushort t_o_headerOffset = 2;                    //Zählt den Sequencecount und evtl 32bit header zu der Länge dazu
            if (T_O_RealTimeFormat == RealTimeFormat.Header32Bit)
                t_o_headerOffset = 6;
            if (T_O_RealTimeFormat == RealTimeFormat.Heartbeat)
                t_o_headerOffset = 0;

            bool largeForwardOpen;
            if (this.TransportClass == 0 || this.TransportClass == 1) //Transport Class 0 or 1 uses UDP
            {
                largeForwardOpen = ((T_O_Length + t_o_headerOffset) > 504); //larger than standard packet
            }
            else
            {
                largeForwardOpen = (this.ConnectionSize > 504); //larger than standard packet
            }

            int lengthOffset = (5 + (O_T_ConnectionType == ConnectionType.Null ? 0 : 2) + (T_O_ConnectionType == ConnectionType.Null ? 0 : 2));

            //Common Packet Format (Table 2-6.1)
            var commonPacketFormat = new Encapsulation.CommonPacketFormat();
            this.ConnectionSequenceCounter = 0; //reset packet counter

            commonPacketFormat.Data.Add((byte) (largeForwardOpen ? CIPConnectionServices.Large_Forward_Open : CIPConnectionServices.Forward_Open)); //commanded service
            commonPacketFormat.Data.Add(0x02); //Requested Path size
            commonPacketFormat.Data.Add(0x20); //Path segment for Class ID
            commonPacketFormat.Data.Add(0x06); //Class ID
            commonPacketFormat.Data.Add(0x24); //Path segment for Instance ID
            commonPacketFormat.Data.Add(0x01); //Instance ID

            commonPacketFormat.Data.Add(0x03); //Priority and Time/Tick - Table 3-5.16 (Vol. 1) 8 ms base clock
            commonPacketFormat.Data.Add(0xfa); //Timeout Ticks - Table 3-5.16 (Vol. 1) 250x timeout so 2 seconds

            this.connectionID_O_T = Convert.ToUInt32(new Random().Next(0x1,0xFFFFFFF));
            this.connectionID_T_O = Convert.ToUInt32(new Random().Next(0x1,0xFFFFFFF));
            this.ConnectionSerialNumber = Convert.ToUInt16(new Random().Next(0x1,0xFFFF));
            commonPacketFormat.Data.AddRange(BitConverter.GetBytes((UInt32) connectionID_O_T));
            commonPacketFormat.Data.AddRange(BitConverter.GetBytes((UInt32) connectionID_T_O));
            commonPacketFormat.Data.AddRange(BitConverter.GetBytes((UInt16) ConnectionSerialNumber)); //Connection serial number (random)
            commonPacketFormat.Data.AddRange(BitConverter.GetBytes((UInt16) this.VendorID)); //Originator Vendor ID
            commonPacketFormat.Data.AddRange(BitConverter.GetBytes((UInt32) this.SerialNumber)); //Originator Serial Number
            commonPacketFormat.Data.Add(0x03); //Timeout Multiplier
            commonPacketFormat.Data.AddRange(new byte[3]); //Reserved octets
            commonPacketFormat.Data.AddRange(BitConverter.GetBytes((UInt32)this.RequestedPacketRate_O_T)); //Requested Packet Rate O->T in Microseconds

            //----------------O->T Network Connection Parameters
            bool redundantOwner = (bool)O_T_OwnerRedundant;
            byte connectionType = (byte)O_T_ConnectionType; //1=Multicast, 2=P2P
            byte priority = (byte)O_T_Priority; //00=low; 01=High; 10=Scheduled; 11=Urgent
            bool variableLength = O_T_VariableLength; //0=fixed; 1=variable
            uint connectionSize = (uint) (T_O_Length + t_o_headerOffset);
            UInt32 networkConnectionParameters;
            if (this.TransportClass == 0 || this.TransportClass == 1) //Transport Class 0 or 1 uses UDP
            {
                connectionSize = (ushort)(O_T_Length + o_t_headerOffset); //The maximum size in bytes of the data for each direction (where applicable) of the connection. For a variable -> maximum
                networkConnectionParameters = (UInt16) (((Convert.ToUInt16(redundantOwner)) << 15) | ((connectionType & 0x03) << 13) | ((priority & 0x03) << 10) | ((Convert.ToUInt16(variableLength)) << 9));
            }
            else
            {
                connectionSize = this.ConnectionSize;
                networkConnectionParameters = (UInt16)(((Convert.ToUInt16(false)) << 15) | ((connectionType & 0x03) << 13) | ((priority & 0x03) << 10) | ((Convert.ToUInt16(variableLength)) << 9));
            }
                
            if (largeForwardOpen)
            {
                networkConnectionParameters <<= 16;
                networkConnectionParameters |= ((UInt16) (connectionSize & 0xFFFF));
                commonPacketFormat.Data.AddRange(BitConverter.GetBytes((UInt32)networkConnectionParameters));
            }
            else
            {
                networkConnectionParameters |= (UInt16) (connectionSize & 0x1FF);
                commonPacketFormat.Data.AddRange(BitConverter.GetBytes((UInt16)networkConnectionParameters));
            }
            //----------------O->T Network Connection Parameters

            commonPacketFormat.Data.AddRange(BitConverter.GetBytes((UInt32) this.RequestedPacketRate_T_O)); //Requested Packet Rate T->O in Microseconds
            //----------------T->O Network Connection Parameters
            redundantOwner = (bool)T_O_OwnerRedundant;
            connectionType = (byte)T_O_ConnectionType; //1=Multicast, 2=P2P
            priority = (byte)T_O_Priority;
            variableLength = T_O_VariableLength;
            connectionSize = (uint) (T_O_Length + t_o_headerOffset);
            if (this.TransportClass == 0 || this.TransportClass == 1) //Transport Class 0 or 1 uses UDP
            {
                networkConnectionParameters = 0;
                connectionSize = (ushort)(O_T_Length + o_t_headerOffset); //The maximum size in bytes of the data for each direction (where applicable) of the connection. For a variable -> maximum
                networkConnectionParameters = (UInt16)(((Convert.ToUInt16(redundantOwner)) << 15) | ((connectionType & 0x03) << 13) | ((priority & 0x03) << 10) | ((Convert.ToUInt16(variableLength)) << 9));
            }
            else //Reuse connection parameters for Class 2 or 3
            {
                connectionSize = this.ConnectionSize;
            }

            if (largeForwardOpen)
            {
                networkConnectionParameters <<= 16;
                networkConnectionParameters |= ((UInt16) (connectionSize & 0xFFFF));
                commonPacketFormat.Data.AddRange(BitConverter.GetBytes((UInt32)networkConnectionParameters));
            }
            else
            {
                networkConnectionParameters |= (UInt16) (connectionSize & 0x1FF);
                commonPacketFormat.Data.AddRange(BitConverter.GetBytes((UInt16)networkConnectionParameters));
            }
            //----------------T->O Network Connection Parameters

            //----------------Transport Type/Trigger
            commonPacketFormat.Data.Add(this.TransportType); //3-4.3.3. transportClass_trigger Attribute - USINT data type

            var connectionPath = new List<byte>();
            if (this.TransportClass == 0 || this.TransportClass == 1) //Transport Class 0 or 1 uses UDP
            {
                //Connection Path
                connectionPath.AddRange(new byte[] { 0x20, AssemblyObjectClass, 0x24, ConfigurationAssemblyInstanceID });
                if (O_T_ConnectionType != ConnectionType.Null)
                {
                    connectionPath.AddRange(new byte[] { 0x2C, O_T_InstanceID });
                }
                if (T_O_ConnectionType != ConnectionType.Null)
                {
                    connectionPath.AddRange(new byte[] { 0x2C, T_O_InstanceID });
                }
                commonPacketFormat.Data.Add((byte) (connectionPath.Count / 2)); //Connection Path size
                commonPacketFormat.Data.AddRange(connectionPath.ToArray()); //Connection Path
                this.ConnectionPath = connectionPath;

                //AddSocket Addrress Item O->T
                commonPacketFormat.SocketaddrInfo_O_T = new Encapsulation.SocketAddress();
                commonPacketFormat.SocketaddrInfo_O_T.SIN_port = OriginatorUDPPort;
                commonPacketFormat.SocketaddrInfo_O_T.SIN_family = 2;
                if (O_T_ConnectionType == ConnectionType.Multicast)
                {
                    UInt32 multicastResponseAddress = EEIPClient.GetMulticastAddress(BitConverter.ToUInt32(System.Net.IPAddress.Parse(IPAddress).GetAddressBytes(), 0));
                    commonPacketFormat.SocketaddrInfo_O_T.SIN_Address = (multicastResponseAddress);
                    multicastAddress = commonPacketFormat.SocketaddrInfo_O_T.SIN_Address;
                }
                else
                {
                    commonPacketFormat.SocketaddrInfo_O_T.SIN_Address = 0;
                }
            }
            else //Transport Class 2 or 3 pointing to a PLC
            {
                commonPacketFormat.Data.Add((byte)(this.ConnectionPath.Count / 2)); //Connection Path size
                commonPacketFormat.Data.AddRange(this.ConnectionPath.ToArray()); //Connection Path
            }
                
            var encapsulation = BuildUCMMHeader(Encapsulation.CommandsEnum.SendRRData, commonPacketFormat);
            var data = GetBytes(encapsulation, commonPacketFormat);
            var status = (Encapsulation.StatusEnum) BitConverter.ToUInt32(data,8);

            //--------------------------BEGIN Error?
            if (status != Encapsulation.StatusEnum.Success)
            {
                throw new CIPException(status.ToString());
            }
            else if (data[42] != 0)      //Exception codes see "Table B-1.1 CIP General Status Codes"
            {
                if (data[42] == 0x1)
                    if (data[43] == 0)
                        throw new CIPException("Connection failure, General Status Code: " + data[42]);
                    else
                        throw new CIPException("Connection failure, General Status Code: " + data[42] + " Additional Status Code: " + ((data[45] << 8) | data[44]) + " " + ObjectLibrary.ConnectionManagerObject.GetExtendedStatus((uint)((data[45] << 8) | data[44])));
                else
                    throw new CIPException(CIPGeneralStatusCodes.GetStatusName(data[42]));
            }
            
            //--------------------------END Error?
            //Read the Network ID from the Reply (see 3-3.7.1.1)
            int itemCount = data[30] + (data[31] << 8);
            int lengthUnconectedDataItem = data[38] + (data[39] << 8);
            this.connectionID_O_T = data[44] + (uint)(data[45] << 8) + (uint)(data[46] << 16) + (uint)(data[47] << 24);
            this.connectionID_T_O = data[48] + (uint)(data[49] << 8) + (uint)(data[50] << 16) + (uint)(data[51] << 24);

            //Is a SocketInfoItem present?
            int numberOfCurrentItem = 0;
            Encapsulation.SocketAddress socketInfoItem;
            while (itemCount > 2)
            {
                int typeID = data[40 + lengthUnconectedDataItem+ 20 * numberOfCurrentItem] + (data[40 + lengthUnconectedDataItem + 1+ 20 * numberOfCurrentItem] << 8);
                if (typeID == 0x8001)
                {
                    socketInfoItem = new Encapsulation.SocketAddress();
                    socketInfoItem.SIN_Address = (UInt32)(data[40 + lengthUnconectedDataItem + 8 + 20 * numberOfCurrentItem]) + (UInt32)(data[40 + lengthUnconectedDataItem + 9 + 20 * numberOfCurrentItem] << 8) + (UInt32)(data[40 + lengthUnconectedDataItem + 10 + 20 * numberOfCurrentItem] << 16) + (UInt32)(data[40 + lengthUnconectedDataItem + 11 + 20 * numberOfCurrentItem] << 24);
                    socketInfoItem.SIN_port = (UInt16)((UInt16)(data[40 + lengthUnconectedDataItem + 7 + 20 * numberOfCurrentItem]) + (UInt16)(data[40 + lengthUnconectedDataItem + 6 + 20 * numberOfCurrentItem] << 8));
                    if (T_O_ConnectionType == ConnectionType.Multicast)
                        multicastAddress = socketInfoItem.SIN_Address;
                    TargetUDPPort = socketInfoItem.SIN_port;
                }
                numberOfCurrentItem++;
                itemCount--;
            }

            if (this.TransportClass == 0 || this.TransportClass == 1) //Transport Class 0 or 1 uses UDP
            {
                //Open UDP-Port
                System.Net.IPEndPoint endPointReceive = new System.Net.IPEndPoint(System.Net.IPAddress.Any, OriginatorUDPPort);
                udpClientReceive = new System.Net.Sockets.UdpClient(endPointReceive);
                UdpState s = new UdpState();
                s.e = endPointReceive;
                s.u = udpClientReceive;
                if (multicastAddress != 0)
                {
                    System.Net.IPAddress multicast = (new System.Net.IPAddress(multicastAddress));
                    udpClientReceive.JoinMulticastGroup(multicast);
                }

                System.Threading.Thread sendThread = new System.Threading.Thread(sendUDP);
                sendThread.Start();

                var asyncResult = udpClientReceive.BeginReceive(new AsyncCallback(ReceiveCallbackClass1), s);
            }
        }

        public virtual void ForwardClose()
        {
            stopUDP = true; //First stop the Thread which send data

            //Common Packet Format (Table 2-6.1)
            var commonPacketFormat = new Encapsulation.CommonPacketFormat();
            commonPacketFormat.Data.Add((byte)CIPConnectionServices.Forward_Close);

            commonPacketFormat.Data.Add(0x02); //Requested Path size
            commonPacketFormat.Data.Add(0x20); //Path segment for Class ID
            commonPacketFormat.Data.Add(0x06); //Class ID
            commonPacketFormat.Data.Add(0x24); //Path segment for Instance ID
            commonPacketFormat.Data.Add(0x01); //Instance ID

            commonPacketFormat.Data.Add(0x03); //Priority and Time/Tick - Table 3-5.16 (Vol. 1)
            commonPacketFormat.Data.Add(0xfa); //Timeout Ticks - Table 3-5.16 (Vol. 1)

            commonPacketFormat.Data.AddRange(BitConverter.GetBytes((UInt16)this.ConnectionSerialNumber)); //Connection serial number (random)
            commonPacketFormat.Data.AddRange(BitConverter.GetBytes((UInt16)this.VendorID)); //Originator Vendor ID
            commonPacketFormat.Data.AddRange(BitConverter.GetBytes((UInt32)this.SerialNumber)); //Originator Serial Number

            var connectionPath = new List<byte>();
            //Connection Path: Route to the target device
            if (this.TransportClass == 0 || this.TransportClass == 1) //Transport Class 0 or 1 uses UDP
            {
                //Connection Path
                connectionPath.AddRange(new byte[] { 0x20, AssemblyObjectClass, 0x24, ConfigurationAssemblyInstanceID });
                if (O_T_ConnectionType != ConnectionType.Null)
                {
                    connectionPath.AddRange(new byte[] { 0x2C, O_T_InstanceID });
                }
                if (T_O_ConnectionType != ConnectionType.Null)
                {
                    connectionPath.AddRange(new byte[] { 0x2C, T_O_InstanceID });
                }
                commonPacketFormat.Data.Add((byte)(connectionPath.Count / 2)); //Connection Path size
                commonPacketFormat.Data.Add(0); //Reserved
                commonPacketFormat.Data.AddRange(connectionPath.ToArray()); //Connection Path
                this.ConnectionPath = connectionPath;
            }
            else //Transport Class 2 or 3 uses just TCP
            {
                commonPacketFormat.Data.Add((byte)(this.ConnectionPath.Count / 2)); //Connection Path size
                commonPacketFormat.Data.Add(0); //Reserved
                commonPacketFormat.Data.AddRange(this.ConnectionPath.ToArray()); //Connection Path
            }

            var encapsulation = BuildUCMMHeader(Encapsulation.CommandsEnum.SendRRData, commonPacketFormat);
            var recvData = GetReply(encapsulation, commonPacketFormat);

            this.ConnectionSerialNumber = 0; //clear out serial number
            //Todo clean up access to Transport class there should be a property for the trigger type and it should get used in Forward Open
            if (this.TransportClass == 0 || this.TransportClass == 1) //Transport Class 0 or 1 uses UDP
            {
                //Close the Socket for Receive
                udpClientReceiveClosed = true;
                udpClientReceive.Close();
            }
        }

        public Encapsulation BuildHeader(Encapsulation.CommonPacketFormat commonPacketFormat)
        {
            if (this.IsConnected)
            {
                return BuildCMMHeader(Encapsulation.CommandsEnum.SendUnitData, commonPacketFormat);
            }
            else
            {
                return BuildUCMMHeader(Encapsulation.CommandsEnum.SendRRData, commonPacketFormat);
            }
        }

        public Encapsulation BuildUCMMHeader(Encapsulation.CommandsEnum command, Encapsulation.CommonPacketFormat commonPacketFormat)
        {
            var header = new Encapsulation();

            header.Command = command;
            header.SessionHandle = this.sessionHandle;

            //Command specific data
            header.CommandSpecificData.AddRange(new byte[4]); //CIP interface handle
            header.CommandSpecificData.AddRange(new byte[2]); //Timeout

            //Common Packet Format (Table 2-6.1)
            commonPacketFormat.ItemCount = 0x02;
            commonPacketFormat.AddressTypeID = 0x0000; //Address item NULL (used for UCMM Messages)
            commonPacketFormat.AddressLength = 0x0000;
            commonPacketFormat.DataTypeID = 0xB2; //Data item
            commonPacketFormat.DataLength = (UInt16)commonPacketFormat.Data.Count;  //Get common packet format data length

            header.Length = (UInt16)(16 + commonPacketFormat.DataLength);
            if (commonPacketFormat.SocketaddrInfo_O_T != null) 
            {
                header.Length += 24; //Get encapsulated packet length
            }

            return header;
        }

        public Encapsulation BuildCMMHeader(Encapsulation.CommandsEnum command, Encapsulation.CommonPacketFormat commonPacketFormat)
        {
            var header = new Encapsulation();

            header.Command = command;
            header.SessionHandle = this.sessionHandle;

            //Command specific data
            header.CommandSpecificData.AddRange(new byte[4]); //CIP interface handle
            header.CommandSpecificData.AddRange(new byte[2]); //Timeout

            //Common Packet Format (Table 2-6.1)
            commonPacketFormat.ItemCount = 0x02;
            commonPacketFormat.AddressTypeID = 0x00A1; //Address item for connected messages
            commonPacketFormat.AddressLength = 0x0004;
            commonPacketFormat.AddressData.AddRange(BitConverter.GetBytes((UInt32)this.connectionID_O_T));
            commonPacketFormat.DataTypeID = 0xB1; //Data item
            commonPacketFormat.SequenceNumber = ++this.ConnectionSequenceCounter;
            commonPacketFormat.DataLength = (UInt16) (commonPacketFormat.Data.Count+2);  //Get common packet format data length

            header.Length = (UInt16)(20 + commonPacketFormat.DataLength);
            if (commonPacketFormat.SocketaddrInfo_O_T != null)
            {
                header.Length += 24; //Get encapsulated packet length
            }

            this.ConnectionSequenceCounter %= UInt16.MaxValue; //prevent overflow on sequence counter. might be unnecessary

            return header;
        }

        protected Tuple<byte,byte[]> GetReply(Encapsulation encapsulation, Encapsulation.CommonPacketFormat commonPacketFormat)
        {
            if (this.sessionHandle == 0) //If a Session is not registered, Try to Register a Session with the predefined IP-Address and Port
                this.RegisterSession();

            var encapsulationData = encapsulation.ToBytes();
            var commonPacketFormatData = commonPacketFormat.ToBytes();
            byte[] dataToWrite = new byte[encapsulationData.Length + commonPacketFormatData.Length];
            System.Buffer.BlockCopy(encapsulationData, 0, dataToWrite, 0, encapsulationData.Length);
            System.Buffer.BlockCopy(commonPacketFormatData, 0, dataToWrite, encapsulationData.Length, commonPacketFormatData.Length);

            byte[] recvBuffer = new byte[1024];
            stream.Write(dataToWrite, 0, dataToWrite.Length);
            var recvLength = stream.Read(recvBuffer, 0, recvBuffer.Length);
            var replyOffset = 40;
            switch (encapsulation.Command)
            {
                case Encapsulation.CommandsEnum.SendRRData:
                    replyOffset = 40;
                    break;
                case Encapsulation.CommandsEnum.SendUnitData:
                    replyOffset = 46;
                    break;
                default:
                    break;
            }

            var statusCode = recvBuffer[replyOffset + 2];
            var status = CIPGeneralStatusCodes.GetStatus(statusCode);

            //Exception codes see "Table B-1.1 CIP General Status Codes"
            if (!status.ContainsKey(CIPGeneralStatusCodes.CIP_SERVICE_SUCCESS))
            {
                //Assuming single int 16 extended status code for now
                var extendedStatusCode = BitConverter.ToInt16(recvBuffer, replyOffset + 4);
                throw new CIPException(status.Values.First());
            }

            var replyData = recvBuffer.Skip(replyOffset).Take(recvLength - replyOffset).ToArray();

            return Tuple.Create(statusCode,replyData);
        }

        protected byte[] GetBytes(Encapsulation encapsulation, Encapsulation.CommonPacketFormat commonPacketFormat)
        {
            if (this.sessionHandle == 0) //If a Session is not registered, Try to Register a Session with the predefined IP-Address and Port
                this.RegisterSession();

            var encapsulationData = encapsulation.ToBytes();
            var commonPacketFormatData = commonPacketFormat.ToBytes();
            byte[] dataToWrite = new byte[encapsulationData.Length + commonPacketFormatData.Length];
            System.Buffer.BlockCopy(encapsulationData, 0, dataToWrite, 0, encapsulationData.Length);
            System.Buffer.BlockCopy(commonPacketFormatData, 0, dataToWrite, encapsulationData.Length, commonPacketFormatData.Length);

            byte[] recvBuffer = new byte[1024];
            stream.Write(dataToWrite, 0, dataToWrite.Length);
            var recvLength = stream.Read(recvBuffer, 0, recvBuffer.Length);

            return recvBuffer.Take(recvLength).ToArray();
        }

        private ushort o_t_detectedLength;
        /// <summary>
        /// Detects the Length of the data Originator -> Target.
        /// The Method uses an Explicit Message to detect the length.
        /// The IP-Address, Port and the Instance ID has to be defined before
        /// </summary>
        public ushort Detect_O_T_Length ()
        {
            if (o_t_detectedLength == 0)
            {
                o_t_detectedLength = (ushort)(this.GetAttributeSingle(0x04, O_T_InstanceID, 3)).Length;
            }
            return o_t_detectedLength;
        }

        private ushort t_o_detectedLength;
        /// <summary>
        /// Detects the Length of the data Target -> Originator.
        /// The Method uses an Explicit Message to detect the length.
        /// The IP-Address, Port and the Instance ID has to be defined before
        /// </summary>
        public ushort Detect_T_O_Length()
        {
            if (t_o_detectedLength == 0)
            {
                t_o_detectedLength = (ushort)(this.GetAttributeSingle(0x04, T_O_InstanceID, 3)).Length;
            }
            return t_o_detectedLength;
        }

        private static UInt32 GetMulticastAddress(UInt32 deviceIPAddress)
        {
            UInt32 cip_Mcast_Base_Addr = 0xEFC00100;
            UInt32 cip_Host_Mask = 0x3FF;
            UInt32 netmask = 0;

            //Class A Network?
            if (deviceIPAddress <= 0x7FFFFFFF)
                netmask = 0xFF000000;
            //Class B Network?
            if (deviceIPAddress >= 0x80000000 && deviceIPAddress <= 0xBFFFFFFF)
                netmask = 0xFFFF0000;
            //Class C Network?
            if (deviceIPAddress >= 0xC0000000 && deviceIPAddress <= 0xDFFFFFFF)
                netmask = 0xFFFFFF00;

            UInt32 hostID = deviceIPAddress & ~netmask;
            UInt32 mcastIndex = hostID - 1;
            mcastIndex &= cip_Host_Mask;

            return (UInt32) (mcastIndex << 5 | cip_Mcast_Base_Addr);

        }

        private bool stopUDP;
        int sequence = 0;
        private void sendUDP()
        {
            System.Net.Sockets.UdpClient udpClientsend = new System.Net.Sockets.UdpClient();
            stopUDP = false;
            uint sequenceCount = 0;

            while (!stopUDP)
            {
                byte[] o_t_IOData = new byte[564];
                System.Net.IPEndPoint endPointsend = new System.Net.IPEndPoint(System.Net.IPAddress.Parse(IPAddress), TargetUDPPort);
               
                UdpState send = new UdpState();
                 
                //---------------Item count
                o_t_IOData[0] = 2;
                o_t_IOData[1] = 0;
                //---------------Item count

                //---------------Type ID
                o_t_IOData[2] = 0x02;
                o_t_IOData[3] = 0x80;
                //---------------Type ID

                //---------------Length
                o_t_IOData[4] = 0x08;
                o_t_IOData[5] = 0x00;
                //---------------Length

                //---------------connection ID
                sequenceCount++;
                o_t_IOData[6] = (byte)(connectionID_O_T);
                o_t_IOData[7] = (byte)(connectionID_O_T >> 8); 
                o_t_IOData[8] = (byte)(connectionID_O_T >> 16); 
                o_t_IOData[9] = (byte)(connectionID_O_T >> 24);
                //---------------connection ID     

                //---------------sequence count
                o_t_IOData[10] = (byte)(sequenceCount);
                o_t_IOData[11] = (byte)(sequenceCount >> 8);
                o_t_IOData[12] = (byte)(sequenceCount >> 16);
                o_t_IOData[13] = (byte)(sequenceCount >> 24);
                //---------------sequence count            

                //---------------Type ID
                o_t_IOData[14] = 0xB1;
                o_t_IOData[15] = 0x00;
                //---------------Type ID

                ushort headerOffset = 0;
                if (O_T_RealTimeFormat == RealTimeFormat.Header32Bit)
                    headerOffset = 4;
                if (O_T_RealTimeFormat == RealTimeFormat.Heartbeat)
                    headerOffset = 0;
                ushort o_t_Length = (ushort)(O_T_Length + headerOffset+2);   //Modeless and zero Length

                //---------------Length
                o_t_IOData[16] = (byte)o_t_Length;
                o_t_IOData[17] = (byte)(o_t_Length >> 8);
                //---------------Length

                //---------------Sequence count
                sequence++;
                if (O_T_RealTimeFormat != RealTimeFormat.Heartbeat)
                {
                    o_t_IOData[18] = (byte)sequence;
                    o_t_IOData[19] = (byte)(sequence >> 8);
                }
                //---------------Sequence count

                if (O_T_RealTimeFormat == RealTimeFormat.Header32Bit)
                {
                    o_t_IOData[20] = (byte)1;
                    o_t_IOData[21] = (byte)0;
                    o_t_IOData[22] = (byte)0;
                    o_t_IOData[23] = (byte)0;

                }

                //---------------Write data
                for ( int i = 0; i < O_T_Length; i++)
                    o_t_IOData[20+headerOffset+i] = (byte)O_T_IOData[i];
                //---------------Write data

                udpClientsend.Send(o_t_IOData, O_T_Length+20+headerOffset, endPointsend);
                System.Threading.Thread.Sleep((int)RequestedPacketRate_O_T/1000);

            }

            udpClientsend.Close();

        }

        private void ReceiveCallbackClass1(IAsyncResult ar)
        {          
            UdpClient u = (UdpClient)((UdpState)(ar.AsyncState)).u;
            if (udpClientReceiveClosed)
                return;

            u.BeginReceive(new AsyncCallback(ReceiveCallbackClass1), (UdpState)(ar.AsyncState));
            System.Net.IPEndPoint e = (System.Net.IPEndPoint)((UdpState)(ar.AsyncState)).e;


            Byte[] receiveBytes = u.EndReceive(ar, ref e);

            // EndReceive worked and we have received data and remote endpoint

            if (receiveBytes.Length > 20)
            {
                //Get the connection ID
                uint connectionID = (uint)(receiveBytes[6] | receiveBytes[7] << 8 | receiveBytes[8] << 16 | receiveBytes[9] << 24);


                if (connectionID == connectionID_T_O)
                {
                    ushort headerOffset = 0;
                    if (T_O_RealTimeFormat == RealTimeFormat.Header32Bit)
                        headerOffset = 4;
                    if (T_O_RealTimeFormat == RealTimeFormat.Heartbeat)
                        headerOffset = 0;
                    for (int i = 0; i < receiveBytes.Length-20-headerOffset; i++)
                    {
                        T_O_IOData[i] = receiveBytes[20 + i + headerOffset];
                    }
                }
            }
            LastReceivedImplicitMessage = DateTime.Now;
        }

        /// <summary>
        /// Sends a RegisterSession command to a target to initiate session
        /// </summary>
        /// <param name="address">IP-Address of the target device</param> 
        /// <param name="port">Port of the target device (default should be 0xAF12)</param> 
        /// <returns>Session Handle</returns>	
        //public UInt32 RegisterSession(string address, UInt16 port)
        //{
        //    string[] addressSubstring = address.Split('.');
        //    var address = IPAddress.
        //    UInt32 ipAddress = (UInt32.Parse(addressSubstring[0]) << 24) | (UInt32.Parse(addressSubstring[1]) << 16) | (UInt32.Parse(addressSubstring[2]) << 8) | UInt32.Parse(addressSubstring[3]);
        //    return RegisterSession(ipAddress);
        //}

        public byte[] GetAttributeSingle(int classID, int instanceID, int attributeID)
        {
            Encapsulation.CommonPacketFormat commonPacketFormat = new Encapsulation.CommonPacketFormat();

            var requestPathData = GetEPath(classID, instanceID, attributeID);

            //Common Packet Format Data
            commonPacketFormat.Data.Add((byte)CIPCommonServices.Get_Attribute_Single); //requested service
            commonPacketFormat.Data.Add((byte)(requestPathData.Length / 2)); //Requested Path size (number of 16 bit words)
            //Request Path
            commonPacketFormat.Data.AddRange(requestPathData); //Request Path

            var encapsulation = BuildUCMMHeader(Encapsulation.CommandsEnum.SendRRData, commonPacketFormat);
            var reply = GetReply(encapsulation, commonPacketFormat);

            return reply.Item2.Skip(4).ToArray(); //Start at reply service in the message reply field
        }

        /// <summary>
        /// Implementation of Common Service "Get_Attribute_All" - Service Code: 0x01
        /// </summary>
        /// <param name="classID">Class id of requested Attributes</param> 
        /// <param name="instanceID">Instance of Requested Attributes (0 for class Attributes)</param> 
        /// <returns>Session Handle</returns>	
        public byte[] GetAttributeAll(int classID, int instanceID)
        {
            Encapsulation.CommonPacketFormat commonPacketFormat = new Encapsulation.CommonPacketFormat();

            var requestPathData = GetEPath(classID, instanceID, 0);

            //Common Packet Format Data
            commonPacketFormat.Data.Add((byte)CIPCommonServices.Get_Attributes_All); //requested service
            commonPacketFormat.Data.Add((byte)(requestPathData.Length / 2)); //Requested Path size (number of 16 bit words)
            //Request Path
            commonPacketFormat.Data.AddRange(requestPathData); //Request Path

            var encapsulation = BuildHeader(commonPacketFormat);
            var reply = GetReply(encapsulation, commonPacketFormat);

            return reply.Item2.Skip(2).ToArray(); //Start at reply service in the message reply field
        }

        /// <summary>
        /// Implementation of Common Service "Get_Attribute_List" - Service Code: 0x03
        /// </summary>
        /// <param name="classID">Class id of requested Attributes</param> 
        /// <param name="instanceID">Instance of Requested Attributes (0 for class Attributes)</param> 
        /// <returns>Reply Data</returns>	
        public byte[] GetAttributeList(int classID, int instanceID, List<UInt16> attributes)
        {
            Encapsulation.CommonPacketFormat commonPacketFormat = new Encapsulation.CommonPacketFormat();

            var requestPathData = GetEPath(classID, instanceID, 0);

            //Common Packet Format Data
            commonPacketFormat.Data.Add((byte)CIPCommonServices.Get_Attribute_List); //requested service
            commonPacketFormat.Data.Add((byte)(requestPathData.Length / 2)); //Requested Path size (number of 16 bit words)
            //Request Path
            commonPacketFormat.Data.AddRange(requestPathData); //Request Path
            commonPacketFormat.Data.AddRange(BitConverter.GetBytes((UInt16) attributes.Count())); //Number of attributes to grab

            foreach (var attribute in attributes)
            {
                commonPacketFormat.Data.AddRange(BitConverter.GetBytes((UInt16)attribute)); //attribute ID
            }

            var encapsulation = BuildHeader(commonPacketFormat);
            var reply = GetReply(encapsulation, commonPacketFormat);

            return reply.Item2.ToArray(); //Start at reply service in the message reply field
        }

        public byte[] SetAttributeSingle(int classID, int instanceID, int attributeID, byte[] value)
        {
            Encapsulation.CommonPacketFormat commonPacketFormat = new Encapsulation.CommonPacketFormat();

            var requestPathData = GetEPath(classID, instanceID, attributeID);

            //Common Packet Format Data
            commonPacketFormat.Data.Add((byte)CIPCommonServices.Set_Attribute_Single); //requested service
            commonPacketFormat.Data.Add((byte)(requestPathData.Length / 2)); //Requested Path size (number of 16 bit words)
            //Request Path
            commonPacketFormat.Data.AddRange(requestPathData); //Request Path
            commonPacketFormat.Data.AddRange(value); //Attribute data

            var encapsulation = BuildHeader(commonPacketFormat);
            var reply = GetReply(encapsulation, commonPacketFormat);

            return reply.Item2.Skip(2).ToArray(); //Start at reply service in the message reply field
        }

        /// <summary>
        /// Implementation of Common Service "Get_Attribute_All" - Service Code: 0x01
        /// </summary>
        /// <param name="classID">Class id of requested Attributes</param> 
        public byte[] GetAttributeAll(int classID)
        {
            return this.GetAttributeAll(classID, 0);
        }

        public byte[] MultiServicePacket(List<byte[]> services)
        {
            Encapsulation.CommonPacketFormat commonPacketFormat = new Encapsulation.CommonPacketFormat();
            var requestPathData = GetEPath(0x02, 0x01, 0x00);

            //Common Packet Format Data
            commonPacketFormat.Data.Add((byte)CIPCommonServices.Multiple_Service_Packet); //requested service
            commonPacketFormat.Data.Add(0x02); //Requested Path size (number of 16 bit words)
            //Request Path
            commonPacketFormat.Data.AddRange(requestPathData); //Request Path
            commonPacketFormat.Data.AddRange(BitConverter.GetBytes((UInt16)services.Count()));

            var offset = 2 + 2 * services.Count();
            foreach (var service in services) //Add length of each service
            {
                commonPacketFormat.Data.AddRange(BitConverter.GetBytes((UInt16)offset));
                offset += service.Length;
            }
            foreach (var service in services)
            {
                commonPacketFormat.Data.AddRange(service);
            }

            var encapsulation = BuildHeader(commonPacketFormat);
            var reply = GetReply(encapsulation, commonPacketFormat);
            var recvData = reply.Item2;

            return recvData.Skip(4).ToArray();
        }

        /// <summary>
        /// Get the Encrypted Request Path - See Volume 1 Appendix C (C9)
        /// e.g. for 8 Bit: 20 05 24 02 30 01
        /// for 16 Bit: 21 00 05 00 24 02 30 01
        /// </summary>
        /// <param name="classID">Requested Class ID</param>
        /// <param name="instanceID">Requested Instance ID</param>
        /// <param name="attributeID">Requested Attribute ID - if "0" the attribute will be ignored</param>
        /// <returns>Encrypted Request Path</returns>
        protected static byte[] GetEPath(int classID, int instanceID, int attributeID)
        {
            List<byte> returnData = new List<byte>();

            if (classID <= byte.MaxValue)
            {
                returnData.Add(0x20);
                returnData.Add((byte)classID);
            }
            else
            {
                returnData.Add(0x21);
                returnData.Add(0x00);
                returnData.AddRange(BitConverter.GetBytes((UInt16)classID));
            }

            if (instanceID <= byte.MaxValue)
            {
                returnData.Add(0x24);
                returnData.Add((byte)instanceID);
            }
            else
            {
                returnData.Add(0x25);
                returnData.Add(0x00);
                returnData.AddRange(BitConverter.GetBytes((UInt16)instanceID));
            }

            if (attributeID != 0)
            {
                if (attributeID <= byte.MaxValue)
                {
                    returnData.Add(0x30);
                    returnData.Add((byte)attributeID);
                }
                else
                {
                    returnData.Add(0x31);
                    returnData.Add(0x00);
                    returnData.AddRange(BitConverter.GetBytes((UInt16)attributeID));
                }
            }

            return returnData.ToArray();
        }

        ObjectLibrary.IdentityObject identityObject;
        /// <summary>
        /// Implementation of the identity Object (Class Code: 0x01) - Required Object according to CIP-Specification
        /// </summary>
        public ObjectLibrary.IdentityObject IdentityObject
        {
            get
            {
                if (identityObject == null)
                    identityObject = new ObjectLibrary.IdentityObject(this);
                return identityObject;
            }
        }

        ObjectLibrary.MessageRouterObject messageRouterObject;
        /// <summary>
        /// Implementation of the Message Router Object (Class Code: 0x02) - Required Object according to CIP-Specification
        /// </summary>
        public ObjectLibrary.MessageRouterObject MessageRouterObject
        {
            get
            {
                if (messageRouterObject == null)
                    messageRouterObject = new ObjectLibrary.MessageRouterObject(this);
                return messageRouterObject;
            }
        }

        ObjectLibrary.AssemblyObject assemblyObject;
        /// <summary>
        /// Implementation of the Assembly Object (Class Code: 0x04)
        /// </summary>
        public ObjectLibrary.AssemblyObject AssemblyObject
        {
            get
            {
                if (assemblyObject == null)
                    assemblyObject = new ObjectLibrary.AssemblyObject(this);
                return assemblyObject;
            }
        }

        ObjectLibrary.TcpIpInterfaceObject tcpIpInterfaceObject;
        /// <summary>
        /// Implementation of the TCP/IP Object (Class Code: 0xF5) - Required Object according to CIP-Specification
        /// </summary>
        public ObjectLibrary.TcpIpInterfaceObject TcpIpInterfaceObject
        {
            get
            {
                if (tcpIpInterfaceObject == null)
                    tcpIpInterfaceObject = new ObjectLibrary.TcpIpInterfaceObject(this);
                return tcpIpInterfaceObject;

            }
        }

        /// <summary>
        /// Returns the "Bool" State of a byte Received via getAttributeSingle
        /// </summary>
        /// <param name="inputByte">byte to convert</param> 
        /// <param name="bitposition">bitposition to convert (First bit = bitposition 0)</param> 
        /// <returns>Converted bool value</returns>
        public static bool GetBit8(byte inputByte, int bitposition)
        {
           
            return (((inputByte>>bitposition)&0x01) != 0) ? true : false;
        }
    }

    public enum ConnectionType : byte
    {
        Null = 0,
        Multicast = 1,
        Point_to_Point = 2
    }

    public enum Priority : byte
    {
        Low = 0,
        High = 1,
        Scheduled = 2,
        Urgent = 3
    }

    public enum RealTimeFormat : byte
    {
        Modeless = 0,
        ZeroLength = 1,
        Heartbeat = 2,
        Header32Bit = 3
    }
}
