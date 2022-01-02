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
}