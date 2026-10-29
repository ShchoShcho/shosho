# ShoSho

Шо-шо ми таке:

WPF-застосунок для локальної колекції медіа. База даних — SQLite через Entity Framework Core 10.

## Запуск

треба мати .NET 10 SDK:

```powershell
dotnet run --project src/ShoSho.UI
```

Під час першого запуску програма автоматично створює каталог
`%LOCALAPPDATA%\ShoSho` та файл `shosho.db`, а міграції створюють таблиці.
Наступні запуски відкривають ту саму базу й застосовують нові міграції, якщо вони є.

Для користувачів можна зібрати комплект із .NET Runtime (приклад для Windows x64):

```powershell
dotnet publish src/ShoSho.UI -c Release -r win-x64 --self-contained true -o publish
```

Передавайте всю папку `publish`; запуск — через `ShoSho.UI.exe`.

## Перевірка (ін прогрес)

```powershell
dotnet build ShoSho.slnx --disable-build-servers -m:1
dotnet run --project tests/ShoSho.SqliteSmoke --no-build
dotnet run --project tests/ShoSho.UiSmoke --no-build
```

Перевірка створює окрему тимчасову SQLite-базу, застосовує міграції двічі,
перевіряє відповідність snapshot, CRUD, повторне відкриття, Unicode, UTC та сортування.
Користувацькі дані вона не змінює.

WPF-перевірка додатково перевіряє фільтри, чернетку редагування, закриття без збереження, правила відгуків і збереження змін. Вона не замінює ручну перевірку інтерфейсу на різних DPI.
