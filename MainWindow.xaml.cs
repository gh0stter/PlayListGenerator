using Extensions;
using Microsoft.WindowsAPICodePack.Dialogs;
using PlayListGenerator.DomainModel;
using PlaylistsNET.Content;
using PlaylistsNET.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PlayListGenerator
{
   /// <summary>
   /// Interaction logic for MainWindow.xaml
   /// </summary>
   public partial class MainWindow : Window, INotifyPropertyChanged
   {
      private double _percentage;
      public double Percentage
      {
         get => _percentage;
         set
         {
            if (Math.Abs(value - _percentage) < 0.0001)
            {
               return;
            }
            _percentage = value;
            OnPropertyChanged("Percentage");
         }
      }

      private string _logs;
      public string Logs
      {
         get => _logs;
         set
         {
            _logs = value;
            OnPropertyChanged("Logs");
         }
      }

      public MainWindow()
      {
         InitializeComponent();
         DataContext = this;
         RandomBySong_RB.IsChecked = true;
      }

      public event PropertyChangedEventHandler PropertyChanged;
      public void OnPropertyChanged(string propertyName)
      {
         PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
      }

      private void Browse_Button_Click(object sender, RoutedEventArgs e)
      {
         var myMusicPath = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
         var dlg = new CommonOpenFileDialog
         {
            Title = "Select Music Folder",
            IsFolderPicker = true,
            InitialDirectory = myMusicPath,
            AddToMostRecentlyUsedList = false,
            AllowNonFileSystemItems = false,
            DefaultDirectory = myMusicPath,
            EnsureFileExists = true,
            EnsurePathExists = true,
            EnsureReadOnly = false,
            EnsureValidNames = true,
            Multiselect = false,
            ShowPlacesList = true
         };

         if (dlg.ShowDialog() == CommonFileDialogResult.Ok)
         {
            var folder = dlg.FileName;
            Path_TextBox.Text = folder;
         }
      }

      private async void Generate_Button_Click(object sender, RoutedEventArgs e)
      {
         string path = Path_TextBox.Text;
         bool isRandomBySong = RandomBySong_RB.IsChecked == true;
         Generate_Button.IsEnabled = false;
         await Task.Run(() =>
         {
            GeneratePlayList(path, isRandomBySong);
         });
         Generate_Button.IsEnabled = true;
      }

      private void GeneratePlayList(string path, bool isRandomBySong)
      {
         bool isPathValid = true;

         try
         {
            string fullPath = Path.GetFullPath(path);
            if (!Directory.Exists(path))
            {
               isPathValid = false;
            }
         }
         catch (Exception)
         {
            isPathValid = false;
         }

         if (!isPathValid)
         {
            MessageBox.Show("Invalid path", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
         }

         var files = Directory.GetFiles(path, "*.mp3", SearchOption.AllDirectories);

         var songList = new List<SongInfo>(files.Length);
         var taskCount = (files.Length * 2) + 2;
         var taskDone = 1;

         for (int i = 0; i < files.Length; i++)
         {
            string file = files[i];
            var tfile = TagLib.File.Create(file);
            songList.Add(new SongInfo
            {
               Title = tfile.Tag.Title,
               Artists = string.Join(string.Empty, tfile.Tag.Artists),
               Album = tfile.Tag.Album,
               TrackNumber = (int)tfile.Tag.Track,
               Duration = tfile.Properties.Duration,
               Path = Path.GetRelativePath(path, file)
            });
            Percentage = ++taskDone / taskCount * 100;
            Logs += $"Processing ({i + 1} of {files.Length}) song: {tfile.Tag.Title}{Environment.NewLine}";
         }

         Logs += $"Start shuffling songs.{Environment.NewLine}";
         if (isRandomBySong)
         {
            songList = songList.Shuffle().ToList();
         }
         else
         {
            songList = songList.GroupBy(i => i.Album).Shuffle().SelectMany(i => i.OrderBy(i => i.TrackNumber)).ToList();
         }
         Percentage = ++taskDone / taskCount * 100;
         Logs += $"Shuffle done.{Environment.NewLine}";

         var playlist = new M3uPlaylist
         {
            IsExtended = true
         };

         for (int i = 0; i < songList.Count; i++)
         {
            SongInfo? song = songList[i];
            playlist.PlaylistEntries.Add(new M3uPlaylistEntry
            {
               Album = song.Album,
               AlbumArtist = song.Artists,
               Path = song.Path,
               Title = song.Title
            });
            Percentage = ++taskDone / taskCount * 100;
            Logs += $"Writing song to play list ({i + 1} of {songList.Count}): {song.Title}{Environment.NewLine}";
         }

         var content = new M3uContent();
         string text = content.ToText(playlist);

         File.WriteAllText(isRandomBySong ? $@"{path}\Random by song.m3u" : $@"{path}\Random by Album.m3u", text);
         Percentage = 100;
         Logs += $"All done.{Environment.NewLine}";
      }

      private void RandomBySong_RB_Click(object sender, RoutedEventArgs e)
      {
         RandomBySong_RB.IsChecked = true;
         RandomByAlbum_RB.IsChecked = false;
      }

      private void RandomByAlbum_RB_Click(object sender, RoutedEventArgs e)
      {
         RandomBySong_RB.IsChecked = false;
         RandomByAlbum_RB.IsChecked = true;
      }
   }
}
