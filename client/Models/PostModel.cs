namespace client
{
    public class PostModel
    {
        public PostModel()
        {

        }
        public PostModel(string key, string value)
        {
            Key = key;
            Value = value;
        }

        public string Key { get; set; }
        public string Value { get; set; }
    }
}