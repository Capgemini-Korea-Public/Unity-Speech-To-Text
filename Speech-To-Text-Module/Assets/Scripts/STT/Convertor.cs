using OpenAI;
using System.Diagnostics;
using System.IO;
using UnityEngine;

public class Convertor : MonoBehaviour
{
    public void ConvertAudioToText()
    {
        string filePath = STTManager.Instance.FilePath;
        if (!File.Exists(filePath))
        {
            UnityEngine.Debug.LogError("File Not Exist: " + filePath);
            return;
        }

        if (!IsValidAudioFormat(filePath))
        {
            UnityEngine.Debug.LogError("Invalid File Extension: " + Path.GetExtension(filePath));
            return;
        }

        if (Path.GetExtension(filePath) == ".mp4")
        {
            string wavFilePath = ConvertToWav(filePath);
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

        WhisperManager.Instance.AskWhisper();
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
    private string ConvertToWav(string filePath)
    {
        UnityEngine.Debug.Log(".wav 파일로 변환");

        string outputPath = Path.Combine(Application.dataPath, "AudioProcessings");
        string outputWavPath = Path.Combine(outputPath, Path.GetFileNameWithoutExtension(filePath) + ".wav");

        // ffmpeg.exe 저장 위치
        string ffmpegPath = Path.Combine(Application.dataPath, "Plugins/ffmpeg/bin/ffmpeg.exe");

        // ffmpeg 명령어 (mp4 파일을 wav로 변환, whisper에서 권장하는 샘플레이트 16000, 모노채널 -ac 1)
        string arguments = $"-i \"{filePath}\" -vn -acodec pcm_s16le -ar 16000 -ac 1 \"{outputWavPath}\"";

        // ProcessStartInfo 설정
        ProcessStartInfo startInfo = new ProcessStartInfo()
        {
            FileName = ffmpegPath, // 실행할 프로그램
            Arguments = arguments, // 전달할 인자
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

        UnityEngine.Debug.Log(outputWavPath);

        // 변환 완료 후 메시지 출력
        if (File.Exists(outputWavPath))
        {
            UnityEngine.Debug.Log($"변환 완료: {outputWavPath}");
            return outputWavPath;
        }
        else
        {
            UnityEngine.Debug.LogError("변환 실패");
            return null;
        }
    }
    #endregion
}
