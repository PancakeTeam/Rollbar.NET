using System.Collections.Concurrent;
using Rollbar.Diagnostics;

namespace Rollbar
{
    internal static class RollbarClientLocator
    {
        private static readonly ConcurrentDictionary<IRollbarConfig, RollbarClient> Clients = new ConcurrentDictionary<IRollbarConfig, RollbarClient>();

        /// <summary>
        /// Gets rollbar client for given <paramref name="configuration"/>.
        /// </summary>
        /// <param name="configuration"></param>
        /// <returns></returns>
        public static RollbarClient GetClient(IRollbarConfig configuration)
        {
            Assumption.AssertNotNull(configuration, nameof(configuration));

            return Clients.GetOrAdd(configuration, config => new RollbarClient(config));
        }
    }
}