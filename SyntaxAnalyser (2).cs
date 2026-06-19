using System;
using System.Collections.Generic;

namespace PascalCompiler
{
    internal class SyntaxAnalyser // Класс синтаксического анализатора (парсера)
    {
        private static byte _sym; // Текущий считанный символ (код токена)
        private static HashSet<byte> _baseConsts; // Набор допустимых базовых констант
        private static bool _skipping; // Флаг состояния пропуска токенов при ошибке

        // Множества восстановления для нейтрализации ошибок (Панический режим)
        private static HashSet<byte> _statementRecovery; // Синхронизирующие токены для операторов
        private static HashSet<byte> _declRecovery; // Синхронизирующие токены для объявлений

        static SyntaxAnalyser() // Статический конструктор для инициализации полей
        {
            _baseConsts = new HashSet<byte> { 15, 82, 83 }; // intc, floatc, charc
            _sym = 0;
            _skipping = false;

            // Токены для восстановления при ошибках в операторах (например: ; , end)
            _statementRecovery = new HashSet<byte> { 14, 104, 64 };
            // Токены для восстановления в блоке объявлений
            _declRecovery = new HashSet<byte> { 14, 110, 101 }; // ;, var, begin
        }

        public static bool Skipping => _skipping; // Свойство для получения статуса пропуска токенов

        private static void GetSym() // Метод получения следующего токена от лексического анализатора
        {
            _sym = LexicalAnalyser.NextSym();
            _skipping = false;
        }

        // Проверка текущего символа и переход к следующему
        private static void Accept(byte expected, byte errorCode) // Сравнение текущего токена с ожидаемым
        {
            if (_sym == expected)
            {
                GetSym();
            }
            else
            {
                InputOutput.Error(errorCode, LexicalAnalyser.Token);
                // Нейтрализация: пытаемся пропустить ошибочный символ
                if (!InputOutput.IsEof) GetSym();
            }
        }

        // Метод нейтрализации ошибок (Панический режим) - Задание 2 и 3
        private static void SkipTo(HashSet<byte> recoverySet, byte errorCode) // Пропуск токенов до синхронизирующего множества
        {
            InputOutput.Error(errorCode, LexicalAnalyser.Token);
            _skipping = true;

            while (!InputOutput.IsEof && !recoverySet.Contains(_sym)) // Цикл поиска токена для возобновления парсинга
            {
                _sym = LexicalAnalyser.NextSym();
            }
        }

        // Главная точка входа в парсер
        public static void Parse() // Запуск процесса синтаксического анализа
        {
            GetSym();
            ProgramBlock();
        }

        private static void ProgramBlock() // Разбор структуры всей программы (от program до .)
        {
            Accept(100, 100); // Ожидается 'program'
            Accept(2, 3);     // Имя программы (идентификатор)
            Accept(14, 14);   // Ожидается ';'

            // Разбор блока объявлений (если есть var)
            if (_sym == 110) // 'var'
            {
                VarDeclarationsPart();
            }

            // Главный составной оператор программы
            CompoundStatement();
            Accept(61, 61);   // Ожидается точка '.' в конце программы
        }

        // ==========================================
        // ВАРИАНТ 6: Описание переменных простых типов
        // ==========================================
        private static void VarDeclarationsPart() // Обработка блока объявления переменных (var)
        {
            Accept(110, 110); // 'var'

            while (_sym == 2) // Пока идут идентификаторы переменных
            {
                List<string> varNames = new List<string>();

                // Сбор списка переменных через запятую (например: a, b, c)
                varNames.Add(LexicalAnalyser.AddrName);
                Accept(2, 3);

                while (_sym == 20) // ',' // Цикл для перечисления переменных через запятую
                {
                    GetSym();
                    if (_sym == 2)
                    {
                        varNames.Add(LexicalAnalyser.AddrName);
                        Accept(2, 3);
                    }
                    else
                    {
                        SkipTo(_declRecovery, 3); // Ошибка в списке идентификаторов
                    }
                }

                Accept(5, 5); // Ожидается ':'

                // Разбор простого типа данных
                DataType currentType = DataType.Unknown; // Инициализируем тип по умолчанию
                if (_sym == 111) // 'integer' // Проверка, является ли тип целочисленным
                {
                    currentType = DataType.Integer;
                    GetSym();
                }
                else if (_sym == 112) // 'char' // Проверка, является ли тип символьным
                {
                    currentType = DataType.Char;
                    GetSym();
                }
                else
                {
                    InputOutput.Error(116, LexicalAnalyser.Token); // Неизвестный или сложный тип
                    SkipTo(_declRecovery, 116);
                }

                // Регистрируем переменные в таблице символов (Семантика)
                foreach (var name in varNames) // Добавление всех распознанных переменных в память анализатора
                {
                    SemanticAnalyser.DeclareSymbol(name, currentType, SymbolType.Variable, LexicalAnalyser.Token);
                }

                Accept(14, 14); // Ожидается ';'
            }
        }

