using UnityEditor;
using UnityEngine;

// Radio tracks arrive as 320 kbps MP3s. Re-encode them small enough for a browser download.
public class MusicImport : AssetPostprocessor
{
    void OnPreprocessAudio()
    {
        if (!assetPath.Contains("/Resources/Music/")) return;
        var imp = (AudioImporter)assetImporter;
        var s = imp.defaultSampleSettings;
        s.loadType = AudioClipLoadType.CompressedInMemory;
        s.compressionFormat = AudioCompressionFormat.Vorbis;
        s.quality = 0.3f;
        s.sampleRateSetting = AudioSampleRateSetting.OverrideSampleRate;
        s.sampleRateOverride = 32000;
        s.preloadAudioData = true;
        imp.defaultSampleSettings = s;
    }
}
