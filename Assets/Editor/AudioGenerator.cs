#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace ProjectBase.Editor
{
    /// <summary>
    /// Công cụ Editor tự động tổng hợp (procedural synthesis) các file âm thanh WAV 16-bit PCM chất lượng cao
    /// cho dự án GAM301: Tiếng súng, nổ, va chạm, ghi điểm, tử trận và nhạc nền Cyber Arena loop.
    /// Hoàn toàn độc lập, không phụ thuộc tài nguyên bên ngoài.
    /// </summary>
    public static class AudioGenerator
    {
        private const string AUDIO_DIR = "Assets/Audio";
        private const int SAMPLE_RATE = 44100;

        [MenuItem("GAM301/Audio/Generate All Sound FX and BGM", false, 10)]
        public static void GenerateAllAudio()
        {
            if (!Directory.Exists(AUDIO_DIR))
            {
                Directory.CreateDirectory(AUDIO_DIR);
            }

            GenerateLaserShoot();
            GenerateExplosion();
            GenerateHit();
            GenerateScoreChime();
            GenerateGameOverSound();
            GenerateCyberArenaBGM();

            AssetDatabase.Refresh();
            Debug.Log("<color=green>[AudioGenerator] ĐÃ TẠO XONG TOÀN BỘ 6 FILE ÂM THANH WAV CHUẨN VÀO Assets/Audio/ !</color>");
        }

        // ==========================================
        // 1. TIẾNG SÚNG BẮN (SFX_LaserShoot.wav)
        // ==========================================
        private static void GenerateLaserShoot()
        {
            float duration = 0.22f;
            int numSamples = (int)(SAMPLE_RATE * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / numSamples;
                // Tần số quét nhanh từ 980Hz tụt xuống 180Hz
                float freq = Mathf.Lerp(980f, 180f, Mathf.Pow(t, 0.45f));
                float phase = 2f * Mathf.PI * freq * (float)i / SAMPLE_RATE;
                
                // Trộn sóng Sine và Triangle cho âm thanh sắc bén
                float wave = Mathf.Sin(phase) * 0.7f + Mathf.PingPong(phase, 1f) * 0.3f;
                // Envelope decay nhanh
                float env = Mathf.Exp(-t * 14f);
                samples[i] = wave * env * 0.9f;
            }

            SaveWavFile($"{AUDIO_DIR}/SFX_LaserShoot.wav", samples, 1, SAMPLE_RATE);
        }

        // ==========================================
        // 2. TIẾNG NỔ TUNG (SFX_Explosion.wav)
        // ==========================================
        private static void GenerateExplosion()
        {
            float duration = 0.85f;
            int numSamples = (int)(SAMPLE_RATE * duration);
            float[] samples = new float[numSamples];
            System.Random rand = new System.Random(42);

            float lastNoise = 0f;
            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / numSamples;
                // White noise qua low-pass filter
                float white = (float)(rand.NextDouble() * 2.0 - 1.0);
                lastNoise = lastNoise * 0.75f + white * 0.25f;

                // Sub-bass rumble 55Hz tụt dần
                float subFreq = Mathf.Lerp(65f, 35f, t);
                float sub = Mathf.Sin(2f * Mathf.PI * subFreq * (float)i / SAMPLE_RATE);

                float env = Mathf.Exp(-t * 5.5f);
                float punch = Mathf.Exp(-t * 22f); // Lực đập ban đầu

                samples[i] = Mathf.Clamp((lastNoise * 0.7f + sub * 0.6f + punch * 0.4f) * env, -1f, 1f);
            }

            SaveWavFile($"{AUDIO_DIR}/SFX_Explosion.wav", samples, 1, SAMPLE_RATE);
        }

        // ==========================================
        // 3. TIẾNG VA CHẠM / TRÚNG ĐẠN (SFX_Hit.wav)
        // ==========================================
        private static void GenerateHit()
        {
            float duration = 0.18f;
            int numSamples = (int)(SAMPLE_RATE * duration);
            float[] samples = new float[numSamples];

            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / numSamples;
                float freq = Mathf.Lerp(450f, 90f, t);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * (float)i / SAMPLE_RATE);
                // Âm kim loại cao
                float metallic = Mathf.Sin(2f * Mathf.PI * 1650f * (float)i / SAMPLE_RATE) * 0.35f;

                float env = Mathf.Exp(-t * 18f);
                samples[i] = (wave * 0.7f + metallic) * env;
            }

            SaveWavFile($"{AUDIO_DIR}/SFX_Hit.wav", samples, 1, SAMPLE_RATE);
        }

        // ==========================================
        // 4. TIẾNG GHI ĐIỂM LENG KENG (SFX_Score.wav)
        // ==========================================
        private static void GenerateScoreChime()
        {
            float duration = 0.38f;
            int numSamples = (int)(SAMPLE_RATE * duration);
            float[] samples = new float[numSamples];

            int splitIndex = (int)(numSamples * 0.35f);
            for (int i = 0; i < numSamples; i++)
            {
                float t = (float)i / numSamples;
                float freq = (i < splitIndex) ? 587.33f : 880f; // D5 -> A5 tươi sáng
                float wave = Mathf.Sin(2f * Mathf.PI * freq * (float)i / SAMPLE_RATE);
                float harmonic = Mathf.Sin(2f * Mathf.PI * freq * 2f * (float)i / SAMPLE_RATE) * 0.25f;

                float noteT = (i < splitIndex) ? ((float)i / splitIndex) : ((float)(i - splitIndex) / (numSamples - splitIndex));
                float env = Mathf.Exp(-noteT * 7f);

                samples[i] = (wave + harmonic) * env * 0.85f;
            }

            SaveWavFile($"{AUDIO_DIR}/SFX_Score.wav", samples, 1, SAMPLE_RATE);
        }

        // ==========================================
        // 5. TIẾNG TỬ TRẬN GAME OVER (SFX_GameOver.wav)
        // ==========================================
        private static void GenerateGameOverSound()
        {
            float duration = 1.6f;
            int numSamples = (int)(SAMPLE_RATE * duration);
            float[] samples = new float[numSamples];

            // 3 nốt trầm lắng: D4 (293.66Hz) -> Bb3 (233.08Hz) -> G3 (196.00Hz)
            float[] notes = { 293.66f, 233.08f, 196.00f };
            int samplesPerNote = numSamples / 3;

            for (int i = 0; i < numSamples; i++)
            {
                int noteIdx = Mathf.Clamp(i / samplesPerNote, 0, 2);
                float freq = notes[noteIdx];
                float subFreq = freq * 0.5f;

                float wave = Mathf.Sin(2f * Mathf.PI * freq * (float)i / SAMPLE_RATE);
                float sub = Mathf.Sin(2f * Mathf.PI * subFreq * (float)i / SAMPLE_RATE) * 0.4f;

                float noteT = (float)(i % samplesPerNote) / samplesPerNote;
                float env = Mathf.Exp(-noteT * 2.5f);

                samples[i] = (wave + sub) * env * 0.8f;
            }

            SaveWavFile($"{AUDIO_DIR}/SFX_GameOver.wav", samples, 1, SAMPLE_RATE);
        }

        // ==========================================
        // 6. NHẠC NỀN CYBER ARENA BGM (Loopable 110 BPM)
        // ==========================================
        private static void GenerateCyberArenaBGM()
        {
            // 110 BPM -> 1 Beat = 60 / 110 = 0.54545s.
            // 4 bars = 16 beats = 8.72727s (chu kỳ loop hoàn hảo)
            float bpm = 110f;
            float beatDuration = 60f / bpm;
            float totalDuration = beatDuration * 16f;
            int numSamples = (int)(SAMPLE_RATE * totalDuration);
            float[] samples = new float[numSamples];

            // Bass notes (D minor: D2, F2, C2, D2)
            float[] bassLine = { 73.42f, 73.42f, 87.31f, 73.42f, 65.41f, 73.42f, 87.31f, 98.00f };

            for (int i = 0; i < numSamples; i++)
            {
                float currentTime = (float)i / SAMPLE_RATE;
                float currentBeat = currentTime / beatDuration;
                
                // Kick Drum ở mỗi beat
                float beatFraction = currentBeat - Mathf.Floor(currentBeat);
                float kick = 0f;
                if (beatFraction < 0.25f)
                {
                    float kickT = beatFraction / 0.25f;
                    float kickFreq = Mathf.Lerp(120f, 45f, Mathf.Pow(kickT, 0.4f));
                    kick = Mathf.Sin(2f * Mathf.PI * kickFreq * (float)i / SAMPLE_RATE) * Mathf.Exp(-kickT * 6f) * 0.7f;
                }

                // 16th-note Synth Bass
                float stepFraction = (currentBeat * 4f) - Mathf.Floor(currentBeat * 4f);
                int stepIndex = ((int)(currentBeat * 2f)) % bassLine.Length;
                float bassFreq = bassLine[stepIndex];
                float bassWave = Mathf.Sin(2f * Mathf.PI * bassFreq * (float)i / SAMPLE_RATE) * 0.5f;
                float bassSaw = (Mathf.PingPong(2f * bassFreq * currentTime, 1f) - 0.5f) * 0.3f;
                float bassEnv = Mathf.Exp(-stepFraction * 5f);
                float bass = (bassWave + bassSaw) * bassEnv * 0.45f;

                // Ambient Pad chord (D minor pad: D3, F3, A3)
                float pad1 = Mathf.Sin(2f * Mathf.PI * 146.83f * currentTime);
                float pad2 = Mathf.Sin(2f * Mathf.PI * 174.61f * currentTime);
                float pad3 = Mathf.Sin(2f * Mathf.PI * 220.00f * currentTime);
                float pad = (pad1 + pad2 + pad3) * 0.1f;

                samples[i] = Mathf.Clamp(kick + bass + pad, -1f, 1f);
            }

            SaveWavFile($"{AUDIO_DIR}/BGM_CyberArena.wav", samples, 1, SAMPLE_RATE);
        }

        // ==========================================
        // TIỆN ÍCH GHI FILE WAV 16-BIT PCM CHUẨN
        // ==========================================
        private static void SaveWavFile(string filePath, float[] samples, short channels, int sampleRate)
        {
            using (FileStream fs = new FileStream(filePath, FileMode.Create))
            using (BinaryWriter bw = new BinaryWriter(fs))
            {
                int bitsPerSample = 16;
                int byteRate = sampleRate * channels * bitsPerSample / 8;
                short blockAlign = (short)(channels * bitsPerSample / 8);
                int subChunk2Size = samples.Length * channels * bitsPerSample / 8;
                int chunkSize = 36 + subChunk2Size;

                // RIFF Header
                bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(chunkSize);
                bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

                // fmt Subchunk
                bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16); // Subchunk1Size (16 for PCM)
                bw.Write((short)1); // AudioFormat (1 for PCM)
                bw.Write(channels);
                bw.Write(sampleRate);
                bw.Write(byteRate);
                bw.Write(blockAlign);
                bw.Write((short)bitsPerSample);

                // data Subchunk
                bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
                bw.Write(subChunk2Size);

                // Sample data
                for (int i = 0; i < samples.Length; i++)
                {
                    short sampleInt = (short)(Mathf.Clamp(samples[i], -1f, 1f) * 32767f);
                    bw.Write(sampleInt);
                }
            }
        }
    }
}
#endif
