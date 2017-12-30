using System;
using Microsoft.Kinect;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace KinectConsoleBridge
{
    class Program
    {
        private static readonly int DefaultPort = 11011;

        private Socket broadcast = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        private IPEndPoint endPoint = null;
        private KinectSensor kinectSensor = null;
        private BodyFrameReader bodyFrameReader = null;
        private Body[] bodies = null;
        private int frameCount = 0;
        private long lastTime = 0;

        static void Main(string[] args)
        {
            string targetStr = null;
            if (args.Length > 0)
            {
                targetStr = args[0];
            }
            else
            {
                Console.Out.Write("Enter target IP address:port (default = 127.0.0.1:11011): ");
                targetStr = Console.In.ReadLine().Trim();
                if (targetStr.Length == 0)
                {
                    targetStr = "127.0.0.1:" + DefaultPort;
                }
            }





            Console.Out.WriteLine("Args: " + args.Length);

            IPAddress ip = null;
            do
            {
                Console.Out.Write("Enter target IP address:port (default = 127.0.0.1:11011): ");
                string ipStr = Console.In.ReadLine().Trim();
                if (ipStr.Length == 0)
                {
                    ip = IPAddress.Parse("127.0.0.1");
                }
                else
                {
                    try
                    {
                        ip = IPAddress.Parse(ipStr);
                    } catch (FormatException)
                    {
                        Console.Out.WriteLine("Invalid IP address");
                    }
                }
            } while (ip == null);

            new Program(ip).Go();
        }

        Program(IPAddress ip, int port)
        {
            this.endPoint = new IPEndPoint(ip, port);
            this.kinectSensor = KinectSensor.GetDefault();
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();
            this.kinectSensor.IsAvailableChanged += this.Sensor_IsAvailableChanged;
            this.kinectSensor.Open();
            this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;
        }

        private void Sensor_IsAvailableChanged(object sender, IsAvailableChangedEventArgs e)
        {
            if (e.IsAvailable)
            {
                Console.Out.WriteLine("Kinect sensor attached");
            } else
            {
                Console.Out.WriteLine("Kinect sensor detached");
            }
        }

        public void Go()
        {
            Console.Out.WriteLine("Sending frame data to " + endPoint);
            while (Char.ToLowerInvariant(Console.ReadKey().KeyChar) != 'q') ;
            Shutdown();
        }

        public void Shutdown()
        {
            if (this.bodyFrameReader != null)
            {
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }
        }

        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            if (dataReceived)
            {
                MemoryStream stream = new MemoryStream();
                BinaryWriter writer = new BinaryWriter(stream);

                writer.Write((byte)0x01);
                writer.Write((byte)0x01);
                writer.Write((byte)0x02);
                writer.Write((byte)0x00);

                byte trackedCount = 0;
                foreach (Body body in this.bodies)
                {
                    if (!body.IsTracked) continue;
                    trackedCount++;
                    writer.Write(body.TrackingId);
                    for (int ix = 0; ix <= 24; ++ix)
                    {
                        writer.Write(body.Joints[(JointType)ix].Position.X);
                        writer.Write(body.Joints[(JointType)ix].Position.Y);
                        writer.Write(body.Joints[(JointType)ix].Position.Z);
                    }
                    for (int ix = 0; ix <= 24; ++ix)
                    {
                        writer.Write((byte)body.Joints[(JointType)ix].TrackingState);
                    }
                }

                byte[] packet = stream.GetBuffer();
                packet[3] = trackedCount;

                try
                {
                    broadcast.SendTo(packet, (int)stream.Length, SocketFlags.None, endPoint);
                }
                catch (Exception)
                {

                }

                frameCount++;
                long now = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
                if (now - lastTime >= 1000)
                {
                    Console.Out.WriteLine("FPS: " + frameCount + ", tracked bodies = " + trackedCount);
                    lastTime = now;
                    frameCount = 0;
                }
            }
        }
    }
}
