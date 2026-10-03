using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Threading;
using Windows.Graphics.Imaging;
using Windows.Media.Ocr;

namespace CEFRSubtitleMasker
{
    public partial class MainWindow : Window
    {
        const int WS_EX_TRANSPARENT = 0x00000020;
        const int GWL_EXSTYLE = (-20);
        const uint WDA_EXCLUDEFROMCAPTURE = 0x00000011;

        [DllImport("user32.dll")]
        static extern int GetWindowLong(IntPtr hwnd, int index);

        [DllImport("user32.dll")]
        static extern int SetWindowLong(IntPtr hwnd, int index, int newStyle);

        [DllImport("user32.dll")]
        static extern uint SetWindowDisplayAffinity(IntPtr hwnd, uint dwAffinity);

        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetDesktopWindow();

        [DllImport("user32.dll")]
        static extern IntPtr GetShellWindow();

        [DllImport("user32.dll")]
        static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        private OcrEngine ocrEngine;
        private DispatcherTimer captureTimer;
        
        private Dictionary<string, int> wordLevels;
        private int targetUserLevel; 

        private string lastValidText = string.Empty;
        private int emptyFrameCount = 0;
        private const int MaxEmptyFrames = 10;
        private bool isProcessing = false;

        public MainWindow(int selectedLevel)
        {
            InitializeComponent();
            
            targetUserLevel = selectedLevel; 

            wordLevels = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            LoadCEFRDictionary("cefr_words.csv");

            ocrEngine = OcrEngine.TryCreateFromLanguage(new Windows.Globalization.Language("en-US"));

            captureTimer = new DispatcherTimer();
            captureTimer.Interval = TimeSpan.FromMilliseconds(100);
            captureTimer.Tick += CaptureTimer_Tick;
        }

        private void LoadCEFRDictionary(string filePath)
        {
            if (!File.Exists(filePath)) return;

            var lines = File.ReadAllLines(filePath);
            foreach (var line in lines)
            {
                var parts = line.Split(',');
                if (parts.Length >= 2)
                {
                    string word = parts[0].Trim();
                    string levelStr = parts[1].Trim().ToUpper();
                    int levelVal = ConvertLevelToInt(levelStr);
                    
                    if (levelVal > 0 && !wordLevels.ContainsKey(word))
                    {
                        wordLevels[word] = levelVal;
                    }
                }
            }
        }

        private int ConvertLevelToInt(string level)
        {
            return level switch
            {
                "A1" => 1,
                "A2" => 2,
                "B1" => 3,
                "B2" => 4,
                "C1" => 5,
                "C2" => 6,
                _ => 0
            };
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            int extendedStyle = GetWindowLong(hwnd, GWL_EXSTYLE);
            SetWindowLong(hwnd, GWL_EXSTYLE, extendedStyle | WS_EX_TRANSPARENT);
            SetWindowDisplayAffinity(hwnd, WDA_EXCLUDEFROMCAPTURE);

            MaskBorder.Visibility = Visibility.Hidden;

            captureTimer.Start();
        }

        private bool IsForegroundFullScreen()
        {
            IntPtr hWnd = GetForegroundWindow();
            if (hWnd == IntPtr.Zero || hWnd == GetDesktopWindow() || hWnd == GetShellWindow())
            {
                return false;
            }

            GetWindowRect(hWnd, out RECT rect);

            int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
            int screenHeight = (int)SystemParameters.PrimaryScreenHeight;

            return (rect.Left <= 0 && rect.Top <= 0 && 
                    (rect.Right - rect.Left) >= screenWidth && 
                    (rect.Bottom - rect.Top) >= screenHeight);
        }

