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

        private Tile _lastAddedTile;
        
        private async void Start()
        {
            _config ??= await ConfigReference.LoadAndCacheAsync(ReleaseKey.Game);
        }

        public bool AddTile(Tile tile)
        {
            _lastAddedTile = tile;
            var inserted = false;
            for (var i = 0; i < tiles.Count; i++)
            {
                if (tiles[i].type != tile.type) continue;
                tiles.Insert(i + 1, tile);
                inserted = true;
                break;
            }

            if (!inserted)
                tiles.Add(tile);
            
            UpdateBufferUI();

            CheckForMatch();

            if (tiles.Count < 8) return true;
            
            ((GameState)StateMachine.CurrentState).GameOver();
            return false;

        }

        private void CheckForMatch()
        {
            if (tiles.Count < 3) return;

            var streak = 1;
            for (var i = 1; i < tiles.Count; i++)
            {
                if (tiles[i].type == tiles[i - 1].type)
                {
                    streak++;
                    if (streak < 3) continue;
                    
                    Most_HapticFeedback.Generate(Most_HapticFeedback.HapticTypes.Success);
                    
                    tiles.RemoveRange(i - 2, 3);
                    
                    UpdateBufferUI();

                    return; // Can't be more than 1 match
                }

                streak = 1;
            }
        }

        private void UpdateBufferUI()
        {
            for (var i = 0; i < tiles.Count; i++)
                images[i].sprite = _config.TileUI[tiles[i].type].LoadAndCache(ReleaseKey.Game);

            for (var i = tiles.Count; i < images.Length; i++)
                images[i].sprite = null;
        }
    }
}