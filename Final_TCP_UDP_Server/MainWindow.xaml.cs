using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RPS_Server
{
    public partial class MainWindow : Window
    {
        private TcpListener _listener;
        private const int Port = 8888;
        private const int Rounds = 5;
        private static readonly string[] Choices = { "Rock", "Paper", "Scissors" };
        private StringBuilder _gameHistory = new StringBuilder();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void StartServerButton_Click(object sender, RoutedEventArgs e)
        {
            _listener = new TcpListener(IPAddress.Any, Port);
            _listener.Start();
            StatusTextBlock.Text = $"Server started on port {Port}";

            await Task.Run(() => ListenForClients());
        }

        private async Task ListenForClients()
        {
            while (true)
            {
                var client = await _listener.AcceptTcpClientAsync();
                Dispatcher.Invoke(() => StatusTextBlock.Text = "Client connected.");
                Task.Run(() => HandleClient(client));
            }
        }

        private async Task HandleClient(TcpClient client)
        {
            var stream = client.GetStream();
            byte[] buffer = new byte[1024];
            int bytesRead;
            int roundsPlayed = 0;
            int playerWins = 0;
            int serverWins = 0;
            string gameMode = null;

            // Read game mode
            bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
            gameMode = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();

            while ((bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length)) != 0)
            {
                string playerChoice = Encoding.ASCII.GetString(buffer, 0, bytesRead).Trim();
                string serverChoice;

                if (gameMode == "Computer vs. Computer")
                {
                    serverChoice = Choices[new Random().Next(Choices.Length)];
                    playerChoice = Choices[new Random().Next(Choices.Length)];
                }
                else if (gameMode == "Player vs. Computer")
                {
                    serverChoice = Choices[new Random().Next(Choices.Length)];
                }
                else
                {
                    serverChoice = playerChoice; // Just to continue the loop if game mode is incorrect
                }

                string result = DetermineResult(playerChoice, serverChoice, ref playerWins, ref serverWins);
                _gameHistory.AppendLine(result);

                roundsPlayed++;
                result += $" Current score: Player {playerWins} - Server {serverWins}.";

                if (roundsPlayed >= Rounds)
                {
                    result += $" Game over. Final score: Player {playerWins} - Server {serverWins}.";
                    Dispatcher.Invoke(() => StatusTextBlock.Text = result);
                    break;
                }

                byte[] response = Encoding.ASCII.GetBytes(result);
                await stream.WriteAsync(response, 0, response.Length);
            }

            client.Close();
            Dispatcher.Invoke(() => StatusTextBlock.Text = "Client disconnected.");
        }

        private string DetermineResult(string playerChoice, string serverChoice, ref int playerWins, ref int serverWins)
        {
            string result = $"Player chose {playerChoice}, Server chose {serverChoice}. ";

            if (playerChoice == serverChoice)
            {
                result += "It's a tie!";
            }
            else if ((playerChoice == "Rock" && serverChoice == "Scissors") ||
                     (playerChoice == "Paper" && serverChoice == "Rock") ||
                     (playerChoice == "Scissors" && serverChoice == "Paper"))
            {
                result += "Player wins this round!";
                playerWins++;
            }
            else
            {
                result += "Server wins this round!";
                serverWins++;
            }

            return result;
        }
    }
}