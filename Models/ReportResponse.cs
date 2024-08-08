namespace KDG.Zoho.Creator.Models
{
    public struct ReportResponse<Response>
    {
        public int code;
        public IEnumerable<Response> data;

        public static ReportResponse<Response> Empty()
        {
            var response = new ReportResponse<Response>();
            response.code = 0;
            response.data = new List<Response>();
            return response;
        }
    }
}
