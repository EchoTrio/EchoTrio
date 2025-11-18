using IniParser;
using IniParser.Model;
using OpenAI;
using ElevenLabs;
using UnityEngine;

namespace EchoTrio {
    public class Authentication {
        private const string FileName = "Authentication.ini";

        public static OpenAIAuthentication GetOpenAIAuthentication() {
            var parser = new FileIniDataParser();
            IniData data = parser.ReadFile($"{Application.streamingAssetsPath}/Configs/{FileName}");
            string apiKey = data["OpenAI"]["api_key"];
            string orgKey = data["OpenAI"]["org_key"];
            string projKey = data["OpenAI"]["proj_key"];
            return new OpenAIAuthentication(apiKey, orgKey, projKey);
        }

        public static ElevenLabsAuthentication GetElevenLabsAuthentication() {
            FileIniDataParser parser = new FileIniDataParser();
            IniData data = parser.ReadFile($"{Application.streamingAssetsPath}/Configs/{FileName}");
            string apiKey = data["ElevenLabs"]["api_key"];
            return new ElevenLabsAuthentication(apiKey);
        }
    }
}