using System.Text;
using System.Diagnostics;

namespace MoogleEngine;

public class IndexData {

    Dictionary<string, Dictionary<int, Occurrences>> words = new Dictionary<string, Dictionary<int, Occurrences>>(); // Toda la info sobre cada palabra que aparece
    Dictionary<int, string> docs = new Dictionary<int, string>(); // Asignar un ID unico a cada documento

    // Cada palabra recortada apunta a su palabra original
    Dictionary<string, List<string>> variations = new Dictionary<string, List<string>>();

    // Cada lexema apuntara a su palabra original
    Dictionary<string, List<string>> lexems = new Dictionary<string, List<string>>();

    public IndexData() {

        Stopwatch crono = new Stopwatch();
        System.Console.WriteLine("Inicio...");
        crono.Start();

        string[] files = Directory.GetFiles("../Content", "*.txt", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++) { // Iterando por cada documento

            StreamReader reader = new StreamReader(files[i]);
            List<Tuple<string, int>> wordList = GetWords(reader.ReadToEnd()); // Recibe el contenido crudo
            reader.Close();
            docs.Add(i, files[i]); // Asignar un ID al documento
            
            foreach (var word in wordList) {
                
                if (!words.ContainsKey(word.Item1)) { // Inicializar el array de docs de cada palabra
                    words.Add(word.Item1, new Dictionary<int, Occurrences>());
                    GetSubwords(word.Item1);
                    GetLexems(word.Item1);
                }
                if (!(words[word.Item1].ContainsKey(i))) { // Inicializar las ocurrencias en un doc especifico
                    words[word.Item1][i] = new Occurrences();
                }
                words[word.Item1][i].Push(word.Item2); // Agrega una nueva ocurrencia de la palabra en el doc
            }
        }

        crono.Stop();
        System.Console.WriteLine("✅ Indexado en {0}ms", crono.ElapsedMilliseconds);
    }

    public Dictionary<string, Dictionary<int, Occurrences>> Words { get { return words; } }

    public Dictionary<int, string> Docs { get { return docs; } }

    public Dictionary<string, List<string>> Variations { get { return variations; } }

    public Dictionary<string, List<string>> Lexems { get { return lexems; } }

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
            
            if (!(variations.ContainsKey(subword))) {
                variations[subword] = new List<string>();
            }
            variations[subword].Add(word);
        }
    }

    void GetLexems(string word) {

        List<string> prefixes = SubWords.GetPrefixes(word);
        foreach (string prefix in prefixes) {
            
            if (!(lexems.ContainsKey(prefix))) {
                lexems[prefix] = new List<string>();
            }
            lexems[prefix].Add(word);
        }
    }
}