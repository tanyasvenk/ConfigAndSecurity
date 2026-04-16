# ConfigAndSecurity - Защищенная веб-служба на ASP.NET Core

## О проекте

Проект представляет собой защищенную веб-службу на платформе .NET, демонстрирующую комплексный подход к безопасности современных распределенных систем. Реализована многоуровневая архитектура конфигурации, механизмы раннего обнаружения ошибок (fail-fast) и интеллектуальное ограничение трафика.

## Назначение

- Обеспечение безопасности веб-службы на всех уровнях
- Демонстрация принципов fail-fast валидации конфигурации
- Защита от распространенных веб-атак (CORS, Clickjacking, MIME-sniffing)
- Управление нагрузкой через rate limiting
- Поддержка двух режимов работы (Educational/Production)

## Структура проекта

```
ConfigAndSecurity/
├── Config/                          # Конфигурационные классы
│   ├── AppMode.cs                   # Режимы работы приложения
│   ├── AppOptions.cs                # Модель настроек приложения
│   └── AppOptionsValidator.cs       # Валидатор настроек
│
├── Middlewares/                     # Компоненты middleware
│   ├── RequestIdMiddleware.cs       # Генерация уникальных ID запросов
│   ├── ErrorHandlingMiddleware.cs   # Глобальная обработка ошибок
│   └── SecurityHeadersMiddleware.cs # Добавление защитных заголовков
│
├── Domain/                          # Доменные модели
│   └── ErrorResponse.cs             # Модель ответа об ошибке
│
├── Program.cs                       # Точка входа в приложение
├── appsettings.json                 # Базовые настройки
└── appsettings.Production.json      # Настройки для Production режима

ConfigAndSecurity.Tests/             # Тестовый проект
├── BaseTest.cs                      # Базовый класс для тестов
├── ConfigurationPriorityTests.cs    # Тесты приоритета конфигурации
├── IntegrationSecurityTests.cs      # Интеграционные тесты безопасности
├── ModeTests.cs                     # Тесты режимов работы
├── LoadTests.cs                     # Нагрузочные тесты
└── xunit.runner.json                # Настройки xUnit
```

## Функциональные возможности

### 1. Иерархическая конфигурация

Конфигурация загружается из трех источников с четким приоритетом:

| Источник | Метод | Приоритет | Сценарий использования |
|----------|-------|-----------|------------------------|
| appsettings.json | `AddJsonFile()` | Низкий | Значения по умолчанию |
| Переменные окружения | `AddEnvironmentVariables()` | Средний | Специфичные для среды |
| Аргументы командной строки | `AddCommandLine()` | Высокий | Оперативное переключение |

### 2. Fail-Fast валидация

- Автоматическая проверка настроек при запуске приложения
- Валидация URL доверенных источников
- Проверка обязательности полей через DataAnnotations
- Запрет запуска при некорректных настройках

### 3. Защита от междоменных атак (CORS)

- Разрешение запросов только из доверенных источников (TrustedOrigins)
- Белый список разрешенных доменов
- Блокировка всех недоверенных источников

### 4. Защитные заголовки ответа

| Заголовок | Значение | Защита от |
|-----------|----------|-----------|
| X-Frame-Options | DENY | Clickjacking атак |
| X-Content-Type-Options | nosniff | MIME-sniffing атак |
| X-XSS-Protection | 1; mode=block | XSS атак |
| X-Request-Id | Уникальный GUID | Трассировки запросов |

### 5. Ограничение частоты запросов (Rate Limiting)

- **Глобальный лимит**: 100 запросов в минуту (настраивается)
- **Строгий лимит для POST**: 5 запросов в минуту
- **Защита от DoS-атак**: блокировка при превышении лимита
- **Статус ответа**: 429 Too Many Requests

### 6. Режимы работы

| Режим | Сообщения об ошибках | Логирование | Использование |
|-------|---------------------|-------------|---------------|
| **Educational** | Детальные (с текстом ошибки и StackTrace) | Подробное | Разработка и отладка |
| **Production** | Общие ("Внутренняя ошибка сервера") | Минимальное | Боевая эксплуатация |

## Запуск приложения

### Учебный режим (Educational)
```bash
dotnet run
```

### Боевой режим (Production)
```bash
# Через аргументы командной строки
dotnet run --AppSecurity:Mode=Production

# Через переменные окружения (Windows)
set AppSecurity__Mode=Production
dotnet run

# Через переменные окружения (Linux/Mac)
export AppSecurity__Mode=Production
dotnet run
```

### С пользовательскими настройками
```bash
# Настройка доверенных источников
dotnet run --AppSecurity:TrustedOrigins:0=https://example.com --AppSecurity:TrustedOrigins:1=https://another.com

# Настройка лимитов
dotnet run --AppSecurity:RateLimit:GlobalPermitLimit=200 --AppSecurity:RateLimit:WindowMinutes=2
```

## API Endpoints

