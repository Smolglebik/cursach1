using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.Sqlite;

//создание построителя веб-приложения
var builder = WebApplication.CreateBuilder(args);

//добавляем сервис решета Эратосфена в контейнер зависимостей
builder.Services.AddScoped<ISieveOfEratosthenesService, SieveOfEratosthenesService>();

//инициализация базы данных
InitializeDatabase();

//построение приложения
var app = builder.Build();

//включение редиректа на HTTPS
app.UseHttpsRedirection();

//эндпоинт для получения простых чисел с использованием решета Эратосфена
//дополнительно принимает имя пользователя для записи действия в историю
app.MapGet("/primes/{username}/{limit}", async (string username, int limit, ISieveOfEratosthenesService sieveService) =>
{
    //проверка, что лимит больше или равен 2
    if (limit < 2)
        return Results.BadRequest("Limit must be greater than 1");

    try
    {
        //вызов метода поиска простых чисел через решето Эратосфена
        var primes = sieveService.FindPrimes(limit);

        //сохранение действия пользователя в историю
        using var connection = new SqliteConnection("Data Source=database.db");
        await connection.OpenAsync();
        await LogUserActionAsync(connection, username, "GetPrimes", $"limit={limit}");

        return Results.Ok(primes);
    }
    catch (ArgumentException ex)
    {
        //обработка ошибки валидации
        return Results.BadRequest(ex.Message);
    }
})
.WithName("GetPrimes")
.WithOpenApi(operation =>
{
    //описание параметра для OpenAPI документации
    operation.Parameters[0].Description = "Верхняя граница для поиска простых чисел (максимум 1 млн)";
    return operation;
});

//эндпоинт для регистрации нового пользователя
app.MapPost("/register", async (RegisterRequest request) =>
{
    //проверка валидности запроса
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { message = "Имя пользователя и пароль обязательны" });
    }

    try
    {
        //проверка существования пользователя
        using var connection = new SqliteConnection("Data Source=database.db");
        await connection.OpenAsync();

        //проверка, существует ли пользователь с таким именем
        var checkCommand = new SqliteCommand("SELECT COUNT(*) FROM Users WHERE Username = @username", connection);
        checkCommand.Parameters.AddWithValue("@username", request.Username);
        var exists = Convert.ToInt32(await checkCommand.ExecuteScalarAsync()) > 0;

        if (exists)
        {
            return Results.BadRequest(new { message = "Пользователь с таким именем уже существует" });
        }

        //хеширование пароля
        var passwordHash = HashPassword(request.Password);

        //добавление нового пользователя в базу данных
        var insertCommand = new SqliteCommand("INSERT INTO Users (Username, PasswordHash) VALUES (@username, @passwordHash)", connection);
        insertCommand.Parameters.AddWithValue("@username", request.Username);
        insertCommand.Parameters.AddWithValue("@passwordHash", passwordHash);
        await insertCommand.ExecuteNonQueryAsync();

        //запись действия в историю
        await LogUserActionAsync(connection, request.Username, "Register", "User registered");

        return Results.Ok(new { message = "Пользователь успешно зарегистрирован" });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Ошибка при регистрации: {ex.Message}");
    }
})
.WithName("Register")
.WithOpenApi();

//эндпоинт для входа в систему
app.MapPost("/login", async (LoginRequest request) =>
{
    //проверка валидности запроса
    if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
    {
        return Results.BadRequest(new { message = "Имя пользователя и пароль обязательны" });
    }

    try
    {
        using var connection = new SqliteConnection("Data Source=database.db");
        await connection.OpenAsync();

        //поиск пользователя в базе данных
        var selectCommand = new SqliteCommand("SELECT PasswordHash FROM Users WHERE Username = @username", connection);
        selectCommand.Parameters.AddWithValue("@username", request.Username);
        var result = await selectCommand.ExecuteScalarAsync();

        if (result == null)
        {
            return Results.Json(new { message = "Неверное имя пользователя или пароль" }, statusCode: StatusCodes.Status401Unauthorized);
        }

        //проверка пароля
        var storedHash = result.ToString();
        var inputHash = HashPassword(request.Password);

        if (storedHash != inputHash)
        {
            return Results.Json(new { message = "Неверное имя пользователя или пароль" }, statusCode: StatusCodes.Status401Unauthorized);
        }

        //успешный вход
        await LogUserActionAsync(connection, request.Username, "Login", "User logged in");
        return Results.Ok(new { message = "Вход выполнен успешно", username = request.Username });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Ошибка при входе: {ex.Message}");
    }
})
.WithName("Login")
.WithOpenApi();

