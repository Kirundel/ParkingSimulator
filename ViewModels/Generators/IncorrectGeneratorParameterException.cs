using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ViewModels.Generators
{
    public enum IncorrectGeneratorParameterExceptionType
    {
        MIN,
        MAX,
        MIN_MORE_MAX
    }

    public class IncorrectGeneratorParameterException : Exception
    {
        string _parametername;
        string _value;
        IncorrectGeneratorParameterExceptionType _parametertype;

        public IncorrectGeneratorParameterException(string parametername, string value, IncorrectGeneratorParameterExceptionType parametertype) : base()
        {
            _parametername = parametername;
            _parametertype = parametertype;
            _value = value;
        }

        public override string ToString()
        {
            switch (_parametertype)
            {
                case IncorrectGeneratorParameterExceptionType.MIN:
                    return $"Параметр {_parametername} меньше минимального значения, равного {_value}";
                case IncorrectGeneratorParameterExceptionType.MAX:
                    return $"Параметр {_parametername} больше максимального значения, равного {_value}";
                case IncorrectGeneratorParameterExceptionType.MIN_MORE_MAX:
                    return $"Нижняя граница больше, чем верхняя";
            }
            return "";
        }
    }
}
