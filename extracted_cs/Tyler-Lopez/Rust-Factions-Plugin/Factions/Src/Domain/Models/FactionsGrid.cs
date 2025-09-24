using Newtonsoft.Json;

namespace Oxide.Plugins
{
    using System.Text;

    public class FactionsGrid
    {
        private readonly byte _columnByte;
        private readonly string _columnString;
        private readonly byte _row;

        private static class Constants
        {
            public const char ColumnFirstChar = 'A';
            public const char ColumnLastBeforeRepeat = 'Z';
            public const char RowColumnDelimiter = ':';
        }

        public FactionsGrid(byte row, byte columnByte)
        {
            _row = row;
            _columnByte = columnByte;
            const int columnRange = (Constants.ColumnLastBeforeRepeat - Constants.ColumnFirstChar) + 1;
            var columnStringBuilder = new StringBuilder();
            for (var excessRanges = 0; excessRanges < (_columnByte / columnRange); excessRanges++)
            {
                columnStringBuilder.Append(Constants.ColumnFirstChar);
            }
            columnStringBuilder.Append((char)(Constants.ColumnFirstChar + (_columnByte % columnRange)));
            _columnString = columnStringBuilder.ToString();
        }

        public byte GetRow()
        {
            return _row;
        }

        public byte GetColumnNumeric()
        {
            return _columnByte;
        }

        public string GetColumnString()
        {
            return _columnString;
        }

        public override string ToString()
        {
            return $"{_columnString}{Constants.RowColumnDelimiter}{_row}";
        }
    }
}
