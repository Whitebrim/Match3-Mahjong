using UnityEngine;

namespace Levels.Configs
{
    [CreateAssetMenu(fileName = "Level 1 Config", menuName = "ScriptableObjects/Level 1 Config", order = 0)]
    public class Level1Config : ScriptableObject
    {
        public GameObject prefab;
    }
}