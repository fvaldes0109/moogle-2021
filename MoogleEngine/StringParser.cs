using System.Text;
using System.Text.RegularExpressions;

namespace MoogleEngine;

public static class StringParser { // Clase para el manejo y formateo de strings

    public static char IsAlphaNum(char car) { // Revisa si es un caracter alfanumerico valido
        string c = car.ToString().ToLower();
        if (Regex.IsMatch(c, "[a-z0-9áéíóúüç]")) {
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

    public static string[] InputParser(string input) { // Devuelve la lista de palabras de la entrada
        
        List<string> list = new List<string>();

        StringBuilder word = new StringBuilder();
        foreach (char c in input) {

            char parse = IsAlphaNum(c);
            if (parse != '\0') {
                word.Append(parse);
            }
            else {
                if (word.Length > 0) {
                    list.Add(word.ToString());
                    word.Clear();
                }
            }
        }
        if (word.Length != 0) {
            list.Add(word.ToString());
        }

        return list.ToArray();
    }
}