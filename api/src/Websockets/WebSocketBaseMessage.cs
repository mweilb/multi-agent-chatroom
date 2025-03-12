namespace AgentOps.WebSockets
{
    /// <summary>
    /// Represents the base structure of a WebSocket message.
    /// Contains common properties such as user and transaction identifiers,
    /// the action to be performed, any sub-action details, and the message content.
    /// </summary>
    public class WebSocketBaseMessage
    {
        /// <summary>
        /// Gets or sets the identifier of the user who sent the message.
        /// </summary>
        public string UserId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets a unique identifier for the message transaction.
        /// </summary>
        public string TransactionId { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the primary action to be performed (e.g., "chat").
        /// </summary>
        public string Action { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the sub-action associated with the primary action (e.g., "chunk", "done", "error").
        /// </summary>
        public string SubAction { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the content or payload of the message.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the content or payload of the message.
        /// </summary>
        public string BotChat { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the content or payload of the message.
        /// </summary>
        public string UserChat { get; set; } = string.Empty;
    }
}
