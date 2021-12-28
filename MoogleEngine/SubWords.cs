using System.Text;

namespace MoogleEngine;

// Clase para la generacion de palabras derivadas para procesar sugerencias
public static class SubWords {

    // Representa el limite de palabras a las que puede apuntar una subpalabra
    // Con 300 esta garantizado que las palabras de 12 letras puedan tener 3 fallos sin margen de error
    static int suggestionLimit = 300; 

    // La cantidad maxima de fallos que el usuario puede cometer en una palabra
    static int maxErrors = 4;

    // El limite de caracteres de una palabra que se pueden descartar para hallar lexemas
    static int suffixLimit = 6;

    static List<string> adding = new List<string>(); // Para llevar la cuenta de las subpalabras generadas

    static HashSet<string> used = new HashSet<string>(); // Para asegurar que no se repitan subpalabras

    // Punto de entrada para la generacion de las palabras derivadas de una palabra
    public static List<string> GetDerivates(string word) {

        adding.Clear();
        used.Clear();

        PushDerivates(word);

        return adding;
    }

    // Metodo para generar los lexemas de una palabra
    public static List<string> GetPrefixes(string word) {
        
        int wordLimit = word.Length - 4; // El limite de caracteres a procesar segun la palabra

        List<string> result = new List<string>();
        StringBuilder temp = new StringBuilder(word);

        // Iterando por cada caracter
        for (int i = 0; i < wordLimit && i < suffixLimit; i++) {
            temp.Remove(temp.Length - 1, 1);
            result.Add(temp.ToString());
        }

        return result;
    }

    static int[,] memo = new int[0,0];
    static bool[,] mk = new bool[0,0];
    // Devuelve las diferencias entre dos palabras (caracteres distintos o diferencia de longitud)
    public static int Distance(string a, string b) {
        
        int m = a.Length;
        int n = b.Length;
        memo = new int[m + 1, n + 1];
        mk = new bool[m + 1, n + 1];

        return EditDistance(a, b, m, n);
    }

    // Algoritmo Edit Distance para calcular las diferencias entre dos palabras
    static int EditDistance(string a, string b, int i, int j) {

        if (i == 0) return j;
        if (j == 0) return i;
        if (mk[i, j]) return memo[i, j];

        mk[i, j] = true;

        if (a[i - 1] == b[j - 1]) {
            memo[i, j] = EditDistance(a, b, i - 1, j - 1);
            return memo[i, j];
        }
        else {
            memo[i, j] = 1 + Math.Min(EditDistance(a, b, i - 1, j - 1), Math.Min(EditDistance(a, b, i - 1, j), EditDistance(a, b, i, j - 1)));
            return memo[i, j];
        }
    }

    // Funcion recursiva para la generacion de derivadas
    static void PushDerivates(string original, int pos = -1) {
        
        // Breakers para dejar de generar (REVISAR)
        if (pos == adding.Count) return;
        if (adding.Count > 0 && adding[adding.Count - 1].Length * 2 - 1 <= original.Length) return;
        if (adding.Count > 0 && adding[adding.Count - 1].Length + maxErrors <= original.Length) return;

        if (pos == -1) { // Caso para generar desde la palabra original
            GenerateSubStrings(original, original);
            pos = 0;
        }
        else { // Caso para generar desde las subpalabras ya generadas
            int startingCount = adding.Count;
            for (; pos < startingCount; pos++) {
                GenerateSubStrings(original, adding[pos]);
            }
        }

        if (adding.Count < suggestionLimit) { // Mientras no se haya superado el limite de generaciones
            PushDerivates(original, pos);
        }
    }

    // Dada una palabra, quitarle 1 caracter de cada posicion y almacenarla
    static void GenerateSubStrings(string original, string subword) {
        
        // Iterando por cada posicion
        for (int i = 0; i < subword.Length && adding.Count < suggestionLimit; i++) {

            string derivate = subword.Remove(i, 1);

            // Si no se ha usado la subpalabra para la original actual:
            if (!(used.Contains(derivate))) {
                adding.Add(derivate);
                used.Add(derivate);
            }
        }
    }
}