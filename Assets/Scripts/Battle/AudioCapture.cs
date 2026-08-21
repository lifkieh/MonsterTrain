using System.IO;
using UnityEngine;

namespace MTA.Battle
{
    // Records the final mixed audio (component sits on the AudioListener GameObject) to a 16-bit PCM
    // WAV. Dev/review tooling ONLY — used to VERIFY that SFX actually fire and that element impacts
    // sound different (envelope + spectrum analysis of the file). Never in normal play. Cosmetic.
    public class AudioCapture : MonoBehaviour
    {
        float[] _buf; int _idx; int _rate = 48000; int _channels = 2; volatile bool _on;

        public void Begin(float seconds)
        {
            _rate = AudioSettings.outputSampleRate;
            _channels = 2;
            _buf = new float[(int)(_rate * _channels * seconds)];
            _idx = 0; _on = true;
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (!_on || _buf == null) return;
            _channels = channels;
            int idx = _idx;
            for (int i = 0; i < data.Length && idx < _buf.Length; i++) _buf[idx++] = data[i];
            _idx = idx;
        }

        public void Write(string path)
        {
            _on = false;
            if (_buf == null) return;
            int samples = Mathf.Min(_idx, _buf.Length);
            try
            {
                using (var fs = new FileStream(path, FileMode.Create))
                using (var w = new BinaryWriter(fs))
                {
                    int byteRate = _rate * _channels * 2;
                    int dataBytes = samples * 2;
                    w.Write(new[] { 'R', 'I', 'F', 'F' });
                    w.Write(36 + dataBytes);
                    w.Write(new[] { 'W', 'A', 'V', 'E' });
                    w.Write(new[] { 'f', 'm', 't', ' ' });
                    w.Write(16); w.Write((short)1); w.Write((short)_channels);
                    w.Write(_rate); w.Write(byteRate); w.Write((short)(_channels * 2)); w.Write((short)16);
                    w.Write(new[] { 'd', 'a', 't', 'a' });
                    w.Write(dataBytes);
                    for (int i = 0; i < samples; i++)
                        w.Write((short)(Mathf.Clamp(_buf[i], -1f, 1f) * 32767f));
                }
            }
            catch { }
        }
    }
}
