using System.Text;
using System.Xml.Serialization;
using System.Text.Json;

namespace MoogleEngine;

// Clase para guardar y cargar la cache de los datos indexados
public static class CacheManager {

    static string rootsPath = Path.Combine("..", "Cache", "roots.json");
    static string wordsPath = Path.Combine("..", "Cache", "words.json");
    static string docsPath = Path.Combine("..", "Cache", "docs.json");

    // Guardar la informacion de las raices generadas con Stemming
    public static void SaveRoots(Dictionary<string, List<string>> data) {

        if (File.Exists(rootsPath)) {
            File.Delete(rootsPath);
        }

        FileStream file = File.Create(rootsPath);
        file.Close();
        StreamWriter writer = new StreamWriter(rootsPath);

        string jsonString = JsonSerializer.Serialize(data);
        writer.Write(jsonString);

        writer.Close();
    }

    // Cargar la informacion de las raices
    public static Dictionary<string, List<string>> LoadRoots() {
        
        StreamReader reader = new StreamReader(rootsPath);

        Dictionary<string, List<string>>? result = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(reader.ReadToEnd());
        reader.Close();

        return result!;
    }

    // Guardar todo lo relacionado a palabras, su ubicacion, relevancia, etc
    public static void SaveWords(Dictionary<string, Dictionary<int, Occurrences>> data) {

        if (File.Exists(wordsPath)) {
            File.Delete(wordsPath);
        }

        FileStream file = File.Create(wordsPath);
        file.Close();
        StreamWriter writer = new StreamWriter(wordsPath);
        
        string jsonString = JsonSerializer.Serialize(data);
        writer.Write(jsonString);
        writer.Close();
    }

    // Cargar los datos de las palabras
    public static Dictionary<string, Dictionary<int, Occurrences>> LoadWords() {

        StreamReader reader = new StreamReader(wordsPath);

        Dictionary<string, Dictionary<int, Occurrences>>? result = JsonSerializer.Deserialize<Dictionary<string, Dictionary<int, Occurrences>>>(reader.ReadToEnd());
        reader.Close();

        return result!;
    }

    // Guardar las relaciones documento-id
    public static void SaveDocs(Dictionary<int, string> data) {

        if (File.Exists(docsPath)) {
            File.Delete(docsPath);
        }

        FileStream file = File.Create(docsPath);
        file.Close();
        StreamWriter writer = new StreamWriter(docsPath);

        string jsonString = JsonSerializer.Serialize(data);
        writer.Write(jsonString);
        writer.Close();
    }

    // Cargar las relaciones documento-id
    public static Dictionary<int, string> LoadDocs() {

        StreamReader reader = new StreamReader(docsPath);

        Dictionary<int, string>? result = JsonSerializer.Deserialize<Dictionary<int, string>>(reader.ReadToEnd());
        reader.Close();

        return result!;
    }
}