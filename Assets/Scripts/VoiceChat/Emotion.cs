namespace EchoTrio {
    /// Enum of all the possible emotions the actor can have. This is then used to trigger the facial expressions of the 3D models.
    /// NOTE: This system is currently not in used, and the facial expressions of the 3D models are being determined via the animation system itself.
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