using System.Collections.Generic;
using UnityEngine;
using Unity.Sentis;
using System.Text;
using Unity.Collections;
using Cysharp.Threading.Tasks;
using UnityEngine.Networking;
using System.IO;

public class SentisWhisperManager : Singleton<SentisWhisperManager>
{
    [Header("Sentis 모델")]
    [SerializeField] private ModelAsset audioDecoder1;
    [SerializeField] private ModelAsset audioDecoder2;
    [SerializeField] private ModelAsset audioEncoder;
    [SerializeField] private ModelAsset logMelSpectro; // 음성 인식에 사용되는 모델

    [Header("변환할 Audio File")]
    [SerializeField] private AudioClip audioClip;

    [Header("Tokenizer")]
    [SerializeField] private TextAsset jsonFile;

    [Header("Output")]
    [SerializeField] private string outputString = "";

    Worker decoder1, decoder2, encoder, spectrogram;
    Worker argmax;

    // 처리 할 최대 토큰 수
    const int maxTokens = 1000;

    // 모델에서 사용할 토큰
    const int END_OF_TEXT = 50257; // 텍스트의 끝을 나타낼 토큰
    const int START_OF_TRANSCRIPT = 50258; // 텍스트 변환 시작 지점
    const int ENGLISH = 50259; // 영어를 나타낼 토큰
    const int TRANSCRIBE = 50359; // 지정된 언어로 음성 텍스트 변환 
    const int TRANSLATE = 50358;  // 영어로 음성 텍스트 변환 
    const int NO_TIME_STAMPS = 50363; // 타임스탬프 없이 텍스트 변환

    int numSamples; // 오디오의 샘플 수
    string[] tokens; // Whisper 모델에서 사용하는 토큰들을 저장하는 배열

    int tokenCount = 0; // 현재까지 처리된 토큰의 수
    NativeArray<int> outputTokens; //모델로부터 나온 결과 토큰을 저장하는 배열

    // 특수문자 Decoding에 사용
    int[] whiteSpaceCharacters = new int[256];

    Tensor<float> encodedAudio;
    Awaitable m_Awaitable;

    NativeArray<int> lastToken;
    Tensor<int> lastTokenTensor;
    Tensor<int> tokensTensor;
    Tensor<float> audioInput;

    private bool transcribe = false;

    // 오디오 최대 사이즈 지정 (30초 at 16kHz)
    private const int maxSamples = 30 * 16000;

    private void Start()
    {
        Init();
    }

