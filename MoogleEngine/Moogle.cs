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
                    double idf = Math.Log2((double)(location.Size) / (double)location.Amount);
                    location[i].Relevance = location[i].StartPos.Count * idf;
                }
            }
        }

        System.Console.WriteLine("✅TF-IDF's calculados");
    }

    public static SearchResult Query(string query) {
        // Modifique este método para responder a la búsqueda

        SearchItem[] items = new SearchItem[3] {
            new SearchItem("Hello World", "Lorem ipsum dolor sit amet", 0.9f),
            new SearchItem("Hello World", "Lorem ipsum dolor sit amet", 0.5f),
            new SearchItem("Hello World", "Lorem ipsum dolor sit amet", 0.1f),
        };

        return new SearchResult(items, query);
    }
}
