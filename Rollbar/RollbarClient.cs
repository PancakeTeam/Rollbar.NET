using System.Threading;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("UnitTest.Rollbar")]

namespace Rollbar
{
    using System;
    using System.Text;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;

    using Newtonsoft.Json;

    using Rollbar.DTOs;
    using Rollbar.Diagnostics;
    using Rollbar.Serialization.Json;
    using Rollbar.PayloadTruncation;
    using Rollbar.Deploys;
    using Rollbar.Common;

    /// <summary>
    /// Client for accessing the Rollbar API
    /// </summary>
    internal class RollbarClient
    {
        private readonly Lazy<HttpClient> _httpClient;

        private HttpClient HttpClient => _httpClient.Value;

        public IRollbarConfig Config { get; }

        public RollbarClient(IRollbarConfig config)
        {
            Assumption.AssertNotNull(config, nameof(config));

            Config = config;
            _httpClient = new Lazy<HttpClient>(BuildWebClient);
        }

        public RollbarResponse PostAsJson(Payload payload, IEnumerable<string> scrubFields)
        {
            Assumption.AssertNotNull(payload, nameof(payload));

            var task = this.PostAsJsonAsync(payload, scrubFields);

            task.Wait();

            return task.Result;
        }

        private readonly IterativeTruncationStrategy _payloadTruncationStrategy = new IterativeTruncationStrategy();

        public async Task<RollbarResponse> PostAsJsonAsync(Payload payload, IEnumerable<string> scrubFields)
        {
            Assumption.AssertNotNull(payload, nameof(payload));

            if (this._payloadTruncationStrategy.Truncate(payload) > this._payloadTruncationStrategy.MaxPayloadSizeInBytes)
            {
                throw new ArgumentOutOfRangeException(
                    paramName: nameof(payload),
                    message: $"Payload size exceeds {this._payloadTruncationStrategy.MaxPayloadSizeInBytes} bytes limit!"
                    );
            }

            var jsonData = JsonConvert.SerializeObject(payload);
            jsonData = ScrubPayload(jsonData, scrubFields);

            var postPayload = new StringContent(jsonData, Encoding.UTF8, "application/json"); //CONTENT-TYPE header
            var postRequest = new HttpRequestMessage(HttpMethod.Post, new Uri($"{Config.EndPoint}item/"));
            postRequest.Content = postPayload;
            postRequest.Headers.Add("X-Rollbar-Access-Token", payload.AccessToken);
            postRequest.Headers
                .Accept
                .Add(new MediaTypeWithQualityHeaderValue("application/json")); //ACCEPT header

            var postResponse = await HttpClient.SendAsync(postRequest);

            postResponse.EnsureSuccessStatusCode();
            string reply = await postResponse.Content.ReadAsStringAsync();
            RollbarResponse response = JsonConvert.DeserializeObject<RollbarResponse>(reply);
            response.HttpDetails =
                $"Response: {postResponse}"
                + Environment.NewLine
                + $"Request: {postResponse.RequestMessage}"
                + Environment.NewLine;

            return response;
        }

        private string ScrubPayload(string payload, IEnumerable<string> scrubFields)
        {
            var jObj = JsonScrubber.CreateJsonObject(payload);
            var dataProperty = JsonScrubber.GetChildPropertyByName(jObj, "data");
            JsonScrubber.ScrubJson(dataProperty, scrubFields);
            var scrubbedPayload = jObj.ToString();
            return scrubbedPayload;
        }

        private HttpClient BuildWebClient()
        {
            HttpClient webClient = null;

            var webProxy = this.BuildWebProxy();

            if (webProxy != null)
            {
                var httpHandler = new HttpClientHandler();
                httpHandler.Proxy = webProxy;
                httpHandler.PreAuthenticate = true;
                httpHandler.UseDefaultCredentials = false;

                webClient = new HttpClient(httpHandler);
            }
            else
            {
                webClient = new HttpClient();
            }

            if (Config.ClientRequestsTimeout.HasValue) webClient.Timeout = Config.ClientRequestsTimeout.Value;

            return webClient;
        }

        private IWebProxy BuildWebProxy()
        {
            if (!string.IsNullOrWhiteSpace(this.Config.ProxyAddress))
            {
                return new WebProxy(this.Config.ProxyAddress);
            }

            return null;
        }

        private const string deployApiPath = @"deploy/";

