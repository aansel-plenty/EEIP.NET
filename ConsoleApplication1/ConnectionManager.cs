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
        public Logix plc = new Logix();
        public Int32 Heartbeat;
        public List<CameraResponse> cameraData = new List<CameraResponse>();
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
