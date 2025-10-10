using System;
using System.Collections.Generic;
using System.Linq;
using Core.Infrastructure.StateMachine;
using Core.Infrastructure.StateMachine.States;
using Game.Tiles;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Utils;
using Utils.Extensions;
using VContainer;
using VContainer.Unity;

namespace Game
{
    public class FieldHandler : SerializedMonoBehaviour
    {
        [Inject] private readonly IObjectResolver _resolver;
        [Inject] private TileDictionaryConfig _config;
        [Inject] private GameStateMachine _stateMachine;
        [SerializeField] private int types = 6;
        [SerializeField, Range(0, 1)] private float difficulty = 0.5f;

        public Field Field;

        [SerializeField] private Vector3Int defaultDimensions = new(7, 9, 5);

        [SerializeField] private Transform fieldRoot;

        [SerializeField] private FitCamera fitCamera;

        [SerializeField] private LevelTemplateSO levelTemplate;

        [SerializeField] private List<LevelTemplateSO> tutorialLevelTemplateList;
        [SerializeField] private List<LevelTemplateSO> levelTemplateList;
        [SerializeField] private TMP_Dropdown levelTemplates;
        [SerializeField] private TMP_InputField typesInput;
        [SerializeField] private Slider difficultySlider;
        [SerializeField] private TMP_Dropdown factoryType;

        private int _level;
        
        private void Start()
        {
            _level = ((GameState)_stateMachine.CurrentState).Level;
            if (_level is >= 1 and <= 4)
                levelTemplate = tutorialLevelTemplateList[_level - 1];
            else
                levelTemplate = levelTemplateList[_level % levelTemplateList.Count];
            GenerateNewMap();
            levelTemplates.ClearOptions();
            levelTemplates.AddOptions(levelTemplateList.Select(x => x.name).ToList());
            levelTemplates.onValueChanged.AddListener(SelectLevel);
        }

        private void SelectLevel(int id)
        {
            levelTemplate = levelTemplateList[id];
        }
        
        public void GenerateNewMap()
        {
            types = int.Parse(typesInput.text);
            difficulty = difficultySlider.value;
            
            fieldRoot.DestroyAllChildren();
            fieldRoot.localPosition = new Vector3(-((levelTemplate.width + 1) / 2), -((levelTemplate.height + 1) / 2), 0);
            fitCamera.Fit(levelTemplate.width + 2);

            var availableTypes = new List<TileType>();
            for (var i = 0; i < types; i++)
            {
                availableTypes.Add((TileType)Enum.GetValues(typeof(TileType)).GetValue(i));
            }
            
            FieldFactory factory;
            switch (factoryType.value)
            {
                case 0:
                    factory = new FieldFactoryFromTemplate(levelTemplate, availableTypes, difficulty);
                    Debug.Log("FieldFactoryFromTemplate: " + difficulty * 100 + "%");
                    break;
                case 1:
                    factory = new AdvancedFieldFactory(levelTemplate, availableTypes, difficulty);
                    Debug.Log("AdvancedFieldFactory: " + difficulty * 100 + "%");
                    break;
                case 2:
                default:
                    factory = new StrategicWaveFactory(levelTemplate, availableTypes, difficulty);
                    Debug.Log("StrategicWaveFactory: " + difficulty * 100 + "%");
                    break;
            }
            
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