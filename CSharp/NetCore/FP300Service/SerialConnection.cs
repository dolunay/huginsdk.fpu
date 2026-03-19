using FP300Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FP300Service
{
#if ON_RDP
    public class SerialConnection : IConnection, IDisposable
#else
    public class SerialConnection : IConnection
#endif
    {
        private string portName = String.Empty;
        private int baudRate = 115200;
        private static MySerialPort sp = null;
        private static int supportedBufferSize = ProgramConfig.DEFAULT_BUFFER_SIZE;

        public SerialConnection(string portName, int baudrate)
        {
            this.portName = portName;
            this.baudRate = baudrate;

            try
            {
                if (IsOpen)
                {
                    Close();
                }
            }
            catch
            {
            }
        }

#if ON_RDP
        ~SerialConnection()
        {
            Dispose();
        }
#endif

        public void Open()
        {
            sp = new MySerialPort(portName, baudRate);
            sp.WriteTimeout = 4500;
            sp.ReadTimeout = 4500;
            sp.ReadBufferSize = supportedBufferSize;
            sp.WriteBufferSize = supportedBufferSize;
            //TODO: sp.Encoding = MainForm.DefaultEncoding;
            sp.Open();
        }

        public bool IsOpen
        {
            get
            {
                if (sp != null && sp.IsOpen)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public void Close()
        {
            if (sp != null)
            {
                sp.Close();
            }
        }

        public int FPUTimeout
        {
            get
            {
                return sp.ReadTimeout;
            }
            set
            {
                sp.ReadTimeout = value;
            }
        }

        public object ToObject()
        {
            return sp;
        }

#if ON_RDP
        public void Dispose()
        {
            try
            {
                Close();
                sp = null;
            }
            catch
            {
            }
        }
#endif


        public int BufferSize
        {
            get
            {
                return sp.ReadBufferSize;
            }
            set
            {
                // Close the connection
                Close();
                // Set new buffer size
                supportedBufferSize = value;
                // Re-open the connection
                Open();
            }
        }
    }
}
