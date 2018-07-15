using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Speech.Recognition;
using System.Globalization;
using System.Threading;
using System.Speech.Synthesis;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using Newtonsoft.Json;
using System.IO;

namespace OpenGameWPF
{
    /// <summary>
    /// MainWindow.xaml 的互動邏輯
    /// </summary>
    public partial class MainWindow : Window
    {
        SpeechRecognitionEngine sre;
        SpeechSynthesizer synthesizer;

        string cultrue = "zh-HK";

        string appDataPath = System.IO.Directory.GetCurrentDirectory() + " /appdata.json";

        List<AppLabel> appList;

        Dictionary<string, int> appGrammarDict;

        bool allowCallGame;

        string[] openGameGammer;

        // Inpupt simulator
        //[DllImport("user32.dll")]
        //public static extern void keybd_event(byte virtualKey, byte scanCode, uint flags, IntPtr extraInfo);

        string speakerName;

        // notify icon
        private System.Windows.Forms.NotifyIcon notifyicon;

        public MainWindow()
        {
            InitializeComponent();

            InitNotifyIcon();

            SizeChanged += Window_StateChanged;

            Loaded += FormLoaded;
        }

        protected override void OnStateChanged(EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.ShowInTaskbar = false;
                notifyicon.Text = "Open Game";
                notifyicon.Visible = true;
            }
            else if (this.WindowState == WindowState.Normal)
            {
                notifyicon.Visible = false;
                this.ShowInTaskbar = true;
            }

            base.OnStateChanged(e);
        }

        void FormLoaded(object sender, RoutedEventArgs e)
        {
            LoadAppData();

            Thread.CurrentThread.CurrentCulture = new CultureInfo(cultrue);

            openGameGammer = new string[] { "幫我開", "我想玩" };

            InitSpeechSynthesizer();

            InitSpeechRecognitionEngine();
        }

        void NotifyIcon_MouseDoubleClick(object sender, System.Windows.Forms.MouseEventArgs e)
        {
            this.WindowState = WindowState.Normal;
            this.Show();
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            if (this.WindowState == WindowState.Minimized)
            {
                this.ShowInTaskbar = false;
                notifyicon.BalloonTipTitle = "Minimize Sucessful";
                notifyicon.BalloonTipText = "Minimized the app ";
                notifyicon.ShowBalloonTip(400);
                notifyicon.Visible = true;
            }
            else if (this.WindowState == WindowState.Normal)
            {
                notifyicon.Visible = false;
                this.ShowInTaskbar = true;
            }
        }

        private void InitSpeechRecognitionEngine()
        {
            // Create a new SpeechRecognitionEngine instance.
            sre = new SpeechRecognitionEngine();

            // Configure the input to the recognizer.
            sre.SetInputToDefaultAudioDevice();

            Log("=== RecognizerInfo ===");
            Log(sre.RecognizerInfo.Culture.DisplayName);
            Log(sre.RecognizerInfo.Culture.CultureTypes.ToString());
            Log(sre.RecognizerInfo.Culture.TextInfo.ToString());
            Log(sre.RecognizerInfo.Description);

            // Create a simple grammar that recognizes "red", "green", or "blue".
            Choices commands = new Choices();
            commands.Add(openGameGammer);

            Choices games = new Choices();
            //games.Add(new string[] { "LoL", "pubg"});
            List<string> appGrammar = new List<string>();
            appGrammarDict = new Dictionary<string, int>();
            for (int i = 0; i < appList.Count; i++)
            {
                AppLabel app = appList[i];
                if (app.grammar != null && app.grammar.Count > 0)
                {
                    foreach (string call in app.grammar)
                    {
                        appGrammar.Add(call);
                        if (!appGrammarDict.ContainsKey(call))
                        {
                            appGrammarDict.Add(call, i);
                        }
                    }
                }
            }
            games.Add(appGrammar.ToArray());

            if (appGrammar.Count <= 0)
            {
                games.Add(new string[] { "none" });
            }

            GrammarBuilder callgb = new GrammarBuilder();
            callgb.Culture = new System.Globalization.CultureInfo(cultrue);
            callgb.Append(speakerName);            

            // Create a GrammarBuilder object and append the Choices object.
            GrammarBuilder gb = new GrammarBuilder();
            gb.Culture = new System.Globalization.CultureInfo(cultrue);
            gb.Append(commands);
            gb.Append(games);

            GrammarBuilder cmdgb = new GrammarBuilder();
            cmdgb.Culture = new System.Globalization.CultureInfo(cultrue);
            cmdgb.Append("關閉");

            // Create the Grammar instance and load it into the speech recognition engine.
            Grammar gcaller = new Grammar(callgb);
            sre.LoadGrammarAsync(gcaller);
            Grammar g = new Grammar(gb);
            sre.LoadGrammarAsync(g);
            Grammar cmdg = new Grammar(cmdgb);
            sre.LoadGrammarAsync(cmdg);

            // Register a handler for the SpeechRecognized event.
            sre.SpeechRecognized += sre_SpeechRecognized;

            // Start recognition.
            sre.RecognizeAsync(RecognizeMode.Multiple);

            Log("SpeechRecognitionEngine Loadded.");

            currentStatus.Text = "Hello, 我叫 " + speakerName;
            synthesizer.SpeakAsync(currentStatus.Text);
        }

        private void InitNotifyIcon()
        {
            notifyicon = new System.Windows.Forms.NotifyIcon();
            notifyicon.Icon = System.Drawing.SystemIcons.Application;
            notifyicon.MouseDoubleClick +=
                new System.Windows.Forms.MouseEventHandler
                    (NotifyIcon_MouseDoubleClick);
        }

