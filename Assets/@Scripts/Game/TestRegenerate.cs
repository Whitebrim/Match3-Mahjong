using Game;
using TMPro;
using UnityEngine;

public class TestRegenerate : MonoBehaviour
{
    [SerializeField] private TMP_InputField x, y, z;
    [SerializeField] private TestFieldView testFieldView;
    [SerializeField] private Buffer buffer;

    public void Regenerate()
    {
        buffer.ClearBuffer();
        testFieldView.GenerateNewMap(int.Parse(x.text), int.Parse(y.text), int.Parse(z.text));
    }
}