        /// <summary>
        /// Posts the asynchronous.
        /// </summary>
        /// <param name="deployment">The deployment.</param>
        /// <returns></returns>
        public async Task PostAsync(IDeployment deployment)
        {
            Assumption.AssertNotNull(this.Config, nameof(this.Config));
            Assumption.AssertNotNullOrWhiteSpace(this.Config.AccessToken, nameof(this.Config.AccessToken));
            Assumption.AssertFalse(string.IsNullOrWhiteSpace(deployment.Environment) && string.IsNullOrWhiteSpace(this.Config.Environment), nameof(deployment.Environment));
            Assumption.AssertNotNullOrWhiteSpace(deployment.Revision, nameof(deployment.Revision));

            Assumption.AssertLessThan(
                deployment.Environment.Length, 256,
                nameof(deployment.Environment.Length)
                );
            Assumption.AssertTrue(
                deployment.LocalUsername == null || deployment.LocalUsername.Length < 256,
                nameof(deployment.LocalUsername)
                );
            Assumption.AssertTrue(
                deployment.Comment == null || StringUtility.CalculateExactEncodingBytes(deployment.Comment, Encoding.UTF8) <= (62 * 1024),
                nameof(deployment.Comment)
                );


            var uri = new Uri(this.Config.EndPoint + RollbarClient.deployApiPath);
            var parameters = new Dictionary<string, string> {
                    { "access_token", this.Config.AccessToken },
                    { "environment", (!string.IsNullOrWhiteSpace(deployment.Environment)) ? deployment.Environment : this.Config.Environment  },
                    { "revision", deployment.Revision },
                    { "rollbar_username", deployment.RollbarUsername },
                    { "local_username", deployment.LocalUsername },
                    { "comment", deployment.Comment },
                };
            var httpContent = new FormUrlEncodedContent(parameters);
            var postResponse = await HttpClient.PostAsync(uri, httpContent);

            if (postResponse.IsSuccessStatusCode)
            {
                string reply = await postResponse.Content.ReadAsStringAsync();
            }
            else
            {
                postResponse.EnsureSuccessStatusCode();
            }

        }

        /// <summary>
        /// Gets the deployment asynchronous.
        /// </summary>
        /// <param name="readAccessToken">The read access token.</param>
        /// <param name="deploymentID">The deployment identifier.</param>
        /// <returns></returns>
        public async Task<DeployResponse> GetDeploymentAsync(string readAccessToken, string deploymentID)
        {
            Assumption.AssertNotNullOrWhiteSpace(readAccessToken, nameof(readAccessToken));
            Assumption.AssertNotNullOrWhiteSpace(deploymentID, nameof(deploymentID));

            var uri = new Uri(
                this.Config.EndPoint
                + RollbarClient.deployApiPath + deploymentID + @"/"
                + $"?access_token={readAccessToken}"
                );

            var httpResponse = await HttpClient.GetAsync(uri);

            DeployResponse response = null;
            if (httpResponse.IsSuccessStatusCode)
            {
                string reply = await httpResponse.Content.ReadAsStringAsync();
                response = JsonConvert.DeserializeObject<DeployResponse>(reply);
            }
            else
            {
                httpResponse.EnsureSuccessStatusCode();
            }

            return response;
        }

        private const string deploysQueryApiPath = @"deploys/";

        /// <summary>
        /// Gets the deployments asynchronous.
        /// </summary>
        /// <param name="readAccessToken">The read access token.</param>
        /// <param name="pageNumber">The page number.</param>
        /// <returns></returns>
        public async Task<DeploysPageResponse> GetDeploymentsAsync(string readAccessToken, int pageNumber = 1)
        {
            Assumption.AssertNotNullOrWhiteSpace(readAccessToken, nameof(readAccessToken));

            var uri = new Uri(
                this.Config.EndPoint
                + RollbarClient.deploysQueryApiPath
                + $"?access_token={readAccessToken}&page={pageNumber}"
                );

            var httpResponse = await HttpClient.GetAsync(uri);

            DeploysPageResponse response = null;
            if (httpResponse.IsSuccessStatusCode)
            {
                string reply = await httpResponse.Content.ReadAsStringAsync();
                response = JsonConvert.DeserializeObject<DeploysPageResponse>(reply);
            }
            else
            {
                httpResponse.EnsureSuccessStatusCode();
            }

            return response;
        }
    }
}
