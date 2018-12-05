using System.IO;
using System.Linq;
using Akka.Actor;
using Akka.DI.Core;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using shome.scene.core.model;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace shome.scene.processor.actors
{
    public class SceneYamlReaderActor : ReceiveActor
    {
        public SceneYamlReaderActor(ILogger<SceneYamlReaderActor> logger, Deserializer deserializer)
        {
            Receive<ReadFiles>(messages =>
            {
                logger.LogDebug("ReadFiles received");
                foreach (var f in messages.FileProvider.GetDirectoryContents(string.Empty).Where(x=>!x.IsDirectory))
                {
                    using (var stream = f.CreateReadStream())
                    {
                        using (var reader = new StreamReader(stream))
                        {
                            try
                            {
                                var scene = deserializer.Deserialize<SceneConfig>(reader);
                                Context.ActorOf(Context.DI().Props<ScenesCreatorActor>()).Tell(scene);
                            }
                            catch (YamlException ex)
                            {
                                logger.LogWarning(ex, $"Invalid File '{f.Name}'. Deserialization Error");
                            }
                        }
                    }
                }
            });
        }

        public class ReadFiles
        {
            public IFileProvider FileProvider { get; set; }
        }

        
    }
}
