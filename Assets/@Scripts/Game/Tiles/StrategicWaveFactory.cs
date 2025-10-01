using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace Game.Tiles
{
    /// <summary>
    /// Волновой генератор уровней с принудительной сложностью.
    /// Гарантирует, что игрок не может просто собирать открытые тройки.
    /// </summary>
    public class StrategicWaveFactory : FieldFactory
    {
        private readonly float _difficulty;
        private readonly LevelTemplateSO _levelTemplate;
        private readonly List<TileType> _typesAvailable;
        private readonly Random _random;
        
        // Настройки сложности
        private readonly int _bufferSize = 7;
        private readonly float _criticalPathRatio; // Процент тайлов в критическом пути
        private readonly int _minWavesPerTriplet; // Минимум волн для распределения тройки
        private readonly float _trapDensity; // Плотность ловушек
        
        private class TileNode
        {
            public Vector3Int Position { get; set; }
            public int Wave { get; set; } // Волна доступности (0 = сразу доступен)
            public HashSet<TileNode> Blocking { get; set; } = new(); // Кого блокирует
            public HashSet<TileNode> BlockedBy { get; set; } = new(); // Кем заблокирован
            public TileType? Type { get; set; }
            public int CriticalityScore { get; set; } // Насколько критичен для прохождения
            public bool IsKeystone { get; set; } // Ключевой тайл для разблокировки
            public int TripletId { get; set; } = -1;
        }
        
        private class Triplet
        {
            public int Id { get; set; }
            public TileType Type { get; set; }
            public List<TileNode> Nodes { get; set; } = new();
            public int SpreadScore { get; set; } // Насколько распределена тройка
            public bool IsCritical { get; set; } // Критична для прохождения
            public HashSet<int> MustBeCollectedBefore { get; set; } = new(); // ID троек, которые должны быть собраны до этой
            public HashSet<int> UnlocksAfter { get; set; } = new(); // ID троек, которые разблокируются после
        }

        public StrategicWaveFactory(LevelTemplateSO levelTemplate, List<TileType> typesAvailable, float difficulty = 0.5f)
        {
            _levelTemplate = levelTemplate;
            _typesAvailable = typesAvailable;
            _difficulty = Mathf.Clamp01(difficulty);
            _random = new Random();
            
            // Настройки в зависимости от сложности
            _criticalPathRatio = 0.3f + _difficulty * 0.5f; // 30-80% тайлов в критическом пути
            _minWavesPerTriplet = _difficulty < 0.5f ? 2 : (_difficulty < 0.8f ? 3 : 4);
            _trapDensity = _difficulty * 0.7f; // 0-70% ловушек
        }

        public override Field Create()
        {
            var gridX = _levelTemplate.width + 2;
            var gridY = _levelTemplate.height + 2;
            var field = new Field(gridX, gridY, _levelTemplate.layers);

            // 1. Создаем граф позиций
            var nodes = BuildNodeGraph();
            
            // 2. Вычисляем волны доступности
            CalculateWaves(nodes);
            
            // 3. Определяем критические узлы и пути
            IdentifyCriticalNodes(nodes);
            
            // 4. Создаем тройки с учетом волн и критичности
            var triplets = CreateStrategicTriplets(nodes);
            
            // 5. Назначаем типы с учетом ловушек
            AssignTypesWithTraps(triplets);
            
            // 6. Валидируем сложность
            ValidateDifficulty(triplets, nodes);
            
            // 7. Размещаем на поле
            PlaceTilesOnField(field, nodes);

            return field;
        }

        private Dictionary<Vector3Int, TileNode> BuildNodeGraph()
        {
            var nodes = new Dictionary<Vector3Int, TileNode>();
            
            // Создаем узлы для всех позиций
            for (var z = 0; z < _levelTemplate.layers; z++)
            {
                for (var y = 0; y < _levelTemplate.height; y++)
                {
                    for (var x = 0; x < _levelTemplate.width; x++)
                    {
                        if (!_levelTemplate.Field[x, y, z]) continue;
                        
                        var pos = new Vector3Int(x + 1, y + 1, z);
                        nodes[pos] = new TileNode { Position = pos };
                    }
                }
            }
            
            // Устанавливаем связи блокировки
            foreach (var node in nodes.Values)
            {
                var pos = node.Position;
                
                // Проверяем кого блокирует этот узел (тайлы под ним)
                if (pos.z > 0)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        for (var dy = -1; dy <= 1; dy++)
                        {
                            var belowPos = new Vector3Int(pos.x + dx, pos.y + dy, pos.z - 1);
                            if (nodes.TryGetValue(belowPos, out var belowNode))
                            {
                                node.Blocking.Add(belowNode);
                                belowNode.BlockedBy.Add(node);
                            }
                        }
                    }
                }
            }
            
            return nodes;
        }

        private void CalculateWaves(Dictionary<Vector3Int, TileNode> nodes)
        {
            var processed = new HashSet<TileNode>();
            var currentWave = 0;
            
            while (processed.Count < nodes.Count)
            {
                var waveNodes = new List<TileNode>();
                
                foreach (var node in nodes.Values)
                {
                    if (processed.Contains(node)) continue;
                    
                    // Узел доступен, если все блокирующие его узлы обработаны
                    if (node.BlockedBy.All(blocker => processed.Contains(blocker)))
                    {
                        node.Wave = currentWave;
                        waveNodes.Add(node);
                    }
                }
                
                if (waveNodes.Count == 0 && processed.Count < nodes.Count)
                {
                    // Deadlock - принудительно открываем узел с минимальными блокировками
                    var stuck = nodes.Values
                        .Where(n => !processed.Contains(n))
                        .OrderBy(n => n.BlockedBy.Count(b => !processed.Contains(b)))
                        .First();
                    stuck.Wave = currentWave;
                    waveNodes.Add(stuck);
                }
                
                foreach (var node in waveNodes)
                {
                    processed.Add(node);
                }
                
                currentWave++;
            }
        }

        private void IdentifyCriticalNodes(Dictionary<Vector3Int, TileNode> nodes)
        {
            // Вычисляем критичность каждого узла
            foreach (var node in nodes.Values)
            {
                // Критичность = сколько узлов зависит от этого
                node.CriticalityScore = CalculateCriticalityScore(node, nodes);
                
                // Ключевые узлы - те, которые открывают много других
                node.IsKeystone = node.Blocking.Count > 4 || 
                                 (node.Blocking.Count > 2 && node.Wave < 3);
            }
            
            // Отмечаем узлы в критическом пути
            var sortedByCriticality = nodes.Values
                .OrderByDescending(n => n.CriticalityScore)
                .ToList();
            
            var criticalCount = (int)(nodes.Count * _criticalPathRatio);
            for (var i = 0; i < criticalCount && i < sortedByCriticality.Count; i++)
            {
                sortedByCriticality[i].CriticalityScore *= 2; // Удваиваем важность
            }
        }

        private int CalculateCriticalityScore(TileNode node, Dictionary<Vector3Int, TileNode> allNodes)
        {
            var score = 0;
            var visited = new HashSet<TileNode>();
            var queue = new Queue<TileNode>();
            
            queue.Enqueue(node);
            
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!visited.Add(current)) continue;
                
                score++;
                
                foreach (var blocked in current.Blocking)
                {
                    if (!visited.Contains(blocked))
                    {
                        queue.Enqueue(blocked);
                    }
                }
            }
            
            // Бонус за позицию
            score += (10 - node.Wave) * 2; // Ранние волны важнее
            if (node.IsKeystone) score *= 2;
            
            return score;
        }

        private List<Triplet> CreateStrategicTriplets(Dictionary<Vector3Int, TileNode> nodes)
        {
            var triplets = new List<Triplet>();
            var availableNodes = nodes.Values.ToList();
            var tripletId = 0;
            
            // Группируем узлы по волнам
            var waveGroups = availableNodes.GroupBy(n => n.Wave)
                .OrderBy(g => g.Key)
                .ToDictionary(g => g.Key, g => g.ToList());
            
            var maxWave = waveGroups.Keys.Max();
            
            while (availableNodes.Count >= 3)
            {
                var triplet = new Triplet { Id = tripletId++ };
                
                if (_difficulty < 0.3f)
                {
                    // Легкий: можем размещать на близких волнах
                    triplet.Nodes = SelectEasyTriplet(availableNodes, waveGroups);
                }
                else if (_difficulty < 0.6f)
                {
                    // Средний: распределяем по волнам
                    triplet.Nodes = SelectMediumTriplet(availableNodes, waveGroups, maxWave);
                }
                else if (_difficulty < 0.85f)
                {
                    // Сложный: максимальное распределение
                    triplet.Nodes = SelectHardTriplet(availableNodes, waveGroups, maxWave);
                }
                else
                {
                    // Экстрим: специальные паттерны
                    triplet.Nodes = SelectExtremeTriplet(availableNodes, waveGroups, maxWave);
                }
                
                if (triplet.Nodes.Count < 3)
                {
                    // Fallback на случайный выбор
                    triplet.Nodes = availableNodes.OrderBy(_ => _random.Next()).Take(3).ToList();
                }
                
                // Вычисляем распределение тройки
                triplet.SpreadScore = CalculateSpread(triplet.Nodes);
                triplet.IsCritical = triplet.Nodes.Any(n => n.CriticalityScore > 10);
                
                // Устанавливаем ID тройки для узлов
                foreach (var node in triplet.Nodes)
                {
                    node.TripletId = triplet.Id;
                    availableNodes.Remove(node);
                }
                
                triplets.Add(triplet);
            }
            
            // Устанавливаем зависимости между тройками
            EstablishTripletDependencies(triplets);
            
            return triplets;
        }

        private List<TileNode> SelectEasyTriplet(List<TileNode> available, 
            Dictionary<int, List<TileNode>> waveGroups)
        {
            var selected = new List<TileNode>();
            
            // Берем 2 из одной волны, 1 из следующей
            var wave = waveGroups.Where(w => w.Value.Count(n => available.Contains(n)) >= 2)
                .OrderBy(w => w.Key)
                .FirstOrDefault();
            
            if (wave.Value != null)
            {
                var fromWave = wave.Value.Where(n => available.Contains(n))
                    .OrderBy(_ => _random.Next())
                    .Take(2)
                    .ToList();
                selected.AddRange(fromWave);
                
                // Третий из близкой волны
                var nextWave = waveGroups
                    .Where(w => Math.Abs(w.Key - wave.Key) <= 2 && w.Key != wave.Key)
                    .SelectMany(w => w.Value)
                    .Where(n => available.Contains(n))
                    .OrderBy(_ => _random.Next())
                    .FirstOrDefault();
                
                if (nextWave != null)
                    selected.Add(nextWave);
            }
            
            return selected;
        }

        private List<TileNode> SelectMediumTriplet(List<TileNode> available, 
            Dictionary<int, List<TileNode>> waveGroups, int maxWave)
        {
            var selected = new List<TileNode>();
            
            // Распределяем по 3 разным волнам
            var targetWaves = new List<int>();
            
            if (maxWave >= 2)
            {
                // Начало, середина, конец
                targetWaves.Add(_random.Next(0, maxWave / 3));
                targetWaves.Add(_random.Next(maxWave / 3, 2 * maxWave / 3));
                targetWaves.Add(_random.Next(2 * maxWave / 3, maxWave + 1));
            }
            
            foreach (var targetWave in targetWaves.Distinct())
            {
                var node = waveGroups
                    .Where(w => Math.Abs(w.Key - targetWave) <= 1)
                    .SelectMany(w => w.Value)
                    .Where(n => available.Contains(n) && !selected.Contains(n))
                    .OrderBy(_ => _random.Next())
                    .FirstOrDefault();
                
                if (node != null)
                    selected.Add(node);
            }
            
            return selected;
        }

        private List<TileNode> SelectHardTriplet(List<TileNode> available, 
            Dictionary<int, List<TileNode>> waveGroups, int maxWave)
        {
            var selected = new List<TileNode>();
            
            // Стратегия: 1 поверхностный (приманка), 2 глубоких
            var surface = available.Where(n => n.Wave <= 1)
                .OrderBy(n => n.CriticalityScore) // Берем наименее критичный
                .FirstOrDefault();
            
            if (surface != null)
            {
                selected.Add(surface);
                
                // 2 глубоких, желательно критичных
                var deep = available
                    .Where(n => n != surface && n.Wave >= maxWave / 2)
                    .OrderByDescending(n => n.CriticalityScore)
                    .ThenByDescending(n => n.Wave)
                    .Take(2);
                
                selected.AddRange(deep);
            }
            
            // Если не получилось, максимально распределяем
            if (selected.Count < 3)
            {
                selected.Clear();
                
                var waves = waveGroups.Keys.OrderBy(k => k).ToList();
                if (waves.Count >= 3)
                {
                    // Берем из максимально разных волн
                    var selectedWaves = new List<int> 
                    { 
                        waves[0], 
                        waves[waves.Count / 2], 
                        waves[waves.Count - 1] 
                    };
                    
                    foreach (var wave in selectedWaves)
                    {
                        var node = waveGroups[wave]
                            .Where(n => available.Contains(n) && !selected.Contains(n))
                            .OrderByDescending(n => n.CriticalityScore)
                            .FirstOrDefault();
                        
                        if (node != null)
                            selected.Add(node);
                    }
                }
            }
            
            return selected;
        }

        private List<TileNode> SelectExtremeTriplet(List<TileNode> available, 
            Dictionary<int, List<TileNode>> waveGroups, int maxWave)
        {
            var selected = new List<TileNode>();
            
            // Экстремальные паттерны
            var pattern = _random.Next(0, 4);
            
            switch (pattern)
            {
                case 0: // "Обманка" - 2 легких, 1 максимально глубокий критичный
                    var easy = available.Where(n => n.Wave <= 1)
                        .OrderBy(_ => _random.Next())
                        .Take(2)
                        .ToList();
                    
                    var hardest = available.Where(n => !easy.Contains(n))
                        .OrderByDescending(n => n.Wave * 10 + n.CriticalityScore)
                        .FirstOrDefault();
                    
                    if (easy.Count == 2 && hardest != null)
                    {
                        selected.AddRange(easy);
                        selected.Add(hardest);
                    }
                    break;
                    
                case 1: // "Каскад" - каждый следующий разблокирует много тайлов
                    var cascade = available
                        .Where(n => n.IsKeystone)
                        .OrderBy(n => n.Wave)
                        .Take(3)
                        .ToList();
                    
                    if (cascade.Count == 3)
                        selected = cascade;
                    break;
                    
                case 2: // "Замок" - все 3 в критическом пути с максимальным распределением
                    selected = available
                        .OrderByDescending(n => n.CriticalityScore)
                        .Take(10) // Берем топ-10 критичных
                        .OrderBy(n => _random.Next())
                        .Take(3)
                        .ToList();
                    
                    // Убеждаемся что они из разных волн
                    if (selected.Select(n => n.Wave).Distinct().Count() < 3)
                    {
                        selected.Clear();
                    }
                    break;
                    
                case 3: // "Лабиринт" - максимально запутанные зависимости
                    var first = available.OrderBy(_ => _random.Next()).FirstOrDefault();
                    if (first != null)
                    {
                        selected.Add(first);
                        
                        // Второй блокирует много других
                        var second = available
                            .Where(n => n != first && n.Blocking.Count > 2)
                            .OrderByDescending(n => n.Blocking.Count)
                            .FirstOrDefault();
                        
                        if (second != null)
                        {
                            selected.Add(second);
                            
                            // Третий заблокирован вторым или в глубине
                            var third = available
                                .Where(n => !selected.Contains(n))
                                .Where(n => n.BlockedBy.Contains(second) || n.Wave > maxWave * 0.7f)
                                .OrderByDescending(n => n.Wave)
                                .FirstOrDefault();
                            
                            if (third != null)
                                selected.Add(third);
                        }
                    }
                    break;
            }
            
            // Fallback на сложный паттерн
            if (selected.Count < 3)
            {
                selected = SelectHardTriplet(available, waveGroups, maxWave);
            }
            
            return selected;
        }

        private int CalculateSpread(List<TileNode> nodes)
        {
            if (nodes.Count < 2) return 0;
            
            var waves = nodes.Select(n => n.Wave).ToList();
            var minWave = waves.Min();
            var maxWave = waves.Max();
            var avgWave = waves.Average();
            
            // Spread = разница волн + бонус за критичность
            var spread = (maxWave - minWave) * 10;
            spread += (int)(nodes.Average(n => n.CriticalityScore));
            
            return spread;
        }

        private void EstablishTripletDependencies(List<Triplet> triplets)
        {
            foreach (var triplet in triplets)
            {
                // Находим какие тройки блокируют текущую
                var blockingTriplets = new HashSet<int>();
                
                foreach (var node in triplet.Nodes)
                {
                    foreach (var blocker in node.BlockedBy)
                    {
                        if (blocker.TripletId >= 0 && blocker.TripletId != triplet.Id)
                        {
                            blockingTriplets.Add(blocker.TripletId);
                            triplet.MustBeCollectedBefore.Add(blocker.TripletId);
                        }
                    }
                }
                
                // Находим какие тройки разблокируются после текущей
                foreach (var node in triplet.Nodes)
                {
                    foreach (var blocked in node.Blocking)
                    {
                        if (blocked.TripletId >= 0 && blocked.TripletId != triplet.Id)
                        {
                            triplet.UnlocksAfter.Add(blocked.TripletId);
                        }
                    }
                }
            }
        }

        private void AssignTypesWithTraps(List<Triplet> triplets)
        {
            // Сортируем тройки по приоритету (критичность + распределение)
            triplets.Sort((a, b) => 
            {
                var scoreA = a.SpreadScore + (a.IsCritical ? 100 : 0);
                var scoreB = b.SpreadScore + (b.IsCritical ? 100 : 0);
                return scoreB.CompareTo(scoreA);
            });
            
            var typeUsage = new Dictionary<TileType, int>();
            foreach (var type in _typesAvailable)
            {
                typeUsage[type] = 0;
            }
            
            foreach (var triplet in triplets)
            {
                TileType selectedType;
                
                // Создаем ловушки на высокой сложности
                if (_random.NextDouble() < _trapDensity)
                {
                    // Ловушка: используем тип, который уже есть в зависимостях
                    var dependencyTypes = triplets
                        .Where(t => triplet.MustBeCollectedBefore.Contains(t.Id))
                        .Select(t => t.Type)
                        .ToList();
                    
                    if (dependencyTypes.Any())
                    {
                        // Создаем конфликт типов
                        selectedType = dependencyTypes[_random.Next(dependencyTypes.Count)];
                    }
                    else if (triplet.IsCritical && typeUsage.Any())
                    {
                        // Для критичных троек используем наиболее использованный тип
                        selectedType = typeUsage.OrderByDescending(kvp => kvp.Value)
                            .ThenBy(_ => _random.Next())
                            .First().Key;
                    }
                    else
                    {
                        // Обычное распределение
                        selectedType = typeUsage.OrderBy(kvp => kvp.Value)
                            .ThenBy(_ => _random.Next())
                            .First().Key;
                    }
                }
                else
                {
                    // Без ловушки - балансированное распределение
                    selectedType = typeUsage.OrderBy(kvp => kvp.Value)
                        .ThenBy(_ => _random.Next())
                        .First().Key;
                }
                
                triplet.Type = selectedType;
                typeUsage[selectedType] += 3;
                
                // Назначаем тип узлам
                foreach (var node in triplet.Nodes)
                {
                    node.Type = selectedType;
                }
            }
            
            // Специальная обработка для максимальной сложности
            if (_difficulty >= 0.9f)
            {
                CreateDeadlockScenarios(triplets);
            }
        }

        private void CreateDeadlockScenarios(List<Triplet> triplets)
        {
            // Создаем сценарии, где неправильный порядок сбора ведет к поражению
            
            // Находим критические цепочки
            var chains = FindCriticalChains(triplets);
            
            foreach (var chain in chains)
            {
                if (chain.Count < 3) continue;
                
                // Делаем типы в цепочке взаимозависимыми
                var chainTypes = _typesAvailable.OrderBy(_ => _random.Next()).Take(2).ToList();
                
                for (var i = 0; i < chain.Count; i++)
                {
                    chain[i].Type = chainTypes[i % chainTypes.Count];
                    foreach (var node in chain[i].Nodes)
                    {
                        node.Type = chain[i].Type;
                    }
                }
            }
        }

        private List<List<Triplet>> FindCriticalChains(List<Triplet> triplets)
        {
            var chains = new List<List<Triplet>>();
            var visited = new HashSet<int>();
            
            foreach (var triplet in triplets.Where(t => t.IsCritical))
            {
                if (visited.Contains(triplet.Id)) continue;
                
                var chain = new List<Triplet>();
                var current = triplet;
                
                while (current != null && !visited.Contains(current.Id))
                {
                    chain.Add(current);
                    visited.Add(current.Id);
                    
                    // Следуем по зависимостям
                    current = triplets.FirstOrDefault(t => 
                        current.UnlocksAfter.Contains(t.Id) && !visited.Contains(t.Id));
                }
                
                if (chain.Count >= 2)
                {
                    chains.Add(chain);
                }
            }
            
            return chains;
        }

        private void ValidateDifficulty(List<Triplet> triplets, Dictionary<Vector3Int, TileNode> nodes)
        {
            // Проверяем, что на каждой волне не слишком много одинаковых типов
            var waveTypes = new Dictionary<int, Dictionary<TileType, int>>();
            
            foreach (var node in nodes.Values.Where(n => n.Type.HasValue))
            {
                if (!waveTypes.ContainsKey(node.Wave))
                    waveTypes[node.Wave] = new Dictionary<TileType, int>();
                
                if (!waveTypes[node.Wave].ContainsKey(node.Type.Value))
                    waveTypes[node.Wave][node.Type.Value] = 0;
                
                waveTypes[node.Wave][node.Type.Value]++;
            }
            
            // Корректируем если на волне 0 слишком много троек
            if (waveTypes.ContainsKey(0))
            {
                foreach (var kvp in waveTypes[0])
                {
                    if (kvp.Value >= 3)
                    {
                        // Находим эти узлы и меняем типы
                        var problematicNodes = nodes.Values
                            .Where(n => n.Wave == 0 && n.Type == kvp.Key)
                            .Skip(2) // Оставляем только 2
                            .ToList();
                        
                        foreach (var node in problematicNodes)
                        {
                            // Меняем на редкий тип
                            var rareType = _typesAvailable
                                .OrderBy(t => nodes.Values.Count(n => n.Type == t))
                                .First();
                            
                            node.Type = rareType;
                            
                            // Обновляем тройку
                            var triplet = triplets.FirstOrDefault(t => t.Id == node.TripletId);
                            if (triplet != null)
                            {
                                triplet.Type = rareType;
                                foreach (var tn in triplet.Nodes)
                                {
                                    tn.Type = rareType;
                                }
                            }
                        }
                    }
                }
            }
            
            // Проверяем проходимость с учетом размера буфера
            ValidateBufferConstraints(triplets, nodes);
            
            // Финальная проверка: убеждаемся, что нет легких путей
            EnsureNoEasyPaths(triplets, nodes);
        }
        
        private void ValidateBufferConstraints(List<Triplet> triplets, Dictionary<Vector3Int, TileNode> nodes)
        {
            // Симулируем прохождение с учетом буфера
            var simulation = SimulateWithBuffer(triplets, nodes);
            
            if (!simulation.IsPossible)
            {
                Debug.LogWarning($"Level might be impossible with buffer size {_bufferSize}. Adjusting...");
                AdjustForBufferConstraints(triplets, nodes, simulation);
            }
            else if (_difficulty >= 0.8f && simulation.BufferPressure < 0.6f)
            {
                // На высокой сложности буфер должен быть под давлением
                IncreasedBufferPressure(triplets, nodes);
            }
        }
        
        private class BufferSimulation
        {
            public bool IsPossible { get; set; }
            public float BufferPressure { get; set; } // 0-1, насколько заполнен буфер в среднем
            public List<int> ProblematicWaves { get; set; } = new();
            public Dictionary<TileType, int> TypeCongestion { get; set; } = new();
        }
        
        private BufferSimulation SimulateWithBuffer(List<Triplet> triplets, Dictionary<Vector3Int, TileNode> nodes)
        {
            var sim = new BufferSimulation { IsPossible = true };
            var buffer = new List<TileType>();
            var maxBufferUsed = 0;
            var totalSteps = 0;
            var waveNodes = nodes.Values.GroupBy(n => n.Wave).ToDictionary(g => g.Key, g => g.ToList());
            
            // Симулируем прохождение по волнам
            for (var wave = 0; wave <= waveNodes.Keys.Max(); wave++)
            {
                if (!waveNodes.ContainsKey(wave)) continue;
                
                var availableTypes = waveNodes[wave]
                    .Where(n => n.Type.HasValue)
                    .GroupBy(n => n.Type.Value)
                    .ToDictionary(g => g.Key, g => g.Count());
                
                // Пытаемся собрать доступные тройки
                foreach (var typeCount in availableTypes)
                {
                    var inBuffer = buffer.Count(t => t == typeCount.Key);
                    var totalAvailable = inBuffer + typeCount.Value;
                    
                    if (totalAvailable >= 3)
                    {
                        // Можем собрать тройку
                        var toCollect = Math.Min(3 - inBuffer, typeCount.Value);
                        
                        // Добавляем в буфер
                        for (var i = 0; i < toCollect; i++)
                        {
                            buffer.Add(typeCount.Key);
                        }
                        
                        // Собираем тройку
                        buffer.RemoveAll(t => t == typeCount.Key);
                    }
                    else
                    {
                        // Добавляем в буфер все доступные
                        for (var i = 0; i < typeCount.Value; i++)
                        {
                            buffer.Add(typeCount.Key);
                        }
                    }
                    
                    // Проверяем переполнение буфера
                    if (buffer.Count > _bufferSize)
                    {
                        sim.IsPossible = false;
                        sim.ProblematicWaves.Add(wave);
                        sim.TypeCongestion[typeCount.Key] = buffer.Count(t => t == typeCount.Key);
                    }
                    
                    maxBufferUsed = Math.Max(maxBufferUsed, buffer.Count);
                    totalSteps++;
                }
            }
            
            sim.BufferPressure = (float)maxBufferUsed / _bufferSize;
            return sim;
        }
        
        private void AdjustForBufferConstraints(List<Triplet> triplets, Dictionary<Vector3Int, TileNode> nodes, BufferSimulation sim)
        {
            // Корректируем проблемные волны
            foreach (var wave in sim.ProblematicWaves)
            {
                var waveNodes = nodes.Values.Where(n => n.Wave == wave).ToList();
                
                // Находим перегруженные типы
                foreach (var congestion in sim.TypeCongestion)
                {
                    var nodesOfType = waveNodes.Where(n => n.Type == congestion.Key).ToList();
                    
                    if (nodesOfType.Count > _bufferSize - 2) // Оставляем запас в 2 слота
                    {
                        // Перемещаем часть на другие волны
                        var toMove = nodesOfType.Skip(_bufferSize - 3).ToList();
                        
                        foreach (var node in toMove)
                        {
                            // Увеличиваем волну (делаем менее доступным)
                            node.Wave += 1 + (int)(_difficulty * 2);
                            
                            // Или меняем тип на менее загруженный
                            if (_random.NextDouble() < 0.5f)
                            {
                                var lessUsedType = _typesAvailable
                                    .OrderBy(t => nodes.Values.Count(n => n.Type == t && n.Wave == wave))
                                    .First();
                                
                                node.Type = lessUsedType;
                                
                                // Обновляем всю тройку
                                var triplet = triplets.FirstOrDefault(t => t.Id == node.TripletId);
                                if (triplet != null)
                                {
                                    triplet.Type = lessUsedType;
                                    foreach (var tn in triplet.Nodes)
                                    {
                                        tn.Type = lessUsedType;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
        
        private void IncreasedBufferPressure(List<Triplet> triplets, Dictionary<Vector3Int, TileNode> nodes)
        {
            // Для высокой сложности - создаем ситуации, где буфер почти заполнен
            var wave0Nodes = nodes.Values.Where(n => n.Wave == 0).ToList();
            var typesToAdd = _typesAvailable.OrderBy(_ => _random.Next()).Take(_bufferSize - 1).ToList();
            
            // Добавляем по 1-2 тайла разных типов на старте, чтобы сразу заполнить буфер
            var nodeIndex = 0;
            foreach (var type in typesToAdd)
            {
                if (nodeIndex >= wave0Nodes.Count) break;
                
                var count = _random.Next(1, 3); // 1 или 2 тайла этого типа
                for (var i = 0; i < count && nodeIndex < wave0Nodes.Count; i++)
                {
                    var node = wave0Nodes[nodeIndex++];
                    if (node.Type != type)
                    {
                        // Находим тройку и меняем тип
                        var triplet = triplets.FirstOrDefault(t => t.Id == node.TripletId);
                        if (triplet != null)
                        {
                            triplet.Type = type;
                            foreach (var tn in triplet.Nodes)
                            {
                                tn.Type = type;
                            }
                        }
                    }
                }
            }
        }

        private void EnsureNoEasyPaths(List<Triplet> triplets, Dictionary<Vector3Int, TileNode> nodes)
        {
            // Симулируем первые несколько ходов
            var wave0Nodes = nodes.Values.Where(n => n.Wave == 0).ToList();
            var wave0Types = wave0Nodes.Where(n => n.Type.HasValue)
                .GroupBy(n => n.Type.Value)
                .ToDictionary(g => g.Key, g => g.Count());
            
            // Проверяем каждый тип
            foreach (var typeCount in wave0Types)
            {
                if (typeCount.Value >= 3)
                {
                    // Слишком легко! Перераспределяем
                    var nodesOfType = wave0Nodes.Where(n => n.Type == typeCount.Key).ToList();
                    
                    // Оставляем максимум 2 на волне 0
                    for (var i = 2; i < nodesOfType.Count; i++)
                    {
                        var node = nodesOfType[i];
                        var triplet = triplets.FirstOrDefault(t => t.Id == node.TripletId);
                        
                        if (triplet != null)
                        {
                            // Меняем всю тройку на другой тип
                            var newType = _typesAvailable
                                .Where(t => wave0Types.GetValueOrDefault(t, 0) < 2)
                                .OrderBy(_ => _random.Next())
                                .FirstOrDefault();

                            triplet.Type = newType;
                            foreach (var tn in triplet.Nodes)
                            {
                                tn.Type = newType;
                            }
                        }
                    }
                }
            }
        }

        private void PlaceTilesOnField(Field field, Dictionary<Vector3Int, TileNode> nodes)
        {
            // Размещаем от верхних слоев к нижним для корректной работы блокировок
            var sortedNodes = nodes.Values
                .Where(n => n.Type.HasValue)
                .OrderByDescending(n => n.Position.z)
                .ThenBy(n => n.Position.y)
                .ThenBy(n => n.Position.x);
            
            foreach (var node in sortedNodes)
            {
                var tile = new Tile
                {
                    type = node.Type.Value,
                    gridPosition = node.Position
                };
                
                if (!field.PlaceTile(tile))
                {
                    Debug.LogError($"Failed to place tile at {node.Position}");
                }
            }
        }
    }
}