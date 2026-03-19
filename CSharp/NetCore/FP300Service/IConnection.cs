namespace FP300Service
{
    enum ContentType
    {
        NONE,
        REPORT,
        FILE
    }
    
    public interface IConnection
    {
        void Open();
        bool IsOpen { get; }
        void Close();
        int FPUTimeout { get; set; }
        object ToObject();
        int BufferSize { get; set; }
    }
}
