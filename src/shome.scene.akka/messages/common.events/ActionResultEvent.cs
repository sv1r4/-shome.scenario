using shome.scene.core.model;

namespace shome.scene.akka.messages.common.events
{
    public class ActionResultEvent
    {
        public string ActionName { get; set; }
        public ActionResult Result { get; set; }
    }
}