        // ==========================================
        // ВАРИАНТ 6: Синтаксический анализ выражений
        // ==========================================
        public static DataType Expression() // Разбор выражений (включая операции отношения)
        {
            DataType type = SimpleExpression();

            // Операции отношения: =, <, >, <=, >=, <>
            if (_sym == 16 || _sym == 65 || _sym == 66 || _sym == 67 || _sym == 68 || _sym == 69) // Проверка наличия знака сравнения
            {
                GetSym();
                DataType rightType = SimpleExpression();
                if (type != rightType) // Проверка совпадения типов левого и правого операндов
                {
                    InputOutput.Error(117, LexicalAnalyser.Token); // Ошибка семантики: несовпадение типов
                }
                type = DataType.Integer; // Результат логического сравнения в учебном Паскале часто Integer/Boolean
            }
            return type;
        }

        private static DataType SimpleExpression() // Разбор простых выражений (+, -, or)
        {
            // Возможный унарный плюс или минус
            if (_sym == 22 || _sym == 23) GetSym(); // Обработка знака перед выражением

            DataType type = Term();

            // Аддитивные операции: +, -, or
            while (_sym == 22 || _sym == 23 || _sym == 113) // Выполнение цепочки сложений/вычитаний
            {
                GetSym();
                DataType rightType = Term();
                if (type != rightType) InputOutput.Error(117, LexicalAnalyser.Token); // Ошибка типов в аддитивной операции
            }
            return type;
        }

        private static DataType Term() // Разбор слагаемых (*, /, div, mod, and)
        {
            DataType type = Factor();

            // Мультипликативные операции: *, /, div, mod, and
            while (_sym == 21 || _sym == 60 || _sym == 114 || _sym == 115 || _sym == 116) // Выполнение цепочки умножений/делений
            {
                GetSym();
                DataType rightType = Factor();
                if (type != rightType) InputOutput.Error(117, LexicalAnalyser.Token); // Ошибка типов в мультипликативной операции
            }
            return type;
        }

        private static DataType Factor() // Разбор множителей (переменная, константа, скобки)
        {
            DataType type = DataType.Unknown;

            if (_sym == 2) // Идентификатор (переменная) // Извлекаем информацию о переменной из таблицы
            {
                SymbolInfo info = SemanticAnalyser.GetSymbol(LexicalAnalyser.AddrName, LexicalAnalyser.Token);
                type = info.Data;
                GetSym();
            }
            else if (_sym == 15) // Целая константа
            {
                type = DataType.Integer;
                GetSym();
            }
            else if (_sym == 83) // Символьная константа
            {
                type = DataType.Char;
                GetSym();
            }
            else if (_sym == 9) // Открывающая скобка '('
            {
                GetSym();
                type = Expression();
                Accept(4, 4); // Ожидается ')'
            }
            else if (_sym == 117) // 'not'
            {
                GetSym();
                type = Factor();
            }
            else
            {
                InputOutput.Error(34, LexicalAnalyser.Token); // Ошибка в выражении
                GetSym();
            }

            return type;
        }

        // ==========================================
        // ВАРИАНТ 6: Операторы
        // ==========================================
        private static void Statement() // Маршрутизация парсинга в зависимости от типа оператора
        {
            switch (_sym)
            {
                case 101: // 'begin' -> Составной оператор
                    CompoundStatement();
                    break;

                case 2: // Идентификатор -> Оператор присваивания
                    AssignmentStatement();
                    break;

                case 105: // 'if' -> Условный оператор
                    IfStatement();
                    break;

                case 107: // 'case' -> Оператор выбора
                    CaseStatement();
                    break;

                case 109: // 'for' -> Цикл с параметром
                    ForStatement();
                    break;

                default:
                    // Если токен не распознан как оператор, нейтрализуем ошибку
                    SkipTo(_statementRecovery, 14);
                    break;
            }
        }

