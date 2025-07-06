using System.Net.Sockets;
using System.Text;

internal class ChatClient
{
    static TcpClient client;           // TCP-клиент для подключения к серверу
    static NetworkStream stream;       // Поток для отправки и получения данных
    static string name;                // Имя пользователя
    static Thread receiveThread;

    static void Main(string[] args)
    {
        // Запрос IP-сервера
        Console.Write("Введите IP-адрес сервера (например, 127.0.0.1): ");
        string serverIp = Console.ReadLine()?.Trim();

        // Запрос имени у пользователя для чата
        Console.Write("Введите имя: ");
        name = Console.ReadLine();

        // Подключение к серверу
        try
        {
            client = new TcpClient(serverIp, 5000);
            stream = client.GetStream();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка подключения: {ex.Message}");
            return;
        }

        // Отправка имени пользователя серверу
        byte[] data = Encoding.UTF8.GetBytes(name);
        stream.Write(data, 0, data.Length);

        // Запуск отдельного потока для приёма сообщений от сервера
        Thread receiveThread = new Thread(ReceiveMessages);
        receiveThread.Start();

        // Считывание ввода пользователя и отправка сообщения
        while (true)
        {
            string message = Console.ReadLine();

            if (message.ToLower() == "/exit")
            {
                break;
            }

            
            data = Encoding.UTF8.GetBytes(message); // Преобразование сообщения в байты и отправка на сервер
            stream.Write(data, 0, data.Length);
        }

        // Закрываем соединение и поток
        stream.Close();
        client.Close();
    }

    // Метод, выполняющийся в отдельном потоке (слушает входящие сообщения от сервера)
    static void ReceiveMessages()
    {
        byte[] buffer = new byte[1024];

        try
        {
            while (true)
            {
                // Чтение сообщения из потока
                int bytes = stream.Read(buffer, 0, buffer.Length);

                if (bytes == 0) break;  // Если байтов нет — сервер закрыл соединение

                // Преобразование байтов в строку и выводим в консоль
                string message = Encoding.UTF8.GetString(buffer, 0, bytes).Trim();
                Console.WriteLine(message);
            }
        }
        catch
        {
            Console.WriteLine("Соединение с сервером прервано.");
        }
    }
}