
namespace shome.scene.akka.util
{
    public static class Salt
    {
        public static string Gen()
        {
            return shortid.ShortId.Generate(7);
        }
    }
}
