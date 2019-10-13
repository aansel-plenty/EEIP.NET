using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleApplication1
{
    class VisionService
    {
    }
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
}
