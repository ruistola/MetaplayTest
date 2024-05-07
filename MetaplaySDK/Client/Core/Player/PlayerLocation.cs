// This file is part of Metaplay SDK which is released under the Metaplay SDK License.

using Metaplay.Core.Model;
using System;

namespace Metaplay.Core.Player
{
    [MetaSerializable]
    public struct PlayerLocation
    {
        [MetaMember(1)] public CountryId Country { get; private set; }

        /// <summary>
        /// The 2-letter continent code for the continent/area, with possible values of:
        /// <list type="bullet">
        /// <item><c>null</c> if PlayerLocation created before MetaplaySDK R27.</item>
        /// <item>"AF" - Africa</item>
        /// <item>"AS" - Asia</item>
        /// <item>"EU" - Europe</item>
        /// <item>"NA" - North America</item>
        /// <item>"SA" - South America</item>
        /// <item>"OC" - Oceania</item>
        /// <item>"AN" - Antarctica</item>
        /// </list>
        /// </summary>
        [MetaMember(2)] public string ContinentCodeMaybe { get; private set; }

        public PlayerLocation(CountryId country, string continentCodeMaybe)
        {
            Country = country;
            ContinentCodeMaybe = continentCodeMaybe;
        }
    }

    [MetaSerializable]
    public struct CountryId
    {
        /// <summary>
        /// The ISO 3166-1 alpha-2 code.
        /// E.g. "FI" or "US"
        /// </summary>
        [MetaMember(1)] public string IsoCode { get; private set; }

        public CountryId(string isoCode)
        {
            IsoCode = isoCode ?? throw new ArgumentNullException(nameof(isoCode));
        }
    }
}
