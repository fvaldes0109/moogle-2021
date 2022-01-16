namespace MoogleEngine;

// Representa un resultado con datos minimalistas
public class PartialItem {

    public PartialItem(string word, int document, float multiplier = 1.0f, string original = "") {
        this.Word = word;
        this.Document = document;
        this.Multiplier = multiplier;
        this.Original = original;
    }

    // La palabra buscada
    public string Word { get; private set; }

    // Es el ID del documento
    public int Document { get; private set; }

    // Multiplicador de relevancia
    public float Multiplier { get; private set; } 

    // Palabra original buscada (en caso de que Word se haya generado por una sugerencia.
    // Si no fue asi, Original estara vacia)
    public string Original { get; private set; }

    // Incrementa el multiplicador de la palabra
    public void Multiply(float value) {
        this.Multiplier *= value;
    }

    public void Multiply(int value) {
        this.Multiplier *= value;
    }

    // Devuelve la cantidad de documentos diferentes en una lista de parciales
    public static int CountDocuments(List<PartialItem> partials) {
        
        HashSet<int> docs = new HashSet<int>();

        foreach (var item in partials) {
            docs.Add(item.Document);
        }
        return docs.Count;
    }
}