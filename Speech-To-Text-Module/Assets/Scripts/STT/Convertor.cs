using System.Diagnostics;
using System.IO;
using UnityEngine;
using Cysharp.Threading.Tasks;

public class Convertor : MonoBehaviour
{
    [SerializeField, Range(0,100)] private int denoiseIntensity = 30;
    public void Convert()
    {
        ConvertAudioToText();
    }

    private async UniTask ConvertAudioToText()
    {
        string filePath = STTManager.Instance.FilePath;
        // 파일 존재하는지 검사
        if (!File.Exists(filePath))
        {
            UnityEngine.Debug.LogError("File Not Exist: " + filePath);
            return;
        }

        // 유효한 확장자인지 검사
        if (!IsValidAudioFormat(filePath))
        {
            UnityEngine.Debug.LogError("Invalid File Extension: " + Path.GetExtension(filePath));
            return;
        }

        // .mp4파일이면 .wav파일로 변환
        if (Path.GetExtension(filePath) == ".mp4")
        {
            string wavFilePath = await ConvertToWav(filePath);
            if (wavFilePath != null)
            {
                STTManager.Instance.SetFilePath(wavFilePath);
            }
            else
            {
                UnityEngine.Debug.LogError("Fail Convert .mp4 File to .wav");
                return;
            }                
        }

        // 노이즈 제거
        string noiseRemovedFilePath = await RemoveNoise(filePath);
        if (noiseRemovedFilePath != null)
        {
            STTManager.Instance.SetFilePath(noiseRemovedFilePath);
        }
        else
        {
            UnityEngine.Debug.LogError("Fail to Remove Noise");
        }

        // 길이가 길면 나누기


        // Processed Audio를 Text로 변환
        await WhisperManager.Instance.AskWhisper();
    }

    #region Audio Verification
    private bool IsValidAudioFormat(string filePath)
    {
        UnityEngine.Debug.Log("오디오 파일 형식 검증");

        string extension = Path.GetExtension(filePath);
        return ExtensionMethods.whisperExtensions.Contains(extension);
    }
    #endregion

    #region Audio Processing
    private async UniTask<bool> ExecuteFFmpegProcess(string argument, string outputPath)
    {
        // ffmpeg.exe 저장 위치
        string ffmpegPath = Path.Combine(Application.dataPath, "Plugins/ffmpeg/bin/ffmpeg.exe");

        // ProcessStartInfo 설정
        ProcessStartInfo startInfo = new ProcessStartInfo()
        {
            FileName = ffmpegPath, // 실행할 프로그램
            Arguments = argument, // 전달할 인자
            RedirectStandardOutput = true, // 외부 프로세스의 output c#으로 가져오기
            RedirectStandardError = true, // 외부 프로세스의 오류 c#으로 가져오기
            UseShellExecute = false, // 셸 사용 안하고 실행
            CreateNoWindow = true // 창 없이 실행
        };

        // ffmpeg 프로세스 시작
        Process process = new Process
        {
            StartInfo = startInfo
        };

        process.Start();
        process.WaitForExit(); // 변환 완료될 때까지 대기

        if (File.Exists(outputPath))
        {
            UnityEngine.Debug.Log($"Execute Success: {outputPath}");
            return true;
        }
        else
        {
            UnityEngine.Debug.LogError("Execute Failed");
            return false;
        }
    }

    private async UniTask<string> ConvertToWav(string filePath)
    {
        UnityEngine.Debug.Log(".wav 파일로 변환");

        string directoryPath = Path.Combine(Application.dataPath, "AudioProcessings");
        string baseFileName = Path.GetFileNameWithoutExtension(filePath);
        string outputPath = Path.Combine(directoryPath, baseFileName + ".wav");

        int count = 1;
        while (File.Exists(outputPath))
        {
            string newFileName = baseFileName + count.ToString(); 
            outputPath = Path.Combine(directoryPath, newFileName + ".wav");
            count++;  
        }

        // ffmpeg 명령어 (mp4 파일을 wav로 변환, whisper에서 권장하는 샘플레이트 16000, 모노채널 -ac 1)
        string arguments = $"-i \"{filePath}\" -vn -acodec pcm_s16le -ar 16000 -ac 1 \"{outputPath}\"";

        if (await ExecuteFFmpegProcess(arguments, outputPath))
            return outputPath;
        else
            return null;
    }

    private async UniTask<string> RemoveNoise(string filePath)
    {
        UnityEngine.Debug.Log("노이즈 제거");

        string directoryPath = Path.Combine(Application.dataPath, "AudioProcessings");
        string baseFileName = Path.GetFileNameWithoutExtension(filePath);
        string outputPath = Path.Combine(directoryPath, baseFileName + Path.GetExtension(filePath));

        int count = 1;
        while (File.Exists(outputPath))
        {
            string newFileName = baseFileName + count.ToString();
            outputPath = Path.Combine(directoryPath, newFileName + Path.GetExtension(filePath));
            count++;
        }

        // ffmpeg 명령어 (noise 제거)
        string arguments = $"-i \"{filePath}\" -vf noise=alls={denoiseIntensity}:allf=t \"{outputPath}\"";

        if (await ExecuteFFmpegProcess(arguments, outputPath))
            return outputPath;
        else
            return null;
    }

    #endregion
}
