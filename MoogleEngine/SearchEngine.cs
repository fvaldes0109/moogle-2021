using System.Text;

namespace MoogleEngine;

public static class SearchEngine {

    public static SearchItem[] GetOneWord(IndexData data, string word, int amount) { // Busca los docs mas relevantes que contengan la palabra

        List<SearchItem> items = new List<SearchItem> ();

        if (!(data.Words.ContainsKey(word))) { // Si la palabra no existe...
            return items.ToArray();
        }
        Location info = data.Words[word];
        int i = 0;
        for (; i < amount && i < info.Size; i++) { // Los docs estan ordenados por TF-IDF

            // Seleccionando el titulo del documento
            string docPath = data.Docs[info[i].Id];
            string[] temp = docPath.Split('/');
            string title = temp[temp.Length - 1].Split('.')[0];

            // DEBE CAMBIARSE. No es optimo. Solo coger el snippet para los resultados finales
            // Seleccionando los limites para el snippet
            int position = info[i].StartPos[0];
            StreamReader reader = new StreamReader(docPath);
            string content = reader.ReadToEnd();
            reader.Close();
            
            StringBuilder snippet = new StringBuilder();
            int start = Math.Max(0, position - 60);
            int end = Math.Min(position + 60, content.Length);
            snippet.Append(content[start .. end]);

            items.Add(new SearchItem(title, snippet.ToString(), info[i].Relevance));
        }

        return items.ToArray();
    }
}