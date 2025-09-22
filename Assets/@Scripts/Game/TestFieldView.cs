using Game.Tiles;
using Sirenix.OdinInspector;
using UnityEngine;
using Utils;
using Utils.Extensions;
using VContainer;
using VContainer.Unity;

namespace Game
{
    public class TestFieldView : SerializedMonoBehaviour
    {
        [Inject] private readonly IObjectResolver _resolver;
        [Inject] private TileDictionaryConfig _config;

        public Field Field;

        [SerializeField] private Vector3Int defaultDimensions = new(7, 7, 3);

        [SerializeField] private Transform fieldRoot;

        [SerializeField] private FitCamera fitCamera;
        
        private void Start()
        {
            GenerateNewMap(defaultDimensions.x, defaultDimensions.y, defaultDimensions.z);
        }

        [Button(ButtonSizes.Medium)]
        public void GenerateNewMap(int dimX, int dimY, int dimZ)
        {
            fieldRoot.DestroyAllChildren();
            fieldRoot.localPosition = new Vector3(-dimX, -dimY, 0);
            fitCamera.unitsWidth = dimX * 2 + 1;
            fitCamera.Fit();
            
            var factory = new SimpleFilledFieldFactory();
            Field = factory.Create(dimX, dimY, dimZ);
            for (var z = 0; z < Field.Grid.GetLength(2); z++)
            for (var y = 0; y < Field.Grid.GetLength(1); y++)
            for (var x = 0; x < Field.Grid.GetLength(0); x++)
            {
                if (Field.Grid[x, y, z] is not Tile tile) continue;
                var newTile = _resolver.Instantiate(_config.TilePrefab, fieldRoot);
                tile.View = newTile;
            }
        }
    }
}