namespace MoogleEngine;

public static class Moogle
{

    static int finalResults = 10; // Cantidad de resultados a mostrar en la pagina

    static int minForSuggestion = 2; // Cantidad de resultados minimas para que no salte una sugerencia

    static IndexData data;

    public static void Init() {
        data = new IndexData();
        foreach (var pair in data.Words) {
            
            foreach (var doc in pair.Value) {
                float idf = (float)Math.Log2((float)(data.Docs.Count) / (float)pair.Value.Count);
                doc.Value.Relevance = doc.Value.StartPos.Count * idf;
            }
        }

        System.Console.WriteLine("✅ TF-IDF's calculados");
    }

    public static SearchResult Query(string query) {

        // Parseando la entrada para obtener palabras y operadores
        ParsedInput parsedInput = StringParser.InputParser(query);
        string[] words = parsedInput.Words.ToArray();

        SearchResult result = new SearchResult();

        if (words.Length >= 1) {

            List<PartialItem> partials = new List<PartialItem> ();

            // Agregando las apariciones mas relevantes de cada palabra a una lista
            foreach (var word in words) {
                partials.AddRange(new List<PartialItem> (SearchEngine.GetOneWord(data, word, minForSuggestion, sameRoot: true)));
            }
            List<CumulativeScore> partialResults = SearchEngine.DocsFromPhrase(data, partials, finalResults);

            result = SearchEngine.GetResults(data, partialResults, words);
        }

        return result;
    }
}
