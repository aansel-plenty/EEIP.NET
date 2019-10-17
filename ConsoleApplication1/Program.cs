using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sres.Net.EEIP;
using static CM.ConnectionManager;

namespace ConsoleApplication1
{
    class Program
    {
        static void Main(string[] args)
        {
            //EIPTests();

            Task.Run(ControlLoop);

            while (true)
            {

            }

            //plc.UnRegisterSession();
            //Console.ReadKey();
        }

        public static async Task ControlLoop ()
        {
            var transplanter = new CM.ConnectionManager()
            {
                readTagPath = "toVision",
                writeTagPath = "fromVision"
            };
            var plc = new Logix()
            {
                IPAddress = "10.100.21.30", //Desk PLC
                TCPClientTimeout = 1000, //1 second timeout for PLC communications
                TransportType = 0x83
            };

            var readTags = new List<string> { transplanter.readTagPath };
            var writeTags = new List<Tuple<string, byte[]>> { Tuple.Create("", new byte[1]) };
            var counter = 0;
            var stopWatch = new Stopwatch();
            long curtime = 0;
            long lasttime = 0;
            stopWatch.Start();

            //Start main task

            while (true)
            {
                switch (transplanter.PLCState)
                {
                    case PLCStates.Timeout:
                        break;
                    case PLCStates.Offline:
                        plc.RegisterSession();
                        plc.ForwardOpen();
                        transplanter.PLCState = PLCStates.Connecting;
                        break;
                    case PLCStates.Connecting:
                        if (plc.IsRegistered)
                        {
                            transplanter.PLCState = PLCStates.Communicating;
                        }
                        break;
                    case PLCStates.Communicating:
                        var tasks = new List<Task>();
                        transplanter.Heartbeat++;
                        writeTags[0] = Tuple.Create(transplanter.writeTagPath, transplanter.GetBytesToWrite());
                        try
                        {
                            Task controlTask = GetReadCommandsAsync(plc, readTags, writeTags);
                            Task timeout = Task.Delay(20);
                            tasks.Add(controlTask);
                            tasks.Add(timeout);
                            Task finished = Task.WhenAll(tasks);
                            await finished;
                            counter++;
                            if (counter %50 == 0)
                            {
                                curtime = stopWatch.ElapsedMilliseconds;
                                Console.WriteLine("Elapsed time {0} ms, per loop {1}", stopWatch.ElapsedMilliseconds, (curtime-lasttime)/50.0);
                                lasttime = stopWatch.ElapsedMilliseconds;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Exception message: " + ex.Message);
                            //throw;
                        }
                        //Console.WriteLine("Elapsed time {0} ms", stopWatch.ElapsedMilliseconds);

                        //Thread.Sleep(20);
                        break;
                    default:
                        break;
                }
            }
        }


        public static async Task<List<CameraCommands>> GetReadCommandsAsync(Logix plc, List<string> readTags, List<Tuple<string, byte[]>> writeTags)
        {
            var reply = await Task.Run(() => plc.MultiReadWrite(readTags, writeTags));
            return ReadCommands(plc, reply);
        }
        public static List<CameraCommands> ReadCommands(Logix plc, byte[] reply)
        {
            var cameraCommands = new List<CameraCommands>();
            var numTags = reply[1] << 8 | reply[0];
            for (int i = 0; i < numTags; i++)
            {
                var offset = 2 + 2 * i;
                var offsetIndex = ((reply[offset + 1] << 8) | reply[offset]);
                var replyService = reply[offsetIndex];
                var statusCode = reply[offsetIndex + 2];
                var status = CIPGeneralStatusCodes.GetStatus(statusCode);
                if (status.ContainsKey(CIPGeneralStatusCodes.CIP_SERVICE_SUCCESS))
                {
                    switch (replyService)
                    {
                        case 0xCC:
                            var tagType = reply[offsetIndex+4];
                            switch (tagType)
                            {
                                case 0xA0:
                                    var structureHandle = BitConverter.ToUInt16(reply, offsetIndex + 6);
                                    if (plc.TagCache.TryGetValue("toVision",out LogixTag tag))
                                    {
                                        if (tag.StructureHandle == structureHandle)
                                        {
                                            var structOffset = offsetIndex + 8;
                                            
                                            for (int j = 0; j < 8; j++)
                                            {
                                                cameraCommands.Add((CameraCommands) BitConverter.ToInt32(reply,structOffset));
                                                structOffset += 4;
                                            }
                                            //for (int k = 0; k < 8; k++)
                                            //{
                                            //    Console.WriteLine("Camera {0} commanded to {1}", k+1, cameraCommands[k]);
                                            //}
                                        }
                                    }
                                    break;
                                default:
                                    break;
                            }
                            break;
                        case 0xCD:
                            break;
                        default:
                            break;
                    }
                }
            }
            return cameraCommands;
        }

        public static void EIPTests()
        {
            var plc = new Logix();
            var transplanter = new CM.ConnectionManager();

            plc.IPAddress = "10.100.21.30"; //Desk PLC
            //plc.IPAddress = "10.100.21.20"; //Workbench PLC
            //plc.IPAddress = "10.100.25.101"; //Tigris Washer???
            plc.RegisterSession();

            //Testing Forward open
            plc.TransportType = 0x83;
            plc.ForwardOpen();
            //System.Threading.Thread.Sleep(10000);
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
            Console.WriteLine(BitConverter.ToInt32(response, 0).ToString());

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

            //now write to the tag utilizing the tag registry
            var success = plc.WriteTagSingle("testEIPWrite", BitConverter.GetBytes(testEIPwrite + 1));

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

            transplanter.Heartbeat = BitConverter.ToInt32(response, 0);

            for (int i = 0; i < 8; i++)
            {
                var c1 = new CameraResponse();
                c1.status = (CameraStatus)BitConverter.ToInt32(response, 4 + 8 * i);
                c1.offset = BitConverter.ToSingle(response, 8 + 8 * i);
                transplanter.cameraData.Add(c1);
            }

            //Just output for debug
            Console.WriteLine();
            Console.WriteLine("Heartbeat is {0}", transplanter.Heartbeat);
            foreach (var item in transplanter.cameraData)
            {
                Console.WriteLine("Status is {0}, offset is {1}", item.status, item.offset);
            }

            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            //write to a udt utilizing the tag registry
            //update data
            var random = new Random();
            transplanter.Heartbeat++;
            for (int i = 0; i < transplanter.cameraData.Count; i++)
            {
                transplanter.cameraData[i].status++;
                transplanter.cameraData[i].offset = (float)(1000.0 * random.NextDouble());
            }
            plc.WriteTagSingle("fromVision", transplanter.GetBytesToWrite());

            var readTags = new List<string>() { "testEIPWrite", "fromVision" };
            var writeTags = new List<Tuple<string, byte[]>>();
            writeTags.Add(Tuple.Create("testEIPwrite", BitConverter.GetBytes(testval)));
            writeTags.Add(Tuple.Create("fromVision", transplanter.GetBytesToWrite()));
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
            transplanter.Heartbeat = testval;

            for (int i = 0; i <= 100; i++)
            {
                //Console.WriteLine("Elapsed time {0} ms", stopWatch.ElapsedMilliseconds);
                writeTags[0] = (Tuple.Create("testEIPWrite", BitConverter.GetBytes(testval++)));
                transplanter.Heartbeat++;
                writeTags[1] = (Tuple.Create("fromVision", transplanter.GetBytesToWrite()));
                resp = plc.MultiReadWrite(readTags, writeTags);
                numTags = resp[1] << 8 | resp[0];
                for (int j = 0; j < numTags; j++)
                {
                    var offset = 2 + 2 * j;
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
            }

            Console.WriteLine("Elapsed time {0} ms", stopWatch.ElapsedMilliseconds);
        }
    }
}
