// By Terri Lim, CMU ETC Class of 2026. Last updated by me in December 2025. Feel free to judge any code up till then.
using UnityEngine;

namespace EchoTrio {
    [CreateAssetMenu(fileName = "DirectorConfig", menuName = "EchoTrio/DirectorConfig")]
    public class DirectorConfig : ScriptableObject {
        [TextArea(64, 128)] public string instructions = string.Empty;
    }
}