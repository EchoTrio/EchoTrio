# EchoTrio

## Documentation Links
- API Documentation: https://echotrio.github.io/EchoTrio/  
- User Documentation: https://docs.google.com/document/d/18ge-kDuYu1pYe3VaC0oYd6mhCOw7E2qkABDeP8M4Ptw/edit?usp=sharing

## Opening the Unity Project
1. Ensure you have [Unity](https://docs.google.com/document/d/18ge-kDuYu1pYe3VaC0oYd6mhCOw7E2qkABDeP8M4Ptw/edit?usp=sharing) installed.
2. Clone this repository to your local machine.
3. Open the project with Unity, and open the `Playtest` scene.

## Authentication & API Keys
In order to run the game, you will need your OpenAI and ElevenLabs API Keys.  
**Note that because we are using GPT5, your OpenAI organisation will need to be [verified](https://help.openai.com/en/articles/10910291-api-organization-verification).**

1. Create a file named `Authentication.ini` in `Assets/StreamingAssets/Configs` and paste in the following template:
```
[OpenAI]
api_key = Your_API_Key
org_key = Your_Organisation_Key
proj_key = Your_Project_Key

[ElevenLabs]
api_key = Your_API_Key
```

2. Then, fill up the template with your API keys, which can be found in the following links:
    - OpenAI
        - API Key: https://platform.openai.com/api-keys
        - Organisation Key: https://platform.openai.com/settings/organization/general
        - Project Key: https://platform.openai.com/settings/project
    - ElevenLabs
        - API Key: https://elevenlabs.io/app/developers/api-keys

## ElevenLabs Voice IDs
You will also need to replace the Actor's ElevenLabs Voice IDs with your own, as they are configured by default to use the voices created in EchoTrio's account, which is not accessible to the public. Open the file `Assets/StreamingAssets/Configs/ActorOverrides.ini` and set the voice IDs of your choosing in the `elevenlabs_voice_id` variable for both Athena and Poseidon.

## Dependencies
- Unofficial OpenAI Package for Unity: https://github.com/RageAgainstThePixel/com.openai.unity
- Unofficial ElevenLabs Package for Unity: https://github.com/RageAgainstThePixel/com.rest.elevenlabs
- Spelunx ORBBEC SDK (Only accessible via CMU's WLAN): https://upm.etc.cmu.edu/-/web/detail/com.spelunx.cavern.orbbec.sdk
- Spelunx ORBBEC Library (Only accessible via CMU's WLAN): https://upm.etc.cmu.edu/-/web/detail/com.spelunx.cavern.orbbec.libs