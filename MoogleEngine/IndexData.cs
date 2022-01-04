using System.Text;
using System.Diagnostics;

namespace MoogleEngine;

public class IndexData {

    public IndexData() {

        Stopwatch crono = new Stopwatch();
        System.Console.WriteLine("Inicio...");
        crono.Start();

        this.Words = new Dictionary<string, Dictionary<int, Occurrences>>();
        this.Docs = new Dictionary<int, string>();
        this.Variations = new Dictionary<string, List<string>>();
        this.Roots = new Dictionary<string, List<string>>();

        string[] files = Directory.GetFiles("../Content", "*.txt", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++) { // Iterando por cada documento

            StreamReader reader = new StreamReader(files[i]);
            List<Tuple<string, int>> wordList = GetWords(reader.ReadToEnd()); // Recibe el contenido crudo
            reader.Close();
            this.Docs.Add(i, files[i]); // Asignar un ID al documento
            
            foreach (var word in wordList) {

                // La palabra sin acentos
                string parsedWord = StringParser.ParseAccents(word.Item1);

                // Inicializar el diccionario al que apunta cada palabra y obtener su raiz
                if (!(this.Words.ContainsKey(parsedWord))) {

                    this.Words.Add(parsedWord, new Dictionary<int, Occurrences>());

                    // Generacion de la raiz
                    string root = Stemming.GetRoot(parsedWord);
                    // Si no se ha usado esta raiz, inicializar su lista
                    if (!(this.Roots.ContainsKey(root))) {
                        this.Roots.Add(root, new List<string>());
                    }
                    this.Roots[root].Add(parsedWord); // Le agrega la palabra original a esta raiz

                    GetSubwords(parsedWord);
                }
                if (!(this.Words[parsedWord].ContainsKey(i))) { // Inicializar las ocurrencias en un doc especifico
                    this.Words[parsedWord][i] = new Occurrences();
                }
                this.Words[parsedWord][i].Push(word.Item2); // Agrega una nueva ocurrencia de la palabra en el doc
            }
        }

        crono.Stop();
        System.Console.WriteLine("✅ Indexado en {0}ms", crono.ElapsedMilliseconds);
    }

    // Guarda todas las palabras, cada una apuntando a los documentos donde aparece
    public Dictionary<string, Dictionary<int, Occurrences>> Words { get; private set; }

    // Asignar un ID unico a cada documento
    public Dictionary<int, string> Docs { get; private set; }

    // Cada palabra recortada apunta a su palabra original
    public Dictionary<string, List<string>> Variations { get; private set; }

    // Cada raiz apunta a sus palabras de origen
    public Dictionary<string, List<string>> Roots { get; private set; }

    List<Tuple<string, int>> GetWords(string content) { // Devuelve la lista de las palabras existentes y su ubicacion
        List<Tuple<string, int>> result = new List<Tuple<string, int>> (); // <palabra, posicionDeInicio, posicionFinal + 1>

        StringBuilder temp = new StringBuilder();
        int start = 0;
        for (int i = 0; i < content.Length; i++) {

            char c = StringParser.IsAlphaNum(content[i]);
            if (c != '\0') {
                temp.Append(c); // Si es un caracter alfanumerico sera parte de una palabra
            }
            else { // Si no, agrega la palabra formada a la lista y continua a la siguiente
                if (temp.Length > 0) { // Para controlar el caso de dos caracteres no alfanumericos juntos
                    result.Add(new Tuple<string, int> (temp.ToString(), start));
                }
                start = i + 1;
                temp.Clear();
            }
        }

        if (temp.Length != 0) {
            result.Add(new Tuple<string, int> (temp.ToString(), start));
        }

        return result;
    }

    // Metodo para insertar las subpalabras derivadas de la palabra dada
    void GetSubwords(string word) {
        
        List<string> derivates = SubWords.GetDerivates(word);
        foreach (string subword in derivates) {
            
            if (!(this.Variations.ContainsKey(subword))) {
                this.Variations[subword] = new List<string>();
            }
            this.Variations[subword].Add(word);
        }
    }
}