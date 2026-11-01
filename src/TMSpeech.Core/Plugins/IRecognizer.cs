using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TMSpeech.Core.Plugins
{
    public class TextInfo
    {
        public DateTime Time { get; set; }
        public string TimeStr => Time.ToString("T");
        public string Text { get; set; }

        /// <summary>
        /// Optional translation of <see cref="Text"/>. Null or empty when no
        /// translator is configured, so existing behaviour is unchanged.
        /// </summary>
        public string? Translation { get; set; }

        /// <summary>True when a non-empty translation is available.</summary>
        public bool HasTranslation => !string.IsNullOrEmpty(Translation);

        public TextInfo(string text)
        {
            Time = DateTime.Now;
            Text = text;
        }
    }

    public class SpeechEventArgs
    {
        public TextInfo Text { get; set; }
    }

    public interface IRecognizer : IPlugin, IRunable
    {
        event EventHandler<SpeechEventArgs> TextChanged;
        event EventHandler<SpeechEventArgs> SentenceDone;

        /// <summary>
        /// Feed audio data to the recognizer (e.g. from a microphone or a file
        /// </summary>
        /// <param name="data"></param>
        void Feed(byte[] data);
    }
}
