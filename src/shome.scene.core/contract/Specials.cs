namespace shome.scene.core.contract
{
    public static class Specials
    {
        public const string Key = "@";
        public static readonly string Greater =$"{Key}>";
        public static readonly string GreaterEqual = $"{Key}>=";
        public static readonly string Less = $"{Key}<";
        public static readonly string LessEqual = $"{Key}<=";

        public static readonly string Proxy = $"{Key}proxy";
        public static readonly string Match = $"{Key}match";
    }
}
