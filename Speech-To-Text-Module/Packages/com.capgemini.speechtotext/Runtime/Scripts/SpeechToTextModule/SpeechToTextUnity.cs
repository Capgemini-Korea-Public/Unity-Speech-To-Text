using UnityEngine;
using OpenAI;
using System.IO;
using NUnit.Framework;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using Unity.Collections;
using Unity.Sentis;
using System;

namespace SpeechToTextUnity
{
    public enum ESTTModelType
    {
        SentisWhisper,
        OpenAIWhisper,
    }

    public static class SpeechToTextUnityModule
    {
        private static bool isTranscribe = false;

        #region OpenAI Whisper
        private static OpenAIApi openAI = new OpenAIApi();
        public static async Task<string> SpeechToTextFromAPI(AudioClip audioClip)
        {
            if (isTranscribe) return null; // avoid duplicate execution 
            isTranscribe = true;

            string filePath = Path.Combine(Application.dataPath, AudioConvertor.AudioProcessingString, audioClip.name);
            var req = new CreateAudioTranscriptionsRequest
            {
                File = filePath,
                Model = "whisper-1",
                Language = "en", // target language
            };

            var res = await openAI.CreateAudioTranscription(req);

            isTranscribe = false;
            Assert.NotNull(res); //response null check

            Debug.Log(res);

            AudioConvertor.RemoveProcessedAudioFile();
            return res.Text;
        }
        #endregion

        #region Sentis Whisper
        private static ModelAsset audioDecoder1;
        private static ModelAsset audioDecoder2;
        private static ModelAsset audioEncoder;
        private static ModelAsset logMelSpectro; 

        private static AudioClip _audioClip;
        private static TextAsset vocab;

        private static Worker decoder1, decoder2, encoder, spectrogram;
        private static Worker argmax;

        private const int END_OF_TEXT = 50257; // 텍스트의 끝을 나타낼 토큰
        private const int START_OF_TRANSCRIPT = 50258; // 텍스트 변환 시작 지점
        private const int ENGLISH = 50259; // 영어를 나타낼 토큰
        private const int TRANSCRIBE = 50359; // 지정된 언어로 음성 텍스트 변환 
        private const int TRANSLATE = 50358;  // 영어로 음성 텍스트 변환 
        private const int NO_TIME_STAMPS = 50363; // 타임스탬프 없이 텍스트 변환

        private const int maxTokens = 1000;
        private static int numSamples; // 오디오의 샘플 수
        private static string[] tokens; // Whisper 모델에서 사용하는 토큰들을 저장하는 배열

        private static int tokenCount = 0; // 현재까지 처리된 토큰의 수
        private static NativeArray<int> outputTokens; //모델로부터 나온 결과 토큰을 저장하는 배열

        // 특수문자 Decoding에 사용
        private static int[] whiteSpaceCharacters = new int[256];

        private static Tensor<float> encodedAudio;
        private static Awaitable<string> m_Awaitable;

        private static NativeArray<int> lastToken;
        private static Tensor<int> lastTokenTensor;
        private static Tensor<int> tokensTensor;
        private static Tensor<float> audioInput;

        // 오디오 최대 사이즈 지정 (30초 at 16kHz)
        private const int maxSamples = 30 * 16000;

        public static async Task<string> SpeechToTextFromSentis(AudioClip audioClip)
        {
            if (isTranscribe) return null; // 중복 실행 방지
            DisposeAll();
            isTranscribe = true;

            string outputString = "";
            StringBuilder stringBuilder = new StringBuilder(outputString);

            outputTokens = new NativeArray<int>(maxTokens, Allocator.Persistent);
            outputTokens[0] = START_OF_TRANSCRIPT;
            outputTokens[1] = ENGLISH;
            outputTokens[2] = TRANSCRIBE; // TRANSLATE; //
                                          //outputTokens[3] = NO_TIME_STAMPS;// START_TIME;//
            tokenCount = 3;

            _audioClip = audioClip;
            await LoadAudioToTensor();
            EncodeAudio();

            tokensTensor = new Tensor<int>(new TensorShape(1, maxTokens));
            ComputeTensorData.Pin(tokensTensor);
            tokensTensor.Reshape(new TensorShape(1, tokenCount));
            tokensTensor.dataOnBackend.Upload<int>(outputTokens, tokenCount);

            lastToken = new NativeArray<int>(1, Allocator.Persistent); lastToken[0] = NO_TIME_STAMPS;
            lastTokenTensor = new Tensor<int>(new TensorShape(1, 1), new[] { NO_TIME_STAMPS });

            // 토큰의 개수가 최대에 달할 때 까지 음성 인식 작업 지속
            while (true)
            {
                if (!isTranscribe || tokenCount >= (outputTokens.Length - 1))
                {
                    AudioConvertor.RemoveProcessedAudioFile();
                    return stringBuilder.ToString();
                }
                m_Awaitable = InferenceStep(outputString);
                stringBuilder.Append(await m_Awaitable);
            }
        }

