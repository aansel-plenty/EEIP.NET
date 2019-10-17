using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sres.Net.EEIP;

namespace CM
{
    public class ConnectionManager
    {
        public Int32 Heartbeat;
        public PLCStates PLCState;
        public string readTagPath = "";
        public string writeTagPath = "";
        private int NumCameras = 8;
        public List<CameraResponse> cameraData = new List<CameraResponse>();
        public ConnectionManager()
        {
            for (int i = 0; i < NumCameras; i++)
            {
                cameraData.Add(new CameraResponse());
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
            Error = -2,
            Offline = -1,
            Idle = 0,
            Acquiring,
            Acquired,
            Processing,
            Processed
        }
        public class CameraResponse
        {
            public CameraStatus status;
            public float offset;
        }

        public byte[] GetBytesToWrite()
        {
            var data = new List<byte>();
            data.AddRange(BitConverter.GetBytes(Heartbeat));
            foreach (var camera in cameraData)
            {
                data.AddRange(BitConverter.GetBytes((Int32) camera.status));
                data.AddRange(BitConverter.GetBytes(camera.offset));
            }
            return data.ToArray();
        }
    }
}
