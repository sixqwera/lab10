using System;
using System.Collections.Generic;
using System.Runtime;
using System.Text;

namespace PascalCompiler
{
    struct BracketInfo // хранит тип скобки и её позицию в тексте
    {
        private byte _bracketSymbol; // код скобки (например 9 = '(')
        private TextPos _pos; // позиция скобки в исходном тексте

        public byte BracketSymbol // свойство доступа к коду скобки
        {
            get
            {
                return _bracketSymbol;
            }
            set
            {
                _bracketSymbol = value;
            }
        }

        public TextPos Pos // свойство доступа к позиции скобки
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

        public BracketInfo(byte bracketSymbol, TextPos pos) // конструктор — принимает код и позицию скобки
        {
            _bracketSymbol = bracketSymbol;
            _pos = pos;
        }
    }
    class LexicalAnalyser // лексический анализатор — превращает текст в поток токенов
    {
        private const byte // числовые коды всех токенов языка
            _star = 21, // *
            _slash = 60, // /
            _equal = 16, // =
            _comma = 20, // ,
            _semicolon = 14, // ;
            _colon = 5, // :
            _point = 61, // .
            _arrow = 62, // ^
            _leftpar = 9,    // (
            _rightpar = 4,   // )
            _lbracket = 11,  // [
            _rbracket = 12,  // ]
            _flpar = 63, // {
            _frpar = 64, // }
            _later = 65, // <
            _greater = 66,   // >
            _laterequal = 67,    //  <=
            _greaterequal = 68,  //  >=
            _latergreater = 69,  //  <>
            _plus = 70,  // +
            _minus = 71, // -
            _lcomment = 72,  //  (*
            _rcomment = 73,  //  *)
            _assign = 51,    //  :=
            _twopoints = 74, //  ..
            _ident = 2,  // идентификатор
            _floatc = 82,    // вещественная константа
            _intc = 15,  // целая константа
            _charc = 83, // символьная константа (добавлено)
            _eofsy = 99, // конец файла (добавлено)

            _casesy = 31,
            _elsesy = 32,
            _filesy = 57,
            _gotosy = 33,
            _thensy = 52,
            _typesy = 34,
            _untilsy = 53,
            _dosy = 54,
            _withsy = 37,
            _ifsy = 56,
            _insy = 100,
            _ofsy = 101,
            _orsy = 102,
            _tosy = 103,
            _endsy = 104,
            _varsy = 105,
            _divsy = 106,
            _andsy = 107,
            _notsy = 108,
            _forsy = 109,
            _modsy = 110,
            _nilsy = 111,
            _setsy = 112,
            _beginsy = 113,
            _whilesy = 114,
            _arraysy = 115,
            _constsy = 116,
            _labelsy = 117,
            _downtosy = 118,
            _packedsy = 119,
            _recordsy = 120,
            _repeatsy = 121,
            _programsy = 122,
            _functionsy = 123,
            _procedurensy = 124;

        private static Dictionary<string, byte> _keywords; // словарь: имя ключевого слова → его код
        private static Stack<BracketInfo> _bracketStack; // стек открытых скобок для проверки парности
        private static byte _symbol; // код текущего распознанного токена
        private static TextPos _token; // позиция текущего токена в тексте
        private static string _addrName; // имя последнего распознанного идентификатора
        private static int _intValue; // значение последней целой константы
        private static float _floatValue; // значение последней вещественной константы
        private static char _charValue; // значение последней символьной константы

        static LexicalAnalyser() // статический конструктор — инициализирует поля один раз при старте
        {
            _keywords = new Dictionary<string, byte>() // заполняем все ключевые слова Pascal и их коды
            {
                { "case", _casesy }, { "else", _elsesy }, { "file", _filesy }, { "goto", _gotosy },
                { "then", _thensy }, { "type", _typesy }, { "until", _untilsy }, { "do", _dosy },
                { "with", _withsy }, { "if", _ifsy }, { "in", _insy }, { "of", _ofsy },
                { "or", _orsy }, { "to", _tosy }, { "end", _endsy }, { "var", _varsy },
                { "div", _divsy }, { "and", _andsy }, { "not", _notsy }, { "for", _forsy },
                { "mod", _modsy }, { "nil", _nilsy }, { "set", _setsy }, { "begin", _beginsy },
                { "while", _whilesy }, { "array", _arraysy }, { "const", _constsy }, { "label", _labelsy },
                { "downto", _downtosy }, { "packed", _packedsy }, { "record", _recordsy }, { "repeat", _repeatsy },
                { "program", _programsy }, { "function", _functionsy }, { "procedure", _procedurensy }
            };
            _bracketStack = new Stack<BracketInfo>();
            _addrName = "";
        }

