using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sres.Net.EEIP
{
    class Logix
    {
    }

    

    public class LogixTag
    {
        //Volume 1 Appendix C-2.1.1 Elementary Data Types
        internal static Dictionary<byte, Tuple<string, int>> TagTypes = new Dictionary<byte, Tuple<string, int>>()
        {
            {0xA0,Tuple.Create("STRUCT",0)},
            {0xC1,Tuple.Create("BOOL",1)},
            {0xC2,Tuple.Create("SINT",1)},
            {0xC3,Tuple.Create("INT",2)},
            {0xC4,Tuple.Create("DINT",4)},
            {0xC5,Tuple.Create("LINT",5)},
            {0xC6,Tuple.Create("USINT",1)},
            {0xC7,Tuple.Create("UINT",2)},
            {0xC8,Tuple.Create("UDINT",4)},
            {0xC9,Tuple.Create("ULINT",8)},
            {0xCA,Tuple.Create("REAL",4)},
            {0xCB,Tuple.Create("LREAL",8)},
            {0xCC,Tuple.Create("STIME",0)},
            {0xCD,Tuple.Create("DATE",0)},
            {0xCE,Tuple.Create("TIME_OF_DAY",0)},
            {0xCF,Tuple.Create("DATE_AND_TIME",0)},
            {0xD0,Tuple.Create("STRING",0)},
            {0xD1,Tuple.Create("BYTE",1)},
            {0xD2,Tuple.Create("WORD",2)},
            {0xD3,Tuple.Create("DWORD",4)},
            {0xD4,Tuple.Create("LWORD",8)},
            {0xD5,Tuple.Create("STRING2",0)},
            {0xD6,Tuple.Create("FTIME",0)},
            {0xD7,Tuple.Create("LTIME",0)},
            {0xD8,Tuple.Create("ITIME",0)},
            {0xD9,Tuple.Create("STRINGN",0)},
            {0xDA,Tuple.Create("SHORT_STRING",0)},
            {0xDB,Tuple.Create("TIME",0)},
            {0xDC,Tuple.Create("EPATH",0)},
            {0xDC,Tuple.Create("ENGUNIT",0)}
        };
        public string TagName = "";

    }
}
