using System.Text;

namespace MoogleEngine;

// Metodos para el manejo del snippet y la busqueda de palabras en un rango
public static class SnippetOperations {

    // La cantidad de caracteres que tendra el snippet
    static int snippetWidth = 600; 

    // Dado un conjunto de posiciones y sus palabras, obtiene el snippet con mas palabras distintas
    public static string GetSnippet(string docPath, WordPositions positionsStore, bool hasRelevant) {

        int maxPoints = 1; // El maximo de palabras en una vecindad
        int pivot = positionsStore.Positions.ElementAt(0); // El pivote de la vecindad

        // Si tiene palabras relevantes, selecciona el mejor snippet
        // Si son solo palabras mongas, escoge el 1ro. Pues si no se demoraria demasiado
        if (hasRelevant) {
            // Recorriendo todas las posiciones con palabras
            foreach (int pos in positionsStore.Positions) {

                // Calculando la cantidad de puntos en la vecindad
                int points = GetZone(pos, positionsStore, snippetWidth);

                if (maxPoints < points) {
                    maxPoints = points;
                    pivot = pos;
                }
            }
        }

        StreamReader reader = new StreamReader(docPath);
        int left, right; // Los limites de la vecindad

        // El tamaño en bytes del documento
        int docSize = (int)reader.BaseStream.Length;

        // Calculando los limites
        if (pivot - snippetWidth / 4 < 0) { // Si el punto esta muy al comienzo del doc
            left = 0;
            right = snippetWidth;
        }
        // Si esta muy al final
        else if (pivot + snippetWidth - snippetWidth / 4 >= docSize) {
            right = docSize;
            left = Math.Max(0, docSize - snippetWidth);
        }
        else { // Si no esta cerca de los bordes
            left = pivot - snippetWidth / 4;
            right = pivot + snippetWidth - snippetWidth / 4;
        }

        // Colocando el puntero del stream al inicio del snippet
        reader.BaseStream.Position = left;
        
        StringBuilder result = new StringBuilder();

        for (int i = 0; i < snippetWidth; i++) {
            result.Append((char)reader.Read());
        }
        reader.Close();

        return TrimSnippet(result);
    }

    // Busca y elimina caracteres invalidos en los bordes del snippet
    // Pueden existir debido a que se esta trabajando con las posiciones en bytes del documento
    // Si un caracter especial es picado, quedara un caracter invalido, por lo que hay que corregirlo
    // Ya de paso se eliminarian signos de puntuacion, dejando solo un alfanumerico en el borde
    static string TrimSnippet(StringBuilder cad) {

        // Trimeando el principio
        while (ArraysAndStrings.IsAlphaNum(cad[0]) == '\0') {
            cad.Remove(0, 1);
        }
        // Trimeando el final
        while (ArraysAndStrings.IsAlphaNum(cad[cad.Length - 1]) == '\0') {
            cad.Remove(cad.Length - 1, 1);
        }

        return cad.ToString();
    }

    // Calcula los puntos en la vecindad de la palabra
    // La vecindad ira desde (point - width/4 ; point + width - width/4)
    public static int GetZone(int point, WordPositions positionsStore, int width) {
        
        if (positionsStore.Positions.Count == 0) return 1;

        int docSize = positionsStore.Positions.Last();
        int left, right; // Los limites de la vecindad

        // Calculando los limites
        if (point - width / 4 < 0) { // Si el punto esta muy al comienzo del doc
            left = 0;
            right = width;
        }
        else if (point + width - width / 4 >= docSize) { // Si esta muy al final
            right = docSize - 1;
            left = Math.Max(0, docSize - width);
        }
        else {
            left = point - width / 4;
            right = point + width - width / 4;
        }

        // Obteniendo los puntos existentes en el rango
        SortedSet<int> rangeSet = positionsStore.Positions.GetViewBetween(left, right);
        
        // Hallando cuantas palabras diferentes existen en el rango
        HashSet<string> diffWords = new HashSet<string>();
        foreach (int pos in rangeSet) {
            diffWords.Add(positionsStore.Words[pos]);
        }

        return diffWords.Count;
    }

    // Resalta las palabras de la busqueda en el snippet a mostrar al usuario
    public static string HighlightWords(string text, List<PartialItem> partials, bool hasRelevant) {

        StringBuilder result = new StringBuilder(text);

        foreach (var item in partials) {

            // Tomando cada palabra            
            string word = item.Word;
            int[] positions = ArraysAndStrings.Substrings(result.ToString(), word);
            // Cantidad de ediciones realizadas, para saber en cuanto desplazar las posiciones
            int editions = 0;
            // Iterando por cada posicion
            foreach (var pos in positions) {

                result.Insert(pos + editions * 7 + word.Length, "</b>");
                result.Insert(pos + editions * 7, "<b>");
                editions++;
            }
        }
        return result.ToString();
    }
}