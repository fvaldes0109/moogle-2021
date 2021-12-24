using System.Text;

namespace MoogleEngine;

public static class SearchEngine {

    public static PartialItem[] GetOneWord(IndexData data, string word, int amount) { // Busca los docs mas relevantes que contengan la palabra

        List<PartialItem> items = new List<PartialItem> ();

        if (!(data.Words.ContainsKey(word))) { // Si la palabra no existe...
            return items.ToArray();
        }
        Location info = data.Words[word];
        for (int i = 0; i < amount && i < info.Size; i++) { // Los docs estan ordenados por TF-IDF

            items.Add(new PartialItem(word, i));
        }
        return items.ToArray();
    }

    // Genera la lista de resultados finales
    public static SearchItem[] GetResults(IndexData data, PartialItem[] partials) {

        SearchItem[] items = new SearchItem[partials.Length];

        for (int i = 0; i < partials.Length; i++) {
            
            Location info = data.Words[partials[i].Word];

            string docPath = data.Docs[info[partials[i].Document].Id];
            string[] temp = docPath.Split('/');
            string title = temp[temp.Length - 1].Split('.')[0];

            int position = info[partials[i].Document].StartPos[0];
            StreamReader reader = new StreamReader(docPath);
            string content = reader.ReadToEnd();
            reader.Close();
            
            StringBuilder snippet = new StringBuilder();
            int start = Math.Max(0, position - 60);
            int end = Math.Min(position + 60, content.Length);
            snippet.Append(content[start .. end]);

            items[i] = new SearchItem(title, snippet.ToString(), info[i].Relevance);
        }
        return items;
    }
}