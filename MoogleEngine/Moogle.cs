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
        
        string[] words = StringParser.InputParser(query);
        SearchItem[] items = new SearchItem[0];

        if (words.Length == 1) {
            PartialItem[] partials = SearchEngine.GetOneWord(data, query, 5);
            items = SearchEngine.GetResults(data, partials);
        }
        else {
            
        }
        return new SearchResult(items, query);
    }
}
