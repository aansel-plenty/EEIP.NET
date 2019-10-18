using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace CM
{
    public class ConnectionManager
    {
        public int RPI = 20; //20 ms delay between tasks
        public Int32 Heartbeat;
        public PLCStates PLCState;
        public string ReadTagPath = "";
        public string WriteTagPath = "";
        private int NumCameras = 8;
        public List<Camera> cameras = new List<Camera>();
        public ConnectionManager()
        {
            for (int i = 0; i < NumCameras; i++)
            {
                cameras.Add(new Camera() { id = i+1 });
            }
        }
        public enum PLCStates
        {
            Timeout = -1,
            Offline = 0,
            Connecting = 1,
            Communicating = 2
        }
        public enum CameraCommands : Int32
        {
            NOP = 0,
            Reset = 1,
            FlushBuffer,
            AcquireImage,
            ProcessImage
        }
        public enum CameraStatus : Int32
        {
            Timeout = -2,
            Error = -1,
            Offline = 0,
            Idle,
            Acquiring,
            Acquired,
            Processing,
            Processed
        }

        public class Camera
        {
            public int id = 0;
            public Thread thread;
            public CameraCommands command = CameraCommands.NOP;
            private CameraStatus lastStatus = CameraStatus.Offline;
            private Stopwatch timer = new Stopwatch();
            public CameraStatus status = CameraStatus.Offline;
            public float offset;
            public Camera()
            {
                this.thread = new Thread(() => VisionThread());
                this.thread.Start();
                timer.Start();
            }
            public void VisionThread()
            {
                while (true)
                {
                    Thread.Sleep(50);
                    //Console.WriteLine("Camera {0} woke up!", this.id);
                    switch (status)
                    {
                        case CameraStatus.Timeout:
                            break;
                        case CameraStatus.Error:
                            break;
                        case CameraStatus.Offline:
                            break;
                        case CameraStatus.Idle:
                            break;
                        case CameraStatus.Acquiring:
                            status = CameraStatus.Acquired;
                            break;
                        case CameraStatus.Acquired:
                            status = CameraStatus.Processing;
                            break;
                        case CameraStatus.Processing:
                            status = CameraStatus.Processed;
                            break;
                        case CameraStatus.Processed:
                            offset =  10.0f*((float) new Random().NextDouble()-0.5f);
                            status = CameraStatus.Idle;
                            break;
                        default:
                            break;
                    }
                    if (lastStatus != status)
                    {
                        Console.WriteLine("Camera {0}: state changed from {1} -> {2}, time since acquistion request {3}", id, lastStatus, status, timer.ElapsedMilliseconds);
                        lastStatus = status;
                    }
                }
                return;
            }
            public void Reset()
            {
                Console.WriteLine("Reset camera {0}", id);
                status = CameraStatus.Idle;
            }
            public void FlushBuffer()
            {
                Console.WriteLine("Flushed buffer for camera {0}", id);
                status = CameraStatus.Idle;
            }
            public void AcquireImage()
            {
                Console.WriteLine("Acquiring image for camera {0}", id);
                timer.Restart();
                status = CameraStatus.Acquiring;
            }
            public void ProcessImage()
            {
                Console.WriteLine("Processing image for camera {0}", id);
                status = CameraStatus.Processing;
            }
        }

        public void UpdateCameraCommand(int cameraNum, CameraCommands command)
        {
            var cameraIdx = cameraNum - 1;
            var camera = cameras[cameraIdx];
            if (camera.command != command)
            {
                Console.WriteLine("Camera {0} got a new command! {1}", cameraNum, command);
                camera.command = command;
                switch (camera.command)
                {
                    case CameraCommands.AcquireImage:
                        camera.AcquireImage();
                        break;
                    case CameraCommands.FlushBuffer:
                        camera.FlushBuffer();
                        break;
                    case CameraCommands.Reset:
                        camera.Reset();
                        break;
                    default:
                        break;
                }
            }
            return;
        }


        public byte[] GetBytesToWrite()
        {
            var data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(Heartbeat));
            foreach (var camera in cameras)
            {
                data.AddRange(BitConverter.GetBytes((Int32) camera.status));
                data.AddRange(BitConverter.GetBytes(camera.offset));
            }
            return data.ToArray();
        }
    }
}
