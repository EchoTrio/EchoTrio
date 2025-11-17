using UnityEngine;
using CrazyMinnow.SALSA;

namespace DemoCode
{
    public class UpstreamAudioFilterProcessing : MonoBehaviour
    {
        public Salsa salsaInstance;
        private float[] analysisBuffer = new float[1024];
        private int bufferPointer = 0;
        private int interleave = 1;

        private void Awake()
        {
            if (!salsaInstance)
                salsaInstance = GetComponent<Salsa>();
            if (salsaInstance)
                salsaInstance.getExternalAnalysis = GetAnalysisValueLeveragingSalsaAnalyzer;
            else
                Debug.Log("SALSA not found");
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {

            //interleave = channels;
            for (int i = 0; i < data.Length; i+=channels)
            {
                analysisBuffer[bufferPointer] = data[i];
                bufferPointer++;
                bufferPointer %= analysisBuffer.Length; // wrap the pointer if necessary
            }
        }

        // utilize the built-in SALSA analyzer on the upstream audio data
        float GetAnalysisValueLeveragingSalsaAnalyzer()
        {
            return salsaInstance.audioAnalyzer(interleave, analysisBuffer);
        }
    }
}

