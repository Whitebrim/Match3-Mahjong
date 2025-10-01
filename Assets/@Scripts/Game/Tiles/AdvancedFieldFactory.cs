using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = System.Random;

namespace Game.Tiles
{
    /// <summary>
    /// Продвинутый генератор уровней с анализом доступности и стратегическим размещением тайлов
    /// </summary>
    public class AdvancedFieldFactory : FieldFactory
    {
        private readonly float _difficulty; // 0.0 - easy, 1.0 - hard
        private readonly LevelTemplateSO _levelTemplate;
        private readonly List<TileType> _typesAvailable;
        private readonly Random _random;
        
        // Структура для анализа доступности
        private class TilePosition
        {
            public Vector3Int Position { get; set; }
            public int AccessibilityLayer { get; set; } // На каком этапе станет доступен
            public List<Vector3Int> BlockedBy { get; set; } = new();
            public List<Vector3Int> Blocking { get; set; } = new();
            public TileType? AssignedType { get; set; }
            public int TripletGroup { get; set; } = -1;
        }

        // Группа из трех тайлов одного типа
        private class TripletGroup
        {
            public TileType Type { get; set; }
            public List<TilePosition> Positions { get; set; } = new();
            public int MinAccessLayer { get; set; }
            public int MaxAccessLayer { get; set; }
            public HashSet<int> DependsOn { get; set; } = new(); // Группы, которые нужно убрать для доступа
            public int Priority { get; set; } // Приоритет размещения
        }

        public AdvancedFieldFactory(LevelTemplateSO levelTemplate, List<TileType> typesAvailable,
            float difficulty = 0.5f)
        {
            _levelTemplate = levelTemplate;
            _typesAvailable = typesAvailable;
            _difficulty = Mathf.Clamp01(difficulty);
            _random = new Random();
        }

        public override Field Create()
        {
            var gridX = _levelTemplate.width + 2;
            var gridY = _levelTemplate.height + 2;
            var field = new Field(gridX, gridY, _levelTemplate.layers);

            // 1. Анализируем структуру уровня
            var positions = AnalyzePositions();
            
            // 2. Определяем слои доступности
            CalculateAccessibilityLayers(positions);
            
            // 3. Создаем группы троек с учетом сложности
            var tripletGroups = CreateTripletGroups(positions);
            
            // 4. Назначаем типы тайлов группам
            AssignTypesToGroups(tripletGroups);
            
            // 5. Размещаем тайлы на поле
            PlaceTilesOnField(field, positions);

            return field;
        }

        private Dictionary<Vector3Int, TilePosition> AnalyzePositions()
        {
            var positions = new Dictionary<Vector3Int, TilePosition>();
            
            // Создаем все позиции
            for (var z = 0; z < _levelTemplate.layers; z++)
            {
                for (var y = 0; y < _levelTemplate.height; y++)
                {
                    for (var x = 0; x < _levelTemplate.width; x++)
                    {
                        if (!_levelTemplate.Field[x, y, z]) continue;
                        
                        var gridPos = new Vector3Int(x + 1, y + 1, z);
                        positions[gridPos] = new TilePosition { Position = gridPos };
                    }
                }
            }
            
            // Определяем блокировки
            foreach (var kvp in positions)
            {
                var pos = kvp.Key;
                var tilePos = kvp.Value;
                
                // Проверяем, какие тайлы блокируют текущий
                for (var dz = 1; dz < _levelTemplate.layers - pos.z; dz++)
                {
                    for (var dx = -1; dx <= 1; dx++)
                    {
                        for (var dy = -1; dy <= 1; dy++)
                        {
                            var checkPos = new Vector3Int(pos.x + dx, pos.y + dy, pos.z + dz);
                            if (positions.ContainsKey(checkPos))
                            {
                                tilePos.BlockedBy.Add(checkPos);
                                positions[checkPos].Blocking.Add(pos);
                            }
                        }
                    }
                }
            }
            
            return positions;
        }