        public static Dictionary<string, byte> Keywords // публичный доступ к словарю ключевых слов
        {
            get
            {
                return _keywords;
            }
            set
            {
                _keywords = value;
            }
        }
        public static Stack<BracketInfo> BracketStack // публичный доступ к стеку скобок
        {
            get
            {
                return _bracketStack;
            }
            set
            {
                _bracketStack = value;
            }
        }
        public static byte Symbol // публичный доступ к коду текущего токена
        {
            get
            {
                return _symbol;
            }
            set
            {
                _symbol = value;
            }
        }
        public static TextPos Token // публичный доступ к позиции текущего токена
        {
            get
            {
                return _token;
            }
            set
            {
                _token = value;
            }
        }
        public static string AddrName // публичный доступ к имени идентификатора
        {
            get
            {
                return _addrName;
            }
            set
            {
                _addrName = value;
            }
        }
        public static int IntValue // публичный доступ к значению целой константы
        {
            get
            {
                return _intValue;
            }
            set
            {
                _intValue = value;
            }
        }
        public static float FloatValue // публичный доступ к значению вещественной константы
        {
            get
            {
                return _floatValue;
            }
            set
            {
                _floatValue = value;
            }
        }
        public static char CharValue // публичный доступ к значению символьной константы
        {
            get
            {
                return _charValue;
            }
            set
            {
                _charValue = value;
            }
        }

        public static byte Eofsy // только чтение — возвращает код конца файла (99)
        {
            get
            {
                return _eofsy;
            }
        }

