# 🎼 Saber Song Patcher

Finally an easy way to add and distribute custom songs for Beat Saber without including any copyrighted content!

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
1. ✌️ **Mapper** runs the `fingerprint` operation to store some information about the master track
    - Includes the length of the song, a SHA-256 hash of the file, and an [audio fingerprint](https://www.codeproject.com/Articles/206507/Duplicates-detector-via-audio-fingerprinting#fingerprint)
1. 🎛️ **Mapper** tweaks the timing of the track by trimming, fading in/out, and/or adding silence to the master song
    - This is done via a few lines in the configuration file and can be tested/tweaked as needed
1. 📊 *Now the configuration file and the fingerprint file together give us a lot of precise information about the "master" song, as well as how we can reproducibly transform that song to match the mapper's needs*
    - In addition to downloading the usual map `.dat` files, and in place of the usual `.ogg`/`.egg` file, we can add the new `audio.json` and `fingerprint.bin`
1. ⚖️ **User** downloads these files, as well as purchasing or ripping a legal copy of the song
1. 🩹 **User** runs `patch` operation (or uses the GUI tool) to verify that their copy of the song will work with the map, patch the timings accoring to the config, and finally ensure that the song is converted to `.ogg` format!
    - If the user provides the exact same file as the original master track then the SHA-256 hash will be the same ✅
    - If the user provides the same exact version of the song but in a different format or bitrate (e.g. purchasing through iTunes instead of Amazon) then the audio fingerprint will still match ✅
    - If the user provides the same song but with incorrect timings the match will fail ❌
    - If the user provides a different rendition of the song the match will fail ❌

## Screenshots

![Screenshot](Images/gui-screenshot.png?raw=true "GUI Patcher")

![Screenshot](Images/cli-screenshot.png?raw=true "CLI Example")

![Screenshot](Images/cli-help.png?raw=true "CLI Help")

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