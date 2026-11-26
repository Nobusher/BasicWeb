using System.Net;
using System.Text;

namespace BasicWeb
{
    internal class Program
    {
        private const int Port = 5000;

        private static async Task Main()
        {
            Console.WriteLine("1 — Запустить HTTP-сервер");
            Console.WriteLine("2 — Отправить сообщение");
            Console.Write("Выберите режим: ");

            switch (Console.ReadLine())
            {
                case "1":
                    await RunServerAsync();
                    break;

                case "2":
                    await RunClientAsync();
                    break;

                default:
                    Console.WriteLine("Неизвестный режим.");
                    break;
            }
        }

        private static async Task RunServerAsync()
        {
            var builder = WebApplication.CreateBuilder();

            // Слушаем порт 5000 на всех локальных IPv4-интерфейсах.
            // Так к серверу сможет обратиться другой ПК в локальной сети.
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Listen(IPAddress.Any, Port);
            });

            var app = builder.Build();

            // Этот адрес можно открыть в браузере для проверки связи.
            app.MapGet("/", () => "HTTP-сервер работает");

            // Этот обработчик вызывается при получении POST /message.
            app.MapPost("/message", async (HttpRequest request) =>
            {
                using var reader = new StreamReader(request.Body, Encoding.UTF8);
                string message = await reader.ReadToEndAsync();

                Console.WriteLine($"Получено: {message}");

                // Это тело HTTP-ответа, который увидит отправитель.
                return Results.Text("Сообщение получено");
            });

            Console.WriteLine($"Сервер слушает порт {Port}. Для остановки нажмите Ctrl+C.");
            await app.RunAsync();
        }

        private static async Task RunClientAsync()
        {
            Console.Write("Введите IPv4 компьютера с сервером: ");
            string? input = Console.ReadLine();

            if (!IPAddress.TryParse(input, out var address) ||
                address.AddressFamily != System.Net.Sockets.AddressFamily.InterNetwork)
            {
                Console.WriteLine("Введите IPv4-адрес, например 192.168.1.10.");
                return;
            }

            Console.Write("Введите сообщение: ");
            string? message = Console.ReadLine();

            if (message == null)
            {
                return;
            }

            using var client = new HttpClient();

            // В теле запроса передаём обычный текст в UTF-8.
            using var content = new StringContent(message, Encoding.UTF8, "text/plain");
            string url = $"http://{address}:{Port}/message";

            try
            {
                using HttpResponseMessage response = await client.PostAsync(url, content);
                string reply = await response.Content.ReadAsStringAsync();

                Console.WriteLine($"HTTP {(int)response.StatusCode}");
                Console.WriteLine(reply);
            }
            catch (HttpRequestException ex)
            {
                Console.WriteLine($"Не удалось отправить запрос: {ex.Message}");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("Время ожидания ответа истекло.");
            }
        }
    }
}