        private async void CaptureTimer_Tick(object? sender, EventArgs e)
        {
            if (isProcessing)
                return;

            if (!IsForegroundFullScreen())
            {
                MaskBorder.Visibility = Visibility.Hidden;
                ResultText.Text = string.Empty;
                lastValidText = string.Empty;
                emptyFrameCount = 0;
                return;
            }

            isProcessing = true;

            try
            {
                int screenWidth = (int)SystemParameters.PrimaryScreenWidth;
                int screenHeight = (int)SystemParameters.PrimaryScreenHeight;
                int roiHeight = (int)(screenHeight * 0.2);
                int roiY = screenHeight - roiHeight;

                using Bitmap bmp = new Bitmap(screenWidth, roiHeight, PixelFormat.Format32bppArgb);
                using Graphics g = Graphics.FromImage(bmp);
                g.CopyFromScreen(0, roiY, 0, 0, bmp.Size, CopyPixelOperation.SourceCopy);

                using MemoryStream ms = new MemoryStream();
                bmp.Save(ms, ImageFormat.Bmp);
                ms.Position = 0;

                var decoder = await BitmapDecoder.CreateAsync(ms.AsRandomAccessStream());
                var softwareBitmap = await decoder.GetSoftwareBitmapAsync();

                var ocrResult = await ocrEngine.RecognizeAsync(softwareBitmap);
                string currentText = ProcessOcrResult(ocrResult);

                if (string.IsNullOrWhiteSpace(currentText))
                {
                    emptyFrameCount++;
                    if (emptyFrameCount > MaxEmptyFrames)
                    {
                        lastValidText = string.Empty;
                        ResultText.Text = string.Empty;
                        MaskBorder.Visibility = Visibility.Hidden;
                    }
                }
                else
                {
                    emptyFrameCount = 0;
                    MaskBorder.Visibility = Visibility.Visible;

                    if (CalculateSimilarity(currentText, lastValidText) < 0.85)
                    {
                        lastValidText = currentText;
                        ResultText.Text = lastValidText;
                    }
                }
            }
            finally
            {
                isProcessing = false;
            }
        }

        private string ProcessOcrResult(OcrResult ocrResult)
        {
            if (ocrResult == null || ocrResult.Lines.Count == 0)
                return string.Empty;

            List<string> currentRawTokens = new List<string>();
            
            foreach (var line in ocrResult.Lines)
            {
                string previousWord = string.Empty;

                foreach (var word in line.Words)
                {
                    string originalWord = word.Text;
                    string cleanWord = Regex.Replace(originalWord, @"[^\w]", "").ToLower();

                    if (originalWord.Contains(">>") || originalWord.Contains("[") || originalWord.Contains("]") || (string.IsNullOrWhiteSpace(cleanWord) && originalWord.Length < 2))
                    {
                        continue;
                    }

                    if (cleanWord == previousWord && !string.IsNullOrWhiteSpace(cleanWord))
                    {
                        continue;
                    }

                    currentRawTokens.Add(originalWord);
                    previousWord = cleanWord;
                }
                currentRawTokens.Add("\n");
            }

            if (currentRawTokens.Count > 0 && currentRawTokens[currentRawTokens.Count - 1] == "\n")
            {
                currentRawTokens.RemoveAt(currentRawTokens.Count - 1);
            }

            StringBuilder sb = new StringBuilder();
            foreach (var token in currentRawTokens)
            {
                if (token == "\n")
                {
                    sb.AppendLine();
                }
                else
                {
                    string cleanWordToCheck = Regex.Replace(token, @"[^\w]", "");
                    
                    if (wordLevels.TryGetValue(cleanWordToCheck, out int wordLevel))
                    {
                        if (wordLevel <= targetUserLevel)
                        {
                            sb.Append(new string('█', token.Length) + " ");
                        }
                        else
                        {
                            sb.Append(token + " ");
                        }
                    }
                    else
                    {
                        sb.Append(token + " ");
                    }
                }
            }

            return sb.ToString().Replace(" \r\n", "\r\n").TrimEnd();
        }

        private double CalculateSimilarity(string source, string target)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(target))
                return 0;

            if (source == target)
                return 1.0;

            int stepsToSame = ComputeLevenshteinDistance(source, target);
            return 1.0 - ((double)stepsToSame / Math.Max(source.Length, target.Length));
        }

        private int ComputeLevenshteinDistance(string source, string target)
        {
            int n = source.Length;
            int m = target.Length;
            int[,] d = new int[n + 1, m + 1];

            if (n == 0) return m;
            if (m == 0) return n;

            for (int i = 0; i <= n; d[i, 0] = i++) { }
            for (int j = 0; j <= m; d[0, j] = j++) { }

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = (target[j - 1] == source[i - 1]) ? 0 : 1;
                    d[i, j] = Math.Min(
                        Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                        d[i - 1, j - 1] + cost);
                }
            }
            return d[n, m];
        }
    }
}