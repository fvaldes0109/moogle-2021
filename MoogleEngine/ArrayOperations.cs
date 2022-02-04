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
    public static string WordsToString(string[] array) {

        StringBuilder result = new StringBuilder();
        foreach (string word in array) {
            result.Append(word);
            result.Append(' ');
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
}