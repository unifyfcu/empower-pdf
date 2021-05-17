using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

namespace empower_pdf
{
    /// <summary>
    /// https://gist.github.com/DanielSWolf/0ab6a96899cc5377bf54
    /// An ASCII progress bar
    /// http://opensource.org/licenses/MIT
    /// </summary>
    public class ProgressBar : TextWriter, IDisposable, IProgress<double>
    {
        private const string _hook = "\u129b";
        private StringWriter _captured;
        private TextWriter _writer;
        private const int blockCount = 100;
        private readonly TimeSpan animationInterval = TimeSpan.FromSeconds(1.0 / 8);
        private readonly char[] animation = { '|', '/', '-', '\\' };

        private readonly Timer timer;

        private int _count = 0;
        private int _maximum = int.MaxValue;
        private double currentProgress = 0;
        private string currentText = string.Empty;
        private bool disposed = false;
        private int animationIndex = 0;

        public ProgressBar(int max) : this() => _maximum = max;

        public ProgressBar()
        {
            _captured = new StringWriter();
            _writer = Console.Out;
            Console.SetOut(this);
            timer = new Timer(TimerHandler);

            // A progress bar is only for temporary display in a console window.
            // If the console output is redirected to a file, draw nothing.
            // Otherwise, we'll end up with a lot of garbage in the target file.
            if (!Console.IsOutputRedirected)
            {
                ResetTimer();
            }
        }

        /// <inheritdoc />
        public override void Write(string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            var previous = _captured.ToString().Split(Environment.NewLine).Last();
            if (previous == value) return;
            if (string.IsNullOrEmpty(previous))
            {
                _writer.Write(value);
                _captured.Write(value);
                return;
            }

            if (previous.StartsWith(_hook))
            {
                Console.SetCursorPosition(0, Console.CursorTop);
                value = value.TrimStart(Environment.NewLine.ToCharArray());
                if (string.IsNullOrEmpty(value)) return;
                var content = value;
                if (!value.StartsWith(_hook))
                    content = value.EndsWith(Environment.NewLine)
                        ? $"{value}{currentText}"
                        : $"{value}{Environment.NewLine}{currentText}";
                _writer.Write(content);
                _captured.Write(content);
                return;
            }

            _writer.Write(value);
            _captured.Write(value);
        }

        /// <inheritdoc />
        public override void WriteLine()
        {
            Console.SetCursorPosition(0, Console.CursorTop);
            _writer.WriteLine($"{Environment.NewLine}{currentText}");
        }

        /// <inheritdoc />
        public override Encoding Encoding => _writer.Encoding;

        public void Report(double value)
        {
            // Make sure value is in [0..1] range
            value = Math.Max(0, Math.Min(1, value));
            Interlocked.Exchange(ref currentProgress, value);
        }

        public void Report()
        {
            _count++;
            var value = Math.Max(0, Math.Min(1, (double)_count / _maximum));
            Report(value);
        }

        private void TimerHandler(object state)
        {
            lock (timer)
            {
                if (disposed) return;


                int progressBlockCount = (int)(currentProgress * blockCount);
                int percent = (int)(currentProgress * 100);
                var builder = new StringBuilder();
                builder.Append($"[{new string('#', progressBlockCount)}");
                builder.Append($"{new string('-', blockCount - progressBlockCount)}] ");
                builder.Append($"{percent}% ");
                builder.Append(animation[animationIndex++ % animation.Length]);
                UpdateText(builder.ToString());
                ResetTimer();
            }
        }

        private void UpdateText(string text)
        {
            if (!text.StartsWith(_hook))
                text = _hook + text;
            currentText = text;
            Console.Write(currentText);
        }

        private void ResetTimer()
        {
            timer.Change(animationInterval, TimeSpan.FromMilliseconds(-1));
        }

        public new void Dispose()
        {
            lock (timer)
            {
                UpdateText($"{currentText}\n");
                _captured?.Dispose();
                Console.SetOut(_writer);
                disposed = true;
            }
        }

        void IDisposable.Dispose() => Dispose();
    }
}