        private void CalculateAccessibilityLayers(Dictionary<Vector3Int, TilePosition> positions)
        {
            var processed = new HashSet<Vector3Int>();
            var currentLayer = 0;
            
            while (processed.Count < positions.Count)
            {
                var foundInThisLayer = false;
                
                foreach (var kvp in positions)
                {
                    if (processed.Contains(kvp.Key)) continue;
                    
                    // Проверяем, все ли блокирующие тайлы уже обработаны
                    var canAccess = kvp.Value.BlockedBy.All(blocker => processed.Contains(blocker));
                    
                    if (canAccess)
                    {
                        kvp.Value.AccessibilityLayer = currentLayer;
                        processed.Add(kvp.Key);
                        foundInThisLayer = true;
                    }
                }
                
                if (!foundInThisLayer && processed.Count < positions.Count)
                {
                    // Если застряли, принудительно открываем следующий слой
                    var stuck = positions.Where(p => !processed.Contains(p.Key)).First();
                    stuck.Value.AccessibilityLayer = currentLayer;
                    processed.Add(stuck.Key);
                }
                
                currentLayer++;
            }
        }

        private List<TripletGroup> CreateTripletGroups(Dictionary<Vector3Int, TilePosition> positions)
        {
            var totalTiles = positions.Count;
            var tripletsNeeded = totalTiles / 3;
            var tripletGroups = new List<TripletGroup>();
            
            var remainingPositions = positions.Values.ToList();
            var groupId = 0;
            
            // Стратегии размещения в зависимости от сложности
            var strategies = DetermineStrategies();
            
            for (var i = 0; i < tripletsNeeded; i++)
            {
                var strategy = strategies[i % strategies.Count];
                var group = CreateTripletByStrategy(remainingPositions, strategy, groupId++);
                
                if (group != null)
                {
                    tripletGroups.Add(group);
                    foreach (var pos in group.Positions)
                    {
                        remainingPositions.Remove(pos);
                    }
                }
            }
            
            // Анализируем зависимости между группами
            AnalyzeGroupDependencies(tripletGroups, positions);
            
            return tripletGroups;
        }

        private List<PlacementStrategy> DetermineStrategies()
        {
            var strategies = new List<PlacementStrategy>();
            
            if (_difficulty < 0.3f)
            {
                // Легкий: много троек на одном слое доступности
                strategies.Add(PlacementStrategy.SameAccessLayer);
                strategies.Add(PlacementStrategy.SameAccessLayer);
                strategies.Add(PlacementStrategy.NearbyLayers);
            }
            else if (_difficulty < 0.6f)
            {
                // Средний: микс стратегий
                strategies.Add(PlacementStrategy.SameAccessLayer);
                strategies.Add(PlacementStrategy.NearbyLayers);
                strategies.Add(PlacementStrategy.BlockingChain);
                strategies.Add(PlacementStrategy.DistributedLayers);
            }
            else if (_difficulty < 0.85f)
            {
                // Сложный: много блокирующих цепочек
                strategies.Add(PlacementStrategy.BlockingChain);
                strategies.Add(PlacementStrategy.BlockingChain);
                strategies.Add(PlacementStrategy.DistributedLayers);
                strategies.Add(PlacementStrategy.DeepNesting);
            }
            else
            {
                // Экстремальный: максимальные зависимости
                strategies.Add(PlacementStrategy.DeepNesting);
                strategies.Add(PlacementStrategy.BlockingChain);
                strategies.Add(PlacementStrategy.MaximalInterdependence);
                strategies.Add(PlacementStrategy.TrapFormation);
            }
            
            // Перемешиваем стратегии
            return strategies.OrderBy(_ => _random.Next()).ToList();
        }

        private enum PlacementStrategy
        {
            SameAccessLayer,        // Все три на одном слое доступности
            NearbyLayers,          // На соседних слоях
            BlockingChain,         // Каждый следующий блокирует предыдущий
            DistributedLayers,     // Максимально распределены по слоям
            DeepNesting,           // Глубокая вложенность
            MaximalInterdependence,// Максимальные перекрестные зависимости
            TrapFormation         // Создание "ловушек" - ложных ходов
        }

