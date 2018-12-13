using shome.scene.core.model;

namespace shome.scene.core.events
{
    public class ActionResultEvent
    {
        public string ActionName { get; set; }
        public ActionResultEnum Result { get; set; }
    }
}