//эндпоинт для получения истории действий пользователя
app.MapGet("/history/{username}", async (string username) =>
{
    try
    {
        using var connection = new SqliteConnection("Data Source=database.db");
        await connection.OpenAsync();

        var command = new SqliteCommand(@"
            SELECT ActionType, Details, Timestamp
            FROM UserActions
            WHERE Username = @username
            ORDER BY Id DESC
            LIMIT 100", connection);
        command.Parameters.AddWithValue("@username", username);

        var history = new List<UserActionResponse>();
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            history.Add(new UserActionResponse(
                reader.GetString(0),
                reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                reader.GetString(2)));
        }

        return Results.Ok(history);
    }
    catch (Exception ex)
    {
        return Results.Problem($"Ошибка при получении истории: {ex.Message}");
    }
})
.WithName("GetUserHistory")
.WithOpenApi();

//запуск приложения
app.Run();

//инициализация базы данных и создание таблиц
void InitializeDatabase()
{
    using var connection = new SqliteConnection("Data Source=database.db");
    connection.Open();

    //создание таблицы пользователей, если она не существует
    var createUsersTableCommand = new SqliteCommand(@"
        CREATE TABLE IF NOT EXISTS Users (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Username TEXT NOT NULL UNIQUE,
            PasswordHash TEXT NOT NULL
        )", connection);
    createUsersTableCommand.ExecuteNonQuery();

    //создание таблицы истории действий пользователей, если она не существует
    var createActionsTableCommand = new SqliteCommand(@"
        CREATE TABLE IF NOT EXISTS UserActions (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Username TEXT NOT NULL,
            ActionType TEXT NOT NULL,
            Details TEXT,
            Timestamp TEXT NOT NULL
        )", connection);
    createActionsTableCommand.ExecuteNonQuery();
}

//асинхронная запись действия пользователя в таблицу истории
async Task LogUserActionAsync(SqliteConnection connection, string username, string actionType, string details)
{
    using var command = new SqliteCommand(@"
        INSERT INTO UserActions (Username, ActionType, Details, Timestamp)
        VALUES (@username, @actionType, @details, @timestamp)", connection);
    command.Parameters.AddWithValue("@username", username);
    command.Parameters.AddWithValue("@actionType", actionType);
    command.Parameters.AddWithValue("@details", details ?? string.Empty);
    command.Parameters.AddWithValue("@timestamp", DateTime.UtcNow.ToString("o"));
    await command.ExecuteNonQueryAsync();
}

//хеширование пароля с использованием SHA256
string HashPassword(string password)
{
    using var sha256 = SHA256.Create();
    var bytes = Encoding.UTF8.GetBytes(password);
    var hash = sha256.ComputeHash(bytes);
    return Convert.ToBase64String(hash);
}

//интерфейс сервиса для поиска простых чисел методом решета Эратосфена
public interface ISieveOfEratosthenesService
{
    //метод для поиска всех простых чисел до указанного лимита
    List<int> FindPrimes(int limit);
}

//реализация сервиса решета Эратосфена для поиска простых чисел
public class SieveOfEratosthenesService : ISieveOfEratosthenesService
{
    //реализация алгоритма решета Эратосфена для поиска простых чисел
    public List<int> FindPrimes(int limit)
    {
        //проверка максимального лимита (не более 1 миллиона)
        if (limit > 1_000_000)
            throw new ArgumentException("Limit cannot exceed 1 million");

        //если лимит меньше 2, простых чисел нет
        if (limit < 2)
            return new List<int>();

        //создание массива для отметки простых чисел
        bool[] isPrime = new bool[limit + 1];
        //изначально все числа считаются простыми
        Array.Fill(isPrime, true);
        //0 и 1 не являются простыми числами
        isPrime[0] = isPrime[1] = false;

        //основной цикл решета Эратосфена: проверяем числа от 2 до квадратного корня из лимита
        for (int p = 2; p * p <= limit; p++)
        {
            //если число p простое, помечаем все его кратные как составные
            if (isPrime[p])
            {
                //помечаем все кратные p как составные числа, начиная с p*p
                for (int i = p * p; i <= limit; i += p)
                    isPrime[i] = false;
            }
        }

        //создание списка для хранения найденных простых чисел
        var primes = new List<int>();
        //собираем все простые числа из массива
        for (int i = 2; i <= limit; i++)
        {
            //если число помечено как простое, добавляем его в список
            if (isPrime[i])
                primes.Add(i);
        }

        //возвращаем список всех найденных простых чисел
        return primes;
    }
}

//класс для запроса регистрации
public record RegisterRequest(string Username, string Password);

//класс для запроса входа
public record LoginRequest(string Username, string Password);

//класс для ответа с историей действий пользователя
public record UserActionResponse(string ActionType, string Details, string Timestamp);
