namespace MoogleEngine;

public class PartialItem { // Representa los datos minimos de un resultado

    public PartialItem(string word, int document, float multiplier = 1.0f, string original = "") {
        this.Word = word;
        this.Document = document;
        this.Multiplier = multiplier;
        this.Original = original;
    }

    public string Word { get; private set; }

    public int Document { get; private set; } // Es el ID del documento

    public float Multiplier { get; private set; } // Multiplicador de relevancia

    public string Original { get; private set; } // Palabra original buscada

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