        private void InitSpeechSynthesizer()
        {
            synthesizer = new SpeechSynthesizer();
            foreach (InstalledVoice voice in synthesizer.GetInstalledVoices(new CultureInfo(cultrue)))
            {
                VoiceInfo info = voice.VoiceInfo;
                OutputVoiceInfo(info);
                synthesizer.SelectVoice(info.Name);
            }
            Log("SpeechSynthesizer Loadded. ");
            string[] temp = synthesizer.Voice.Name.Split(' ');
            if (temp.Length >= 3)
                speakerName = temp[1];

            Log("Speaker : " + speakerName);
        }

        void LoadAppData()
        {
            if (!System.IO.File.Exists(appDataPath))
            {
                SearchSteamGame();
                return;
            }

            try
            {
                appList = JsonConvert.DeserializeObject<List<AppLabel>>(System.IO.File.ReadAllText(appDataPath));
            }
            catch(Exception e)
            {
                var done = MessageBox.Show(e.Message);
                appList = new List<AppLabel>();
            }

            Log("App Data JSON Loaded. ");
        }

        void SearchSteamGame()
        {
            appList = new List<AppLabel>();

            Log("Searching Steam Games... ");
            RegistryKey OurKey = Registry.CurrentUser;
            OurKey = OurKey.OpenSubKey(@"Software\\Valve\\Steam\\Apps", true);

            foreach (string keyname in OurKey.GetSubKeyNames())
            {
                //Log("Keyname : " + Keyname);
                RegistryKey key = OurKey.OpenSubKey(keyname);
                bool installed = false;
                if(key.GetValue("Installed") != null)
                {
                    installed = (int)(key.GetValue("Installed")) != 0;
                }
                string name = string.Empty;
                if (key.GetValue("Name") != null)
                {
                    name = key.GetValue("Name").ToString();
                }
                if (installed && !string.IsNullOrEmpty(name))
                {
                    Log(key.GetValue("Name").ToString() + "Added. ");
                    AppLabel app = new AppLabel();
                    app.name = name;
                    app.steamAppid = keyname;
                    app.grammar = new List<string>();
                    if (calledbyName.IsChecked.Value)
                    {
                        app.grammar.Add(name);                        
                    }
                    appList.Add(app);
                }
            }

            SaveAppSettings();

            Log("Searching Game Completed. ");
        }

        void SaveAppSettings()
        {
            string json = JsonConvert.SerializeObject(appList);
            System.IO.File.WriteAllText(appDataPath, json);
        }

        void sre_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            string result = e.Result.Text;
            float confidence = e.Result.Confidence;

            //RecognizedAudio audio = e.Result.Audio;
            //TimeSpan start = new TimeSpan(0);
            //TimeSpan duration = audio.Duration - start;
            //string path = @"/cmd.wav";
            //using (Stream outputStream = new FileStream(path, FileMode.Create))
            //{
            //    RecognizedAudio nameAudio = audio.GetRange(start, duration);
            //    nameAudio.WriteToWaveStream(outputStream);
            //    outputStream.Close();
            //}

            Log(confidence + "% > " + result + " ");

            if(result.IndexOf(speakerName) >= 0)
            {
                if (confidence < .94f)
                    return;

                allowCallGame = true;
                synthesizer.SpeakAsync("係");
                return;
            }

            bool isOpenGameCMD = false;
            for(int i=0; i< openGameGammer.Length; i++)
            {
                if (result.IndexOf(openGameGammer[i]) >= 0)
                {
                    isOpenGameCMD = true;
                    break;
                }
            }
            
            // Open game
            if ( allowCallGame && isOpenGameCMD)
            {
                if (confidence < .65f)
                    return;

                synthesizer.SpeakAsync("Okay");

                string target = result.Substring(3, result.Length - 3);
                AppLabel targetApp = appList[appGrammarDict[target]];
                currentStatus.Text = targetApp.name;

                if (!string.IsNullOrEmpty(targetApp.steamAppid))
                    System.Diagnostics.Process.Start("steam://run/" + targetApp.steamAppid);
                else if (!string.IsNullOrEmpty(targetApp.path))
                    System.Diagnostics.Process.Start(targetApp.path);

                Log("[ACT] Run " + targetApp.name);

                allowCallGame = false;
            }
            else if (allowCallGame && result.IndexOf("關閉") >= 0)
            {
                if (confidence < .65f)
                    return;

                Application.Current.Shutdown();
            }
        }

        // Media Control
        //public const int KEYEVENTF_EXTENTEDKEY = 1;
        //public const int KEYEVENTF_KEYUP = 0;
        //public const int VK_MEDIA_NEXT_TRACK = 0xB0;
        //public const int VK_MEDIA_PLAY_PAUSE = 0xB3;
        //public const int VK_MEDIA_PREV_TRACK = 0xB1;

        //void PlayNextSong()
        //{
        //    keybd_event(VK_MEDIA_PLAY_PAUSE, 0, KEYEVENTF_EXTENTEDKEY, IntPtr.Zero);
        //}

        //void PlayPrevSong()
        //{
        //    keybd_event(VK_MEDIA_PREV_TRACK, 0, KEYEVENTF_EXTENTEDKEY, IntPtr.Zero);
        //}

        private void OutputVoiceInfo(VoiceInfo info)
        {
            string cultureInfo = String.Format("Name: {0}, culture: {1}, gender: {2}, age: {3}. ",
              info.Name, info.Culture, info.Gender, info.Age);
            string cultureText = String.Format("    Description: {0} ", info.Description);
            Log(cultureInfo);
            Log(cultureText);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            SearchSteamGame();
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {

        }

        void Log(string msg)
        {
            logText.AppendText(msg + "\n");
            logText.ScrollToEnd();
        }
    }
}
