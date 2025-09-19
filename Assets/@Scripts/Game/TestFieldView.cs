using Core.Services.AssetManagement;
using Game.Tiles;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using Utils;
using Utils.Extensions;
using VContainer;
using VContainer.Unity;

namespace Game
{
    public class TestFieldView : SerializedMonoBehaviour
    {
        private const string TileDictionarySOName = "Tile Dictionary";
        
        [Inject] private readonly IObjectResolver _resolver;

        public Field Field;

        [SerializeField] private Vector3Int dimensions = new(3, 3, 3);
        
        private static readonly AssetReferenceT<TileDictionaryConfig> ConfigReference = new(TileDictionarySOName);
        [ShowInInspector] private TileDictionaryConfig _config;

        [SerializeField] private Transform fieldRoot;

        [SerializeField] private FitCamera fitCamera;
        
        private async void Start()
        {
            _config ??= await ConfigReference.LoadAndCacheAsync(ReleaseKey.Game);
            GenerateNewMap();
        }

        [Button(ButtonSizes.Medium)]
        private void GenerateNewMap()
        {
            fieldRoot.DestroyAllChildren();
            fieldRoot.localPosition = new Vector3(-dimensions.x, -dimensions.y, 0);
            fitCamera.unitsWidth = dimensions.x * 2 + 1;
            fitCamera.Fit();
            
            var factory = new SimpleFilledFieldFactory();
            Field = factory.Create(dimensions.x, dimensions.y, dimensions.z);
            for (var z = 0; z < Field.Grid.GetLength(2); z++)
            for (var y = 0; y < Field.Grid.GetLength(1); y++)
            for (var x = 0; x < Field.Grid.GetLength(0); x++)
            {
                if (Field.Grid[x, y, z] is not Tile tile) continue;
                var newTile = _resolver.Instantiate(_config.TilePrefabs[tile.type].LoadAndCache(ReleaseKey.Game), fieldRoot);
                newTile.transform.SetLocalPositionAndRotation(new Vector3(x, y * 0.96f + z * 0.25f, z), Quaternion.identity);
                newTile.GetComponent<SpriteRenderer>().sortingOrder = z * 100 - y;
                tile.View = newTile.GetComponent<TileView>();
            }
        }
    }
}