using System.Text;

namespace MoogleEngine;

public static class SearchEngine {

    // El score minimo necesario para que un documento se muestre
    static float minScore = 0.0001f; 

    // La cantidad de caracteres que tendra el snippet
    static int snippetWidth = 150; 

    // Las longitudes de los rangos para el operador ~
    static int[] closerDiameter = new int[] { 20, 50, 100, 150 };

    // La cantidad minima de documentos que debe generar una palabra para no procesar sus sugerencias
    static int minAcceptable = 3;

    // La cantidad de sugerencias que se buscaran por una palabra mal escrita
    static int suggestionsByWord = 3;

    // Si una sugerencia genera al menos estos resultados, no se agregaran mas sugerencias a la busqueda
    static int resultsWithSuggestion = 5;

    // Busca los docs mas relevantes que contengan la palabra 'word'
    // suggest indica si se desea generar las sugerencias de la palabra
    // multiplier es el multiplicador a aplicarle al parcial
    // original: en caso de estar procesando una sugerencia, original es la palabra original del query
    // relatedWords indica si se desea generar las misma-raiz y los sinonimos
    public static List<PartialItem> GetOneWord(IndexData data, string word, bool suggest = false, float multiplier = 1.0f, string original = "", bool relatedWords = false) { 

        // Para guardar los parciales finales
        List<PartialItem> items = new List<PartialItem> ();
        // Para guardar los parciales de las mismas raices y sugerencias
        List<PartialItem> lowerResults = new List<PartialItem>();

        // Generando las sugerencias si la palabra no existe
        if (!(data.Words.ContainsKey(word))) {
            
            List<Tuple<string, float>> suggestions = GetSuggestions(data, word);
            // Si existen sugerencias
            if (suggest && suggestions.Count > 0) {
                // Hallar los parciales de cada una
                foreach (var suggestion in suggestions) {
                    lowerResults.AddRange(GetOneWord(data, suggestion.Item1, false, suggestion.Item2, word, true));
                    // Si se considera que se hayaron suficientes sugerencias, no agregar mas
                    if (lowerResults.Count >= resultsWithSuggestion) break;
                }
            }
        }
        else {
            Dictionary<int, Occurrences> info = data.Words[word];
            foreach (var doc in info) {
                items.Add(new PartialItem(word, doc.Key, multiplier, original));
            }
        }

        // Generando las palabras de raiz similar
        if (relatedWords) {
            lowerResults.AddRange(GetSameRoot(data, word));
            lowerResults.AddRange(GetSynonyms(data, word));
        }

        items.AddRange(lowerResults);

        // Si hay muy pocos resultados, generar sugerencias
            if (suggest && PartialItem.CountDocuments(items) < minAcceptable) {
                
                List<Tuple<string, float>> suggestions = GetSuggestions(data, word);
                if (suggestions.Count > 0) {
                    foreach (var suggestion in suggestions) {
                        items.AddRange(GetOneWord(data, suggestion.Item1, false, suggestion.Item2, word, true));
                        // Si se considera que se hayaron suficientes sugerencias, no agregar mas
                        if (items.Count >= resultsWithSuggestion) break;
                    }
                }
            }

        return items;
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
            int docID = docsData[i].Content[0].Document;

            // Obteniendo el nombre del doc
            string docPath = data.Docs[docID];
            string[] temp = docPath.Split('/');
            string title = temp[temp.Length - 1].Split('.')[0];

            float docScore = docsData[i].TotalScore; // Score del doc

            if (docScore <= minScore && hasRelevant) continue; // Si el documento es poco relevante, saltarselo
            else if (docScore > minScore) {
                hasRelevant = true;
            }
            // Almacena todas las posiciones que tengan palabras buscadas para generar el snippet
            WordPositions positionsStore = new WordPositions();

            // Revisando entre todas las palabras que aparecen en el documento
            foreach (var partial in docsData[i].Content) {
                
                Occurrences occurrences = data.Words[partial.Word][partial.Document];

                // Si es una palabra poco relevante se ignora
                // Como el documento paso el if anterior esta garantizado que contiene
                // al menos una palabra relevante
                if (occurrences.Relevance < minScore && hasRelevant) continue;

                // Si la palabra se obtuvo de una sugerencia
                if (partial.Original != "") {
                    suggestedWords.Add(partial);
                }
                
                // Guardando las ocurrencias de la palabra en el doc
                positionsStore.Insert(partial.Word, occurrences.StartPos.ToArray());
            }
            string snippet = GetSnippet(docPath, positionsStore, hasRelevant);

            // Creando el SearchItem correspondiente a este doc
            items.Add(new SearchItem(title, snippet.ToString(), docsData[i].TotalScore));
        }
        
        string suggestions = GenerateSuggestionString(originalWords, suggestedWords.ToArray());

