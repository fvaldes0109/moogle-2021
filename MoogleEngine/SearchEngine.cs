using System.Text;

namespace MoogleEngine;

public static class SearchEngine {

    // Busca los docs mas relevantes que contengan la palabra
    // MinAcceptable indica la cantidad minima de resultados para no generar sugerencias
    public static PartialItem[] GetOneWord(IndexData data, string word, int minAcceptable, float multiplier = 1.0f, string original = "", bool sameRoot = false) { 

        // Para guardar los parciales finales
        List<PartialItem> items = new List<PartialItem> ();
        // Para guardar los parciales de las mismas raices y sugerencias
        List<PartialItem> lowerResults = new List<PartialItem>();

        // Generando las sugerencias si la palabra no existe
        if (!(data.Words.ContainsKey(word))) {
            Tuple<string, float> suggestion = GetSuggestions(data, word);
            if (suggestion.Item1 != "") {
                lowerResults.AddRange(GetOneWord(data, suggestion.Item1, 0, suggestion.Item2, word));
            }
        }
        else {
            Location info = data.Words[word];
            for (int i = 0; i < data.Docs.Count && i < info.Size; i++) { // Los docs estan ordenados por TF-IDF

                items.Add(new PartialItem(word, i, multiplier, original));
            }
            // Si hay muy pocos resultados, generar sugerencias
            if (items.Count < minAcceptable) {
                Tuple<string, float> suggestion = GetSuggestions(data, word);
                if (suggestion.Item1 != "") {
                    items.AddRange(GetOneWord(data, suggestion.Item1, 0, suggestion.Item2, word));
                }
            }
        }

        // Generando las palabras de raiz similar
        if (sameRoot) {
            lowerResults.AddRange(GetSameRoot(data, word));
        }

        items.AddRange(lowerResults);
        return items.ToArray();
    }

    // Genera la lista de resultados finales
    public static SearchItem[] GetResults(IndexData data, PartialItem[] partials) {

        List<SearchItem> items = new List<SearchItem>();

        bool hasRelevant = false;
        for (int i = 0; i < partials.Length; i++) {
            
            Location info = data.Words[partials[i].Word];

            // Si aparece un elemento de relevancia muuuy baja, los siguientes tambien lo seran
            if (info[partials[i].Document].Relevance < 0.0001f && hasRelevant) {
                continue;
            }
            else if (info[partials[i].Document].Relevance > 0.0001f) {
                hasRelevant = true;
            }

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

            items.Add(new SearchItem(title, snippet.ToString(), info[partials[i].Document].Relevance));
        }
        return items.ToArray();
    }

    // Devuelve los resultados parciales de una query frase
    public static PartialItem[] DocsFromPhrase(IndexData data, string[] originalQuery, PartialItem[] partials, int amount) {
        
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
        var sortedRelevances = relevances.OrderByDescending(x => x.Value.TotalScore).ToList();
        
        List<PartialItem> result = new List<PartialItem>();

        // Procediendo a seleccionar las palabras que seran el eje del snippet
        for (int i = 0; i < amount && i < sortedRelevances.Count; i++) {
            
            float maxScore = 0.0f;
            int maxIndex = 0;
            var content = sortedRelevances[i].Value.Content;
            // Buscando la palabra de mayor score entre las que contiene el documento
            for (int j = 0; j < content.Count; j++) {

                float relevance = data.Words[content[j].Word][content[j].Document].Relevance;
                // Queremos evitar escoger palabras muy poco relevantes como pivote
                if (relevance > maxScore && relevance > 0.0001f) {
                    // Si la palabra esta en el query original, sera una candidata como pivote del snippet del doc
                    // Si aun no se ha escogido ninguna palabra, no importa que esta no aparezca en el query original
                    // Si ninguna esta en el query original, simplemente se escogera la 1ra
                    if (ArrayOperation.Find(originalQuery, content[j].Word) != -1 || maxScore == 0.0f) {
                        maxScore = relevance;
                        maxIndex = j;
                    }
                }
            }
            result.Add(sortedRelevances[i].Value.Content[maxIndex]);
        }

        return result.ToArray();
    }

    // Genera la mejor sugerencia para una palabra. Devuelve la palabra y el multiplicador
    static Tuple<string, float> GetSuggestions(IndexData data, string word) {

        // Aqui van todas las derivadas de 'word'
        List<string> derivates = SubWords.GetDerivates(word);
        // Aqui se acumulara el score total de cada sugerencia para determinar la mejor
        Dictionary<string, float> cumulativeWord = new Dictionary<string, float>();
        
        // Buscando entre cada derivada
        foreach (string subword in derivates) {
            if (data.Variations.ContainsKey(subword)) {
                // Buscando entre cada posible origen de la derivada actual
                foreach (string possibleOrigin in data.Variations[subword]) {
                    // Verificando que no sugiera la palabra original
                    if (word != possibleOrigin) {
                        // Hallando la distancia entre la palabra escrita y la sugerencia
                        float priority = 1.0f - (float)SubWords.Distance(word, possibleOrigin) / (float)Math.Max(word.Length,possibleOrigin.Length);
                        // Calculando los TF-IDF de la sugerencia en cada doc
                        if (!(cumulativeWord.ContainsKey(possibleOrigin))) {
                            cumulativeWord[possibleOrigin] = priority;
                        }
                        else {
                            cumulativeWord[possibleOrigin] += priority;
                        }
                    }
                }
            }
        }
        if (cumulativeWord.Count > 0) {
            //Determinando la sugerencia de mayor relevancia
            string result = cumulativeWord.OrderByDescending(x => x.Value).ToList()[0].Key;
            float finalMult = 1.0f / (float)SubWords.Distance(result, word);
            return new Tuple<string, float> (result, finalMult);
        }
        else {
            return new Tuple<string, float>("", 0.0f);
        }
    }

    // Para buscar los resultados de las palabras con la misma raiz
    static List<PartialItem> GetSameRoot(IndexData data, string word) {

        // Buscando los lexemas de la palabra
        List<string> roots = SubWords.GetPrefixes(word);
        List<PartialItem> results = new List<PartialItem>();

        // Iterando por cada lexema
        foreach (string prefix in roots) {
            if (data.Lexems.ContainsKey(prefix)) {
                // Iterando por cada posible origen
                foreach (string possibleOrigin in data.Lexems[prefix]) {
                    if (word != possibleOrigin) {
                        // Distancia entre la nueva palabra y la original
                        float priority = 1.0f - (float)SubWords.Distance(word, possibleOrigin) / (float)Math.Max(word.Length,possibleOrigin.Length);
                        // Buscando la nueva palabra en cada documento
                        List<PartialItem> newResults = new List<PartialItem>(GetOneWord(data, possibleOrigin, 0, priority * 0.001f));
                        results.AddRange(newResults);
                    }
                }
            }
        }
        // Ordenando los resultados
        results = results.OrderByDescending(x => data.Words[x.Word][x.Document].Relevance * x.Multiplier).ToList();
        return results;
    }
}