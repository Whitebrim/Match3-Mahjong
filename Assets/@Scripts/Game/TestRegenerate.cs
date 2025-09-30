using TMPro;
using UnityEngine;

namespace Game
{
    public class TestRegenerate : MonoBehaviour
    {
        [SerializeField] private TMP_InputField x, y, z;
        [SerializeField] private FieldHandler fieldHandler;
        [SerializeField] private Buffer buffer;

        public void Regenerate()
        {
            buffer.ClearBuffer();
            //testFieldView.GenerateNewMap(int.Parse(x.text), int.Parse(y.text), int.Parse(z.text));
            fieldHandler.GenerateNewMap();
        }
    }
}