        return new SearchResult(items.ToArray(), suggestions);
    }

    // Devuelve una lista ordenada con documentos a mostrar
    public static List<CumulativeScore> DocsFromPhrase(IndexData data, List<PartialItem> partials, ParsedInput parsedInput, int amount) {

        List<PartialItem> filtered = FilterByOperators(data, partials, parsedInput);

        // Cada documento apuntara al score acumulativo de las palabras que contiene y tambien a las palabras en si
        Dictionary<int, CumulativeScore> relevances = new Dictionary<int, CumulativeScore> ();

        // Iterando por los resultados parciales analizados
        foreach (var partial in filtered) {
            
            Dictionary<int, Occurrences> info = data.Words[partial.Word];
            // Si no se ha analizado el documento, se creara su clave
            if (!(relevances.ContainsKey(partial.Document))) {
                relevances.Add(partial.Document, new CumulativeScore());
            }
            relevances[partial.Document].AddWord(info[partial.Document].Relevance, partial);
        }

        // Ordena los documentos por su relevancia total
        var sortedRelevances = relevances.OrderByDescending(x => x.Value.TotalScore).ToList();

        List<CumulativeScore> results = new List<CumulativeScore>();

        for (int i = 0; i < sortedRelevances.Count && i < amount; i++) {
            results.Add(sortedRelevances[i].Value);
        }

        return results;
    }

    // Recibe una lista de parciales y los filtra segun los operadores dados por el usuario
    static List<PartialItem> FilterByOperators(IndexData data, List<PartialItem> partials, ParsedInput parsedInput) {

        string[] mandatoryWords = parsedInput.MandatoryWords; // Las palabras con operador ^
        string[] forbiddenWords = parsedInput.ForbiddenWords; // Las palabras con operador !
        Tuple<string, int>[] multipliedWords = parsedInput.MultipliedWords; // Las palabras con operador *
        List<string>[] closerWords = parsedInput.CloserWords; // Las palabras entre operadores ~

        List<PartialItem> result = new List<PartialItem>();

        // Cuando se calcule el multiplicador de un documento, o se descarte se guarda aqui
        // Esto evita recalcular el mismo documento varias veces
        Dictionary<int, float> memo = new Dictionary<int, float> ();

        // Recorriendo y validando cada documento
        foreach (var partial in partials) {

            int Id = partial.Document;

            // Si el documento ya se calculo
            if (memo.ContainsKey(Id)) {
                // Si no fue descartado, colocar el parcial en los resultados
                if (memo[Id] != 0.0f) {
                    partial.Multiply(memo[Id]);
                    result.Add(partial);
                }
                continue;
            }

            bool flag = true; // Bandera para detectar si el documento pasa todos los filtros

            // Seleccionando solo los documentos que contengan todas las palabras obligatorias
            foreach (string word in mandatoryWords) {
                
                // Si el documento no contiene la palabra, ya no lo queremos
                if (!(data.Words.ContainsKey(word)) || !(data.Words[word].ContainsKey(Id))) {
                    flag = false;
                    break;
                }
            }

            // Seleccionando solo los documentos que no contengan palabras excluidas
            foreach (string word in forbiddenWords) {

                // Si el documento contiene la palabra, ya no lo queremos
                if (data.Words.ContainsKey(word) && data.Words[word].ContainsKey(Id)) {
                    flag = false;
                    break;
                }
            }
            
            if (flag) { // Si no es false, todas las palabras requeridas estan. Lo usaremos

                // Aplicando los multiplicadores del operador *
                foreach (var pair in multipliedWords) {

                    // Si estamos analizando la misma palabra
                    if (pair.Item1 == partial.Word) {
                        partial.Multiply((float)(Math.Pow(pair.Item2 + 1, 2)));
                    }
                }

                // Aplicando los operadores ~
                int maxMult = 1; // Multiplicador que se aplicara al documento
                foreach (var wordSet in closerWords) { // Analizando cada grupo de palabras
                    // Para almacenar las posiciones de este grupo de palabras
                    WordPositions wordPositions = new WordPositions(); 
                    foreach (string word in wordSet) {

                        if (data.Words.ContainsKey(word) && data.Words[word].ContainsKey(Id)) {
                            wordPositions.Insert(word, data.Words[word][Id].StartPos.ToArray());
                        }
                    }

                    bool achievedBest = false;
                    // Analizando cada diametro
                    for (int i = 0; i < closerDiameter.Length && !achievedBest; i++) {
                        // Analizando cada posicion con ese diametro
                        foreach (int pos in wordPositions.Positions) {
                            // Calculando los multiplicadores y guardando el maximo
                            int amount = GetZone(pos, wordPositions, closerDiameter[i]);
                            maxMult = Math.Max(maxMult, (amount - 1) * (closerDiameter.Length - i + 1));
                            // Si se hallo un intervalo con todas las palabras, no existe uno mejor
                            if (amount == wordSet.Count) {
                                achievedBest = true;
                                break;
                            }
                        }
                    }
                }
                partial.Multiply(maxMult);
                memo[Id] = maxMult;

                result.Add(partial);
            }
            else {
                memo[Id] = 0.0f;
            }
        }

        return result;
    }

    // Genera las mejores sugerencias para una palabra. Devuelve la palabra y el multiplicador
    static List<Tuple<string, float>> GetSuggestions(IndexData data, string word) {

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
                        float priority = 1.0f - SubWords.Distance(word, possibleOrigin) / (float)Math.Max(word.Length, possibleOrigin.Length);
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
        // Ordenando las sugerencias por su parecido a la palabra
        var suggestions = cumulativeWord.OrderByDescending(x => x.Value).ToList();
        // Aqui iran las sugerencias resultantes
        List<Tuple<string, float>> result = new List<Tuple<string, float>>();

        int i = 0; // Para contar cuantas sugerencias se enviaran
        foreach (var suggestion in suggestions) {
            
            if (i == suggestionsByWord) break;

            string finalWord = suggestion.Key;
            float finalMult = 1.0f / SubWords.Distance(finalWord, word);
            result.Add(new Tuple<string, float>(finalWord, finalMult));

            i++;
        }
        // Ordenando y devolviendo las sugerencias finales
        return result.OrderByDescending(x => x.Item2).ToList();
    }

    // Para buscar los resultados de las palabras con la misma raiz
    static List<PartialItem> GetSameRoot(IndexData data, string word) {

        // Buscando los lexemas de la palabra
        string root = Stemming.GetRoot(word);
        List<PartialItem> results = new List<PartialItem>();

        // Evitar usar la propia palabra como raiz. Probablemente se trate de un falso positivo
        if (root == word) return results;

        // Revisando si el lexema existe
        if (data.Roots.ContainsKey(root)) {
            // Iterando por cada posible origen
            foreach (string possibleOrigin in data.Roots[root]) {
                if (word != possibleOrigin) {
                    // Distancia entre la nueva palabra y la original
                    float priority = 1.0f - SubWords.Distance(word, possibleOrigin) / (float)Math.Max(word.Length,possibleOrigin.Length);
                    // Buscando la nueva palabra en cada documento
                    List<PartialItem> newResults = new List<PartialItem>(GetOneWord(data, possibleOrigin, false, priority * 0.1f));
                    results.AddRange(newResults);
                }
            }
        }

        // Ordenando los resultados
        results = results.OrderByDescending(x => data.Words[x.Word][x.Document].Relevance * x.Multiplier).ToList();
        return results;
    }

    // Genera las mejores sugerencias para una palabra. Devuelve la palabra y el multiplicador
    static List<PartialItem> GetSynonyms(IndexData data, string word) {
        
        // Obteniendo la raiz de la palabra
        string root = Stemming.GetRoot(word);

        // Aqui van los sinonimos de la palabra
        List<string> rawSynonyms = new List<string>();

        // Para guardar los resultados
        List<PartialItem> result = new List<PartialItem>();

        if (data.Roots.ContainsKey(root)) {
            // Recorriendo cada palabra con la misma raiz que la enviada
            foreach (string possibleOrigin in data.Roots[root]) {
                // Si la palabra generada contiene sinonimos, los guardamos en la lista
                if (data.Synonyms.ContainsKey(possibleOrigin)) {
                    rawSynonyms.AddRange(data.Synonyms[possibleOrigin]);
                }
            }
        }

        // Recorriendo cada sinonimo crudo resultante
        foreach (string rawSynonym in rawSynonyms) {
            // Obteniendo su raiz
            string synRoot = Stemming.GetRoot(rawSynonym);

            if (data.Roots.ContainsKey(synRoot)) {
                // Buscando cada palabra con la misma raiz que rawSynonym
                foreach (string derivedSynonym in data.Roots[synRoot]) {
                    // Buscando derivedSynonym en los documentos y agregando los resultados
                    result.AddRange(GetOneWord(data, derivedSynonym, false, 0.001f));
                }
            }
        }
        return result;
    }

    // Dado un conjunto de posiciones y sus palabras, obtiene el snippet con mas palabras
    static string GetSnippet(string docPath, WordPositions positionsStore, bool hasRelevant) {

        int maxPoints = 1; // El maximo de palabras en una vecindad
        int pivot = positionsStore.Positions.ElementAt(0); // El pivote de la vecindad

        // Si tiene palabras relevantes, selecciona el mejor snippet
        // Si son solo palabras mongas, escoge el 1ro. Pues si no se demoraria demasiado
        if (hasRelevant) {
            // Recorriendo todas las posiciones con palabras
            foreach (int pos in positionsStore.Positions) {

                // Calculando la cantidad de puntos en la vecindad
                int points = GetZone(pos, positionsStore, snippetWidth);

                if (maxPoints < points) {
                    maxPoints = points;
                    pivot = pos;
                }
            }
        }

        StreamReader reader = new StreamReader(docPath);
        int left, right; // Los limites de la vecindad

        // El tamaño en bytes del documento
        int docSize = (int)reader.BaseStream.Length;

        // Calculando los limites
        if (pivot - snippetWidth / 4 < 0) { // Si el punto esta muy al comienzo del doc
            left = 0;
            right = snippetWidth;
        }
        // Si esta muy al final
        else if (pivot + snippetWidth - snippetWidth / 4 >= docSize) {
            right = docSize;
            left = docSize - snippetWidth;
        }
        else { // Si no esta cerca de los bordes
            left = pivot - snippetWidth / 4;
            right = pivot + snippetWidth - snippetWidth / 4;
        }

        // Colocando el puntero del stream al inicio del snippet
        reader.BaseStream.Position = left;
        
        StringBuilder result = new StringBuilder();

        for (int i = 0; i < snippetWidth; i++) {
            result.Append((char)reader.Read());
        }
        reader.Close();

        return TrimSnippet(result);
    }

    // Busca y elimina caracteres invalidos en los bordes del snippet
    // Pueden existir debido a que se esta trabajando con las posiciones en bytes del documento
    // Si un caracter especial es picado, quedara un caracter invalido
    // Ya de paso se eliminarian signos de puntuacion, dejando solo un alfanumerico en el borde
    static string TrimSnippet(StringBuilder cad) {

        // Trimeando el principio
        while (StringParser.IsAlphaNum(cad[0]) == '\0') {
            cad.Remove(0, 1);
        }
        // Trimeando el final
        while (StringParser.IsAlphaNum(cad[cad.Length - 1]) == '\0') {
            cad.Remove(cad.Length - 1, 1);
        }

        return cad.ToString();
    }

    // Calcula los puntos en la vecindad de la palabra
    // La vecindad ira desde (point - width/4 ; point + width - width/4)
    static int GetZone(int point, WordPositions positionsStore, int width) {
        
        if (positionsStore.Positions.Count == 0) return 1;

        int docSize = positionsStore.Positions.Last();
        int left, right; // Los limites de la vecindad

        // Calculando los limites
        if (point - width / 4 < 0) { // Si el punto esta muy al comienzo del doc
            left = 0;
            right = width;
        }
        else if (point + width - width / 4 >= docSize) { // Si esta muy al final
            right = docSize - 1;
            left = docSize - width;
        }
        else {
            left = point - width / 4;
            right = point + width - width / 4;
        }

        // Obteniendo los puntos existentes en el rango
        SortedSet<int> rangeSet = positionsStore.Positions.GetViewBetween(left, right);
        
        // Hallando cuantas palabras diferentes existen en el rango
        HashSet<string> diffWords = new HashSet<string>();
        foreach (int pos in rangeSet) {
            if (!(diffWords.Contains(positionsStore.Words[pos]))) {
                diffWords.Add(positionsStore.Words[pos]);
            }
        }

        return diffWords.Count;
    }

    // Dada la cadena original y los parciales de las sugerencias, genera el string de sugerencias
    static string GenerateSuggestionString(string[] originalWords, PartialItem[] partials) {

        // Para cada palabra original, almacena su mejor sugerencia
        Dictionary<string, Tuple<string, float>> bestSuggestions = new Dictionary<string, Tuple<string, float>>();

        foreach (var partial in partials) {

            // Busca cual es la palabra original en el query de la que salio esta sugetencia
            int pos = ArrayOperations.Find(originalWords, partial.Original);
            // Si existe
            if (pos != -1) {
                // Determina la distancia entre la palabra original y la sugerencia
                float distance = SubWords.Distance(originalWords[pos], partial.Word);
                // Si aun no se han analizado sugerencias para la palabra, se agrega
                if (!(bestSuggestions.ContainsKey(originalWords[pos]))) {
                    bestSuggestions[originalWords[pos]] = new Tuple<string, float>(partial.Word, distance);
                }
                // Si la palabra ya tiene una sugerencia, comprobar si la que tenemos es mejor
                else if(bestSuggestions[originalWords[pos]].Item2 > distance) {
                    bestSuggestions[originalWords[pos]] = new Tuple<string, float>(partial.Word, distance);
                }
            }
        }
        // Si hubo cambios...
        if (bestSuggestions.Count > 0) {
            // Recorrer cada palabra que haya sido modificada
            foreach (var replace in bestSuggestions) {
                int pos = ArrayOperations.Find(originalWords, replace.Key);
                originalWords[pos] = replace.Value.Item1;
            }
            return ArrayOperations.WordsToString(originalWords);
        }
        else return "@null";
    }
}