using System.Windows;
using System.Net;
using System.IO;
using System;
using System.Text;
using System.Windows.Controls;
using System.Windows.Input;
using System.Linq;

namespace TwitchToMPC
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string ChannelName = "";
        string Json = "";
        string Token = "";
        string Sig = "0000000000000000000000000000000000000000"; // 40
        string Nauth = "";
        string Nauthsig = "0000000000000000000000000000000000000000"; // 40
        string VODid = "";
        string PlayerPath = @"C:\Program Files\MPC-HC\mpc-hc.exe";
        FileStream fs;
        string Path = "list.txt";
        byte[] b;
        string[] Channels;

        public MainWindow()
        {
            InitializeComponent();
            Channels = File.ReadAllLines(Path);
            foreach (string s in Channels)
            {
                ListOfChannels.Items.Add(new ListBoxItem().Content = s.ToLower());
            }
            ListOfChannels.SelectionChanged += new SelectionChangedEventHandler(SelectionChanged);
            ListOfChannels.GotFocus += new RoutedEventHandler(ListGotFocus);

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
        }

        private void StartStream_Click(object sender, RoutedEventArgs e)
        {
            ChannelName = NewChannel.Text.ToLower();
            /*
            if (File.Exists(ChannelName + ".m3u8"))
            {
                System.Diagnostics.Process.Start(PlayerPath, ChannelName + ".m3u8");
                return;
            }
            */
            if (ChannelName == "")
                return;
            if (ChannelName.Contains("https://www.twitch.tv/"))
                ChannelName = ChannelName.Substring(ChannelName.LastIndexOf("/") + 1);
            using (WebClient wc = new WebClient())
            {
                try
                {
                    //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                    //Json = wc.DownloadString("https://api.twitch.tv/kraken/channels/" + ChannelName + "/access_token?client_id=your-client-id");
                    Json = wc.UploadString("https://id.twitch.tv/oauth2/token","client_id=your-client-id&client_secret=your-client-secret&grant_type=client_credentials");
                    Token = Json;             
                    text1.AppendText(Json + "\n");
                }
                catch (WebException exc)
                {
                    MessageBox.Show(exc.Message);
                    return;
                }
            }
            
            int StartIndex = Json.IndexOf("access_token");
            int EndIndex = Json.IndexOf("\",\"expires_in\"");
            Token = Json.Substring(StartIndex, EndIndex - StartIndex).Replace("\\", "");
            Sig = Json.Substring(Json.IndexOf("sig\":\"") + "sig\":\"".Length, Sig.Length);
            text1.AppendText(Token + "\n" + Sig + "\n");
            
            using (WebClient wc = new WebClient())
            {
                try
                {

                    //Json = wc.DownloadString("http://usher.twitch.tv/api/channel/hls/" + ChannelName + ".m3u8?player=twitchweb&token=" + Token + "&sig=" + Sig + "&allow_audio_only=true&allow_source=true&type=any&p=9333029");
                    //HttpRequestHeader.Authorization ClientID=new HttpRequestHeader();
                    wc.Headers.Add("Client-ID", "your-client-id");
                    Json = wc.DownloadString("https://api.twitch.tv/helix/streams?user_login="+ ChannelName);
    
                        text1.AppendText(Json + "\n");
                }
                catch (WebException exc)
                {
                    MessageBox.Show(exc.Message);
                    return;
                }
            }
            b = Encoding.ASCII.GetBytes(Json);
            fs = new FileStream(ChannelName + ".m3u8", FileMode.Create, FileAccess.Write);
            fs.Write(b, 0, b.Length);
            fs.Close();

            //Playlist = Json.Substring(Json.IndexOf("http://video"), Json.IndexOf(".m3u8") + ".m3u8".Length - Json.IndexOf("http://video"));
            System.Diagnostics.Process.Start(PlayerPath, ChannelName + ".m3u8");
        }

        private void AddChannel_Click(object sender, RoutedEventArgs e)
        {
            if (NewChannel.Text != "")
            {
                File.AppendAllText(Path, NewChannel.Text.ToLower() + "\r\n");
                ListOfChannels.Items.Add(new ListBoxItem().Content = NewChannel.Text.ToLower());
            }
        }

        private void DeleteChannel_Click(object sender, RoutedEventArgs e)
        {
            if (ListOfChannels.SelectedValue != null)
            {
                File.WriteAllLines(Path, File.ReadLines(Path).Where(l => l != ListOfChannels.SelectedValue.ToString()).ToList());
                ListOfChannels.Items.Remove(ListOfChannels.SelectedValue);
                NewChannel.Text = "";
            }
        }

        private void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ListOfChannels.SelectedValue != null)
                NewChannel.Text = ListOfChannels.SelectedValue.ToString();
        }

        private void ListGotFocus(object sender, RoutedEventArgs e)
        {
            if (ListOfChannels.SelectedValue!=null)
                NewChannel.Text = ListOfChannels.SelectedValue.ToString();
        }

        private void GetVOD_Click(object sender, RoutedEventArgs e)
        {
            VODid = NewChannel.Text;
            if (VODid == "")
                return;
            if (VODid.Contains("https://www.twitch.tv/videos/"))
                VODid = VODid.Substring(VODid.LastIndexOf("/") + 1);
            using (WebClient wc = new WebClient())
            {
                try
                {
                    Json = wc.DownloadString("https://api.twitch.tv/api/vods/" + VODid + "/access_token?client_id=your-client-id");
                }
                catch (WebException exc)
                {
                    MessageBox.Show(exc.Message);
                    return;
                }
            }
            int StartIndex = Json.IndexOf("{\\\"");
            int EndIndex = Json.IndexOf("\",\"sig\"");
            Nauth = Json.Substring(StartIndex, EndIndex - StartIndex).Replace("\\", "");
            Nauthsig = Json.Substring(Json.IndexOf("sig\":\"") + "sig\":\"".Length, Nauthsig.Length);
            text1.AppendText(Nauth + "\n" + Nauthsig + "\n");
            string ss = "";
            using (WebClient wc = new WebClient())
            {
                try
                {
                    ss = "http://usher.twitch.tv/vod/" + VODid + "?nauthsig=" + Nauthsig + "&nauth=" + Nauth + "&allow_source=true&player=twitchweb&allow_spectre=true&allow_audio_only=true";
                    Json = wc.DownloadString("http://usher.twitch.tv/vod/" + VODid + "?nauthsig=" + Nauthsig + "&nauth=" + Nauth + "&allow_source=true&player=twitchweb&allow_spectre=true&allow_audio_only=true");
                }
                catch (WebException exc)
                {
                    MessageBox.Show(exc.Message);
                    return;
                }
            }

            b = Encoding.ASCII.GetBytes(Json);
            fs = new FileStream(VODid + ".m3u8", FileMode.Create, FileAccess.Write);
            fs.Write(b, 0, b.Length);
            fs.Close();

            System.Diagnostics.Process.Start(PlayerPath, VODid + ".m3u8");
        }
    }
}
