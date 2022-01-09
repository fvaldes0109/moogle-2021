namespace MoogleEngine;

public static class Moogle
{

    static int finalResults = 10; // Cantidad de resultados a mostrar en la pagina

    // Si al calcular el IDF la razon Frecuencia / TotalDocs da mayor que el valor, se tomara como 1
    // Esto anula el TF-IDF de las palabras que aparecen en casi todos los documentos
    static float percentToNullify = 0.9f;

    // La 1ra vez que se acceda a esta clase, se indexaran todas las palabras
    static IndexData data = new IndexData();

    public static void Init() {

        // Calculando los TF-IDF
        foreach (var pair in data.Words) {
            
            foreach (var doc in pair.Value) {
                
                float tf = (float)Math.Log2((float)doc.Value.StartPos.Count + 1);
                float idf = (float)Math.Log2((float)(data.Docs.Count) / (float)pair.Value.Count);
                doc.Value.Relevance = ((float)pair.Value.Count / (float)data.Docs.Count < percentToNullify ? tf * idf : 0.0f);
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
                partials.AddRange(SearchEngine.GetOneWord(data, word, true, relatedWords: true));
            }
            List<CumulativeScore> partialResults = SearchEngine.DocsFromPhrase(data, partials, parsedInput, finalResults);

            result = SearchEngine.GetResults(data, partialResults, words);
        }

        return result;
    }
}
