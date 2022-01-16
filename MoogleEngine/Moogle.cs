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
                // Si la palabra aparece en muchos documentos se le asigna un TF-IDF infinitesimal
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

            // Agregando las apariciones de cada palabra a una lista
            foreach (var word in words) {
                partials.AddRange(SearchEngine.GetOneWord(data, word, true, relatedWords: true));
            }
            // Cruza los resultados de las palabras separadas y obtiene los doc mas relevantes
            List<CumulativeScore> partialResults = SearchEngine.DocsFromPhrase(data, partials, parsedInput, finalResults);
            // Genera los resultados finales
            result = SearchEngine.GetResults(data, partialResults, words);
        }

        return result;
    }

    // Devuelve una lista de las palabras ordenadas por la cantidad de documentos en que aparecen
    public static List<Tuple<string, int, int, float>> FrequentWords() {
        
        List<Tuple<string, int, int, float>> result = new List<Tuple<string, int, int, float>>();

        foreach (var word in data.Words) {
            
            int docs = 0; // Cantidad de documentos en que aparece
            int freq = 0; // Cantidad de ocurrencias
            float tfidf = 0.0f; // TF-IDF total

            foreach (var occurrences in word.Value) {
                docs++;
                freq += occurrences.Value.StartPos.Count;
                tfidf += occurrences.Value.Relevance;
            }
            result.Add(new Tuple<string, int, int, float>(word.Key, docs, freq, tfidf));
        }

        return result.OrderByDescending(x => x.Item2).ToList();
    }

    // Devuelve la cantidad de documentos existentes
    public static int DocumentAmount() {
        return data.Docs.Count;
    }
}
