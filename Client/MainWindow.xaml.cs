using System;
using System.Windows;

namespace Client
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        static Game _game;

        public MainWindow()
        {
            InitializeComponent();

            button_disconnect.Visibility = Visibility.Hidden;
        }

        //Shows connect button
        private void ShowConnect()
        {
            button_disconnect.Visibility = Visibility.Hidden;
            button_connect.Visibility = Visibility.Visible;
        }

        //Shows disconnect button
        private void ShowDisconnect()
        {
            button_disconnect.Visibility = Visibility.Visible;
            button_connect.Visibility = Visibility.Hidden;
        }

        //Performs cleanup after closing game window
        public void GameWindowClosed()
        {
            _game.Stop();
            ShowConnect();
        }

        //Shows wrong ip message
        public void WrongIP()
        {
            textBox_serverIP.Text = "Wrong adress";
            ShowConnect();
        }

        //Tries to connect to the server, on success starts the game window
        private async void button_connect_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string ip = textBox_serverIP.Text;
                string name = textBox_name.Text;
                if (name == "")
                {
                    textBox_name.Text = "Pick your name";
                    return;
                }

                int port = Int32.Parse(textBox_serverPort.Text);

                ShowDisconnect();
                _game = new Game(ip, port, name, this);
                await _game.Start();
            }
            catch
            {
                textBox_serverPort.Text = "This should be numeric";
            }
        }

        //Disconnects from the server, closes game window
        private void button_disconnect_Click(object sender, RoutedEventArgs e)
        {
            ShowConnect();
            if (_game != null)
                _game.Stop();
        }

        //Performs cleanup on window close
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (_game != null)
                _game.Stop();
        }
    }
}
