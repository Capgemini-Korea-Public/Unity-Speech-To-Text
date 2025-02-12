using System.Collections.Generic;

public static class ExtensionMethods
{
    // whisper에서 지원되는 오디오 확장자 목록
    public static readonly HashSet<string> whisperExtensions = new HashSet<string>
    {
        ".mp3", ".mp4", ".mpeg", ".mpga", ".m4a", ".wav", ".webm", "flac",  "oga", "ogg"
    };
}