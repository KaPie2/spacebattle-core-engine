# Space Battle: Game Engine Architecture

[![CI Pipeline](https://github.com/KaPie2/spacebattle-core-engine/actions/workflows/ci.yml/badge.svg)](https://github.com/KaPie2/spacebattle-core-engine/actions/workflows/ci.yml)
[![Coverage](https://img.shields.io/badge/Coverage-%3E90%25-brightgreen.svg)]()


Ядро высоконагруженного многопоточного игрового сервера космических баталий. Проект разрабатывается с применением принципов SOLID, строгой инверсии зависимостей (IoC) и многопоточной изоляции (Actor-like Pattern) в рамках курса "Объектно-ориентированный анализ и проектирование" (4-5 семестр).

## Команда разработки (ФИТ-241)
- **Добролежа Олеся Игоревна**
- **Пирогова Екатерина Алексеевна**
- **Фирстова Ксения Максимовна**

## Архитектурные принципы
- **IoC-First:** Полный отказ от оператора `new` для бизнес-логики.
- **Code Generation:** Автоматическая генерация классов-адаптеров "на лету" через Reflection Engine.
- **Thread Confinement:** Выполнение игровых сессий в строго изолированных потоках без применения блокировок `lock`.
- **$O(1)$ Operations:** Остановка операций и проверка коллизий за гарантированное константное время.

## Структура документации
- [DECISIONS.md](DECISIONS.md) — Журнал архитектурных решений (ADR).
- [ROADMAP.md](ROADMAP.md) — План разработки и распределение задач.

## Структура кода

| Папка в `SpaceBattle.Lib` | Содержимое |
| --- | --- |
| `CodeGeneration` | Генерация адаптеров, автоинъекция команд и регистрация фабрик в IoC. TXT-шаблоны находятся в `CodeGeneration/Templates`. |
| `Commands` | Контракты команд, команды движения и поворота, макрокоманды, команды остановки потока (`HardStopCommand`, `SoftStopCommand`) и сжигание топлива (`BurnFuelCommand`). |
| `Collisions` | Проверка коллизий, пространственная сетка `SpatialGrid` и определение пересечений. |
| `GameObjects` | Игровые объекты и интерфейсы: `IUObject`, `UObject`, `IMovingObject`, `IRotatingObject`, `IFuelable`. |
| `Geometry` | Типы для геометрических расчётов, например `NVector` и `Angle`. |
| `IoC` | Общие регистрации и инфраструктура IoC. |
| `Threading` | Игровые потоки, очереди и отправка команд. |
| `Transactions` | Менеджер транзакций (`ITransactionManager`, `TransactionManager`) для атомарных и согласованных изменений. |

Тесты в `SpaceBattle.Lib.Tests` сгруппированы по тем же разделам. Сквозные сценарии находятся в `SpaceBattle.Lib.Tests/Integration`.
