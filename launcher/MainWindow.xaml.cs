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
using System.Timers;

using Firebase.Auth;
using Firebase.Auth.Providers;
using Firebase.Auth.Repository;
using FireSharp.Interfaces;
using FireSharp;
using FireSharp.Config;
using FireSharp.Response;
using Firebase.Storage;
using System.Diagnostics.Eventing.Reader;

using SCREW.Auth.System4TT;
using System.Net;
using System.Xml.Linq;
using System.Net.Http;
using System.IO;
using System.Security.Cryptography;
using System.Windows.Threading;
using Newtonsoft.Json.Linq;

namespace launcher
{
    public partial class MainWindow : Window
    {
        InitConfigsSys LauncherSys = new InitConfigsSys();

        DispatcherTimer timer;

        public MainWindow()
        {
            InitializeComponent();
            ShowLoginScreen();
        }
        public void ShowLoginScreen()
        {
            WorkSpace.Visibility = Visibility.Hidden;
            RegisterScreen.Visibility = Visibility.Hidden;
            LoginScreen.Visibility = Visibility.Visible;
            Authentication.Visibility = Visibility.Hidden;
        }
        public async void ShowWorkspace()
        {
            WorkSpace.Visibility = Visibility.Visible;
            LoginScreen.Visibility = Visibility.Hidden;
            RegisterScreen.Visibility = Visibility.Hidden;
            Settings.Visibility = Visibility.Hidden;
            Authentication.Visibility = Visibility.Hidden;
            Friends.Visibility = Visibility.Hidden;

            var _avatar = await LauncherSys.GetFirebaseClient().GetAsync($"Information/{LauncherSys.GetUserCredential().User.Uid}/Avatar");
            var _number = await LauncherSys.GetFirebaseClient().GetAsync($"Information/{LauncherSys.GetUserCredential().User.Uid}/PhoneNumber");
            var _age = await LauncherSys.GetFirebaseClient().GetAsync($"Information/{LauncherSys.GetUserCredential().User.Uid}/Age");
            var _username = await LauncherSys.GetFirebaseClient().GetAsync($"Information/{LauncherSys.GetUserCredential().User.Uid}/Name");
            var _address = await LauncherSys.GetFirebaseClient().GetAsync($"Information/{LauncherSys.GetUserCredential().User.Uid}/Address");

            UserAge.Content = _age.ResultAs<string>();
            UserID.Content = LauncherSys.GetUserCredential().User.Uid;
            Usermail.Content = LauncherSys.GetUserCredential().User.Info.Email;
            UserNumber.Content = _number.ResultAs<string>();
            profile_name.Text = _username.ResultAs<string>();
            UserAddress.Content = _address.ResultAs<string>();
            InitTimer();
            try
            {
                string path = _avatar.ResultAs<string>();
                await DownloadAvatar(path, profile_avatar);
            }
            catch
            {
                await DownloadAvatar("1675553088_www-funnyart-club-p-memi-s-gorillami-kartinki-28.jpg", profile_avatar);
            }
        }
        public async Task DownloadAvatar(string key, Image avatar)
        {
            if (String.IsNullOrEmpty(key))
            {
                key = "1675553088_www-funnyart-club-p-memi-s-gorillami-kartinki-28.jpg";
            }
            FirebaseStorageReference imageRef = LauncherSys.GetStorage().Child(key);
            
            var downloadUrl = await imageRef.GetDownloadUrlAsync();
            using (var httpClient = new HttpClient())
            {
                var stream = await httpClient.GetStreamAsync(downloadUrl);
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = stream;
                bitmapImage.EndInit();
                avatar.Source = bitmapImage;
            }
        }
        private void OnExitClicked(object sender, RoutedEventArgs e)
        {
            System.Environment.Exit(0);
        }
        private async void TryToLogin(object sender, RoutedEventArgs e)
        {
            if (username.Text.Length > 0 && password.Password.Length > 0)
            {
                bool isLoggedIn = await LauncherSys.Login(username.Text, password.Password);
                if (isLoggedIn)
                {
                    Authentication.Visibility = Visibility.Visible;
                }
                else
                {
                    MessageBox.Show("Неверный логин или пароль!");
                }
            }
            else
            {
                MessageBox.Show("Неверное заполены поля!");
            }
        }
        public async void GetAllUsers()
        {
            users_list.Children.Clear();
            var users = await LauncherSys.GetFirebaseClient().GetAsync("Information");
            string json = users.Body;

            JObject jsonObject = JObject.Parse(json);

            foreach (var user in jsonObject)
            {
                string userId = user.Key;
                JObject userData = (JObject)user.Value;
                string name = (string)userData["Name"];
                string avatar = null;

                if (userData.ContainsKey("Avatar"))
                {
                    avatar = (string)userData["Avatar"];
                }
                InitializeUserInList(userId, avatar, name);
            }
        }
        private async void InitializeUserInList(string uid, string avatarUrl, string username)
        {
            if (uid == LauncherSys.GetUserCredential().User.Uid)
                return;

            Grid mainGrid = new Grid();
            mainGrid.Background = new SolidColorBrush(Color.FromRgb(215, 241, 247));
            mainGrid.Margin = new Thickness(0, 0, 0, 6);

            StackPanel stackPanel = new StackPanel();
            stackPanel.Orientation = Orientation.Horizontal;

            Image image = new Image();
            image.Width = 50;
            image.Height = 50;
            image.Margin = new Thickness(6);
            image.Stretch = Stretch.Fill;
            DownloadAvatar(avatarUrl, image);

            Label label = new Label();
            label.VerticalContentAlignment = VerticalAlignment.Center;
            label.Content = username;

            Border border = new Border();
            border.BorderThickness = new Thickness(1);
            border.CornerRadius = new CornerRadius(300);
            border.HorizontalAlignment = HorizontalAlignment.Left;
            border.VerticalAlignment = VerticalAlignment.Center;
            border.Width = 12;
            border.Height = 12;
            border.Background = Brushes.Red;
            isOnline(uid, border);
            stackPanel.Children.Add(image);
            stackPanel.Children.Add(label);
            stackPanel.Children.Add(border);

            mainGrid.Children.Add(stackPanel);
            users_list.Children.Add(mainGrid);
        }
        public void InitTimer()
        {
            timer_Tick(null, null);
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromSeconds(20);
            timer.Tick += timer_Tick;
            timer.Start();
        }
        public async void timer_Tick(object sender, EventArgs e)
        {
            SetResponse online = await LauncherSys.GetFirebaseClient().SetAsync($"Information/{LauncherSys.GetUserCredential().User.Uid}/Online", DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            isOnline(LauncherSys.GetUserCredential().User.Uid, profile_Online);
        }
        public async void isOnline(string uid, Border border)
        {
            try
            {
                var online = await LauncherSys.GetFirebaseClient().GetAsync($"Information/{uid}/Online");
                if (online.ResultAs<long>() + 100 >= DateTimeOffset.UtcNow.ToUnixTimeSeconds())
                {
                    border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#00ff0a"));
                }
                else
                {
                    border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff0000"));
                }
            }
            catch
            {
                border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#ff0000"));
            }
        }
        private void OnAuthClicked(object sender, RoutedEventArgs e)
        {
            TwoFactorAuthenticator authenticator = new TwoFactorAuthenticator("1234");
            if (auth_.Text != null && authenticator.VerifyAuthenticationCode(auth_.Text))
            {
                ShowWorkspace();
            }
            else
            {
                MessageBox.Show("Неверно введен код!");
            }
        }
        private void OnProfileClicked(object sender, RoutedEventArgs e)
        {
            Settings.Visibility = Visibility.Hidden;
            Profile.Visibility = Visibility.Visible;
            Friends.Visibility = Visibility.Hidden;
        }
        private void OnFriendsClicked(object sender, RoutedEventArgs e)
        {
            GetAllUsers();
            Settings.Visibility = Visibility.Hidden;
            Profile.Visibility = Visibility.Hidden;
            Friends.Visibility = Visibility.Visible;
        }
        private void OnSettingsClicked(object sender, RoutedEventArgs e)
        {
            Profile.Visibility = Visibility.Hidden;
            Settings.Visibility = Visibility.Visible;
            Friends.Visibility = Visibility.Hidden;
        }
        private async void TryToCreate(object sender, RoutedEventArgs e)
        {
            if (username_.Text.Length > 0 && password_.Text.Length > 0)
            {
                bool isRegisterIn = await LauncherSys.Create(username_.Text, password_.Text);
                if (isRegisterIn)
                {
                    var userInfo = new
                    {
                        Name = name_.Text,
                        Address = address_.Text,
                        Age = age_.Text,
                        PhoneNumber = number_.Text
                    };
                    await LauncherSys.GetFirebaseClient().SetAsync($"Information/{LauncherSys.GetUserCredential().User.Uid}", userInfo);
                    ShowWorkspace();
                }
                else
                {
                    MessageBox.Show("Неверный логин или пароль!");
                }
            }
            else
            {
                MessageBox.Show("Неверное заполены поля!");
            }
        }
        private void ShowRegistrationScreen(object sender, RoutedEventArgs e)
        {
            WorkSpace.Visibility = Visibility.Hidden;
            RegisterScreen.Visibility = Visibility.Visible;
            LoginScreen.Visibility = Visibility.Hidden;
            reg2screen.Visibility = Visibility.Hidden;
        }
        private void ShowReg_2_Screen(object sender, RoutedEventArgs e)
        {
            WorkSpace.Visibility = Visibility.Hidden;
            reg1screen.Visibility = Visibility.Hidden;
            LoginScreen.Visibility = Visibility.Hidden;
            reg2screen.Visibility = Visibility.Visible;
        }
        private async void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            if (launcher.Properties.Settings.Default.login != null && launcher.Properties.Settings.Default.password != null)
            {
                bool isAuthed = await LauncherSys.Login(launcher.Properties.Settings.Default.login, launcher.Properties.Settings.Default.password);
                if (isAuthed)
                {
                    Authentication.Visibility = Visibility.Visible;
                }
            }
        }
        private void OnAvatarUpload(object sender, MouseButtonEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Images (*.BMP;*.JPG;*.GIF;*.PNG)|*.BMP;*.JPG;*.GIF;*.PNG"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                UploadAvatarFile(openFileDialog.FileName);
            }
        }
        public async void UploadAvatarFile(string path)
        {
            try
            {
                var stream = System.IO.File.Open(path, FileMode.Open);
                var name = $"{new Random().Next(0, 100000)}{System.IO.Path.GetExtension(path)}";
                var task = new FirebaseStorage(
                    "screw-launcher.appspot.com",

                     new FirebaseStorageOptions
                     {
                         AuthTokenAsyncFactory = () => Task.FromResult(LauncherSys.GetUserCredential().User.Credential.IdToken),
                         ThrowOnCancel = true,
                     })
                    .Child("user-data")
                    .Child(name)
                    .PutAsync(stream);

                var downloadUrl = await task;
                FirebaseResponse avatar = await LauncherSys.GetFirebaseClient().SetAsync("Information/" + LauncherSys.GetUserCredential().User.Uid + "/Avatar", $"user-data/{name}");
                await DownloadAvatar($"user-data/{name}", profile_avatar);
            }
            catch
            {

            }
        }
    }
    public class InitConfigsSys
    {
        static FirebaseAuthClient client;
        static FireSharp.FirebaseClient firebaseClient;
        static UserCredential userCredential;
        static FirebaseStorage storage;
        public InitConfigsSys()
        {
            InitConfigs();
        }

        public FireSharp.FirebaseClient GetFirebaseClient()
        {
            return firebaseClient;
        }
        public UserCredential GetUserCredential()
        {
            return userCredential;
        }
        public FirebaseStorage GetStorage()
        {
            return storage;
        }
        private void InitConfigs()
        {
            FirebaseAuthConfig config = new FirebaseAuthConfig
            {
                ApiKey = "AIzaSyAEF_kcSK3w1Bg11_cqZrtxWj7UPoP_Iio",
                AuthDomain = "screw-launcher.firebaseapp.com",
                Providers = new FirebaseAuthProvider[]
            {
                    new EmailProvider()
            },
                UserRepository = new FileUserRepository("test")
            };

            client = new FirebaseAuthClient(config);
        }
        private void InitConfigFirebase()
        {
            string _authSecret = null;

            if (userCredential != null && userCredential.User != null && userCredential.User.Credential != null)
            {
                _authSecret = userCredential.User.Credential.IdToken;
            }

            IFirebaseConfig firebaseConfig = new FireSharp.Config.FirebaseConfig
            {
                RequestTimeout = TimeSpan.FromDays(1),
                BasePath = "https://screw-launcher-default-rtdb.firebaseio.com/",
                AuthSecret = _authSecret
            };
            firebaseClient = new FirebaseClient(firebaseConfig);
        }
        private void InitConfigFirebaseStorage()
        {
            storage = new FirebaseStorage("screw-launcher.appspot.com", new FirebaseStorageOptions
            {
                AuthTokenAsyncFactory = () => Task.FromResult(userCredential.User.Credential.IdToken)
            });
        }
        public async Task<bool> Login(string username, string password)
        {
            try
            {
                userCredential = await client.SignInWithEmailAndPasswordAsync(username, password);
                InitConfigFirebaseStorage();
                InitConfigFirebase();
                launcher.Properties.Settings.Default.login = username;
                launcher.Properties.Settings.Default.password = password;
                launcher.Properties.Settings.Default.Save();
                return true;
            }
            catch
            {
                return false;
            }
        }
        public async Task<bool> Create(string username, string password)
        {
            try
            {
                userCredential = await client.CreateUserWithEmailAndPasswordAsync(username, password);
                InitConfigFirebaseStorage();
                InitConfigFirebase();
                launcher.Properties.Settings.Default.login = username;
                launcher.Properties.Settings.Default.password = password;
                launcher.Properties.Settings.Default.Save();
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
