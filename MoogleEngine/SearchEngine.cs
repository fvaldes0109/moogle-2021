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

            items[i] = new SearchItem(title, snippet.ToString(), info[partials[i].Document].Relevance);
        }
        return items;
    }

    public static PartialItem[] DocsFromPhrase(IndexData data, PartialItem[] partials, int amount) {
        
        // Cada documento apuntara al score acumulativo de las palabras que contiene y tambien a las palabras en si
        Dictionary<int, CumulativeScore> relevances = new Dictionary<int, CumulativeScore> ();

        // Iterando por los resultados parciales analizados
        foreach (var partial in partials) {
            
            Location info = data.Words[partial.Word];
            // Si no se ha analizado el documento, se creara su clave
            if (!(relevances.ContainsKey(info[partial.Document].Id))) {
                relevances.Add(info[partial.Document].Id, new CumulativeScore());
            }
            relevances[info[partial.Document].Id].AddWord(info[partial.Document].Relevance, partial);
        }

        // Ordena los documentos por su relevancia total
        var sortedRelevances = relevances.ToList();
        sortedRelevances.OrderByDescending(x => x.Value.TotalScore);
        
        List<PartialItem> result = new List<PartialItem>();
        for (int i = 0; i < amount && i < sortedRelevances.Count; i++) {
            result.Add(sortedRelevances[i].Value.Content[0]);
        }

        return result.ToArray();
    }
}