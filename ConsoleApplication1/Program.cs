using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sres.Net.EEIP;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            EEIPClient eeipClient = new EEIPClient();
            eeipClient.IPAddress = "10.100.21.30";
            eeipClient.RegisterSession();

            byte[] response = eeipClient.GetAttributeSingle(0x01, 1, 1);
            var resp_indentity = (response[1] * 255 + response[0]).ToString();
            Console.WriteLine("Revision of indentity object is " + resp_indentity);

            //read simple tag
            response = eeipClient.readTag("testEIPRead");
            Console.WriteLine(BitConverter.ToString(response));
            Console.WriteLine(String.Format("Read {0} bytes", response.Length));
            Console.WriteLine(BitConverter.ToInt32(response,0).ToString());

            //read simple udt
            response = eeipClient.readTag("toVision");
            Console.WriteLine(BitConverter.ToString(response));
            Console.WriteLine(String.Format("Read {0} bytes",response.Length));

            //read simple udt to be able to write to it
            response = eeipClient.readTag("fromVision");
            Console.WriteLine(BitConverter.ToString(response));
            Console.WriteLine(String.Format("Read {0} bytes", response.Length));

            eeipClient.UnRegisterSession();
            Console.ReadKey();
       
        }
    }
}
