using System.Text;
using System.Diagnostics;

namespace MoogleEngine;

// Clase para almacenar toda la data de los documentos en memoria y en caché
public class IndexData {

    // Si al calcular el IDF la razon Frecuencia / TotalDocs da mayor que el valor, se tomara como 1
    // Esto anula el TF-IDF de las palabras que aparecen en la gran mayoria de los documentos
    static float percentToNullify = 0.95f;

    // El parametro indica si se desea recorrer los documentos y precalcular y guardar la informacion
    public IndexData(bool dataCalculate) {
        // Cronometro para saber el tiempo que lleva indexar todo
        Stopwatch crono = new Stopwatch();
        System.Console.WriteLine("Inicio...");
        crono.Start();

        this.Words = new Dictionary<string, Dictionary<int, Occurrences>>();
        this.Docs = new Dictionary<int, string>();
        this.Roots = new Dictionary<string, List<string>>();
        this.Synonyms = new Dictionary<string, List<string>>();

        // Si no se desea precalcular, se cargan los datos en la cache y se asignan a sus respectivas estructuras
        if (!dataCalculate) {
            try {
                this.Docs = CacheManager.LoadDocs();
                System.Console.WriteLine("✅ Documentos cargados desde la caché");
                this.Words = CacheManager.LoadWords();
                System.Console.WriteLine("✅ Palabras cargadas desde la caché");
                this.Roots = CacheManager.LoadRoots();
                System.Console.WriteLine("✅ Raíces cargadas desde la caché");
            }
            catch (System.Exception) {
                // Si hubo un error leyendo los datos, se indexara y almacenara todo de nuevo
                System.Console.WriteLine("🛑 Error en la caché. Calculando...");
                dataCalculate = true;
            }
        }
        // Si se desea precalcular, o la carga de informacion anterior fallo
        if (dataCalculate) {

            string[] files = Directory.GetFiles(Path.Combine(".", "Content"), "*.txt", SearchOption.AllDirectories);
            for (int i = 0; i < files.Length; i++) { // Iterando por cada documento

                StreamReader reader = new StreamReader(files[i]);
                List<(string, int)> wordList = GetWords(reader); // Separa todas las palabras del doc
                reader.Close();
                this.Docs.Add(i, files[i]); // Asignar un ID al documento
                
                foreach (var word in wordList) { // Procesa cada palabra del doc

                    // Inicializar el diccionario al que apunta cada palabra y obtener su raiz
                    if (!(this.Words.ContainsKey(word.Item1))) {

                        this.Words.Add(word.Item1, new Dictionary<int, Occurrences>());

                        // Generacion de la raiz
                        string root = Stemming.GetRoot(word.Item1);
                        // Si la raiz es diferente a la palabra original, agregarla
                        if (root != ArraysAndStrings.ParseAccents(word.Item1)) {
                            // Si no se ha usado esta raiz, inicializar su lista
                            if (!(this.Roots.ContainsKey(root))) {
                                this.Roots.Add(root, new List<string>());
                            }
                            // Le agrega la palabra original a esta raiz
                            this.Roots[root].Add(word.Item1); 
                        }
                    }
                    // Inicializar las ocurrencias en un doc especifico
                    if (!(this.Words[word.Item1].ContainsKey(i))) {
                        this.Words[word.Item1][i] = new Occurrences();
                    }
                    // Agrega una nueva ocurrencia de la palabra en el doc
                    this.Words[word.Item1][i].StartPos.Add(word.Item2); 
                }
            }

            System.Console.WriteLine("✅ Datos indexados en la RAM");

            // Ordenando el diccionario por longitud de palabras
            this.Words = this.Words.OrderBy(x => x.Key.Length).ToDictionary(x => x.Key, x => x.Value);

            // Calculando los TF-IDF
            foreach (var pair in this.Words) {
                
                foreach (var doc in pair.Value) {

                    float tf = (float)Math.Log2((float)doc.Value.StartPos.Count + 1);
                    float idf = (float)Math.Log2((float)(this.Docs.Count) / (float)pair.Value.Count);
                    // Si la palabra aparece en muchos documentos se le asigna un TF-IDF infinitesimal
                    doc.Value.Relevance = ((float)pair.Value.Count / (float)this.Docs.Count < percentToNullify ? tf * idf : 0.0f);
                }
            }

            System.Console.WriteLine("✅ TF-IDF's calculados");

            // Almacenando todos los datos calculados en la memoria fisica
            CacheManager.SaveWords(this.Words);
            CacheManager.SaveDocs(this.Docs);
            CacheManager.SaveRoots(this.Roots);

            System.Console.WriteLine("✅ Datos almacenados en la caché");
        }

        LoadSynonyms();
        System.Console.WriteLine("✅ Sinónimos cargados");

        crono.Stop();
        System.Console.WriteLine("✅ Indexado en {0}ms", crono.ElapsedMilliseconds);
    }

