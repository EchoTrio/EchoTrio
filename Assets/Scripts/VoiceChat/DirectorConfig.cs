using UnityEngine;

namespace EchoTrio {
    [CreateAssetMenu(fileName = "DirectorConfig", menuName = "EchoTrio/DirectorConfig")]
    public class DirectorConfig : ScriptableObject {
        [TextArea(64, 128)] public string instructions = string.Empty;
    }
}