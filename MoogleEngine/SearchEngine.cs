namespace MoogleEngine;

public static class SearchEngine {

    // El score minimo necesario para que un documento se muestre
    static float minScore = 0.0001f; 

    // Las longitudes de los rangos para el operador ~
    static int[] closerDiameter = new int[] { 50, 100, 150, 300, 600 };

    // La cantidad minima de documentos que debe generar una palabra para no procesar sus sugerencias
    static int minAcceptable = 3;

    // La cantidad de sugerencias que se buscaran por una palabra mal escrita
    static int suggestionsByWord = 3;

    // Si una sugerencia genera al menos estos resultados, no se agregaran mas sugerencias a la busqueda
    static int resultsWithSuggestion = 10;

    // La mayor diferencia de longitudes permitida entre una palabra y su sugerencia
    static int maxCharDiff = 2;

    // La mayor distancia de Lev. permitida entre una palabra y su sugerencia
    static int maxDistance = 4;

    #region Metodos publicos

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

        if (data.Words.ContainsKey(word)) {
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
            if (suggest && (PartialItem.CountDocuments(items) < minAcceptable || !data.Words.ContainsKey(word))) {
                
                List<(string, float)> suggestions = GetSuggestions(data, word);

                if (suggestions.Count > 0) {
                    foreach (var suggestion in suggestions) {
                        items.AddRange(GetOneWord(data, suggestion.Item1, false, suggestion.Item2, word, true));
                        // Si se considera que se hayaron suficientes sugerencias, no agregar mas
                        if (items.Count >= resultsWithSuggestion) break;
                    }
                }
            }

        items = items.OrderByDescending(x => data.Words[x.Word][x.Document].Relevance * x.Multiplier).ToList();
        return items;
    }

