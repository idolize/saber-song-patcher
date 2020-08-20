<img align="right" width="130" height="130" src="Images/patcher.png?raw=true" alt="">

# Saber Song Patcher

> Add and distribute custom songs for Beat Saber without including any copyrighted content!

## Background

Read [this blog post by Shane Monroe](https://medium.com/@darkuni/beat-saber-why-custom-maps-cannot-be-made-with-legal-music-9e68a01cfd42) for a good description of the problem.

**Summary:**

- Most custom Beat Saber songs distributed currently include copyrighted song data
- This is partially due to the fact it is very difficult to guarantee each player has the right copy of the song to fit the timings of the matching beat map
- Songs may also have timings tweaked, silence added, etc.
- Additionally, the song must be in a format that Beat Saber understands (Ogg Vorbis codec)

## Solution

This tool comes with both a commandline and a GUI application to assist in (1) distributing custom maps without copyrighted data and (2) quickly and reliably using these maps by providing your own legally purchases/ripped copy of the song

Here's the process:

1. 🎧 **Mapper** selects a "master" track for the song they are working on
2. 🎛️ **Mapper** tweaks the timing of the track by trimming, fading in/out, and/or adding silence to the master song
    - This is done via a few lines in the configuration file and can be tested/tweaked as needed
3. ✌️ **Mapper** runs the **`fingerprint` operation** to store some information about the master track
    - Includes the length of the song, a SHA-256 hash of the file, and an [audio fingerprint](https://www.codeproject.com/Articles/206507/Duplicates-detector-via-audio-fingerprinting#fingerprint)
    - In addition to downloading the usual map `.dat` files, and in place of the usual `.ogg`/`.egg` file, we can add the new `audio.json` and `fingerprint.bin`

<br />

4. ⚖️ **User** downloads these files, as well as purchasing or ripping a legal copy of the song
5. 🩹 **User** runs **`patch` operation** (or uses the GUI tool) to verify that their copy of the song will work with the map, patch the timings accoring to the config, and finally ensure that the song is converted to `.ogg` format!
    - If the user provides the exact same file as the original master track then the SHA-256 hash will match ✅
    - If the user provides the same exact version of the song but in a different format or bitrate (e.g. purchasing through iTunes instead of Amazon) then the audio fingerprint will still match ✅
    - If the user provides the same song but with incorrect timings the match will fail ❌
    - If the user provides a different rendition of the song the match will fail ❌

## Screenshots

<p align="center">
 <img src="Images/gui-screenshot.png?raw=true" title="GUI Patcher" alt="Screenshot">
</p>

<p align="center">
 <img src="Images/cli-help.png?raw=true" title="CLI Help" alt="Screenshot">
</p>

<p align="center">
 <img src="Images/cli-screenshot.png?raw=true" title="CLI Example" alt="Screenshot">
</p>

## Config format

```js
{
  // Version of the schema
  "schemaVersion": 1,
  // Length of the master song (autopopulated by `fingerprint`)
  "lengthMs": 12345,
  // Any notes for users about the song (optional)
  "notes": "Please download version X from Amazon Music",
  // Where to buy a copy of the song in the correct format (optional)
  "downloadUrls": [
    "https://www.amazon.com/dp/B08FCZ6J55/"
  ],
  // Hashes of song files that are guaranteed to work with the map (optional - at least one added by `fingerprint`)
  "knownGoodHashes": [
    { "type": "sha256", "hash": "xxxxx" }
  ],
  // Audio changes to apply to the master track before use with the map
  "patches": {
    "delayStartMs": 5000,
    "padEndMs": 5000,
    "trim": {
      "startMs": 0,
      "endMs": 30000
    },
    "fadeIn": { "startMs": 0, "durationMs": 10000 },
    "fadeOut": { "startMs": 20000, "durationMs": 10000 }
  }
}
```

## Example CLI commands

Save fingerprint information for a master track

```cmd
rem Supported formats: .mp3, .m4a, .flac, .wma, .wav, etc
SaberSongPatcherCLI fingerprint --master MasterAudioFile.mp3
```

Verify and patch input audio

```cmd
SaberSongPatcherCLI patch --input AnotherAudioFile.m4a
```

View help for a command

```cmd
SaberSongPatcherCLI patch --help
```