        private TripletGroup CreateTripletByStrategy(List<TilePosition> available, 
            PlacementStrategy strategy, int groupId)
        {
            if (available.Count < 3) return null;
            
            var group = new TripletGroup();
            
            switch (strategy)
            {
                case PlacementStrategy.SameAccessLayer:
                    group.Positions = SelectSameLayerPositions(available, 3);
                    break;
                    
                case PlacementStrategy.NearbyLayers:
                    group.Positions = SelectNearbyLayerPositions(available, 3);
                    break;
                    
                case PlacementStrategy.BlockingChain:
                    group.Positions = SelectBlockingChain(available, 3);
                    break;
                    
                case PlacementStrategy.DistributedLayers:
                    group.Positions = SelectDistributedPositions(available, 3);
                    break;
                    
                case PlacementStrategy.DeepNesting:
                    group.Positions = SelectDeepNestedPositions(available, 3);
                    break;
                    
                case PlacementStrategy.MaximalInterdependence:
                    group.Positions = SelectMaximalInterdependence(available, 3);
                    break;
                    
                case PlacementStrategy.TrapFormation:
                    group.Positions = SelectTrapFormation(available, 3);
                    break;
                    
                default:
                    group.Positions = SelectRandomPositions(available, 3);
                    break;
            }
            
            if (group.Positions.Count < 3)
            {
                // Fallback на случайный выбор
                group.Positions = SelectRandomPositions(available, 3);
            }
            
            // Устанавливаем группу для каждой позиции
            foreach (var pos in group.Positions)
            {
                pos.TripletGroup = groupId;
            }
            
            // Вычисляем слои доступности
            group.MinAccessLayer = group.Positions.Min(p => p.AccessibilityLayer);
            group.MaxAccessLayer = group.Positions.Max(p => p.AccessibilityLayer);
            
            return group;
        }

        private List<TilePosition> SelectSameLayerPositions(List<TilePosition> available, int count)
        {
            var layerGroups = available.GroupBy(p => p.AccessibilityLayer)
                .Where(g => g.Count() >= count)
                .OrderBy(g => g.Key)
                .ToList();
            
            if (!layerGroups.Any()) return new List<TilePosition>();
            
            // Выбираем слой в зависимости от сложности
            var targetLayer = _difficulty > 0.5f 
                ? layerGroups.Last()  // Для сложности берем глубокие слои
                : layerGroups.First(); // Для легкости берем поверхностные
            
            return targetLayer.OrderBy(_ => _random.Next()).Take(count).ToList();
        }

        private List<TilePosition> SelectNearbyLayerPositions(List<TilePosition> available, int count)
        {
            if (available.Count < count) return new List<TilePosition>();
            
            var selected = new List<TilePosition>();
            var first = available[_random.Next(available.Count)];
            selected.Add(first);
            
            var targetLayer = first.AccessibilityLayer;
            var remaining = available.Where(p => p != first && 
                Math.Abs(p.AccessibilityLayer - targetLayer) <= 2)
                .OrderBy(_ => _random.Next())
                .Take(count - 1);
            
            selected.AddRange(remaining);
            
            // Добавляем случайные, если не хватило
            if (selected.Count < count)
            {
                var toAdd = available.Except(selected)
                    .OrderBy(_ => _random.Next())
                    .Take(count - selected.Count);
                selected.AddRange(toAdd);
            }
            
            return selected;
        }

        private List<TilePosition> SelectBlockingChain(List<TilePosition> available, int count)
        {
            var selected = new List<TilePosition>();
            
            // Находим позицию с максимальным количеством блокируемых
            var start = available.OrderByDescending(p => p.Blocking.Count)
                .ThenBy(_ => _random.Next())
                .FirstOrDefault();
            
            if (start == null) return new List<TilePosition>();
            
            selected.Add(start);
            
            // Добавляем позиции, которые блокируют предыдущую
            for (var i = 1; i < count && i < available.Count; i++)
            {
                var lastPos = selected.Last();
                var blocker = available.Where(p => !selected.Contains(p) && 
                    p.Blocking.Contains(lastPos.Position))
                    .OrderBy(_ => _random.Next())
                    .FirstOrDefault();
                
                if (blocker != null)
                {
                    selected.Add(blocker);
                }
                else
                {
                    // Если не можем продолжить цепь, берем из того же слоя
                    var sameLayer = available.Where(p => !selected.Contains(p) &&
                        p.AccessibilityLayer == lastPos.AccessibilityLayer)
                        .OrderBy(_ => _random.Next())
                        .FirstOrDefault();
                    
                    if (sameLayer != null)
                        selected.Add(sameLayer);
                    else
                        break;
                }
            }
            
            // Добавляем случайные, если не хватило
            while (selected.Count < count && selected.Count < available.Count)
            {
                var toAdd = available.Except(selected)
                    .OrderBy(_ => _random.Next())
                    .First();
                selected.Add(toAdd);
            }
            
            return selected;
        }

