using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using Utils.Extensions;

namespace Core.Services.AssetManagement
{
    /// <summary>
    /// Provides a cache mechanism for managing assets loaded via <see cref="AddressablesLoader"/>.
    /// </summary>
    public static class AddressablesCache
    {
        private static readonly Dictionary<string, HashSet<AssetReference>> Cache = new();

        /// <summary>
        /// Adds an asset reference to the cache under the specified release key.
        /// </summary>
        /// <param name="assetReference">The asset reference to be cached.</param>
        /// <param name="releaseKey">The key under which the asset reference is cached, used for future resource release.</param>
        public static void AddAsset(AssetReference assetReference, string releaseKey)
        {
            Cache.TryAdd(releaseKey, new HashSet<AssetReference>());
            Cache[releaseKey].Add(assetReference);
        }
        
        /// <summary>
        /// Releases all assets associated with the specified release key and removes them from the cache.
        /// </summary>
        /// <param name="releaseKey">The key associated with the assets to be released.</param>
        public static void ReleaseAssets(string releaseKey)
        {
            if (!Cache.TryGetValue(releaseKey, out var cacheScope)) return;
            
            foreach (var asset in cacheScope)
            {
                asset.ReleaseAsset();
            }

            Cache.Remove(releaseKey);
        }
    }
}