    // Guarda todas las palabras, cada una apuntando a los documentos donde aparece
    public Dictionary<string, Dictionary<int, Occurrences>> Words { get; private set; }

    // Asignar un ID unico a cada documento
    public Dictionary<int, string> Docs { get; private set; }

    // Cada raiz apunta a sus palabras de origen
    public Dictionary<string, List<string>> Roots { get; private set; }

    // Para cada palabra en el Thesaurus.csv almacena sus sinonimos
    public Dictionary<string, List<string>> Synonyms { get; private set; }

    // Devuelve la lista de las palabras existentes y su ubicacion
    List<(string, int)> GetWords(StreamReader reader) {
        List<(string, int)> result = new List<(string, int)> (); // <palabra, posicionDeInicio>

        // Aqui se ira almacenando cada palabra
        StringBuilder temp = new StringBuilder();
        int start = 0; // La posicion inicial de la palabra a guardar (en bytes)
        while (!reader.EndOfStream) {
            
            char original = (char)reader.Read();
            // Parsea el caracter
            char c = ArraysAndStrings.IsAlphaNum(original);
            if (c != '\0') {
                temp.Append(c); // Si es un caracter alfanumerico sera parte de una palabra
            }
            else { // Si no, agrega la palabra formada a la lista y continua a la siguiente
                if (temp.Length > 0) { // Para controlar el caso de dos caracteres no alfanumericos juntos
                    result.Add((temp.ToString(), start));
                }
                // Avanza la posicion inicial en la cantidad de bytes de la palabra que se agrego
                // Mas cualquier caracter no alfanumerico que aparezca
                start += Encoding.Default.GetByteCount(temp.Append(original).ToString());

                temp.Clear();
            }
        }

        // Agregando la ultima palabra
        if (temp.Length != 0) {
            result.Add((temp.ToString(), start));
        }

        return result;
    }

    // Carga todos los sinonimos en Thesaurus.csv y los guarda en Synonyms
    void LoadSynonyms() {

        StreamReader reader = new StreamReader("./MoogleEngine/Thesaurus.csv");

        while (true) {
            // Leyendo cada linea
            var rawLine = reader.ReadLine();
            // Si llegamos al final del archivo, terminar el ciclo
            if (rawLine == null) break;
            // Toda una fila de palabras relacionadas
            string[] words = rawLine.Split(new char[] {',', ' '});

            if (words[0] == "key" && words[1] == "synonyms") continue;

            // Iterando por cada palabra, y guardando las otras en su posicion del diccionario
            for (int i = 0; i < words.Length; i++) {
                for (int j = 0; j < words.Length; j++) {
                    
                    if (i == j) continue; // No se va a relacionar cada palabra con sigo misma

                    if (!Synonyms.ContainsKey(words[i])) {
                        Synonyms[words[i]] = new List<string>();
                    }
                    Synonyms[words[i]].Add(words[j]);
                }
            }
        }
    }
}