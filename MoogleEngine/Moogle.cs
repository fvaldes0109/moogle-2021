namespace MoogleEngine;

public static class Moogle
{

    static int finalResults = 10; // Cantidad de resultados a mostrar en la pagina

    static int minForSuggestion = 2; // Cantidad de resultados minimas para que no salte una sugerencia

    static IndexData data;

    public static void Init() {
        data = new IndexData();
        foreach (var pair in data.Words) {
            
            string word = pair.Key;
            Location location = pair.Value;
            location.RemoveNull();

            for (int i = 0; i < location.Size; i++) {
                if (location[i] != null) {
                    float idf = (float)Math.Log2((float)(data.Docs.Count) / (float)location.Size);
                    location[i].Relevance = location[i].StartPos.Count * idf;
                }
            }
        }

        foreach (var word in data.Words) { // Ordenando las palabras por su TF-IDF
            word.Value.Sort();
        }

        System.Console.WriteLine("✅ TF-IDF's calculados");
    }

    public static SearchResult Query(string query) {

        // Parseando la entrada para obtener palabras y operadores
        ParsedInput parsedInput = StringParser.InputParser(query);
        string[] words = parsedInput.Words.ToArray();

        SearchItem[] items = new SearchItem[0];
        string suggestions = "";

        if (words.Length >= 1) {

            List<PartialItem> partials = new List<PartialItem> ();

            // Agregando las apariciones mas relevantes de cada palabra a una lista
            foreach (var word in words) {
                partials.AddRange(new List<PartialItem> (SearchEngine.GetOneWord(data, word, minForSuggestion, sameRoot: true)));
            }
            PartialItem[] partialResults = SearchEngine.DocsFromPhrase(data, words, partials.ToArray(), finalResults);
            items = SearchEngine.GetResults(data, partialResults);
            // Generando el string de sugerencias para mostrar
            suggestions = GenerateSuggestionString(words, partialResults);
        }
        // foreach (var item in items) {
        //     System.Console.WriteLine("Titulo: {0} - Score: {1}", item.Title, item.Score);
        // }


        return new SearchResult(items, suggestions);
    }

    static string GenerateSuggestionString(string[] words, PartialItem[] partials) {

        bool changed = false; // Para saber si el string original sera modificado
        foreach (var partial in partials) {
            if (partial.Original != "") { 
                
                int pos = ArrayOperation.Find(words, partial.Original);
                if (pos != -1) {
                    changed = true;
                    words[pos] = partial.Word;
                }
            }
        }
        if (changed) {
            return ArrayOperation.String(words);
        }
        else return "@null";
    }
}
