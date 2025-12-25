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

//если приложение работает в режиме разработки, добавляем OpenAPI документацию
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

//включение редиректа на HTTPS
app.UseHttpsRedirection();

//эндпоинт для получения простых чисел с использованием решета Эратосфена
app.MapGet("/primes/{limit}", (int limit, ISieveOfEratosthenesService sieveService) =>
{
    //проверка, что лимит больше или равен 2
    if (limit < 2)
        return Results.BadRequest("Limit must be greater than 1");

    try
    {
        //вызов метода поиска простых чисел через решето Эратосфена
        var primes = sieveService.FindPrimes(limit);
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

    //проверка минимальной длины пароля
    if (request.Password.Length < 3)
    {
        return Results.BadRequest(new { message = "Пароль должен содержать минимум 3 символа" });
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
            return Results.Unauthorized();
        }

        //проверка пароля
        var storedHash = result.ToString();
        var inputHash = HashPassword(request.Password);

        if (storedHash != inputHash)
        {
            return Results.Unauthorized();
        }

        //успешный вход
        return Results.Ok(new { message = "Вход выполнен успешно", username = request.Username });
    }
    catch (Exception ex)
    {
        return Results.Problem($"Ошибка при входе: {ex.Message}");
    }
})
.WithName("Login")
.WithOpenApi();

//запуск приложения
app.Run();

//инициализация базы данных и создание таблицы пользователей
void InitializeDatabase()
{
    using var connection = new SqliteConnection("Data Source=database.db");
    connection.Open();

    //создание таблицы пользователей, если она не существует
    var createTableCommand = new SqliteCommand(@"
        CREATE TABLE IF NOT EXISTS Users (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            Username TEXT NOT NULL UNIQUE,
            PasswordHash TEXT NOT NULL
        )", connection);
    createTableCommand.ExecuteNonQuery();
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
