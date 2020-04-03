namespace AleXr64.FastHttpPostReceiver
{
    public struct HttpHeader
    {
        public HttpHeader(string name, string value)
        {
            Value = value;
            Name = name;
        }
        public string Name { get; }
        public string Value { get; }
    }
}
