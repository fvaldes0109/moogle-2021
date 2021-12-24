namespace MoogleEngine;

public static class SearchEngine {

    public static SearchItem[] GetOneWord(IndexData data, string word, int amount) { // Busca los docs mas relevantes que contengan la palabra

        List<SearchItem> items = new List<SearchItem> ();

        if (!(data.Words.ContainsKey(word))) {
            return items.ToArray();
        }
        Location info = data.Words[word];
        int i = 0;
        for (; i < amount && i < info.Size; i++) {
            string docPath = data.Docs[info[i].Id];
            items.Add(new SearchItem(docPath, "Probando el cacho", info[i].Relevance));
        }

        return items.ToArray();
    }
}