        private List<TilePosition> SelectDistributedPositions(List<TilePosition> available, int count)
        {
            if (available.Count < count) return new List<TilePosition>();
            
            var selected = new List<TilePosition>();
            var layers = available.Select(p => p.AccessibilityLayer).Distinct().OrderBy(l => l).ToList();
            
            if (layers.Count >= count)
            {
                // Берем по одному с каждого слоя
                var selectedLayers = layers.OrderBy(_ => _random.Next()).Take(count);
                foreach (var layer in selectedLayers)
                {
                    var pos = available.Where(p => p.AccessibilityLayer == layer && !selected.Contains(p))
                        .OrderBy(_ => _random.Next())
                        .FirstOrDefault();
                    if (pos != null)
                        selected.Add(pos);
                }
            }
            else
            {
                // Распределяем равномерно
                foreach (var layer in layers)
                {
                    if (selected.Count >= count) break;
                    var positions = available.Where(p => p.AccessibilityLayer == layer && !selected.Contains(p))
                        .OrderBy(_ => _random.Next())
                        .Take(Math.Max(1, count / layers.Count));
                    selected.AddRange(positions);
                }
            }
            
            // Добавляем случайные, если не хватило
            while (selected.Count < count && selected.Count < available.Count)
            {
                var toAdd = available.Except(selected)
                    .OrderBy(_ => _random.Next())
                    .First();
                selected.Add(toAdd);
            }
            
            return selected;
        }

        private List<TilePosition> SelectDeepNestedPositions(List<TilePosition> available, int count)
        {
            // Выбираем позиции с максимальным количеством блокировок
            return available.OrderByDescending(p => p.BlockedBy.Count)
                .ThenByDescending(p => p.AccessibilityLayer)
                .Take(count)
                .ToList();
        }

        private List<TilePosition> SelectMaximalInterdependence(List<TilePosition> available, int count)
        {
            if (available.Count < count) return new List<TilePosition>();
            
            var selected = new List<TilePosition>();
            
            // Выбираем первую позицию из середины по доступности
            var midLayer = available.Select(p => p.AccessibilityLayer).Distinct().OrderBy(l => l).ToList();
            var targetLayer = midLayer[midLayer.Count / 2];
            
            var first = available.Where(p => p.AccessibilityLayer == targetLayer)
                .OrderBy(_ => _random.Next())
                .FirstOrDefault();
            
            if (first == null) return SelectRandomPositions(available, count);
            
            selected.Add(first);
            
            // Выбираем позиции, которые максимально связаны с уже выбранными
            for (var i = 1; i < count; i++)
            {
                var best = available.Where(p => !selected.Contains(p))
                    .Select(p => new
                    {
                        Position = p,
                        Interdependence = CalculateInterdependence(p, selected)
                    })
                    .OrderByDescending(x => x.Interdependence)
                    .ThenBy(_ => _random.Next())
                    .FirstOrDefault();
                
                if (best != null)
                    selected.Add(best.Position);
            }
            
            return selected;
        }

        private int CalculateInterdependence(TilePosition pos, List<TilePosition> selected)
        {
            var score = 0;
            
            foreach (var sel in selected)
            {
                // Проверяем прямые блокировки
                if (pos.BlockedBy.Contains(sel.Position)) score += 2;
                if (pos.Blocking.Contains(sel.Position)) score += 2;
                
                // Проверяем общие блокировки
                score += pos.BlockedBy.Intersect(sel.BlockedBy).Count();
                score += pos.Blocking.Intersect(sel.Blocking).Count();
                
                // Штраф за один слой доступности (слишком просто)
                if (pos.AccessibilityLayer == sel.AccessibilityLayer) score -= 1;
            }
            
            return score;
        }

        private List<TilePosition> SelectTrapFormation(List<TilePosition> available, int count)
        {
            if (available.Count < count) return new List<TilePosition>();
            
            var selected = new List<TilePosition>();
            
            // Выбираем две легкодоступные позиции (приманка)
            var easyAccess = available.Where(p => p.AccessibilityLayer <= 1)
                .OrderBy(_ => _random.Next())
                .Take(2)
                .ToList();
            
            selected.AddRange(easyAccess);
            
            // Третью позицию выбираем максимально заблокированную
            var hardAccess = available.Where(p => !selected.Contains(p))
                .OrderByDescending(p => p.AccessibilityLayer)
                .ThenByDescending(p => p.BlockedBy.Count)
                .FirstOrDefault();
            
            if (hardAccess != null)
                selected.Add(hardAccess);
            
            // Добавляем случайные, если не хватило
            while (selected.Count < count && selected.Count < available.Count)
            {
                var toAdd = available.Except(selected)
                    .OrderBy(_ => _random.Next())
                    .First();
                selected.Add(toAdd);
            }
            
            return selected;
        }

