namespace MoogleEngine;

public class Location { // Almacena todos los datos de cada palabra

    Occurrences[] occurrences; // Las apariciones de la palabra en cada documento
    int amount; // La cantidad de documentos en que se encuentra

    public Location(int totalDocs) {
        this.occurrences = new Occurrences[totalDocs];
        amount = 0;
    }

    public int Size { get { return occurrences.Length; } }

    public int Amount {
        get {
            return amount;
        }
        set {
            amount = value;
        }
    }

    public Occurrences this[int i] {
        get {
            return this.occurrences[i];
        }
        set {
            this.occurrences[i] = value;
        }
    }
}