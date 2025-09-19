using System.Collections.Generic;
using System.Linq;
using Core.Infrastructure.StateMachine;
using Core.Infrastructure.StateMachine.States;
using Core.Services.AssetManagement;
using Game.Tiles;
using Sirenix.OdinInspector;
using Solo.MOST_IN_ONE;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UI;
using Utils.Extensions;
using VContainer;

namespace Game
{
    public class Buffer : MonoBehaviour
    {
        private const string TileDictionarySOName = "Tile Dictionary";
        private static readonly AssetReferenceT<TileDictionaryConfig> ConfigReference = new(TileDictionarySOName);
        [ShowInInspector] private TileDictionaryConfig _config;
        
        [Inject] protected readonly GameStateMachine StateMachine;
        
        [SerializeField] private Image[] images = new Image[8];
        [SerializeField] private List<Tile> tiles = new List<Tile>(8);
        
        private async void Start()
        {
            _config ??= await ConfigReference.LoadAndCacheAsync(ReleaseKey.Game);
        }

        public bool AddTile(Tile tile)
        {
            tiles.Add(tile);
            images[tiles.Count - 1].sprite = _config.TileUI[tile.type].LoadAndCache(ReleaseKey.Game);

            CheckForMatch();

            if (tiles.Count < 8) return true;
            
            ((GameState)StateMachine.CurrentState).GameOver();
            return false;

        }

        private void CheckForMatch()
        {
            var typeToIndices = new Dictionary<TileType, List<int>>();

            for (var i = 0; i < tiles.Count; i++)
            {
                var type = tiles[i].type;

                if (!typeToIndices.TryGetValue(type, out var indices))
                {
                    indices = new List<int>(3);
                    typeToIndices[type] = indices;
                }

                indices.Add(i);

                if (indices.Count != 3) continue;
                
                Most_HapticFeedback.Generate(Most_HapticFeedback.HapticTypes.Success);
                
                tiles.RemoveAt(indices[2]);
                tiles.RemoveAt(indices[1]);
                tiles.RemoveAt(indices[0]);

                for (var j = 0; j < tiles.Count; j++)
                    images[j].sprite = _config.TileUI[tiles[j].type].LoadAndCache(ReleaseKey.Game);
                
                for (var j = tiles.Count; j < images.Length; j++) images[j].sprite = null;
                
                return; // Can't be more than 1 match
            }
        }
    }
}