using UnityEngine;
using TMPro;
using System.IO;

namespace EchoTrio.Log {
    public class ConversationLogger : MonoBehaviour {
        [Header("References")]
        public TextMeshProUGUI conversationText;

        void OnApplicationQuit() {
            SaveConversation();
        }

        void SaveConversation() {
            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string fileName = $"Playtest_{timestamp}.txt";

            //create folder if not exists
            string folderPath = Path.Combine(Application.dataPath, "PlaytestLogs");
            Directory.CreateDirectory(folderPath);

            string filePath = Path.Combine(folderPath, fileName);

            using (StreamWriter writer = new StreamWriter(filePath, false)) {
                writer.WriteLine("========== Playtest Session ==========");
                writer.WriteLine("Timestamp: " + System.DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                writer.WriteLine("Conversation History:");
                writer.WriteLine(conversationText.text);
                writer.WriteLine("======================================");
            }

            Debug.Log("Conversation saved to: " + filePath);
        }
    }
}