// By Terri Lim, CMU ETC Class of 2026. Last updated by me in December 2025. Feel free to judge any code up till then.
using OpenAI;
using ElevenLabs;
using UnityEngine;
using Microsoft.Extensions.Configuration;

namespace EchoTrio {
    /// Helper class to load the authentication file and retrieve API keys.
    public class Authentication {
        private const string FileName = "Authentication.ini";

        public static bool AuthenticationFileExists() {
            return System.IO.File.Exists($"{Application.streamingAssetsPath}/Configs/{FileName}");
        }

        public static OpenAIAuthentication GetOpenAIAuthentication() {
            IConfiguration config = new ConfigurationBuilder().AddIniFile($"{Application.streamingAssetsPath}/Configs/{FileName}").Build();
            IConfigurationSection section = config.GetSection("OpenAI");
            string apiKey = section["api_key"];
            string orgId = section["org_id"];
            string projId = section["proj_id"];
            return new OpenAIAuthentication(apiKey, orgId, projId);
        }

        public static ElevenLabsAuthentication GetElevenLabsAuthentication() {
            IConfiguration config = new ConfigurationBuilder().AddIniFile($"{Application.streamingAssetsPath}/Configs/{FileName}").Build();
            IConfigurationSection section = config.GetSection("ElevenLabs");
            string apiKey = section["api_key"];
            return new ElevenLabsAuthentication(apiKey);
        }
    }
}