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
}