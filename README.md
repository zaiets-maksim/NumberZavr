# PhoneBot — Telegram бот для видачі номерів

## Структура

```
PhoneBot/
├── Program.cs        — точка входу, webhook endpoint
├── BotHandler.cs     — вся логіка бота (повідомлення, кнопки)
├── DataService.cs    — читання/запис data.json
├── Models.cs         — моделі даних
├── appsettings.json  — конфігурація
└── Dockerfile        — для Render.com
```

## Конфігурація

Всі значення краще передавати через **Environment Variables** на Render:

| Змінна | Значення |
|--------|---------|
| `BotToken` | Токен від @BotFather |
| `AdminId` | Твій Telegram ID (число) |
| `WebhookUrl` | `https://YOUR_APP.onrender.com` |
| `DataFilePath` | `/data/data.json` (persistent disk) |

> Як дізнатися свій Telegram ID: напиши @userinfobot

## Деплой на Render.com

1. Запушити код у GitHub репозиторій
2. Render → **New** → **Web Service** → підключити репо
3. **Runtime**: Docker (Render автоматично знайде `Dockerfile`)
4. **Add Persistent Disk**: Mount Path `/data`, розмір 1 GB
5. Додати Environment Variables (таблиця вище)
6. **Deploy** — Render побудує образ і запустить бот

## Функціонал

- **📋 Номер** — видає номер телефону з бази, ліміт 2 рази / 24 год
  - Номер відображається як `код` — на мобільному одне натискання копіює
  - Роздача round-robin (рівномірно між усіма номерами)
- **⚙️ Налаштування** — тільки для адміна (перевірка по Telegram ID)
  - ➕ Додати номер — бот просить ввести номер текстом
  - ➖ Видалити номер — показує список кнопками, обираєш і видаляєш

## Локальний запуск

```bash
dotnet run
# потрібен ngrok або cloudflared для webhook:
ngrok http 8080
# WebhookUrl = https://xxxx.ngrok.io
```
