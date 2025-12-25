# Инструкция по использованию приложения

## Запуск приложения

1. Запустите веб-приложение:
```bash
dotnet run
```

Приложение будет доступно по адресу: `http://localhost:5032`

## Взаимодействие через консоль

### Способ 1: Консольный клиент (рекомендуется)

Запустите скрипт `client.sh`:
```bash
./client.sh
```

В меню выберите:
- `1` - Регистрация нового пользователя
- `2` - Вход в систему
- `3` - Получить простые числа
- `4` - Выход

### Способ 2: Прямые HTTP-запросы через wget

**Регистрация:**
```bash
wget -qO- --post-data='{"username":"testuser","password":"test123"}' \
  --header="Content-Type: application/json" \
  http://localhost:5032/register
```

**Вход:**
```bash
wget -qO- --post-data='{"username":"testuser","password":"test123"}' \
  --header="Content-Type: application/json" \
  http://localhost:5032/login
```

**Получить простые числа:**
```bash
wget -qO- http://localhost:5032/primes/20
```

### Способ 3: Использование curl (если установлен)

**Регистрация:**
```bash
curl -X POST http://localhost:5032/register \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","password":"test123"}'
```

**Вход:**
```bash
curl -X POST http://localhost:5032/login \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","password":"test123"}'
```

**Получить простые числа:**
```bash
curl http://localhost:5032/primes/20
```

## Доступные эндпоинты

- `POST /register` - Регистрация нового пользователя
  - Тело запроса: `{"username":"имя","password":"пароль"}`
  
- `POST /login` - Вход в систему
  - Тело запроса: `{"username":"имя","password":"пароль"}`
  
- `GET /primes/{limit}` - Получить простые числа до указанного лимита
  - Пример: `/primes/20` вернет `[2,3,5,7,11,13,17,19]`

## База данных

База данных SQLite создается автоматически в файле `database.db` при первом запуске приложения.

