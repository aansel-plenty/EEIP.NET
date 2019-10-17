using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sres.Net.EEIP
{

    /// <summary>
    /// Table A-3.1 Volume 1 Chapter A-3
    /// </summary>
    public enum CIPCommonServices : byte
    {
        Get_Attributes_All = 0x01,
        Set_Attributes_All_Request = 0x02,
        Get_Attribute_List = 0x03,
        Set_Attribute_List = 0x04,
        Reset = 0x05,
        Start = 0x06,
        Stop = 0x07,
        Create = 0x08,
        Delete = 0x09,
        Multiple_Service_Packet = 0x0A,
        Apply_Attributes = 0x0D,
        Get_Attribute_Single = 0x0E,
        Set_Attribute_Single = 0x10,
        Find_Next_Object_Instance = 0x11,
        Error_Response = 0x14,
        Restore = 0x15,
        Save = 0x16,
        NOP = 0x17,
        Get_Member = 0x18,
        Set_Member = 0x19,
        Insert_Member = 0x1A,
        Remove_Member = 0x1B,
        GroupSync = 0x1C
    }

    /// <summary>
    /// Table 3-5.5. Connection Manager Object Instance Object Specific Services
    /// </summary>
    public enum CIPConnectionServices : byte
    {
        Forward_Close = 0x4E,
        Unconnected_Send = 0x52,
        Forward_Open = 0x54,
        Get_Connection_Data = 0x56,
        Search_Connection_data = 0x57,
        Ex_Forward_Open = 0x59,
        Get_Connection_Owner = 0x5A,
        Large_Forward_Open = 0x5B
    }

    public class CIPException : Exception
    {
        public CIPException()
        {
        }

        public CIPException(string message)
            : base(message)
        {
        }

        public CIPException(string message, Exception inner)
            : base(message, inner)
        {
        }
    }

    /// <summary>
    /// Table B-1.1 CIP General Status Codes
    /// </summary>
    public static class CIPGeneralStatusCodes
    {
        public static Dictionary<byte, string> GetStatus(byte code)
        {
            var statusName = GetStatusName(code);
            return new Dictionary<byte, string>
                {
                    {code,statusName}
                };
        }
        public static string GetStatusName(byte code)
        {
            if (!StatusCodes.TryGetValue(code, out string statusName))
            {
                if (code >= 0x2B & code <= 0xCF)
                {
                    statusName = "Reserved by CIP for future extensions";
                }
                else  //between 0xD0 and 0xFF
                {
                    statusName = "Reserved for Object Class and service errors";
                }
            }
            return statusName;
        }

        public const byte CIP_SERVICE_SUCCESS = 0x00;

        internal static Dictionary<byte,string> StatusCodes = new Dictionary<byte,string>()
        {
            {0x00,"Success"},
            {0x01,"Connection failure"},
            {0x02,"Resource unavailable"},
            {0x03,"Invalid Parameter value"},
            {0x04,"Path segment error"},
            {0x05,"Path destination unknown"},
            {0x06,"Partial transfer"},
            {0x07,"Connection lost"},
            {0x08,"Service not supported"},
            {0x09,"Invalid attribute value"},
            {0x0A,"Attribute List error"},
            {0x0B,"Already in requested mode/state"},
            {0x0C,"Object state conflict"},
            {0x0D,"Object already exists"},
            {0x0E,"Attribute not settable"},
            {0x0F,"Privilege violation"},
            {0x10,"Device state conflict"},
            {0x11,"Reply data too large"},
            {0x12,"Fragmentation of a primitive value"},
            {0x13,"Not enough data"},
            {0x14,"Attribute not supported"},
            {0x15,"Too much data"},
            {0x16,"Object does not exist"},
            {0x17,"Service fragmentation sequence not in progress"},
            {0x18,"No stored attribute data"},
            {0x19,"Store operation failure"},
            {0x1A,"Routing failure, request packet too large"},
            {0x1B,"Routing failure, response packet too large"},
            {0x1C,"Missing attribute list entry data"},
            {0x1D,"Invalid attribute value list"},
            {0x1E,"Embedded service error"},
            {0x1F,"Vendor specific error"},
            {0x20,"Invalid parameter"},
            {0x21,"Write-once value or medium atready written"},
            {0x22,"Invalid Reply Received"},
            {0x23,"Buffer overflow"},
            {0x24,"Message format error"},
            {0x25,"Key failure path"},
            {0x26,"Path size invalid"},
            {0x27,"Unecpected attribute list"},
            {0x28,"Invalid Member ID"},
            {0x29,"Member not settable"},
            {0x2A,"Group 2 only Server failure"},
            {0x2B,"Unknown Modbus Error"}
        };
    }
}
