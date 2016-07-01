namespace Examples.Mobile
{
    using Trimble.Identity;
    using Trimble.Connect.Client;

    /// <summary>
    /// The application state.
    /// </summary>
    public static class AppState
    {
        /// <summary>
        /// Gets or sets the TID authentication context.
        /// </summary>
        /// <value>
        /// The client.
        /// </value>
        public static AuthenticationContext AuthContext { get; set; }

        /// <summary>
        /// Gets or sets the current user.
        /// </summary>
        /// <value>
        /// The current user.
        /// </value>
        public static AuthenticationResult CurrentUser { get; set; }

        /// <summary>
        /// Gets or sets the TC client wrapper.
        /// </summary>
        /// <value>
        /// The client.
        /// </value>
        public static ITrimbleConnectClient Client { get; set; }
    }
}
