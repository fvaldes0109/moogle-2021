using System.Text;
using System.Diagnostics;

namespace MoogleEngine;

public class IndexData {

    // Representa el limite de palabras a las que puede apuntar una subpalabra
    // Con 300 esta garantizado que las palabras de 12 letras puedan tener 3 fallos sin margen de error
    int suggestionLimit = 300;

    Dictionary<string, Location> words = new Dictionary<string, Location>(); // Toda la info sobre cada palabra que aparece
    Dictionary<int, string> docs = new Dictionary<int, string>(); // Asignar un ID unico a cada documento

    // Cada palabra recortada apunta a su palabra original y al hecho de si es un lexema o no
    Dictionary<string, List<string>> variations = new Dictionary<string, List<string>>();

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
                    words.Add(word.Item1, new Location(files.Length));
                    // Borrando las derivadas anteriores
                    adding.Clear();
                    used.Clear();
                    PushDerivates(word.Item1); // Guardar las palabras derivadas en 'variations'
                }
                if (words[word.Item1][i] == null) { // Inicializar las ocurrencias en un doc especifico
                    words[word.Item1][i] = new Occurrences(i);
                }
                words[word.Item1][i].Push(word.Item2); // Agrega una nueva ocurrencia de la palabra en el doc
            }
        }

        crono.Stop();
        System.Console.WriteLine("✅ Indexado en {0}ms", crono.ElapsedMilliseconds);
    }

    public Dictionary<string, Location> Words { get { return words; } }

    public Dictionary<int, string> Docs { get { return docs; } }

    public Dictionary<string, List<string>> Variations { get { return variations; } }

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

    List<string> adding = new List<string>(); // Para llevar la cuenta de las subpalabras generadas
    HashSet<string> used = new HashSet<string>(); // Para asegurar que no se repitan subpalabras
    // Metodo para generar y almacenar las derivadas de una palabra
    void PushDerivates(string original, int pos = -1) {
        
        // Breakers para dejar de generar (REVISAR)
        if (pos == adding.Count) return;
        if (adding.Count > 0 && adding[adding.Count - 1].Length * 2 - 1 <= original.Length) return;
        if (adding.Count > 0 && adding[adding.Count - 1].Length + 4 <= original.Length) return;

        if (pos == -1) { // Caso para generar desde la palabra original
            GenerateSubStrings(original, original);
            pos = 0;
        }
        else { // Caso para generar desde las subpalabras ya generadas
            int startingCount = adding.Count;
            for (; pos < startingCount; pos++) {
                GenerateSubStrings(original, adding[pos]);
            }
        }

        if (adding.Count < suggestionLimit) { // Mientras no se haya superado el limite de generaciones
            PushDerivates(original, pos);
        }
    }

    // Dada una palabra, quitarle 1 caracter de cada posicion y almacenarla
    void GenerateSubStrings(string original, string subword) {
        
        // Iterando por cada posicion
        for (int i = 0; i < subword.Length && adding.Count < suggestionLimit; i++) {

            string derivate = subword.Remove(i, 1);

            // Si no se ha evaluado nunca, inicializar la lista
            if (!(variations.ContainsKey(derivate))) {
                variations[derivate] = new List<string>();
            }
            // Si no se ha usado la subpalabra para la original actual:
            if (!(used.Contains(derivate))) {
                adding.Add(derivate);
                variations[derivate].Add(original);
                used.Add(derivate);
            }
        }
    }
}