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
            var plc = new Logix();

            plc.IPAddress = "10.100.21.30";
            plc.RegisterSession();

            //Testing Forward open
            plc.TransportType = 0x83;
            //plc.ForwardOpen();

            //plc.ForwardClose();

            byte[] response = plc.GetAttributeSingle(0x01, 1, 1);
            var resp_identity = (response[1] << 8 | response[0]).ToString();
            Console.WriteLine("Revision of identity object is " + resp_identity);

            var refresh = plc.CheckForControllerChange();

            //read simple tag
            response = plc.ReadTagSingle("testEIPRead");
            Console.WriteLine();
            Console.WriteLine(BitConverter.ToString(response));
            Console.WriteLine("Read {0} bytes", response.Length);
            Console.WriteLine(BitConverter.ToInt32(response,0).ToString());

            //read slightly more complicated tag (bit access)
            //response = plc.client.ReadTagSingle("testEIPRead.1");
            //Console.WriteLine();
            //Console.WriteLine(BitConverter.ToString(response));
            //Console.WriteLine("Read {0} bytes", response.Length);
            //Console.WriteLine(BitConverter.ToInt32(response, 0).ToString());

            //read slightly more complicated tag (array access)
            response = plc.ReadTagSingle("opcArray[0]");
            Console.WriteLine();
            Console.WriteLine(BitConverter.ToString(response));
            Console.WriteLine("Read {0} bytes", response.Length);
            Console.WriteLine(BitConverter.ToInt32(response, 0).ToString());

            //read slightly more complicated tag (udt)
            response = plc.ReadTagSingle("fromVision.heartbeat");
            Console.WriteLine();
            Console.WriteLine(BitConverter.ToString(response));
            Console.WriteLine("Read {0} bytes", response.Length);
            Console.WriteLine(BitConverter.ToInt32(response, 0).ToString());

            //read simple tag
            response = plc.ReadTagSingle("testEIPWrite");
            Console.WriteLine();
            Console.WriteLine(BitConverter.ToString(response));
            Console.WriteLine("Read {0} bytes", response.Length);
            var testEIPwrite = BitConverter.ToInt32(response, 0);
            var testval = testEIPwrite;
            Console.WriteLine(testEIPwrite);

            //now write to the tag
            var success = plc.WriteTagSingle("testEIPWrite", 0x00C4, BitConverter.GetBytes(testEIPwrite + 1));

            //read simple udt
            response = plc.ReadTagSingle("toVision");
            Console.WriteLine();
            Console.WriteLine(BitConverter.ToString(response));
            Console.WriteLine("Read {0} bytes", response.Length);

            //read simple udt
            response = plc.ReadTagSingle("fromVision.towerCamera[0].offset1");
            Console.WriteLine();
            Console.WriteLine(BitConverter.ToString(response));
            Console.WriteLine("Read {0} bytes", response.Length);

            //read simple udt to be able to write to it
            response = plc.ReadTagSingle("fromVision");
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
            foreach (var item in visionData.cameraData)
            {
                Console.WriteLine("Status is {0}, offset is {1}",item.status,item.offset);
            }

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();
            
            //Test read and write latency
            //for (int i = 0; i <= 100; i++)
            //{
            //    response = plc.ReadTagSingle("testEIPWrite");
            //    var readVal = BitConverter.ToInt32(response, 0);
            //    var writeVal = readVal + 1;
            //    testval = writeVal;
            //    plc.WriteTagSingle("testEIPWrite", 0x00C4, BitConverter.GetBytes(writeVal));
            //    //Console.WriteLine("Read {0}, Wrote {1}",readVal,writeVal);
            //    //Console.WriteLine("Elapsed time {0} ms", stopWatch.ElapsedMilliseconds);
            //}
            //
            //Console.WriteLine("Elapsed time {0} ms", stopWatch.ElapsedMilliseconds);
            
            var readTags = new List<string>() { "testEIPWrite","fromVision"};
            var writeTags = new List<Tuple<string, UInt16, byte[]>>();
            writeTags.Add(Tuple.Create("testEIPwrite", (UInt16)0x00C4, BitConverter.GetBytes(testval)));
            writeTags.Add(Tuple.Create("fromVision.towerCamera[0].offset1", (UInt16)0xCA, BitConverter.GetBytes(1234.5678f)));
            var resp = plc.MultiReadWrite(readTags, writeTags);
            Console.WriteLine();
            Console.WriteLine("Multi read/write succeeded!");
            Console.WriteLine(BitConverter.ToString(resp));
            var numTags = resp[1] << 8 | resp[0];
            for (int i = 0; i < numTags; i++)
            {
                var offset = 2 + 2 * i;
                var offsetIndex = ((resp[offset + 1] << 8) | resp[offset]);
                if (resp[offsetIndex] == 0xCC)
                {
                    if (resp[offsetIndex + 4] == 0xC4)
                    {
                        var readval = BitConverter.ToInt32(resp.ToArray(), offsetIndex + 6);
                        //Console.WriteLine(readval);
                    }
                }
            }

            //Time 100 multi read writes
            stopWatch.Restart();

            for (int i = 0; i <= 100; i++)
            {
                //Console.WriteLine("Elapsed time {0} ms", stopWatch.ElapsedMilliseconds);
                writeTags[0] = (Tuple.Create("testEIPwrite",(UInt16) 0x00C4, BitConverter.GetBytes(testval++)));
                resp = plc.MultiReadWrite(readTags, writeTags);
                numTags = resp[1] << 8 | resp[0];
                for (int j = 0; j < numTags; j++)
                {
                    var offset = 2 + 2 * j;
                    var offsetIndex = ((resp[offset + 1] << 8) | resp[offset]);
                    if (resp[offsetIndex] == 0xCC)
                    {
                        if (resp[offsetIndex+4] == 0xC4)
                        {
                            var readval = BitConverter.ToInt32(resp.ToArray(), offsetIndex + 6);
                            Console.WriteLine(readval);
                        }
                    }  
                }
            }
            
            Console.WriteLine("Elapsed time {0} ms",stopWatch.ElapsedMilliseconds);

            plc.UnRegisterSession();
            Console.ReadKey();
        }
    }
}
