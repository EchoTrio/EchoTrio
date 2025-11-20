// By Terri Lim, CMU ETC Class of 2026. Last updated by me in November 2025. Feel free to judge any code up till then.
namespace EchoTrio {
    public enum Emotion {
        Neutral = 0,
        Frustrated,
        Happy,
        Sad,

        Num,
    }

    public static class EmotionExtensions {
        public static string ToString(this Emotion emotion) {
            switch (emotion) {
                case Emotion.Neutral: return "Neutral";
                case Emotion.Frustrated: return "Frustrated";
                case Emotion.Happy: return "Happy";
                case Emotion.Sad: return "Sad";
                default: throw new System.NotImplementedException();
            }
        }

        public static Emotion ToEmotion(this string str) {
            switch (str) {
                case "Neutral": return Emotion.Neutral;
                case "Frustrated": return Emotion.Frustrated;
                case "Happy": return Emotion.Happy;
                case "Sad": return Emotion.Sad;
                default: return Emotion.Neutral;
            }
        }
    }
}