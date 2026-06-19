using System;
using System.Collections.Generic;
using System.Text;
using System.IO;


namespace PascalCompiler
{
    // Структура для хранения позиции символа в исходном тексте (строка + столбец)
    struct TextPos
    {
        // Номер строки в файле
        private uint _lineNumber;
        // Номер символа (столбца) в строке
        private byte _charNumber;

        // Конструктор с параметрами по умолчанию (0, 0)
        public TextPos(uint ln = 0, byte c = 0)
        {
            _lineNumber = ln;
            _charNumber = c;
        }

        // Свойство доступа к номеру строки
        public uint LineNumber
        {
            get
            {
                return _lineNumber;
            }
            set
            {
                _lineNumber = value;
            }
        }
        // Свойство доступа к номеру символа в строке
        public byte CharNumber
        {
            get
            {
                return _charNumber;
            }
            set
            {
                _charNumber = value;
            }
        }
    }

    // Структура для хранения информации об одной ошибке (позиция + код)
    struct Err
    {
        // Позиция, где произошла ошибка
        private TextPos _errorPosition;
        // Код ошибки
        private byte _errorCode;

        // Конструктор, задающий позицию и код ошибки сразу
        public Err(TextPos errorPosition, byte errorCode)
        {
            this._errorPosition = errorPosition;
            this._errorCode = errorCode;
        }

        // Свойство доступа к позиции ошибки
        public TextPos ErrorPosition
        {
            get
            {
                return this._errorPosition;
            }
            set
            {
                this._errorPosition = value;
            }
        }
        // Свойство доступа к коду ошибки
        public byte ErrorCode
        {
            get
            {
                return this._errorCode;
            }
            set
            {
                this._errorCode = value;
            }
        }
    }

    // Класс, отвечающий за чтение исходного файла и вывод ошибок компиляции
    class InputOutput
    {
        // Максимальное количество ошибок, которое можно зафиксировать
        private const byte ERRMAX = 99;
        // Текущий считанный символ
        private static char _ch;
        // Текущая позиция чтения (строка + столбец)
        private static TextPos _pos;
        // Текущая строка текста, прочитанная из файла
        private static string _line;
        // Индекс последнего символа в текущей строке
        private static byte _lastInLine;
        // Список ошибок (не используется в коде)
        private static List<Err> _err;
        // Поток для чтения исходного файла
        private static StreamReader _file;
        // Счётчик найденных ошибок
        private static uint _errCount;
        // Флаг конца файла
        private static bool _isEof;
        // Словарь: код ошибки -> текст сообщения
        private static Dictionary<byte, string> _errorDict;
        // Список возможных кодов ошибок (не используется в коде)
        private static List<byte> _possibleCodes;
        // Генератор случайных чисел (не используется в коде)
        private static Random _rnd;

        // Статический конструктор - выполняется один раз при первом обращении к классу
        static InputOutput()
        {
            _pos = new TextPos();
            _lastInLine = 0;
            _errCount = 0;
            _line = "";
            _err = new List<Err>();
            _rnd = new Random();
            // Заполнение словаря кодов ошибок и их текстовых описаний
            _errorDict = new Dictionary<byte, string>
            {
                { 34,  "Недопустимый символ в программе (неизвестный знак)" },
                { 83,  "Неверный или пустой символьный литерал (ошибка в константе '...')" },
                { 203, "Целая константа превышает предел (переполнение типа Int16)" },

                { 4,   "Ожидалась закрывающая круглая скобка ')'" },
                { 9,   "Ожидалась открывающая круглая скобка '('" },
                { 11,  "Ожидалась открывающая квадратная скобка '['" },
                { 12,  "Ожидалась закрывающая квадратная скобка ']'" },
                { 63,  "Ожидалась открывающая фигурная скобка '{'" },
                { 64,  "Ожидалась закрывающая фигурная скобка '}'" },

                { 1,   "Имя (идентификатор) не описано" },
                { 2,   "Повторное описание имени" },
                { 3,   "Ожидался идентификатор" },
                { 5,   "Ожидалось двоеточие ':'" },
                { 6,   "Ожидалась точка '.'" },
                { 14,  "Ожидалась точка с запятой ';'" },
                { 16,  "Ожидался знак равенства '='" },
                { 51,  "Ожидался знак присваивания ':='" },
                { 74,  "Ожидалось две точки '..'" },
                { 101, "Ожидалось ключевое слово 'of'" },
                { 104, "Ожидалось ключевое слово 'end'" },
                { 113, "Ожидалось ключевое слово 'begin'" },
                { 114, "Идентификатор уже объявлен" },
                { 115, "Неизвестный идентификатор" },
                { 116, "Попытка изменить константу" },
                { 117, "Несовпадение типов" },
                { 118, "Нельзя присваивать значение процедуре / функции" },
                { 119, "Переменную нельзя вызвать как процедуру" },
                { 120, "Неизвестный тип данных"}
            };
        }
        // Свойство доступа к текущему символу
        public static char Ch
        {
            get
            {
                return _ch;
            }
            set
            {
                _ch = value;
            }
        }
        // Свойство доступа к потоку чтения файла
        public static StreamReader File
        {
            get
            {
                return _file;
            }
            set
            {
                _file = value;
            }
        }
        // Свойство доступа к флагу конца файла
        public static bool IsEof
        {
            get
            {
                return _isEof;
            }
            set
            {
                _isEof = value;
            }
        }

