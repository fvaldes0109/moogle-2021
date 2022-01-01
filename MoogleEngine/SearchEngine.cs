using System.Text;

namespace MoogleEngine;

public static class SearchEngine {

    // El score minimo necesario para que un documento se muestre
    static float minScore = 0.0001f; 

    // La cantidad de caracteres que tendra el snippet
    static int snippetWidth = 150; 

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
    public static SearchResult GetResults(IndexData data, List<CumulativeScore> docsData, string[] originalWords) {

        // Aqui va la lista de resultados
        List<SearchItem> items = new List<SearchItem>();
        // Aqui van las palabras encontradas en los documentos. Se usara para las sugerencias
        HashSet<PartialItem> suggestedWords = new HashSet<PartialItem>();

        bool hasRelevant = false; // Indicador de si en los resultados hay docs de alta relevancia
        // Procesando cada documento a mostrar
        for (int i = 0; i < docsData.Count; i++) {
            
            // Obteniendo datos del documento
            int docID = data.Words[docsData[i].Content[0].Word][docsData[i].Content[0].Document].Id;

            // Obteniendo el nombre del doc
            string docPath = data.Docs[docID];
            string[] temp = docPath.Split('/');
            string title = temp[temp.Length - 1].Split('.')[0];

            float docScore = docsData[i].TotalScore; // Score del doc

            if (docScore <= minScore && hasRelevant) continue; // Si el documento es poco relevante, saltarselo
            else if (docScore > minScore) {
                hasRelevant = true;
            }
            
            // Asocia a cada posicion del doc (que tenga una palabra buscada) su respectiva palabra
            Dictionary<int, string> words = new Dictionary<int, string>();
            // Guarda todas las posiciones ocupadas. SortedSet para buscar rangos mas facilmente
            SortedSet<int> positions = new SortedSet<int>();

            // Revisando entre todas las palabras que aparecen en el documento
            foreach (var partial in docsData[i].Content) {
                
                Occurrences occurrences = data.Words[partial.Word][partial.Document];

                // Si es una palabra poco relevante se ignora
                // Como el documento paso el if anterior esta garantizado que contiene
                // al menos una palabra relevante
                if (occurrences.Relevance < minScore) continue;

                // Si la palabra se obtuvo de una sugerencia
                if (partial.Original != "") {
                    suggestedWords.Add(partial);
                }
                
                // Guardando las ocurrencias de la palabra en el doc
                foreach (var pos in occurrences.StartPos) {
                    if (!positions.Contains(pos)) {
                        words.Add(pos, partial.Word);
                        positions.Add(pos);
                    }
                }
            }
            string snippet = GetSnippet(docPath, words, positions);

            // Creando el SearchItem correspondiente a este doc
            items.Add(new SearchItem(title, snippet.ToString(), docsData[i].TotalScore));
        }
        
        string suggestions = GenerateSuggestionString(originalWords, suggestedWords.ToArray());

        return new SearchResult(items.ToArray(), suggestions);
    }

    // Devuelve una lista ordenada
    public static List<CumulativeScore> DocsFromPhrase(IndexData data, PartialItem[] partials, int amount) {

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

        List<CumulativeScore> results = new List<CumulativeScore>();

        foreach (var docContent in sortedRelevances) {
            results.Add(docContent.Value);
        }

        return results;
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

    // Dado un conjunto de posiciones y sus palabras, obtiene el snippet con mas palabras
    static string GetSnippet(string docPath, Dictionary<int, string> words, SortedSet<int> positions) {

        StreamReader reader = new StreamReader(docPath);
        string content = reader.ReadToEnd();
        reader.Close();
        
        int maxPoints = 1; // El maximo de palabras en una vecindad
        int pivot = positions.ElementAt(0); // El pivote de la vecindad

        // Recorriendo todas las posiciones con palabras
        foreach (int pos in positions) {

            // Calculando la cantidad de puntos en la vecindad
            int points = GetZone(pos, positions, words, content.Length);

            if (maxPoints < points) {
                maxPoints = points;
                pivot = pos;
            }
        }

        string snippet = "";

        int left, right; // Los limites de la vecindad

        // Calculando los limites
        if (pivot - snippetWidth / 4 < 0) { // Si el punto esta muy al comienzo del doc
            left = 0;
            right = snippetWidth;
        }
        else if (pivot + snippetWidth - snippetWidth / 4 >= content.Length) { // Si esta muy al final
            right = content.Length - 1;
            left = content.Length - snippetWidth;
        }
        else {
            left = pivot - snippetWidth / 4;
            right = pivot + snippetWidth - snippetWidth / 4;
        }
        
        snippet = content[left .. right];

        return snippet;
    }

    // Calcula los puntos en la vecindad de la palabra
    // La vecindad ira desde (point - snippetWidth/4 ; point + snippetWidth - snippetWidth/4)
    static int GetZone(int point, SortedSet<int> positions, Dictionary<int, string> words, int docSize) {

        int left, right; // Los limites de la vecindad

        // Calculando los limites
        if (point - snippetWidth / 4 < 0) { // Si el punto esta muy al comienzo del doc
            left = 0;
            right = snippetWidth;
        }
        else if (point + snippetWidth - snippetWidth / 4 >= docSize) { // Si esta muy al final
            right = docSize - 1;
            left = docSize - snippetWidth;
        }
        else {
            left = point - snippetWidth / 4;
            right = point + snippetWidth - snippetWidth / 4;
        }

        // Obteniendo los puntos existentes en el rango
        var rangeSet = positions.GetViewBetween(left, right);
        int result = rangeSet.Count;

        return result;
    }

    // Dada la cadena original y los parciales de las sugerencias, genera el string de sugerencias
    static string GenerateSuggestionString(string[] originalWords, PartialItem[] partials) {

        bool changed = false; // Para saber si el string original sera modificado
        foreach (var partial in partials) {
            
            int pos = ArrayOperation.Find(originalWords, partial.Original);
            if (pos != -1) {
                changed = true;
                originalWords[pos] = partial.Word;
            }
        }
        if (changed) {
            return ArrayOperation.String(originalWords);
        }
        else return "@null";
    }
}