using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VJoy;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace VJoyUdp.Controller
{
    public delegate void JoystickMessageEventHandler(object sender, VJoy.VJoy.JoystickState state);

    public class VJoyUdpServer
    {
        VJoyUdpReceiver r = null;
        VJoy.VJoy vjoy = null;
        int joyidx = 0;

        public VJoyUdpServer(int port, int joyidx)
        {
            r = new VJoyUdpReceiver(port);
            r.MessageEvent += HandleMessage;
            this.joyidx = joyidx;

            vjoy = new VJoy.VJoy();
            vjoy.Initialize();

        }

        public void HandleMessage(object sender, VJoy.VJoy.JoystickState state)
        {
            vjoy.SetXAxis(joyidx, state.XAxis);
            vjoy.SetYAxis(joyidx, state.YAxis);
            vjoy.SetZAxis(joyidx, state.ZAxis);
            vjoy.SetXRotation(joyidx, state.XRotation);
            vjoy.SetYRotation(joyidx, state.YRotation);
            vjoy.SetZRotation(joyidx, state.ZRotation);
            vjoy.SetDial(joyidx, state.Dial);
            vjoy.SetSlider(joyidx, state.Slider);

            vjoy.SetPOVRaw(joyidx, state.POV);
            vjoy.SetButtonRaw(joyidx, state.Buttons);

            vjoy.Update(joyidx);
        }

        public bool run()
        {
            return r.Receive();
        }

        public void shutdown()
        {
            r.Shutdown();
            vjoy.Shutdown();
        }
    }

    public class VJoyUdpReceiver
    {
        UdpClient udpsock;

        public event JoystickMessageEventHandler MessageEvent;

        public VJoyUdpReceiver(int port)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);
            udpsock = new UdpClient(ep);
            udpsock.Client.ReceiveTimeout = 500;
        }

        public bool Receive()
        {
            IPEndPoint ep = null;
            MemoryStream stream = new MemoryStream();
            BinaryReader br = new BinaryReader(stream);
            byte[] bytes = null;
            try
            {
                bytes = udpsock.Receive(ref ep);
            }
            catch (SocketException e)
            {
                return false;
            }
            if (bytes != null && bytes.Length > 0)
            {
                stream.Write(bytes, 0, bytes.Length);
                stream.Seek(0, SeekOrigin.Begin);

                VJoy.VJoy.JoystickState s = new VJoy.VJoy.JoystickState();

                try
                {
                    s.XAxis = br.ReadInt16();
                    s.YAxis = br.ReadInt16();
                    s.ZAxis = br.ReadInt16();
                    s.XRotation = br.ReadInt16();
                    s.XRotation = br.ReadInt16();
                    s.XRotation = br.ReadInt16();
                    s.Slider = br.ReadInt16();
                    s.Dial = br.ReadInt16();
                    s.POV = br.ReadUInt16();
                    s.Buttons = br.ReadUInt32();
                }
                catch (EndOfStreamException e)
                {
                    return false;
                }

                OnRaiseJoyStickMessage(s);
                return true;
            }
            return false;
        }

        public void Shutdown()
        {
            udpsock.Close();
        }

        protected virtual void OnRaiseJoyStickMessage(VJoy.VJoy.JoystickState state)
        {
            if (MessageEvent != null)
            {
                MessageEvent(this, state);
            }
        }
    }
}
