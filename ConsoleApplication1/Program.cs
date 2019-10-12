using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
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
            var resp_identity = (response[1] << 8 | response[0]).ToString();
            Console.WriteLine("Revision of identity object is " + resp_identity);

            //read simple tag
            response = eeipClient.ReadTagSingle("testEIPRead");
            Console.WriteLine(BitConverter.ToString(response));
            Console.WriteLine(String.Format("Read {0} bytes", response.Length));
            Console.WriteLine(BitConverter.ToInt32(response,0).ToString());

            //read simple udt
            response = eeipClient.ReadTagSingle("toVision");
            Console.WriteLine(BitConverter.ToString(response));
            Console.WriteLine(String.Format("Read {0} bytes",response.Length));

            //read simple udt to be able to write to it
            response = eeipClient.ReadTagSingle("fromVision");
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
            Console.WriteLine("Heartbeat is {0}", visionData.heartbeat);
            foreach (CameraData item in visionData.cameraData)
            {
                Console.WriteLine("Status is {0}, offset is {1}",item.status,item.offset);
            }

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            //Test read and write latency
            for (int i = 0; i < 100; i++)
            {
                //read simple tag
                response = eeipClient.ReadTagSingle("fromVision");
                visionData.heartbeat = BitConverter.ToInt32(response, 0);
                //Console.WriteLine("Heartbeat is {0}", visionData.heartbeat);
                Console.WriteLine("Elapsed time {0} ms", stopWatch.ElapsedMilliseconds);
            }

            //Console.WriteLine("Elapsed time {0} ms",stopWatch.ElapsedMilliseconds);

            eeipClient.UnRegisterSession();
            Console.ReadKey();
       
        }
    }
}
