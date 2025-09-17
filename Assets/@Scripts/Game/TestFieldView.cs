using Core.Services.AssetManagement;
using Game.Tiles;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Utils.Extensions;
using VContainer;
using VContainer.Unity;

namespace Game
{
    public class TestFieldView : SerializedMonoBehaviour
    {
        private const string TileDictionarySOName = "Tile Dictionary";
        
        [Inject] private readonly IObjectResolver _resolver;

        [SerializeField] private Vector3Int dimensions = new(3, 3, 3);
        
        private static readonly AssetReferenceT<TileDictionaryConfig> ConfigReference = new(TileDictionarySOName);
        [ShowInInspector] private TileDictionaryConfig _config;

        [SerializeField] private Transform fieldRoot;
        
        private async void Start()
        {
            _config = await ConfigReference.LoadAndCacheAsync(ReleaseKey.Game);
            GenerateNewMap();
        }

        [Button(ButtonSizes.Medium)]
        private void GenerateNewMap()
        {
            fieldRoot.DestroyAllChildren();
            var factory = new SimpleFilledFieldFactory();
            var field = factory.Create(dimensions.x, dimensions.y, dimensions.z);
            for (var z = 0; z < field.Grid.GetLength(2); z++)
            for (var y = 0; y < field.Grid.GetLength(1); y++)
            for (var x = 0; x < field.Grid.GetLength(0); x++)
            {
                if (field.Grid[x, y, z] is not Tile tile) continue;
                var newTile = _resolver.Instantiate(_config.TilePrefabs[tile.Type].LoadAndCache(ReleaseKey.Game), fieldRoot);
                newTile.transform.SetLocalPositionAndRotation(new Vector3(x, y + z * 0.25f, z), Quaternion.identity);
                newTile.GetComponent<SpriteRenderer>().sortingOrder = z * 100 - y;
            }
        }
    }
}