namespace AleXr64.FastHttpPostReceiver
{
    public struct HttpPostData
    {
        public HttpPostData(HttpHeader[] headers, byte[] message, string query)
        {
            Headers = headers;
            Message = message;
            Query = query;
        }
        public HttpHeader[] Headers { get; }
        public byte[] Message { get; }
        public string Query { get; }
    }
}
