namespace shome.scene.core.contract
{
    public static class Specials
    {
        public const string Key = "@";
        public static readonly string PrefixGreater =$"{Key}>";
        public static readonly string PrefixGreaterEqual = $"{Key}>=";
        public static readonly string PrefixLess = $"{Key}<";
        public static readonly string PrefixLessEqual = $"{Key}<=";
        //todo regex match

        public static readonly string Proxy = $"{Key}proxy";
    }
}
