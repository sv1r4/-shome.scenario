using shome.scene.core.model;
using Xunit;
using Xunit.Abstractions;
using YamlDotNet.Serialization;

namespace shome.provider.yml.unittest
{
    public class YmlSceneProviderTest
    {
        private readonly ITestOutputHelper _output;

        public YmlSceneProviderTest(ITestOutputHelper output)
        {
            this._output = output;
        }

        [Fact]
        public void YamlSerialization()
        {
            var scene = new Scene
            {
                Actions = new[]
                {
                    new SceneAction
                    {
                        Name = "LightOn",
                        If = new[]
                        {
                            new SceneIf
                            {
                                Topic = "bath/switch/e/state",
                                Value = "1"
                            }
                        },
                        Then = new[]
                        {
                            new SceneThen
                            {
                                Topic = "bath/light/0/c/state",
                                Message = "1"
                            },
                            new SceneThen
                            {
                                Topic = "bath/light/1/c/state",
                                Message = "1"
                            },
                            new SceneThen
                            {
                                Topic = "bath/mirror/lcd/c/state",
                                Message = "1"
                            },
                            new SceneThen
                            {
                                Topic = "bath/sound/c/power",
                                Message = "1"
                            }
                        }
                    }
                }
            };


            var serializer = new SerializerBuilder().Build();
            var yaml = serializer.Serialize(scene);
            _output.WriteLine(yaml);
        }
    }
}
