using Core.Services.AssetManagement;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Utils.Extensions
{
    public static class AddressablesLoader
    {
        /// <summary>
        /// Loads an asset from an AssetReference and caches it in AddressablesCache using the specified release key.
        /// If the asset is already loaded, returns the cached asset.
        /// </summary>
        /// <typeparam name="T">The type of the asset to load.</typeparam>
        /// <param name="asset">The AssetReference containing the asset to load.</param>
        /// <param name="releaseKey">The key used to cache the asset, allowing for future release of resources via <see cref="AddressablesCache.ReleaseAssets(string)"/>.</param>
        /// <returns>The loaded asset of type <typeparamref name="T"/>.</returns>
        public static T LoadAndCache<T>(this AssetReference asset, string releaseKey) where T : Object
        {
            if (asset.IsValid())
                return asset.Asset as T;

            AddressablesCache.AddAsset(asset, releaseKey);
            return asset.LoadAssetAsync<T>().WaitForCompletion();
        }

        /// <summary>
        /// Loads an asset from an AssetReferenceT and caches it in AddressablesCache using the specified release key.
        /// If the asset is already loaded, returns the cached asset.
        /// </summary>
        /// <typeparam name="T">The type of the asset to load.</typeparam>
        /// <param name="asset">The AssetReferenceT containing the asset to load.</param>
        /// <param name="releaseKey">The key used to cache the asset, allowing for future release of resources via <see cref="AddressablesCache.ReleaseAssets(string)"/>.</param>
        /// <returns>The loaded asset of type <typeparamref name="T"/>.</returns>
        public static T LoadAndCache<T>(this AssetReferenceT<T> asset, string releaseKey) where T : Object =>
            LoadAndCache<T>((AssetReference)asset, releaseKey);

        /// <summary>
        /// Asynchronously loads an asset from an AssetReference and caches it in AddressablesCache using the specified release key.
        /// If the asset is already loaded, returns the cached asset.
        /// </summary>
        /// <typeparam name="T">The type of the asset to load.</typeparam>
        /// <param name="asset">The AssetReference containing the asset to load.</param>
        /// <param name="releaseKey">The key used to cache the asset, allowing for future release of resources via <see cref="AddressablesCache.ReleaseAssets(string)"/>.</param>
        /// <returns>UniTask representing the loaded asset of type <typeparamref name="T"/>.</returns>
        public static async UniTask<T> LoadAndCacheAsync<T>(this AssetReference asset, string releaseKey) where T : Object
        {
            if (asset.IsValid())
                return asset.Asset as T;

            AddressablesCache.AddAsset(asset, releaseKey);
            return await asset.LoadAssetAsync<T>();
        }

        /// <summary>
        /// Asynchronously loads an asset from an AssetReferenceT and caches it in AddressablesCache using the specified release key.
        /// If the asset is already loaded, returns the cached asset.
        /// </summary>
        /// <typeparam name="T">The type of the asset to load.</typeparam>
        /// <param name="asset">The AssetReferenceT containing the asset to load.</param>
        /// <param name="releaseKey">The key used to cache the asset, allowing for future release of resources via <see cref="AddressablesCache.ReleaseAssets(string)"/>.</param>
        /// <returns>UniTask representing the loaded asset of type <typeparamref name="T"/>.</returns>
        public static async UniTask<T> LoadAndCacheAsync<T>(this AssetReferenceT<T> asset, string releaseKey) where T : Object => 
            await LoadAndCacheAsync<T>((AssetReference)asset, releaseKey);
    }
}