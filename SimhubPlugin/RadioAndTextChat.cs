using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Threading;
using System.Windows.Forms;
using NAudio;
using NAudio.Wave;
using System.Speech.Synthesis;

using vJoyInterfaceWrap;

using NAudio.CoreAudioApi;

using NAudio.Dmo;

namespace APR.DashSupport {
    internal class RadioAndTextChat {
        [DllImport("user32.dll")]
        public static extern IntPtr PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")]
        static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int SW_SHOWMAXIMIZED = 3;

        public static void iRacingChat(string text, bool asAdmin = false) {

            if (asAdmin) {
                text = "/all " + text; 
            }

            const uint WM_KEYDOWN = 0x100;
            const Int32 WM_CHAR = 0x0102;
            const Int32 WM_KEYUP = 0x0101;
            const Int32 WM_KILLFOCUS = 0x0008;

            Process p = Process.GetProcessesByName("iRacingSim64DX11").FirstOrDefault(); //iRacingSim64DX11
            if (p != null) {
                IntPtr windowHandle = (IntPtr)p.MainWindowHandle;
                char[] chars = text.ToCharArray();
  
                PostMessage(windowHandle, WM_KEYDOWN, (IntPtr)Keys.T, (IntPtr)0);
                PostMessage(windowHandle, WM_KEYUP, (IntPtr)Keys.T, (IntPtr)0);
                Thread.Sleep(50);
                for (int i = 0; i < chars.Count(); i++) { PostMessage(windowHandle, WM_CHAR, (IntPtr)chars[i], (IntPtr)0); }
                PostMessage(windowHandle, WM_KEYDOWN, (IntPtr)Keys.Enter, (IntPtr)0);
                PostMessage(windowHandle, WM_KEYUP, (IntPtr)Keys.Enter, (IntPtr)0);
                Thread.Sleep(50);
                PostMessage(windowHandle, WM_KILLFOCUS, (IntPtr)0, (IntPtr)0);
            }
        }

        public static void Fullscreen() {
            Process iRacing = Process.GetProcessesByName("iRacingSim64DX11").FirstOrDefault(); //iRacingSim64DX11
            if (iRacing != null) {
                ShowWindow(iRacing.MainWindowHandle, SW_SHOWMAXIMIZED);
            }
        }

        public static void playSound(string file) {
            playSound(file, string.Empty);
        }


        public static void playSound(string file, string deviceName) {
            Guid deviceGuid = Guid.Empty;

            // if we pass a specific device, use it instead
            if (deviceName != string.Empty) {
                foreach (var dev in DirectSoundOut.Devices) {
                    Console.WriteLine($"{dev.Guid} {dev.ModuleName} {dev.Description}");
                    if (dev.Description.Contains("Sample (TC-HELICON GoXLR Mini)")) {
                        deviceGuid = dev.Guid;
                        break;
                    }
                }
            }
            
            

            AudioFileReader audioFileReader = new AudioFileReader(file);

  
            
            var outputDevice = new DirectSoundOut(deviceGuid);
            outputDevice.Init(audioFileReader);
            outputDevice.Play();
            while (outputDevice.PlaybackState == PlaybackState.Playing) {
                Thread.Sleep(100);
            }
            outputDevice.Dispose();
        }


        public static void speakText(string text) {
            System.Speech.Synthesis.SpeechSynthesizer synth = new System.Speech.Synthesis.SpeechSynthesizer();


            synth.SetOutputToDefaultAudioDevice();
            synth.Volume = 100;
            synth.SelectVoiceByHints(System.Speech.Synthesis.VoiceGender.Female);
            synth.Rate = 2;

            PromptBuilder builder = new PromptBuilder(new System.Globalization.CultureInfo("en-US"));
            builder.AppendBreak(System.Speech.Synthesis.PromptBreak.ExtraSmall);
            builder.AppendText(text);
            synth.SpeakAsyncCancelAll();
            synth.SpeakAsync(builder);

            synth.Dispose();
        }

        public static void pressVJoyButton(uint id, uint button) {
            var joystick = new vJoy();
            VjdStat status = joystick.GetVJDStatus(id);
            switch (status) {
                case VjdStat.VJD_STAT_OWN:
                    Console.WriteLine("vJoy Device {0} is already owned by this feeder\n", id);
                    break;
                case VjdStat.VJD_STAT_FREE:
                    Console.WriteLine("vJoy Device {0} is free\n", id);
                    break;
                case VjdStat.VJD_STAT_BUSY:
                    Console.WriteLine("vJoy Device {0} is already owned by another feeder\nCannot continue\n", id);
                    return;
                case VjdStat.VJD_STAT_MISS:
                    Console.WriteLine("vJoy Device {0} is not installed or disabled\nCannot continue\n", id);
                    return;
                default:
                    Console.WriteLine("vJoy Device {0} general error\nCannot continue\n", id);
                    return;
            };
            joystick.SetBtn(true, id, button);

            // joystick.Aquire();                                  // Aquire vJoy device
            // joystick.SetJoystickButton(true, (uint)button);                // Press button
            //joystick.SetJoystickButton(false, 1);              
        }

        public static void releaseVJoyButton(uint id, uint button) {
            var joystick = new vJoy();
            VjdStat status = joystick.GetVJDStatus(id);
            switch (status) {
                case VjdStat.VJD_STAT_OWN:
                    Console.WriteLine("vJoy Device {0} is already owned by this feeder\n", id);
                    break;
                case VjdStat.VJD_STAT_FREE:
                    Console.WriteLine("vJoy Device {0} is free\n", id);
                    break;
                case VjdStat.VJD_STAT_BUSY:
                    Console.WriteLine("vJoy Device {0} is already owned by another feeder\nCannot continue\n", id);
                    return;
                case VjdStat.VJD_STAT_MISS:
                    Console.WriteLine("vJoy Device {0} is not installed or disabled\nCannot continue\n", id);
                    return;
                default:
                    Console.WriteLine("vJoy Device {0} general error\nCannot continue\n", id);
                    return;
            };
            joystick.SetBtn(false, id, button);
        }
    }
}