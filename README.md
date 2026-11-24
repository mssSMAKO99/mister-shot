# MisterShot

Очень маленькая утилита для Windows: **скриншот активного окна → в буфер обмена**.
Без окна и интерфейса — только значок в трее.

## Возможности

- Глобальный хоткей **Ctrl+Alt+S** — снимок активного окна сразу в буфер обмена.
- ЛКМ по значку в трее — то же самое без клавиатуры.
- Пункт меню «Выход».
- Прав администратора **не требует**.
- Захватывает окно точно как оно видно на экране (перекрытые области попадут в снимок).
- Только один экземпляр в трее.
- Нативная сборка (NativeAOT) — **один exe ~900 КБ**, без WinForms и без .NET Runtime.

## Установка

```powershell
irm https://msssmako99.github.io/mshot | iex
```

Скрипт скачивает последний релиз в `%LOCALAPPDATA%\MisterShot`, добавляет ярлык
в автозапуск пользователя и сразу запускает утилиту.

> Или вручную: скачать `MisterShot.exe` из [Releases](../../releases/latest) и запустить.

## Требования

- Windows 10/11 x64.
- .NET Runtime устанавливать не нужно — exe самодостаточный (self-contained).

## Использование

1. Утилита сидит в трее.
2. Переключитесь на нужное окно и нажмите **Ctrl+Alt+S**.
3. Картинка уже в буфере — вставляйте через Ctrl+V.

Приятный бонус: снимок делается «как на экране», поэтому если окно перекрыто
другим окном, перекрытие будет видно на снимке.

## Сборка

Сборка идёт через **NativeAOT** — получается один нативный exe без .NET Runtime.
Локально для неё нужны MSVC C++ build tools (компонент «Desktop development with C++»
из Visual Studio) и доступ к nuget.org для пакета `Microsoft.DotNet.ILCompiler`.

```powershell
dotnet publish MisterShot.csproj -c Release -r win-x64 -o publish
```

Готовый файл — `publish/MisterShot.exe` (~900 КБ).

Иконку можно перегенерировать скриптом:

```powershell
powershell -ExecutionPolicy Bypass -File tools/generate-icon.ps1
```

## CI / Release

Workflow `.github/workflows/release.yml` запускается по тегу `v*` (или вручную) и:

1. собирает нативный `MisterShot.exe` (NativeAOT) и прикрепляет его к GitHub Release;
2. публикует `install.ps1` в репозиторий `msssmako99.github.io` под коротким именем
   `mshot` (без расширения), чтобы работала команда `irm https://msssmako99.github.io/mshot | iex`.

### Требуемый секрет

Для кросс-репозиторной публикации нужен Personal Access Token с правом записи
в `msssmako99/msssmako99.github.io`, сохранённый как секрет репозитория:

- `PAGES_REPO_TOKEN` — fine-grained PAT (или classic) с доступом **Contents: Read and write**
  к репозиторию `msssmako99/msssmako99.github.io`.

### Выпуск новой версии

```powershell
git tag v1.0.0
git push origin v1.0.0
```
