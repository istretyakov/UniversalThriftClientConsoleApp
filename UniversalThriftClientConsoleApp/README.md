# Universal Thrift Client

Универсальный клиент для работы с Thrift-сервисами без генерации клиентского кода. Позволяет динамически сериализовать/десериализовать запросы и ответы, поддерживает сложные типы данных (структуры, списки, карты, множества).

## Как это работает

### 1. **UniversalThriftClient**
- **Сериализация запросов**:
  - Преобразует параметры метода в Thrift-структуру
  - Поддерживает все базовые типы (int, string, bool и т.д.)
  - Обрабатывает вложенные объекты через рефлексию (свойства с атрибутами `[JsonPropertyName("field_X")]`)
  - Корректно работает с коллекциями (List, Dictionary, HashSet)

- **Десериализация ответов**:
  - Преобразует Thrift-ответ в динамический объект
  - Конвертирует результат в указанный тип через System.Text.Json

### 2. **ThriftDeserializer**
- Рекурсивно обрабатывает Thrift-структуры
- Создает динамические объекты с полями вида `field_X` (согласно Thrift-спецификации)
- Поддерживает вложенные типы:
  - Структуры → ExpandoObject
  - Списки → List<object>
  - Карты → Dictionary<object, object>
  - Множества → HashSet<object>

## Пример использования

```csharp
var client = new UniversalThriftClient("http://localhost:9090");

var user = new User
{
    ID = 1,
    Name = "TestUser",
    Faction = new Faction
    {
        ID = 100,
        Name = "Red Team",
        Rank = "Captain"
    }
};

// Вызов метода Thrift-сервиса
var result = await client.CallMethodAsync<User>("GetUser", user);
Console.WriteLine(JsonSerializer.Serialize(result));