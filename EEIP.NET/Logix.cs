using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sres.Net.EEIP
{
    public class Logix
    {
        public enum Logix5000Services : byte
        {
            Read_Tag_Service = 0x4C,
            Read_Tag_Fragmented_Service = 0x52,
            Write_Tag_Service = 0x4D,
            Write_Tag_Fragmented_Service = 0x53,
            Read_Modify_Write_Tag_Service = 0x4E
        }

        public bool CheckForControllerChange()
        {
            var attributes = new List<UInt16>() { 1, 2, 3, 4, 10 };
            return true;
        }
    }

    public class LogixTag
    {
        public string TagName = "";
        
        private UInt16 SymbolType = 0x00;
        public UInt32 InstanceID { get => (UInt16) (SymbolType & 0x0FFF); }
        public string DataKeyword
        { 
            get 
            {
                var success = TagTypes.TryGetValue((byte) (SymbolType & 0xF),out var returnValue);
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
        private UInt16 StructureHandle = 0x00;

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

        /// <summary>
        /// Builds request path for tag access (r/w)
        /// Logix 5000 Controllers Data Access 1756-PM020F-EN-P
        /// </summary>
        internal static byte[] GetRequestPath(string tagPath)
        {
            var requestPathData = new List<byte>();
            var splitPaths = tagPath.Split('.').ToList(); //remove udt and bit access junk

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
    }
}
