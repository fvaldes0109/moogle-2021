using System.Text;
using System.Text.RegularExpressions;

namespace MoogleEngine;

public static class StringParser { // Clase para el manejo y formateo de strings

    public static char IsAlphaNum(char car) { // Revisa si es un caracter alfanumerico valido
        string c = car.ToString().ToLower();
        if (Regex.IsMatch(c, "[a-z0-9áéíóúüçñ]")) {
            switch (c) {
                case "á":
                    c = "a";
                    break;
                case "é":
                    c = "e";
                    break;
                case "í":
                    c = "i";
                    break;
                case "ó":
                    c = "o";
                    break;
                case "ú":
                case "ü":
                    c = "u";
                    break;
            }
            return c[0];
        }
        return '\0';
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
            result.Words.Add(word.ToString());
        }
        
        result.TrimEnd(); // Eliminando los posibles operadores sobrantes al final y ajustando tamaños

        return result;
    }
}