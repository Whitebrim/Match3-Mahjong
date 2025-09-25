using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace Game.Tiles
{
    public class FieldFactoryFromTemplate : FieldFactory
    {
        private readonly float _difficulty; // 0.0 - easy, 1.0 - hard
        private readonly LevelTemplateSO _levelTemplate;
        private readonly List<TileType> _typesAvailable;

        public FieldFactoryFromTemplate(LevelTemplateSO levelTemplate, List<TileType> typesAvailable,
            float difficulty = 0.5f)
        {
            _levelTemplate = levelTemplate;
            _typesAvailable = typesAvailable;
            _difficulty = Mathf.Clamp01(difficulty);
        }

        public override Field Create()
        {
            var gridX = _levelTemplate.width + 2;
            var gridY = _levelTemplate.height + 2;
            var field = new Field(gridX, gridY, _levelTemplate.layers);

            var tileTypes = GenerateTileTypes();

            for (var z = _levelTemplate.layers - 1; z >= 0; z--)
            for (var y = 1; y < gridY - 1; y++)
            for (var x = 1; x < gridX - 1; x++)
            {
                if (!_levelTemplate.Field[x - 1, y - 1, z]) continue;

                var pos = new Vector3Int(x, y, z);
                var tile = new Tile
                {
                    type = tileTypes[x - 1, y - 1, z],
                    gridPosition = pos
                };

                if (!field.PlaceTile(tile))
                    throw new Exception("Unexpectedly can't place tile at " + pos);
            }

            return field;
        }

        private TileType[,,] GenerateTileTypes()
        {
            var totalTiles = CountTotalTiles();
            var typeDistribution = DistributeTypes(totalTiles);
            var tileTypes = new TileType[_levelTemplate.width, _levelTemplate.height, _levelTemplate.layers];

            PlaceTripletsStrategically(tileTypes, typeDistribution);

            return tileTypes;
        }

        private int CountTotalTiles()
        {
            var count = 0;
            for (var z = 0; z < _levelTemplate.layers; z++)
            for (var y = 0; y < _levelTemplate.height; y++)
            for (var x = 0; x < _levelTemplate.width; x++)
                if (_levelTemplate.Field[x, y, z])
                    count++;
            return count;
        }

        private Dictionary<TileType, int> DistributeTypes(int totalTiles)
        {
            var distribution = new Dictionary<TileType, int>();
            var baseCount = totalTiles / _typesAvailable.Count / 3 * 3;
            var remainingTiles = totalTiles;

            foreach (var type in _typesAvailable)
            {
                distribution[type] = baseCount;
                remainingTiles -= baseCount;
            }

            var typeIndex = 0;
            while (remainingTiles >= 3)
            {
                distribution[_typesAvailable[typeIndex]] += 3;
                remainingTiles -= 3;
                typeIndex = (typeIndex + 1) % _typesAvailable.Count;
            }

            return distribution;
        }

        private void PlaceTripletsStrategically(TileType[,,] tileTypes, Dictionary<TileType, int> typeDistribution)
        {
            var random = new Random();
            var availablePositions = GetAllAvailablePositions();

            var dependencyChains = CreateDependencyChains(typeDistribution.Keys.ToList(), random);

            foreach (var chain in dependencyChains)
                for (var i = 0; i < chain.Count; i++)
                {
                    var tileType = chain[i];
                    var count = typeDistribution[tileType];
                    var tripletsCount = count / 3;

                    for (var triplet = 0; triplet < tripletsCount; triplet++)
                    {
                        var strategyLevel = i;
                        PlaceStrategicTriplet(tileTypes, availablePositions, tileType, random, strategyLevel);
                    }
                }
        }

        private List<List<TileType>> CreateDependencyChains(List<TileType> types, Random random)
        {
            var shuffled = types.OrderBy(_ => random.Next()).ToList();
            var chains = new List<List<TileType>>();

            var chainSize = Mathf.Max(3, Mathf.RoundToInt(types.Count * (0.3f + _difficulty * 0.3f)));

            for (var i = 0; i < shuffled.Count; i += chainSize)
                chains.Add(shuffled.Skip(i).Take(chainSize).ToList());

            return chains;
        }

        private List<Vector3Int> GetAllAvailablePositions()
        {
            var positions = new List<Vector3Int>();
            for (var z = 0; z < _levelTemplate.layers; z++)
            for (var y = 0; y < _levelTemplate.height; y++)
            for (var x = 0; x < _levelTemplate.width; x++)
                if (_levelTemplate.Field[x, y, z])
                    positions.Add(new Vector3Int(x, y, z));
            return positions;
        }

        private void PlaceStrategicTriplet(TileType[,,] tileTypes, List<Vector3Int> availablePositions,
            TileType tileType, Random random, int strategyLevel)
        {
            var tripletType = ChooseTripletType(random, strategyLevel);
            var positions = SelectTripletPositions(availablePositions, tripletType, random);

            foreach (var pos in positions)
            {
                tileTypes[pos.x, pos.y, pos.z] = tileType;
                availablePositions.Remove(pos);
            }
        }

        private int ChooseTripletType(Random random, int strategyLevel)
        {
            var rand = (float)random.NextDouble();
            var probabilities = CalculateTripletProbabilities(strategyLevel);

            var cumulative = 0f;
            for (var i = 0; i < probabilities.Length; i++)
            {
                cumulative += probabilities[i];
                if (rand <= cumulative)
                    return i + 1;
            }

            return 1;
        }

        private float[] CalculateTripletProbabilities(int strategyLevel)
        {
            var difficultyFactor = _difficulty + strategyLevel * 0.1f;
            difficultyFactor = Mathf.Clamp01(difficultyFactor);

            return new[]
            {
                0.8f - difficultyFactor * 0.6f,
                0.15f + difficultyFactor * 0.1f,
                0.05f + difficultyFactor * 0.15f,
                difficultyFactor * 0.35f
            };
        }

        private List<Vector3Int> SelectTripletPositions(List<Vector3Int> availablePositions, int tripletType,
            Random random)
        {
            switch (tripletType)
            {
                case 1: return SelectSameLevelTriplet(availablePositions, random);
                case 2: return SelectTwoOneTriplet(availablePositions, random);
                case 3: return SelectOneTwoTriplet(availablePositions, random);
                case 4: return SelectCascadeTriplet(availablePositions, random);
                default: return SelectSameLevelTriplet(availablePositions, random);
            }
        }

        private List<Vector3Int> SelectSameLevelTriplet(List<Vector3Int> availablePositions, Random random)
        {
            var layerGroups = availablePositions.GroupBy(p => p.z).Where(g => g.Count() >= 3).ToList();
            if (!layerGroups.Any())
                return SelectRandomPositions(availablePositions, random, 3);

            var selectedLayer = layerGroups[random.Next(layerGroups.Count())];
            return SelectDistantPositions(selectedLayer.ToList(), random, 3);
        }

        private List<Vector3Int> SelectTwoOneTriplet(List<Vector3Int> availablePositions, Random random)
        {
            var result = new List<Vector3Int>();
            var upper = SelectSameLevelTriplet(availablePositions, random);
            if (upper.Count >= 2)
            {
                result.AddRange(upper.Take(2));
                var targetZ = upper[0].z - 1;
                var lower = availablePositions.Where(p => p.z == targetZ && !result.Contains(p)).ToList();
                if (lower.Any())
                    result.Add(lower[random.Next(lower.Count)]);
            }

            return result.Count == 3 ? result : SelectRandomPositions(availablePositions, random, 3);
        }

        private List<Vector3Int> SelectOneTwoTriplet(List<Vector3Int> availablePositions, Random random)
        {
            var result = new List<Vector3Int>();
            var upper = availablePositions.Where(p => p.z >= 1).ToList();
            if (upper.Any())
            {
                result.Add(upper[random.Next(upper.Count)]);
                var targetZ = result[0].z - 1;
                var lower = availablePositions.Where(p => p.z == targetZ && !result.Contains(p)).ToList();
                if (lower.Count >= 2)
                    result.AddRange(SelectDistantPositions(lower, random, 2));
            }

            return result.Count == 3 ? result : SelectRandomPositions(availablePositions, random, 3);
        }

        private List<Vector3Int> SelectCascadeTriplet(List<Vector3Int> availablePositions, Random random)
        {
            var result = new List<Vector3Int>();
            var maxZ = availablePositions.Max(p => p.z);
            for (var z = maxZ; z >= 0 && result.Count < 3; z--)
            {
                var level = availablePositions.Where(p => p.z == z && !result.Contains(p)).ToList();
                if (level.Any())
                    result.Add(level[random.Next(level.Count)]);
            }

            return result.Count == 3 ? result : SelectRandomPositions(availablePositions, random, 3);
        }

        private List<Vector3Int> SelectDistantPositions(List<Vector3Int> positions, Random random, int count)
        {
            if (positions.Count <= count)
                return positions.ToList();

            var result = new List<Vector3Int>();
            var candidates = positions.ToList();

            var first = candidates[random.Next(candidates.Count)];
            result.Add(first);
            candidates.Remove(first);

            for (var i = 1; i < count && candidates.Any(); i++)
            {
                var farthest = candidates.OrderByDescending(c =>
                    result.Min(r => Vector3Int.Distance(c, r))).First();
                result.Add(farthest);
                candidates.Remove(farthest);
            }

            return result;
        }

        private List<Vector3Int> SelectRandomPositions(List<Vector3Int> positions, Random random, int count)
        {
            if (positions.Count <= count)
                return positions.ToList();
            return positions.OrderBy(_ => random.Next()).Take(count).ToList();
        }
    }
}