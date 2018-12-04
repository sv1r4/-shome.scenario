using System.IO;

namespace shome.scene.provider.yml.config
{
    public class SceneYamlConfig
    {
        public string Directory { get; set; }

        public string DirectoryAbsolute
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.Directory))
                {
                    return Directory;
                }

                if (Path.IsPathRooted(Directory))
                {
                    return Directory;
                }

                return Path.Combine(System.IO.Directory.GetCurrentDirectory(), Directory);
            }
        }

    }
}
