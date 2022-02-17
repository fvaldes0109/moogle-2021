using System.Text;

namespace MoogleEngine;

// Algunas operaciones basicas sobre un array
public static class ArrayOperations {

    // Busca un string en el arreglo
    public static int Find(string[] array, string word) {
        for (int i = 0; i < array.Length; i++) {
            if (array[i] == word) return i;
        }
        return -1;
    }

    // Convierte un arreglo de string (palabras) a un string representando una frase
    public static string WordsToString(string[] array, ParsedInput input) {

        StringBuilder result = new StringBuilder();

        int i = 0;
        // Agregando los operadores y la palabra en su posicion
        foreach (string word in array) {

            if (input.Operators[i] != "") result.Append(input.Operators[i]);

            result.Append(word);
            result.Append(' ');

            if (input.Tildes[i]) result.Append("~ ");

            i++;
        }
        result.Remove(result.Length - 1, 1);
        return result.ToString();
    }

    // Invierte un string
    public static string Reverse(string s) {

        char[] charArray = s.ToCharArray();
        Array.Reverse(charArray);
        return new string(charArray);
    }

    // Devuelve un array con las posiciones donde aparece la subcadena enviada
    public static int[] Substrings(string cad, string substr) {

        StringBuilder dynamic = new StringBuilder(cad);
        List<int> positions = new List<int>();

        bool found = false;
        do {
            // Si existe el substring dentro de la cadena
            if (dynamic.ToString().Contains(substr, StringComparison.OrdinalIgnoreCase)) {
                // Agregar su posicion a la lista
                int pos = dynamic.ToString().IndexOf(substr, StringComparison.OrdinalIgnoreCase);
                // Si lo encontrado fue una palabra, agregarla a la lista de posiciones
                // Revisando que no hayan otras letras a la izquierda
                if (pos == 0 || !char.IsLetterOrDigit(dynamic[pos - 1])) {
                    // Revisando que no hayan otras letras a la derecha
                    if (pos + substr.Length == cad.Length || ( pos + substr.Length < cad.Length && !char.IsLetterOrDigit(dynamic[pos + substr.Length]))) {
                        positions.Add(pos);
                    }
                }
                // Reemplaar el caracter para que no vuelva a aparecer en el resultado
                dynamic[pos] = '*';
                found = true;
            }
            else {
                found = false;
            }
        } while (found);
        return positions.ToArray();
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
        if (a[i - 1] == b[j - 1]) memo[i, j] = EditDistance(a, b, i - 1, j - 1);
        else 
            memo[i, j] = 1 + Math.Min(EditDistance(a, b, i - 1, j - 1), Math.Min(EditDistance(a, b, i - 1, j), EditDistance(a, b, i, j - 1)));
        return memo[i, j];
    }
}