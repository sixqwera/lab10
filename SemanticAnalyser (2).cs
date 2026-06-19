using System;
using System.Collections.Generic;
using System.Text;

namespace PascalCompiler
{
    enum DataType
    {
        Unknown,
        Integer,
        Char,
        Interval
    }
    enum SymbolType
    {
        Variable,
        Constant,
        Procedure,
        Function
    }
    class SymbolInfo
    {
        private string _name;
        private DataType _data;
        private SymbolType _type;
        private int _minValue;
        private int _maxValue;

        public SymbolInfo(string name, DataType data, SymbolType type)
        {
            _name = name;
            _data = data;
            _type = type;
            _minValue = 0;
            _maxValue = 0;
        }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
            }
        }
        public DataType Data
        {
            get
            {
                return _data;
            }
            set
            {
                _data = value;
            }
        }
        public SymbolType Type
        {
            get
            {
                return _type;
            }
            set
            {
                _type = value;
            }
        }
        public int MinValue
        {
            get
            {
                return _minValue;
            }
            set
            {
                _minValue = value;
            }
        }
        public int MaxValue
        {
            get
            {
                return _maxValue;
            }
            set
            {
                _maxValue = value;
            }
        }
    }
    class SemanticAnalyser
    {
        private static Dictionary<string, SymbolInfo> _symbolTable;

        static SemanticAnalyser()
        {
            _symbolTable = new Dictionary<string, SymbolInfo>()
            {
                { "writeln", new SymbolInfo("writeln", DataType.Unknown, SymbolType.Procedure) },
                { "readln",  new SymbolInfo("readln",  DataType.Unknown, SymbolType.Procedure) },
                { "write",   new SymbolInfo("write",   DataType.Unknown, SymbolType.Procedure) },
                { "read",    new SymbolInfo("read",    DataType.Unknown, SymbolType.Procedure) },
                { "abs",     new SymbolInfo("abs",     DataType.Integer, SymbolType.Function)  },
                { "sqr",     new SymbolInfo("sqr",     DataType.Integer, SymbolType.Function)  },
                { "ord",     new SymbolInfo("ord",     DataType.Integer, SymbolType.Function)  },
                { "chr",     new SymbolInfo("chr",     DataType.Char,    SymbolType.Function)  },
                { "pred",    new SymbolInfo("pred",    DataType.Unknown, SymbolType.Function)  },
                { "succ",    new SymbolInfo("succ",    DataType.Unknown, SymbolType.Function)  }
            };
        }

        public static void DeclareSymbol(string name, DataType data, SymbolType type, TextPos pos, int min = 0, int max = 0)
        {
            string lowerName = name.ToLower();
            if (_symbolTable.ContainsKey(lowerName))
            {
                InputOutput.Error(114, pos);
            }
            else
            {
                _symbolTable.Add(lowerName, new SymbolInfo(name, data, type) { MinValue = min, MaxValue = max });
            }
        }
        public static SymbolInfo GetSymbol(string name, TextPos pos)
        {
            string lowerName = name.ToLower();
            if (!_symbolTable.ContainsKey(lowerName))
            {
                InputOutput.Error(115, pos);
                return new SymbolInfo(name, DataType.Unknown, SymbolType.Variable);
            }
            return _symbolTable[lowerName];
        }
    }
}
