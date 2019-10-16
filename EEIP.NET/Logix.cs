using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sres.Net.EEIP
{
    public class Logix : EEIPClient
    {
        public enum Logix5000Services : byte
        {
            Read_Tag_Service = 0x4C,
            Read_Tag_Fragmented_Service = 0x52,
            Write_Tag_Service = 0x4D,
            Write_Tag_Fragmented_Service = 0x53,
            Read_Modify_Write_Tag_Service = 0x4E
        }

        /// <summary>
        /// Volume 1 Appendix C-2.1.1 Elementary Data Types
        /// </summary>
        internal static Dictionary<byte, Tuple<string, int>> TagTypes = new Dictionary<byte, Tuple<string, int>>()
        {
            {0xA0,Tuple.Create("STRUCT",0)},
            {0xC1,Tuple.Create("BOOL",1)},
            {0xC2,Tuple.Create("SINT",1)},
            {0xC3,Tuple.Create("INT",2)},
            {0xC4,Tuple.Create("DINT",4)},
            {0xC5,Tuple.Create("LINT",8)},
            {0xC6,Tuple.Create("USINT",1)},
            {0xC7,Tuple.Create("UINT",2)},
            {0xC8,Tuple.Create("UDINT",4)},
            {0xC9,Tuple.Create("ULINT",8)},
            {0xCA,Tuple.Create("REAL",4)},
            {0xCB,Tuple.Create("LREAL",8)},
            {0xCC,Tuple.Create("STIME",0)},
            {0xCD,Tuple.Create("DATE",2)},
            {0xCE,Tuple.Create("TIME_OF_DAY",4)},
            {0xCF,Tuple.Create("DATE_AND_TIME",6)},
            {0xD0,Tuple.Create("STRING",0)},
            {0xD1,Tuple.Create("BYTE",1)},
            {0xD2,Tuple.Create("WORD",2)},
            {0xD3,Tuple.Create("DWORD",4)},
            {0xD4,Tuple.Create("LWORD",8)},
            {0xD5,Tuple.Create("STRING2",0)},
            {0xD6,Tuple.Create("FTIME",4)},
            {0xD7,Tuple.Create("LTIME",8)},
            {0xD8,Tuple.Create("ITIME",0)},
            {0xD9,Tuple.Create("STRINGN",0)},
            {0xDA,Tuple.Create("SHORT_STRING",0)},
            {0xDB,Tuple.Create("TIME",4)},
            {0xDC,Tuple.Create("EPATH",0)},
            {0xDD,Tuple.Create("ENGUNIT",0)}
        };
        public static Dictionary<string, Tuple<byte, int>> TagStringTypes = new Dictionary<string, Tuple<byte, int>>();

        public Dictionary<string, LogixTag> TagCache = new Dictionary<string, LogixTag>();
        private bool RefreshTagRegistry = true;
        private List<int> LastControllerState = new List<int>() { 1, 1, 1, 1, 1 };
        private List<int> ControllerState = new List<int>() { 0, 0, 0, 0, 0 };

        public Logix()
        {
            //build reverse lookup for convenience
            foreach (var keyValuePair in TagTypes)
            {
                TagStringTypes[keyValuePair.Value.Item1] = Tuple.Create(keyValuePair.Key, keyValuePair.Value.Item2);
            }
        }

        public bool CheckForControllerChange()
        {
            var attributes = new List<UInt16>() { 1, 2, 3, 4, 10 };
            //TODO: read something
            var reply = GetAttributeList(0xAC, 0x0001, attributes);

            var replyService = reply[0];
            var generalStatus = reply[2];
            var extendedStatus = reply[3];
            var numAttributes = reply[5] << 8 | reply[4];

            var offset = 6;

            //Check general status
            if (!this.RefreshTagRegistry)
            {
                switch (generalStatus)
                {
                    case 0x05:
                        Console.WriteLine("Read general status 0x05: Path not known, download is in progress");
                        break;
                    case 0x10:
                        Console.WriteLine("Read general status 0x10: Device state conflict, controller is password locked");
                        break;
                    default:
                        break;
                }
            }
            
            //Read attribute 1
            if ((reply[offset+1] << 8 | reply[offset]) == attributes[0])
            {
                offset += 2;
                if ((reply[offset + 1] << 8 | reply[offset]) == CIPGeneralStatusCodes.CIP_SERVICE_SUCCESS) {
                    offset += 2;
                    this.ControllerState[0] = (reply[offset + 1] << 8 | reply[offset]);
                }
                else
                {
                    this.RefreshTagRegistry = true;
                }
            }
            offset += 2;

            //Read attribute 2
            if ((reply[offset + 1] << 8 | reply[offset]) == attributes[1])
            {
                offset += 2;
                if ((reply[offset + 1] << 8 | reply[offset]) == CIPGeneralStatusCodes.CIP_SERVICE_SUCCESS)
                {
                    offset += 2;
                    this.ControllerState[1] = (reply[offset + 1] << 8 | reply[offset]);
                }
                else
                {
                    this.RefreshTagRegistry = true;
                }
            }
            offset += 2;

            //Read attribute 3
            if ((reply[offset + 1] << 8 | reply[offset]) == attributes[2])
            {
                offset += 2;
                if ((reply[offset + 1] << 8 | reply[offset]) == CIPGeneralStatusCodes.CIP_SERVICE_SUCCESS)
                {
                    offset += 2;
                    this.ControllerState[2] = (reply[offset + 3] << 24 | reply[offset + 2] << 16 | reply[offset + 1] << 8 | reply[offset]);
                }
                else
                {
                    this.RefreshTagRegistry = true;
                }
            }
            offset += 4;

            //Read attribute 4
            if ((reply[offset + 1] << 8 | reply[offset]) == attributes[3])
            {
                offset += 2;
                if ((reply[offset + 1] << 8 | reply[offset]) == CIPGeneralStatusCodes.CIP_SERVICE_SUCCESS)
                {
                    offset += 2;
                    this.ControllerState[3] = (reply[offset + 3] << 24 | reply[offset + 2] << 16 | reply[offset + 1] << 8 | reply[offset]);
                }
                else
                {
                    this.RefreshTagRegistry = true;
                }
            }
            offset += 4;

            //Read attribute 10
            if ((reply[offset + 1] << 8 | reply[offset]) == attributes[4])
            {
                offset += 2;
                if ((reply[offset + 1] << 8 | reply[offset]) == CIPGeneralStatusCodes.CIP_SERVICE_SUCCESS)
                {
                    offset += 2;
                    this.ControllerState[4] = (reply[offset + 3] << 24 | reply[offset + 2] << 16 | reply[offset + 1] << 8 | reply[offset]);
                }
                else
                {
                    this.RefreshTagRegistry = true;
                }
            }

            if (!ControllerState.Equals(LastControllerState))
            {
                this.RefreshTagRegistry = true;
            }

            if (this.RefreshTagRegistry)
            {
                TagCache.Clear();
            }

            LastControllerState = ControllerState;

            return this.RefreshTagRegistry;
        }

        /// <summary>
        /// Builds request path for tag access (r/w)
        /// Logix 5000 Controllers Data Access 1756-PM020F-EN-P
        /// </summary>
        public byte[] GetRequestPath(string tagPath)
        {
            var requestPathData = new List<byte>();
            var splitPaths = tagPath.Split('.').ToList(); //remove udt and bit access junk

            //Check if tag exists in the registry
            //if (TagRegistry.TryGetValue(tagPath,out LogixTag logixTag))
            //{
            //    if (logixTag.DataKeyword == "STRUCT" & logixTag.InstanceID != 0)
            //    {
            //
            //    }
            //}

            if (int.TryParse(splitPaths.Last(), out var bit))  //check for bit level access
            {
                if (bit >= 0 & bit <= 31)
                {
                    throw new CIPException("Bit access not supported!");
                }
                else
                {
                    throw new CIPException(String.Format("Out of bounds bit access for bit @ {0}", bit));
                }
            }
            else
            {
                foreach (var splitPath in splitPaths)
                {
                    var path = splitPath;
                    var isArray = (splitPath.IndexOf("[") != -1); //square brace means this path segment contains an array
                    var arrayIndices = new List<int>();

                    if (isArray) //check for array
                    {
                        var startIndex = splitPath.IndexOf("[");
                        if (splitPath.Last() != ']') //we must end on the closing square brace
                        {
                            throw new CIPException("End brace (]) misplaced in array!");
                        }

                        path = splitPath.Substring(0, startIndex);
                        var stringIndices = splitPath.Substring(startIndex + 1, splitPath.Count() - startIndex - 2).Split(',');
                        arrayIndices = stringIndices.Select(int.Parse).ToList();
                    }
                    var padTagPath = (path.Length % 2 == 1);
                    var requestPathSize = (tagPath.Length + 2 + (padTagPath ? 1 : 0)) / 2;

                    requestPathData.Add(0x91); //Logical segment
                    requestPathData.Add((byte)path.Length); //number of chars in tag path
                    requestPathData.AddRange(Encoding.ASCII.GetBytes(path)); //tag path string
                    if (padTagPath) requestPathData.Add(0x00); //add pad byte if odd number of bytes

                    if (isArray)
                    {
                        foreach (var arrayIndex in arrayIndices)
                        {
                            if (arrayIndex <= byte.MaxValue)
                            {
                                requestPathData.Add(0x28);
                                requestPathData.Add((byte)arrayIndex);
                            }
                            else if (arrayIndex <= UInt16.MaxValue)
                            {
                                requestPathData.Add(0x29);
                                requestPathData.AddRange(BitConverter.GetBytes((UInt16)arrayIndex));
                            }
                            else
                            {
                                requestPathData.Add(0x2A);
                                requestPathData.AddRange(BitConverter.GetBytes((UInt32)arrayIndex));
                            }
                        }
                    }
                }
            }

            return requestPathData.ToArray();
        }

        internal List<byte> BuildReadTagData(string tagPath)
        {
            var data = new List<byte>();

            var requestPathData = GetRequestPath(tagPath);
            data.Add((byte)Logix.Logix5000Services.Read_Tag_Service); //requested service
            data.Add((byte)(requestPathData.Length / 2)); //Requested Path size (number of 16 bit words)
            data.AddRange(requestPathData); //Request Path
            data.AddRange(BitConverter.GetBytes((Int16)0x0001)); //Number of elements to read

            return data;
        }

        public byte[] ReadTagSingle(string tagPath)
        {
            Encapsulation.CommonPacketFormat commonPacketFormat = new Encapsulation.CommonPacketFormat();

            commonPacketFormat.Data.AddRange(BuildReadTagData(tagPath));

            var encapsulation = BuildUCMMHeader(Encapsulation.CommandsEnum.SendRRData, commonPacketFormat);
            var recvData = GetUCMMreply(encapsulation, commonPacketFormat);

            var tagTypeServiceParam = (UInt16)(recvData[45] << 8 | recvData[44]);
            var isUDT = (tagTypeServiceParam == 0x02A0);
            var replyDataOffset = isUDT ? 48 : 46;
            var returnData = new byte[recvData.Length - replyDataOffset];

            if (!TagCache.ContainsKey(tagPath)) //Add tag to registry
            {
                TagCache[tagPath] = new LogixTag()
                {
                    TagName = tagPath,
                    SymbolType = tagTypeServiceParam,
                    StructureHandle = (UInt16)(isUDT ? ((recvData[47] << 8) | recvData[46]) : 0x0000)
                };
            }

            System.Buffer.BlockCopy(recvData, replyDataOffset, returnData, 0, recvData.Length - replyDataOffset);

            var tag = TagCache[tagPath];
            tag.rawData = returnData.ToList();
            tag.UpdateValue();

            return returnData;
        }

        internal List<byte> BuildWriteTagData(string tagPath, UInt16 tagType, byte[] tagData)
        {
            var data = new List<byte>();

            var requestPathData = GetRequestPath(tagPath);
            data.Add((byte)Logix.Logix5000Services.Write_Tag_Service); //requested service
            data.Add((byte)(requestPathData.Length / 2)); //Requested Path size (number of 16 bit words)
            data.AddRange(requestPathData); //Request Path
            data.AddRange(BitConverter.GetBytes(tagType)); //get the type of the tag
            //For structs add the structure handle
            if (TagCache.TryGetValue(tagPath,out LogixTag value))
            {
                if (value.DataKeyword == "STRUCT")
                {
                    data.AddRange(BitConverter.GetBytes(value.StructureHandle));
                }
            }
            data.AddRange(BitConverter.GetBytes((Int16)0x0001)); //Number of elements to write
            data.AddRange(tagData);

            return data;
        }

        public bool WriteTagSingle(string tagPath, UInt16 tagType, byte[] tagData)
        {
            Encapsulation.CommonPacketFormat commonPacketFormat = new Encapsulation.CommonPacketFormat();

            var requestPathData = GetRequestPath(tagPath);

            commonPacketFormat.Data.AddRange(BuildWriteTagData(tagPath, tagType, tagData));

            var encapsulation = BuildUCMMHeader(Encapsulation.CommandsEnum.SendRRData, commonPacketFormat);
            var recvData = GetUCMMreply(encapsulation, commonPacketFormat);

            return true;
        }

        public bool WriteTagSingle(string tagPath, byte[] tagData)
        {
            if (!TagCache.TryGetValue(tagPath,out LogixTag tag))
            {
                ReadTagSingle(tagPath);  
            }
            return WriteTagSingle(tagPath, tag.SymbolType, tagData);
        }

        //Add read write and controller change

        public byte[] MultiReadWrite(List<string> readTags, List<Tuple<string,byte[]>> writeTags)
        {
            var services = new List<byte[]>();
            //add all read services
            foreach (var tag in readTags)
            {
                services.Add(BuildReadTagData(tag).ToArray());
            }
            //add all write services
            foreach (var tag in writeTags)
            {
                var tagPath = tag.Item1;
                var tagData = tag.Item2;
                if (!TagCache.ContainsKey(tagPath))
                {
                    ReadTagSingle(tagPath);
                }
                services.Add(BuildWriteTagData(tag.Item1, TagCache[tagPath].SymbolType, tagData).ToArray());
            }
            return MultiServicePacket(services);
        }
    }

    public class LogixTag
    {
        public string TagName = "";
        public UInt16 SymbolType = 0x00;
        public UInt32 InstanceID { get => (UInt16) (SymbolType & 0x0FFF); }
        public string DataKeyword
        { 
            get 
            {
                var success = Logix.TagTypes.TryGetValue((byte) (SymbolType & 0xFF),out var returnValue);
                if (success)
                {
                    return returnValue.Item1;
                }
                else
                {
                    return "UNKNOWN";
                }
            }
        }
        public bool IsReserved { get => ((SymbolType >> 12) & 0x1) != 0; }
        public bool IsArray { get => ((SymbolType >> 13) & 0x3) != 0; }
        public int ArrayNumDims { get => ((SymbolType >> 13) & 0x3); }
        public bool IsAtomic { get => ((SymbolType >> 15) & 0x1) == 0; }
        public bool IsStruct { get => ((SymbolType >> 15) & 0x1) != 0; }
        public UInt16 StructureHandle;

        public List<byte> rawData = new List<byte>();
        private object LastValue;
        private object PreviousValue;
        public object Value;

        public void UpdateValue()
        {
            switch (DataKeyword)
            {
                case "STRUCT":
                    Value = rawData;
                    break;
                case "BOOL":
                    Value = (bool) (rawData[0] > 0);
                    break;
                case "SINT":
                    Value = rawData[0];
                    break;
                case "INT":
                    Value = BitConverter.ToInt16(rawData.ToArray(), 0);
                    break;
                case "DINT":
                    Value = BitConverter.ToInt32(rawData.ToArray(), 0);
                    break;
                case "LINT":
                    Value = BitConverter.ToInt64(rawData.ToArray(), 0);
                    break;
                case "REAL":
                    Value = BitConverter.ToSingle(rawData.ToArray(), 0);
                    break;
                case "STRING": //TODO: double check this. definitely going to be wrong
                    Value = BitConverter.ToString(rawData.ToArray(), 0);
                    break;
                default:
                    break;
            }
            if (!Value.Equals(LastValue))
            {
                //value has changed
                PreviousValue = LastValue;
                LastValue = Value;
            }
        }
    }
}
