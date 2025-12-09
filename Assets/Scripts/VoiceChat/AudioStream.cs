using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ElevenLabs;
using ElevenLabs.TextToSpeech;
using UnityEngine;

namespace EchoTrio {
    [System.Serializable] public class AudioStream {
        private ElevenLabsClient elevenLabsApi = null;
        private TextToSpeechRequest request = null;
        private CancellationToken cancellationToken;
        private Queue<AudioClip> audioQueue = new Queue<AudioClip>();
        private bool streamEnded = false;

        public AudioStream(ElevenLabsClient eleventLabsApi, TextToSpeechRequest request, CancellationToken cancellationToken) {
            this.elevenLabsApi = eleventLabsApi;
            this.request = request;
            this.cancellationToken = cancellationToken;
            _ = StreamThread();
        }

        public AudioStream() {
            streamEnded = true;
        }

        public bool IsDone() {
            if (!streamEnded) return false;
            lock (audioQueue) {
                return audioQueue.Count == 0;
            }
        }

        public AudioClip TryGetAudioClip() {
            lock (audioQueue) {
                if (audioQueue.Count == 0) { return null; }
                return audioQueue.Dequeue();
            }
        }

        private async Task PartialClipCallback(VoiceClip voiceClip) {
            lock (audioQueue) {
                audioQueue.Enqueue(voiceClip.AudioClip);
            }
            await Task.CompletedTask;
        }

        private async Task StreamThread() {
            await elevenLabsApi.TextToSpeechEndpoint.TextToSpeechAsync(request, PartialClipCallback, cancellationToken);
            streamEnded = true;
        }
    }
}