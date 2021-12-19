using System.Text;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace MoogleEngine;

public class IndexData {

    Dictionary<string, Occurrences[]> words = new Dictionary<string, Occurrences[]>(); // Las apariciones de cada palabra en los docs
    Dictionary<int, string> docs = new Dictionary<int, string>(); // Asignar un ID unico a cada documento

    public IndexData() {

        // Stopwatch crono = new Stopwatch();
        // System.Console.WriteLine("Inicio...");
        // crono.Start();

        string[] files = Directory.GetFiles("../Content", "*.txt", SearchOption.AllDirectories);
        for (int i = 0; i < files.Length; i++) { // Iterando por cada documento

            StreamReader reader = new StreamReader(files[i]);
            List<Tuple<string, int>> wordList = GetWords(reader.ReadToEnd()); // Recibe el contenido crudo
            docs.Add(i, files[i]); // Asignar un ID al documento
            
            foreach (var word in wordList) {
                
                if (!words.ContainsKey(word.Item1)) { // Inicializar el array de docs de cada palabra
                    words.Add(word.Item1, new Occurrences[files.Length]);
                }
                if (words[word.Item1][i] == null) { // Inicializar las ocurrencias en un doc especifico
                    words[word.Item1][i] = new Occurrences(i);
                }
                words[word.Item1][i].Push(word.Item2); // Agrega una nueva ocurrencia de la palabra en el doc
            }
        }

        // crono.Stop();
        // System.Console.WriteLine("Indexado en {0}ms", crono.ElapsedMilliseconds);

        // DEBUGUEO DEL DICCIONARIO
        // int k = 0;
        // foreach (var item in words) {
        //     k++;
        //     if (k == 40) break;

        //     System.Console.WriteLine(item.Key + ": ");
        //     foreach (var occ in item.Value) {
        //         if (occ != null) {
        //             System.Console.WriteLine("\t: {0}, {1}", occ.Id, occ.StartPos.Count);
        //         }
        //     }
        // }
    }

    List<Tuple<string, int>> GetWords(string content) { // Devuelve la lista de las palabras existentes y su ubicacion
        List<Tuple<string, int>> result = new List<Tuple<string, int>> (); // <palabra, posicionDeInicio, posicionFinal + 1>

        StringBuilder temp = new StringBuilder();
        int start = 0;
        for (int i = 0; i < content.Length; i++) {

            string c = content[i].ToString().ToLower();
            if (Regex.IsMatch(c, "[a-z0-9áéíóúüñ]")) {
                switch (c) {
                    case "á":
                        c = "a";
                        break;
                    case "é":
                        c = "e";
                        break;
                    case "í":
                        c = "i";
                        break;
                    case "ó":
                        c = "o";
                        break;
                    case "ú":
                    case "ü":
                        c = "u";
                        break;
                }
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

        return result;
    }
}