    // Devuelve una lista ordenada con documentos a mostrar
    public static List<CumulativeScore> DocsFromPhrase(IndexData data, List<PartialItem> partials, ParsedInput parsedInput, int amount, List<PartialItem> suggestedWords) {

        // Tomando las palabras que salieron por sugerencias
        foreach (var partial in partials) {
            if (partial.Original != "") {
                suggestedWords.Add(partial);
            }
        }

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

    // Genera la lista de resultados finales
    public static SearchResult GetResults(IndexData data, List<CumulativeScore> docsData, ParsedInput input, List<PartialItem> suggestedWords) {

        // Aqui va la lista de resultados
        List<SearchItem> items = new List<SearchItem>();
        // Aqui van las palabras encontradas en los documentos. Sirve para generar el string de sugerencias

        bool hasRelevant = false; // Indicador de si en los resultados hay docs de alta relevancia
        // Procesando cada documento a mostrar
        for (int i = 0; i < docsData.Count; i++) {
            
            // Obteniendo datos del documento
            int docID = docsData[i].Content[0].Document;

            // Obteniendo el nombre del doc
            string docPath = data.Docs[docID];
            string[] temp = docPath.Split(new char[] {'/', '\\'}, StringSplitOptions.RemoveEmptyEntries);
            string title = string.Join('.', temp[temp.Length - 1].Split('.')[..^1]);

            float docScore = docsData[i].TotalScore; // Score del doc

            if (docScore <= minScore && hasRelevant) continue; // Si el documento es poco relevante, saltarselo
            else if (docScore > minScore) {
                hasRelevant = true;
            }
            // Almacena todas las posiciones que tengan palabras buscadas para generar el snippet
            WordPositions positionsStore = new WordPositions();
            List<PartialItem> wordsToHighlight = new List<PartialItem>();

            // Revisando entre todas las palabras que aparecen en el documento
            foreach (var partial in docsData[i].Content) {
                
                Occurrences occurrences = data.Words[partial.Word][partial.Document];

                // Si es una palabra poco relevante se ignora
                // Como el documento paso el if anterior esta garantizado que contiene
                // al menos una palabra relevante
                if (occurrences.Relevance < minScore && hasRelevant) continue;
                wordsToHighlight.Add(partial);

                // Guardando las ocurrencias de la palabra en el doc
                positionsStore.Insert(partial.Word, occurrences.StartPos.ToArray());
            }
            // Obteniendo el snippet y resaltando las palabras
            string snippet = SnippetOperations.GetSnippet(docPath, positionsStore, hasRelevant);
            snippet = SnippetOperations.HighlightWords(snippet, wordsToHighlight, hasRelevant);

            // Creando el SearchItem correspondiente a este doc
            items.Add(new SearchItem(title, snippet.ToString(), docsData[i].TotalScore, docPath));
        }
        
        string suggestions = GenerateSuggestionString(input, suggestedWords.ToArray());

        return new SearchResult(items.ToArray(), suggestions);
    }

    #endregion

    #region Metodos privados

    // Recibe una lista de parciales y los filtra segun los operadores dados por el usuario
    static List<PartialItem> FilterByOperators(IndexData data, List<PartialItem> partials, ParsedInput parsedInput) {

        string[] mandatoryWords = parsedInput.MandatoryWords; // Las palabras con operador ^
        string[] forbiddenWords = parsedInput.ForbiddenWords; // Las palabras con operador !
        (string, int)[] multipliedWords = parsedInput.MultipliedWords; // Las palabras con operador *
        List<string>[] closerWords = parsedInput.CloserWords; // Las palabras entre operadores ~

        List<PartialItem> result = new List<PartialItem>();

        // Cuando se calcule el multiplicador de un documento, o se descarte se guarda aqui
        // Esto evita recalcular el mismo documento varias veces
        Dictionary<int, float> memo = new Dictionary<int, float> ();

        // Recorriendo y validando cada documento
        foreach (var partial in partials) {

            int Id = partial.Document;

            // Aplicando los multiplicadores del operador *
            foreach (var pair in multipliedWords) {
                // Si estamos analizando la misma palabra
                if (pair.Item1 == partial.Word) {
                    partial.Multiply(pair.Item2 + 1);
                }
            }

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

                // Aplicando los operadores ~
                int maxMult = 1; // Multiplicador que se aplicara al documento
                foreach (var wordSet in closerWords) { // Analizando cada grupo de palabras
                    // Aquí se guardará el mayor multiplicador para este grupo
                    int groupMult = 1;
                    // Para saber si el grupo actual contiene palabras relevantes
                    bool hasRelevant = false;
                    // Para almacenar las posiciones de este grupo de palabras
                    WordPositions wordPositions = new WordPositions(); 
                    foreach (string word in wordSet) {
                        // Si la palabra esta en el documento, insertarla en el wordPositions
                        if (data.Words.ContainsKey(word) && data.Words[word].ContainsKey(Id)) {
                            wordPositions.Insert(word, data.Words[word][Id].StartPos.ToArray());
                            if (data.Words[word][Id].Relevance > minScore) hasRelevant = true;
                        }
                    }
                    // Si el doc solo contiene una palabra del grupo, ahorrarse la busqueda
                    if (wordPositions.Differents <= 1) continue;

                    bool achievedBest = false; // Para saber si ya se encontro el mejor rango posible
                    // Analizando cada diametro
                    for (int i = 0; i < closerDiameter.Length && !achievedBest; i++) {
                        // Analizando cada posicion con ese diametro
                        foreach (int pos in wordPositions.Positions) {
                            // Si la palabra actual es poco relevante, saltarsela
                            if (data.Words[wordPositions.Words[pos]][partial.Document].Relevance < minScore && hasRelevant) continue;
                            // Calculando los multiplicadores y guardando el maximo
                            int amount = SnippetOperations.GetZone(pos, wordPositions, closerDiameter[i]);
                            groupMult = Math.Max(groupMult, (amount - 1) * (closerDiameter.Length - i + 1));
                            // Si se hallo un intervalo con todas las palabras, no existe uno mejor
                            if (amount == wordPositions.Differents) {
                                achievedBest = true;
                                break;
                            }
                        }
                    }
                    // Agregando el multiplicador de este grupo al total del operador ~
                    maxMult *= groupMult;
                }
                // Modificando el multiplicador del PartialItem y memoizando
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
    static List<(string, float)> GetSuggestions(IndexData data, string word) {

        // Aqui se acumulara el score de cada sugerencia para determinar la mejor
        // Dicho score tendra en cuenta el parecido con la palabra original usando Edit Distance
        List<(string, float)> suggestionsPriority = new List<(string, float)>();
        // Va almacenando la cantidad de palabras de distancia 1
        int diff1 = 0;

        // Va iterando por las palabras de igual longitud
        // Luego por las de longitud de diferencia 1, luego 2 y asi
        for (int i = 0; i < 2 * maxCharDiff + 1 && diff1 < suggestionsByWord; i++) {

            int size = Math.Max(1, word.Length + ((i + 1) / 2 * (int)Math.Pow(-1, i)));
            // Primera palabra del bloque de longitud buscada
            int lowLengthPos = GetLengthInDict(data, size, 0, data.Words.Count);

            foreach (var dictWord in data.Words.Skip(lowLengthPos)) {
                // Si se supera la longitud actual, no seguir buscando
                if (dictWord.Key.Length > size) break;
                if (word != dictWord.Key) {
                    // Hallando la distancia entre la palabra escrita y la sugerencia
                    int distance = ArraysAndStrings.Distance(dictWord.Key, word);
                    if (distance > maxDistance) continue;
                    suggestionsPriority.Add((dictWord.Key, 1.0f / (float)distance));
                    // Llevando la cuenta de las sugerencias de distancia 1
                    if (distance == 1) diff1++;
                    if (diff1 == suggestionsByWord) break;
                }
            }
        }

        // Ordenando las sugerencias por su parecido a la palabra
        var suggestions = suggestionsPriority.OrderByDescending(x => x.Item2).ToList();
        // Aqui iran las sugerencias resultantes
        List<(string, float)> result = new List<(string, float)>();

        for (int i = 0; i < suggestionsByWord && i < suggestions.Count; i++) {
            result.Add(suggestions[i]);
        }

        return result;
    }
    
    // Busqueda binaria para hallar la 1ra palabra de cierta longitud en la data de palabras
    static int GetLengthInDict(IndexData data, int length, int i, int j) {

        if (i > j) return int.MaxValue;

        int mid = (i + j) / 2;
        int currentLength = data.Words.ElementAt(mid).Key.Length;

        if (currentLength >= length) return Math.Min(mid, GetLengthInDict(data, length, i, mid - 1));
        else return GetLengthInDict(data, length, mid + 1, j);
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
                    float priority = 1.0f - (float)ArraysAndStrings.Distance(word, possibleOrigin) / (float)Math.Max(word.Length,possibleOrigin.Length);
                    // Buscando la nueva palabra en cada documento
                    List<PartialItem> newResults = new List<PartialItem>(GetOneWord(data, possibleOrigin, false, priority * 0.1f));
                    results.AddRange(newResults);
                }
            }
        }
        return results;
    }