        private List<TilePosition> SelectRandomPositions(List<TilePosition> available, int count)
        {
            return available.OrderBy(_ => _random.Next()).Take(count).ToList();
        }

        private void AnalyzeGroupDependencies(List<TripletGroup> groups, 
            Dictionary<Vector3Int, TilePosition> positions)
        {
            for (var i = 0; i < groups.Count; i++)
            {
                var group = groups[i];
                
                // Находим все группы, которые блокируют текущую
                foreach (var pos in group.Positions)
                {
                    foreach (var blockerPos in pos.BlockedBy)
                    {
                        if (positions.TryGetValue(blockerPos, out var blocker))
                        {
                            if (blocker.TripletGroup >= 0 && blocker.TripletGroup != i)
                            {
                                group.DependsOn.Add(blocker.TripletGroup);
                            }
                        }
                    }
                }
                
                // Устанавливаем приоритет на основе зависимостей и слоев
                group.Priority = group.DependsOn.Count * 10 + group.MaxAccessLayer;
            }
        }

        private void AssignTypesToGroups(List<TripletGroup> groups)
        {
            // Сортируем группы по приоритету (сложные размещаем первыми)
            groups.Sort((a, b) => b.Priority.CompareTo(a.Priority));
            
            var typeDistribution = new Dictionary<TileType, int>();
            foreach (var type in _typesAvailable)
            {
                typeDistribution[type] = 0;
            }
            
            // На высокой сложности создаем больше конфликтов типов
            var conflictProbability = _difficulty * 0.5f;
            
            foreach (var group in groups)
            {
                TileType selectedType;
                
                if (_random.NextDouble() < conflictProbability && group.DependsOn.Any())
                {
                    // Пытаемся выбрать тип, который уже есть в зависимостях
                    var dependentGroups = groups.Where(g => group.DependsOn.Contains(groups.IndexOf(g)))
                        .Where(_ => true)
                        .ToList();
                    
                    if (dependentGroups.Any())
                    {
                        // Выбираем конфликтующий тип
                        selectedType = dependentGroups[_random.Next(dependentGroups.Count)].Type;
                    }
                    else
                    {
                        // Выбираем наименее использованный тип
                        selectedType = typeDistribution.OrderBy(kvp => kvp.Value)
                            .ThenBy(_ => _random.Next())
                            .First().Key;
                    }
                }
                else
                {
                    // Выбираем наименее использованный тип для баланса
                    selectedType = typeDistribution.OrderBy(kvp => kvp.Value)
                        .ThenBy(_ => _random.Next())
                        .First().Key;
                }
                
                group.Type = selectedType;
                typeDistribution[selectedType] += 3;
                
                // Назначаем тип позициям
                foreach (var pos in group.Positions)
                {
                    pos.AssignedType = selectedType;
                }
            }
            
            // Проверка решаемости на высокой сложности
            if (_difficulty > 0.8f)
            {
                ValidateAndAdjustSolvability(groups);
            }
        }

        private void ValidateAndAdjustSolvability(List<TripletGroup> groups)
        {
            // Симулируем прохождение уровня
            var simulation = SimulateSolution(groups);
            
            if (!simulation.IsSolvable)
            {
                // Корректируем типы, чтобы гарантировать хотя бы одно решение
                AdjustTypesForSolvability(groups, simulation);
            }
            else if (simulation.SolutionPaths.Count > 3 && _difficulty > 0.9f)
            {
                // На максимальной сложности уменьшаем количество решений
                ReduceSolutionPaths(groups, simulation);
            }
        }

        private class SimulationResult
        {
            public bool IsSolvable { get; set; }
            public List<List<int>> SolutionPaths { get; set; } = new();
            public HashSet<int> ProblematicGroups { get; set; } = new();
        }

