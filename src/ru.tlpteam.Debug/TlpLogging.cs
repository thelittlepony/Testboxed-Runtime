using System;
using System.Runtime.CompilerServices;
using System.IO;

namespace ru.tlpteam.Debug
{
    /// <summary>
    /// Единый API логирования для скриптов проекта Testboxed.
    /// Выводит время, имя класса, метод и само сообщение.
    /// </summary>
    public static class TlpLogging
    {
        // Основные методы для игрока (скриптов)
        // Теперь они всеядные благодаря object

        public static void Info(object message, [CallerMemberName] string member = "", [CallerFilePath] string file = "")
            => Print("INFO", message?.ToString() ?? "null", ConsoleColor.White, member, file);

        public static void Warning(object message, [CallerMemberName] string member = "", [CallerFilePath] string file = "")
            => Print("WARN", message?.ToString() ?? "null", ConsoleColor.Yellow, member, file);

        public static void Error(object message, [CallerMemberName] string member = "", [CallerFilePath] string file = "")
            => Print("ERR", message?.ToString() ?? "null", ConsoleColor.Red, member, file);

        /// <summary>
        /// Универсальный принт, который собирает всю инфу в кучу.
        /// </summary>
        private static void Print(string tag, string text, ConsoleColor color, string member, string filePath)
        {
            string time = DateTime.Now.ToString("HH:mm:ss");

            // Если Roslyn не передал путь, попробуем вытащить хоть что-то.
            // При динамической компиляции filePath может быть пустой строкой или содержать <динамический_код>
            string className = "UnknownClass";

            if (!string.IsNullOrEmpty(filePath) && filePath.Contains(Path.DirectorySeparatorChar.ToString()))
            {
                className = Path.GetFileNameWithoutExtension(filePath);
            }
            else if (!string.IsNullOrEmpty(filePath))
            {
                // Бывает, что путь приходит просто как имя файла без папок
                className = filePath.Replace(".cs", "");
            }

            // Формируем красивый источник: Класс.Метод()
            string source = $"{className}.{member}";

            Console.ForegroundColor = color;
            Console.WriteLine($"[{tag}] {time} | {source}(): {text}");
            Console.ResetColor();
        }
    }
}