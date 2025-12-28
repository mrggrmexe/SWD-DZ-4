# AsyncShopPlatform

Домашняя работа 4 по дисциплине **«Конструирование программного обеспечения»**  
Тема: **Асинхронное межсервисное взаимодействие**

---

## Описание проекта

**SWD-DZ-4 (Gozon)** — микросервисная система для обработки заказов и платежей
с асинхронным взаимодействием через брокер сообщений.

Проект реализует следующие требования:

- API Gateway (YARP Reverse Proxy) — маршрутизация HTTP-запросов
- OrdersService — управление заказами
- PaymentsService — управление счетами и платежами
- Асинхронное взаимодействие через RabbitMQ
- Гарантия at-least-once delivery + идемпотентная обработка
- Transactional Outbox / Inbox
- Отдельный Frontend (React + Vite), упакованный в Docker
- Запуск всей системы через Docker Compose
- CI (GitHub Actions): build → run → test

---

## Архитектура

### Сервисы

- **ApiGateway**
  - YARP Reverse Proxy
  - Единая точка входа в систему
  - Health endpoints (/health, /health/live)

- **OrdersService**
  - Создание заказа
  - Получение списка заказов
  - Обновление статуса заказа
  - Публикация события OrderCreated (Outbox)

- **PaymentsService**
  - Создание счёта (1 счёт на пользователя)
  - Пополнение баланса
  - Проверка баланса
  - Обработка платежей (Inbox)
  - Публикация PaymentSucceeded / PaymentFailed

- **Frontend**
  - React + Vite
  - Работа через ApiGateway
  - Упакован в Docker (Nginx + SPA fallback)

### Инфраструктура

- RabbitMQ (AMQP)
- PostgreSQL (отдельная БД на сервис)
- Docker / Docker Compose

---

## Структура репозитория

```
SWD-DZ-4/
├── deploy/
│   └── docker-compose.yml
|
├── AsyncShopPlatform/
|
├── src/
│   ├── ApiGateway/
│   ├── OrdersService/
│   ├── PaymentsService/
│   ├── Frontend/
│   ├── Contracts/
│   └── Common/
├── tests/
├── .github/workflows/ci.yml
└── README.md
```

---

## Запуск проекта

### Требования
- Docker
- Docker Compose

### Запуск


```bash
docker compose -f deploy/docker-compose.yml up -d --build
```

### Проверка

- Frontend: http://localhost:8083
- ApiGateway: http://localhost:8080
- RabbitMQ UI: http://localhost:15672
- Health: http://localhost:8080/health

---

## CI

GitHub Actions workflow:
- Сборка backend (.NET 9)
- Сборка frontend (Node.js)
- Поднятие системы через docker-compose
- Smoke-check
- Запуск автоматических тестов

---

## Автор

**Штукмайстер Г.П. БПИ246**  
Дисциплина: *Конструирование программного обеспечения*