    // Genera los sinonimos de una palabra y devuelve los parciales con los resultados de estos
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

    // Dada la cadena original y los parciales de las sugerencias, genera el string de sugerencias
    static string GenerateSuggestionString(ParsedInput input, PartialItem[] partials) {

        string[] originalWords = input.Words.ToArray();
        // Para cada palabra original, almacena su mejor sugerencia
        Dictionary<string, (string, int)> bestSuggestions = new Dictionary<string, (string, int)>();

        foreach (var partial in partials) {

            // Busca cual es la palabra original en el query de la que salio esta sugetencia
            int pos = ArraysAndStrings.Find(originalWords, partial.Original);
            // Si existe
            if (pos != -1) {
                // Determina la distancia entre la palabra original y la sugerencia
                int distance = ArraysAndStrings.Distance(originalWords[pos], partial.Word);
                // Si aun no se han analizado sugerencias para la palabra, se agrega
                if (!(bestSuggestions.ContainsKey(originalWords[pos]))) {
                    bestSuggestions[originalWords[pos]] = (partial.Word, distance);
                }
                // Si la palabra ya tiene una sugerencia, comprobar si la que tenemos es mejor
                else if(bestSuggestions[originalWords[pos]].Item2 > distance) {
                    bestSuggestions[originalWords[pos]] = (partial.Word, distance);
                }
            }
        }
        // Si hubo cambios...
        if (bestSuggestions.Count > 0) {
            // Recorrer cada palabra que haya sido modificada
            foreach (var replace in bestSuggestions) {
                int pos = ArraysAndStrings.Find(originalWords, replace.Key);
                originalWords[pos] = replace.Value.Item1;
            }
            return ArraysAndStrings.WordsToString(originalWords, input);
        }
        else return "@null";
    }
    #endregion
}