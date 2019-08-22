using shome.scene.core.model;

namespace shome.scene.core.events
{
    public class ThenDoneEvent
    {
        public ThenDoneEvent(SceneConfig.SceneThen then)
        {
            Then = then;
        }

        public SceneConfig.SceneThen Then { get; }
    }
}