        public static void InitializeSentisModel()
        {
            DisposeAll();
            try
            {
                if (audioDecoder1 == null || audioDecoder2 == null || audioEncoder == null || logMelSpectro == null || vocab == null)
                {
                    audioDecoder1 = Resources.Load<ModelAsset>("decoder_model");
                    audioDecoder2 = Resources.Load<ModelAsset>("decoder_with_past_model");
                    audioEncoder = Resources.Load<ModelAsset>("encoder_model");
                    logMelSpectro = Resources.Load<ModelAsset>("logmel_spectrogram");
                    vocab = Resources.Load<TextAsset>("vocab");
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Asset loading failed: {ex.Message}");
            }

            // 모델 로드해서 Worker 객체에 할당 & GPU에서 모델을 실행
            decoder1 = new Worker(ModelLoader.Load(audioDecoder1), BackendType.GPUCompute);
            decoder2 = new Worker(ModelLoader.Load(audioDecoder2), BackendType.GPUCompute);
            encoder = new Worker(ModelLoader.Load(audioEncoder), BackendType.GPUCompute);
            spectrogram = new Worker(ModelLoader.Load(logMelSpectro), BackendType.GPUCompute);

            // 모델의 계산 그래프
            FunctionalGraph graph = new FunctionalGraph();
            var input = graph.AddInput(DataType.Float, new DynamicTensorShape(1, 1, 51865));
            var amax = Functional.ArgMax(input, -1, false);
            var selectTokenModel = graph.Compile(amax);
            argmax = new Worker(selectTokenModel, BackendType.GPUCompute);

            SetupWhiteSpaceShifts();
            // 토큰 load
            GetTokens();
        }

        // 모델에서 사용할 토큰 불러오기
        private static void GetTokens()
        {
            var vocab = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, int>>(SpeechToTextUnityModule.vocab.text);
            tokens = new string[vocab.Count];
            foreach (var item in vocab)
            {
                tokens[item.Value] = item.Key;
            }
        }
        // 공백문자 처리
        private static void SetupWhiteSpaceShifts()
        {
            for (int i = 0, n = 0; i < 256; i++)
            {
                if (IsWhiteSpace((char)i)) whiteSpaceCharacters[n++] = i;
            }
        }

        private static bool IsWhiteSpace(char c)
        {
            return !(('!' <= c && c <= '~') || ('�' <= c && c <= '�') || ('�' <= c && c <= '�'));
        }

        private static async Task LoadAudioToTensor()
        {
            numSamples = _audioClip.samples;
            var data = new float[maxSamples];
            numSamples = maxSamples;
            _audioClip.GetData(data, 0); 
            audioInput = new Tensor<float>(new TensorShape(1, numSamples), data);
        }

        // Audio 모델에 입력할 수 있는 형태로 변환
        private static void EncodeAudio()
        {
            spectrogram.Schedule(audioInput);
            var logmel = spectrogram.PeekOutput() as Tensor<float>;
            encoder.Schedule(logmel);
            encodedAudio = encoder.PeekOutput() as Tensor<float>;
        }

        private static async Awaitable<string> InferenceStep(string outputString)
        {
            decoder1.SetInput("input_ids", tokensTensor);
            decoder1.SetInput("encoder_hidden_states", encodedAudio);
            decoder1.Schedule();

            var past_key_values_0_decoder_key = decoder1.PeekOutput("present.0.decoder.key") as Tensor<float>;
            var past_key_values_0_decoder_value = decoder1.PeekOutput("present.0.decoder.value") as Tensor<float>;
            var past_key_values_1_decoder_key = decoder1.PeekOutput("present.1.decoder.key") as Tensor<float>;
            var past_key_values_1_decoder_value = decoder1.PeekOutput("present.1.decoder.value") as Tensor<float>;
            var past_key_values_2_decoder_key = decoder1.PeekOutput("present.2.decoder.key") as Tensor<float>;
            var past_key_values_2_decoder_value = decoder1.PeekOutput("present.2.decoder.value") as Tensor<float>;
            var past_key_values_3_decoder_key = decoder1.PeekOutput("present.3.decoder.key") as Tensor<float>;
            var past_key_values_3_decoder_value = decoder1.PeekOutput("present.3.decoder.value") as Tensor<float>;

            var past_key_values_0_encoder_key = decoder1.PeekOutput("present.0.encoder.key") as Tensor<float>;
            var past_key_values_0_encoder_value = decoder1.PeekOutput("present.0.encoder.value") as Tensor<float>;
            var past_key_values_1_encoder_key = decoder1.PeekOutput("present.1.encoder.key") as Tensor<float>;
            var past_key_values_1_encoder_value = decoder1.PeekOutput("present.1.encoder.value") as Tensor<float>;
            var past_key_values_2_encoder_key = decoder1.PeekOutput("present.2.encoder.key") as Tensor<float>;
            var past_key_values_2_encoder_value = decoder1.PeekOutput("present.2.encoder.value") as Tensor<float>;
            var past_key_values_3_encoder_key = decoder1.PeekOutput("present.3.encoder.key") as Tensor<float>;
            var past_key_values_3_encoder_value = decoder1.PeekOutput("present.3.encoder.value") as Tensor<float>;

            decoder2.SetInput("input_ids", lastTokenTensor);
            decoder2.SetInput("past_key_values.0.decoder.key", past_key_values_0_decoder_key);
            decoder2.SetInput("past_key_values.0.decoder.value", past_key_values_0_decoder_value);
            decoder2.SetInput("past_key_values.1.decoder.key", past_key_values_1_decoder_key);
            decoder2.SetInput("past_key_values.1.decoder.value", past_key_values_1_decoder_value);
            decoder2.SetInput("past_key_values.2.decoder.key", past_key_values_2_decoder_key);
            decoder2.SetInput("past_key_values.2.decoder.value", past_key_values_2_decoder_value);
            decoder2.SetInput("past_key_values.3.decoder.key", past_key_values_3_decoder_key);
            decoder2.SetInput("past_key_values.3.decoder.value", past_key_values_3_decoder_value);

            decoder2.SetInput("past_key_values.0.encoder.key", past_key_values_0_encoder_key);
            decoder2.SetInput("past_key_values.0.encoder.value", past_key_values_0_encoder_value);
            decoder2.SetInput("past_key_values.1.encoder.key", past_key_values_1_encoder_key);
            decoder2.SetInput("past_key_values.1.encoder.value", past_key_values_1_encoder_value);
            decoder2.SetInput("past_key_values.2.encoder.key", past_key_values_2_encoder_key);
            decoder2.SetInput("past_key_values.2.encoder.value", past_key_values_2_encoder_value);
            decoder2.SetInput("past_key_values.3.encoder.key", past_key_values_3_encoder_key);
            decoder2.SetInput("past_key_values.3.encoder.value", past_key_values_3_encoder_value);

            decoder2.Schedule(); // 학습

            var logits = decoder2.PeekOutput("logits") as Tensor<float>;
            argmax.Schedule(logits);
            using var t_Token = await argmax.PeekOutput().ReadbackAndCloneAsync() as Tensor<int>;
            int index = t_Token[0];

            outputTokens[tokenCount] = lastToken[0];
            lastToken[0] = index;
            tokenCount++;
            tokensTensor.Reshape(new TensorShape(1, tokenCount));
            tokensTensor.dataOnBackend.Upload<int>(outputTokens, tokenCount);
            lastTokenTensor.dataOnBackend.Upload<int>(lastToken, 1);

            string inferenceString = outputString;
            if (index == END_OF_TEXT)
            {
                isTranscribe = false;
            }
            else if (index < tokens.Length)
            {
                inferenceString += GetUnicodeText(tokens[index]);
            }
            return inferenceString;
        }

        private static string GetUnicodeText(string text)
        {
            var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(ShiftCharacterDown(text));
            return Encoding.UTF8.GetString(bytes);
        }

        private static string ShiftCharacterDown(string text)
        {
            string outText = "";
            foreach (char letter in text)
            {
                outText += ((int)letter <= 256) ? letter :
                    (char)whiteSpaceCharacters[(int)(letter - 256)];
            }
            return outText;
        }

        private static void DisposeAll()
        {
            audioInput?.Dispose();
            encodedAudio?.Dispose();

            DisposeToken(outputTokens);
            DisposeToken(lastToken);

            DisposeTensor(ref tokensTensor);
            DisposeTensor(ref lastTokenTensor);
        }

        private static void DisposeToken(NativeArray<int> tokens)
        {
            if (tokens.IsCreated)
                tokens.Dispose();
        }

        private static void DisposeTensor(ref Tensor<int> tensor)
        {
            if (tensor != null)
            {
                tensor.Dispose();
                tensor = null;
            }
        }

        public static void OnDestroy()
        {
            decoder1?.Dispose();
            decoder2?.Dispose();
            encoder?.Dispose();
            spectrogram?.Dispose();
            argmax?.Dispose();

            DisposeAll();
        }
        #endregion
    }
}