        public static byte NextSym() // читает следующий токен из входного потока и возвращает его код
        {
            while (!InputOutput.IsEof && (InputOutput.Ch == ' ' || InputOutput.Ch == '\t' || // пропускаем пробелы и переносы строк
                                         InputOutput.Ch == '\r' || InputOutput.Ch == '\n'))
            {
                InputOutput.NextCh();
            }
            _token.LineNumber = InputOutput.Pos.LineNumber;
            _token.CharNumber = InputOutput.Pos.CharNumber;

            if (InputOutput.IsEof) // конец файла — проверяем незакрытые скобки и возвращаем _eofsy
            {
                CheckBrackets();
                _symbol = _eofsy;
                return _symbol;
            }

            char ch = InputOutput.Ch;

            if (ch >= '0' && ch <= '9') // цифра — читаем целое число
            {
                byte digit = 0; // текущая цифра
                Int16 maxInt = Int16.MaxValue; // верхняя граница допустимого значения (32767)
                _intValue = 0; // сбрасываем накопленное значение

                while (InputOutput.Ch >= '0' && InputOutput.Ch <= '9') // читаем цифры пока они идут подряд
                {
                    digit = (byte)(InputOutput.Ch - '0');
                    if (_intValue < maxInt / 10 || _intValue == maxInt / 10 && digit <= maxInt % 10)
                    {
                        _intValue = _intValue * 10 + digit;
                    }
                    else
                    {
                        InputOutput.Error(203, InputOutput.Pos); // ошибка — число превышает Int16.MaxValue
                        _intValue = 0;
                        while (InputOutput.Ch >= '0' && InputOutput.Ch <= '9') // доедаем оставшиеся цифры
                        {
                            InputOutput.NextCh();
                        }
                        break;
                    }
                    InputOutput.NextCh();
                }
                _symbol = _intc;
                return _symbol;
            }
            if ((ch >= 'a' && ch <= 'z') || // буква или подчёркивание — читаем идентификатор или ключевое слово
                (ch >= 'A' && ch <= 'Z') ||
                (ch == '_'))
            {
                string name = "";
                while ((InputOutput.Ch >= 'a' && InputOutput.Ch <= 'z') || // читаем все буквы, цифры и подчёркивания
                       (InputOutput.Ch >= 'A' && InputOutput.Ch <= 'Z') ||
                       (InputOutput.Ch >= '0' && InputOutput.Ch <= '9') ||
                       (InputOutput.Ch == '_'))
                {
                    name += InputOutput.Ch;
                    InputOutput.NextCh();
                }

                _addrName = name;
                if (Keywords.TryGetValue(name.ToLower(), out byte keywordCode)) // ищем слово в словаре ключевых слов
                {
                    _symbol = keywordCode;
                }
                else
                {
                    _symbol = _ident;
                }
                return _symbol;
            }
            switch (InputOutput.Ch) // определяем токен по символу-оператору
            {
                case '\'':
                    InputOutput.NextCh();
                    if (InputOutput.Ch != '\'')
                    {
                        _charValue = InputOutput.Ch;
                        InputOutput.NextCh();
                        if (InputOutput.Ch == '\'')
                        {
                            InputOutput.NextCh();
                            _symbol = _charc;
                        }
                        else
                        {
                            InputOutput.Error(83, _token);
                            _symbol = _ident;
                        }
                    }
                    else
                    {
                        InputOutput.Error(83, _token);
                        InputOutput.NextCh();
                        _symbol = _ident;
                    }
                    break;
                case '<':
                    InputOutput.NextCh();
                    if (InputOutput.Ch == '=')
                    {
                        _symbol = _laterequal;
                        InputOutput.NextCh();
                    }
                    else if (InputOutput.Ch == '>')
                    {
                        _symbol = _latergreater;
                        InputOutput.NextCh();
                    }
                    else
                    {
                        _symbol = _later;
                    }
                    break;
                case '>':
                    InputOutput.NextCh();
                    if (InputOutput.Ch == '=')
                    {
                        _symbol = _greaterequal;
                        InputOutput.NextCh();
                    }
                    else
                    {
                        _symbol = _greater;
                    }
                    break;
                case ':':
                    InputOutput.NextCh();
                    if (InputOutput.Ch == '=')
                    {
                        _symbol = _assign;
                        InputOutput.NextCh();
                    }
                    else
                    {
                        _symbol = _colon;
                    }
                    break;
                case ';':
                    _symbol = _semicolon;
                    InputOutput.NextCh();
                    break;
                case '.':
                    InputOutput.NextCh();
                    if (InputOutput.Ch == '.')
                    {
                        _symbol = _twopoints;
                        InputOutput.NextCh();
                    }
                    else
                    {
                        _symbol = _point;
                    }
                    break;
                case '=':
                    _symbol = _equal;
                    InputOutput.NextCh();
                    break;
                case ',':
                    _symbol = _comma;
                    InputOutput.NextCh();
                    break;
                case '+':
                    _symbol = _plus;
                    InputOutput.NextCh();
                    break;
                case '-':
                    _symbol = _minus;
                    InputOutput.NextCh();
                    break;
                case '*':
                    _symbol = _star;
                    InputOutput.NextCh();
                    break;
                case '/':
                    _symbol = _slash;
                    InputOutput.NextCh();
                    if (InputOutput.Ch == '/') // если после / идёт ещё / — это однострочный комментарий
                    {
                        while (!InputOutput.IsEof && // читаем до конца строки и игнорируем всё
                               InputOutput.Pos.CharNumber !=
                               InputOutput.LastInLine)
                        {
                            InputOutput.NextCh();
                        }
                    }
                    if (!InputOutput.IsEof)
                    {
                        InputOutput.NextCh();
                    }
                    break;
                case '^':
                    _symbol = _arrow;
                    InputOutput.NextCh();
                    break;
                case '(':
                    _symbol = _leftpar;
                    _bracketStack.Push(new BracketInfo(_leftpar, _token));
                    InputOutput.NextCh();
                    break;
                case '[':
                    _symbol = _lbracket;
                    _bracketStack.Push(new BracketInfo(_lbracket, _token));
                    InputOutput.NextCh();
                    break;
                case '{':
                    while (!InputOutput.IsEof && InputOutput.Ch != '}')
                    {
                        InputOutput.NextCh();
                    }

                    if (!InputOutput.IsEof)
                    {
                        InputOutput.NextCh();
                    }
                    return NextSym();
                case ')':
                    _symbol = _rightpar;
                    CheckClosingBracket(_leftpar);
                    InputOutput.NextCh();
                    break;
                case ']':
                    _symbol = _rbracket;
                    CheckClosingBracket(_lbracket);
                    InputOutput.NextCh();
                    break;
                case '}':
                    _symbol = _frpar;
                    CheckClosingBracket(_flpar);
                    InputOutput.NextCh();
                    break;
                default:
                    InputOutput.Error(34, _token);
                    InputOutput.NextCh();
                    _symbol = _ident;
                    break;
            }
            return _symbol;
        }

        private static void CheckClosingBracket(byte expected) // проверяет что закрывающая скобка парная
        {
            if (_bracketStack.Count == 0) // стек пуст — закрывающая скобка без открывающей → ошибка
            {
                InputOutput.Error(expected, InputOutput.Pos);
                return;
            }
            BracketInfo top = _bracketStack.Pop();
            if (top.BracketSymbol != expected) // тип скобки не совпадает с ожидаемым → ошибка
            {
                InputOutput.Error(expected, InputOutput.Pos);
            }
        }

        private static void CheckBrackets() // вызывается в конце файла — сообщает о всех незакрытых скобках
        {
            BracketInfo unclosed;
            byte errorCode;

            while (_bracketStack.Count > 0) // пока в стеке есть скобки — они не были закрыты
            {
                unclosed = _bracketStack.Pop();

                errorCode = _rightpar;
                if (unclosed.BracketSymbol == _lbracket)
                {
                    errorCode = _rbracket;
                }
                else if (unclosed.BracketSymbol == _flpar)
                {
                    errorCode = _frpar;
                }
                InputOutput.Error(errorCode, unclosed.Pos);
            }
        }
    }
}