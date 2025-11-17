using ElevenLabs;
using IniParser;
using IniParser.Model;
using OpenAI;
using UnityEngine;

namespace EchoTrio {
    public class Authentication {
        private const string FileName = "Authentication.ini";

        public static OpenAIAuthentication GetOpenAIAuthentication() {
            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile($"{Application.streamingAssetsPath}/Configs/{FileName}");
            string apiKey = data["OpenAI Authentication"]["api_key"];
            string orgKey = data["OpenAI Authentication"]["org_key"];
            string projKey = data["OpenAI Authentication"]["proj_key"];
            return new OpenAIAuthentication(apiKey, orgKey, projKey);
        }

        public static ElevenLabsAuthentication GetElevenLabsAuthentication() {
            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile($"{Application.streamingAssetsPath}/Configs/{FileName}");
            string apiKey = data["ElevenLabs Authentication"]["api_key"];
            return new ElevenLabsAuthentication(apiKey);
        }
    }
}