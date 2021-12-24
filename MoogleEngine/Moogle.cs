namespace MoogleEngine;

public static class Moogle
{
    static IndexData data;

    public static void Init() {
        data = new IndexData();

        foreach (var pair in data.Words) {
            
            string word = pair.Key;
            Location location = pair.Value;
            
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

        // Asumiendo que la entrada sera una palabra unica
        SearchItem[] items = SearchEngine.GetOneWord(data, query, 5);

        return new SearchResult(items, query);
    }
}
