# Unity-Speech-to-text-module

A Unity package that converts audio files to text directly within the Unity Engine.



## How to Use

This package provides two methods for converting audio to text:
- Using the OpenAI API
- Using the Unity Sentis model

To import this package, follow these steps:

1. In Unity, go to **Window > Package Manager**.
2. Click the **`+`** button and select **Add package from git URL**.
3. Paste the following repository URL https://github.com/Capgemini-Korea-Public/Unity-Speech-To-Text.git and click **Add**


And then, follow the steps below to set up **Sentis**, **OpenAI API**, and **FFmpeg** for the Unity Speech-to-Text module.


## Using the Sentis Model

To convert audio files using the Sentis model, follow these steps:

1. **Create a Resources Folder**  
- In your project's **Assets** folder, create a folder named exactly `Resources`.

2. **Download the Models and Data**  
- Visit [Sentis Whisper Tiny](https://huggingface.co/unity/sentis-whisper-tiny).
- Under the **Files and versions** tab, download the four ONNX models from the `Assets/Models` folder.
- Also, download the `vocab.json` file from the `Assets/Data` folder.

3. **Place the Files**  
- Move the `four downloaded model files` and the `vocab.json` file into your newly created `Assets/Resources` folder.

Your Sentis model setup is now complete.



## Installing the OpenAI API

To use the OpenAI API for audio conversion, you need an OpenAI account. Follow these steps:

1. Go to [https://openai.com/api](https://openai.com/api) and sign up for an account.
2. After creating your account, navigate to [https://beta.openai.com/account/api-keys](https://beta.openai.com/account/api-keys) to create a new secret key and save it.

Next, configure your local environment:

1. Create a folder named **.openai** in your user directory:
- For Windows: `C:\Users\<YourUserName>\`
- For Linux/Mac: `~/`
2. Inside the **.openai** folder, create a file named `auth.json`.
3. In `auth.json`, enter your API credentials in the following JSON format:

```json
{
    "api_key": "your api key name",
    "organization": "your organization name"
}
```

## Installing FFmpeg

This package uses FFmpeg to process audio files, so you must install FFmpeg on your computer before using the package.

### For Windows

1. Visit the [FFmpeg Downloads page](https://ffmpeg.org/download.html).
2. Click on **Windows** and then follow the link under **Windows EXE Files** for **Windows builds from gyan.dev**.
3. Download the FFmpeg build, extract the archive, and locate the `ffmpeg.exe` file in the `bin` folder.
4. In your Unity project, create a folder named **Plugins** inside the **Assets** folder.
5. Place `ffmpeg.exe` into the **Assets/Plugins** folder.

### For Mac

1. **Install Homebrew** (if not already installed)

   ```bash
   /bin/bash -c "$(curl -fsSL https://raw.githubusercontent.com/Homebrew/install/HEAD/install.sh)"
    ```
2. **Install FFmpeg using Homebrew** 

   ```bash
   brew install ffmpeg
    ```

3. Open the **AudioConvertor.cs**  file in your Unity project.
4. Search for the **ExecuteFFmpegProcess** method.
3. **Modify the FFmpeg path for macOS by ensuring the following code is correctly set** 
   ```csharp
    if (Application.platform == RuntimePlatform.OSXEditor || Application.platform == RuntimePlatform.OSXPlayer)
    {
        // Write your ffmpeg.dll Path
        ffmpegPath = "/usr/local/bin/ffmpeg";
    }
    ```

    Ensure the specified path (/usr/local/bin/ffmpeg) matches the actual installation location of FFmpeg on your Mac.