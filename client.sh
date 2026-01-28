#!/bin/bash

#простой консольный клиент для взаимодействия с веб-приложением
BASE_URL="http://localhost:5032"

#функция для отображения меню
show_menu() {
    echo "=== Консольный клиент ==="
    echo "1. Регистрация"
    echo "2. Вход"
    echo "3. Получить простые числа"
    echo "4. Показать историю действий"
    echo "5. Выход"
    echo -n "Выберите действие: "
}

#функция для регистрации
register() {
    echo -n "Введите имя пользователя: "
    read username
    echo -n "Введите пароль: "
    read -s password
    echo

    if [ -z "$username" ] || [ -z "$password" ]; then
        echo "Ошибка: имя пользователя и пароль не могут быть пустыми."
        echo
        return
    fi
    
    response=$(wget -qO- --post-data="{\"username\":\"$username\",\"password\":\"$password\"}" \
        --header="Content-Type: application/json" \
        "$BASE_URL/register" 2>/dev/null)
    
    if [ $? -eq 0 ]; then
        echo "$response" | python3 -m json.tool 2>/dev/null || echo "$response"
    else
        echo "Ошибка подключения. Убедитесь, что приложение запущено."
    fi
    echo
}

#функция для входа
login() {
    echo -n "Введите имя пользователя: "
    read username
    echo -n "Введите пароль: "
    read -s password
    echo

    if [ -z "$username" ] || [ -z "$password" ]; then
        echo "Ошибка: имя пользователя и пароль не могут быть пустыми."
        echo
        return
    fi
    
    response=$(wget -qO- --post-data="{\"username\":\"$username\",\"password\":\"$password\"}" \
        --header="Content-Type: application/json" \
        "$BASE_URL/login" 2>/dev/null)
    
    if [ $? -eq 0 ]; then
        echo "$response" | python3 -m json.tool 2>/dev/null || echo "$response"
    else
        echo "Ошибка подключения. Убедитесь, что приложение запущено."
    fi
    echo
}

#функция для получения простых чисел
get_primes() {
    echo -n "Введите имя пользователя: "
    read username
    echo -n "Введите верхнюю границу (от 2 до 1000000): "
    read limit
    
    response=$(wget -qO- "$BASE_URL/primes/$username/$limit" 2>/dev/null)
    
    if [ $? -eq 0 ]; then
        echo "$response" | python3 -m json.tool 2>/dev/null || echo "$response"
    else
        echo "Ошибка подключения. Убедитесь, что приложение запущено."
    fi
    echo
}

#функция для получения истории действий пользователя
get_history() {
    echo -n "Введите имя пользователя: "
    read username

    response=$(wget -qO- "$BASE_URL/history/$username" 2>/dev/null)

    if [ $? -eq 0 ]; then
        echo "$response" | python3 -m json.tool 2>/dev/null || echo "$response"
    else
        echo "Ошибка подключения. Убедитесь, что приложение запущено."
    fi
    echo
}

#основной цикл
while true; do
    show_menu
    read choice
    
    case $choice in
        1) register ;;
        2) login ;;
        3) get_primes ;;
        4) get_history ;;
        5) echo "До свидания!"; exit 0 ;;
        *) echo "Неверный выбор. Попробуйте снова."; echo ;;
    esac
done