    #region Init
    private void Init()
    {
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
    private void GetTokens()
    {
        var vocab = Newtonsoft.Json.JsonConvert.DeserializeObject<Dictionary<string, int>>(jsonFile.text);
        tokens = new string[vocab.Count];
        foreach (var item in vocab)
        {
            tokens[item.Value] = item.Key;
        }
    }
    // 공백문자 처리
    private void SetupWhiteSpaceShifts()
    {
        for (int i = 0, n = 0; i < 256; i++)
        {
            if (IsWhiteSpace((char)i)) whiteSpaceCharacters[n++] = i;
        }
    }

    private bool IsWhiteSpace(char c)
    {
        return !(('!' <= c && c <= '~') || ('�' <= c && c <= '�') || ('�' <= c && c <= '�'));
    }
    #endregion

    [ContextMenu("AskSentisWhisper")]
    public async UniTask AskSentisWhisper()
    {
        if (transcribe) return; // 중복 실행 방지
        DisposeAll();
        transcribe = true;

        outputString = "";

        outputTokens = new NativeArray<int>(maxTokens, Allocator.Persistent);
        outputTokens[0] = START_OF_TRANSCRIPT;
        outputTokens[1] = ENGLISH;
        outputTokens[2] = TRANSCRIBE; // TRANSLATE; //
        //outputTokens[3] = NO_TIME_STAMPS;// START_TIME;//
        tokenCount = 3;

        await LoadAudio();

        tokensTensor = new Tensor<int>(new TensorShape(1, maxTokens));
        ComputeTensorData.Pin(tokensTensor);
        tokensTensor.Reshape(new TensorShape(1, tokenCount));
        tokensTensor.dataOnBackend.Upload<int>(outputTokens, tokenCount);

        lastToken = new NativeArray<int>(1, Allocator.Persistent); lastToken[0] = NO_TIME_STAMPS;
        lastTokenTensor = new Tensor<int>(new TensorShape(1, 1), new[] { NO_TIME_STAMPS });

        // 토큰의 개수가 최대에 달할 때 까지 음성 인식 작업 지속
        while (true)
        {
            if (!transcribe || tokenCount >= (outputTokens.Length - 1))
            {
                Debug.Log(outputString);
                ExtensionMethods.RemoveProcessedAudioFile();
                return;
            }
            m_Awaitable = InferenceStep();
            await m_Awaitable;
        }
    }

    #region Audio
    private async UniTask LoadAudio()
    {
        string filePath = "file://" + STTManager.Instance.FilePath;
        string fileExtension = Path.GetExtension(filePath).ToLower();

        AudioType audioType = GetAudioType(fileExtension);
        if (audioType == AudioType.UNKNOWN)
        {
            Debug.LogError("Unsupported AudioType: " + fileExtension);
            return;
        }

        using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(filePath, audioType))
        {
            await www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                audioClip = DownloadHandlerAudioClip.GetContent(www);
                Debug.Log("Successfully Audio Load ");
                LoadAudioToTensor();
                EncodeAudio();
            }
            else
            {
                Debug.LogError("Failed Audio Load: " + www.error);
            }
        }
    }

    private void LoadAudioToTensor()
    {
        numSamples = audioClip.samples;
        var data = new float[maxSamples];
        numSamples = maxSamples;
        audioClip.GetData(data, 0);
        audioInput = new Tensor<float>(new TensorShape(1, numSamples), data);
    }

    // Audio 모델에 입력할 수 있는 형태로 변환
    private void EncodeAudio()
    {
        spectrogram.Schedule(audioInput);
        var logmel = spectrogram.PeekOutput() as Tensor<float>;
        encoder.Schedule(logmel);
        encodedAudio = encoder.PeekOutput() as Tensor<float>;
    }

    private AudioType GetAudioType(string extension)
    {
        switch (extension)
        {
            case ".mp3":
            case ".mpeg":
                return AudioType.MPEG;
            case ".wav":
                return AudioType.WAV;
            case ".ogg":
                return AudioType.OGGVORBIS;
            default:
                return AudioType.UNKNOWN; 
        }
    }

    #endregion

    #region  Inference
    private async Awaitable InferenceStep()
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

        if (index == END_OF_TEXT)
        {
            transcribe = false;
        }
        else if (index < tokens.Length)
        {
            outputString += GetUnicodeText(tokens[index]);
        }
        STTManager.Instance.SetConvertedText(outputString);
    }

    private string GetUnicodeText(string text)
    {
        var bytes = Encoding.GetEncoding("ISO-8859-1").GetBytes(ShiftCharacterDown(text));
        return Encoding.UTF8.GetString(bytes);
    }

    private string ShiftCharacterDown(string text)
    {
        string outText = "";
        foreach (char letter in text)
        {
            outText += ((int)letter <= 256) ? letter :
                (char)whiteSpaceCharacters[(int)(letter - 256)];
        }
        return outText;
    }

    #endregion

    #region Dispose
    private void DisposeAll()
    {
        audioInput?.Dispose();
        encodedAudio?.Dispose();

        DisposeToken(outputTokens);
        DisposeToken(lastToken);

        DisposeTensor(ref tokensTensor);
        DisposeTensor(ref lastTokenTensor);
    }

    private void DisposeToken(NativeArray<int> tokens)
    {
        if (tokens.IsCreated)
            tokens.Dispose();
    }

    private void DisposeTensor(ref Tensor<int> tensor)
    {
        if (tensor != null)
        {
            tensor.Dispose();
            tensor = null;
        }
    }

    private void OnDestroy()
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