        // Свойство доступа к текущей позиции чтения
        public static TextPos Pos
        {
            get
            {
                return _pos;
            }
            set
            {
                _pos = value;
            }
        }

        // Свойство доступа (только чтение) к индексу последнего символа строки
        public static int LastInLine
        {
            get
            {
                return _lastInLine;
            }
        }

        // Инициализация: подготовка к чтению файла, чтение первой строки
        public static void Init(StreamReader stream)
        {
            _file = stream;
            _errCount = 0;
            _pos = new TextPos(1, 0);
            _err = new List<Err>();
            _isEof = false;

            ReadNextLine();
            ++_pos.LineNumber;
            if (_line.Length > 0)
            {
                _ch = _line[0];
            }
            else
            {
                _ch = ' ';
            }
        }

        // Переход к следующему символу исходного файла
        public static void NextCh()
        {
            if (_isEof)
            {
                return;
            }
            // Если дошли до конца текущей строки - читаем следующую строку
            if (_pos.CharNumber >= _lastInLine)
            {
                ReadNextLine();

                if (_isEof)
                {
                    return;
                }

                ++_pos.LineNumber;
                _pos.CharNumber = 0;
            }
            // Иначе просто сдвигаемся на один символ вправо в той же строке
            else
            {
                ++_pos.CharNumber;
            }

            // Получаем символ по текущей позиции
            if (_line.Length > 0 && _pos.CharNumber < _line.Length)
            {
                _ch = _line[_pos.CharNumber];
            }
            // Если индекс вышел за пределы строки - подставляем пробел
            else
            {
                _ch = ' ';
            }
        }
        // Вывод текущей строки в консоль с номером строки
        private static void ListThisLine()
        {
            Console.WriteLine($"{_pos.LineNumber,4} | {_line}");
        }
        // Чтение следующей строки исходного файла
        private static void ReadNextLine()
        {
            // Если файл ещё не закончился
            if (!_file.EndOfStream)
            {
                // Читаем строку, заменяем табуляцию на 4 пробела
                _line = _file.ReadLine().Replace("\t", "    ");
                // Добавляем пробел в конец строки
                _line += " ";
                // Запоминаем индекс последнего символа строки
                _lastInLine = (byte)(_line.Length - 1);
                // Очищаем список ошибок текущей строки (не используется)
                _err.Clear();

                ListThisLine();
            }
            // Если файл закончился
            else
            {
                _isEof = true;
                _line = "";
                _lastInLine = 0;
                _ch = ' ';
            }
        }
        // Вывод итогового сообщения по завершении компиляции
        public static void End()
        {
            Console.WriteLine($"Компиляция завершена: ошибок - {_errCount}!");
        }

        // Вывод сообщения об ошибке компиляции
        static public void Error(byte errorCode, TextPos pos)
        {
            // Проверка: лимит ошибок не превышен и не находимся в режиме пропуска (панический режим)
            if (_errCount < ERRMAX && !SyntaxAnalyser.Skipping)
            {
                ++_errCount;

                int offset = 7;
                string s1 = "**";
                string s2 = "******";

                // Добавляем ведущий ноль к номеру ошибки, если это не последняя возможная ошибка
                if (_errCount < ERRMAX)
                {
                    s1 += "0";
                }
                s1 += $"{_errCount}**";

                // Добавляем пробелы, чтобы стрелка-указатель встала под нужным символом
                while (s1.Length < offset + pos.CharNumber)
                {
                    s1 += " ";
                    s2 += " ";
                }

                s1 += $"^ ошибка код {errorCode}";

                // Ищем текст сообщения по коду ошибки; если не найден - выводим общее сообщение
                if (!_errorDict.TryGetValue(errorCode, out string message))
                {
                    message = $"Неизвестная синтаксическая ошибка (код {errorCode})";
                }
                s2 += message;

                Console.WriteLine(s1);
                Console.WriteLine(s2);
            }
        }
    }
}