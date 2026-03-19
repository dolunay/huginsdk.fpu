namespace FP300Service
{
    public class MySerialPort : System.IO.Ports.SerialPort
    {
        public MySerialPort(string portName, int baudrate) :
            base(portName, baudrate)
        {
        }
#if ON_RDP
        protected override void Dispose(bool disposing)
        {
            // our variant for
            // 
            // http://social.msdn.microsoft.com/Forums/en-US/netfxnetcom/thread/8b02d5d0-b84e-447a-b028-f853d6c6c690
            // http://connect.microsoft.com/VisualStudio/feedback/details/140018/serialport-crashes-after-disconnect-of-usb-com-port

            var stream = (System.IO.Stream)typeof(SerialPort).GetField("internalSerialStream", 
                                                                    System.Reflection.BindingFlags.Instance | 
                                                                    System.Reflection.BindingFlags.NonPublic).GetValue(this);

            if (stream != null)
            {
                try { stream.Dispose(); }
                catch { }
            }

            base.Dispose(disposing);
        }
#endif
    }
}
