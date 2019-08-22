namespace shome.scene.core.model
{

    public enum ActionStateEnum
    {
        Undefined,
        /// <summary>
        /// Waiting for dependencies
        /// </summary>
        Idle,
        /// <summary>
        /// Waiting triggers
        /// </summary>
        Pending,
        /// <summary>
        /// Triggers complete, then to be executed
        /// </summary>
        Active,
        /// <summary>
        /// all Then executed
        /// </summary>
        Done
    }
}
