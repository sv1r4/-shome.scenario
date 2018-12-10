using shome.scene.core.model;
using Xunit;
using Xunit.Abstractions;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace shome.provider.yml.unittest
{
    public class YmlSceneProviderTest
    {
        private readonly ITestOutputHelper _output;

        public YmlSceneProviderTest(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void YamlSerialization()
        {
            var scene = new SceneConfig
            {
                Name = "scene",
                Actions = new[]
                {
                    new SceneConfig.SceneAction
                    {
                        Name = "LightOn",
                        If = new []
                        {
                            new SceneConfig.SceneIf
                            {
                                Topic = "bath/switch/e/state",
                                Value = "1"
                            },
                        },
                        Then = new[]
                        {
                            new SceneConfig.SceneThen
                            {
                                Topic = "bath/light/0/c/state",
                                Message = "1"
                            },
                            new SceneConfig.SceneThen
                            {
                                Topic = "bath/light/1/c/state",
                                Message = "1"
                            },
                            new SceneConfig.SceneThen
                            {
                                Topic = "bath/mirror/lcd/c/state",
                                Message = "1"
                            },
                            new SceneConfig.SceneThen
                            {
                                Topic = "bath/sound/c/power",
                                Message = "1"
                            }
                        },
                        DependsOn = new []
                        {
                            new SceneConfig.SceneDependency
                            {
                                Name = "testaction",
                                When = ActionResult.Success
                            }
                        }
                    }
                }
            };


            var serializer = new SerializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();
            var yaml = serializer.Serialize(scene);
            _output.WriteLine(yaml);
        }

        [InlineData(@"
name: scene
actions:
- name: LightOn
  if:
  - topic: bath/switch/e/state
    value: 1
  then:
  - topic: bath/light/0/c/state
    message: 1
  - topic: bath/light/1/c/state
    message: 1
  - topic: bath/mirror/lcd/c/state
    message: 1
  - topic: bath/sound/c/power
    message: 1
  dependsOn:
  - name: testaction
    when: Success
")]
        [Theory]
        public void YamlDeserialization(string yaml)
        {

            var deserializer = new DeserializerBuilder()
                .WithNamingConvention(new CamelCaseNamingConvention())
                .Build();
            
            var scene = deserializer.Deserialize<SceneConfig>(yaml);
            Assert.NotNull(scene);
            Assert.NotEmpty(scene.Actions);
        }
    }

}
