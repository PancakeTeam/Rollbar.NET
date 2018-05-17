﻿namespace Rollbar
{
    using Newtonsoft.Json;
    using Newtonsoft.Json.Converters;
    using Rollbar.Common;
    using Rollbar.Diagnostics;
    using Rollbar.DTOs;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;

#pragma warning disable CS1584 // XML comment has syntactically incorrect cref attribute
#pragma warning disable CS1658 // Warning is overriding an error
    /// <summary>
    /// Models Rollbar client/notifier configuration data.
    /// </summary>
    /// <seealso cref="Rollbar.Common.ReconfigurableBase{Rollbar.RollbarConfig}" />
    /// <seealso cref="Common.ReconfigurableBase{Rollbar.RollbarConfig}" />
    /// <seealso cref="Rollbar.ITraceable" />
    public class RollbarConfig
#pragma warning restore CS1658 // Warning is overriding an error
#pragma warning restore CS1584 // XML comment has syntactically incorrect cref attribute
        : ReconfigurableBase<RollbarConfig, IRollbarConfig>
        , ITraceable
        , IRollbarConfig
    {
        private static readonly string[] listValueSplitters = new string[] { ", ", "; ", " " };

        private readonly RollbarLogger _logger = null;

        private RollbarConfig()
        {
        }

        internal RollbarConfig(RollbarLogger logger)
        {
            this._logger = logger;

            this.SetDefaults();
        }

        internal RollbarLogger Logger
        {
            get { return this._logger; }
        }

        /// <summary>
        /// Reconfigures this object similar to the specified one.
        /// </summary>
        /// <param name="likeMe">The pre-configured instance to be cloned in terms of its configuration/settings.</param>
        /// <returns>
        /// Reconfigured instance.
        /// </returns>
        public override RollbarConfig Reconfigure(IRollbarConfig likeMe)
        {
            base.Reconfigure(likeMe);

            this.Logger.Queue.NextDequeueTime = DateTimeOffset.Now;

            return this;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RollbarConfig"/> class.
        /// </summary>
        /// <param name="accessToken">The access token.</param>
        public RollbarConfig(string accessToken)
        {
            Assumption.AssertNotNullOrWhiteSpace(accessToken, nameof(accessToken));

            this.SetDefaults();
            this.AccessToken = accessToken;
        }

        private void SetDefaults()
        {
            // let's set some default values:
            this.Environment = "production";
            this.Enabled = true;
            this.MaxReportsPerMinute = 60;
            this.ReportingQueueDepth = 20;
            this.LogLevel = ErrorLevel.Debug;
            this.ScrubFields = new string[]
            {
                "passwd",
                "password",
                "secret",
                "confirm_password",
                "password_confirmation",
            };
            this.ScrubWhitelistFields = new string[]
            {
            };
            this.EndPoint = "https://api.rollbar.com/api/1/";
            this.ProxyAddress = null;
            this.CheckIgnore = null;
            this.Transform = null;
            this.Truncate = null;
            this.Server = null;
            this.Person = null;

            this.PersonDataCollectionPolicies = PersonDataCollectionPolicies.None;
            this.IpAddressCollectionPolicy = IpAddressCollectionPolicy.Collect;

#if NETFX
            // initialize based on app.config settings of Rollbar section (if any):
            this.InitFromAppConfig();
#endif
        }

#if NETFX
        private void InitFromAppConfig()
        {
            Rollbar.NetFramework.RollbarConfigSection config = 
                Rollbar.NetFramework.RollbarConfigSection.GetConfiguration();
            if (config == null)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(config.AccessToken))
            {
                this.AccessToken = config.AccessToken;
            }
            if (!string.IsNullOrWhiteSpace(config.Environment))
            {
                this.Environment = config.Environment;
            }
            if (config.Enabled.HasValue)
            {
                this.Enabled = config.Enabled.Value;
            }
            if (config.MaxReportsPerMinute.HasValue)
            {
                this.MaxReportsPerMinute = config.MaxReportsPerMinute.Value;
            }
            if (config.ReportingQueueDepth.HasValue)
            {
                this.ReportingQueueDepth = config.ReportingQueueDepth.Value;
            }
            if (config.LogLevel.HasValue)
            {
                this.LogLevel = config.LogLevel.Value;
            }
            if (config.ScrubFields != null && config.ScrubFields.Length > 0)
            {
                this.ScrubFields = 
                    string.IsNullOrEmpty(config.ScrubFields) ? new string[0] 
                    : config.ScrubFields.Split(listValueSplitters, StringSplitOptions.RemoveEmptyEntries);
            }
            if (config.ScrubWhitelistFields != null && config.ScrubWhitelistFields.Length > 0)
            {
                this.ScrubWhitelistFields =
                    string.IsNullOrEmpty(config.ScrubWhitelistFields) ? new string[0]
                    : config.ScrubWhitelistFields.Split(listValueSplitters, StringSplitOptions.RemoveEmptyEntries);
            }
            if (!string.IsNullOrWhiteSpace(config.EndPoint))
            {
                this.EndPoint = config.EndPoint;
            }
            if (!string.IsNullOrWhiteSpace(config.ProxyAddress))
            {
                this.ProxyAddress = config.ProxyAddress;
            }
            if (config.PersonDataCollectionPolicies.HasValue)
            {
                this.PersonDataCollectionPolicies = config.PersonDataCollectionPolicies.Value;
            }
            if (config.IpAddressCollectionPolicy.HasValue)
            {
                this.IpAddressCollectionPolicy = config.IpAddressCollectionPolicy.Value;
            }
        }
#endif
        /// <summary>
        /// Gets the access token.
        /// </summary>
        /// <value>
        /// The access token.
        /// </value>
        public string AccessToken { get; set; }

        /// <summary>
        /// Gets or sets the end point.
        /// </summary>
        /// <value>
        /// The end point.
        /// </value>
        public string EndPoint { get; set; }

        /// <summary>
        /// Gets or sets the scrub fields.
        /// </summary>
        /// <value>
        /// The scrub fields.
        /// </value>
        public string[] ScrubFields { get; set; }

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
        public string[] ScrubWhitelistFields { get; set; }

        /// <summary>
        /// Gets or sets the log level.
        /// </summary>
        /// <value>
        /// The log level.
        /// </value>
        public ErrorLevel? LogLevel { get; set; }

        /// <summary>
        /// Gets or sets the enabled.
        /// </summary>
        /// <value>
        /// The enabled.
        /// </value>
        public bool? Enabled { get; set; }

        /// <summary>
        /// Gets or sets the environment.
        /// </summary>
        /// <value>
        /// The environment.
        /// </value>
        public string Environment { get; set; }

        /// <summary>
        /// Gets or sets the check ignore.
        /// </summary>
        /// <value>
        /// The check ignore.
        /// </value>
        public Func<Payload, bool> CheckIgnore { get; set; }

        /// <summary>
        /// Gets or sets the transform.
        /// </summary>
        /// <value>
        /// The transform.
        /// </value>
        public Action<Payload> Transform { get; set; }

        /// <summary>
        /// Gets or sets the truncate.
        /// </summary>
        /// <value>
        /// The truncate.
        /// </value>
        public Action<Payload> Truncate { get; set; }

        /// <summary>
        /// Gets or sets the server.
        /// </summary>
        /// <value>
        /// The server.
        /// </value>
        public Server Server { get; set; }

        /// <summary>
        /// Gets or sets the person.
        /// </summary>
        /// <value>
        /// The person.
        /// </value>
        public Person Person { get;set; }

        /// <summary>
        /// Gets or sets the proxy address.
        /// </summary>
        /// <value>
        /// The proxy address.
        /// </value>
        public string ProxyAddress { get; set; }

        /// <summary>
        /// Gets or sets the maximum reports per minute.
        /// </summary>
        /// <value>
        /// The maximum reports per minute.
        /// </value>
        public int MaxReportsPerMinute { get; set; }

        /// <summary>
        /// Gets or sets the reporting queue depth.
        /// </summary>
        /// <value>
        /// The reporting queue depth.
        /// </value>
        public int ReportingQueueDepth { get; set; }

        /// <summary>
        /// Gets or sets the person data collection policies.
        /// </summary>
        /// <value>
        /// The person data collection policies.
        /// </value>
        [JsonConverter(typeof(StringEnumConverter))]
        public PersonDataCollectionPolicies PersonDataCollectionPolicies { get; set; }

        /// <summary>
        /// Gets or sets the IP address collection policy.
        /// </summary>
        /// <value>
        /// The IP address collection policy.
        /// </value>
        [JsonConverter(typeof(StringEnumConverter))]
        public IpAddressCollectionPolicy IpAddressCollectionPolicy { get; set; }

        /// <summary>
        /// Specifies a duration after which requests to Rollbar API are being cancelled.
        /// </summary>
        public TimeSpan? ClientRequestsTimeout { get; set; }

        /// <summary>
        /// Traces as a string.
        /// </summary>
        /// <param name="indent">The indent.</param>
        /// <returns>
        /// String rendering of this instance.
        /// </returns>
        public string TraceAsString(string indent = "")
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(indent + this.GetType().Name + ":");
            sb.AppendLine(indent + "  AccessToken: " + this.AccessToken);
            sb.AppendLine(indent + "  EndPoint: " + this.EndPoint);
            sb.AppendLine(indent + "  ScrubFields: " + this.ScrubFields);
            sb.AppendLine(indent + "  ScrubWhitelistFields: " + this.ScrubWhitelistFields);
            sb.AppendLine(indent + "  Enabled: " + this.Enabled);
            sb.AppendLine(indent + "  Environment: " + this.Environment);
            sb.AppendLine(indent + "  Server: " + this.Server);
            sb.AppendLine(indent + "  Person: " + this.Person);
            sb.AppendLine(indent + "  ProxyAddress: " + this.ProxyAddress);
            sb.AppendLine(indent + "  MaxReportsPerMinute: " + this.MaxReportsPerMinute);
            sb.AppendLine(indent + "  IpAddressCollectionPolicy: " + this.IpAddressCollectionPolicy);
            sb.AppendLine(indent + "  PersonDataCollectionPolicies: " + this.PersonDataCollectionPolicies);
            sb.AppendLine(indent + "  ClientRequestsTimeout: " + this.ClientRequestsTimeout);
            //sb.AppendLine(indent + this.Result.Trace(indent + "  "));
            return sb.ToString();
        }

        /// <summary>
        /// Gets the safe scrub fields. 
        /// Basically this.ScrubFields "minus" this.ScrubWhitelistFields.
        /// </summary>
        /// <returns></returns>
        internal IReadOnlyCollection<string> GetSafeScrubFields()
        {
            if (this.ScrubFields == null || this.ScrubFields.Length == 0)
            {
                return new string[0];
            }

            if (this.ScrubWhitelistFields == null || this.ScrubWhitelistFields.Length == 0)
            {
                return this.ScrubFields.ToArray();
            }

            var whitelist = this.ScrubWhitelistFields.ToArray();
            return this.ScrubFields.Where(i => !whitelist.Contains(i)).ToArray();
        }

        protected bool Equals(RollbarConfig other)
        {
            return Equals(_logger, other._logger) && string.Equals(AccessToken, other.AccessToken) && string.Equals(EndPoint, other.EndPoint) && Equals(ScrubFields, other.ScrubFields) && Equals(ScrubWhitelistFields, other.ScrubWhitelistFields) && LogLevel == other.LogLevel && Enabled == other.Enabled && string.Equals(Environment, other.Environment) && Equals(CheckIgnore, other.CheckIgnore) && Equals(Transform, other.Transform) && Equals(Truncate, other.Truncate) && Equals(Server, other.Server) && Equals(Person, other.Person) && string.Equals(ProxyAddress, other.ProxyAddress) && MaxReportsPerMinute == other.MaxReportsPerMinute && ReportingQueueDepth == other.ReportingQueueDepth && PersonDataCollectionPolicies == other.PersonDataCollectionPolicies && IpAddressCollectionPolicy == other.IpAddressCollectionPolicy && ClientRequestsTimeout.Equals(other.ClientRequestsTimeout);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((RollbarConfig) obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = (_logger != null ? _logger.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (AccessToken != null ? AccessToken.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (EndPoint != null ? EndPoint.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ScrubFields != null ? ScrubFields.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ScrubWhitelistFields != null ? ScrubWhitelistFields.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ LogLevel.GetHashCode();
                hashCode = (hashCode * 397) ^ Enabled.GetHashCode();
                hashCode = (hashCode * 397) ^ (Environment != null ? Environment.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (CheckIgnore != null ? CheckIgnore.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Transform != null ? Transform.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Truncate != null ? Truncate.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Server != null ? Server.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (Person != null ? Person.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ (ProxyAddress != null ? ProxyAddress.GetHashCode() : 0);
                hashCode = (hashCode * 397) ^ MaxReportsPerMinute;
                hashCode = (hashCode * 397) ^ ReportingQueueDepth;
                hashCode = (hashCode * 397) ^ (int) PersonDataCollectionPolicies;
                hashCode = (hashCode * 397) ^ (int) IpAddressCollectionPolicy;
                hashCode = (hashCode * 397) ^ ClientRequestsTimeout.GetHashCode();
                return hashCode;
            }
        }
    }
}
