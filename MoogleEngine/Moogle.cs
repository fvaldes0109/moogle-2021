namespace MoogleEngine;

public static class Moogle
{
    static int finalResults = 10; // Cantidad de resultados a mostrar en la pagina

    // En este objeto se guardaran todos los datos indexados extraidos de los documentos
    static IndexData? data;

    public static void Init(string[] args) {
        data = new IndexData(args[0] == "index");
    }

    public static SearchResult Query(string query) {

        // Parseando la entrada para obtener palabras y operadores
        ParsedInput parsedInput = new ParsedInput(query);
        string[] words = parsedInput.Words.ToArray();

        SearchResult result = new SearchResult();

        if (words.Length >= 1) {

            List<PartialItem> partials = new List<PartialItem> ();

            // Agregando las apariciones de cada palabra a una lista
            for (int i = 0; i < words.Length; i++) {
                // Si la palabra tiene un operador !, no se generaran sugerencias ni relacionadas
                partials.AddRange(SearchEngine.GetOneWord(data!, words[i],
                suggest: !parsedInput.Operators[i].Contains('!'), relatedWords: !parsedInput.Operators[i].Contains('!')));
            }

            // Contenedor de las palabras que salieron de sugerencias
            List<PartialItem> suggestedWords = new List<PartialItem>();

            // Cruza los resultados de las palabras separadas y obtiene los doc mas relevantes
            List<CumulativeScore> partialResults = SearchEngine.DocsFromPhrase(data!, partials, parsedInput, finalResults, suggestedWords);
            // Genera los resultados finales
            result = SearchEngine.GetResults(data!, partialResults, parsedInput, suggestedWords);
        }

        return result;
    }
}
