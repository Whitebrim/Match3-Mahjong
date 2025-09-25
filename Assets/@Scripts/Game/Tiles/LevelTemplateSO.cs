using UnityEngine;
using Sirenix.OdinInspector;

namespace Game.Tiles
{
    [CreateAssetMenu(fileName = "LevelTemplate", menuName = "Scriptable Objects/Level Template")]
    public class LevelTemplateSO : SerializedScriptableObject
    {
        [HideInInspector]
        public bool[,,] Field;

        #region Level Dimensions
        
        [PropertyOrder(0)]
        [TitleGroup("Level Dimensions")]
        [HorizontalGroup("Level Dimensions/Dimensions")]
        [LabelWidth(50)]
        [MinValue(1), MaxValue(25)]
        public int width = 13;
        
        [PropertyOrder(0)]
        [HorizontalGroup("Level Dimensions/Dimensions")]
        [LabelWidth(50)]
        [MinValue(1), MaxValue(25)]
        public int height = 17;
        
        [PropertyOrder(0)]
        [HorizontalGroup("Level Dimensions/Dimensions")]
        [LabelWidth(50)]
        [MinValue(1), MaxValue(20)]
        public int layers = 5;
        private int LayersMaxIndex => layers - 1;
        
        #endregion

#if UNITY_EDITOR
        
        #region Layer Navigation
        
        [PropertyOrder(1)]
        [TitleGroup("Layer Navigation")]
        [PropertyRange(0, "LayersMaxIndex")]
        [OnValueChanged("OnLayerChanged")]
        [InfoBox("Select which layer to edit")]
        public int currentLayer = 0;
        
        [PropertyOrder(1)]
        [HorizontalGroup("Layer Navigation/LayerNav")]
        [Button("◄ Previous Layer", ButtonSizes.Medium)]
        [EnableIf("@currentLayer > 0")]
        private void PreviousLayer()
        {
            currentLayer = Mathf.Max(0, currentLayer - 1);
        }
        
        [PropertyOrder(1)]
        [HorizontalGroup("Layer Navigation/LayerNav")]
        [Button("Next Layer ►", ButtonSizes.Medium)]
        [EnableIf("@currentLayer < LayersMaxIndex")]
        private void NextLayer()
        {
            currentLayer = Mathf.Min(LayersMaxIndex, currentLayer + 1);
        }
        
        [PropertyOrder(2)]
        [HorizontalGroup("Layer Navigation/LayerNav")]
        [ShowInInspector]
        [DisplayAsString]
        [LabelText("Current")]
        private string LayerDisplay => $"Layer {currentLayer + 1} / {layers}";
        
        private void OnLayerChanged()
        {
            currentLayer = Mathf.Clamp(currentLayer, 0, Mathf.Max(0, LayersMaxIndex));
        }
        
        #endregion

        #region Grid

        [PropertyOrder(2)]
        [OnInspectorGUI]
        private void DrawGridButtons()
        {
            if (Field == null)
            {
                Resize();
                return;
            }
            
            GUILayout.Space(10);
            
            GUILayout.BeginVertical();
            
            for (int y = height - 1; y >= 0; y--)
            {
                GUILayout.BeginHorizontal();
                
                for (int x = 0; x < width; x++)
                {
                    bool isEnabled = Field[x, y, currentLayer];
                    bool isBlocked = IsBlockedByNeighbors(x, y, currentLayer);
                    
                    GUI.enabled = !isBlocked;
                    
                    if (isEnabled)
                    {
                        GUI.backgroundColor = Color.green;
                    }
                    else if (isBlocked)
                    {
                        GUI.backgroundColor = Color.gray;
                    }
                    else
                    {
                        GUI.backgroundColor = Color.white;
                    }
                    
                    string buttonText = isEnabled ? "■" : "□";
                    if (isBlocked && !isEnabled)
                    {
                        buttonText = "×";
                    }
                    
                    if (GUILayout.Button(buttonText, GUILayout.Width(30), GUILayout.Height(30)))
                    {
                        ToggleCell(x, y, currentLayer);
                    }
                }
                
                GUILayout.EndHorizontal();
            }
            
            GUILayout.EndVertical();
            
            GUI.enabled = true;
            GUI.backgroundColor = Color.white;
        }
        
        private void ToggleCell(int x, int y, int layer)
        {
            if (Field == null) return;
            
            Field[x, y, layer] = !Field[x, y, layer];
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        private bool IsBlockedByNeighbors(int x, int y, int layer)
        {
            if (Field == null) return false;
            
            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0) continue;
                    
                    int nx = x + dx;
                    int ny = y + dy;
                    
                    if (nx >= 0 && nx < width && ny >= 0 && ny < height)
                    {
                        if (Field[nx, ny, layer])
                        {
                            return true;
                        }
                    }
                }
            }
            
