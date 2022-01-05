namespace MoogleEngine;

// Clase para la generacion de palabras derivadas para procesar sugerencias
public static class SubWords {

    // Representa el limite de palabras a las que puede apuntar una subpalabra
    // Con 300 esta garantizado que las palabras de 12 letras puedan tener 3 fallos sin margen de error
    static int suggestionLimit = 300; 

    // La cantidad maxima de fallos que el usuario puede cometer en una palabra
    static int maxErrors = 4;

    static List<string> adding = new List<string>(); // Para llevar la cuenta de las subpalabras generadas

    static HashSet<string> used = new HashSet<string>(); // Para asegurar que no se repitan subpalabras

    // Punto de entrada para la generacion de las palabras derivadas de una palabra
    public static List<string> GetDerivates(string word) {

        adding.Clear();
        used.Clear();

        PushDerivates(word);

        return adding;
    }

    static float[,] memo = new float[0,0];
    static bool[,] mk = new bool[0,0];
    // Devuelve las diferencias entre dos palabras (caracteres distintos o diferencia de longitud)
    public static float Distance(string a, string b) {
        
        int m = a.Length;
        int n = b.Length;
        memo = new float[m + 1, n + 1];
        mk = new bool[m + 1, n + 1];

        float d1 = EditDistance(a, b, m, n);
        
        // Debido a que el costo de edicion es menor, se calculara la distancia desde el inicio
        // de las cadenas, para contrarrestar los posibles casos en que falle
        // Ejemplo: Cuando el cambio es una adicion al principio de la cadena, devolvia 1 en vez de 1.5

        memo = new float[m + 1, n + 1];
        mk = new bool[m + 1, n + 1];

        float d2 = EditDistance(a.Reverse().ToString(), b.Reverse().ToString(), m, n);
        return Math.Max(d1, d2);
    }

    // Algoritmo Edit Distance para calcular las diferencias entre dos palabras
    static float EditDistance(string a, string b, int i, int j) {

        if (i == 0) return j;
        if (j == 0) return i;
        if (mk[i, j]) return memo[i, j];

        mk[i, j] = true;

        if (a[i - 1] == b[j - 1]) {
            memo[i, j] = EditDistance(a, b, i - 1, j - 1);
            return memo[i, j];
        }
        else {
            // Se le dara menos costo a la edicion de un caracter
            memo[i, j] = Math.Min(1.0f + EditDistance(a, b, i - 1, j - 1), Math.Min(1.5f + EditDistance(a, b, i - 1, j), 1.5f + EditDistance(a, b, i, j - 1)));
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