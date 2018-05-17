﻿namespace Rollbar
{
    using System;
    using Rollbar.DTOs;

    /// <summary>
    /// Models Rollbar configuration interface.
    /// </summary>
    public interface IRollbarConfig
        : ITraceable
    {
        /// <summary>
        /// Gets the access token.
        /// </summary>
        /// <value>
        /// The access token.
        /// </value>
        string AccessToken { get; }

        /// <summary>
        /// Gets the environment.
        /// </summary>
        /// <value>
        /// The environment.
        /// </value>
        string Environment { get; }

        /// <summary>
        /// Gets the end point.
        /// </summary>
        /// <value>
        /// The end point.
        /// </value>
        string EndPoint { get; }

        /// <summary>
        /// Gets or sets the enabled flag.
        /// </summary>
        /// <value>
        /// The enabled.
        /// </value>
        bool? Enabled { get; set; }

        /// <summary>
        /// Gets or sets the log level.
        /// </summary>
        /// <value>
        /// The log level.
        /// </value>
        ErrorLevel? LogLevel { get; set; }

        /// <summary>
        /// Gets the maximum reports per minute.
        /// </summary>
        /// <value>
        /// The maximum reports per minute.
        /// </value>
        int MaxReportsPerMinute { get; }

        /// <summary>
        /// Gets the proxy address.
        /// </summary>
        /// <value>
        /// The proxy address.
        /// </value>
        string ProxyAddress { get; }

        /// <summary>
        /// Gets the reporting queue depth.
        /// </summary>
        /// <value>
        /// The reporting queue depth.
        /// </value>
        int ReportingQueueDepth { get; }

        /// <summary>
        /// Gets the scrub fields.
        /// </summary>
        /// <value>
        /// The scrub fields.
        /// </value>
        string[] ScrubFields { get; }

        /// <summary>
        /// Gets the scrub white-list fields.
        /// </summary>
        /// <value>
        /// The scrub white-list fields.
        /// </value>
        /// <remarks>
        /// The fields mentioned in this list are guaranteed to be excluded 
        /// from the ScrubFields list in cases when the lists overlap.
        /// </remarks>
        string[] ScrubWhitelistFields { get; }

        /// <summary>
        /// Gets the transform.
        /// </summary>
        /// <value>
        /// The transform.
        /// </value>
        Action<Payload> Transform { get; }

        /// <summary>
        /// Gets the truncate.
        /// </summary>
        /// <value>
        /// The truncate.
        /// </value>
        Action<Payload> Truncate { get; }

        /// <summary>
        /// Gets the check ignore.
        /// </summary>
        /// <value>
        /// The check ignore.
        /// </value>
        Func<Payload, bool> CheckIgnore { get; }

        /// <summary>
        /// Gets or sets the person.
        /// </summary>
        /// <value>
        /// The person.
        /// </value>
        Person Person { get; set; }

        /// <summary>
        /// Gets or sets the server.
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        Server Server { get; set; }

        /// <summary>
        /// Gets or sets the person data collection policies.
        /// </summary>
        /// <value>
        /// The person data collection policies.
        /// </value>
        PersonDataCollectionPolicies PersonDataCollectionPolicies { get; set; }

        /// <summary>
        /// Gets or sets the IP address collection policy.
        /// </summary>
        /// <value>
        /// The IP address collection policy.
        /// </value>
        IpAddressCollectionPolicy IpAddressCollectionPolicy { get; set; }

        /// <summary>
        /// Specifies a duration after which requests to Rollbar API are being cancelled.
        /// </summary>
        TimeSpan? ClientRequestsTimeout { get; set; }
    }
}