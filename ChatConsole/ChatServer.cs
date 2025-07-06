using System.Net;
using System.Net.Sockets;
using System.Text;

internal class ChatServer
{
    // TCP-сервер для прослушивания входящих соединений
    static TcpListener listener;

    // Список всех подключённых клиентов
    static List<ClientHandler> clients = new List<ClientHandler>();

    private static void Main(string[] args)
    {
        int port = 5000;

        // Запускаем TCP-сервер на указанном порту
        listener = new TcpListener(IPAddress.Any, port);
        listener.Start();

        // Получение IP-адреса сервера
        string localIPs = string.Join(", ", GetLocalIPv4Addresses());
        Console.WriteLine($"Сервер запущен по IP: {localIPs}:{port}");

        // Цикл ожидания клиентов
        while (true)
        {
            // Приняте входящего соединения от клиента
            TcpClient tcpClient = listener.AcceptTcpClient();

            // Создание обработчика клиента
            ClientHandler clientHandler = new ClientHandler(tcpClient);

            // Добавление клиента в общий список
            lock (clients)
                clients.Add(clientHandler);

            // Обработка клиента в отдельном потоке
            Thread thread = new Thread(clientHandler.Process);
            thread.Start();
        }
    }


    // Метод для получения IPv4-текущей машины
    private static List<string> GetLocalIPv4Addresses()
    {
        List<string> addresses = new List<string>();

        foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList) // Получение IP-адреса устройства
        {
            // Фильтрация только IPv4-адресов
            if (ip.AddressFamily == AddressFamily.InterNetwork)
                addresses.Add(ip.ToString());
        }
        return addresses;
    }


    // Метод для рассылки сообщений всем клиентам
    public static void Broadcast(string message, ClientHandler exclude = null)
    {
        byte[] data = Encoding.UTF8.GetBytes(message + "\n");

        lock (clients)
        {
            foreach (var client in clients)
            {
                // Пропуск отправки указанному клиенту
                if (client != exclude)
                {
                    try
                    {
                        client.Stream.Write(data, 0, data.Length);
                    }
                    catch
                    {
                        // Игнорируем ошибки отправки
                    }
                }
            }
        }

        Console.WriteLine(message); // Вывод сообщения на серверную консоль
    }

    // Удаление клиента из общего списка (отключение клиента)
    public static void RemoveClient(ClientHandler client)
    {
        lock (clients)
        {
            clients.Remove(client);
        }
    }

    // Обработчик одного клиента
    internal class ClientHandler
    {
        public string Name;             // Имя пользователя
        public TcpClient Client;        // TCP-соединение с клиентом
        public NetworkStream Stream;    // Сетевой поток для чтения/записи

        public ClientHandler(TcpClient client)
        {
            Client = client;
            Stream = client.GetStream();
        }

        // Основной метод обработки клиента
        public void Process()
        {
            try
            {
                // Получение имни клиента при подключении
                byte[] buffer = new byte[1024];
                int bytes = Stream.Read(buffer, 0, buffer.Length);
                Name = Encoding.UTF8.GetString(buffer, 0, bytes).Trim();

                // Уведомление всех о новом пользователе
                ChatServer.Broadcast($"{Name} вошёл в чат", this);

                // Считывание сообщения от клиента и рассылка остальным
                while (true)
                {
                    bytes = Stream.Read(buffer, 0, buffer.Length);
                    if (bytes == 0) break;

                    string message = Encoding.UTF8.GetString(buffer, 0, bytes).Trim();
                    ChatServer.Broadcast($"{Name}: {message}", this);
                }
            }
            catch
            {
                // Игнорируем ошибки при чтении (отключение клиента)
            }
            finally
            {
                ChatServer.Broadcast($"{Name} покинул чат");    // Уведомляем об отключении
                ChatServer.RemoveClient(this);                  // Очищаем ресурсы
                Stream.Close();
                Client.Close();
            }
        }
    }
}