            return false;
        }

        #endregion

        #region Statistics

        [PropertyOrder(3)]
        [ShowInInspector]
        [DisplayAsString(false)]
        [BoxGroup("Statistics")]
        [PropertySpace(SpaceBefore = 5)]
        private string Info
        {
            get
            {
                if (Field == null) return "No data";
                
                _totalTiles = 0;
                int[] tilesPerLayer = new int[layers];
                
                for (int z = 0; z < layers; z++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        for (int y = 0; y < height; y++)
                        {
                            if (Field[x, y, z])
                            {
                                _totalTiles++;
                                tilesPerLayer[z]++;
                            }
                        }
                    }
                }
                
                string stats = $"Total Tiles: {_totalTiles}\n";
                for (int i = 0; i < layers; i++)
                {
                    stats += $"Layer {i}: {tilesPerLayer[i]} tiles\n";
                }
                stats = stats.Trim('\n');
                
                return stats;
            }
        }
        private int _totalTiles;
        
        [PropertyOrder(3)]
        [ShowInInspector]
        [DisplayAsString(false, TextAlignment.Left, enableRichText:true)]
        [BoxGroup("Statistics")]
        [PropertySpace(SpaceAfter = 5)]
        private string Validation
        {
            get
            {
                if (_totalTiles % 3 == 0)
                    return "<color=green>Ok</color>";
                else
                    return "<color=red>Need " + (3 - _totalTiles % 3) + " more</color>";
            }
        }

        #endregion

        #region Actions

        [PropertyOrder(4)]
        [TitleGroup("Actions")]
        [ButtonGroup("Actions/Buttons")]
        [Button(ButtonSizes.Large)]
        [GUIColor(0.3f, 0.8f, 0.6f)]
        [EnableIf("@currentLayer > 0")]
        private void CopyLowerLayer()
        {
            if (!UnityEditor.EditorUtility.DisplayDialog("Copy Lower Layer",
                    "This will overwrite the current level. Continue?",
                    "Yes", "No")) 
                return;
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Field[x, y, currentLayer] = Field[x, y, currentLayer - 1];
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        [PropertyOrder(4)]
        [ButtonGroup("Actions/Buttons")]
        [Button(ButtonSizes.Large)]
        [GUIColor(0.6f, 0.8f, 0.3f)]
        [EnableIf("@currentLayer < LayersMaxIndex")]
        private void CopyUpperLayer()
        {
            if (!UnityEditor.EditorUtility.DisplayDialog("Copy Upper Layer",
                    "This will overwrite the current level. Continue?",
                    "Yes", "No")) 
                return;
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Field[x, y, currentLayer] = Field[x, y, currentLayer + 1];
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        [PropertyOrder(4)]
        [ButtonGroup("Actions/Buttons")]
        [Button("Reverse Layers Order", ButtonSizes.Large)]
        [GUIColor(0.8f, 0.6f, 0.4f)]
        private void ReverseLayersOrder(bool prompt = true)
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (Field == null || layers <= 1) return;
    
            if (prompt && !UnityEditor.EditorUtility.DisplayDialog("Reverse Layers Order",
                    "This will reverse the order of all layers. Continue?",
                    "Yes", "No")) 
                return;
            
            bool[,,] reversedField = new bool[width, height, layers];
            
            for (int layer = 0; layer < layers; layer++)
            {
                int reversedLayer = layers - 1 - layer;
        
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        reversedField[x, y, reversedLayer] = Field[x, y, layer];
                    }
                }
            }
            
            Field = reversedField;
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        [PropertyOrder(4)]
        [ButtonGroup("Actions/Buttons")]
        [Button(ButtonSizes.Large)]
        [GUIColor(0.8f, 0.3f, 0.3f)]
        [EnableIf("@layers > 1")]
        private void DeleteLayer()
        {
   
            if (!UnityEditor.EditorUtility.DisplayDialog("Delete Layer",
                    $"Are you sure you want to delete Layer {currentLayer + 1}?\nThis action cannot be undone.",
                    "Yes", "No")) 
                return;
            
            bool[,,] newField = new bool[width, height, layers - 1];
            
            int newLayerIndex = 0;
            for (int oldLayerIndex = 0; oldLayerIndex < layers; oldLayerIndex++)
            {
                if (oldLayerIndex == currentLayer)
                    continue;
                
                for (int x = 0; x < width; x++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        newField[x, y, newLayerIndex] = Field[x, y, oldLayerIndex];
                    }
                }
        
                newLayerIndex++;
            }
            
            Field = newField;
            layers--;
            
            if (currentLayer >= layers)
            {
                currentLayer = layers - 1;
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        [PropertyOrder(4)]
        [ButtonGroup("Actions/Buttons")]
        [Button(ButtonSizes.Large)]
        [GUIColor(0.4f, 0.6f, 0.8f)]
        private void ClearCurrentLayer()
        {
            if (Field == null) return;
            if (!UnityEditor.EditorUtility.DisplayDialog("Clear Current Layer",
                    "Are you sure you want to clear current layer?",
                    "Yes", "No")) 
                return;
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Field[x, y, currentLayer] = false;
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        [PropertyOrder(4)]
        [ButtonGroup("Actions/Buttons")]
        [Button(ButtonSizes.Large)]
        [GUIColor(0.8f, 0.4f, 0.4f)]
        private void Clear()
        {
            if (!UnityEditor.EditorUtility.DisplayDialog("Clear Level",
                    "Are you sure you want to clear all tiles?",
                    "Yes", "No")) 
                return;
            
            Field = new bool[width, height, layers];
            
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [PropertyOrder(4)]
        [TitleGroup("Actions")]
        [PropertySpace(SpaceBefore = 10)]
        [InfoBox("Move all tiles on current layer")]
        [OnInspectorGUI]
        private void DrawDPad()
        {
            GUILayout.Space(5);
            
            // Центрируем D-pad
            GUILayout.BeginHorizontal();
            //GUILayout.FlexibleSpace();
            
            GUILayout.BeginVertical();
            
            // Верхняя кнопка
            GUILayout.BeginHorizontal();
            GUILayout.Space(30);
            
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("▲", GUILayout.Width(40), GUILayout.Height(30)))
            {
                MoveTilesUp();
            }
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            // Средний ряд с левой и правой кнопками
            GUILayout.BeginHorizontal();
            
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("◄", GUILayout.Width(40), GUILayout.Height(30)))
            {
                MoveTilesLeft();
            }
            
            GUILayout.Space(11); // Промежуток между кнопками
            
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("►", GUILayout.Width(40), GUILayout.Height(30)))
            {
                MoveTilesRight();
            }
            
            GUILayout.EndHorizontal();
            
            // Нижняя кнопка
            GUILayout.BeginHorizontal();
            GUILayout.Space(30);
            
            GUI.backgroundColor = Color.cyan;
            if (GUILayout.Button("▼", GUILayout.Width(40), GUILayout.Height(30)))
            {
                MoveTilesDown();
            }
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUILayout.EndVertical();
            
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();
            
            GUI.backgroundColor = Color.white;
            
            GUILayout.Space(5);
        }

        private void MoveTilesUp()
        {
            if (Field == null) return;
            
            bool[,] tempLayer = new bool[width, height];
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (Field[x, y, currentLayer])
                    {
                        int newY = y + 1;
                        if (newY < height)
                        {
                            tempLayer[x, newY] = true;
                        }
                    }
                }
            }
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Field[x, y, currentLayer] = tempLayer[x, y];
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void MoveTilesDown()
        {
            if (Field == null) return;
            
            bool[,] tempLayer = new bool[width, height];
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (Field[x, y, currentLayer])
                    {
                        int newY = y - 1;
                        if (newY >= 0)
                        {
                            tempLayer[x, newY] = true;
                        }
                    }
                }
            }
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Field[x, y, currentLayer] = tempLayer[x, y];
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void MoveTilesLeft()
        {
            if (Field == null) return;
            
            bool[,] tempLayer = new bool[width, height];
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (Field[x, y, currentLayer])
                    {
                        int newX = x - 1;
                        if (newX >= 0)
                        {
                            tempLayer[newX, y] = true;
                        }
                    }
                }
            }
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Field[x, y, currentLayer] = tempLayer[x, y];
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void MoveTilesRight()
        {
            if (Field == null) return;
            
            bool[,] tempLayer = new bool[width, height];
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    if (Field[x, y, currentLayer])
                    {
                        int newX = x + 1;
                        if (newX < width)
                        {
                            tempLayer[newX, y] = true;
                        }
                    }
                }
            }
            
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Field[x, y, currentLayer] = tempLayer[x, y];
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        #endregion

        #region Templates

        [PropertyOrder(5)]
        [TitleGroup("Templates")]
        [ButtonGroup("Templates/Buttons")]
        [Button("Pyramid", ButtonSizes.Medium)]
        private void GeneratePyramid()
        {
            if (!UnityEditor.EditorUtility.DisplayDialog("Generate Pyramid",
                    "This will overwrite the current level. Continue?", "Yes", "No")) return;
            
            Field = new bool[width, height, layers];
                
            for (int layer = 0; layer < layers; layer++)
            {
                int margin = layer;
                int startX = margin;
                int endX = width - margin;
                int startY = margin;
                int endY = height - margin;
                    
                for (int x = startX; x < endX; x += 2)
                {
                    for (int y = startY; y < endY; y += 2)
                    {
                        if (x < width && y < height)
                        {
                            Field[x, y, layer] = true;
                        }
                    }
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        [PropertyOrder(5)]
        [ButtonGroup("Templates/Buttons")]
        [Button("Diamond", ButtonSizes.Medium)]
        private void GenerateDiamond()
        {
            if (!UnityEditor.EditorUtility.DisplayDialog("Generate Diamond",
                    "This will overwrite the current level. Continue?", "Yes", "No")) return;
            
            Field = new bool[width, height, layers];
                
            int centerX = width / 2;
            int centerY = height / 2;
            int maxRadius = Mathf.Min(width, height) / 2;
                
            for (int layer = 0; layer < layers; layer++)
            {
                float layerScale = 1f - (layer / (float)layers * 0.5f);
                int radius = Mathf.RoundToInt(maxRadius * layerScale);
                    
                for (int x = 0; x < width; x += 2)
                {
                    for (int y = 0; y < height; y += 2)
                    {
                        float distance = Mathf.Abs(x - centerX) + Mathf.Abs(y - centerY);
                        if (distance <= radius * 2)
                        {
                            Field[x, y, layer] = true;
                        }
                    }
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        [PropertyOrder(5)]
        [ButtonGroup("Templates/Buttons")]
        [Button("Fill", ButtonSizes.Medium)]
        private void GenerateFill()
        {
            if (!UnityEditor.EditorUtility.DisplayDialog("Generate Fill",
                    "This will overwrite the current level. Continue?", "Yes", "No")) return;
            
            Field = new bool[width, height, layers];
        
            for (int layer = 0; layer < layers; layer++)
            {
                for (int x = 0; x < width; x += 2)
                {
                    for (int y = 0; y < height; y += 2)
                    {
                        Field[x, y, layer] = true;
                    }
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        [PropertyOrder(5)]
        [ButtonGroup("Templates/Buttons")]
        [Button("Classic Mahjong", ButtonSizes.Medium)]
        private void GenerateClassicMahjong()
        {
            if (!UnityEditor.EditorUtility.DisplayDialog("Generate Classic Mahjong",
                    "This will overwrite the current level. Continue?", "Yes", "No")) return;
            
            Field = new bool[width, height, layers];
            
            int baseWidth = Mathf.Min(13, width);
            int baseHeight = Mathf.Min(9, height);
                
            for (int layer = 0; layer < layers; layer++)
            {
                int layerWidth = baseWidth - layer * 2;
                int layerHeight = baseHeight - layer;
                    
                if (layerWidth < 3 || layerHeight < 3) break;
                    
                int offsetX = (width - layerWidth) / 2;
                int offsetY = (height - layerHeight) / 2;
                
                for (int x = 0; x < layerWidth; x += 2)
                {
                    for (int y = 0; y < layerHeight; y += 2)
                    {
                        int posX = offsetX + x;
                        int posY = offsetY + y;
                            
                        if (posX < width && posY < height)
                        {
                            Field[posX, posY, layer] = true;
                        }
                    }
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        [PropertyOrder(5)]
        [ButtonGroup("Templates/Buttons")]
        [Button("Hourglass", ButtonSizes.Medium)]
        private void GenerateHourglass()
        {
            if (!UnityEditor.EditorUtility.DisplayDialog("Generate Hourglass",
                    "This will overwrite the current level. Continue?", "Yes", "No")) return;
            
            Field = new bool[width, height, layers];
                
            int centerX = width / 2;
            int maxWidth = Mathf.Min(width, height) - 2;
                
            for (int layer = 0; layer < layers; layer++)
            {
                int layerWidth = maxWidth - layer * 2;
                if (layerWidth < 3) break;
                    
                for (int y = 0; y < height; y += 2)
                {
                    int currentWidth = layerWidth - Mathf.Abs(y - height/2) * layerWidth / height;
                    currentWidth = Mathf.Max(1, currentWidth);
                        
                    int startX = centerX - currentWidth;
                    int endX = centerX + currentWidth;
                        
                    for (int x = startX; x <= endX; x += 2)
                    {
                        if (x >= 0 && x < width && y >= 0)
                        {
                            Field[x, y, layer] = true;
                        }
                    }
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        [PropertyOrder(5)]
        [ButtonGroup("Templates/Buttons2")]
        [Button("Cross", ButtonSizes.Medium)]
        private void GenerateCross()
        {
            if (!UnityEditor.EditorUtility.DisplayDialog("Generate Cross",
                    "This will overwrite the current level. Continue?", "Yes", "No")) return;
            
            Field = new bool[width, height, layers];
                
            int centerX = width / 2;
            int centerY = height / 2;
                
            for (int layer = 0; layer < layers; layer++)
            {
                int thickness = Mathf.Max(1, 3 - layer);
                
                for (int x = 2; x < width - 2; x += 2)
                {
                    for (int dy = -thickness; dy <= thickness; dy++)
                    {
                        int y = centerY + dy;
                        if (y >= 0 && y < height && y % 2 == 0)
                        {
                            Field[x, y, layer] = true;
                        }
                    }
                }
                
                for (int y = 2; y < height - 2; y += 2)
                {
                    for (int dx = -thickness; dx <= thickness; dx++)
                    {
                        int x = centerX + dx;
                        if (x >= 0 && x < width && x % 2 == 0)
                        {
                            Field[x, y, layer] = true;
                        }
                    }
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [PropertyOrder(5)]
        [ButtonGroup("Templates/Buttons2")]
        [Button("Circle", ButtonSizes.Medium)]
        private void GenerateCircle()
        {
            if (!UnityEditor.EditorUtility.DisplayDialog("Generate Circle",
                    "This will overwrite the current level. Continue?", "Yes", "No")) return;
            
            Field = new bool[width, height, layers];
                
            int centerX = width / 2;
            int centerY = height / 2;
            float maxRadius = Mathf.Min(width, height) / 2.5f;
                
            for (int layer = 0; layer < layers; layer++)
            {
                float layerRadius = maxRadius - layer * 1.5f;
                if (layerRadius <= 0) break;
                    
                for (int x = 0; x < width; x += 2)
                {
                    for (int y = 0; y < height; y += 2)
                    {
                        float distance = Mathf.Sqrt((x - centerX) * (x - centerX) + (y - centerY) * (y - centerY));
                        if (distance <= layerRadius && distance >= layerRadius - 2)
                        {
                            Field[x, y, layer] = true;
                        }
                    }
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [PropertyOrder(5)]
        [ButtonGroup("Templates/Buttons2")]
        [Button("Star", ButtonSizes.Medium)]
        private void GenerateStar()
        {
            if (!UnityEditor.EditorUtility.DisplayDialog("Generate Star",
                    "This will overwrite the current level. Continue?", "Yes", "No")) return;
            
            Field = new bool[width, height, layers];
                
            int centerX = width / 2;
            int centerY = height / 2;
                
            for (int layer = 0; layer < layers; layer++)
            {
                float scale = 1f - layer * 0.2f;
                int radius = Mathf.RoundToInt(Mathf.Min(width, height) * 0.35f * scale);
                    
                for (int angle = 0; angle < 360; angle += 36)
                {
                    bool isOuter = (angle / 36) % 2 == 0;
                    float currentRadius = isOuter ? radius : radius * 0.5f;
                        
                    float rad = angle * Mathf.Deg2Rad;
                        
                    for (float r = 0; r <= currentRadius; r += 1f)
                    {
                        int x = centerX + Mathf.RoundToInt(Mathf.Cos(rad) * r);
                        int y = centerY + Mathf.RoundToInt(Mathf.Sin(rad) * r);
                            
                        x = (x / 2) * 2;
                        y = (y / 2) * 2;
                            
                        if (x >= 0 && x < width && y >= 0 && y < height)
                        {
                            Field[x, y, layer] = true;
                        }
                    }
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [PropertyOrder(5)]
        [ButtonGroup("Templates/Buttons2")]
        [Button("Flower", ButtonSizes.Medium)]
        private void GenerateFlower()
        {
            if (!UnityEditor.EditorUtility.DisplayDialog("Generate Flower",
                    "This will overwrite the current level. Continue?", "Yes", "No")) return;
            
            Field = new bool[width, height, layers];
                
            int centerX = width / 2;
            int centerY = height / 2;
                
            for (int layer = 0; layer < layers; layer++)
            {
                float scale = 1f - layer * 0.15f;
                    
                for (int dx = -1; dx <= 1; dx++)
                {
                    for (int dy = -1; dy <= 1; dy++)
                    {
                        int x = centerX + dx * 2;
                        int y = centerY + dy * 2;
                        if (x >= 0 && x < width && y >= 0 && y < height)
                        {
                            Field[x, y, layer] = true;
                        }
                    }
                }
                    
                for (int petal = 0; petal < 6; petal++)
                {
                    float angle = petal * 60f * Mathf.Deg2Rad;
                    int petalLength = Mathf.RoundToInt(4 * scale);
                        
                    for (int i = 1; i <= petalLength; i++)
                    {
                        int x = centerX + Mathf.RoundToInt(Mathf.Cos(angle) * i * 2);
                        int y = centerY + Mathf.RoundToInt(Mathf.Sin(angle) * i * 2);
                            
                        x = (x / 2) * 2;
                        y = (y / 2) * 2;
                            
                        if (x >= 0 && x < width && y >= 0 && y < height)
                        {
                            Field[x, y, layer] = true;
                                
                            if (i > 1 && i < petalLength)
                            {
                                for (int side = -1; side <= 1; side += 2)
                                {
                                    float sideAngle = angle + side * 30f * Mathf.Deg2Rad;
                                    int sx = x + Mathf.RoundToInt(Mathf.Cos(sideAngle) * 2);
                                    int sy = y + Mathf.RoundToInt(Mathf.Sin(sideAngle) * 2);
                                        
                                    sx = (sx / 2) * 2;
                                    sy = (sy / 2) * 2;
                                        
                                    if (sx >= 0 && sx < width && sy >= 0 && sy < height)
                                    {
                                        Field[sx, sy, layer] = true;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [PropertyOrder(5)]
        [ButtonGroup("Templates/Buttons2")]
        [Button("Butterfly", ButtonSizes.Medium)]
        private void GenerateButterfly()
        {
            if (!UnityEditor.EditorUtility.DisplayDialog("Generate Butterfly",
                    "This will overwrite the current level. Continue?", "Yes", "No")) return;
            
            Field = new bool[width, height, layers];
                
            int centerX = width / 2;
            int centerY = height / 2;
                
            for (int layer = 0; layer < layers; layer++)
            {
                float scale = 1f - layer * 0.1f;
                    
                for (int y = centerY - 6; y <= centerY + 6; y += 2)
                {
                    if (y >= 0 && y < height)
                    {
                        Field[centerX, y, layer] = true;
                    }
                }
                    
                GenerateButterflyWing(centerX, centerY, -1, layer, scale);
                    
                GenerateButterflyWing(centerX, centerY, 1, layer, scale);
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private void GenerateButterflyWing(int centerX, int centerY, int side, int layer, float scale)
        {
            for (int i = 1; i <= 4; i++)
            {
                for (int j = 0; j <= 3; j++)
                {
                    int x = centerX + side * i * 2;
                    int y = centerY - 2 - j * 2;
                    
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        float wingRadius = 3f - j * 0.5f;
                        if (i <= wingRadius * scale)
                        {
                            Field[x, y, layer] = true;
                        }
                    }
                }
            }
            
            for (int i = 1; i <= 3; i++)
            {
                for (int j = 1; j <= 2; j++)
                {
                    int x = centerX + side * i * 2;
                    int y = centerY + j * 2;
                    
                    if (x >= 0 && x < width && y >= 0 && y < height)
                    {
                        float wingRadius = 2f - j * 0.3f;
                        if (i <= wingRadius * scale)
                        {
                            Field[x, y, layer] = true;
                        }
                    }
                }
            }
        }

        [PropertyOrder(5)]
        [ButtonGroup("Templates/Buttons3")]
        [Button("Mountain", ButtonSizes.Medium)]
        private void GenerateMountain()
        {
            if (!UnityEditor.EditorUtility.DisplayDialog("Generate Mountain",
                    "This will overwrite the current level. Continue?", "Yes", "No")) return;
            
            Field = new bool[width, height, layers];
                
            for (int layer = 0; layer < layers; layer++)
            {
                float layerOffset = layer * 0.5f;
                    
                for (int x = 0; x < width; x += 2)
                {
                    float mountainHeight = 0;
                        
                    mountainHeight = Mathf.Max(mountainHeight, 
                        GetMountainPeak(x, width / 2f, height * 0.8f, width * 0.3f));
                        
                    mountainHeight = Mathf.Max(mountainHeight, 
                        GetMountainPeak(x, width / 4f, height * 0.6f, width * 0.2f));
                        
                    mountainHeight = Mathf.Max(mountainHeight, 
                        GetMountainPeak(x, width * 3 / 4f, height * 0.7f, width * 0.25f));
                        
                    mountainHeight -= layerOffset * 2;
                        
                    for (int y = 0; y < Mathf.Min(mountainHeight, height); y += 2)
                    {
                        Field[x, y, layer] = true;
                    }
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }

        private float GetMountainPeak(int x, float peakX, float peakHeight, float peakWidth)
        {
            float distance = Mathf.Abs(x - peakX);
            if (distance > peakWidth) return 0;
            
            float normalizedDistance = distance / peakWidth;
            return peakHeight * (1 - normalizedDistance * normalizedDistance);
        }

        [PropertyOrder(5)]
        [ButtonGroup("Templates/Buttons3")]
        [Button("Maze", ButtonSizes.Medium)]
        private void GenerateMaze()
        {
            if (!UnityEditor.EditorUtility.DisplayDialog("Generate Maze",
                    "This will overwrite the current level. Continue?", "Yes", "No")) return;
            
            Field = new bool[width, height, layers];
                
            for (int layer = 0; layer < layers; layer++)
            {
                for (int x = 0; x < width; x += 4)
                {
                    for (int y = 0; y < height; y += 2)
                    {
                        Field[x, y, layer] = true;
                    }
                }
                    
                for (int y = 0; y < height; y += 4)
                {
                    for (int x = 0; x < width; x += 2)
                    {
                        Field[x, y, layer] = true;
                    }
                }
                
                System.Random random = new System.Random();
                for (int i = 0; i < width * height / 20; i++)
                {
                    int x = random.Next(0, width / 2) * 2;
                    int y = random.Next(0, height / 2) * 2;
                    Field[x, y, layer] = false;
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [PropertyOrder(5)]
        [ButtonGroup("Templates/Buttons3")]
        [Button("Heart", ButtonSizes.Medium)]
        private void GenerateHeart()
        {
            if (!UnityEditor.EditorUtility.DisplayDialog("Generate Heart",
                    "This will overwrite the current level. Continue?", "Yes", "No")) return;
            
            Field = new bool[width, height, layers];
                
            int centerX = width / 2;
            int centerY = Mathf.RoundToInt(height * 0.25f);
                
            for (int layer = 0; layer < layers; layer++)
            {
                float scale = 1f - layer * 0.1f;
                    
                for (int x = 0; x < width; x += 2)
                {
                    for (int y = 0; y < height; y += 2)
                    {
                        if (IsInsideHeart(x - centerX, y - centerY, scale))
                        {
                            Field[x, y, layer] = true;
                        }
                    }
                }
            }

            ReverseLayersOrder(false);
        }

        private bool IsInsideHeart(float x, float y, float scale)
        {
            // Hearth: (x^2 + y^2 - 1)^3 - x^2*y^3 <= 0
            x = x * 0.2f * scale;
            y = (y - 2) * 0.2f * scale;
            
            float left = (x * x + y * y - 1);
            left = left * left * left;
            float right = x * x * y * y * y;
            
            return left - right <= 0;
        }

        [PropertyOrder(5)]
        [ButtonGroup("Templates/Buttons3")]
        [Button("Random", ButtonSizes.Medium)]
        private void GenerateRandom()
        {
            if (!UnityEditor.EditorUtility.DisplayDialog("Generate Random",
                    "This will overwrite the current level. Continue?", "Yes", "No")) return;
            
            Field = new bool[width, height, layers];
                
            System.Random random = new System.Random();
                
            for (int layer = 0; layer < layers; layer++)
            {
                float density = 0.7f - layer * 0.15f;
                    
                for (int x = 0; x < width; x += 2)
                {
                    for (int y = 0; y < height; y += 2)
                    {
                        if (random.NextDouble() < density)
                        {
                            Field[x, y, layer] = true;
                        }
                    }
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [PropertyOrder(5)]
        [ButtonGroup("Templates/Buttons3")]
        [Button("Border", ButtonSizes.Medium)]
        private void GenerateBorder()
        {
            if (!UnityEditor.EditorUtility.DisplayDialog("Generate Border",
                    "This will overwrite the current level. Continue?", "Yes", "No")) return;
            
            Field = new bool[width, height, layers];
                
            for (int layer = 0; layer < layers; layer++)
            {
                for (int x = 0; x < width; x += 2)
                {
                    Field[x, 0, layer] = true;
                    int bottomY = height - 1;
                    bottomY = (bottomY / 2) * 2;
                    if (bottomY >= 0) Field[x, bottomY, layer] = true;
                }
                
                for (int y = 0; y < height; y += 2)
                {
                    Field[0, y, layer] = true;
                    int rightX = width - 1;
                    rightX = (rightX / 2) * 2;
                    if (rightX >= 0) Field[rightX, y, layer] = true;
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }
        
        [PropertyOrder(5)]
        [ButtonGroup("Templates/Buttons4")]
        [Button("Wave Pattern", ButtonSizes.Medium)]
        private void GenerateWavePattern()
        {
            if (!UnityEditor.EditorUtility.DisplayDialog("Generate Wave Pattern",
                    "This will overwrite the current level. Continue?", "Yes", "No")) return;
            
            Field = new bool[width, height, layers];
                
            for (int layer = 0; layer < layers; layer++)
            {
                float frequency = 2f + layer * 0.5f;
                float amplitude = 2f + layer;
                    
                for (int x = 0; x < width; x += 2)
                {
                    float wave = Mathf.Sin(x * frequency * 0.3f) * amplitude;
                    int yPos = height / 2 + Mathf.RoundToInt(wave);
                    yPos = (yPos / 2) * 2;
                        
                    for (int yOffset = -2; yOffset <= 2; yOffset += 2)
                    {
                        int y = yPos + yOffset;
                        if (y >= 0 && y < height && x >= 0)
                        {
                            Field[x, y, layer] = true;
                        }
                    }
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [PropertyOrder(5)]
        [ButtonGroup("Templates/Buttons4")]
        [Button("Vortex", ButtonSizes.Medium)]
        private void GenerateVortex()
        {
            if (!UnityEditor.EditorUtility.DisplayDialog("Generate Vortex",
                    "This will overwrite the current level. Continue?", "Yes", "No")) return;
            
            Field = new bool[width, height, layers];
                
            float centerX = width / 2f;
            float centerY = height / 2f;
                
            for (int layer = 0; layer < layers; layer++)
            {
                float maxRadius = Mathf.Min(width, height) * 0.8f - layer * 3;
                float spiralTightness = 0.3f + layer * 0.1f;
                    
                for (float radius = 2f; radius <= maxRadius; radius += 2f)
                {
                    for (float angle = 0; angle < Mathf.PI * 2; angle += Mathf.PI / 8f)
                    {
                        float spiralRadius = radius * (1f + Mathf.Sin(angle * spiralTightness) * 0.3f);
                        int x = Mathf.RoundToInt(centerX + Mathf.Cos(angle) * spiralRadius);
                        int y = Mathf.RoundToInt(centerY + Mathf.Sin(angle) * spiralRadius);
                            
                        x = (x / 2) * 2;
                        y = (y / 2) * 2;
                            
                        if (x >= 0 && x < width && y >= 0 && y < height)
                        {
                            Field[x, y, layer] = true;
                        }
                    }
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [PropertyOrder(5)]
        [ButtonGroup("Templates/Buttons4")]
        [Button("Fractal Tree", ButtonSizes.Medium)]
        private void GenerateFractalTree()
        {
            if (!UnityEditor.EditorUtility.DisplayDialog("Generate Fractal Tree",
                    "This will overwrite the current level. Continue?", "Yes", "No")) return;
            
            Field = new bool[width, height, layers];
                
            int trunkX = width / 2;
                
            for (int layer = 0; layer < layers; layer++)
            {
                int trunkHeight = height / 3 + layer * 2;
                for (int y = 0; y < trunkHeight; y += 2)
                {
                    if (y >= 0 && y < height)
                    {
                        Field[trunkX, y, layer] = true;
                    }
                }
                
                GenerateBranches(trunkX, trunkHeight, trunkHeight / 2f, 45f, layer, 3);
            }

            ReverseLayersOrder(false);
        }

        private void GenerateBranches(int startX, int startY, float length, float angle, int layer, int depth)
        {
            if (depth <= 0 || length < 2) return;
            
            float angleRad = angle * Mathf.Deg2Rad;
            
            int endXLeft = startX + Mathf.RoundToInt(Mathf.Cos(angleRad + Mathf.PI) * length);
            int endYLeft = startY + Mathf.RoundToInt(Mathf.Sin(angleRad + Mathf.PI) * length);
            
            int endXRight = startX + Mathf.RoundToInt(Mathf.Cos(-angleRad) * length);
            int endYRight = startY + Mathf.RoundToInt(Mathf.Sin(-angleRad) * length);
            
            DrawLine(startX, startY, endXLeft, endYLeft, layer);
            DrawLine(startX, startY, endXRight, endYRight, layer);
            
            GenerateBranches(endXLeft, endYLeft, length * 0.7f, angle * 0.8f, layer, depth - 1);
            GenerateBranches(endXRight, endYRight, length * 0.7f, angle * 0.8f, layer, depth - 1);
        }

        private void DrawLine(int x1, int y1, int x2, int y2, int layer)
        {
            x1 = (x1 / 2) * 2; y1 = (y1 / 2) * 2;
            x2 = (x2 / 2) * 2; y2 = (y2 / 2) * 2;
            
            int dx = Mathf.Abs(x2 - x1);
            int dy = Mathf.Abs(y2 - y1);
            int steps = Mathf.Max(dx, dy) / 2;
            
            for (int i = 0; i <= steps; i++)
            {
                float t = (float)i / steps;
                int x = Mathf.RoundToInt(Mathf.Lerp(x1, x2, t));
                int y = Mathf.RoundToInt(Mathf.Lerp(y1, y2, t));
                
                x = (x / 2) * 2;
                y = (y / 2) * 2;
                
                if (x >= 0 && x < width && y >= 0 && y < height)
                {
                    Field[x, y, layer] = true;
                }
            }
        }

        [PropertyOrder(5)]
        [ButtonGroup("Templates/Buttons4")]
        [Button("Celestial Orbits", ButtonSizes.Medium)]
        private void GenerateCelestialOrbits()
        {
            if (!UnityEditor.EditorUtility.DisplayDialog("Generate Celestial Orbits",
                    "This will overwrite the current level. Continue?", "Yes", "No")) return;
            
            Field = new bool[width, height, layers];
                
            float centerX = width / 2f;
            float centerY = height / 2f;
                
            for (int layer = 0; layer < layers; layer++)
            {
                int orbits = 3 + layer;
                float maxOrbitRadius = Mathf.Min(width, height) * 0.4f;
                    
                for (int orbit = 0; orbit < orbits; orbit++)
                {
                    float radius = (orbit + 1) * (maxOrbitRadius / orbits);
                    int planets = 6 + orbit * 2;
                        
                    for (int i = 0; i < planets; i++)
                    {
                        float angle = i * (Mathf.PI * 2 / planets);
                        float offset = Mathf.Sin(angle * 3) * radius * 0.2f;
                            
                        int x = Mathf.RoundToInt(centerX + Mathf.Cos(angle) * (radius + offset));
                        int y = Mathf.RoundToInt(centerY + Mathf.Sin(angle) * (radius + offset));
                            
                        x = (x / 2) * 2;
                        y = (y / 2) * 2;
                            
                        if (x >= 0 && x < width && y >= 0 && y < height)
                        {
                            Field[x, y, layer] = true;
                            
                            for (int dx = -2; dx <= 2; dx += 2)
                            {
                                for (int dy = -2; dy <= 2; dy += 2)
                                {
                                    if (Mathf.Abs(dx) + Mathf.Abs(dy) <= 2 && Random.Range(0, 100) < 30)
                                    {
                                        int clusterX = x + dx;
                                        int clusterY = y + dy;
                                        if (clusterX >= 0 && clusterX < width && clusterY >= 0 && clusterY < height)
                                        {
                                            Field[clusterX, clusterY, layer] = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            UnityEditor.EditorUtility.SetDirty(this);
        }

        [PropertyOrder(5)]
        [ButtonGroup("Templates/Buttons4")]
        [Button("Neural Network", ButtonSizes.Medium)]
        private void GenerateNeuralNetwork()
        {
            if (!UnityEditor.EditorUtility.DisplayDialog("Generate Neural Network",
                    "This will overwrite the current level. Continue?", "Yes", "No")) return;
            
            Field = new bool[width, height, layers];
                
            System.Random rand = new System.Random();
                
            for (int layer = 0; layer < layers; layer++)
            {
                int nodesPerLayer = 4 + layer;
                float layerSpacing = width / (float)(nodesPerLayer + 1);
                    
                Vector2[] nodes = new Vector2[nodesPerLayer];
                for (int i = 0; i < nodesPerLayer; i++)
                {
                    float x = (i + 1) * layerSpacing;
                    float y = height / 2f + Mathf.Sin(i * Mathf.PI * 0.3f) * height * 0.3f;
                    nodes[i] = new Vector2(x, y);
                    
                    int nodeX = Mathf.RoundToInt(x); nodeX = (nodeX / 2) * 2;
                    int nodeY = Mathf.RoundToInt(y); nodeY = (nodeY / 2) * 2;
                        
                    if (nodeX >= 0 && nodeX < width && nodeY >= 0 && nodeY < height)
                    {
                        Field[nodeX, nodeY, layer] = true;
                    }
                }
                
                for (int i = 0; i < nodesPerLayer - 1; i++)
                {
                    for (int j = i + 1; j < nodesPerLayer; j++)
                    {
                        if (rand.Next(0, 100) < 60) // 60% chance to connect
                        {
                            DrawLine(
                                Mathf.RoundToInt(nodes[i].x), Mathf.RoundToInt(nodes[i].y),
                                Mathf.RoundToInt(nodes[j].x), Mathf.RoundToInt(nodes[j].y),
                                layer
                            );
                        }
                    }
                }
            }
            
            ReverseLayersOrder(false);
        }
        
        #endregion
        
        private void OnValidate()
        {
            width = Mathf.Max(1, width);
            height = Mathf.Max(1, height);
            layers = Mathf.Max(1, layers);
            currentLayer = Mathf.Clamp(currentLayer, 0, LayersMaxIndex);
            
            Resize();
        }
        
        private void Resize()
        {
            bool[,,] newField = new bool[width, height, layers];
            
            if (Field != null)
            {
                int minWidth = Mathf.Min(width, Field.GetLength(0));
                int minHeight = Mathf.Min(height, Field.GetLength(1));
                int minLayers = Mathf.Min(layers, Field.GetLength(2));
                
                for (int x = 0; x < minWidth; x++)
                {
                    for (int y = 0; y < minHeight; y++)
                    {
                        for (int z = 0; z < minLayers; z++)
                        {
                            newField[x, y, z] = Field[x, y, z];
                        }
                    }
                }
            }
            
            Field = newField;
            currentLayer = Mathf.Clamp(currentLayer, 0, LayersMaxIndex);
        }

#endif
    }
}