using System;
using System.Collections.Generic;
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
        [SerializeField] private int types = 6;
        [SerializeField, Range(0, 1)] private float difficulty = 0.5f;

        public Field Field;

        [SerializeField] private Vector3Int defaultDimensions = new(7, 9, 5);

        [SerializeField] private Transform fieldRoot;

        [SerializeField] private FitCamera fitCamera;

        [SerializeField] private LevelTemplateSO levelTemplate;
        
        private void Start()
        {
            GenerateNewMap();
        }
        
        public void GenerateNewMap()
        {
            fieldRoot.DestroyAllChildren();
            fieldRoot.localPosition = new Vector3(-((levelTemplate.width + 1) / 2), -((levelTemplate.height + 1) / 2), 0);
            fitCamera.Fit(levelTemplate.width + 2);

            var availableTypes = new List<TileType>();
            for (var i = 0; i < types; i++)
            {
                availableTypes.Add((TileType)Enum.GetValues(typeof(TileType)).GetValue(i));
            }
            var factory = new FieldFactoryFromTemplate(levelTemplate, availableTypes, difficulty);
            Field = factory.Create();
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