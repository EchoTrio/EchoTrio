# EchoTrio

## Documentation Links
- API Documentation: https://echotrio.github.io/EchoTrio/  
- User Documentation: https://docs.google.com/document/d/18ge-kDuYu1pYe3VaC0oYd6mhCOw7E2qkABDeP8M4Ptw/edit?usp=sharing

## Running the Game
### Opening the Unity Project
1. Ensure you have [Unity 6.2](https://docs.google.com/document/d/18ge-kDuYu1pYe3VaC0oYd6mhCOw7E2qkABDeP8M4Ptw/edit?usp=sharing) or higher installed.
2. Clone this repository to your local machine.
3. Open the project with Unity, and open the `Playtest` scene.
4. Ensure you have 3 Game windows opened in Unity and set them to Display 1, Display 2, Display 3 respectively.

### Authentication & API Keys
In order to run the game, you will need your OpenAI and ElevenLabs API Keys.  
**Note that because we are using GPT5, your OpenAI organisation will need to be [verified](https://help.openai.com/en/articles/10910291-api-organization-verification).**

1. Create a file named `Authentication.ini` in `Assets/StreamingAssets/Configs` and paste in the following template:
```
; Do not put quotation marks "" around your values.

[OpenAI]
api_key = Your_API_Key
org_id = Your_Organisation_ID
proj_id = Your_Project_ID

[ElevenLabs]
api_key = Your_API_Key
```

2. Then, fill up the template with your API keys and IDs, which can be found in the following links:
    - OpenAI
        - API Key: https://platform.openai.com/api-keys
        - Organisation ID: https://platform.openai.com/settings/organization/general
        - Project ID: https://platform.openai.com/settings/project
    - ElevenLabs
        - API Key: https://elevenlabs.io/app/developers/api-keys

### ElevenLabs Voice IDs
You will also need to replace the Actor's ElevenLabs Voice IDs with your own, as they are configured by default to use the voices created in EchoTrio's account, which is not accessible to the public. Open the file `Assets/StreamingAssets/Configs/ActorOverrides.ini` and set the voice IDs of your choosing in the `elevenlabs_voice_id` variable for both Athena and Poseidon. You can find a list of default available voices here: https://elevenlabs.io/app/voice-library , and you can right-click the three dots to copy the voice ID of your preferred voice line.

## Dependencies
The following dependencies have already been configured in Unity's Package Manager:
- Unofficial OpenAI Package for Unity: https://github.com/RageAgainstThePixel/com.openai.unity  
- Unofficial ElevenLabs Package for Unity: https://github.com/RageAgainstThePixel/com.rest.elevenlabs

The following dependencies have been directly placed in the `Packages` folder:
- Spelunx ORBBEC SDK to detect if a user has walked up to the booth: https://upm.etc.cmu.edu/-/web/detail/com.spelunx.cavern.orbbec.sdk (Link only accessible via CMU's WLAN.)

The following dependencies are automatically downloaded as a dependency. (Must be connected to CMU's WLAN!)
- Spelunx ORBBEC Library for the binary dependencies needed for the ORBBEC Femto Bolt: https://upm.etc.cmu.edu/-/web/detail/com.spelunx.cavern.orbbec.libs (Link only accessible via CMU's WLAN.)

### IMPORTANT NOTES WHEN BUILDING YOUR .EXE!
When you build your executable, you have to copy the following ORBBEC binaries into your build folder too! They will be found in your root folder if you have the Spelunx ORBBEC Library downloaded.
- /directml.dll
- /dnn_model_2_0_op11.onnx
- /onnxruntime.dll
- /onnxruntime_providers_cuda.dll
- /onnxruntime_providers_shared.dll
- /onnxruntime_providers_tensorrt.dll
