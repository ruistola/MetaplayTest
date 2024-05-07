// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

using Metaplay.Core;
using System.Runtime.Serialization;
using System.Threading.Tasks;

namespace Metaplay.Cloud.RuntimeOptions
{
    /// <summary>
    /// Base class for declaring a section of RuntimeOptions. The deriving concrete
    /// class should also be annotated with <see cref="RuntimeOptionsAttribute"/>.
    /// </summary>
    public abstract class RuntimeOptionsBase
    {
        public static bool IsServerApplication      => RuntimeOptionsRegistry.Instance.ApplicationName == "Server";
        public static bool IsBotClientApplication   => RuntimeOptionsRegistry.Instance.ApplicationName == "BotClient";

        public static bool IsLocalEnvironment       => RuntimeOptionsRegistry.Instance.EnvironmentFamily == EnvironmentFamily.Local;
        public static bool IsDevelopmentEnvironment => RuntimeOptionsRegistry.Instance.EnvironmentFamily == EnvironmentFamily.Development;
        public static bool IsStagingEnvironment     => RuntimeOptionsRegistry.Instance.EnvironmentFamily == EnvironmentFamily.Staging;
        public static bool IsProductionEnvironment  => RuntimeOptionsRegistry.Instance.EnvironmentFamily == EnvironmentFamily.Production;
        public static bool IsCloudEnvironment       => !IsLocalEnvironment;

        [IgnoreDataMember]
        protected IMetaLogger Log { get; }

        protected RuntimeOptionsBase()
        {
            Log = MetaLogger.ForContext(GetType());
        }

        /// <summary>
        /// Async on-loaded handler for ensuring that the options section is fully populated. Useful
        /// for resolving secrets or other external data.
        /// </summary>
        /// <returns></returns>
        public virtual Task OnLoadedAsync() => Task.CompletedTask;
    }
}
