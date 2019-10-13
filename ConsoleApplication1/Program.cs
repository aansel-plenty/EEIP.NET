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
    class Program
    {
        static void Main(string[] args)
        {
            var eeipClient = new EEIPClient();
            eeipClient.IPAddress = "10.100.21.30";
            eeipClient.RegisterSession();

            byte[] response = eeipClient.GetAttributeSingle(0x01, 1, 1);
            var resp_identity = (response[1] << 8 | response[0]).ToString();
            Console.WriteLine("Revision of identity object is " + resp_identity);

            //read simple tag
            response = eeipClient.ReadTagSingle("testEIPRead");
            Console.WriteLine();
            Console.WriteLine(BitConverter.ToString(response));
            Console.WriteLine("Read {0} bytes", response.Length);
            Console.WriteLine(BitConverter.ToInt32(response,0).ToString());

            //read slightly more complicated tag (bit access)
            //response = eeipClient.ReadTagSingle("testEIPRead.1");
            //Console.WriteLine();
            //Console.WriteLine(BitConverter.ToString(response));
            //Console.WriteLine("Read {0} bytes", response.Length);
            //Console.WriteLine(BitConverter.ToInt32(response, 0).ToString());

            //read slightly more complicated tag (udt)
            response = eeipClient.ReadTagSingle("fromVision.heartbeat");
            Console.WriteLine();
            Console.WriteLine(BitConverter.ToString(response));
            Console.WriteLine("Read {0} bytes", response.Length);
            Console.WriteLine(BitConverter.ToInt32(response, 0).ToString());

            //read simple tag
            response = eeipClient.ReadTagSingle("testEIPWrite");
            Console.WriteLine();
            Console.WriteLine(BitConverter.ToString(response));
            Console.WriteLine("Read {0} bytes", response.Length);
            var testEIPwrite = BitConverter.ToInt32(response, 0);
            Console.WriteLine(testEIPwrite);

            //now write to the tag
            var success = eeipClient.WriteTagSingle("testEIPWrite", 0x00C4, BitConverter.GetBytes(testEIPwrite + 1));

            //read simple udt
            response = eeipClient.ReadTagSingle("toVision");
            Console.WriteLine();
            Console.WriteLine(BitConverter.ToString(response));
            Console.WriteLine("Read {0} bytes", response.Length);

            //read simple udt to be able to write to it
            response = eeipClient.ReadTagSingle("fromVision");
            Console.WriteLine();
            Console.WriteLine(BitConverter.ToString(response));
            Console.WriteLine("Read {0} bytes", response.Length);

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
                response = eeipClient.ReadTagSingle("testEIPWrite");
                var readVal = BitConverter.ToInt32(response, 0);
                var writeVal = readVal + 1;
                eeipClient.WriteTagSingle("testEIPWrite", 0x00C4, BitConverter.GetBytes(writeVal));
                //Console.WriteLine("Read {0}, Wrote {1}",readVal,writeVal);
                //Console.WriteLine("Elapsed time {0} ms", stopWatch.ElapsedMilliseconds);
            }
            
            Console.WriteLine("Elapsed time {0} ms",stopWatch.ElapsedMilliseconds);
            
            eeipClient.UnRegisterSession();
            Console.ReadKey();
        }
    }
}
