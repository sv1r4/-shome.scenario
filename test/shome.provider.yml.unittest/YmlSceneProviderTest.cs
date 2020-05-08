using System;
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
                                Message = "1",
                                Delay = TimeSpan.FromHours(1)
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
                                Action = "testaction",
                                When = ActionResultEnum.Success
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
  - action: testaction
    when: success
   

- name: testaction
  if:
  - topic: bath/switch/e/state
    value: test
    jsonMember: j1
  then:
  - topic: bath/switch/e/state
    message: done
    
    
- name: t2
  if:
  - topic: t2/value
    value: ""@<=1.5""
  then:
  - topic: t2/result
    message: done
    
- name: t3
  if:
  - topic: tt3/value
  - topic: tt3/value2
  then:
  - topic: tt3/result
    message: ""@proxy1""
")]
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
  - action: testaction
    when: success
   

- name: testaction
  if:
  - topic: bath/switch/e/state
    value: test
  then:
  - topic: bath/switch/e/state
    message: done
")]
        [InlineData(@"name: lr_light
actions:
- name: lr_switch_dblclick
  if:
  - topic: /lr/light/switch/e/clk
    value: 2
  then:
  - topic: lr/light/c/led
    message: '{""Mode"":1,""R"":0,""G"":0,""B"":0}'

- name: balcony_switch_hold
  if:
  - topic: balcony/btn/e/click
    value: hold
  then:
  - topic: lr/light/c/led
    message: '{""Mode"":0,""R"":0,""G"":0,""B"":0}'
  - topic: /lr/light/switch/c/main
    message: 0
    delay: 01:00:00
    
- name: lr_switch_hold
  if:
  - topic: /lr/light/switch/e/clk
    value: hold
  then:
  - topic: lr/light/c/led
    message: '{""Mode"":0,""R"":0,""G"":0,""B"":0}'
  - topic: /lr/light/switch/c/main
    message: 0
    retained: true
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
