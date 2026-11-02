using UnityEditor;
using UnityEngine;

// Radio tracks arrive as 320 kbps MP3s. Re-encode them small enough for a browser download, and
// never preload: a browser decodes a whole song into raw samples (about 45 MB for four minutes of
// mono), so loading all six at once is enough to make a phone kill the page. Radio loads one at a time.
public class MusicImport : AssetPostprocessor
{
    // Bump when these settings change, so Unity re-imports the songs with them.
    public override uint GetVersion() => 2;

    void OnPreprocessAudio()
    {
        if (!assetPath.Contains("/Resources/Music/")) return;
        var imp = (AudioImporter)assetImporter;
        imp.forceToMono = true;
        var s = imp.defaultSampleSettings;
        s.loadType = AudioClipLoadType.CompressedInMemory;
        s.compressionFormat = AudioCompressionFormat.Vorbis;
        s.quality = 0.3f;
        s.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
        s.sampleRateOverride = 32000;
        s.preloadAudioData = false;
        imp.defaultSampleSettings = s;
    }
}
