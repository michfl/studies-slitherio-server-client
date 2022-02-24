using System.Windows;
using System.Windows.Input;

namespace Client
{
    /// <summary>
    /// Interaction logic for GameWindow.xaml
    /// </summary>
    public partial class GameWindow : Window
    {
        public Game GameRef { get; set; }

        public GameWindow(Game game)
        {
            InitializeComponent();
            GameRef = game;
        }

        //Performs cleanup after window closing
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            var mainWindow = (MainWindow)Owner;
            mainWindow.GameWindowClosed();
        }

        //Takes keyboard input from the client
        private void HandleKeyPress(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.A:
                    GameRef.DirectionLeft();
                    break;
                case Key.Left:
                    GameRef.DirectionLeft();
                    break;
                case Key.D:
                    GameRef.DirectionRight();
                    break;
                case Key.Right:
                    GameRef.DirectionRight();
                    break;
                case Key.Space:
                    GameRef.RequesSpawn();
                    break;
                case Key.Escape:
                    Close();
                    break;
            }
        }

        //Performs window init
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            window.KeyDown += HandleKeyPress;
        }
    }
}
