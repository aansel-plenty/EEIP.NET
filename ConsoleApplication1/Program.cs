using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sres.Net.EEIP;

namespace ConsoleApplication1
{
    public struct CameraData
    {
        public Int32 status;
        public float offset;
    }

    public class VisionRecvData
    {
        public Int32 heartbeat;
        public List<CameraData> cameraData;

        public VisionRecvData()
        {
            heartbeat = 0;
            cameraData = new List<CameraData>();
        }
    }
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

            VisionRecvData visionData = new VisionRecvData();
            visionData.heartbeat = BitConverter.ToInt32(response,0);
            
            for (int i = 0; i < 8; i++)
            {
                CameraData c1 = new CameraData();
                c1.status = BitConverter.ToInt32(response, 4 + 8 * i);
                c1.offset = BitConverter.ToSingle(response, 8 + 8 * i);
                visionData.cameraData.Add(c1);
            }

            //Just output for debug
            Console.WriteLine();
            Console.WriteLine("Hearbeat is {0}", visionData.heartbeat);
            foreach (var item in visionData.cameraData)
            {
                Console.WriteLine("Status is {0}, offset is {1}",item.status,item.offset);
            }

            eeipClient.UnRegisterSession();
            Console.ReadKey();
       
        }
    }
}