| Метод | Endpoint | Описание | Rate Limit |
|-------|----------|----------|------------|
| GET | `/api/items` | Получение списка элементов | Глобальный (100/мин) |
| GET | `/api/items/{id}` | Получение элемента по ID | Глобальный (100/мин) |
| POST | `/api/items` | Создание нового элемента | Строгий (5/мин) |
| GET | `/api/error` | Тестовый эндпоинт для проверки обработки ошибок | Глобальный |

## Тестирование

### Структура тестов

| Файл | Количество тестов | Что проверяет |
|------|------------------|---------------|
| `ConfigurationPriorityTests.cs` | 4 | Приоритет источников конфигурации и валидацию |
| `IntegrationSecurityTests.cs` | 5 | CORS, заголовки безопасности, rate limiting, RequestId |
| `ModeTests.cs` | 2 | Режимы работы Educational и Production |
| `LoadTests.cs` | 1 | Защиту от DoS-атак при высокой нагрузке |

### Описание тестов

#### 1. ConfigurationPriorityTests
- **CommandLine_ShouldOverride_EnvironmentVariables**: Проверяет, что аргументы командной строки имеют высший приоритет
- **EnvironmentVariables_ShouldOverride_Json**: Проверяет, что переменные окружения имеют приоритет над JSON
- **InvalidTrustedOrigin_ShouldFailValidation**: Проверяет валидацию некорректных URL
- **EmptyTrustedOrigins_ShouldFailDataAnnotations**: Проверяет, что пустой список доверенных источников недопустим

#### 2. IntegrationSecurityTests
- **Cors_ShouldBlock_UntrustedOrigin**: Проверяет блокировку запросов с недоверенных доменов
- **SecurityHeaders_ShouldBePresent**: Проверяет наличие защитных заголовков
- **RateLimiting_ShouldBlock_ExcessiveRequests**: Проверяет глобальное ограничение частоты запросов
- **StrictRateLimiting_ShouldBlockPostRequests_AfterLimit**: Проверяет строгий лимит для POST запросов
- **RequestId_ShouldBeUnique_ForEachRequest**: Проверяет уникальность идентификаторов запросов

#### 3. ModeTests
- **EducationalMode_ShouldReturn_DetailedErrorMessage**: Проверяет детальные сообщения об ошибках в Educational режиме
- **ProductionMode_ShouldReturn_GenericErrorMessage**: Проверяет общие сообщения об ошибках в Production режиме

#### 4. LoadTests
- **RateLimiting_ShouldProtect_AgainstDosAttack**: Нагрузочный тест - проверяет защиту от DoS-атак при 200 параллельных запросах

### Запуск тестов

#### В Visual Studio (Test Explorer)
1. Откройте **Test Explorer** (`Ctrl + E, T`)
2. Нажмите **Run All** или выберите конкретные тесты
3. Для просмотра деталей нажмите на тест и откройте **Output**

#### Через командную строку
```bash
# Запуск всех тестов
dotnet test

# Запуск с подробным выводом
dotnet test --verbosity detailed

# Запуск конкретного класса
dotnet test --filter "FullyQualifiedName~IntegrationSecurityTests"

# Запуск конкретного теста
dotnet test --filter "Name~Cors_ShouldBlock_UntrustedOrigin"

# Сохранение результатов в файл
dotnet test --verbosity detailed > test-results.txt
```

#### С генерацией отчета
```bash
# HTML отчет
dotnet test --logger "html;logfilename=test-report.html"

# TRX отчет (можно открыть в Visual Studio)
dotnet test --logger "trx;logfilename=results.trx"
```

### Ожидаемые результаты тестов

Все 12 тестов должны проходить успешно:

```
✅ ConfigurationPriorityTests (4 теста)
✅ IntegrationSecurityTests (5 тестов)  
✅ ModeTests (2 теста)
✅ LoadTests (1 тест)

Результат: 12/12 пройдено (100%)
```

## Критичные настройки безопасности

| Настройка | Почему критична | Что проверяем |
|-----------|-----------------|---------------|
| `TrustedOrigins` | Определяет, какие домены могут обращаться к API. Пустой список делает CORS защиту неэффективной | Проверка на пустоту и валидность URL |
| `Mode` | Влияет на информативность ошибок. В Production важно не раскрывать детали внутренней структуры | Корректное переключение режимов |
| `RateLimit` | Защита от DoS атак. Слишком высокие лимиты делают защиту бесполезной | Проверка блокировки при превышении |

## Принципы безопасности

1. **Fail-Fast**: Приложение не запускается с некорректными настройками
2. **Защита по умолчанию**: Минимальные привилегии, явное разрешение
3. **Многоуровневая защита**: CORS + Security Headers + Rate Limiting
4. **Безопасность через конфигурацию**: Режимы работы без изменения кода

## Стек технологий

- **.NET 9.0** - платформа разработки
- **ASP.NET Core** - веб-фреймворк
- **xUnit** - фреймворк для тестирования
- **Microsoft.AspNetCore.Mvc.Testing** - интеграционное тестирование

## Лицензия

Учебный проект для курса "Технологии разработки приложений на базе фреймворков"

---

**Разработано:** В рамках практического занятия №3  
**Преподаватель:** Макиевский Станислав Евгеньевич  
**Семестр:** 6 семестр, 2025-2026 гг.
