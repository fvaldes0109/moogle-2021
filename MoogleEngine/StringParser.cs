using System.Text;

namespace MoogleEngine;

public static class StringParser { // Clase para el manejo y formateo de strings

    public static char IsAlphaNum(char car) { // Revisa si es un caracter alfanumerico valido
        string c = car.ToString().ToLower();
        if (char.IsLetterOrDigit(c[0])) {
            return c[0];
        }
        return '\0';
    }

    public static string ParseAccents(string word) {

        StringBuilder result = new StringBuilder();

        foreach (char c in word) {
            char temp = c;
            switch (temp) {
                case 'á':
                case 'ä':
                case 'à':
                case 'â':
                    temp = 'a';
                    break;
                case 'é':
                case 'ë':
                case 'è':
                case 'ê':
                    temp = 'e';
                    break;
                case 'í':
                case 'ï':
                case 'ì':
                case 'î':
                    temp = 'i';
                    break;
                case 'ó':
                case 'ö':
                case 'ò':
                case 'ô':
                    temp = 'o';
                    break;
                case 'ú':
                case 'ü':
                case 'ù':
                case 'û':
                    temp = 'u';
                    break;
            }
            result.Append(temp);
        }
        return result.ToString();
    }

    public static ParsedInput InputParser(string input) { // Devuelve la lista de palabras de la entrada
        
        ParsedInput result = new ParsedInput();

        StringBuilder word = new StringBuilder();
        foreach (char c in input) {

            if (c == '!' || c == '^' || c == '*') { // Si el caracter es un operador
                result.PushOperator(c);
            }
            else if (c == '~') { // Si es el operador ~
                result.PushTilde();
            }

            char parse = IsAlphaNum(c);

            if (parse != '\0') { // Si es alfanumerico, agregarlo a la palabra actual
                word.Append(parse);
            }
            else {
                if (word.Length > 0) { // Si no, termina la palabra y la agrega a la lista
                    result.Words.Add(word.ToString());
                    word.Clear();
                }
            }
        }
        if (word.Length != 0) { // Guardando la ultima palabra
            result.Words.Add(ParseAccents(word.ToString()));
        }
        
        result.TrimEnd(); // Eliminando los posibles operadores sobrantes al final y ajustando tamaños

        return result;
    }
}