        private SimulationResult SimulateSolution(List<TripletGroup> groups)
        {
            var result = new SimulationResult();
            
            // Упрощенная симуляция - проверяем базовую решаемость
            var availableGroups = new HashSet<int>();
            var removedGroups = new HashSet<int>();
            
            // Находим изначально доступные группы
            for (var i = 0; i < groups.Count; i++)
            {
                if (groups[i].MinAccessLayer == 0)
                {
                    availableGroups.Add(i);
                }
            }
            
            var bufferSlots = 7; // Стандартный размер буфера
            var currentPath = new List<int>();
            
            while (availableGroups.Count > 0 || removedGroups.Count < groups.Count)
            {
                var canRemove = false;
                
                // Проверяем, можем ли убрать какую-то группу
                foreach (var groupIndex in availableGroups)
                {
                    if (removedGroups.Contains(groupIndex)) continue;
                    
                    // Симулируем, поместится ли в буфер
                    if (SimulateBufferFit(groups, groupIndex, removedGroups, bufferSlots))
                    {
                        removedGroups.Add(groupIndex);
                        currentPath.Add(groupIndex);
                        canRemove = true;
                        
                        // Обновляем доступные группы
                        for (var i = 0; i < groups.Count; i++)
                        {
                            if (!removedGroups.Contains(i) && 
                                groups[i].DependsOn.All(dep => removedGroups.Contains(dep)))
                            {
                                availableGroups.Add(i);
                            }
                        }
                        
                        break;
                    }
                }
                
                if (!canRemove)
                {
                    // Не можем продолжить - уровень может быть нерешаемым
                    result.ProblematicGroups = new HashSet<int>(availableGroups);
                    break;
                }
            }
            
            result.IsSolvable = removedGroups.Count == groups.Count;
            if (result.IsSolvable)
            {
                result.SolutionPaths.Add(currentPath);
            }
            
            return result;
        }

        private bool SimulateBufferFit(List<TripletGroup> groups, int targetGroup, 
            HashSet<int> removedGroups, int bufferSize)
        {
            // Упрощенная проверка - считаем, что если группа доступна, её можно убрать
            // В реальной игре нужно учитывать текущее состояние буфера
            return groups[targetGroup].DependsOn.All(dep => removedGroups.Contains(dep));
        }

        private void AdjustTypesForSolvability(List<TripletGroup> groups, SimulationResult simulation)
        {
            // Находим проблемные группы и меняем их типы
            foreach (var problematicGroup in simulation.ProblematicGroups)
            {
                var group = groups[problematicGroup];
                
                // Меняем тип на менее конфликтный
                var typeCount = new Dictionary<TileType, int>();
                foreach (var type in _typesAvailable)
                {
                    typeCount[type] = groups.Count(g => g.Type == type);
                }
                
                var newType = typeCount.OrderBy(kvp => kvp.Value).First().Key;
                group.Type = newType;
                
                foreach (var pos in group.Positions)
                {
                    pos.AssignedType = newType;
                }
            }
        }

        private void ReduceSolutionPaths(List<TripletGroup> groups, SimulationResult simulation)
        {
            // Увеличиваем зависимости между группами
            var accessibleGroups = groups.Where(g => g.MinAccessLayer <= 1).ToList();
            
            foreach (var group in accessibleGroups.Skip(1)) // Оставляем хотя бы одну доступную
            {
                // Добавляем искусственную зависимость
                var dependency = accessibleGroups.First();
                group.DependsOn.Add(groups.IndexOf(dependency));
                group.MinAccessLayer = Math.Max(group.MinAccessLayer, dependency.MaxAccessLayer + 1);
            }
        }

        private void PlaceTilesOnField(Field field, Dictionary<Vector3Int, TilePosition> positions)
        {
            // Размещаем тайлы от верхних слоев к нижним
            var sortedPositions = positions.Values
                .OrderByDescending(p => p.Position.z)
                .ThenBy(p => p.Position.y)
                .ThenBy(p => p.Position.x);
            
            foreach (var tilePos in sortedPositions)
            {
                if (tilePos.AssignedType == null) continue;
                
                var tile = new Tile
                {
                    type = tilePos.AssignedType.Value,
                    gridPosition = tilePos.Position
                };
                
                if (!field.PlaceTile(tile))
                {
                    throw new Exception($"Failed to place tile at {tilePos.Position}");
                }
            }
        }
    }
}