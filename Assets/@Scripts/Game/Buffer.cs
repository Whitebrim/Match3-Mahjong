using System.Collections.Generic;
using Core.Infrastructure.StateMachine;
using Core.Infrastructure.StateMachine.States;
using Core.Services.AssetManagement;
using Game.Tiles;
using Solo.MOST_IN_ONE;
using UnityEngine;
using UnityEngine.UI;
using Utils.Extensions;
using VContainer;

namespace Game
{
    public class Buffer : MonoBehaviour
    {
        [Inject] protected readonly GameStateMachine StateMachine;
        
        [SerializeField] private TileDictionaryConfig config;
        [SerializeField] private Image[] images = new Image[8];
        
        public List<Tile> tiles = new(8);

        private int _activeSlots = 7; // One pay-walled slot
        private Tile _lastAddedTile;
        
        private void Start()
        {
            _activeSlots = 7;
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

            if (tiles.Count < _activeSlots) return true;
            
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
                    
                    Most_HapticFeedback.Generate(Most_HapticFeedback.HapticTypes.MediumImpact);
                    
                    tiles.RemoveRange(i - 2, 3);
                    _lastAddedTile = null;
                    
                    UpdateBufferUI();

                    return; // Can't be more than 1 match
                }

                streak = 1;
            }
        }

        public void UpdateBufferUI()
        {
            for (var i = 0; i < tiles.Count; i++)
            {
                images[i].sprite = config.TileSprites[tiles[i].type].LoadAndCache(ReleaseKey.Game);
                images[i].color = new Color(1, 1, 1, 1);
            }

            for (var i = tiles.Count; i < _activeSlots; i++)
            {
                images[i].sprite = null;
                images[i].color = new Color(1, 1, 1, 20f/255f);
            }
        }

        public void ClearBuffer()
        {
            tiles.Clear();
            _lastAddedTile = null;
            UpdateBufferUI();
        }

        /// <summary>
        /// Unlock pay-walled 8th slot
        /// </summary>
        public void UnlockSlot()
        {
            _activeSlots = 8;
            UpdateBufferUI();
        }

        /// <summary>
        /// Undo PowerUp places last added tile back to the board
        /// </summary>
        public Tile Undo()
        {
            if (_lastAddedTile is null) return null;
            
            tiles.Remove(_lastAddedTile);
            UpdateBufferUI();
            var output = _lastAddedTile;
            _lastAddedTile = null;
            return output;
        }
    }
}