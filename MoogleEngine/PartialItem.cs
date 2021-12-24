namespace MoogleEngine;

public class PartialItem { // Representa los datos minimos de un resultado

    public PartialItem(string word, int document) {
        this.Word = word;
        this.Document = document;
    }

    public string Word { get; private set; }

    public int Document { get; private set; }
}