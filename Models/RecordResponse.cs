namespace KDG.Zoho.Creator.Models
{
    public class RecordResponse<T>
    {
        public int Code { get; set; }
        public T Data { get; set; }
        public RecordResponse(int code, T data)
        {
            Code = code;
            Data = data;
        }
    }
}