        // 1. Составной оператор (begin ... end)
        private static void CompoundStatement() // Обработка блока кода между begin и end
        {
            Accept(101, 101); // Ожидается 'begin'

            Statement();
            while (_sym == 14) // ';'
            {
                GetSym();
                Statement();
            }

            Accept(104, 104); // Ожидается 'end'
        }

        // Вспомогательный оператор присваивания (нужен для тела циклов и обычных действий)
        private static void AssignmentStatement() // Обработка операции присваивания (:=)
        {
            string varName = LexicalAnalyser.AddrName;
            SymbolInfo varInfo = SemanticAnalyser.GetSymbol(varName, LexicalAnalyser.Token);

            Accept(2, 3);     // Идентификатор переменной
            Accept(51, 51);   // Ожидается ':='

            DataType exprType = Expression();

            if (varInfo.Data != exprType && varInfo.Data != DataType.Unknown) // Сверка типа переменной и присваиваемого значения
            {
                InputOutput.Error(117, LexicalAnalyser.Token); // Семантическая ошибка: несовпадение типов
            }
        }

        // 2. Условный оператор (if ... then ... else)
        private static void IfStatement() // Обработка ветвления if
        {
            Accept(105, 105); // 'if'

            DataType condType = Expression(); // Условие

            Accept(106, 106); // 'then'
            Statement();      // Оператор после then

            if (_sym == 108) // 'else' (необязательная ветка) // Проверка на наличие и обработка блока else
            {
                GetSym();
                Statement();
            }
        }

        // 3. Оператор выбора (case ... of ... end)
        private static void CaseStatement() // Обработка множественного выбора case
        {
            Accept(107, 107); // 'case'

            DataType selectorType = Expression(); // Выражение-селектор

            Accept(118, 118); // Ожидается 'of'

            while (_sym == 15 || _sym == 83 || _sym == 2) // Константы выбора // Разбор конкретных веток case
            {
                // Разбор списка констант одной ветки
                if (_baseConsts.Contains(_sym) || _sym == 2)
                {
                    GetSym();
                }

                while (_sym == 20) // ',' если констант несколько // Чтение нескольких констант для одной ветки
                {
                    GetSym();
                    if (_baseConsts.Contains(_sym) || _sym == 2) GetSym(); // Пропуск валидного токена константы
                }

                Accept(5, 5); // ':'
                Statement();  // Оператор ветки

                if (_sym == 14) // ';' // Пропуск точки с запятой после выполнения ветки
                {
                    GetSym();
                }
            }

            Accept(104, 104); // Ожидается 'end'
        }

        // 4. Цикл с параметром (for ... to/downto ... do)
        private static void ForStatement() // Обработка цикла for
        {
            Accept(109, 109); // 'for'

            string varName = LexicalAnalyser.AddrName;
            SymbolInfo counterSymbol = SemanticAnalyser.GetSymbol(varName, LexicalAnalyser.Token);

            Accept(2, 3);   // Переменная-счетчик
            Accept(51, 51); // ':='

            DataType startExprType = Expression(); // Начальное значение

            if (counterSymbol.Data != startExprType) // Сверка типа счетчика и начального значения
            {
                InputOutput.Error(117, LexicalAnalyser.Token); // Ошибка типов
            }

            if (_sym == 103 || _sym == 119) // 'to' или 'downto' (например, 103 и 119) // Проверка направления шага цикла
            {
                GetSym();
            }
            else // Ошибка: отсутствует to или downto
            {
                InputOutput.Error(103, LexicalAnalyser.Token); // Ожидалось 'to'/'downto'
            }

            DataType endExprType = Expression(); // Конечное значение
            if (counterSymbol.Data != endExprType)
            {
                InputOutput.Error(117, LexicalAnalyser.Token);
            }

            Accept(54, 54); // 'do'
            Statement();    // Тело цикла
        }
    }
}