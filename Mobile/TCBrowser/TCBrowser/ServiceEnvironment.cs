namespace Examples.Mobile
{
    using System;
    using Trimble.Identity;

    /// <summary>
    /// Trimble Connect Service environment information.
    /// </summary>
    public class ServiceEnvironment
    {
        /// <summary>
        /// The production environment configuration.
        /// </summary>
        public static readonly ServiceEnvironment Production = new ServiceEnvironment
        {
            ClientCredentials = new ClientCredential("<Key>", "<Secret>", "<name>")
            {
                RedirectUri = new Uri("tcps://localhost")
            },
            AuthorityUri = AuthorityUris.ProductionUri,
            ServiceUri = "https://app.connect.trimble.com/tc/api/2.0/",
            WebAppUri = "https://app.connect.trimble.com/tc/app",
            SupportUri = "http://www.trimble.com/support/support_request.aspx",
            EndUserLicenseAgreementUrl = "https://community.trimble.com/docs/DOC-10003-terms-of-service",
        };

        /// <summary>
        /// The staging environment configuration.
        /// </summary>
        public static readonly ServiceEnvironment Staging = new ServiceEnvironment
        {
            ClientCredentials = new ClientCredential("<Key>", "<Secret>", "<name>")
            {
                RedirectUri = new Uri("tcps://localhost")
            },
            AuthorityUri = AuthorityUris.StagingUri,
            ServiceUri = "https://app.stage.connect.trimble.com/tc/api/2.0/",
            WebAppUri = "https://app.stage.connect.trimble.com/tc/app",
            SupportUri = "http://www.trimble.com/support/support_request.aspx",
            EndUserLicenseAgreementUrl = "https://community.trimble.com/docs/DOC-10003-terms-of-service",
        };

        /// <summary>
        /// The QA environment configuration.
        /// </summary>
        public static readonly ServiceEnvironment Qa = new ServiceEnvironment
        {
            ClientCredentials = new ClientCredential("<Key>", "<Secret>", "<name>")
            {
                RedirectUri = new Uri("tcps://localhost")
            },
            AuthorityUri = AuthorityUris.StagingUri,
            ServiceUri = "https://app.qa.connect.trimble.com/tc/api/2.0/",
            WebAppUri = "https://app.qa.connect.trimble.com/tc/app",
            SupportUri = "http://www.trimble.com/support/support_request.aspx",
            EndUserLicenseAgreementUrl = "https://community.trimble.com/docs/DOC-10003-terms-of-service",
        };

        /// <summary>
        /// Gets or sets the Identity Provider url.
        /// </summary>
        /// <value>The url.</value>
        public string AuthorityUri { get; set; }

        /// <summary>
        /// Gets or sets the Trimble Connect Service url.
        /// </summary>
        /// <value>The url.</value>
        public string ServiceUri { get; set; }

        /// <summary>
        /// Gets or sets the Trimble Connect Web app.
        /// </summary>
        /// <value>The url.</value>
        public string WebAppUri { get; set; }

        /// <summary>
        /// Gets or sets the Trimble Connect Support request page.
        /// </summary>
        /// <value>The url.</value>
        public string SupportUri { get; set; }

        /// <summary>
        /// Gets or sets the end user license agreement.
        /// </summary>
        /// <value>The url.</value>
        public string EndUserLicenseAgreementUrl { get; set; }

        /// <summary>
        /// Gets or sets the client application credentials.
        /// </summary>
        /// <value>The client credentials.</value>
        public ClientCredential ClientCredentials { get; set; }
    }
}
